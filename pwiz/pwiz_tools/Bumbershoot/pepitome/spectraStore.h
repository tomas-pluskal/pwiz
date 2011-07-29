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
// The Original Code is the Pepitome search engine.
//
// The Initial Developer of the Original Code is Surendra Dasari.
//
// Copyright 2011 Vanderbilt University
//
// Contributor(s):
//

#ifndef _SPECTRASTORE_H
#define _SPECTRASTORE_H

#ifdef _MSC_VER // Compiling with MSVC compiler
#define file_seek _fseeki64_nolock
#define file_tell _ftelli64_nolock
#elif __MINGW || defined(__MINGW32__)
#define file_seek fseeko64
#define file_tell ftello64
#else // assume POSIX
#define file_seek fseeko
#define file_tell ftello
#endif

#include "stdafx.h"
#include "freicore.h"
#include "PeakSpectrum.h"
#include "shared_types.h"
#include "portable_iarchive.hpp"
#include "portable_oarchive.hpp"

#include "sqlite/sqlite3pp.h"

#include <boost/tokenizer.hpp>
#include <boost/assign.hpp>
#include <boost/algorithm/string.hpp>
#include <boost/algorithm/string/find.hpp>
#include <boost/algorithm/string/trim.hpp>
#include <boost/algorithm/string/split.hpp>
#include <boost/algorithm/string/predicate.hpp>

#include <boost/iostreams/copy.hpp>
#include <boost/iostreams/compose.hpp>
#include <boost/iostreams/filter/zlib.hpp> 
#include <boost/iostreams/filtering_stream.hpp>
#include <boost/iostreams/filtering_streambuf.hpp> 
#include <boost/iostreams/detail/config/zlib.hpp> 
#include <boost/iostreams/filter/base64.hpp>

#include <boost/lexical_cast.hpp>

#include <iostream>
#include <ctype.h>
#include <fstream>

using namespace boost::assign;
using namespace boost::algorithm;

namespace sqlite = sqlite3pp;

namespace freicore
{

namespace pepitome
{

    static const boost::char_separator<char> delim(" =\r\n");
    static const boost::char_separator<char> modsDelim("@");
    static const boost::char_separator<char> peakDelim("\t ");
    static const boost::char_separator<char> colon(":");
    static const boost::char_separator<char> backslash("/");
    static const boost::char_separator<char> dot(".");
    static const boost::char_separator<char> comma(",");
    static const boost::char_separator<char> equals("=");


    typedef boost::tokenizer<boost::char_separator<char> > tokenizer;
    typedef multimap<double,float> Peaks;
    typedef multimap<int,double> ModMap;
    typedef multimap<string, unsigned int> ProteinMap;

    typedef BasePeakData< PeakInfo > PeakData;
    typedef map<float, string>       PeakAnnotations;
    typedef boost::uint64_t stream_offset;
    typedef int file_handle;

    // Modification values taken from http://chemdata.nist.gov/mass-spc/ftp/mass-spc/PepLib.pdf    
    static map<string,double> ModNamesToMasses = map_list_of ("Oxidation", 15.994915) ("Carbamidomethyl", 57.02146) \
        ("ICAT_light", 227.12) ("ICAT_heavy", 236.12) \
        ("AB_old_ICATd0", 442.20) ("AB_old_ICATd8", 450.20) \
        ("Acetyl", 42.0106) ("Deamidation", 0.9840) ("Pyro-cmC", 39.994915) \
        ("Pyro-glu", -17.026549) ("Pyro_glu", -18.010565) ("Amide", -0.984016) \
        ("Phospho", 79.9663) ("Methyl", 14.0157) ("Carbamyl", 43.00581) \
        ("Gln->pyro-Glu", -17.0265) ("Glu->pyro-Glu", -18.0106) ("Carboxymethyl", 58.005479) \
        ("Deamidated", 0.984016);

    // This is used to random access the library file. 
    struct NativeFileReader
    {
        FILE * handle;
        string filename;
        bool isOpen;
        
        NativeFileReader(string name)
        {
           handle = fopen(name.c_str(), "rb");
            isOpen = (handle == NULL) ? false : true;
            if(!isOpen)
                throw runtime_error("[NativeFileReader] Failed to open file " + name);
        }

        ~NativeFileReader()
        {
            if(isOpen)
               fclose(handle);
            isOpen = false;
        }
        
        int getline(string& out)
        {
            stringstream s;
            char currChar;
            while(true)
            {
                currChar = fgetc(handle);
                if(currChar == EOF)
                {
                    if(ferror(handle))
                        throw runtime_error("[NativeFileReader] Failed while reading a file.");
                    else
                        break;
                } else if(currChar == '\n')
                    break;
                s << currChar;
            }
            out = s.str();
            if(s.str().size()==0)
                return 0;
            return 1;
        }

        void seek(stream_offset pos)
        {
            file_seek(handle, pos, SEEK_SET);
        }
    };

    struct UnpackingTimers
    {
        Profiler           basicTypes;
        Profiler           peakData;
        Profiler           peakAnns;
        Profiler           peptide;
        Profiler           proteins;
        Profiler           totalTimer;
        Profiler           decompTimer;
        boost::mutex    timerMutex;
        
        UnpackingTimers()
        {
            basicTypes.Begin(); basicTypes.End();
            peakData.Begin(); peakData.End();
            peakAnns.Begin(); peakAnns.End();
            peptide.Begin(); peptide.End();
            proteins.Begin(); proteins.End();
            totalTimer.Begin(); totalTimer.End();
            decompTimer.Begin(); decompTimer.End();
        }

        void printTimers()
        {
            cout << "basicTypes:" << basicTypes.End() << endl;
            cout << "peakData:" << peakData.End() << endl;
            cout << "peakAnns:" << peakAnns.End() << endl;
            cout << "peptide:" << peptide.End() << endl;
            cout << "proteins:" << proteins.End() << endl;
            cout << "loadFuncTimer:" << totalTimer.End() << endl;
            cout << "decompTimer:" << decompTimer.End() << endl;
        }
    };

    UnpackingTimers unpackingTimers;

    struct BaseLibrarySpectrum
    {
        // Peptide seqeunce
        shared_ptr<DigestedPeptide> matchedPeptide;
        ProteinMap matchedProteins;

        // Spectrum info
        SpectrumId id;
        PeakPreData peakPreData;
        PeakData    peakData;
        PeakAnnotations peakAnns;
        int peakPreCount;
        // Neutral mass
        double libraryMass;
        double monoisotopicMass;
        double averageMass;
        // Data indices
        stream_offset peakDataOffset;
        stream_offset headerOffset;
        // Peptide data
        size_t numMissedCleavages;
        int NTT;

        BaseLibrarySpectrum() : peakPreCount(0), libraryMass(0.0), peakDataOffset(0), headerOffset(0), numMissedCleavages(0), NTT(-1) {}
        BaseLibrarySpectrum( const BaseLibrarySpectrum& old )
        {
            matchedPeptide = old.matchedPeptide;
            matchedProteins = old.matchedProteins;
            id = old.id;
            peakPreData = old.peakPreData;
            peakData = old.peakData;
            peakAnns = old.peakAnns;
            peakPreCount = old.peakPreCount;
            libraryMass = old.libraryMass;
            peakDataOffset = old.peakDataOffset;
            headerOffset = old.headerOffset;
            numMissedCleavages = old.numMissedCleavages;
            NTT = old.NTT;
        }

        virtual ~BaseLibrarySpectrum() 
        {
            matchedPeptide.reset(); 
            matchedProteins.clear(); 
            peakPreData.clear();
            peakData.clear();
            peakAnns.clear();
        }

        virtual void readHeader(NativeFileReader& library) {}

        virtual void readSpectrum()
        {
            //cout << "querying " << id.source << "," << id.index << endl;
            sqlite3* db;
            sqlite3_open_v2(id.source.c_str(), &db, SQLITE_OPEN_NOMUTEX|SQLITE_OPEN_READONLY, NULL);
            sqlite::database library(db);
            string queryStr = "SELECT NumPeaks, SpectrumData FROM LibSpectrumData WHERE Id = '" + (string) id.nativeID + "'";
            sqlite::query qry(library, queryStr.c_str() );
            int numPeaks;
            string data;
            for (sqlite::query::iterator qItr = qry.begin(); qItr != qry.end(); ++qItr) 
            {
                (*qItr).getter() >> numPeaks >> data;
                stringstream encoded(data);
                stringstream decoded;
                bio::copy(bio::compose(bio::zlib_decompressor(), bio::compose(bio::base64_decoder(), encoded)), decoded);
                text_iarchive packArchive( decoded );
                packArchive & *this;
            }
        }

        virtual void readPeaks(NativeFileReader& library) {}

        // Indexing functions
        virtual void readSpectrumForIndexing() {}

        template<class Archive>
        void save(Archive& ar, const unsigned int version) const
        {
            ar << NTT;
            ar << numMissedCleavages;
            ar << matchedProteins;
            ar << peakPreData;
            ar << peakAnns;
            ar << (*matchedPeptide);
        }

        template <class Archive>
        void load(Archive& ar, const unsigned int version)
        {
            //unpackingTimers.basicTypes.Begin(false); 
            ar >> NTT;
            ar >> numMissedCleavages;
            //unpackingTimers.basicTypes.End();
            //unpackingTimers.proteins.Begin(false);
            ar >> matchedProteins;
            //unpackingTimers.proteins.End();
            //unpackingTimers.peakData.Begin(false);
            ar >> peakPreData;
            //unpackingTimers.peakData.End();
            //unpackingTimers.peakAnns.Begin(false);
            ar >> peakAnns;
            //unpackingTimers.peakAnns.End();
            //unpackingTimers.peptide.Begin(false);
            // Nasty hack to accomplish non-intrusive serialize of 
            // objects containing no default constructors.
            DigestedPeptide tmp("A");
            ar >> tmp;
            matchedPeptide.reset(new DigestedPeptide(tmp));
            //unpackingTimers.peptide.End();
        }
        BOOST_SERIALIZATION_SPLIT_MEMBER()

        virtual void clearSpectrum()
        {
            matchedPeptide.reset();
            matchedProteins.clear();
            peakPreData.clear();
            peakData.clear();
            peakAnns.clear();
        }

        virtual void clearHeader()
        {
            matchedPeptide.reset();
            matchedProteins.clear();
        }

        void clearPeaks()
        {
            peakPreData.clear();
            peakData.clear();
            peakAnns.clear();
        }

        // Filters out the peaks with the lowest intensities until only <ticCutoffPercentage> of the total ion current remains
        void FilterByTIC( double ticCutoffPercentage )
        {

            //cout << "TicCutoffPercentage:" << ticCutoffPercentage << endl;
            //exit(1);
            if( !peakPreData.empty() )
            {
                // Sort peak list in descending order of intensity while calculating the total ion current in the spectrum.
                // Use a multimap because multiple peaks can have the same intensity.
                float totalIonCurrent = 0.0f;
                typedef multimap< double, double > IntenSortedPeakPreData;
                IntenSortedPeakPreData intenSortedPeakPreData;
                for( PeakPreData::iterator itr = peakPreData.begin(); itr != peakPreData.end(); ++itr )
                {
                    totalIonCurrent += itr->second;
                    IntenSortedPeakPreData::iterator iItr = intenSortedPeakPreData.insert( make_pair( itr->second, itr->second ) );
                    iItr->second = itr->first;
                }

                double relativeIntensity = 0.0f;
                IntenSortedPeakPreData::reverse_iterator r_iItr;
                for(	r_iItr = intenSortedPeakPreData.rbegin();
                    relativeIntensity < ticCutoffPercentage && r_iItr != intenSortedPeakPreData.rend();
                    ++r_iItr )
                {
                    //cout << relativeIntensity << " / " << totalIonCurrent << endl;
                    relativeIntensity += r_iItr->first / totalIonCurrent; // add current peak's relative intensity to the sum
                }

                if( r_iItr == intenSortedPeakPreData.rend() )
                    --r_iItr;

                peakPreData.clear();

                for(	IntenSortedPeakPreData::iterator iItr = intenSortedPeakPreData.lower_bound( r_iItr->first );
                    iItr != intenSortedPeakPreData.end();
                    ++iItr )
                {
                    PeakPreData::iterator itr = peakPreData.insert( make_pair( iItr->second, iItr->second ) ).first;
                    itr->second = iItr->first;
                }
            }
        }

        void FilterByPeakCount( size_t maxPeakCount )
        {
            if( !peakPreData.empty() )
            {
                // Sort peak list in descending order of intensity
                // Use a multimap because multiple peaks can have the same intensity.
                typedef multimap< double, double > IntenSortedPeakPreData;
                IntenSortedPeakPreData intenSortedPeakPreData;
                for( PeakPreData::iterator itr = peakPreData.begin(); itr != peakPreData.end(); ++itr )
                {
                    IntenSortedPeakPreData::iterator iItr = intenSortedPeakPreData.insert( make_pair( itr->second, itr->second ) );
                    iItr->second = itr->first;
                }

                peakPreData.clear();

                size_t peakCount = 0;
                for(	IntenSortedPeakPreData::reverse_iterator r_iItr = intenSortedPeakPreData.rbegin();
                    r_iItr != intenSortedPeakPreData.rend() && peakCount < maxPeakCount;
                    ++r_iItr, ++peakCount )
                {
                    PeakPreData::iterator itr = peakPreData.insert( PeakPreData::value_type( r_iItr->second, r_iItr->second ) ).first;
                    itr->second = r_iItr->first;
                }
            }
        }

        void preprocessSpectrum(float TICCutoff = 1.0f, size_t maxPeakCount = 150, bool cleanSpectrum = false)
        {
            if(cleanSpectrum && peakAnns.size()>0)
            {
                typedef pair<float,string> PeakAnnotation;
                BOOST_FOREACH(const PeakAnnotation& p, peakAnns)
                {
                    if(icontains(p.second, "i") || icontains(p.second, "?"))
                    {
                        PeakPreData::iterator begin = peakPreData.lower_bound(p.first);
                        PeakPreData::iterator end = peakPreData.upper_bound(p.first);
                        while(begin != end)
                            peakPreData.erase(begin++);
                    }
                }
            }

            double parentIonEraseWindow = 3.0;
            if( !peakPreData.empty() )
            {
                double maxPeakMass = libraryMass + PROTON + parentIonEraseWindow;
                PeakPreData::iterator itr = peakPreData.upper_bound( maxPeakMass );
                peakPreData.erase( itr, peakPreData.end() );
            }

            BOOST_FOREACH(const double& parentIon, getPrecursorIons())
            {
                PeakPreData::iterator begin = peakPreData.lower_bound(parentIon - parentIonEraseWindow);
                PeakPreData::iterator end = peakPreData.upper_bound(parentIon + parentIonEraseWindow);
                while(begin != end)
                    peakPreData.erase(begin++);
            }    

            FilterByTIC(TICCutoff);
            FilterByPeakCount(maxPeakCount);

            // Sort the peaks by intensity
            double TIC = 0.0;
            typedef multimap< double, double > IntenSortedPeakPreData;
            IntenSortedPeakPreData intenSortedPeakPreData;
            for( PeakPreData::iterator itr = peakPreData.begin(); itr != peakPreData.end(); ++itr )
            {
                IntenSortedPeakPreData::iterator iItr = intenSortedPeakPreData.insert( make_pair( itr->second, itr->second ) );
                iItr->second = itr->first;
                TIC += itr->second;
            }

            peakPreData.clear();
            peakData.clear();
            
            IntenSortedPeakPreData::reverse_iterator iItr = intenSortedPeakPreData.rbegin();
            double prevPeakInten = iItr->first;
            int prevPeakRank = 1;
            for( ; iItr != intenSortedPeakPreData.rend(); ++iItr )
            {
                double mz = iItr->second;
                double inten = iItr->first;
                peakData[ mz ].rawIntensity = inten;
                peakData[ mz ].normIntensity = inten/TIC;
                
                if(inten != prevPeakInten)
                {
                    ++prevPeakRank; 
                    prevPeakInten = inten;
                }
                peakData[ mz ].intensityRank = prevPeakRank;
            }
            peakAnns.clear();
        }

        set<double> getPrecursorIons()
        {
            set<double> precursorIons;
            double precursorMZ = (libraryMass+id.charge*PROTON)/(double) id.charge;
            // Water, double water, and ammonia loss
            precursorIons.insert(precursorMZ - WATER_MONO/id.charge);
            precursorIons.insert(precursorMZ - 2.0*WATER_MONO/id.charge);
            precursorIons.insert(precursorMZ - AMMONIA_MONO/id.charge);
            precursorIons.insert(precursorMZ);
            return precursorIons;
        }
    };

    struct SpectraSTSpectrum : public virtual BaseLibrarySpectrum {

        size_t numPeaks;

        SpectraSTSpectrum() : BaseLibrarySpectrum() { numPeaks = 0;}

        SpectraSTSpectrum( const SpectraSTSpectrum& old ) : BaseLibrarySpectrum( old ) { numPeaks = old.numPeaks; }

        ~SpectraSTSpectrum()
        {
            BaseLibrarySpectrum::clearSpectrum();
        }

        void readPeaks(NativeFileReader& library)
        {
            library.seek(peakDataOffset);

            peakPreData.clear();
            peakAnns.clear();

            string buffer;
            while(library.getline(buffer))
            {
                bal::trim_right_if(buffer, bal::is_any_of(" \r"));
                if(buffer.empty()) // Break for empty line
                    break;
                
                if(!buffer.empty() && !isdigit(buffer[0]))
                    throw runtime_error("[SpectrumStore::readPeaks] Invalid index offset");

                tokenizer parser(buffer, peakDelim);
                tokenizer::iterator itr = parser.begin();
                string attribute = *(itr);
                string value = *(++itr);
                string peakAnn;
                //Skip the peak annotations if they exist
                if(++itr != parser.end())
                    peakAnn = *itr;
                if(isdigit(attribute[0]))
                {
                    float peakMass = lexical_cast<float>(attribute);
                    float intensity = lexical_cast<float>(value);
                    peakPreData.insert(pair<float,float>(peakMass,intensity));
                    peakAnns.insert(pair<float,string>(peakMass,peakAnn));
                }
            }
        }
        
        void clearHeader()
        {
            BaseLibrarySpectrum::clearHeader();
        }

        void clearSpectrum()
        {
            BaseLibrarySpectrum::clearSpectrum();
        }

        void readHeader(NativeFileReader& library)
        {
            //cout << headerOffset << "," << peakDataOffset << endl;
            library.seek(headerOffset);
            string buffer;
            matchedProteins.clear();
            ModMap mods;
            // First parse out the peptide FullName: -.n[43]AASC[160]VLLHTGQK.M/2
            library.getline(buffer);
            if(!boost::starts_with(buffer,"FullName:"))
                throw runtime_error("[SpectraStore::readHeader]: Invalid header offset");
            
            bal::trim_right_if(buffer, bal::is_any_of(" \r"));
            string::size_type pepStart = buffer.find(".");
            string::size_type pepEnd = buffer.find(".",pepStart+1);
            string peptide = buffer.substr(pepStart+1, pepEnd-pepStart-1);
            string prevAA = "-";
            string nextAA = "-";
            if(pepStart > 0)
                prevAA = buffer[pepStart-1];
            if(pepEnd < buffer.length())
                nextAA = buffer[pepEnd+1];

            if(peptide[0]=='n')
                peptide.erase(0,1);

            while(peptide.find("[") != string::npos)
            {
                string::size_type startPos = peptide.find("[");
                string::size_type endPos = peptide.find("]");
                peptide.erase(startPos, endPos-startPos+1);
            }

            //cout << peptide << endl;
            if(peptide.length() == 0)
                throw runtime_error("[SpectraStore::readHeader]: Failed to parse peptide the spectrum");

            // Get the "Comment:" line. See below for an example string (all in one line). Yuck!
            /* Comment: Spec=Consensus Pep=Tryptic/miss_bad_unconfirmed Fullname=-.AAAAAAGAGPEM(O)VRGQVFDVGPR.Y/3 \
            Mods=2/0,A,Acetyl/11,M,Oxidation Parent=752.712 Inst=qtof Mz_diff=0.002 Mz_exact=752.7117
            Mz_av=753.182 Protein="tr|Q1HBJ4|Q1HBJ4_HUMAN Mitogen-activated protein kinase 1 
            [Homo sapiens]" Pseq=38 Organism="human" Se=3^X2:ex=0.0001057/0.0001043,td=0/0,sd=0/0,
            hs=54.2/3.6,bs=1.4e-006,b2=0.00021^O2:ex=2.67192e-008/2.668e-008,td=4.435e+010/4.435e+010,
            pr=9.0156e-012/8.984e-012,bs=3.83e-011,b2=5.34e-008,bd=898^P2:sc=25.7/2.2,dc=15.6/2.2,
            ps=2.98/0.33,bs=0 Sample=1/mpi_a459_cam,2,2 Nreps=2/2 Missing=0.3298/0.0646 
            Parent_med=752.7136/0.00 Max2med_orig=53.6/12.4 Dotfull=0.796/0.012 Dot_cons=0.842/0.005
            Unassign_all=0.096 Unassigned=0.026 Dotbest=0.85 Flags=0,1,0 Naa=23 DUScorr=3.4/2.1/4.2 
            Dottheory=0.82 Pfin=2.4e+022 Probcorr=0.001 Tfratio=2.2e+012 Pfract=0 */
            library.getline(buffer);
            bal::trim_right_if(buffer, bal::is_any_of(" \r"));
            if(!istarts_with(buffer,"Comment: "))
                throw runtime_error("[SpectraStore::readHeader]: Failed to parse peptide data.");
            else
                erase_head(buffer,9);

            string modStr;
            string nmcStr;
            string nttStr;
            typedef vector<string> SplitVec;
            SplitVec splitVec;
            split(splitVec, buffer, is_any_of("= "));
            for(size_t index=0; index < splitVec.size(); ++index)
            {
                if(iequals(splitVec[index],"mods") && modStr.length()==0 )
                    modStr = splitVec[++index];
                else if(iequals(splitVec[index],"nmc") && nmcStr.length() ==0 )
                    nmcStr = splitVec[++index];
                else if(iequals(splitVec[index],"ntt") && nttStr.length() == 0 )
                    nttStr = splitVec[++index];
            }

            if(nmcStr.length()>0)
                numMissedCleavages = lexical_cast<size_t>(nmcStr);
            if(nttStr.length()>0)
                NTT = lexical_cast<int>(nttStr);

            if(NTT == 1)
                matchedPeptide.reset(new DigestedPeptide(peptide.begin(), peptide.end(), 0, numMissedCleavages, false, true, prevAA, nextAA));
            else if(NTT == 0)
                matchedPeptide.reset(new DigestedPeptide(peptide.begin(), peptide.end(), 0, numMissedCleavages, false, false, prevAA, nextAA));
            else
                matchedPeptide.reset(new DigestedPeptide(peptide.begin(), peptide.end(), 0, numMissedCleavages, true, true, prevAA, nextAA));
            //cout << getInterpretation(*matchedPeptide) << "," << peptide << endl;

            // Parse out the modifications [2/0,A,Acetyl/11,M,Oxidation]
            if(modStr != "0")
            {
                ModificationMap& modMap = matchedPeptide->modifications();

                tokenizer modsParser(modStr, backslash);
                tokenizer::iterator modsItr = modsParser.begin();
                ++modsItr;
                while(modsItr != modsParser.end())
                {
                    string mod = *(modsItr);
                    tokenizer modParser(mod, comma);
                    tokenizer::iterator modItr = modParser.begin();
                    int position = lexical_cast<int>(*(modItr));
                    ++modItr;
                    string modName = *(++modItr);
                    double modMass = ModNamesToMasses[modName];
                    if(fabs(modMass) > 0)
                    {
                        if(position < 0)
                        {
                            DynamicMod mod('(','(',modMass);
                            modMap.insert(make_pair<int,DynamicMod>(modMap.NTerminus(),mod));
                        } 
                        else
                        {
                            char aa = matchedPeptide->sequence().at(position);
                            DynamicMod mod(aa,aa,modMass);
                            modMap.insert(make_pair<int,DynamicMod>(position,mod));
                        }
                        //cout << position << "," << modName << "," << aa << "," << modMass << endl;
                    }
                    ++modsItr;
                }
            }

            // Parse out the protein
            string::size_type proteinStart = buffer.find(" Protein=");
            string::size_type proteinEnd = buffer.find(" ",proteinStart+1);
            string proteinAnn = buffer.substr(proteinStart + 9, proteinEnd - proteinStart - 9);
            //cout << proteinStart << "," << proteinEnd << "," << proteinAnn << endl;
            if(proteinAnn.find("/") == string::npos)
            {
                matchedProteins.insert(pair<string,size_t>(proteinAnn,0));
                return;
            }
            tokenizer protParser(proteinAnn, backslash);
            tokenizer::iterator protsItr = protParser.begin();
            ++protsItr;
            while(protsItr != protParser.end())
            {
                string proteinStr = *(protsItr);
                if(proteinStr.find(",") != string::npos)
                {
                    tokenizer splitProtStr(proteinStr,comma);
                    tokenizer::iterator splitItr = splitProtStr.begin();
                    string proteinAcc = *(splitItr);
                    size_t pos = lexical_cast<size_t>(*(++splitItr));
                    matchedProteins.insert(make_pair(proteinAcc,pos));
                }
                else
                    matchedProteins.insert(pair<string,size_t>(proteinStr,0));
                ++protsItr;
            }
        }

        void readSpectrum()
        {
            if(bal::ends_with(id.source,".index"))
            {
                BaseLibrarySpectrum::readSpectrum();
                return;
            }
            NativeFileReader library(id.source);
            readHeader(library);
            readPeaks(library);
        }

        void readSpectrumForIndexing()
        {
            NativeFileReader library(id.source);
            readHeader(library);
            averageMass = matchedPeptide->molecularWeight();
            monoisotopicMass = matchedPeptide->monoisotopicMass();
            readPeaks(library);
        }
    };

    struct NISTSpectrum : public virtual BaseLibrarySpectrum {

        size_t numPeaks;

        NISTSpectrum() : BaseLibrarySpectrum() { numPeaks = 0;}

        NISTSpectrum( const NISTSpectrum& old ) : BaseLibrarySpectrum( old ) { numPeaks = old.numPeaks; }

        ~NISTSpectrum()
        {
            BaseLibrarySpectrum::clearSpectrum();
        }

        void readPeaks(NativeFileReader& library)
        {
            library.seek(peakDataOffset);

            peakPreData.clear();
            peakAnns.clear();

            string buffer;
            while(library.getline(buffer))
            {
                bal::trim_right_if(buffer, bal::is_any_of(" \r"));
                if(buffer.empty()) // Break for empty line
                    break;
                
                if(!buffer.empty() && !isdigit(buffer[0]))
                    throw runtime_error("[SpectrumStore::readPeaks] Invalid index offset");

                tokenizer parser(buffer, peakDelim);
                tokenizer::iterator itr = parser.begin();
                string attribute = *(itr);
                string value = *(++itr);
                string peakAnn;
                //Skip the peak annotations if they exist
                if(++itr != parser.end())
                    peakAnn = *itr;
                if(isdigit(attribute[0]))
                {
                    float peakMass = lexical_cast<float>(attribute);
                    float intensity = lexical_cast<float>(value);
                    peakPreData.insert(pair<float,float>(peakMass,intensity));
                    peakAnns.insert(pair<float,string>(peakMass,peakAnn));
                }
            }
        }
        
        void clearHeader()
        {
            BaseLibrarySpectrum::clearHeader();
        }

        void clearSpectrum()
        {
            BaseLibrarySpectrum::clearSpectrum();
        }

        void readHeader(NativeFileReader& library)
        {
            library.seek(headerOffset);
            string input;
            matchedProteins.clear();
            ModMap mods;

            // Get the "Comment:" line. See below for an example string (all in one line). Yuck!
            /* Comment: Spec=Consensus Pep=Tryptic/miss_bad_unconfirmed Fullname=-.AAAAAAGAGPEM(O)VRGQVFDVGPR.Y/3 \
            Mods=2/0,A,Acetyl/11,M,Oxidation Parent=752.712 Inst=qtof Mz_diff=0.002 Mz_exact=752.7117
            Mz_av=753.182 Protein="tr|Q1HBJ4|Q1HBJ4_HUMAN Mitogen-activated protein kinase 1 
            [Homo sapiens]" Pseq=38 Organism="human" Se=3^X2:ex=0.0001057/0.0001043,td=0/0,sd=0/0,
            hs=54.2/3.6,bs=1.4e-006,b2=0.00021^O2:ex=2.67192e-008/2.668e-008,td=4.435e+010/4.435e+010,
            pr=9.0156e-012/8.984e-012,bs=3.83e-011,b2=5.34e-008,bd=898^P2:sc=25.7/2.2,dc=15.6/2.2,
            ps=2.98/0.33,bs=0 Sample=1/mpi_a459_cam,2,2 Nreps=2/2 Missing=0.3298/0.0646 
            Parent_med=752.7136/0.00 Max2med_orig=53.6/12.4 Dotfull=0.796/0.012 Dot_cons=0.842/0.005
            Unassign_all=0.096 Unassigned=0.026 Dotbest=0.85 Flags=0,1,0 Naa=23 DUScorr=3.4/2.1/4.2 
            Dottheory=0.82 Pfin=2.4e+022 Probcorr=0.001 Tfratio=2.2e+012 Pfract=0 */

            library.getline(input);
            // Get the spectrum type, interact style peptide sequence, and the modification string
            //cout << input << endl;
            size_t specStart = input.find("Spec=");
            size_t specEnd = input.find("Pep=");
            size_t pepStart = input.find("Fullname=");
            size_t pepEnd = input.find("Mods=");
            size_t modsEnd = input.find("Parent=");

            string specType;
            if(specStart != input.npos && specEnd != input.npos)
                specType = input.substr(specStart + 5, specEnd - specStart - 6);
            string interactSeq;
            if(pepStart != input.npos && pepEnd != input.npos)
                interactSeq = input.substr(pepStart + 9, pepEnd - pepStart - 10);
            string modStr;
            if(pepEnd != input.npos && modsEnd != input.npos)
                modStr = input.substr(pepEnd + 5, modsEnd - pepEnd - 6);

            //cout << specType << "," << interactSeq << "," << modStr << endl;
            if(interactSeq.length() == 0)
                throw "Failed to parse header entry for the spectrum";

            // Parse out the peptide string from the interact style sequence [-.AAAAAAGAGPEM(O)VRGQVFDVGPR.Y/3]
            tokenizer peptideParser(interactSeq, dot);
            tokenizer::iterator pepItr = peptideParser.begin();
            string prevAA = *(pepItr++);
            string peptide = *(pepItr++);
            string nextAA = *(pepItr);
            nextAA = nextAA[0];
            while(peptide.find("(") != string::npos)
            {
                string::size_type startPos = peptide.find("(");
                string::size_type endPos = peptide.find(")");
                peptide.erase(startPos, endPos-startPos+1);
            }
            matchedPeptide.reset(new DigestedPeptide(peptide));
            //cout << getInterpretation(*matchedPeptide) << "," << peptide << endl;

            // Parse out the modifications [2/0,A,Acetyl/11,M,Oxidation]
            if(modStr != "0")
            {
                ModificationMap& modMap = matchedPeptide->modifications();

                tokenizer modsParser(modStr, backslash);
                tokenizer::iterator modsItr = modsParser.begin();
                ++modsItr;
                while(modsItr != modsParser.end())
                {
                    string mod = *(modsItr);
                    tokenizer modParser(mod, comma);
                    tokenizer::iterator modItr = modParser.begin();
                    int position = lexical_cast<int>(*(modItr));
                    ++modItr;
                    string modName = *(++modItr);
                    double modMass = ModNamesToMasses[modName];
                    if(fabs(modMass) > 0)
                    {
                        if(position < 0)
                        {
                            DynamicMod mod('(','(',modMass);
                            modMap.insert(make_pair<int,DynamicMod>(modMap.NTerminus(),mod));
                        } 
                        else
                        {
                            char aa = matchedPeptide->sequence().at(position);
                            DynamicMod mod(aa,aa,modMass);
                            modMap.insert(make_pair<int,DynamicMod>(position,mod));
                        }
                        //cout << position << "," << modName << "," << aa << "," << modMass << endl;
                    }
                    ++modsItr;
                }
            }

            // Parse out the protein
            size_t proteinStart = input.find("Protein=");
            size_t proteinEnd = input.find("Pseq=");
            //cout << proteinStart << "," << proteinEnd << endl;
            string proteinAnn = input.substr(proteinStart + 8, proteinEnd - proteinStart - 9);
            proteinAnn = proteinAnn.substr(1,proteinAnn.length()-2);
            size_t pos = proteinAnn.find(" ");
            string proteinAcc = proteinAnn;
            if(pos != proteinAnn.npos)
                proteinAcc = proteinAnn.substr(0,proteinAnn.find(" "));
            matchedProteins.insert(pair<string,size_t>(proteinAcc,0));
        }

        void readSpectrum()
        {
            if(bal::ends_with(id.source,".index"))
            {
                BaseLibrarySpectrum::readSpectrum();
                return;
            }
            NativeFileReader library(id.source);
            readPeaks(library);
            readHeader(library);
        }

        void readSpectrumForIndexing()
        {
            NativeFileReader library(id.source);
            readHeader(library);
            averageMass = matchedPeptide->molecularWeight();
            monoisotopicMass = matchedPeptide->monoisotopicMass();
            readPeaks(library);
        }
    };

    struct SpectraStore : public vector< shared_ptr<BaseLibrarySpectrum> >
    {
        string                            libraryName;
        shared_ptr<sqlite::database>      library;
        boost::mutex                      libMutex;

        SpectraStore() { }

        ~SpectraStore()
        {
            clear();
        }

        void random_shuffle()
        {
            std::random_shuffle( begin(), end() );
        }

        void loadLibrary(const string& libName)
        {
            libraryName = libName;
            if(bal::ends_with(libraryName,".msp"))
                loadNISTLibraryFromMSP();
            else if(bal::ends_with(libraryName,".sptxt"))
                loadSpectraSTLibraryFromSptxt();
            else if(bal::ends_with(libraryName, ".index"))
                loadIndexedLibrary();
        }

        void loadIndexedLibrary()
        {
            cout << "Reading \"" << libraryName << "\"" << endl;
            Timer libReadTime(true);
            size_t spectrumIndex = 0;
            library.reset(new sqlite::database(libraryName.c_str()));
            library->execute("PRAGMA journal_mode=OFF;"
                                  "PRAGMA synchronous=OFF;"
                                  "PRAGMA automatic_indexing=OFF;"
                                  "PRAGMA default_cache_size=500000;"
                                  "PRAGMA temp_store=MEMORY"
                                 );

            sqlite::query qry(*library, "SELECT Id, Peptide, LibraryMass, MonoMass, AvgMass, Charge FROM LibMetaData");
            for (sqlite::query::iterator qItr = qry.begin(); qItr != qry.end(); ++qItr) 
            {
                ++spectrumIndex;
                if(!(spectrumIndex % 10000)) 
                    cout << ">> " << spectrumIndex << '\r' << flush;

                int charge;
                string peptide,id;
                double libMass, monoMass, avgMass;
                (*qItr).getter() >> id >> peptide >> libMass >> monoMass >> avgMass >> charge;
                
                shared_ptr<SpectraSTSpectrum> spectrum(new SpectraSTSpectrum);
                spectrum->id.charge = charge; 
                spectrum->id.source = libraryName;
                spectrum->id.nativeID = id;
                spectrum->libraryMass = libMass;
                spectrum->monoisotopicMass = monoMass;
                spectrum->averageMass = avgMass;
                spectrum->peakDataOffset = 0;
                spectrum->headerOffset = 0;
                push_back(spectrum);
            }
            cout << "Read " << spectrumIndex << " spectra from the library. " << libReadTime.End() << " seconds elapsed." << endl;;
        }

        void loadSpectraSTLibraryFromSptxt()
        {
            cout << "Reading \"" << libraryName << "\"" << endl;
            Timer libReadTime(true);
            ifstream library(libraryName.c_str(), ios::binary);
            size_t spectrumIndex = 0;
            stream_offset headerOffset = 0;
            stream_offset peakOffset = 0;
            stream_offset dataOffset = 0;
            if(library)
            {
                string buf;
                double parentMass;
                int charge;
                size_t numPeaks;
                while(getline(library,buf)) 
                {
                    size_t bufLength = buf.length()+1;
                    dataOffset += bufLength;
                    bal::trim_right_if(buf, bal::is_any_of(" \r"));
                    // Skip empty lines and comments
                    if (boost::starts_with(buf, "#") )
                        continue;

                    if(buf.empty() && headerOffset != 0 && peakOffset != 0)
                    {
                        shared_ptr<SpectraSTSpectrum> spectrum(new SpectraSTSpectrum);
                        spectrum->id.charge = charge; 
                        spectrum->id.source = libraryName;
                        spectrum->id.nativeID = "scan="+boost::lexical_cast<string>(spectrumIndex);
                        spectrum->libraryMass = (parentMass * charge) - (charge * 1.00727);
                        spectrum->peakDataOffset = peakOffset;
                        spectrum->headerOffset = headerOffset;
                        push_back(spectrum);
                        continue;
                    }

                    if(buf.empty())
                        continue;

                    if(boost::starts_with(buf, "Name:"))
                    {
                        tokenizer parser(buf, colon);
                        tokenizer::iterator itr = parser.begin();
                        string attribute = *(itr);
                        string value = *(++itr);
                        ++spectrumIndex;
                        if(!(spectrumIndex % 10000)) 
                            cout << spectrumIndex << ": " << dataOffset << '\r' << flush;
                        tokenizer splitter(value, backslash);
                        tokenizer::iterator pItr = splitter.begin();
                        ++pItr;
                        charge = lexical_cast<int>(*pItr);
                    } else if(boost::starts_with(buf, "NumPeaks"))
                    {
                        tokenizer parser(buf, colon);
                        tokenizer::iterator itr = parser.begin();
                        string attribute = *(itr);
                        string value = *(++itr);
                        numPeaks = lexical_cast<size_t>(value);
                    } else if(boost::starts_with(buf, "PrecursorMZ"))
                    {
                        tokenizer parser(buf, colon);
                        tokenizer::iterator itr = parser.begin();
                        string attribute = *(itr);
                        string value = *(++itr);
                        parentMass = lexical_cast<double>(value);
                    } else if(boost::starts_with(buf, "FullName"))
                    {
                        headerOffset = dataOffset - bufLength ;
                        peakOffset = 0;
                    } else if(!buf.empty() && isdigit(buf[0]) && peakOffset == 0)
                    {
                        peakOffset = dataOffset-bufLength;
                    }
                }
            }
            cout << "Read " << (spectrumIndex+1) << " spectra from library; " << libReadTime.End() << " seconds elapsed." << endl;
        }

        void loadNISTLibraryFromMSP()
        {
            cout << "Reading \"" << libraryName << "\"" << endl;
            Timer libReadTime(true);
            ifstream library(libraryName.c_str(), ios::binary);
            size_t spectrumIndex = 0;
            stream_offset headerOffset = 0;
            stream_offset peakOffset = 0;
            stream_offset dataOffset = 0;
            if(library)
            {
                string buf;
                double parentMass;
                int charge;
                size_t numPeaks;
                while(getline(library,buf)) 
                {
                    size_t bufLength = buf.length()+1;
                    dataOffset += bufLength;
                    bal::trim_right_if(buf, bal::is_any_of(" \r"));
                    // Skip the comments
                    if( boost::starts_with(buf, "#") )
                        continue;

                    if(buf.empty() && headerOffset != 0 && peakOffset != 0)
                    {
                        shared_ptr<NISTSpectrum> spectrum(new NISTSpectrum);
                        spectrum->id.charge = charge; 
                        spectrum->id.source = libraryName;
                        spectrum->id.nativeID = "scan="+boost::lexical_cast<string>(spectrumIndex);
                        spectrum->libraryMass = (parentMass * charge) - (charge * 1.00727);
                        spectrum->peakDataOffset = peakOffset;
                        spectrum->headerOffset = headerOffset;
                        push_back(spectrum);
                        continue;
                    }

                    if(boost::starts_with(buf, "Name:"))
                    {
                        tokenizer parser(buf, colon);
                        tokenizer::iterator itr = parser.begin();
                        string attribute = *(itr);
                        string value = *(++itr);
                        ++spectrumIndex;
                        if(!(spectrumIndex % 10000)) 
                            cout << spectrumIndex << ": " << dataOffset << '\r' << flush;
                        tokenizer splitter(value, backslash);
                        tokenizer::iterator pItr = splitter.begin();
                        ++pItr;
                        charge = lexical_cast<int>(*pItr);
                    } else if(boost::starts_with(buf, "Num peaks"))
                    {
                        tokenizer parser(buf, colon);
                        tokenizer::iterator itr = parser.begin();
                        string attribute = *(itr);
                        string value = *(++itr);
                        numPeaks = lexical_cast<size_t>(value);
                    } else if(boost::starts_with(buf, "Comment"))
                    {
                        headerOffset = dataOffset- bufLength ;
                        peakOffset = 0;
                        // Parse out the parent mass
                        size_t pStart = buf.find("Parent=");
                        size_t pEnd = buf.find("Inst=");
                        parentMass = lexical_cast<double>(buf.substr(pStart + 7, pEnd-pStart-8));
                    } else if (!buf.empty() && isdigit(buf[0]) && peakOffset == 0)
                    {
                        peakOffset = dataOffset-bufLength;
                    }
                }
            }
            cout << "Read " << (spectrumIndex+1) << " spectra from library; " << libReadTime.End() << " seconds elapsed." << endl;
        }

        void readSpectra()
        {
            for(vector<shared_ptr<BaseLibrarySpectrum> >::iterator sItr = begin(); sItr != end(); ++sItr )
                (*sItr)->readSpectrum();
        }

        void readSpectraAsBatch(const vector<size_t>& indices)
        {
            if(bal::ends_with(libraryName,".index"))
            {
                map<string, size_t> libraryIndexToArrayIndex;
                stringstream batchedIndicesStream;
                BOOST_FOREACH(size_t arrayIndex, indices)
                {
                    string libraryIndex = at(arrayIndex)->id.nativeID;
                    batchedIndicesStream << "'" << libraryIndex << "'" << ",";
                    libraryIndexToArrayIndex.insert(make_pair(libraryIndex,arrayIndex));
                }

                string batchedIndices = batchedIndicesStream.str();
                bal::trim_right_if(batchedIndices, bal::is_any_of(" ,"));
                bal::trim_left_if(batchedIndices, bal::is_any_of(" ,"));
                
                string queryStr = "SELECT Id, NumPeaks, SpectrumData FROM LibSpectrumData WHERE Id IN (" + batchedIndices + ")";
                
                map<string, shared_ptr<string> > spectraData;
                {
                    boost::unique_lock<boost::mutex> guard(libMutex,boost::defer_lock);
                    guard.lock();
                    //cout << boost::this_thread::get_id() << "fetching data" << endl;
                    START_PROFILER(0)
                    sqlite::query qry(*library, queryStr.c_str() );
                    for (sqlite::query::iterator qItr = qry.begin(); qItr != qry.end(); ++qItr) 
                    {
                        try {
                            string index;
                            int numPeaks;
                            (*qItr).getter() >> index >> numPeaks;
                            shared_ptr<string> data(new string(static_cast<const char*>((*qItr).get<void const*>(2)), (*qItr).column_bytes(2)));
                            spectraData.insert(make_pair(index,data));
                        }catch(exception&) { cout << "Spectral data reading error.";}
                    }
                    STOP_PROFILER(0)
                    //cout << boost::this_thread::get_id() << "finished fetching data :" << spectraData.size() << endl;
                    guard.unlock();
                }

                START_PROFILER(1)
                typedef pair<string, shared_ptr<string> > SpectrumData;
                BOOST_FOREACH(const SpectrumData& sd, spectraData)
                {
                    shared_ptr<BaseLibrarySpectrum> spectrum = at(libraryIndexToArrayIndex[sd.first]);
                    START_PROFILER(2)    
                    stringstream encoded(*sd.second.get());
                    STOP_PROFILER(2)
                    eos::portable_iarchive packArchive( encoded );
                    packArchive & *spectrum;
                }
                STOP_PROFILER(1)
            } else if(bal::ends_with(libraryName,".sptxt"))
            {
                BOOST_FOREACH(const size_t& arrayIndex, indices)
                {
                    shared_ptr<BaseLibrarySpectrum> spectrum = at(arrayIndex);
                    spectrum->readSpectrum();
                }
            }
        }


        void recalculatePrecursorMasses()
        {
            if(bal::ends_with(libraryName,".index"))
                return;

            NativeFileReader library(libraryName.c_str());
            size_t spectraIndex = 0;
            size_t totalSpectra = size();
            for(vector<shared_ptr<BaseLibrarySpectrum> >::iterator sItr = begin(); sItr != end(); ++sItr )
            {
                ++spectraIndex;
                (*sItr)->readHeader(library);
                (*sItr)->averageMass = (*sItr)->matchedPeptide->molecularWeight();
                (*sItr)->monoisotopicMass = (*sItr)->matchedPeptide->monoisotopicMass();
                (*sItr)->matchedPeptide.reset();
                if(!(spectraIndex % 10000))
                    cout << totalSpectra << ": " << spectraIndex << '\r' << flush;
            }
        }

        void printSpectra()
        {
            for(vector<shared_ptr<BaseLibrarySpectrum> >::const_iterator sItr = begin(); sItr != end(); ++sItr )
            {
                cout << (*sItr)->id.source << "," << (*sItr)->id.charge << "," << (string) (*sItr)->id.nativeID << endl;
                cout << (*sItr)->libraryMass << endl;
                cout << (*sItr)->matchedPeptide->sequence() << "->" << getInterpretation(*((*sItr)->matchedPeptide)) << endl;
                cout << (*sItr)->numMissedCleavages << "," << (*sItr)->NTT << endl;
                for(PeakPreData::const_iterator pItr = (*sItr)->peakPreData.begin(); pItr != (*sItr)->peakPreData.end(); ++pItr)
                    cout << (*pItr).first << "," << (*pItr).second << endl;
                for(ProteinMap::const_iterator pItr = (*sItr)->matchedProteins.begin(); pItr != (*sItr)->matchedProteins.end(); ++pItr)
                    cout << (*pItr).first << "," << (*pItr).second << endl;
            }
        }
    };
}
}

#endif
