#ifndef _MYRIMATCHCONFIG_H
#define _MYRIMATCHCONFIG_H

#include "stdafx.h"
#include "freicore.h"
#include "BaseRunTimeConfig.h"
#include "pwiz/utility/math/erf.hpp"

using namespace freicore;

#define MYRIMATCH_RUNTIME_CONFIG \
	COMMON_RTCONFIG SPECTRUM_RTCONFIG SEQUENCE_RTCONFIG MULTITHREAD_RTCONFIG \
	RTCONFIG_VARIABLE( string,			OutputSuffix,				""				) \
    RTCONFIG_VARIABLE( string,          ProteinDatabase,            ""              ) \
    RTCONFIG_VARIABLE( string,          FragmentationRule,          "cid"           ) \
    RTCONFIG_VARIABLE( bool,            FragmentationAutoRule,      true            ) \
	RTCONFIG_VARIABLE( int,				MaxResults,					5				) \
	RTCONFIG_VARIABLE( int,				NumIntensityClasses,		3				) \
	RTCONFIG_VARIABLE( int,				NumMzFidelityClasses,		3				) \
	RTCONFIG_VARIABLE( int,				StartSpectraScanNum,		0				) \
	RTCONFIG_VARIABLE( int,				EndSpectraScanNum,			-1				) \
	RTCONFIG_VARIABLE( int,				NumBatches,					50				) \
	RTCONFIG_VARIABLE( double,			TicCutoffPercentage,		0.98			) \
	RTCONFIG_VARIABLE( double,			ClassSizeMultiplier,		2.0			    ) \
	RTCONFIG_VARIABLE( double,			MinResultScore,				0.0			    ) \
	RTCONFIG_VARIABLE( bool,			AdjustPrecursorMass,		false			) \
	RTCONFIG_VARIABLE( double,			MinPrecursorAdjustment,		-2.5			) \
	RTCONFIG_VARIABLE( double,			MaxPrecursorAdjustment,		2.5			    ) \
	RTCONFIG_VARIABLE( double,			PrecursorAdjustmentStep,	0.1			    ) \
	RTCONFIG_VARIABLE( int,				NumSearchBestAdjustments,	1				) \
	RTCONFIG_VARIABLE( double,			MinSequenceMass,			0.0				) \
	RTCONFIG_VARIABLE( double,			MaxSequenceMass,			10000.0f		) \
    RTCONFIG_VARIABLE( int,				MaxSequenceLength,	        75				) \
	RTCONFIG_VARIABLE( bool,			PreferIntenseComplements,	true			) \
	RTCONFIG_VARIABLE( int,				DeisotopingMode,			0				) \
	RTCONFIG_VARIABLE( int,				ProteinSamplingTime,		15				) \
	RTCONFIG_VARIABLE( bool,			EstimateSearchTimeOnly,		false			) \
	RTCONFIG_VARIABLE( int,				StartProteinIndex,			0				) \
	RTCONFIG_VARIABLE( int,				EndProteinIndex,			-1				) \
	RTCONFIG_VARIABLE( string,			CleavageRules,				"[|K|R . . ]"	) \
    RTCONFIG_VARIABLE( string,          DigestionRules,             "[KR]|"         ) \
	RTCONFIG_VARIABLE( int,				NumMinTerminiCleavages,		2				) \
	RTCONFIG_VARIABLE( int,				NumMaxMissedCleavages,		-1				) \
	RTCONFIG_VARIABLE( int,				MinCandidateLength,			5				) \
	RTCONFIG_VARIABLE( bool,			CalculateRelativeScores,	false			) \
	RTCONFIG_VARIABLE( bool,			MakeSpectrumGraphs,			false			) \
	RTCONFIG_VARIABLE( bool,			MakeScoreHistograms,		false			) \
	RTCONFIG_VARIABLE( int,				NumScoreHistogramBins,		100				) \
	RTCONFIG_VARIABLE( int,				MaxScoreHistogramValues,	100				) \
	RTCONFIG_VARIABLE( int,				ScoreHistogramWidth,		800				) \
	RTCONFIG_VARIABLE( int,				ScoreHistogramHeight,		600				) \
	RTCONFIG_VARIABLE( int,				MaxFragmentChargeState,		0				)
	//RTCONFIG_VARIABLE( int,				DeisotopingTestMode,		0				)


namespace freicore
{
namespace myrimatch
{
	struct RunTimeConfig : public BaseRunTimeConfig
	{
	public:
		RTCONFIG_DEFINE_MEMBERS( RunTimeConfig, MYRIMATCH_RUNTIME_CONFIG, "\r\n\t ", "myrimatch.cfg", "\r\n#" )

		CleavageRuleSet	_CleavageRules;
        vector<Digestion::Motif> digestionMotifs;
        Digestion::Config digestionConfig;

        FragmentTypesBitset defaultFragmentTypes;

        DynamicModSet   dynamicMods;
        StaticModSet    staticMods;

		int				SpectraBatchSize;
		int				ProteinBatchSize;
		int				ProteinIndexOffset;
		double			curMinSequenceMass;
		double			curMaxSequenceMass;
		int				minIntensityClassCount;
		int				minMzFidelityClassCount;
		int				maxFragmentChargeState;
		int             maxChargeStateFromSpectra;
		vector<double>	PrecursorMassTolerance;
		// Compute the fragment mass error bins and their associated log odds scores
		vector < double > massErrors;
		vector < double > mzFidelityLods;
		// Mass units
		MassUnits		precursorMzToleranceUnits;
		MassUnits		fragmentMzToleranceUnits;

	private:
		void finalize()
		{
			stringstream CleavageRulesStream( CleavageRules );
			_CleavageRules.clear();
			CleavageRulesStream >> _CleavageRules;

            vector<string> motifs;
            boost::split(motifs, DigestionRules, boost::is_space());
            digestionMotifs.clear();
            digestionMotifs.insert(digestionMotifs.end(), motifs.begin(), motifs.end());

            NumMaxMissedCleavages = NumMaxMissedCleavages < 0 ? 100000 : NumMaxMissedCleavages;

            vector<string> fragmentationRuleTokens;
            split( fragmentationRuleTokens, FragmentationRule, is_any_of(":") );
            if( fragmentationRuleTokens.empty() )
                throw runtime_error("invalid blank fragmentation rule");

            const string& mode = fragmentationRuleTokens[0];
            if( mode == "cid" )
            {
                defaultFragmentTypes[FragmentType_B] = true;
                defaultFragmentTypes[FragmentType_Y] = true;
            } else if( mode == "etd" )
            {
                defaultFragmentTypes[FragmentType_C] = true;
                defaultFragmentTypes[FragmentType_Z_Radical] = true;
            } else if( mode == "manual" )
            {
                if( fragmentationRuleTokens.size() != 2 )
                    throw runtime_error("manual fragmentation mode requires comma-separated list, e.g. 'manual:b,y'");

                vector<string> fragmentTypeTokens;
                split( fragmentTypeTokens, fragmentationRuleTokens[1], is_any_of(",") );
                
                if( fragmentTypeTokens.empty() )
                    throw runtime_error("no fragment types specified for manual fragmentation mode");

                for( size_t i=0; i < fragmentTypeTokens.size(); ++i )
                {
                    string fragmentType = to_lower_copy(fragmentTypeTokens[i]);
                    if( fragmentType == "a" )
                        defaultFragmentTypes[FragmentType_A] = true;
                    else if( fragmentType == "b" )
                        defaultFragmentTypes[FragmentType_B] = true;
                    else if( fragmentType == "c" )
                        defaultFragmentTypes[FragmentType_C] = true;
                    else if( fragmentType == "x" )
                        defaultFragmentTypes[FragmentType_X] = true;
                    else if( fragmentType == "y" )
                        defaultFragmentTypes[FragmentType_Y] = true;
                    else if( fragmentType == "z" )
                        defaultFragmentTypes[FragmentType_Z] = true;
                    else if( fragmentType == "z*" )
                        defaultFragmentTypes[FragmentType_Z_Radical] = true;
                }
            } else
                throw runtime_error("invalid fragmentation mode \"" + mode + "\"");

			if( ProteinSamplingTime == 0 )
            {
                EstimateSearchTimeOnly = 0;
                if( g_pid == 0 )
                    cerr << g_hostString << ": ProteinSamplingTime = 0 disables EstimateSearchTimeOnly" << endl;
            }

			ProteinIndexOffset = 0;

			string cwd;
			cwd.resize( MAX_PATH );
			getcwd( &cwd[0], MAX_PATH );
			WorkingDirectory = cwd.c_str();

			if( AdjustPrecursorMass )
			{
				UseAvgMassOfSequences = false;
			}

			if( TicCutoffPercentage > 1.0 )
			{
				TicCutoffPercentage /= 100.0;
				if( g_pid == 0 )
					cerr << g_hostString << ": TicCutoffPercentage > 1.0 (100%) corrected, now at: " << TicCutoffPercentage << endl;
			}


			if( !DynamicMods.empty() )
			{
				DynamicMods = TrimWhitespace( DynamicMods );
				dynamicMods = DynamicModSet( DynamicMods );
			}

			if( !StaticMods.empty() )
			{
				StaticMods = TrimWhitespace( StaticMods );
				staticMods = StaticModSet( StaticMods );
			}

			// Setting mass units to unknown.
			precursorMzToleranceUnits = UNKNOWN;
			fragmentMzToleranceUnits = UNKNOWN;
			// Convert the user input to lower case
			to_lower(PrecursorMzToleranceUnits);
			to_lower(FragmentMzToleranceUnits);
			// Set the units approriately
			if(PrecursorMzToleranceUnits.compare("daltons")==0) 
			{
				precursorMzToleranceUnits = DALTONS;
			} else if(PrecursorMzToleranceUnits.compare("ppm")==0) {
				precursorMzToleranceUnits = PPM;
			}
			if(FragmentMzToleranceUnits.compare("daltons")==0) 
			{
				fragmentMzToleranceUnits = DALTONS;
			} else if(FragmentMzToleranceUnits.compare("ppm")==0) {
				fragmentMzToleranceUnits = PPM;
			}
			// Make sure we know the mass units before we proceed with the search
			if(precursorMzToleranceUnits == UNKNOWN || fragmentMzToleranceUnits == UNKNOWN) 
			{
				cout << "Error: Precursor and Fragment mass units are either unknown. Please set them to either daltons or ppm" << endl;
				exit(1);
			}
			// Sanity checks
			if((PrecursorMzTolerance-(int)PrecursorMzTolerance) > 0.0 && precursorMzToleranceUnits == PPM) 
			{
				cout << "Warning: PrecusorMzTolerance is set to fractional PPM (" << PrecursorMzTolerance << ")" << endl;
			}
			if((FragmentMzTolerance-(int)FragmentMzTolerance) > 0.0 && fragmentMzToleranceUnits == PPM) 
			{
				cout << "Warning: FragmentMzTolerance is set to fractional PPM (" << FragmentMzTolerance << ")" << endl;
			}

			// Set the mass tolerances for different charge state precursors
			vector<double>& precursorMassTolerance = PrecursorMassTolerance;
			precursorMassTolerance.clear();
			for( int z=1; z <= NumChargeStates; ++z )
				precursorMassTolerance.push_back( PrecursorMzTolerance * z );

			if( ClassSizeMultiplier > 1 )
			{
				minIntensityClassCount = int( ( pow( ClassSizeMultiplier, NumIntensityClasses ) - 1 ) / ( ClassSizeMultiplier - 1 ) );
				minMzFidelityClassCount = int( ( pow( ClassSizeMultiplier, NumMzFidelityClasses ) - 1 ) / ( ClassSizeMultiplier - 1 ) );
			} else
			{
				minIntensityClassCount = NumIntensityClasses;
				minMzFidelityClassCount = NumMzFidelityClasses;
			}
			
			maxFragmentChargeState = ( MaxFragmentChargeState > 0 ? MaxFragmentChargeState+1 : NumChargeStates );
			
			vector<double> insideProbs;
			int numBins = 5;
			// Divide the fragment mass error into half and use it as standard deviation
			double stdev = FragmentMzTolerance*0.5;
			massErrors.clear();
			insideProbs.clear();
			mzFidelityLods.clear();
			// Divide the mass error distributions into 10 bins.
			for(int j = 1; j <= numBins; ++j) 
			{
				// Compute the mass error associated with each bin.
				double massError = FragmentMzTolerance*((double)j/(double)numBins);
				// Compute the cumulative distribution function of massError 
				// with mu=0 and sig=stdev
				double errX = (massError-0)/(stdev*sqrt(2));
				double cdf = 0.5 * (1.0+erf(errX));
				// Compute the gaussian inside probability
				double insideProb = 2.0*cdf-1.0;
				// Save the mass errors and inside probabilities
				massErrors.push_back(massError);
				insideProbs.push_back(insideProb);
			}
			// mzFidelity bin probablities are dependent on the number of bin. So,
			// compute the probabilities only once.
			// Compute the probability associated with each mass error bin
			double denom = insideProbs.back();
			for(int j = 0; j < numBins; ++j) 
			{
				double prob;
				if(j==0) {
					prob = insideProbs[j]/denom;
				} else {
					prob = (insideProbs[j]-insideProbs[j-1])/denom;
				}
				// Compute the log odds ratio of GaussianProb to Uniform probability and save it
				mzFidelityLods.push_back(log(prob*(double)numBins));
			}
			/*cout << "Error-Probs:" << endl;
			for(int j = 0; j < numBins; ++j) 
			{
				cout << massErrors[j] << ":" << mzFidelityLods[j] << " ";
			}
			cout << endl;*/
			//exit(1);

			
		}
	};

	extern RunTimeConfig* g_rtConfig;
}
}

#endif
