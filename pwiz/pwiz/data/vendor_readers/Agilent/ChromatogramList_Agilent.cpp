//
// ChromatogramList_Agilent.cpp
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


#define PWIZ_SOURCE

#ifdef PWIZ_READER_AGILENT
#include "pwiz/data/msdata/CVTranslator.hpp"
#include "pwiz/utility/vendor_api/thermo/RawFile.h"
#include "pwiz/utility/misc/SHA1Calculator.hpp"
#include "boost/shared_ptr.hpp"
#include "pwiz/utility/misc/String.hpp"
#include "pwiz/utility/misc/Filesystem.hpp"
#include "pwiz/utility/misc/Stream.hpp"
#include "Reader_Agilent_Detail.hpp"
#include "ChromatogramList_Agilent.hpp"
#include <boost/bind.hpp>


namespace pwiz {
namespace msdata {
namespace detail {

ChromatogramList_Agilent::ChromatogramList_Agilent(AgilentDataReaderPtr rawfile)
:   rawfile_(rawfile), indexInitialized_(BOOST_ONCE_INIT)
{
}


PWIZ_API_DECL size_t ChromatogramList_Agilent::size() const
{
    boost::call_once(indexInitialized_, boost::bind(&ChromatogramList_Agilent::createIndex, this));
    return index_.size();
}


PWIZ_API_DECL const ChromatogramIdentity& ChromatogramList_Agilent::chromatogramIdentity(size_t index) const
{
    boost::call_once(indexInitialized_, boost::bind(&ChromatogramList_Agilent::createIndex, this));
    if (index>size())
        throw runtime_error(("[ChromatogramList_Agilent::chromatogramIdentity()] Bad index: " 
                            + lexical_cast<string>(index)).c_str());
    return reinterpret_cast<const ChromatogramIdentity&>(index_[index]);
}


PWIZ_API_DECL size_t ChromatogramList_Agilent::find(const string& id) const
{
    boost::call_once(indexInitialized_, boost::bind(&ChromatogramList_Agilent::createIndex, this));
    map<string, size_t>::const_iterator itr = idMap_.find(id);
    if (itr != idMap_.end())
        return itr->second;

    return size();
}


PWIZ_API_DECL ChromatogramPtr ChromatogramList_Agilent::chromatogram(size_t index, bool getBinaryData) const 
{
    boost::call_once(indexInitialized_, boost::bind(&ChromatogramList_Agilent::createIndex, this));
    if (index>size())
        throw runtime_error(("[ChromatogramList_Agilent::chromatogram()] Bad index: " 
                            + lexical_cast<string>(index)).c_str());

    const IndexEntry& ci = index_[index];
    ChromatogramPtr result(new Chromatogram);
    result->index = ci.index;
    result->id = ci.id;

    result->set(ci.chromatogramType);

    switch (ci.chromatogramType)
    {
        default:
            break;

        case MS_TIC_chromatogram: // generate TIC for entire run
        {
            vector<double> intensities(rawfile_->ticIntensities.begin(), rawfile_->ticIntensities.end());
            if (getBinaryData) result->setTimeIntensityArrays(rawfile_->ticTimes, intensities, UO_minute, MS_number_of_counts);
            else result->defaultArrayLength = rawfile_->ticTimes.size();
        }
        break;

        case MS_SIC_chromatogram: // generate SRM SIC for transition <precursor>,<product>
        {
            IChromatogramFilterPtr filterPtr(BDA::CLSID_BDAChromFilter);
            filterPtr->ChromatogramType = ChromType_MultipleReactionMode;
            filterPtr->SingleChromatogramForAllMasses = VARIANT_FALSE;
            filterPtr->ExtractOneChromatogramPerScanSegment = VARIANT_TRUE;

            vector<IChromatogramPtr> chromatogramArray;
            convertSafeArrayToVector(rawfile_->dataReaderPtr->GetChromatogram(filterPtr), chromatogramArray);
            IChromatogramPtr& chromatogramPtr = chromatogramArray[ci.index-1];

            result->precursor.isolationWindow.set(MS_isolation_window_target_m_z, ci.q1, MS_m_z);

            result->precursor.activation.set(MS_CID);
            result->precursor.activation.set(MS_collision_energy, chromatogramPtr->CollisionEnergy);

            result->product.isolationWindow.set(MS_isolation_window_target_m_z, ci.q3, MS_m_z);
            //result->product.isolationWindow.set(MS_isolation_window_lower_offset, ci.q3Offset, MS_m_z);
            //result->product.isolationWindow.set(MS_isolation_window_upper_offset, ci.q3Offset, MS_m_z);

            vector<double> times, intensities;
            convertSafeArrayToVector(chromatogramPtr->xArray, times);

            vector<float> yArray;
            convertSafeArrayToVector(chromatogramPtr->yArray, yArray);
            intensities.assign(yArray.begin(), yArray.end());

            if (getBinaryData) result->setTimeIntensityArrays(times, intensities, UO_minute, MS_number_of_counts);
            else result->defaultArrayLength = times.size();
        }
        break;
    }

    return result;
}


PWIZ_API_DECL void ChromatogramList_Agilent::createIndex() const
{
    // support file-level TIC for all file types
    index_.push_back(IndexEntry());
    IndexEntry& ci = index_.back();
    ci.index = index_.size()-1;
    ci.chromatogramType = MS_TIC_chromatogram;
    ci.id = "TIC";
    idMap_[ci.id] = ci.index;

    vector<IRangePtr> transitions;
    convertSafeArrayToVector(rawfile_->scanFileInfoPtr->MRMTransitions, transitions);
 
    for (size_t i=0, end=transitions.size(); i < end; ++i)
    {
        index_.push_back(IndexEntry());
        IndexEntry& ci = index_.back();
        ci.index = index_.size()-1;
        ci.chromatogramType = MS_SIC_chromatogram;
        ci.q1 = transitions[i]->Start;
        ci.q3 = transitions[i]->End;
        ci.id = (format("SRM SIC %.10g,%.10g")
                 % ci.q1
                 % ci.q3
                ).str();
        idMap_[ci.id] = ci.index;
    }
}

} // detail
} // msdata
} // pwiz

#endif // PWIZ_READER_AGILENT
