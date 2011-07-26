//
// $Id$
//
// The contents of this file are subject to the Mozilla Public License
// Version 1.1 (the "License"); you may not use this file except in
// compliance with the License. You may obtain a copy of the License at
// http://www.mozilla.org/MPL/
//
// Software distributed under the License is distributed on an "AS IS"
// basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
// License for the specific language governing rights and limitations
// under the License.
//
// The Original Code is the IDPicker suite.
//
// The Initial Developer of the Original Code is Matt Chambers.
//
// Copyright 2009 Vanderbilt University
//
// Contributor(s): Surendra Dasaris
//


#include "pwiz/utility/misc/Std.hpp"
#include "pwiz/utility/misc/Filesystem.hpp"
#include "pwiz/data/common/diff_std.hpp"
#include "../freicore/freicore.h"
#include "boost/foreach_field.hpp"
#include "boost/thread/mutex.hpp"

#include "Parser.hpp"
#include "idpQonvert.hpp"
#include "../Lib/SQLite/sqlite3pp.h"
#include <iomanip>
//#include "svnrev.hpp"

using namespace freicore;
using namespace IDPicker;
namespace sqlite = sqlite3pp;
using std::setw;
using std::setfill;
using boost::format;


BEGIN_IDPICKER_NAMESPACE

int Version::Major()                {return 3;}
int Version::Minor()                {return 0;}
int Version::Revision()             {return 0;}//SVN_REV;}
string Version::LastModified()      {return "";}//SVN_REVDATE;}
string Version::str()
{
    std::ostringstream v;
    v << Major() << "." << Minor() << "." << Revision();
    return v.str();
}

RunTimeConfig* g_rtConfig;

int InitProcess( argList_t& args )
{
    cout << "IDPickerQonvert " << Version::str() << " (" << Version::LastModified() << ")\n" <<
            "FreiCore " << freicore::Version::str() << " (" << freicore::Version::LastModified() << ")\n" <<
            "" << endl;

	g_rtConfig = new RunTimeConfig;
	g_rtSharedConfig = (BaseRunTimeConfig*) g_rtConfig;
	g_numWorkers = GetNumProcessors();

	// First set the working directory, if provided
	for( size_t i=1; i < args.size(); ++i )
	{
		if( args[i] == "-workdir" && i+1 <= args.size() )
		{
			chdir( args[i+1].c_str() );
			args.erase( args.begin() + i );
		}
        else if( args[i] == "-cpus" && i+1 <= args.size() )
		{
			g_numWorkers = atoi( args[i+1].c_str() );
			args.erase( args.begin() + i );
		}
        else
			continue;

		args.erase( args.begin() + i );
		--i;
	}

    vector<string> extraFilepaths;

	for( size_t i=1; i < args.size(); ++i )
	{
		if( args[i] == "-cfg" && i+1 <= args.size() )
		{
			if( g_rtConfig->initializeFromFile( args[i+1] ) )
			{
				cerr << "Could not find runtime configuration at \"" << args[i+1] << "\"." << endl;
				return QONVERT_ERROR_RUNTIME_CONFIG_FILE_FAILURE;
			}
			args.erase( args.begin() + i );

		}
        else if( args[i] == "-b" && i+1 <= args.size() )
        {
            // read filepaths from file
            if( !bfs::exists(args[i+1]) )
            {
                cerr << "Could not find list file at \"" << args[i+1] << "\"." << endl;
                return QONVERT_ERROR_RUNTIME_CONFIG_FILE_FAILURE;
            }

            ifstream listFile(args[i+1].c_str());
            string line;
            while (getline(listFile, line))
                extraFilepaths.push_back(line);

			args.erase( args.begin() + i );
        }
        else
			continue;

		args.erase( args.begin() + i );
		--i;
	}

	if( g_rtConfig->inputFilepaths.empty() && args.size() < 2 )
	{
		cerr << "Not enough arguments.\nUsage: idpQonvert <analyzed data filemask> [<another filemask> ...]" << endl;
		return QONVERT_ERROR_NOT_ENOUGH_ARGUMENTS;
	}

	if( !g_rtConfig->initialized() )
	{
		if( g_rtConfig->initializeFromFile() )
		{
			cerr << "Could not find the default configuration file (hard-coded defaults in use)." << endl;
		}
	}

	// Command line overrides happen after config file has been distributed but before PTM parsing
	RunTimeVariableMap vars = g_rtConfig->getVariables();
	for( RunTimeVariableMap::iterator itr = vars.begin(); itr != vars.end(); ++itr )
	{
		string varName;
		varName += "-" + itr->first;

		for( size_t i=1; i < args.size(); ++i )
		{
			if( args[i] == varName && i+1 < args.size() )
			{
				//cout << varName << " " << itr->second << " " << args[i+1] << endl;
				itr->second = args[i+1];
				args.erase( args.begin() + i );
				args.erase( args.begin() + i );
				--i;
			}
		}
	}

	try
	{
		g_rtConfig->setVariables( vars );
	} catch( std::exception& e )
	{
		cerr << "Error overriding runtime variables: " << e.what() << endl;
		return QONVERT_ERROR_RUNTIME_CONFIG_OVERRIDE_FAILURE;
	}

	for( size_t i=1; i < args.size(); ++i )
	{
		if( args[i] == "-dump" )
		{
			g_rtConfig->dump();
			g_residueMap->dump();
			args.erase( args.begin() + i );
			--i;
		}
        else if( args[i][0] == '-' )
		{
			cerr << "Warning: ignoring unrecognized parameter \"" << args[i] << "\"" << endl;
			args.erase( args.begin() + i );
			--i;
		}
	}

    args.insert(args.end(), extraFilepaths.begin(), extraFilepaths.end());

	return 0;
}

struct ImportSettingsHandler : public Parser::ImportSettingsCallback
{
    virtual void operator() (const vector<Parser::ConstAnalysisPtr>& distinctAnalyses, bool& cancel) const
    {
        typedef pair<string, string> AnalysisParameter;

        if (distinctAnalyses.size() > 1)
        {
            ostringstream error;
            error << "Warning: multiple distinct analyses detected in the input files. IdpQonvert settings apply to every analysis." << endl;

            for (size_t i=0; i < distinctAnalyses.size(); ++i)
            {
                const Parser::Analysis& analysis = *distinctAnalyses[i];
                error << "Analysis " << analysis.name << " (" << analysis.softwareName << " " << analysis.softwareVersion << ")" << endl;
                error << "\tstartTime: " << analysis.startTime << endl;
                error << "\tinputFilepaths:" << endl;
                BOOST_FOREACH(const string& filepath, analysis.filepaths)
                    error << "\t\t" << filepath << endl;

                if (i == 0)
                {
                    vector<string> firstParameters, secondParameters;
                    BOOST_FOREACH(const AnalysisParameter& kvp, analysis.parameters)
                        firstParameters.push_back(kvp.first + ": " + kvp.second);
                    BOOST_FOREACH(const AnalysisParameter& kvp, distinctAnalyses[1]->parameters)
                        secondParameters.push_back(kvp.first + ": " + kvp.second);

                    vector<string> a_b, b_a;
                    pwiz::data::diff_impl::vector_diff(firstParameters, secondParameters, a_b, b_a);

                    error << "\tdistinct parameters in first analysis: " << bal::join(a_b, ", ") << endl;
                    error << "\tdistinct parameters in second analysis: " << bal::join(b_a, ", ") << endl;
                    //BOOST_FOREACH(const AnalysisParameter& kvp, analysis.parameters)
                    //    cerr << "\t\t" << kvp.first << ": " << kvp.second << endl;
                    //error << endl;
                }
            }
            cerr << error.str() << endl;
        }

        // verify the protein database from the imported files matches ProteinDatabase
        BOOST_FOREACH(const Parser::ConstAnalysisPtr& analysisPtr, distinctAnalyses)
        {
            const Parser::Analysis& analysis = *analysisPtr;

            if (!bal::iequals(bfs::path(analysis.importSettings.proteinDatabaseFilepath).replace_extension("").filename(),
                              bfs::path(g_rtConfig->ProteinDatabase).replace_extension("").filename()))
                throw runtime_error("ProteinDatabase " + bfs::path(g_rtConfig->ProteinDatabase).filename() +
                                    " does not match " + bfs::path(analysis.importSettings.proteinDatabaseFilepath).filename());

            analysis.importSettings.proteinDatabaseFilepath = g_rtConfig->ProteinDatabase;

            Qonverter::Settings& settings = analysis.importSettings.qonverterSettings;
            settings.qonverterMethod = g_rtConfig->QonverterMethod;
            settings.kernel = g_rtConfig->Kernel;
            settings.chargeStateHandling = g_rtConfig->ChargeStateHandling;
            settings.terminalSpecificityHandling = g_rtConfig->TerminalSpecificityHandling;
            settings.missedCleavagesHandling = g_rtConfig->MissedCleavagesHandling;
            settings.massErrorHandling = g_rtConfig->MassErrorHandling;
            settings.decoyPrefix = g_rtConfig->DecoyPrefix;
            settings.scoreInfoByName = g_rtConfig->scoreInfoByName;
        }
    }
};

struct UserFeedbackIterationListener : public IterationListener
{
    struct PersistentUpdateMessage
    {
        size_t iterationIndex;
        size_t iterationCount; // 0 == unknown
        string message;

        PersistentUpdateMessage() {}

        PersistentUpdateMessage(const UpdateMessage& updateMessage)
        {
            iterationIndex = updateMessage.iterationIndex;
            iterationCount = updateMessage.iterationCount;
            message = updateMessage.message;
        }
    };

    boost::mutex mutex;
    map<string, PersistentUpdateMessage> lastUpdateByFilepath;
    string currentMessage; // when the message changes, make a new line

    virtual Status update(const UpdateMessage& updateMessage)
    {
        vector<string> parts;
        bal::split(parts, updateMessage.message, bal::is_any_of("*"));

        // parts[0] must be filepath, parts[1] must be original update message
        if (parts.size() != 2)
        {
            cerr << "Invalid update message: " << updateMessage.message << endl;
            return Status_Cancel;
        }

        const string& originalMessage = parts[1];

        boost::mutex::scoped_lock lock(mutex);

        // when the message changes, clear lastUpdateByFilepath and make a new line
        if (currentMessage.empty() || currentMessage != originalMessage)
        {
            if (!currentMessage.empty())
                cout << endl;
            lastUpdateByFilepath.clear();
            currentMessage = originalMessage;
        }

        lastUpdateByFilepath[parts[0]] = PersistentUpdateMessage(updateMessage);

        vector<string> updateStrings;

        BOOST_FOREACH_FIELD((const string& filepath)(PersistentUpdateMessage& updateMessage), lastUpdateByFilepath)
        {
            // create a message for each file like "source (message: index/count)"
            string source = bfs::path(filepath).replace_extension("").filename();
            int index = updateMessage.iterationIndex;
            int count = updateMessage.iterationCount;
            const string& message = originalMessage;

            if (index == 0 && count == 0)
            {
                updateStrings.push_back((boost::format("%1% (%2%)")
                                         % source % message).str());
            }
            else if (count > 0)
            {
                updateStrings.push_back((boost::format("%1% (%2%: %3%/%4%)")
                                         % source % message % (index+1) % count).str());
            }
            else
                updateStrings.push_back((boost::format("%1% (%2%: %3%)")
                                         % source % message % (index+1)).str());
        }

        cout << "\r" << bal::join(updateStrings, "; ") << flush;

        return Status_Ok;
    }
};

END_IDPICKER_NAMESPACE


void summarizeQonversion(const string& filepath)
{
    sqlite::database idpDb(filepath);

    string sql = "SELECT Name, COUNT(DISTINCT Spectrum), COUNT(DISTINCT Peptide)"
                 "  FROM PeptideSpectrumMatch"
                 "  JOIN Spectrum s ON Spectrum=s.Id"
                 "  JOIN SpectrumSource ss ON Source=ss.Id"
                 " WHERE QValue < " + lexical_cast<string>(g_rtConfig->MaxFDR) +
                 " GROUP BY Source";

    sqlite::query summaryQuery(idpDb, sql.c_str());

    BOOST_FOREACH(sqlite::query::rows row, summaryQuery)
    {
        string source;
        int spectra, peptides;
        boost::tie(source, spectra, peptides) = row.get_columns<string, int, int>(0, 1, 2);
        cout << left << setw(8) << spectra
             << left << setw(9) << peptides
             << source << endl;
    }
}


int main( int argc, char* argv[] )
{
    try
    {
	    argList_t args( argv, argv+argc );

	    int rv;
        if( ( rv = InitProcess(args) ) > 0 )
		    return rv;

	    for( size_t i = 1; i < args.size(); ++i )
		    FindFilesByMask( args[i], g_rtConfig->inputFilepaths );

	    if( g_rtConfig->inputFilepaths.empty() )
	    {
		    cout << "Error: no files found matching input filemasks." << endl;
		    return QONVERT_ERROR_NO_INPUT_FILES_FOUND;
	    }

        vector<string> idpDbFilepaths, parserFilepaths;
        BOOST_FOREACH(const string& filepath, g_rtConfig->inputFilepaths)
            if (bfs::path(filepath).extension() == ".idpDB")
                idpDbFilepaths.push_back(filepath);
            else
                parserFilepaths.push_back(filepath);

        Parser parser;

        // update on the first spectrum, the last spectrum, the 100th spectrum, the 200th spectrum, etc.
        const size_t iterationPeriod = 100;
        IterationListenerRegistry ilr;
        ilr.addListenerWithTimer(IterationListenerPtr(new UserFeedbackIterationListener), 1.0);

        parser.importSettingsCallback = Parser::ImportSettingsCallbackPtr(new ImportSettingsHandler);
        parser.parse(parserFilepaths, g_numWorkers, &ilr);

        // output summary statistics for each input file
        cout << "\nSpectra Peptides Filepath\n"
                "-------------------------\n";

        BOOST_FOREACH(const string& filepath, parserFilepaths)
            summarizeQonversion(bfs::path(filepath).replace_extension(".idpDB").string());

        BOOST_FOREACH(const string& filepath, idpDbFilepaths)
        {
            Qonverter qonverter;
            qonverter.logQonversionDetails = g_rtConfig->WriteQonversionDetails;
                
            Qonverter::Settings& settings = qonverter.settingsByAnalysis[0];
            settings.qonverterMethod = g_rtConfig->QonverterMethod;
            settings.kernel = g_rtConfig->Kernel;
            settings.chargeStateHandling = g_rtConfig->ChargeStateHandling;
            settings.terminalSpecificityHandling = g_rtConfig->TerminalSpecificityHandling;
            settings.missedCleavagesHandling = g_rtConfig->MissedCleavagesHandling;
            settings.massErrorHandling = g_rtConfig->MassErrorHandling;
            settings.decoyPrefix = g_rtConfig->DecoyPrefix;
            settings.scoreInfoByName = g_rtConfig->scoreInfoByName;

            qonverter.reset(filepath);
            qonverter.qonvert(filepath);

            summarizeQonversion(filepath);
        }
    }
    catch (exception& e)
    {
        cerr << "\nError: " << e.what() << endl;
        return QONVERT_ERROR_UNHANDLED_EXCEPTION;
    }
    catch (...)
    {
        cerr << "\nError: unhandled exception." << endl;
        return QONVERT_ERROR_UNHANDLED_EXCEPTION;
    }

	return 0;
}