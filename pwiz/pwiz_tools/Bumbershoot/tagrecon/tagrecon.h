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
// The Original Code is the Bumbershoot core library.
//
// The Initial Developer of the Original Code is Matt Chambers.
//
// Copyright 2009 Vanderbilt University
//
// Contributor(s): Surendra Dasari
//

#ifndef _TAGRECON_H
#define _TAGRECON_H

#include "stdafx.h"
#include "freicore.h"
#include "tagreconSpectrum.h"
#include <boost/atomic.hpp>
#include <boost/cstdint.hpp>

#define TAGRECON_LICENSE			COMMON_LICENSE

//#define DEBUG 1

using namespace freicore;

namespace freicore
{
	#ifdef USE_MPI
        extern MPI_Status st;
        extern void* g_mpiBuffer;
    #endif

namespace tagrecon
{
	typedef struct spectrumInfo
	{
		vector< string > sequences;
		bool hasCorrectTag;
	} spectrumInfo_t;

	typedef flat_map< float, string >				                    modMap_t;

	typedef flat_multimap< float, SpectraList::iterator >				SpectraMassMap;
	typedef vector< SpectraMassMap >									SpectraMassMapList;

	/**
		Structure TagSetInfo stores the spectrum, tag sequence, n-terminal and c-terminal
		masses that sourround the tag.
	*/
	struct TagSetInfo
	{
		TagSetInfo( const SpectraList::iterator& itr, string tag, float nT, float cT ) { 
			sItr = itr;
			nTerminusMass = nT;
			cTerminusMass = cT;
			candidateTag = tag;
		}

		TagSetInfo(string tag, float nT, float cT) {
			candidateTag = tag;
			nTerminusMass = nT;
			cTerminusMass = cT;
		}

        template< class Archive >
		void serialize( Archive& ar, const unsigned int version )
		{
			ar & candidateTag & nTerminusMass & cTerminusMass & tagChargeState & sItr;
		}

		SpectraList::iterator sItr;
		float nTerminusMass;
		float cTerminusMass;
		string candidateTag;
        int tagChargeState;
	};

    struct TagMatchInfo
    {

        TagMatchInfo() {}

        TagMatchInfo(Spectrum* spec, float modMass, TermMassMatch nTerm, TermMassMatch cTerm)
        {
            spectrum = spec;
            modificationMass = modMass;
            nTermMatch = nTerm;
            cTermMatch = cTerm;
        }

        bool operator < (const TagMatchInfo& rhs) const
        {
            if(spectrum->id==rhs.spectrum->id)
                if(nTermMatch == rhs.nTermMatch)
                    if(cTermMatch == rhs.cTermMatch)
                        return modificationMass < rhs.modificationMass;
                    else
                        return cTermMatch < rhs.cTermMatch;
                else
                    return nTermMatch < rhs.nTermMatch;
            else
                return spectrum->id < rhs.spectrum->id;
        }

        Spectrum* spectrum;
        float modificationMass;
        TermMassMatch nTermMatch;
        TermMassMatch cTermMatch;
    };

	/**
		Class TagMapCompare sorts tag to spectrum map based on spectral similarity.
		Two spectrum are said to be similar if the tag sequences match and also 
		the total mass deviation between n-terminal and c-terminal masses that
		sourround the tags is <= +/-maxDeviation. This comparator essentially 
		sorts similar tags together. 
	*/
	class TagSetCompare {

		// Maximum deviation observed between the terminal masses
		// the sourround a tag match.
		float maxDeviation;

	public:
		TagSetCompare(float maxDeviation = 300.0f) : maxDeviation(maxDeviation) {};

		/**
			operator () sorts the tags based on tag sequence first. If two tag sequences
			match then we cluster them based on their total terminal mass deviation from each
			other. Spectra with tags that have total terminal deviations <= +/-maxDeviation
			are kept together. If two tags don't satisfy this criterion then they are
			sorted based on their n-terminal masses.
		*/
		bool operator ()(const TagSetInfo& lhs, const TagSetInfo& rhs) const {
			if(lhs.candidateTag < rhs.candidateTag) {
				return lhs.candidateTag < rhs.candidateTag;
			} else if(lhs.candidateTag > rhs.candidateTag) {
				return lhs.candidateTag < rhs.candidateTag;
			} else {
				float nTerminalAbsMassDiff = fabs(lhs.nTerminusMass-rhs.nTerminusMass);
				float cTerminalAbsMassDiff =  fabs(lhs.cTerminusMass-rhs.cTerminusMass);
				//if((nTerminalAbsMassDiff+cTerminalAbsMassDiff) > maxDeviation) {
                if(nTerminalAbsMassDiff > maxDeviation && cTerminalAbsMassDiff > maxDeviation) {
					return lhs.nTerminusMass < rhs.nTerminusMass;
				} else {
					return false;
				}
			}
		}
        
	};

	// A spectra to tag map (tag to spectrum) that sorts tags based on spectral similarity
	typedef flat_multiset< TagSetInfo, TagSetCompare >					SpectraTagMap;
    typedef multiset< TagSetInfo, TagSetCompare>                        TempSpectraTagMap;
	//typedef multimap<pair <string, float>, TagMapInfo>				SpectraTagMap;
	typedef vector< SpectraTagMap >										SpectraTagMapList;

	struct SearchStatistics
	{
        SearchStatistics()
        :   numProteinsDigested(0),
            numCandidatesGenerated(0),
            numCandidatesQueried(0),
            numComparisonsDone(0),
            numCandidatesSkipped(0)
        {}

        SearchStatistics(const SearchStatistics& other)
        {
            operator=(other);
        }

		SearchStatistics& operator=(const SearchStatistics& other)
		{
            numProteinsDigested.store(other.numProteinsDigested);
            numCandidatesGenerated.store(other.numCandidatesGenerated);
            numCandidatesQueried.store(other.numCandidatesQueried);
            numComparisonsDone.store(other.numComparisonsDone);
            numCandidatesSkipped.store(other.numCandidatesSkipped);
            return *this;
        }

        boost::atomic_uint32_t numProteinsDigested;
		boost::atomic_uint64_t numCandidatesGenerated;
		boost::atomic_uint64_t numCandidatesQueried;
		boost::atomic_uint64_t numComparisonsDone;
        boost::atomic_uint64_t numCandidatesSkipped;

		template< class Archive >
		void serialize( Archive& ar, const unsigned int version )
		{
			ar & numProteinsDigested & numCandidatesGenerated & numCandidatesQueried & numComparisonsDone & numCandidatesSkipped;
		}

		SearchStatistics operator+ ( const SearchStatistics& rhs )
		{
			SearchStatistics tmp(*this);
			tmp.numProteinsDigested.fetch_add(rhs.numProteinsDigested);
			tmp.numCandidatesGenerated.fetch_add(rhs.numCandidatesGenerated);
			tmp.numCandidatesQueried.fetch_add(rhs.numCandidatesQueried);
			tmp.numComparisonsDone.fetch_add(rhs.numComparisonsDone);
            tmp.numCandidatesSkipped.fetch_add(rhs.numCandidatesSkipped);
			return tmp;
		}

		operator string()
		{
			stringstream s;
			s	<< numProteinsDigested << " proteins; " << numCandidatesGenerated << " candidates; "
				<< numCandidatesQueried << " queries; " << numComparisonsDone << " comparisons";
            if(numCandidatesSkipped>0) {
                s << "; " << numCandidatesSkipped << " skipped";
            }
			return s.str();
		}
	};

    #ifdef USE_MPI
		void TransmitConfigsToChildProcesses();
		void ReceiveConfigsFromRootProcess();
        void ReceiveNETRewardsFromRootProcess();
        void TransmitNETRewardsToChildProcess();
		int ReceivePreparedSpectraFromChildProcesses();
		int TransmitPreparedSpectraToRootProcess( SpectraList& preparedSpectra );
		int ReceiveUnpreparedSpectraBatchFromRootProcess();
		int TransmitUnpreparedSpectraToChildProcesses();
		int ReceiveSpectraFromRootProcess();
		int TransmitSpectraToChildProcesses( int done );
		int TransmitProteinsToChildProcesses();
		int ReceiveProteinBatchFromRootProcess();
		int TransmitResultsToRootProcess();
		int ReceiveResultsFromChildProcesses( bool firstBatch );
	#endif

	extern proteinStore         proteins;
    extern SearchStatistics     searchStatistics;

	extern SpectraList			spectra;
	extern SpectraTagMap		spectraTagMapsByChargeState;
}
}

#endif
