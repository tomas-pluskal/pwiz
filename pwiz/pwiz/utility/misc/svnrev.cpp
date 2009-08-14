//
// $Id:$
//
//
// Original author: Matt Chambers <matt.chambers .@. vanderbilt.edu>
//
// Copyright 2009 Vanderbilt University - Nashville, TN 37232
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


// This utility retrieves the highest number that follows the "$Id: $" keyword
// or a combination of the $Rev: $ and $Date: $ keywords. The Subversion
// version control system expands these keywords and keeps them up to date.
// For an example of the tag, see the end of this comment.
//
// This is a C++ tool inspired by the C version copyrighted by ITB CompuPhase.
// Their version of the tool is available at http://www.compuphase.com/svnrev.htm.
//


#include "Filesystem.hpp"
#include "Stream.hpp"
#include "DateTime.hpp"
#include "Exception.hpp"
#include <boost/foreach.hpp>
#include <boost/regex.hpp>
#include <boost/program_options.hpp>
#include <boost/filesystem/fstream.hpp>


using namespace pwiz::util;


struct Config
{
    Config() : incremental(false), verbose(false) {}

    bool incremental;
    bool verbose;
    string headerFilepath;
    vector<bfs::path> filepaths;
};


Config parseCommandLine(int argc, const char* argv[])
{
    namespace po = boost::program_options;

    ostringstream usage;
    usage << "Usage: svnrev [options] [filemasks]\n"
          << "This utility retrieves the highest number that follows the \"$Id: $\" keyword\n"
          << "or a combination of the $Rev$ and $Date$ keywords. The Subversion\n"
          << "version control system expands these keywords and keeps them up to date.\n"
          << "\n";
        
    Config config;

    po::options_description od_config("Options");
    od_config.add_options()
        ("incremental,i",
            po::value<bool>(&config.incremental)->zero_tokens(),
            ": take max. revision from updated files and existing header file")
        ("verbose,v",
            po::value<bool>(&config.verbose)->zero_tokens(),
            ": display detailed progress information")
        ("header,h",
            po::value<string>(&config.headerFilepath)->default_value("svnrev.hpp"),
            ": sets the filepath of the header to contain the revision info")
        ;

    // append options description to usage string

    usage << od_config;

    if (argc <= 1)
        throw runtime_error(usage.str().c_str());

    // handle positional arguments

    const char* label_args = "args";

    po::options_description od_args;
    od_args.add_options()(label_args, po::value< vector<string> >(), "");

    po::positional_options_description pod_args;
    pod_args.add(label_args, -1);
   
    po::options_description od_parse;
    od_parse.add(od_config).add(od_args);

    // parse command line

    po::variables_map vm;
    po::store(po::command_line_parser(argc, (char**)argv).
              options(od_parse).positional(pod_args).run(), vm);
    po::notify(vm);

    // remember filenames from command line

    if (vm.count(label_args))
    {
        vector<string> filemasks = vm[label_args].as< vector<string> >();

        // expand the filemasks by globbing to handle wildcards
        BOOST_FOREACH(const string& filemask, filemasks)
        {
            expand_pathmask(bfs::path(filemask), config.filepaths);
        }
    }

    if (config.filepaths.empty())
        throw runtime_error("[svnrev] No files specified.");

    return config;
}


struct Revision
{
    int number;
    boost::gregorian::date date;

    Revision() : number(0), date(bdt::not_a_date_time) {}
    bool operator< (const Revision& rhs) const {return number < rhs.number;}
    bool operator== (const Revision& rhs) const {return number == rhs.number;}
    bool operator!= (const Revision& rhs) const {return number != rhs.number;}
    bool empty() const {return number == 0 || date == boost::gregorian::date(bdt::not_a_date_time);}
};


std::ostream& operator<< (std::ostream& os, const Revision& r)
{
    return os << r.number << " " << r.date;
}


template <typename stream_type>
void imbueDateFacet(stream_type& stream)
{
    boost::gregorian::date_facet* output_facet = new boost::gregorian::date_facet();
    output_facet->set_iso_extended_format();
    stream.imbue(std::locale(std::locale::classic(), output_facet));
}

Revision getRevision(const bfs::path& filepath)
{
    // $Id: calc.c 148 2002-07-28 21:30:43Z sally $
    static const boost::regex idRegex(".*?\\$Id: \\S+ (\\d+) (\\d{4}-\\d{2}-\\d{2}) \\d{2}:\\d{2}:\\d{2}Z \\S+ \\$.*?");

    // $Date$
    // or
    // $Date: 2002-07-22
    static const boost::regex dateRegex(".*?\\$(?:Date|LastChangedDate): (\\d{4}-\\d{2}-\\d{2}) (?:\\d{2}:\\d{2}:\\d{2} \\S+ \\(.+\\) )?\\$.*?");

    // $Revision$
    static const boost::regex revisionRegex(".*?\\$(?:Revision|Rev|LastChangedRevision): (\\d+) \\$.*?");

    Revision r;

    bfs::ifstream filestream(filepath);

    string line;
    boost::smatch match_result;
    while (std::getline(filestream, line))
    {
        if (r.empty() && boost::regex_match(line, match_result, idRegex))
        {
            r.number = lexical_cast<int>(match_result[1]);
            r.date = bdt::parse_date<boost::gregorian::date>(match_result[2]);
        }
        else
        {
            if (r.date == boost::gregorian::date(bdt::not_a_date_time) &&
                boost::regex_match(line, match_result, dateRegex))
                r.date = bdt::parse_date<boost::gregorian::date>(match_result[1]);

            if (r.number == 0 && boost::regex_match(line, match_result, revisionRegex))
                r.number = lexical_cast<int>(match_result[1]);
        }

        if (!r.empty())
            break;
    }
    return r;
}


int go(const Config& config)
{
    // incremental runs should already have an existing svnrev.hpp and since only
    // the updated files are passed, we take the maximum of the svnrev.hpp build
    // and the updated files
    Revision maxRevision, existingHeaderRevision;
    time_t maxLastWriteTime = 0, existingHeaderLastWriteTime;
    if (config.incremental && bfs::exists(config.headerFilepath))
    {
        maxRevision = existingHeaderRevision = getRevision(config.headerFilepath);
        existingHeaderLastWriteTime = bfs::last_write_time(config.headerFilepath);
        if (config.verbose)
            cout << "Existing maximum revision: " << maxRevision << endl;
    }

    BOOST_FOREACH(const bfs::path& filepath, config.filepaths)
    {
        try
        {
            Revision fileRevision = getRevision(filepath);
            if (!fileRevision.empty())
            {
                if (config.verbose)
                    cout << filepath << ": " << fileRevision << endl;
                maxRevision = std::max(maxRevision, getRevision(filepath));
            }
            maxLastWriteTime = std::max(bfs::last_write_time(filepath), maxLastWriteTime);
        }
        catch (exception& e)
        {
            cerr << e.what() << endl;
            cerr << "Error processing file " << filepath << endl;
            // TODO: decide if errors should be fatal
        }
    }

    if (config.verbose)
        cout << "Current maximum revision: " << maxRevision << endl;

    // the header needs to be rewritten if the maximum revision is different than the existing revision;
    // it could be higher or lower: it could be lower if the working copy has changed from a later revision
    // to an earlier revision without deleting the existing header
    if (existingHeaderRevision != maxRevision)
    {
        cout << "[svnrev] Updating revision info in " << config.headerFilepath << "." << endl;

        bfs::ofstream header(config.headerFilepath);
        imbueDateFacet(header);

        header << "// This file was generated by the \"svnrev\" utility\n"
               << "// You should not modify it manually, as it may be re-generated.\n"
               << "//\n"
               // $ is split from the rest of the keyword so that SVN doesn't replace it
               << "// $" << "Revision: " << maxRevision.number << " $\n"
               << "// $" << "Date: " << maxRevision.date << " $\n"
               << "//\n"
               << endl
               << "#ifndef SVNREV_HPP\n"
               << "#define SVNREV_HPP\n\n"
               << "#define SVN_REV      " << maxRevision.number << "\n"
               << "#define SVN_REVDATE  \"" << maxRevision.date << "\"\n"
               << "\n#endif /* SVNREV_HPP */"
               << endl;
    }
    // the header must always be more recent than the most recent versioned filepath
    else if (existingHeaderLastWriteTime < maxLastWriteTime)
    {
        cout << "[svnrev] Updating last write time of " << config.headerFilepath << "." << endl;
        bfs::last_write_time(config.headerFilepath, maxLastWriteTime+1);
    }
    else if (config.verbose)
        cout << "[svnrev] " << config.headerFilepath << " is up to date." << endl;

    return 0;
}


int main(int argc, const char* argv[])
{
    try
    {
        Config config = parseCommandLine(argc, argv);
        imbueDateFacet(cout);
        return go(config);
    }
    catch (exception& e)
    {
        cerr << e.what() << endl;
    }
    catch (...)
    {
        cerr << "[svnrev] Caught unknown exception.\n";
    }

    return 1;
}