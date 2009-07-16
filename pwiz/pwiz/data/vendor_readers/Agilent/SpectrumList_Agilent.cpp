//
// SpectrumList_Agilent.cpp
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


#include "SpectrumList_Agilent.hpp"


#ifdef PWIZ_READER_AGILENT
#include "Reader_Agilent_Detail.hpp"
#include "pwiz/utility/misc/SHA1Calculator.hpp"
#include "pwiz/utility/misc/String.hpp"
#include "pwiz/utility/misc/Stream.hpp"
#include "pwiz/utility/misc/Filesystem.hpp"
#include "boost/shared_ptr.hpp"
#include <boost/bind.hpp>


using boost::format;


namespace pwiz {
namespace msdata {
namespace detail {


SpectrumList_Agilent::SpectrumList_Agilent(MassHunterDataPtr rawfile)
:   rawfile_(rawfile),
    size_(0),
    indexInitialized_(BOOST_ONCE_INIT)
{
}


PWIZ_API_DECL size_t SpectrumList_Agilent::size() const
{
    boost::call_once(indexInitialized_, boost::bind(&SpectrumList_Agilent::createIndex, this));
    return size_;
}


PWIZ_API_DECL const SpectrumIdentity& SpectrumList_Agilent::spectrumIdentity(size_t index) const
{
    boost::call_once(indexInitialized_, boost::bind(&SpectrumList_Agilent::createIndex, this));
    if (index>size_)
        throw runtime_error(("[SpectrumList_Agilent::spectrumIdentity] Bad index: " 
                            + lexical_cast<string>(index)).c_str());
    return index_[index];
}


PWIZ_API_DECL size_t SpectrumList_Agilent::find(const string& id) const
{
    boost::call_once(indexInitialized_, boost::bind(&SpectrumList_Agilent::createIndex, this));
    try
    {
        size_t scanNumber = lexical_cast<size_t>(id);
        if (scanNumber>=1 && scanNumber<=size())
            return scanNumber-1;
    }
    catch (bad_lexical_cast&)
    {
        try
        {
            size_t scanNumber = lexical_cast<size_t>(id::value(id, "scan"));
            if (scanNumber>=1 && scanNumber<=size())
                return scanNumber-1;
        }
        catch (bad_lexical_cast&) {}
    }

    return size();
}


PWIZ_API_DECL SpectrumPtr SpectrumList_Agilent::spectrum(size_t index, bool getBinaryData) const
{
    return spectrum(index, getBinaryData, pwiz::util::IntegerSet());
}


PWIZ_API_DECL SpectrumPtr SpectrumList_Agilent::spectrum(size_t index, bool getBinaryData, const pwiz::util::IntegerSet& msLevelsToCentroid) const 
{ 
    boost::call_once(indexInitialized_, boost::bind(&SpectrumList_Agilent::createIndex, this));
    if (index>size_)
        throw runtime_error(("[SpectrumList_Agilent::spectrum] Bad index: " 
                            + lexical_cast<string>(index)).c_str());

    IndexEntry& ie = index_[index];

    // allocate a new Spectrum
    SpectrumPtr result(new Spectrum);
    if (!result.get())
        throw runtime_error("[SpectrumList_Agilent::spectrum] Allocation error.");

    result->index = index;
    result->id = ie.id;

    pwiz::vendor_api::Agilent::SpectrumPtr spectrumPtr = rawfile_->getProfileSpectrumByRow(ie.rowNumber);
    MSScanType scanType = spectrumPtr->getMSScanType();
    DeviceType deviceType = spectrumPtr->getDeviceType();

    result->scanList.set(MS_no_combination);
    result->scanList.scans.push_back(Scan());
    Scan& scan = result->scanList.scans[0];
    scan.set(MS_scan_start_time, rawfile_->getTicTimes()[ie.rowNumber], UO_minute);
    scan.set(translateAsPolarityType(spectrumPtr->getIonPolarity()));

    int msLevel = translateAsMSLevel(scanType);
    if (msLevel == -1) // precursor ion scan
        result->set(MS_precursor_ion_spectrum);
    else
    {
        result->set(MS_ms_level, msLevel);
        result->set(translateAsSpectrumType(scanType));
    }

    // MHDAC doesn't support centroiding of non-TOF spectra
    bool canCentroid = deviceType != DeviceType_Quadrupole &&
                       deviceType != DeviceType_TandemQuadrupole;

    bool doCentroid = msLevelsToCentroid.contains(msLevel);
    MSStorageMode storageMode = spectrumPtr->getMSStorageMode();
    bool hasProfile = storageMode == MSStorageMode_ProfileSpectrum ||
                      storageMode == MSStorageMode_Mixed;

    if (hasProfile && (!canCentroid || !doCentroid))
    {
        result->set(MS_profile_spectrum);
    }
    else
    {
        result->set(MS_centroid_spectrum); 
        doCentroid = hasProfile && canCentroid;
    }

    //result->set(MS_base_peak_m_z, scanInfo->basePeakMass(), MS_m_z);
    result->set(MS_base_peak_intensity, rawfile_->getBpcIntensities()[ie.rowNumber], MS_number_of_counts);
    result->set(MS_total_ion_current, rawfile_->getTicIntensities()[ie.rowNumber], MS_number_of_counts);

    MassRange minMaxMz = spectrumPtr->getMeasuredMassRange();
    scan.scanWindows.push_back(ScanWindow(minMaxMz.start, minMaxMz.end, MS_m_z));

    vector<double> precursorMZs;
    spectrumPtr->getPrecursorIons(precursorMZs);
    if (!precursorMZs.empty())
    {
        if (precursorMZs.size() > 1)
            throw runtime_error("[SpectrumList_Agilent::spectrum] Cannot handle more than one precursor.");

        Precursor precursor;
        Product product;
        SelectedIon selectedIon;

        // isolationWindow

        if (msLevel == -1) // precursor ion scan
        {
            product.isolationWindow.set(MS_isolation_window_target_m_z, precursorMZs[0], MS_m_z);
            //product.isolationWindow.set(MS_isolation_window_lower_offset, isolationWidth/2, MS_m_z);
            //product.isolationWindow.set(MS_isolation_window_upper_offset, isolationWidth/2, MS_m_z);
        }
        else
        {
            precursor.isolationWindow.set(MS_isolation_window_target_m_z, precursorMZs[0], MS_m_z);
            //precursor.isolationWindow.set(MS_isolation_window_lower_offset, isolationWidth/2, MS_m_z);
            //precursor.isolationWindow.set(MS_isolation_window_upper_offset, isolationWidth/2, MS_m_z);
            
            int parentScanId = spectrumPtr->getParentScanId();
            if (parentScanId > 0)
                precursor.spectrumID = "scanId=" + lexical_cast<string>(parentScanId);

            selectedIon.set(MS_selected_ion_m_z, precursorMZs[0], MS_m_z);
            precursor.selectedIons.push_back(selectedIon);
        }

        int precursorCharge;
        if (spectrumPtr->getPrecursorCharge(precursorCharge))
            selectedIon.set(MS_charge_state, precursorCharge);

        double precursorIntensity;
        if (spectrumPtr->getPrecursorIntensity(precursorIntensity))
            selectedIon.set(MS_intensity, precursorIntensity, MS_number_of_counts);

        precursor.activation.set(MS_CID); // MSDR provides no access to this, so assume CID
        precursor.activation.set(MS_collision_energy, spectrumPtr->getCollisionEnergy(), UO_electronvolt);

        result->precursors.push_back(precursor);
        if (msLevel == -1)
            result->products.push_back(product);
    }

    /*if (massList->size() > 0)
    {
        result->set(MS_lowest_observed_m_z, massList->data()[0].mass, MS_m_z);
        result->set(MS_highest_observed_m_z, massList->data()[massList->size()-1].mass, MS_m_z);
    }*/

    // if a centroided spectrum is desired, we make a new call
    if (doCentroid)
    {
        spectrumPtr = rawfile_->getPeakSpectrumByRow(ie.rowNumber);
    }

    if (getBinaryData)
    {
        result->setMZIntensityArrays(vector<double>(), vector<double>(), MS_number_of_counts);
 
        automation_vector<double> xArray;
        spectrumPtr->getXArray(xArray);
        result->getMZArray()->data.assign(xArray.begin(), xArray.end());

        automation_vector<float> yArray;
        spectrumPtr->getYArray(yArray);
        result->getIntensityArray()->data.assign(yArray.begin(), yArray.end());

        result->defaultArrayLength = xArray.size();
    }
    else
        result->defaultArrayLength = (size_t) spectrumPtr->getTotalDataPoints();

    return result;
}


PWIZ_API_DECL void SpectrumList_Agilent::createIndex() const
{
    MSScanType scanTypes = rawfile_->getScanTypes();

    // if any of these types are present, we enumerate each spectrum
    if (scanTypes & MSScanType_Scan ||
        scanTypes & MSScanType_ProductIon ||
        scanTypes & MSScanType_PrecursorIon)
    {
        int size = rawfile_->getTotalScansPresent();
        index_.reserve(size);

        for (size_t i=0, end = (size_t) size; i < end; ++i)
        {
            pwiz::vendor_api::Agilent::SpectrumPtr spectrumPtr = rawfile_->getProfileSpectrumByRow(i);
            MSScanType scanType = spectrumPtr->getMSScanType();

            // these spectra are chromatogram-centric
            if (scanType == MSScanType_SelectedIon ||
                scanType == MSScanType_TotalIon ||
                scanType == MSScanType_MultipleReaction)
                continue;

            index_.push_back(IndexEntry());
            IndexEntry& ie = index_.back();
            ie.rowNumber = (int) i;
            ie.scanId = spectrumPtr->getScanId();
            ie.index = index_.size()-1;

            ostringstream oss;
            oss << "scanId=" << ie.scanId;
            ie.id = oss.str();
        }
    }

    size_ = index_.size();
}


} // detail
} // msdata
} // pwiz


#else // PWIZ_READER_AGILENT

//
// non-MSVC implementation
//

namespace pwiz {
namespace msdata {
namespace detail {

namespace {const SpectrumIdentity emptyIdentity;}

SpectrumList_Agilent::SpectrumList_Agilent(MassHunterDataPtr rawfile) {}
size_t SpectrumList_Agilent::size() const {return 0;}
const SpectrumIdentity& SpectrumList_Agilent::spectrumIdentity(size_t index) const {return emptyIdentity;}
size_t SpectrumList_Agilent::find(const std::string& id) const {return 0;}
SpectrumPtr SpectrumList_Agilent::spectrum(size_t index, bool getBinaryData) const {return SpectrumPtr();}
SpectrumPtr SpectrumList_Agilent::spectrum(size_t index, bool getBinaryData, const pwiz::util::IntegerSet& msLevelsToCentroid) const {return SpectrumPtr();}
void SpectrumList_Agilent::createIndex() const {}

} // detail
} // msdata
} // pwiz

#endif // PWIZ_READER_AGILENT
