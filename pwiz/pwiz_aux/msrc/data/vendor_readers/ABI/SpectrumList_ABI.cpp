//
// SpectrumList_ABI.cpp
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


#include "SpectrumList_ABI.hpp"
#include "Reader_ABI_Detail.hpp"


#ifdef PWIZ_READER_ABI
#include "pwiz/utility/misc/SHA1Calculator.hpp"
#include "boost/shared_ptr.hpp"
#include "pwiz/utility/misc/String.hpp"
#include "pwiz/utility/misc/Filesystem.hpp"
#include <boost/bind.hpp>
#include <iostream>

using namespace std;
using boost::shared_ptr;


namespace pwiz {
namespace msdata {
namespace detail {


PWIZ_API_DECL SpectrumList_ABI::SpectrumList_ABI(const MSData& msd, WiffFilePtr wifffile, int sample)
:   msd_(msd),
    wifffile_(wifffile),
    sample(sample),
    size_(0),
    indexInitialized_(BOOST_ONCE_INIT)
{
}


PWIZ_API_DECL size_t SpectrumList_ABI::size() const
{
    boost::call_once(indexInitialized_, boost::bind(&SpectrumList_ABI::createIndex, this));
    return size_;
}


PWIZ_API_DECL const SpectrumIdentity& SpectrumList_ABI::spectrumIdentity(size_t index) const
{
    boost::call_once(indexInitialized_, boost::bind(&SpectrumList_ABI::createIndex, this));
    if (index>size_)
        throw runtime_error(("[SpectrumList_ABI::spectrumIdentity()] Bad index: " 
                            + lexical_cast<string>(index)).c_str());
    return index_[index];
}


PWIZ_API_DECL size_t SpectrumList_ABI::find(const string& id) const
{
    boost::call_once(indexInitialized_, boost::bind(&SpectrumList_ABI::createIndex, this));

    map<string, size_t>::const_iterator scanItr = idToIndexMap_.find(id);
    if (scanItr == idToIndexMap_.end())
        return size_;
    return scanItr->second;
}


PWIZ_API_DECL SpectrumPtr SpectrumList_ABI::spectrum(size_t index, bool getBinaryData) const
{
    return spectrum(index, getBinaryData, pwiz::util::IntegerSet());
}

PWIZ_API_DECL SpectrumPtr SpectrumList_ABI::spectrum(size_t index, bool getBinaryData, const pwiz::util::IntegerSet& msLevelsToCentroid) const 
{
    boost::call_once(indexInitialized_, boost::bind(&SpectrumList_ABI::createIndex, this));
    if (index>size_)
        throw runtime_error(("[SpectrumList_ABI::spectrum()] Bad index: " 
                            + lexical_cast<string>(index)).c_str());

    
    // allocate a new Spectrum
    IndexEntry& ie = index_[index];
    SpectrumPtr result = SpectrumPtr(new Spectrum);
    if (!result.get())
        throw std::runtime_error("[SpectrumList_Thermo::spectrum()] Allocation error.");

    result->index = index;
    result->id = ie.id;

    //Console::WriteLine("spce: {0}.{1}.{2}.{3}", ie.sample, ie.period, ie.cycle, ie.experiment);

    result->scanList.set(MS_no_combination);
    result->scanList.scans.push_back(Scan());
    Scan& scan = result->scanList.scans[0];

    ExperimentPtr msExperiment = wifffile_->getExperiment(ie.sample, ie.period, ie.experiment);
    pwiz::vendor_api::ABI::SpectrumPtr spectrum = wifffile_->getSpectrum(msExperiment, ie.cycle);

    double scanTime = msExperiment->getCycleStartTime(ie.cycle);
    if (scanTime > 0)
        scan.set(MS_scan_start_time, scanTime, UO_minute);

    ScanType scanType = msExperiment->getScanType();
    int msLevel = translateAsMSLevel(scanType);
    result->set(MS_ms_level, msLevel);
    result->set(translateAsSpectrumType(scanType));

    double startMz, stopMz;
    msExperiment->getAcquisitionMassRange(startMz, stopMz);
    scan.scanWindows.push_back(ScanWindow(startMz, stopMz, MS_m_z));

    // decide whether to use Points or Peaks to populate data arrays
    bool doCentroid = msLevelsToCentroid.contains(msLevel);

    bool continuousData = spectrum->getDataIsContinuous();
    if (continuousData && !doCentroid)
        result->set(MS_profile_spectrum);
    else
    {
        result->set(MS_centroid_spectrum);
        doCentroid = continuousData;
    }

    if (scanType == MRM)
    {
        /*MRMTransitions^ transitions = msExperiment->MRMTransitions;
        double q1mz = transitions[ie.transition]->Q1Mass;//ie.transition->first;
        double q3mz = transitions[ie.transition]->Q3Mass;
        double intensity = points[ie.transition]->Y;
        result->defaultArrayLength = 1;//ie.transition->second.size();

        Precursor precursor;
        SelectedIon selectedIon;

        selectedIon.set(MS_selected_ion_m_z, q1mz, MS_m_z);

        precursor.activation.set(MS_CID); // assume CID

        precursor.selectedIons.push_back(selectedIon);
        result->precursors.push_back(precursor);

        if (getBinaryData)
        {
            mzArray.resize(result->defaultArrayLength, q3mz);
            intensityArray.resize(result->defaultArrayLength, intensity);
        }*/
    }
    else
    {
        if (spectrum->getHasPrecursorInfo())
        {
            double selectedMz, intensity;
            int charge;
            spectrum->getPrecursorInfo(selectedMz, intensity, charge);

            Precursor precursor;
            SelectedIon selectedIon;

            selectedIon.set(MS_selected_ion_m_z, selectedMz, MS_m_z);
            if (charge > 0)
                selectedIon.set(MS_charge_state, charge);

            precursor.activation.set(MS_CID); // assume CID

            precursor.selectedIons.push_back(selectedIon);
            result->precursors.push_back(precursor);
        }

        result->set(MS_lowest_observed_m_z, spectrum->getMinX(), MS_m_z);
        result->set(MS_highest_observed_m_z, spectrum->getMaxX(), MS_m_z);
        result->set(MS_base_peak_intensity, spectrum->getBasePeakY(), MS_number_of_counts);
        result->set(MS_base_peak_m_z, spectrum->getBasePeakX(), MS_m_z);
        result->set(MS_total_ion_current, spectrum->getSumY(), MS_number_of_counts);

        if (getBinaryData)
        {
            result->setMZIntensityArrays(std::vector<double>(), std::vector<double>(), MS_number_of_counts);
            BinaryDataArrayPtr mzArray = result->getMZArray();
            BinaryDataArrayPtr intensityArray = result->getIntensityArray();

            spectrum->getData(doCentroid, mzArray->data, intensityArray->data);
        }

        result->defaultArrayLength = spectrum->getDataSize(doCentroid);
    }

    return result;
}


PWIZ_API_DECL void SpectrumList_ABI::createIndex() const
{
    int periodCount = wifffile_->getPeriodCount(sample);
    for (int ii=1; ii <= periodCount; ++ii)
    {
        //Console::WriteLine("Sample {0}, Period {1}", sample, ii);

        int experimentCount = wifffile_->getExperimentCount(sample, ii);
        for (int iii=1; iii <= experimentCount; ++iii)
        {
            ExperimentPtr msExperiment = wifffile_->getExperiment(sample, ii, iii);
            if (msExperiment->getScanType() != MRM)
            {
                vector<double> times, intensities;
                msExperiment->getTIC(times, intensities);

                for (int iiii = 0, end = intensities.size(); iiii < end; ++iiii)
                {
                    if (intensities[iiii] > 0)
                    {
                        index_.push_back(IndexEntry());
                        IndexEntry& ie = index_.back();
                        ie.sample = sample;
                        ie.period = ii;
                        ie.cycle = iiii+1;
                        ie.experiment = iii;
                        ie.index = index_.size()-1;

                        std::ostringstream oss;
                        oss << "sample=" << ie.sample <<
                               " period=" << ie.period <<
                               " cycle=" << ie.cycle <<
                               " experiment=" << ie.experiment;
                        ie.id = oss.str();
                    }
                }
            }
        }
    }

    size_ = index_.size();
}


} // detail
} // msdata
} // pwiz


#else // PWIZ_READER_ABI

//
// non-MSVC implementation
//

namespace pwiz {
namespace msdata {
namespace detail {

namespace {const SpectrumIdentity emptyIdentity;}

SpectrumList_ABI::SpectrumList_ABI(const MSData& msd, WiffFilePtr wifffile, int sample) : msd_(msd) {}
size_t SpectrumList_ABI::size() const {return 0;}
const SpectrumIdentity& SpectrumList_ABI::spectrumIdentity(size_t index) const {return emptyIdentity;}
size_t SpectrumList_ABI::find(const std::string& id) const {return 0;}
SpectrumPtr SpectrumList_ABI::spectrum(size_t index, bool getBinaryData) const {return SpectrumPtr();}
SpectrumPtr SpectrumList_ABI::spectrum(size_t index, bool getBinaryData, const pwiz::util::IntegerSet& msLevelsToCentroid) const {return SpectrumPtr();}
void SpectrumList_ABI::createIndex() const {}

} // detail
} // msdata
} // pwiz

#endif // PWIZ_READER_ABI
