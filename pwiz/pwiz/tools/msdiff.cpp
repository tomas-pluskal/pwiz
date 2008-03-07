//
// msdiff.cpp
//
//
// Darren Kessner <Darren.Kessner@cshs.org>
//
// Copyright 2008 Spielberg Family Center for Applied Proteomics
//   Cedars-Sinai Medical Center, Los Angeles, California  90048
//   Unauthorized use or reproduction prohibited
//


#include "pwiz/msdata/MSDataFile.hpp"
#include "pwiz/msdata/Diff.hpp"
#include "boost/program_options.hpp"
#include <iostream>
#include <iterator>


using namespace std;
using namespace pwiz::msdata;


struct Config
{
    vector<string> filenames;
    DiffConfig diffConfig;
};


ostream& operator<<(ostream& os, const Config& config)
{
    os << "filenames: ";
    copy(config.filenames.begin(), config.filenames.end(), ostream_iterator<string>(os," "));
    os << endl
       << "precision: " << config.diffConfig.precision << endl
       << "ignore meta-data: " << boolalpha << config.diffConfig.ignoreMetadata << endl;
    return os;
}


Config parseCommandLine(int argc, const char* argv[])
{
    namespace po = boost::program_options;

    ostringstream usage;
    usage << "Usage: msdiff [options] filename1 filename2\n"
          << "Compare two mass spec data files.\n\n";

    Config config;

    po::options_description od_config("Options");
    od_config.add_options()
        ("precision,p",
            po::value<double>(&config.diffConfig.precision)
                ->default_value(config.diffConfig.precision),
            ": set floating point precision for comparing binary data")
        ("ignore,i",
            po::value<bool>(&config.diffConfig.ignoreMetadata)
                ->default_value(config.diffConfig.ignoreMetadata)
                ->zero_tokens(),
            ": ignore metadata (compare scan binary data and important scan metadata only)")
   	     ;

    // append options description to usage string

    usage << od_config;

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
        config.filenames = vm[label_args].as< vector<string> >();

    // check stuff

    usage << endl
          << "Spielberg Family Center for Applied Proteomics\n"
          << "Cedars-Sinai Medical Center, Los Angeles, California\n"
          << "http://sfcap.cshs.org\n";

    if (config.filenames.size() != 2)
        throw runtime_error(usage.str());

    return config;
}


int do_diff(const Config& config)
{
    MSDataFile msd1(config.filenames[0]); 
    MSDataFile msd2(config.filenames[1]);

    Diff<MSData> diff(msd1, msd2, config.diffConfig);
    if (diff) cout << diff; 
    return diff;
}


int main(int argc, const char* argv[])
{
    try
    {
        Config config = parseCommandLine(argc, argv);        
        return do_diff(config);
    }
    catch (exception& e)
    {
        cerr << e.what() << endl;
    }
    catch (...)
    {
        cerr << "[msdiff] Caught unknown exception.\n";
    }

    return 1;
}

