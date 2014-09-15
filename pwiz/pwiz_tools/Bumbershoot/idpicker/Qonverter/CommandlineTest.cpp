//
// $Id$
//
// Licensed under the Apache License, Version 2.0 (the "License"); 
// you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at 
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software 
// distributed under the License is distributed on an "AS IS" BASIS, 
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
// See the License for the specific language governing permissions and 
// limitations under the License.
//
// The Original Code is the IDPicker project.
//
// The Initial Developer of the Original Code is Matt Chambers.
//
// Copyright 2014 Vanderbilt University
//
// Contributor(s):
//

#include "pwiz/utility/misc/unit.hpp"
#include "pwiz/utility/misc/Std.hpp"
#include <boost/foreach_field.hpp>
#include "pwiz/utility/chemistry/Ion.hpp"
#include "pwiz/data/proteome/Digestion.hpp"
#include "pwiz/data/proteome/AminoAcid.hpp"
#include "pwiz/utility/misc/SHA1Calculator.hpp"
#include "Embedder.hpp"
#include <boost/assign/list_of.hpp> // for 'list_of()'
#include <boost/assign.hpp>
#include <boost/range/adaptor/transformed.hpp>
#include <boost/thread.hpp>
#include <sqlite3pp.h>


using namespace pwiz::proteome;
using namespace pwiz::chemistry;
using namespace pwiz::util;
using namespace boost::assign;


inline std::string unit_assert_exception_thrown_message(const char* filename, int line, const char* expression, const std::string& exception)
{
    std::ostringstream oss;
    oss << "[" << filename << ":" << line << "] Assertion \"" << expression << "\" was not expected to throw, but threw " << exception;
    return oss.str();
}

#define unit_assert_does_not_throw(x, exception) \
    { \
        bool threw = false; \
        try { (x); } \
        catch (exception&) \
        { \
            threw = true; \
        } \
        if (threw) \
            throw std::runtime_error(unit_assert_exception_thrown_message(__FILE__, __LINE__, #x, #exception)); \
    }


#ifdef WIN32
const char* commandQuote = "\""; // workaround for weird behavior with Win32's system() call, which needs quotes around the entire command-line if the command-line has quoted arguments (like filepaths with spaces)
#else
const char* commandQuote = "";
#endif


// find filenames or file extensions matching trailingFilename
vector<size_t> findTrailingFilename(const string& trailingFilename, const vector<string>& args)
{
    vector<size_t> matches;
    for (size_t i=0; i < args.size(); ++i)
        if (bal::iends_with(args[i], trailingFilename))
            matches.push_back(i);
    return matches;
}

size_t findOneFilename(const string& filename, const vector<string>& args)
{
    vector<size_t> matches = findTrailingFilename(filename, args);
    if (matches.empty()) throw runtime_error("[findOneFilename] No match for filename \"" + filename + "\"");
    if (matches.size() > 1) throw runtime_error("[findOneFilename] More than one match for filename \"" + filename + "\"");
    return matches[0];
}

struct path_stringer
{
    typedef string result_type;
    result_type operator()(const bfs::path& x) const { return x.string(); }
};


void testIdpQonvert(const string& idpQonvertPath, const bfs::path& testDataPath)
{
    // clean up existing intermediate files
    vector<bfs::path> intermediateFiles;
    pwiz::util::expand_pathmask(testDataPath / "*.idpDB", intermediateFiles);
    pwiz::util::expand_pathmask(testDataPath / "broken.pepXML", intermediateFiles);
    BOOST_FOREACH(const bfs::path& intermediateFile, intermediateFiles)
        bfs::remove(intermediateFile);

    vector<bfs::path> idFiles;
    pwiz::util::expand_pathmask(testDataPath / "*.pep.xml", idFiles);
    pwiz::util::expand_pathmask(testDataPath / "*.pepXML", idFiles);
    pwiz::util::expand_pathmask(testDataPath / "*.mzid", idFiles);
    if (idFiles.empty())
        throw runtime_error("[testIdpQonvert] No identification files found in test path \"" + testDataPath.string() + "\"");


    // <idpQonvertPath> <matchPaths>
    string command = (format("%1%\"%2%\" \"%3%\" -DecoyPrefix XXX%1%") % commandQuote % idpQonvertPath % bal::join(idFiles | boost::adaptors::transformed(path_stringer()), "\" \"")).str();
    cout << endl << command << endl;
    unit_assert_operator_equal(0, system(command.c_str()));

    vector<bfs::path> idpDbFiles;
    BOOST_FOREACH(const bfs::path& idFile, idFiles)
    {
        idpDbFiles.push_back(idFile);
        idpDbFiles.back().replace_extension(".idpDB");

        unit_assert(bfs::exists(idpDbFiles.back()));
        sqlite3pp::database db(idpDbFiles.back().string());
        unit_assert_does_not_throw(sqlite3pp::query(db, "SELECT * FROM IntegerSet").begin(), sqlite3pp::database_error);

        // test analysis name differentiation
        string softwareName = sqlite3pp::query(db, "SELECT SoftwareName FROM Analysis").begin()->get<string>(0);
        string analysisName = sqlite3pp::query(db, "SELECT Name FROM Analysis").begin()->get<string>(0);
        if (softwareName == "MyriMatch")
        {
            unit_assert(bal::contains(analysisName, "MinTerminiCleavages"));
            if (bal::contains(analysisName, "MinTerminiCleavages=1"))
                unit_assert_operator_equal("MyriMatch 2.2.140 (MinTerminiCleavages=1, MonoPrecursorMzTolerance=50ppm, PrecursorMzToleranceRule=mono, parent tolerance minus value=50.0 ppm, parent tolerance plus value=50.0 ppm)", analysisName);
            else
                unit_assert_operator_equal("MyriMatch 2.2.140 (MinTerminiCleavages=0, MonoPrecursorMzTolerance=20ppm, PrecursorMzToleranceRule=auto, parent tolerance minus value=20.0 ppm, parent tolerance plus value=20.0 ppm)", analysisName);
        }
        else if (softwareName == "MS-GF+")
        {
            unit_assert(bal::contains(analysisName, "Instrument"));
            if (bal::contains(analysisName, "Instrument=LowRes"))
                unit_assert_operator_equal("MS-GF+ Beta (v10072) (FragmentMethod=As written in the spectrum or CID if no info, Instrument=LowRes, NumTolerableTermini=0, parent tolerance minus value=20.0 ppm, parent tolerance plus value=20.0 ppm)", analysisName);
            else
                unit_assert_operator_equal("MS-GF+ Beta (v10072) (FragmentMethod=HCD, Instrument=QExactive, NumTolerableTermini=1, parent tolerance minus value=50.0 ppm, parent tolerance plus value=50.0 ppm)", analysisName);
        }
        else
            throw runtime_error("[testIdpQonvert] Software name is not one of the expected values.");
    }


    // test overwrite of existing idpDBs (should succeed, but idpDBs should be unmodified since idpDBs exist)
    cout << endl << command << endl;
    string oldHash = SHA1Calculator::hashFile(idpDbFiles[0].string());
    unit_assert_operator_equal(0, system(command.c_str()));
    unit_assert_operator_equal(oldHash, SHA1Calculator::hashFile(idpDbFiles[0].string()));

    {
        // test embedding gene metadata in existing idpDB
        command = (format("%1%\"%2%\" \"%3%\" -DecoyPrefix XXX -EmbedGeneMetadata 1%1%") % commandQuote % idpQonvertPath % idpDbFiles[0].string()).str();
        cout << endl << command << endl;
        unit_assert_operator_equal(0, system(command.c_str()));

        sqlite3pp::database db(idpDbFiles[0].string());
        unit_assert(sqlite3pp::query(db, "SELECT GeneId FROM Protein").begin()->get<string>(0).length() > 0);
    }

    {
        // test embedding gene metadata while overwriting existing idpDBs (should succeed with OverwriteExistingFiles=1)
        command = (format("%1%\"%2%\" \"%3%\" -DecoyPrefix XXX -OverwriteExistingFiles 1 -EmbedGeneMetadata 1%1%") % commandQuote % idpQonvertPath % bal::join(idFiles | boost::adaptors::transformed(path_stringer()), "\" \"")).str();
        cout << endl << command << endl;
        unit_assert_operator_equal(0, system(command.c_str()));

        sqlite3pp::database db(idpDbFiles[0].string());
        unit_assert(sqlite3pp::query(db, "SELECT GeneId FROM Protein").begin()->get<string>(0).length() > 0);
    }

    {
        // test overwrite of existing idpDBs (should succeed with OverwriteExistingFiles=1, idpDB file hashes should not match due to timestamp difference, gene metadata should be gone)
        command = (format("%1%\"%2%\" \"%3%\" -DecoyPrefix XXX -OverwriteExistingFiles 1%1%") % commandQuote % idpQonvertPath % bal::join(idFiles | boost::adaptors::transformed(path_stringer()), "\" \"")).str();
        cout << endl << command << endl;
        boost::this_thread::sleep(bpt::seconds(1));
        unit_assert_operator_equal(0, system(command.c_str()));
        unit_assert(oldHash != SHA1Calculator::hashFile(idpDbFiles[0].string()));

        sqlite3pp::database db(idpDbFiles[0].string());
        unit_assert(sqlite3pp::query(db, "SELECT GeneId FROM Protein").begin()->get<string>(0).length() == 0);
    }

    {
        // test ONLY embedding gene metadata in existing idpDB; the bogus DecoyPrefix should be ignored
        command = (format("%1%\"%2%\" \"%3%\" -DecoyPrefix XYZ -EmbedGeneMetadata 1 -EmbedOnly 1%1%") % commandQuote % idpQonvertPath % idpDbFiles[0].string()).str();
        cout << endl << command << endl;
        unit_assert_operator_equal(0, system(command.c_str()));

        sqlite3pp::database db(idpDbFiles[0].string());
        unit_assert(sqlite3pp::query(db, "SELECT GeneId FROM Protein").begin()->get<string>(0).length() > 0);
    }

    {
        // create a broken pepXML and check that errors or skipping errors are handled as intended
        string brokenPepXmlFilename = (testDataPath / "broken.pepXML").string();
        ofstream brokenPepXML(brokenPepXmlFilename.c_str());
        brokenPepXML << "<?xml version=\"1.0\" encoding=\"ISO-8859-1\"?>\n<msms_pipeline_analysis></msms_pipeline_analysis>" << endl;
        brokenPepXML.close();

        command = (format("%1%\"%2%\" \"%3%\" -DecoyPrefix XXX%1%") % commandQuote % idpQonvertPath % brokenPepXmlFilename).str();
        cout << endl << command << endl;
        unit_assert(0 < system(command.c_str()));

        command = (format("%1%\"%2%\" \"%3%\" -DecoyPrefix XXX -SkipSourceOnError 1%1%") % commandQuote % idpQonvertPath % brokenPepXmlFilename).str();
        cout << endl << command << endl;
        unit_assert_operator_equal(0, system(command.c_str()));
    }


    // test qonversion of existing idpDB


    // test embedding scan times
    {
        // test it when importing pepXML
        {
            command = (format("%1%\"%2%\" \"%3%\" -DecoyPrefix XXX -OverwriteExistingFiles 1 -EmbedSpectrumScanTimes 1%1%") % commandQuote % idpQonvertPath % bal::join(idFiles | boost::adaptors::transformed(path_stringer()), "\" \"")).str();
            cout << endl << command << endl;
            unit_assert_operator_equal(0, system(command.c_str()));

            sqlite3pp::database db(idpDbFiles[0].string());
            unit_assert(sqlite3pp::query(db, "SELECT COUNT(*) FROM Spectrum WHERE ScanTimeInSeconds > 0").begin()->get<int>(0) > 0);
        }

        // test it on an existing idpDB
        {
            command = (format("%1%\"%2%\" \"%3%\" -EmbedSpectrumScanTimes 1 -EmbedOnly 1%1%") % commandQuote % idpQonvertPath % idpDbFiles[0]).str();
            cout << endl << command << endl;
            unit_assert_operator_equal(0, system(command.c_str()));

            sqlite3pp::database db(idpDbFiles[0].string());
            unit_assert(sqlite3pp::query(db, "SELECT COUNT(*) FROM Spectrum WHERE ScanTimeInSeconds > 0").begin()->get<int>(0) > 0);
        }
    }


    // test embedding spectra
    {
        // test it when importing pepXML
        {
            command = (format("%1%\"%2%\" \"%3%\" -DecoyPrefix XXX -OverwriteExistingFiles 1 -EmbedSpectrumSources 1%1%") % commandQuote % idpQonvertPath % bal::join(idFiles | boost::adaptors::transformed(path_stringer()), "\" \"")).str();
            cout << endl << command << endl;
            unit_assert_operator_equal(0, system(command.c_str()));

            sqlite3pp::database db(idpDbFiles[0].string());
            unit_assert(sqlite3pp::query(db, "SELECT COUNT(*) FROM Spectrum WHERE ScanTimeInSeconds > 0").begin()->get<int>(0) > 0);
            unit_assert(sqlite3pp::query(db, "SELECT COUNT(*) FROM SpectrumSourceMetadata WHERE MsDataBytes IS NOT NULL").begin()->get<int>(0) > 0);
        }

        // test it on an existing idpDB
        {
            command = (format("%1%\"%2%\" \"%3%\" -EmbedSpectrumSources 1 -EmbedOnly 1%1%") % commandQuote % idpQonvertPath % idpDbFiles[0]).str();
            cout << endl << command << endl;
            unit_assert_operator_equal(0, system(command.c_str()));

            sqlite3pp::database db(idpDbFiles[0].string());
            unit_assert(sqlite3pp::query(db, "SELECT COUNT(*) FROM Spectrum WHERE ScanTimeInSeconds > 0").begin()->get<int>(0) > 0);
            unit_assert(sqlite3pp::query(db, "SELECT COUNT(*) FROM SpectrumSourceMetadata WHERE MsDataBytes IS NOT NULL").begin()->get<int>(0) > 0);
        }
    }


    // test embedding quantitation
    // ITRAQ4plex
    {
        command = (format("%1%\"%2%\" \"%3%\" -DecoyPrefix XXX -OverwriteExistingFiles 1 -QuantitationMethod ITRAQ4plex%1%") % commandQuote % idpQonvertPath % bal::join(idFiles | boost::adaptors::transformed(path_stringer()), "\" \"")).str();
        cout << endl << command << endl;
        unit_assert_operator_equal(0, system(command.c_str()));

        sqlite3pp::database db(idpDbFiles[0].string());
        unit_assert(sqlite3pp::query(db, "SELECT COUNT(*) FROM SpectrumSource WHERE QuantitationMethod > 0").begin()->get<int>(0) > 0);
        unit_assert(sqlite3pp::query(db, "SELECT COUNT(*) FROM SpectrumQuantitation WHERE iTRAQ_ReporterIonIntensities IS NOT NULL").begin()->get<int>(0) > 0);
    }

    // ITRAQ8plex
    {
        command = (format("%1%\"%2%\" \"%3%\" -DecoyPrefix XXX -OverwriteExistingFiles 1 -QuantitationMethod ITRAQ8plex%1%") % commandQuote % idpQonvertPath % bal::join(idFiles | boost::adaptors::transformed(path_stringer()), "\" \"")).str();
        cout << endl << command << endl;
        unit_assert_operator_equal(0, system(command.c_str()));

        sqlite3pp::database db(idpDbFiles[0].string());
        unit_assert(sqlite3pp::query(db, "SELECT COUNT(*) FROM SpectrumSource WHERE QuantitationMethod > 0").begin()->get<int>(0) > 0);
        unit_assert(sqlite3pp::query(db, "SELECT COUNT(*) FROM SpectrumQuantitation WHERE iTRAQ_ReporterIonIntensities IS NOT NULL").begin()->get<int>(0) > 0);
    }

    // TMT2plex
    {
        command = (format("%1%\"%2%\" \"%3%\" -DecoyPrefix XXX -OverwriteExistingFiles 1 -QuantitationMethod TMT2plex%1%") % commandQuote % idpQonvertPath % bal::join(idFiles | boost::adaptors::transformed(path_stringer()), "\" \"")).str();
        cout << endl << command << endl;
        unit_assert_operator_equal(0, system(command.c_str()));

        sqlite3pp::database db(idpDbFiles[0].string());
        unit_assert(sqlite3pp::query(db, "SELECT COUNT(*) FROM SpectrumSource WHERE QuantitationMethod > 0").begin()->get<int>(0) > 0);
        unit_assert(sqlite3pp::query(db, "SELECT COUNT(*) FROM SpectrumQuantitation WHERE TMT_ReporterIonIntensities IS NOT NULL").begin()->get<int>(0) > 0);
    }

    // TMT6plex
    {
        command = (format("%1%\"%2%\" \"%3%\" -DecoyPrefix XXX -OverwriteExistingFiles 1 -QuantitationMethod TMT6plex%1%") % commandQuote % idpQonvertPath % bal::join(idFiles | boost::adaptors::transformed(path_stringer()), "\" \"")).str();
        cout << endl << command << endl;
        unit_assert_operator_equal(0, system(command.c_str()));

        sqlite3pp::database db(idpDbFiles[0].string());
        unit_assert(sqlite3pp::query(db, "SELECT COUNT(*) FROM SpectrumSource WHERE QuantitationMethod > 0").begin()->get<int>(0) > 0);
        unit_assert(sqlite3pp::query(db, "SELECT COUNT(*) FROM SpectrumQuantitation WHERE TMT_ReporterIonIntensities IS NOT NULL").begin()->get<int>(0) > 0);
    }
}


void testIdpAssemble(const string& idpQonvertPath, const string& idpAssemblePath, const bfs::path& testDataPath)
{
    string mergedOutputFilepath = (testDataPath / "merged.idpDB").string();

    // clean up existing intermediate files
    vector<bfs::path> intermediateFiles;
    pwiz::util::expand_pathmask(mergedOutputFilepath, intermediateFiles);
    BOOST_FOREACH(const bfs::path& intermediateFile, intermediateFiles)
        bfs::remove(intermediateFile);

    vector<bfs::path> idFiles;
    pwiz::util::expand_pathmask(testDataPath / "*.idpDB", idFiles);
    if (idFiles.empty())
        throw runtime_error("[testIdpAssemble] No idpDB files found in test path \"" + testDataPath.string() + "\"");

    // <idpAssemblePath> <matchPaths>
    string command = (format("%1%\"%2%\" \"%3%\" -MergedOutputFilepath \"%4%\"%1%") % commandQuote % idpAssemblePath % bal::join(idFiles | boost::adaptors::transformed(path_stringer()), "\" \"") % mergedOutputFilepath).str();
    cout << endl << command << endl;
    unit_assert_operator_equal(0, system(command.c_str()));

    unit_assert(bfs::exists(mergedOutputFilepath));
    sqlite3pp::database db(mergedOutputFilepath);
    unit_assert_does_not_throw(sqlite3pp::query(db, "SELECT * FROM IntegerSet").begin(), sqlite3pp::database_error);

    // test that MergedFiles table contains each original idpDB filepath
    BOOST_FOREACH(const bfs::path& idFile, idFiles)
    {
        cout << ("SELECT COUNT(*) FROM MergedFiles WHERE Filepath='" + idFile.string() + "'") << endl;
        unit_assert(0 < sqlite3pp::query(db, ("SELECT COUNT(*) FROM MergedFiles WHERE Filepath='" + idFile.string() + "'").c_str()).begin()->get<int>(0));
    }

    string defaultHash = SHA1Calculator::hashFile(mergedOutputFilepath);

    // test that rerunning on the merged idpDB with the same filter settings will not change the database
    command = (format("%1%\"%2%\" \"%3%\"%1%") % commandQuote % idpAssemblePath % mergedOutputFilepath).str();
    cout << endl << command << endl;
    unit_assert_operator_equal(0, system(command.c_str()));
    unit_assert_operator_equal(defaultHash, SHA1Calculator::hashFile(mergedOutputFilepath));

    // test filter arguments: that each filter results in the correct FilterHistory changes
    {
        // [-MaxFDRScore <real>]
        mergedOutputFilepath = (testDataPath / "merged-MaxFDRScore.idpDB").string();
        command = (format("%1%\"%2%\" \"%3%\" -MergedOutputFilepath \"%4%\" -MaxFDRScore 0.1%1%") % commandQuote % idpAssemblePath % bal::join(idFiles | boost::adaptors::transformed(path_stringer()), "\" \"") % mergedOutputFilepath).str();
        cout << endl << command << endl;
        unit_assert_operator_equal(0, system(command.c_str()));
        { sqlite3pp::database db(mergedOutputFilepath); unit_assert_operator_equal(0.1, sqlite3pp::query(db, "SELECT MaximumQValue FROM FilterHistory").begin()->get<double>(0)); }

        // [-MinDistinctPeptides <integer>]
        mergedOutputFilepath = (testDataPath / "merged-MinDistinctPeptides.idpDB").string();
        command = (format("%1%\"%2%\" \"%3%\" -MergedOutputFilepath \"%4%\" -MinDistinctPeptides 5%1%") % commandQuote % idpAssemblePath % bal::join(idFiles | boost::adaptors::transformed(path_stringer()), "\" \"") % mergedOutputFilepath).str();
        cout << endl << command << endl;
        unit_assert_operator_equal(0, system(command.c_str()));
        { sqlite3pp::database db(mergedOutputFilepath); unit_assert_operator_equal(5, sqlite3pp::query(db, "SELECT MinimumDistinctPeptides FROM FilterHistory").begin()->get<double>(0)); }

        // [-MinSpectra <integer>]
        mergedOutputFilepath = (testDataPath / "merged-MinSpectra.idpDB").string();
        command = (format("%1%\"%2%\" \"%3%\" -MergedOutputFilepath \"%4%\" -MinSpectra 5%1%") % commandQuote % idpAssemblePath % bal::join(idFiles | boost::adaptors::transformed(path_stringer()), "\" \"") % mergedOutputFilepath).str();
        cout << endl << command << endl;
        unit_assert_operator_equal(0, system(command.c_str()));
        { sqlite3pp::database db(mergedOutputFilepath); unit_assert_operator_equal(5, sqlite3pp::query(db, "SELECT MinimumSpectra FROM FilterHistory").begin()->get<double>(0)); }

        // [-MinAdditionalPeptides <integer>]
        mergedOutputFilepath = (testDataPath / "merged-MinAdditionalPeptides.idpDB").string();
        command = (format("%1%\"%2%\" \"%3%\" -MergedOutputFilepath \"%4%\" -MinAdditionalPeptides 15%1%") % commandQuote % idpAssemblePath % bal::join(idFiles | boost::adaptors::transformed(path_stringer()), "\" \"") % mergedOutputFilepath).str();
        cout << endl << command << endl;
        unit_assert_operator_equal(0, system(command.c_str()));
        { sqlite3pp::database db(mergedOutputFilepath); unit_assert_operator_equal(15, sqlite3pp::query(db, "SELECT MinimumAdditionalPeptides FROM FilterHistory").begin()->get<double>(0)); }

        // [-MinSpectraPerDistinctMatch <integer>]
        mergedOutputFilepath = (testDataPath / "merged-MinSpectraPerDistinctMatch.idpDB").string();
        command = (format("%1%\"%2%\" \"%3%\" -MergedOutputFilepath \"%4%\" -MinSpectraPerDistinctMatch 2%1%") % commandQuote % idpAssemblePath % bal::join(idFiles | boost::adaptors::transformed(path_stringer()), "\" \"") % mergedOutputFilepath).str();
        cout << endl << command << endl;
        unit_assert_operator_equal(0, system(command.c_str()));
        { sqlite3pp::database db(mergedOutputFilepath); unit_assert_operator_equal(2, sqlite3pp::query(db, "SELECT MinimumSpectraPerDistinctMatch FROM FilterHistory").begin()->get<double>(0)); }

        // [-MinSpectraPerDistinctPeptide <integer>]
        mergedOutputFilepath = (testDataPath / "merged-MinSpectraPerDistinctPeptide.idpDB").string();
        command = (format("%1%\"%2%\" \"%3%\" -MergedOutputFilepath \"%4%\" -MinSpectraPerDistinctPeptide 2%1%") % commandQuote % idpAssemblePath % bal::join(idFiles | boost::adaptors::transformed(path_stringer()), "\" \"") % mergedOutputFilepath).str();
        cout << endl << command << endl;
        unit_assert_operator_equal(0, system(command.c_str()));
        { sqlite3pp::database db(mergedOutputFilepath); unit_assert_operator_equal(2, sqlite3pp::query(db, "SELECT MinimumSpectraPerDistinctPeptide FROM FilterHistory").begin()->get<double>(0)); }

        // [-MaxProteinGroupsPerPeptide <integer>]
        mergedOutputFilepath = (testDataPath / "merged-MaxProteinGroupsPerPeptide.idpDB").string();
        command = (format("%1%\"%2%\" \"%3%\" -MergedOutputFilepath \"%4%\" -MaxProteinGroupsPerPeptide 2%1%") % commandQuote % idpAssemblePath % bal::join(idFiles | boost::adaptors::transformed(path_stringer()), "\" \"") % mergedOutputFilepath).str();
        cout << endl << command << endl;
        unit_assert_operator_equal(0, system(command.c_str()));
        { sqlite3pp::database db(mergedOutputFilepath); unit_assert_operator_equal(2, sqlite3pp::query(db, "SELECT MaximumProteinGroupsPerPeptide FROM FilterHistory").begin()->get<double>(0)); }
    }

    // TODO: test automatic output filename and that it is the same as the manually named one
    //command = (format("%1%\"%2%\" \"%3%\"%1%") % commandQuote % idpAssemblePath % bal::join(idFiles | boost::adaptors::transformed(path_stringer()), "\" \"")).str();
    //cout << endl << command << endl;
    //unit_assert_operator_equal(0, system(command.c_str()));
    //unit_assert(bfs::exists(testDataPath / "20120.idpDB"));
    //unit_assert_operator_equal(defaultHash, SHA1Calculator::hashFile((testDataPath / "20120.idpDB").string()));

    mergedOutputFilepath = (testDataPath / "merged.idpDB").string();

    // test assign source hierarchy
    {
        // test single layer hierarchy
        {
            string assemblyeFilepath = (testDataPath / "groups.txt").string();
            ofstream assemblyFile(assemblyeFilepath.c_str());
            assemblyFile << "/201203" << "\t201203-624176-12\n"
                         << "/201208" << "\t201208-378803\n";
            assemblyFile.close();

            command = (format("%1%\"%2%\" \"%3%\" -AssignSourceHierarchy \"%4%\"%1%") % commandQuote % idpAssemblePath % mergedOutputFilepath % assemblyeFilepath).str();
            cout << endl << command << endl;
            unit_assert_operator_equal(0, system(command.c_str()));
            sqlite3pp::database db(mergedOutputFilepath);
            unit_assert_operator_equal(3, sqlite3pp::query(db, "SELECT COUNT(*) FROM SpectrumSourceGroup").begin()->get<int>(0));
            unit_assert_operator_equal("/", sqlite3pp::query(db, "SELECT Name FROM SpectrumSourceGroup WHERE Id=1").begin()->get<string>(0));
            unit_assert_operator_equal("/201203", sqlite3pp::query(db, "SELECT Name FROM SpectrumSourceGroup WHERE Id=2").begin()->get<string>(0));
            unit_assert_operator_equal("/201208", sqlite3pp::query(db, "SELECT Name FROM SpectrumSourceGroup WHERE Id=3").begin()->get<string>(0));
            unit_assert_operator_equal(2, sqlite3pp::query(db, "SELECT Group_ FROM SpectrumSource WHERE Name='201203-624176-12'").begin()->get<int>(0));
            unit_assert_operator_equal(3, sqlite3pp::query(db, "SELECT Group_ FROM SpectrumSource WHERE Name='201208-378803'").begin()->get<int>(0));
        }

        // test multi-layer hierarchy
        {
            string assemblyeFilepath = (testDataPath / "groups.txt").string();
            ofstream assemblyFile(assemblyeFilepath.c_str());
            assemblyFile << "/201203/624176" << "\t201203-624176-12\n"
                         << "/201208/378803" << "\t201208-378803\n";
            assemblyFile.close();

            command = (format("%1%\"%2%\" \"%3%\" -AssignSourceHierarchy \"%4%\"%1%") % commandQuote % idpAssemblePath % mergedOutputFilepath % assemblyeFilepath).str();
            cout << endl << command << endl;
            unit_assert_operator_equal(0, system(command.c_str()));
            sqlite3pp::database db(mergedOutputFilepath);
            unit_assert_operator_equal(5, sqlite3pp::query(db, "SELECT COUNT(*) FROM SpectrumSourceGroup").begin()->get<int>(0));
            unit_assert_operator_equal(1, sqlite3pp::query(db, "SELECT COUNT(*) FROM SpectrumSourceGroup WHERE Name='/201203/624176'").begin()->get<int>(0));
            unit_assert_operator_equal(1, sqlite3pp::query(db, "SELECT COUNT(*) FROM SpectrumSourceGroup WHERE Name='/201208/378803'").begin()->get<int>(0));
            unit_assert_operator_equal("/201203/624176", sqlite3pp::query(db, "SELECT ssg.Name FROM SpectrumSource ss, SpectrumSourceGroup ssg WHERE ss.Name='201203-624176-12' AND Group_=ssg.Id").begin()->get<string>(0));
            unit_assert_operator_equal("/201208/378803", sqlite3pp::query(db, "SELECT ssg.Name FROM SpectrumSource ss, SpectrumSourceGroup ssg WHERE ss.Name='201208-378803' AND Group_=ssg.Id").begin()->get<string>(0));
        }
    }

    // test filtering a single file
    command = (format("%1%\"%2%\" \"%3%\" -MaxFDRScore 0.1%1%") % commandQuote % idpAssemblePath % mergedOutputFilepath).str();
    cout << endl << command << endl;
    unit_assert_operator_equal(0, system(command.c_str()));
    int filteredSpectraAfterMerge; { sqlite3pp::database db((testDataPath / "merged-MaxFDRScore.idpDB").string()); filteredSpectraAfterMerge = sqlite3pp::query(db, "SELECT FilteredSpectra FROM FilterHistory").begin()->get<int>(0); }
    int filteredSpectraAfterFilter; { sqlite3pp::database db(mergedOutputFilepath); filteredSpectraAfterFilter = sqlite3pp::query(db, "SELECT FilteredSpectra FROM FilterHistory LIMIT 1 OFFSET 1").begin()->get<int>(0); }
    unit_assert_operator_equal(filteredSpectraAfterMerge, filteredSpectraAfterFilter);

    // test embedding quantitation on a new file; the values embedded here will be tested in idpQuery
    // ITRAQ4plex
    // ITRAQ8plex
    // TMT2plex
    // TMT6plex
    {
        string mergedQuantifiedOutputFilepath = (testDataPath / "merged-ITRAQ8plex.idpDB").string();
        bfs::copy_file(mergedOutputFilepath, mergedQuantifiedOutputFilepath);
        command = (format("%1%\"%2%\" \"%3%\" -QuantitationMethod ITRAQ8plex -EmbedOnly 1%1%") % commandQuote % idpQonvertPath % mergedQuantifiedOutputFilepath).str();
        cout << endl << command << endl;
        unit_assert_operator_equal(0, system(command.c_str()));

        sqlite3pp::database db(mergedQuantifiedOutputFilepath);
        unit_assert(sqlite3pp::query(db, "SELECT COUNT(*) FROM SpectrumSource WHERE QuantitationMethod > 0").begin()->get<int>(0) > 0);
        unit_assert(sqlite3pp::query(db, "SELECT COUNT(*) FROM SpectrumQuantitation WHERE iTRAQ_ReporterIonIntensities IS NOT NULL").begin()->get<int>(0) > 0);
    }
}


void testIdpQuery(const string& idpQueryPath, const bfs::path& testDataPath)
{
    //    Protein, ProteinGroup, Cluster, Gene, GeneGroup
    //    ------------------------------
    //    Accession
    //    GeneId
    //    GeneGroup
    //    DistinctPeptides
    //    DistinctMatches
    //    FilteredSpectra
    //    IsDecoy
    //    Cluster
    //    ProteinGroup
    //    Length
    //    PercentCoverage
    //    Sequence
    //    Description
    //    TaxonomyId
    //    GeneName
    //    GeneFamily
    //    Chromosome
    //    GeneDescription
    //    iTRAQ4plex
    //    iTRAQ8plex
    //    TMT2plex
    //    TMT6plex
    //    PivotMatchesByGroup
    //    PivotMatchesBySource
    //    PivotPeptidesByGroup
    //    PivotPeptidesBySource
    //    PivotSpectraByGroup
    //    PivotSpectraBySource
    //    PivotITRAQByGroup
    //    PivotITRAQBySource
    //    PivotTMTByGroup
    //    PivotTMTBySource
    //    PeptideGroups
    //    PeptideSequences

    string inputFilepath = (testDataPath / "merged.idpDB").string();
    string outputFilepath = (testDataPath / "merged.tsv").string();
    string quantifiedInputFilepath = (testDataPath / "merged-ITRAQ8plex.idpDB").string();
    string quantifiedOutputFilepath = (testDataPath / "merged-ITRAQ8plex.tsv").string();
    string command;

    // clean up existing intermediate files
    vector<bfs::path> intermediateFiles;
    pwiz::util::expand_pathmask(testDataPath / "*.tsv", intermediateFiles);
    BOOST_FOREACH(const bfs::path& intermediateFile, intermediateFiles)
        bfs::remove(intermediateFile);

    vector<string> groupColumns;
    groupColumns +=  "Protein", "ProteinGroup", "Cluster";

    string mainColumns = " Accession,GeneId,GeneGroup,DistinctPeptides,DistinctMatches,FilteredSpectra,IsDecoy,Cluster,ProteinGroup"
                         ",Length,PercentCoverage,Sequence,Description,TaxonomyId,GeneName,GeneFamily,Chromosome,GeneDescription";

    // run through all group modes with all columns when no gene metadata is embedded
    BOOST_FOREACH(const string& groupColumn, groupColumns)
    {
        command = (format("%1%\"%2%\" %3% %4% \"%5%\"%1%") % commandQuote % idpQueryPath % groupColumn % mainColumns % inputFilepath).str();
        cout << endl << command << endl;
        unit_assert_operator_equal(0, system(command.c_str()));
        unit_assert(bfs::exists(outputFilepath));
        bfs::rename(outputFilepath, testDataPath / ("merged-no-quantitation-" + groupColumn + ".tsv"));

        command = (format("%1%\"%2%\" %3% %4%,iTRAQ4plex,PivotITRAQByGroup \"%5%\"%1%") % commandQuote % idpQueryPath % groupColumn % mainColumns % inputFilepath).str();
        cout << endl << command << endl;
        unit_assert_operator_equal(0, system(command.c_str()));
        unit_assert(bfs::exists(outputFilepath));
        bfs::rename(outputFilepath, testDataPath / ("merged-empty-iTRAQ4plex-" + groupColumn + ".tsv"));

        command = (format("%1%\"%2%\" %3% %4%,iTRAQ8plex,PivotITRAQBySource \"%5%\"%1%") % commandQuote % idpQueryPath % groupColumn % mainColumns % inputFilepath).str();
        cout << endl << command << endl;
        unit_assert_operator_equal(0, system(command.c_str()));
        unit_assert(bfs::exists(outputFilepath));
        bfs::rename(outputFilepath, testDataPath / ("merged-empty-iTRAQ8plex-" + groupColumn + ".tsv"));

        command = (format("%1%\"%2%\" %3% %4%,TMT2plex,PivotTMTByGroup \"%5%\"%1%") % commandQuote % idpQueryPath % groupColumn % mainColumns % inputFilepath).str();
        cout << endl << command << endl;
        unit_assert_operator_equal(0, system(command.c_str()));
        unit_assert(bfs::exists(outputFilepath));
        bfs::rename(outputFilepath, testDataPath / ("merged-empty-TMT2plex-" + groupColumn + ".tsv"));

        command = (format("%1%\"%2%\" %3% %4%,TMT6plex,PivotTMTBySource \"%5%\"%1%") % commandQuote % idpQueryPath % groupColumn % mainColumns % inputFilepath).str();
        cout << endl << command << endl;
        unit_assert_operator_equal(0, system(command.c_str()));
        unit_assert(bfs::exists(outputFilepath));
        bfs::rename(outputFilepath, testDataPath / ("merged-empty-TMT6plex-" + groupColumn + ".tsv"));

        command = (format("%1%\"%2%\" %3% %4%,TMT6plex,PivotTMTBySource \"%5%\"%1%") % commandQuote % idpQueryPath % groupColumn % mainColumns % inputFilepath).str();
        cout << endl << command << endl;
        unit_assert_operator_equal(0, system(command.c_str()));
        unit_assert(bfs::exists(outputFilepath));
        bfs::rename(outputFilepath, testDataPath / ("merged-empty-TMT6plex-" + groupColumn + ".tsv"));

        command = (format("%1%\"%2%\" %3% %4%,iTRAQ8plex,PivotITRAQBySource \"%5%\"%1%") % commandQuote % idpQueryPath % groupColumn % mainColumns % quantifiedInputFilepath).str();
        cout << endl << command << endl;
        unit_assert_operator_equal(0, system(command.c_str()));
        unit_assert(bfs::exists(quantifiedOutputFilepath));
        bfs::rename(quantifiedOutputFilepath, testDataPath / ("merged-quantified-iTRAQ8plex-" + groupColumn + ".tsv"));
    }


    // test that Gene and GeneGroup modes issue an error when gene metadata is not embedded
    command = (format("%1%\"%2%\" %3% %4% \"%5%\"%1%") % commandQuote % idpQueryPath % "Gene" % mainColumns % inputFilepath).str();
    cout << endl << command << endl;
    unit_assert(0 < system(command.c_str()));
    unit_assert(!bfs::exists(outputFilepath));

    command = (format("%1%\"%2%\" %3% %4% \"%5%\"%1%") % commandQuote % idpQueryPath % "GeneGroup" % mainColumns % inputFilepath).str();
    cout << endl << command << endl;
    unit_assert(0 < system(command.c_str()));
    unit_assert(!bfs::exists(outputFilepath));


    groupColumns += "Gene", "GeneGroup";
    IDPicker::Embedder::embedGeneMetadata(inputFilepath);
    IDPicker::Embedder::embedGeneMetadata(quantifiedInputFilepath);

    // run through all the group modes with all columns after embedding gene metadata
    BOOST_FOREACH(const string& groupColumn, groupColumns)
    {
        command = (format("%1%\"%2%\" %3% %4% \"%5%\"%1%") % commandQuote % idpQueryPath % groupColumn % mainColumns % inputFilepath).str();
        cout << endl << command << endl;
        unit_assert_operator_equal(0, system(command.c_str()));
        unit_assert(bfs::exists(outputFilepath));
        bfs::rename(outputFilepath, testDataPath / ("merged-no-quantitation-" + groupColumn + ".tsv"));

        command = (format("%1%\"%2%\" %3% %4%,iTRAQ4plex,PivotITRAQByGroup \"%5%\"%1%") % commandQuote % idpQueryPath % groupColumn % mainColumns % inputFilepath).str();
        cout << endl << command << endl;
        unit_assert_operator_equal(0, system(command.c_str()));
        unit_assert(bfs::exists(outputFilepath));
        bfs::rename(outputFilepath, testDataPath / ("merged-empty-iTRAQ4plex-" + groupColumn + ".tsv"));

        command = (format("%1%\"%2%\" %3% %4%,iTRAQ8plex,PivotITRAQBySource \"%5%\"%1%") % commandQuote % idpQueryPath % groupColumn % mainColumns % inputFilepath).str();
        cout << endl << command << endl;
        unit_assert_operator_equal(0, system(command.c_str()));
        unit_assert(bfs::exists(outputFilepath));
        bfs::rename(outputFilepath, testDataPath / ("merged-empty-iTRAQ8plex-" + groupColumn + ".tsv"));

        command = (format("%1%\"%2%\" %3% %4%,TMT2plex,PivotTMTByGroup \"%5%\"%1%") % commandQuote % idpQueryPath % groupColumn % mainColumns % inputFilepath).str();
        cout << endl << command << endl;
        unit_assert_operator_equal(0, system(command.c_str()));
        unit_assert(bfs::exists(outputFilepath));
        bfs::rename(outputFilepath, testDataPath / ("merged-empty-TMT2plex-" + groupColumn + ".tsv"));

        command = (format("%1%\"%2%\" %3% %4%,TMT6plex,PivotTMTBySource \"%5%\"%1%") % commandQuote % idpQueryPath % groupColumn % mainColumns % inputFilepath).str();
        cout << endl << command << endl;
        unit_assert_operator_equal(0, system(command.c_str()));
        unit_assert(bfs::exists(outputFilepath));
        bfs::rename(outputFilepath, testDataPath / ("merged-empty-TMT6plex-" + groupColumn + ".tsv"));

        command = (format("%1%\"%2%\" %3% %4%,TMT6plex,PivotTMTBySource \"%5%\"%1%") % commandQuote % idpQueryPath % groupColumn % mainColumns % inputFilepath).str();
        cout << endl << command << endl;
        unit_assert_operator_equal(0, system(command.c_str()));
        unit_assert(bfs::exists(outputFilepath));
        bfs::rename(outputFilepath, testDataPath / ("merged-empty-TMT6plex-" + groupColumn + ".tsv"));

        command = (format("%1%\"%2%\" %3% %4%,iTRAQ8plex,PivotITRAQBySource \"%5%\"%1%") % commandQuote % idpQueryPath % groupColumn % mainColumns % quantifiedInputFilepath).str();
        cout << endl << command << endl;
        unit_assert_operator_equal(0, system(command.c_str()));
        unit_assert(bfs::exists(quantifiedOutputFilepath));
        bfs::rename(quantifiedOutputFilepath, testDataPath / ("merged-quantified-iTRAQ8plex-" + groupColumn + ".tsv"));
    }
}


int main(int argc, char* argv[])
{
    TEST_PROLOG(argc, argv)

#ifdef WIN32
        string exeExtension = ".exe";
#else
        string exeExtension = "";
#endif

    try
    {
        vector<string> args(argv+1, argv + argc);

        size_t idpQonvertArg = findOneFilename("idpQonvert" + exeExtension, args);
        string idpQonvertPath = args[idpQonvertArg];
        args.erase(args.begin() + idpQonvertArg);

        size_t idpAssembleArg = findOneFilename("idpAssemble" + exeExtension, args);
        string idpAssemblePath = args[idpAssembleArg];
        args.erase(args.begin() + idpAssembleArg);

        size_t idpQueryArg = findOneFilename("idpQuery" + exeExtension, args);
        string idpQueryPath = args[idpQueryArg];
        args.erase(args.begin() + idpQueryArg);

        // the rest of the arguments should be directories
        BOOST_FOREACH(const string& arg, args)
        {
            if (!bfs::is_directory(arg))
                throw runtime_error("expected a path to test files, got \"" + arg + "\"");

            testIdpQonvert(idpQonvertPath, arg);
            testIdpAssemble(idpQonvertPath, idpAssemblePath, arg);
            testIdpQuery(idpQueryPath, arg);
        }
    }
    catch (exception& e)
    {
        TEST_FAILED(e.what())
    }
    catch (...)
    {
        TEST_FAILED("Caught unknown exception.")
    }

    TEST_EPILOG
}
