//
// $Id$
//
//
// Original author: Darren Kessner <darren@proteowizard.org>
//
// Copyright 2008 Spielberg Family Center for Applied Proteomics
//   Cedars-Sinai Medical Center, Los Angeles, California  90048
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

#include "RAMPAdapter.hpp"
#include "MSDataFile.hpp"
#include "LegacyAdapter.hpp"
#include "CVTranslator.hpp"
#include "boost/lexical_cast.hpp"
#include "boost/static_assert.hpp"
#include <stdexcept>
#include <iostream>
#include <algorithm>


namespace pwiz {
namespace msdata {


using namespace std;
using boost::lexical_cast;
using boost::bad_lexical_cast;


class RAMPAdapter::Impl
{
    public:

    Impl(const string& filename) 
    :   msd_(filename), firstIndex_((size_t)-1), lastIndex_(0)
    {
        if (!msd_.run.spectrumListPtr.get())
            throw runtime_error("[RAMPAdapter] Null spectrumListPtr.");

        // flag spectra not from the default source file
        size_ = msd_.run.spectrumListPtr->size();
        nonDefaultSpectra_.resize(size_, false);
        for (size_t i=0, end=size_; i < end; ++i)
        {
            SpectrumPtr s = msd_.run.spectrumListPtr->spectrum(i, false);
            if (s->sourceFilePtr.get() && s->sourceFilePtr != msd_.run.defaultSourceFilePtr)
            {
                nonDefaultSpectra_[i] = true;
                --size_;
            }
            else
            {
                if (firstIndex_ > lastIndex_)
                    firstIndex_ = i;
                lastIndex_ = i;
            }
        }
    }

    size_t scanCount() const
    {
        return size_;
    }

    size_t index(int scanNumber) const 
    {
        CVID nativeIdFormat = id::getDefaultNativeIDFormat(msd_);
        string scanNumberStr = lexical_cast<string>(scanNumber);
        string id = id::translateScanNumberToNativeID(nativeIdFormat, scanNumberStr);
        if (id.empty()) // unsupported nativeID type
        {
            size_t index = scanNumber-1; // assume scanNumber is a 1-based index
            if (index >= size_)
                throw out_of_range("[RAMPAdapter] scanNumber " + scanNumberStr + " (treated as 1-based index) is out of range");
            return index;
        }
        return msd_.run.spectrumListPtr->find(id);
    }

    void getScanHeader(size_t index, ScanHeaderStruct& result) const;
    void getScanPeaks(size_t index, std::vector<double>& result) const;
    void getRunHeader(RunHeaderStruct& result) const;
    void getInstrument(InstrumentStruct& result) const;

    private:
    MSDataFile msd_;
    CVTranslator cvTranslator_;
    vector<bool> nonDefaultSpectra_;
    size_t firstIndex_, lastIndex_;
    size_t size_;
};


namespace {

double retentionTime(const Scan& scan)
{
    CVParam param = scan.cvParam(MS_scan_start_time);
    if (param.units == UO_second) 
        return param.valueAs<double>();
    else if (param.units == UO_minute) 
        return param.valueAs<double>() * 60;
    return 0;
}

} // namespace


void RAMPAdapter::Impl::getScanHeader(size_t index, ScanHeaderStruct& result) const
{
    const SpectrumList& spectrumList = *msd_.run.spectrumListPtr;
    SpectrumPtr spectrum = spectrumList.spectrum(index);

    Scan dummy;
    Scan& scan = spectrum->scanList.scans.empty() ? dummy : spectrum->scanList.scans[0];

    CVID nativeIdFormat = id::getDefaultNativeIDFormat(msd_);
    string scanNumber = id::translateNativeIDToScanNumber(nativeIdFormat, spectrum->id);
    result.seqNum = static_cast<int>(index + 1);
    if (scanNumber.empty()) // unsupported nativeID type
    {
        // assume scanNumber is a 1-based index, consistent with this->index() method
        result.acquisitionNum = result.seqNum;
    } 
    else 
    {
        result.acquisitionNum = lexical_cast<int>(scanNumber);
    }
    result.msLevel = spectrum->cvParam(MS_ms_level).valueAs<int>();
    result.peaksCount = static_cast<int>(spectrum->defaultArrayLength);
    result.totIonCurrent = spectrum->cvParam(MS_total_ion_current).valueAs<double>();
    result.retentionTime = scan.cvParam(MS_scan_start_time).timeInSeconds();
    result.basePeakMZ = spectrum->cvParam(MS_base_peak_m_z).valueAs<double>();    
    result.basePeakIntensity = spectrum->cvParam(MS_base_peak_intensity).valueAs<double>();    
    result.collisionEnergy = 0;
    result.ionisationEnergy = spectrum->cvParam(MS_ionization_energy).valueAs<double>();
    result.lowMZ = spectrum->cvParam(MS_lowest_observed_m_z).valueAs<double>();        
    result.highMZ = spectrum->cvParam(MS_highest_observed_m_z).valueAs<double>();        
    result.precursorScanNum = 0;
    result.precursorMZ = 0;
    result.precursorCharge = 0;
    result.precursorIntensity = 0;

    if (!spectrum->precursors.empty())
    {
        const Precursor& precursor = spectrum->precursors[0];
        result.collisionEnergy = precursor.activation.cvParam(MS_collision_energy).valueAs<double>();
        size_t precursorIndex = msd_.run.spectrumListPtr->find(precursor.spectrumID);

        if (precursorIndex < spectrumList.size())
        {
            SpectrumPtr precursorSpectrum = spectrumList.spectrum(precursorIndex);
            string precursorScanNumber = id::translateNativeIDToScanNumber(nativeIdFormat, precursorSpectrum->id);
            
            if (precursorScanNumber.empty()) // unsupported nativeID type
            {
                // assume scanNumber is a 1-based index, consistent with this->index() method
                result.precursorScanNum = precursorIndex+1;
            } 
            else 
            {
                result.precursorScanNum = lexical_cast<int>(precursorScanNumber);
            }
        }
        if (!precursor.selectedIons.empty())
        {
            result.precursorMZ = precursor.selectedIons[0].cvParam(MS_selected_ion_m_z).valueAs<double>();
            if (!result.precursorMZ)
            { // mzML 1.0?
                result.precursorMZ = precursor.selectedIons[0].cvParam(MS_m_z).valueAs<double>();
            }
            result.precursorCharge = precursor.selectedIons[0].cvParam(MS_charge_state).valueAs<int>();
            result.precursorIntensity = precursor.selectedIons[0].cvParam(MS_intensity).valueAs<double>();
        }
    }

    BOOST_STATIC_ASSERT(SCANTYPE_LENGTH > 4);
    memset(result.scanType, 0, SCANTYPE_LENGTH);
    strcpy(result.scanType, "Full"); // default
    if (spectrum->hasCVParam(MS_zoom_scan))
        strcpy(result.scanType, "Zoom");

    result.mergedScan = 0; // TODO 
    result.mergedResultScanNum = 0; // TODO 
    result.mergedResultStartScanNum = 0; // TODO 
    result.mergedResultEndScanNum = 0; // TODO 
    result.filePosition = spectrum->sourceFilePosition; 
}


void RAMPAdapter::Impl::getScanPeaks(size_t index, std::vector<double>& result) const
{
    SpectrumPtr spectrum = msd_.run.spectrumListPtr->spectrum(index, true);

    result.clear();
    result.resize(spectrum->defaultArrayLength * 2);
    if (spectrum->defaultArrayLength == 0) return;

    spectrum->getMZIntensityPairs(reinterpret_cast<MZIntensityPair*>(&result[0]), 
                                  spectrum->defaultArrayLength);
}


void RAMPAdapter::Impl::getRunHeader(RunHeaderStruct& result) const
{
    const SpectrumList& spectrumList = *msd_.run.spectrumListPtr;
    result.scanCount = static_cast<int>(size_);

    result.lowMZ = 0; // TODO
    result.highMZ = 0; // TODO
    result.startMZ = 0; // TODO
    result.endMZ = 0; // TODO

    if (size_ == 0) return;

    Scan dummy;

    SpectrumPtr firstSpectrum = spectrumList.spectrum(firstIndex_, false);
    Scan& firstScan = firstSpectrum->scanList.scans.empty() ? dummy : firstSpectrum->scanList.scans[0];
    result.dStartTime = retentionTime(firstScan);

    SpectrumPtr lastSpectrum = spectrumList.spectrum(lastIndex_, false);
    Scan& lastScan = lastSpectrum->scanList.scans.empty() ? dummy : lastSpectrum->scanList.scans[0];
    result.dEndTime = retentionTime(lastScan);
}


namespace {
inline void copyInstrumentString(char* to, const string& from)
{
    strncpy(to, from.substr(0,INSTRUMENT_LENGTH-1).c_str(), INSTRUMENT_LENGTH);
}
} // namespace


void RAMPAdapter::Impl::getInstrument(InstrumentStruct& result) const
{
    const InstrumentConfiguration& instrumentConfiguration = 
        (!msd_.instrumentConfigurationPtrs.empty() && msd_.instrumentConfigurationPtrs[0].get()) ?
        *msd_.instrumentConfigurationPtrs[0] :
        InstrumentConfiguration(); // temporary bound to const reference 

    // this const_cast is ok since we're only calling const functions,
    // but we wish C++ had "const constructors"
    const LegacyAdapter_Instrument adapter(const_cast<InstrumentConfiguration&>(instrumentConfiguration), cvTranslator_); 

    copyInstrumentString(result.manufacturer, adapter.manufacturer());
    copyInstrumentString(result.model, adapter.model());
    copyInstrumentString(result.ionisation, adapter.ionisation());
    copyInstrumentString(result.analyzer, adapter.analyzer());
    copyInstrumentString(result.detector, adapter.detector());
}


//
// RAMPAdapter
//


PWIZ_API_DECL RAMPAdapter::RAMPAdapter(const std::string& filename) : impl_(new Impl(filename)) {}
PWIZ_API_DECL size_t RAMPAdapter::scanCount() const {return impl_->scanCount();}
PWIZ_API_DECL size_t RAMPAdapter::index(int scanNumber) const {return impl_->index(scanNumber);}
PWIZ_API_DECL void RAMPAdapter::getScanHeader(size_t index, ScanHeaderStruct& result) const {impl_->getScanHeader(index, result);}
PWIZ_API_DECL void RAMPAdapter::getScanPeaks(size_t index, std::vector<double>& result) const {impl_->getScanPeaks(index, result);}
PWIZ_API_DECL void RAMPAdapter::getRunHeader(RunHeaderStruct& result) const {impl_->getRunHeader(result);}
PWIZ_API_DECL void RAMPAdapter::getInstrument(InstrumentStruct& result) const {impl_->getInstrument(result);}


} // namespace msdata
} // namespace pwiz


