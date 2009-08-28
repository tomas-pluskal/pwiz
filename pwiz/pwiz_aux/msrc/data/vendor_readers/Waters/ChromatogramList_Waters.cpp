//
// $Id$
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

#ifdef PWIZ_READER_WATERS

#include "ChromatogramList_Waters.hpp"
#include "Reader_Waters_Detail.hpp"
#include "pwiz/utility/misc/SHA1Calculator.hpp"
#include "boost/shared_ptr.hpp"
#include "pwiz/utility/misc/String.hpp"
#include "pwiz/utility/misc/Filesystem.hpp"
#include <boost/bind.hpp>
#include <boost/foreach.hpp>
#include <iostream>

using namespace std;
using boost::shared_ptr;
using boost::lexical_cast;
using boost::bad_lexical_cast;


namespace pwiz {
namespace msdata {
namespace detail {


PWIZ_API_DECL ChromatogramList_Waters::ChromatogramList_Waters(RawDataPtr rawdata)
:   rawdata_(rawdata),
    size_(0),
    indexInitialized_(BOOST_ONCE_INIT)
{
}


PWIZ_API_DECL size_t ChromatogramList_Waters::size() const
{
    boost::call_once(indexInitialized_, boost::bind(&ChromatogramList_Waters::createIndex, this));
    return size_;
}


PWIZ_API_DECL const ChromatogramIdentity& ChromatogramList_Waters::chromatogramIdentity(size_t index) const
{
    boost::call_once(indexInitialized_, boost::bind(&ChromatogramList_Waters::createIndex, this));
    if (index>size_)
        throw runtime_error(("[ChromatogramList_Waters::chromatogramIdentity()] Bad index: " 
                            + lexical_cast<string>(index)).c_str());
    return index_[index];
}


PWIZ_API_DECL size_t ChromatogramList_Waters::find(const string& id) const
{
    boost::call_once(indexInitialized_, boost::bind(&ChromatogramList_Waters::createIndex, this));

    map<string, size_t>::const_iterator scanItr = idToIndexMap_.find(id);
    if (scanItr == idToIndexMap_.end())
        return size_;
    return scanItr->second;
}


PWIZ_API_DECL ChromatogramPtr ChromatogramList_Waters::chromatogram(size_t index, bool getBinaryData) const
{
    boost::call_once(indexInitialized_, boost::bind(&ChromatogramList_Waters::createIndex, this));
    if (index>size_)
        throw runtime_error(("[ChromatogramList_Waters::chromatogram()] Bad index: " 
                            + lexical_cast<string>(index)).c_str());

    
    // allocate a new Chromatogram
    IndexEntry& ie = index_[index];
    ChromatogramPtr result = ChromatogramPtr(new Chromatogram);
    if (!result.get())
        throw std::runtime_error("[ChromatogramList_Thermo::chromatogram()] Allocation error.");

    result->index = index;
    result->id = ie.id;
    result->set(ie.chromatogramType);

    switch (ie.chromatogramType)
    {
        case MS_TIC_chromatogram:
        {
            map<double, double> fullFileTIC;

            BOOST_FOREACH(const FunctionPtr& functionPtr, rawdata_->functions())
            {
                // add current function TIC to full file TIC
                automation_vector<float> times, intensities;
                functionPtr->getTIC(times, intensities);
                for (int i = 0, end = intensities.size(); i < end; ++i)
                    fullFileTIC[times[i]] += intensities[i];
            }

            result->setTimeIntensityArrays(std::vector<double>(), std::vector<double>(), UO_minute, MS_number_of_counts);

            if (getBinaryData)
            {
                BinaryDataArrayPtr timeArray = result->getTimeArray();
                BinaryDataArrayPtr intensityArray = result->getIntensityArray();

                timeArray->data.reserve(fullFileTIC.size());
                intensityArray->data.reserve(fullFileTIC.size());
                for (map<double, double>::iterator itr = fullFileTIC.begin();
                     itr != fullFileTIC.end();
                     ++itr)
                {
                    timeArray->data.push_back(itr->first);
                    intensityArray->data.push_back(itr->second);
                }
            }

            result->defaultArrayLength = fullFileTIC.size();
        }
        break;

        case MS_SRM_chromatogram:
        {
            result->precursor.isolationWindow.set(MS_isolation_window_target_m_z, ie.target.Q1, MS_m_z);
            //result->precursor.isolationWindow.set(MS_isolation_window_lower_offset, ie.q1, MS_m_z);
            //result->precursor.isolationWindow.set(MS_isolation_window_upper_offset, ie.q1, MS_m_z);
            result->precursor.activation.set(MS_CID);

            result->product.isolationWindow.set(MS_isolation_window_target_m_z, ie.target.Q3, MS_m_z);
            //result->product.isolationWindow.set(MS_isolation_window_lower_offset, ie.q3, MS_m_z);
            //result->product.isolationWindow.set(MS_isolation_window_upper_offset, ie.q3, MS_m_z);

            result->setTimeIntensityArrays(std::vector<double>(), std::vector<double>(), UO_minute, MS_number_of_counts);

            automation_vector<float> times, intensities;
            ie.functionPtr->getSIC(ie.SRMIndex, times, intensities);
            result->defaultArrayLength = times.size();

            if (getBinaryData)
            {
                result->getTimeArray()->data.assign(times.begin(), times.end());
                result->getIntensityArray()->data.assign(intensities.begin(), intensities.end());
            }
        }
        break;
    }

    return result;
}


PWIZ_API_DECL void ChromatogramList_Waters::createIndex() const
{
    index_.push_back(IndexEntry());
    IndexEntry& ie = index_.back();
    ie.index = index_.size()-1;
    ie.id = "TIC";
    ie.chromatogramType = MS_TIC_chromatogram;
    idToIndexMap_[ie.id] = ie.index;

    BOOST_FOREACH(const FunctionPtr& functionPtr, rawdata_->functions())
    {
        if (functionPtr->getFunctionType() != FunctionType_MRM)
            continue;

        for (size_t i=0, end=functionPtr->getSRMSize(); i < end; ++i)
        {
            index_.push_back(IndexEntry());
            IndexEntry& ie = index_.back();
            functionPtr->getSRM(i, ie.target);
            ie.chromatogramType = MS_SRM_chromatogram;
            ie.index = index_.size()-1;
            ie.SRMIndex = i;
            ie.functionPtr = functionPtr;

            std::ostringstream oss;
            oss << "SRM SIC Q1=" << ie.target.Q1 <<
                   " Q3=" << ie.target.Q3 <<
                   " function=" << ie.functionPtr->getFunctionNumber() <<
                   " index=" << i;
            ie.id = oss.str();
            idToIndexMap_[ie.id] = ie.index;
        }
    }

    size_ = index_.size();
}


} // detail
} // msdata
} // pwiz


#endif // PWIZ_READER_WATERS
