//
/// msdigest.cpp
///

#include "pwiz/utility/proteome/IPIFASTADatabase.hpp"
#include "pwiz/utility/proteome/Digestion.hpp"
#include "boost/program_options.hpp"
#include <iostream>
#include <fstream>
#include <string>

using namespace std;
using namespace pwiz;
using namespace pwiz::proteome;

struct Config
{
    ProteolyticEnzyme proteolyticEnzyme;
    Digestion::Config digestionConfig;
    vector<string> filenames;
    size_t precision;

    Config() : proteolyticEnzyme(ProteolyticEnzyme_Trypsin), digestionConfig(0,0,100000), precision(12) {}

};

ProteolyticEnzyme translateProteolyticEnzyme(const string& s)
{    
    if (s.compare("trypsin") == 0) return ProteolyticEnzyme_Trypsin;
    else if (s.compare("chymotrypsin") == 0) return ProteolyticEnzyme_Chymotrypsin;
    else if (s.compare("chymotrypsin") == 0) return ProteolyticEnzyme_Chymotrypsin;
    else if (s.compare("clostripain") == 0) return ProteolyticEnzyme_Clostripain;
    else if (s.compare("cyanogenBromide") == 0) return ProteolyticEnzyme_CyanogenBromide;
    else if (s.compare("pepsin") == 0) return ProteolyticEnzyme_Pepsin;
    else throw runtime_error(("[msdigest] Unsupported proteolyticEnzyme: " + s).c_str());

}

Digestion::Specificity translateSpecificity(const string& s)
{
    if (s.compare("none") == 0 ) return Digestion::NonSpecific;
    else if (s.compare("semi") == 0 ) return Digestion::SemiSpecific;
    else if (s.compare("fully") == 0 ) return Digestion::FullySpecific;
    else throw runtime_error(("[msdigest] Unsupported specificity: " + s).c_str());

}

Config parseCommandLine(int argc, const char* argv[])
{
    namespace po = boost::program_options;
    Config config;

    ostringstream usage;
    usage << "Usage: msdigest [options] [filenames] \n"
          << endl;

    // local variables for translation
    string tempEnzyme;
    string tempSpecificity;

    // define command line options                                                                                           
    po::options_description od_config("Options");
    od_config.add_options()
        ("proteolyticEnzyme,e", po::value<string>(&tempEnzyme), " : specify proteolytic enzyme for digestion. Options: trypsin, chromotrypsin, clostripain, cyanogenBromide, pepsin. \nDefault : trypsin")
        ("numMissedCleavages,n", po::value<int>(&config.digestionConfig.maximumMissedCleavages)->default_value(config.digestionConfig.maximumMissedCleavages), " : specify number of missed cleavages to allow.")
        ("specificity,s", po::value<string>(&tempSpecificity)," : specify minimum specificity. Options: non, semi, fully. \nDefault: fully")
        ("minLength,m",po::value<int>(&config.digestionConfig.minimumLength)->default_value(config.digestionConfig.minimumLength), " : specify minimum length of digested peptides")
        ("maxLength,M",po::value<int>(&config.digestionConfig.maximumLength)->default_value(config.digestionConfig.maximumLength), " : specify maximum length of digested peptides")
        ("massPrecison,p", po::value<size_t>(&config.precision)->default_value(config.precision)," : specify precision of calculated mass of digested peptides");
    
    
    // append options to usage string                                                                                        
    usage << od_config;

    // handle positional args                                                                                                
    const char* label_args = "args";

    po::options_description od_args;
    od_args.add_options()(label_args, po::value< vector<string> >(), "");

    po::positional_options_description pod_args;
    pod_args.add(label_args, -1);

    po::options_description od_parse;
    od_parse.add(od_config).add(od_args);

    // parse command line                                                                                                    
    po::variables_map vm;
    po::store(po::command_line_parser(argc, (char**)argv).options(od_parse).positional(pod_args).run(), vm);
    po::notify(vm);

    // get filenames                                                                                                         
    if (vm.count(label_args))
        config.filenames = vm[label_args].as< vector<string> >();

    // usage if incorrect                                                                                                    
    if (config.filenames.empty())
        throw runtime_error(usage.str());

    // assign local variables to config
    if (tempEnzyme.size() > 0) config.proteolyticEnzyme = translateProteolyticEnzyme(tempEnzyme);
    if (tempSpecificity.size() > 0) config.digestionConfig.minimumSpecificity = translateSpecificity(tempSpecificity);

    return config;

}

void go(const Config& config)
{
    vector<string>::const_iterator file_it = config.filenames.begin();
    for( ; file_it != config.filenames.end(); ++file_it)
        {
            ofstream ofs((*file_it + "_digestedPeptides.txt").c_str());
            ofs << "sequence" 
                << "\t" << "protein" 
                << "\t" << "mass" 
                << "\t" << "missedCleavages" 
                << "\t" << "specificity"
                << "\t" << "nTerminusIsSpecific"
                << "\t" << "cTerminusIsSpecific"
                << "\n";

            ofs.precision(config.precision);

            IPIFASTADatabase db(*config.filenames.begin());
            IPIFASTADatabase::const_iterator it = db.begin();

            for(; it != db.end(); ++it)
                {
                    // digest
                    Peptide peptide(it->sequence);
                    Digestion digestion(peptide, config.proteolyticEnzyme, config.digestionConfig);        

                    // iterate through digested peptides and output
                    vector<DigestedPeptide> digestedPeptides(digestion.begin(), digestion.end());
                    vector<DigestedPeptide>::iterator jt = digestedPeptides.begin();
                    for(; jt!= digestedPeptides.end(); ++jt)                
                        ofs << jt->sequence() 
                            << "\t" << it->faID 
                            << "\t" << jt->monoisotopicMass(0, false) /* unmodified neutral mass + h2o*/ 
                            << "\t" << jt->missedCleavages() 
                            << "\t" << jt->specificTermini() 
                            << "\t" << jt->NTerminusIsSpecific() 
                            << "\t" << jt->CTerminusIsSpecific() 
                            << "\n";
                
                }

        }
}

int main(int argc, const char* argv[])
{
    
    try
        {
            Config config = parseCommandLine(argc, argv);
            go(config);

            return 0;
        }

    catch (exception& e)
        {
            cout << e.what() << endl;
        }

    
    return 0;

}
