//
// Reader_Thermo.cpp
//
//
// Original author: Darren Kessner <Darren.Kessner@cshs.org>
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


#include "Reader_Thermo.hpp"


namespace {
// helper function used by both forms (real and stubbed) of Reader_Thermo
bool _hasRAWHeader(const std::string& head)
{
    const char rawHeader[] =
    {
        '\x01', '\xA1', 
        'F', '\0', 'i', '\0', 'n', '\0', 'n', '\0', 
        'i', '\0', 'g', '\0', 'a', '\0', 'n', '\0'
    };

    for (size_t i=0; i<sizeof(rawHeader); i++)
        if (head[i] != rawHeader[i]) 
            return false;

    return true;
}
} // namespace


#ifndef PWIZ_NO_READER_RAW
#include "data/msdata/CVTranslator.hpp"
#include "utility/vendor_api/thermo/RawFile.h"
#include "utility/misc/SHA1Calculator.hpp"
#include "boost/shared_ptr.hpp"
#include "boost/lexical_cast.hpp"
#include "boost/algorithm/string.hpp"
#include "boost/filesystem/path.hpp"
#include <iostream>
#include <stdexcept>


namespace pwiz {
namespace msdata {


using namespace std;
using boost::shared_ptr;
using boost::lexical_cast;
using boost::bad_lexical_cast;
using namespace pwiz::raw;
using namespace pwiz::util;
namespace bfs = boost::filesystem;


//
// SpectrumList_Thermo
//


namespace {


class SpectrumList_Thermo : public SpectrumList
{
    public:

    SpectrumList_Thermo(const MSData& msd, shared_ptr<RawFile> rawfile);
    virtual size_t size() const;
    virtual const SpectrumIdentity& spectrumIdentity(size_t index) const;
    virtual size_t find(const string& id) const;
    virtual size_t findNative(const string& nativeID) const;
    virtual SpectrumPtr spectrum(size_t index, bool getBinaryData) const;

    private:

    const MSData& msd_;
    shared_ptr<RawFile> rawfile_;
    size_t size_;
    mutable vector<SpectrumPtr> spectrumCache_;
    vector<SpectrumIdentity> index_;

    void createIndex();
    string findPrecursorID(int precursorMsLevel, size_t index) const;
};


SpectrumList_Thermo::SpectrumList_Thermo(const MSData& msd, shared_ptr<RawFile> rawfile)
:   msd_(msd), rawfile_(rawfile),
    size_(rawfile->value(NumSpectra)),
    spectrumCache_(size_), index_(size_)
{
    createIndex();
}


size_t SpectrumList_Thermo::size() const
{
    return size_;
}


const SpectrumIdentity& SpectrumList_Thermo::spectrumIdentity(size_t index) const
{
    if (index>size_)
        throw runtime_error(("[SpectrumList_Thermo::spectrumIdentity()] Bad index: " 
                            + lexical_cast<string>(index)).c_str());
    return index_[index];
}


size_t SpectrumList_Thermo::find(const string& id) const
{
    try
    {
        size_t scanNumber = lexical_cast<size_t>(id);
        if (scanNumber>=1 && scanNumber<=size()) 
            return scanNumber-1;
    }
    catch (bad_lexical_cast&) {}

    return size();
}


size_t SpectrumList_Thermo::findNative(const string& nativeID) const
{
    return find(nativeID);
}


CVParam translateAsScanningMethod(ScanType scanType)
{
    switch (scanType)
    {
        case ScanType_Full:
            return MS_full_scan;
        case ScanType_Zoom:
            return MS_zoom_scan;
        case ScanType_SIM:
            return MS_SIM;
        case ScanType_SRM:
            return MS_SRM;
        case ScanType_CRM:
            return MS_CRM;
        case ScanType_Unknown:
        default:
            return CVParam();
    }
}


CVParam translateAsSpectrumType(ScanType scanType)
{
    switch (scanType)
    {
        case ScanType_Full:
        case ScanType_Zoom:
            return MS_MSn_spectrum;
        case ScanType_SIM:
            return MS_SIM_spectrum;
        case ScanType_SRM:
            return MS_SRM_spectrum;
        case ScanType_CRM:
            return MS_CRM_spectrum;
        case ScanType_Unknown:
        default:
            return CVParam();
    }
}


CVParam translate(MassAnalyzerType type)
{
    switch (type)
    {
        case MassAnalyzerType_ITMS: return MS_ion_trap;
        case MassAnalyzerType_FTMS: return MS_FT_ICR;
        case MassAnalyzerType_TOFMS: return MS_time_of_flight;
        case MassAnalyzerType_TQMS: return MS_quadrupole;
        case MassAnalyzerType_SQMS: return MS_quadrupole;
        case MassAnalyzerType_Sector: return MS_magnetic_sector;
        case MassAnalyzerType_Unknown:
        default:
            return CVParam();
    }
}


CVParam translateAsIonizationType(IonizationType ionizationType)
{
    switch (ionizationType)
    {
        case IonizationType_EI: return MS_electron_ionization;
        case IonizationType_CI: return MS_chemical_ionization;
        case IonizationType_FAB: return MS_fast_atom_bombardment_ionization;
        case IonizationType_ESI: return MS_electrospray_ionization;
        case IonizationType_NSI: return MS_nanoelectrospray;
        case IonizationType_APCI: return MS_atmospheric_pressure_chemical_ionization;
        //case IonizationType_TSP: return MS_thermospray_ionization;
        case IonizationType_FD: return MS_field_desorption;
        case IonizationType_MALDI: return MS_matrix_assisted_laser_desorption_ionization;
        case IonizationType_GD: return MS_glow_discharge_ionization;
        case IonizationType_Unknown:
        default:
            return CVParam();
    }
}

    
CVParam translateAsInletType(IonizationType ionizationType)
{
    switch (ionizationType)
    {
        //case IonizationType_EI: return MS_electron_ionization;
        //case IonizationType_CI: return MS_chemical_ionization;
        case IonizationType_FAB: return MS_continuous_flow_fast_atom_bombardment;
        case IonizationType_ESI: return MS_electrospray_inlet;
        case IonizationType_NSI: return MS_nanospray_inlet;
        //case IonizationType_APCI: return MS_atmospheric_pressure_chemical_ionization;
        case IonizationType_TSP: return MS_thermospray_inlet;
        //case IonizationType_FD: return MS_field_desorption;
        //case IonizationType_MALDI: return MS_matrix_assisted_laser_desorption_ionization;
        //case IonizationType_GD: return MS_glow_discharge_ionization;
        case IonizationType_Unknown:
        default:
            return CVParam();
    }
}


CVParam translate(PolarityType polarityType)
{
    switch (polarityType)
    {
        case PolarityType_Positive:
            return MS_positive_scan;
        case PolarityType_Negative:
            return MS_negative_scan;
        case PolarityType_Unknown:
        default:
            return CVParam();
    }
}


SpectrumPtr SpectrumList_Thermo::spectrum(size_t index, bool getBinaryData) const 
{ 
    if (index>size_)
        throw runtime_error(("[SpectrumList_Thermo::spectrum()] Bad index: " 
                            + lexical_cast<string>(index)).c_str());

    // returned cached Spectrum if possible

    if (!getBinaryData && spectrumCache_[index].get())
        return spectrumCache_[index];

    // allocate a new Spectrum

    SpectrumPtr result(new Spectrum);
    if (!result.get())
        throw runtime_error("[SpectrumList_Thermo::spectrum()] Allocation error.");

    // get rawfile::ScanInfo and translate

    long scanNumber = static_cast<int>(index) + 1;
    auto_ptr<ScanInfo> scanInfo = rawfile_->getScanInfo(scanNumber);
    if (!scanInfo.get())
        throw runtime_error("[SpectrumList_Thermo::spectrum()] Error retrieving ScanInfo.");

    result->index = index;
    result->id = result->nativeID = lexical_cast<string>(scanNumber);

    SpectrumDescription& sd = result->spectrumDescription;
    Scan& scan = sd.scan;

    if (msd_.instrumentPtrs.empty())
        throw runtime_error("[SpectrumList_Thermo::spectrum()] No instruments defined.");
    scan.instrumentPtr = msd_.instrumentPtrs[0];

    string filterString = scanInfo->filter();

    scan.cvParams.push_back(CVParam(MS_filter_string, filterString));

    string scanEvent = scanInfo->trailerExtraValue("Scan Event:");
    scan.cvParams.push_back(CVParam(MS_preset_scan_configuration, scanEvent));

    /* currently non-standard for mzML
    if (scanInfo->massAnalyzerType_ > MassAnalyzerType_Unknown)
        scan.cvParams.push_back(translate(scanInfo->massAnalyzerType_));*/
     
    result->set(MS_ms_level, scanInfo->msLevel());

    ScanType scanType = scanInfo->scanType();
    if (scanType!=ScanType_Unknown)
    {
        result->cvParams.push_back(translateAsSpectrumType(scanType));
        scan.cvParams.push_back(translateAsScanningMethod(scanType));
    }

    PolarityType polarityType = scanInfo->polarityType();
    if (polarityType!=PolarityType_Unknown) scan.cvParams.push_back(translate(polarityType));

    if (scanInfo->isProfileScan()) sd.cvParams.push_back(MS_profile_mass_spectrum); 
    else if (scanInfo->isCentroidScan()) sd.cvParams.push_back(MS_centroid_mass_spectrum); 

    scan.cvParams.push_back(CVParam(MS_scan_time, scanInfo->startTime(), MS_minute));
    sd.cvParams.push_back(CVParam(MS_lowest_m_z_value, scanInfo->lowMass()));
    sd.cvParams.push_back(CVParam(MS_highest_m_z_value, scanInfo->highMass()));
    sd.cvParams.push_back(CVParam(MS_base_peak_m_z, scanInfo->basePeakMass()));
    sd.cvParams.push_back(CVParam(MS_base_peak_intensity, scanInfo->basePeakIntensity()));
    sd.cvParams.push_back(CVParam(MS_total_ion_current, scanInfo->totalIonCurrent()));

    for (long i=0, precursorCount=scanInfo->parentCount(); i<precursorCount; i++)
    {
        // Note: we report what RawFile gives us, which comes from the filter string;
        // we can look in the trailer extra values for better (but still unreliable) 
        // info.  Precursor recalculation should be done outside the Reader.

        Precursor precursor;

        // TODO: better test here for data dependent modes
        if ((scanType==ScanType_Full || scanType==ScanType_Zoom ) && scanInfo->msLevel() > 1)
            precursor.spectrumID = findPrecursorID(scanInfo->msLevel()-1, index);

        precursor.ionSelection.cvParams.push_back(CVParam(MS_m_z, scanInfo->parentMass(i)));
        // TODO: determine precursor intensity? (parentEnergy is not precursor intensity!)
        precursor.activation.cvParams.push_back(CVParam(MS_collision_energy, scanInfo->parentEnergy(i)));
        sd.precursors.push_back(precursor); 
    }

    if (getBinaryData)
    {
        auto_ptr<raw::MassList> massList = 
            rawfile_->getMassList(scanNumber, "", raw::Cutoff_None, 0, 0, false);

        result->setMZIntensityPairs(reinterpret_cast<MZIntensityPair*>(massList->data()), 
                                    massList->size());
    }

    // save to cache if no binary data

    if (!getBinaryData && !spectrumCache_[index].get())
        spectrumCache_[index] = result; 

    return result;
}


void SpectrumList_Thermo::createIndex()
{
    for (size_t i=0; i<size_; i++)
    {
        SpectrumIdentity& si = index_[i];
        si.index = i;
        si.id = si.nativeID = lexical_cast<string>(i+1);
    }
}


string SpectrumList_Thermo::findPrecursorID(int precursorMsLevel, size_t index) const
{
    // for MSn spectra (n > 1): return first scan with MSn-1

    while (index>0)
    {
	    --index;
	    SpectrumPtr candidate = spectrum(index, false);
	    if (candidate->cvParam(MS_ms_level).valueAs<int>() == precursorMsLevel)
		    return candidate->id;
    }

    return "";
}


} // namespace


//
// Reader_Thermo
//


bool Reader_Thermo::hasRAWHeader(const string& head)
{
    return _hasRAWHeader(head);
}

namespace {

auto_ptr<RawFileLibrary> rawFileLibrary_;

void fillInstrumentComponentMetadata(RawFile& rawfile, MSData& msd, InstrumentPtr& instrument)
{
    auto_ptr<ScanInfo> firstScanInfo = rawfile.getScanInfo(1);

    instrument->componentList.source.order = 1;
    instrument->componentList.source.cvParams.push_back(translateAsIonizationType(firstScanInfo->ionizationType()));
    if (translateAsInletType(firstScanInfo->ionizationType()).cvid != CVID_Unknown)
        instrument->componentList.source.cvParams.push_back(translateAsInletType(firstScanInfo->ionizationType()));

    // due diligence to determine the mass analyzer(s) (TODO: also try to get a quantative resolution estimate)
    instrument->componentList.analyzer.order = 2;
    string model = boost::to_lower_copy( rawfile.value(InstName) + rawfile.value(InstModel) );
    if (model.find("ltq") != string::npos)
        instrument->componentList.analyzer.cvParams.push_back(CVParam(MS_ion_trap));
    if (model.find("ft") != string::npos)
        instrument->componentList.analyzer.cvParams.push_back(CVParam(MS_FT_ICR));
    if (model.find("orbitrap") != string::npos)
        instrument->componentList.analyzer.cvParams.push_back(CVParam(MS_orbitrap));
    if (model.find("tsq") != string::npos || model.find("quantum") != string::npos)
        instrument->componentList.analyzer.cvParams.push_back(CVParam(MS_quadrupole));
    if (model.find("tof") != string::npos)
        instrument->componentList.analyzer.cvParams.push_back(CVParam(MS_time_of_flight));
    if (model.find("sector") != string::npos)
        instrument->componentList.analyzer.cvParams.push_back(CVParam(MS_magnetic_sector));

    instrument->componentList.detector.order = 3;
    // TODO: verify that all Thermo instruments use EM
    instrument->componentList.detector.cvParams.push_back(CVParam(MS_electron_multiplier));
}

void fillInMetadata(const string& filename, RawFile& rawfile, MSData& msd)
{
    msd.cvs.resize(1);
    CV& cv = msd.cvs.front();
    cv.URI = "psi-ms.obo"; 
    cv.cvLabel = "MS";
    cv.fullName = "Proteomics Standards Initiative Mass Spectrometry Ontology";
    cv.version = "1.0";

    msd.fileDescription.fileContent.cvParams.push_back(translateAsSpectrumType(rawfile.getScanInfo(1)->scanType()));

    SourceFilePtr sourceFile(new SourceFile);
    bfs::path p(filename);
    sourceFile->id = "rawfile";
    sourceFile->name = p.leaf();
    sourceFile->location = p.branch_path().string();
    sourceFile->cvParams.push_back(MS_Xcalibur_RAW_file);
    string sha1 = SHA1Calculator::hashFile(filename);
    sourceFile->cvParams.push_back(CVParam(MS_SHA_1, sha1));
    msd.fileDescription.sourceFilePtrs.push_back(sourceFile);

    SoftwarePtr softwareXcalibur(new Software);
    softwareXcalibur->id = "Xcalibur";
    softwareXcalibur->softwareParam = MS_Xcalibur;
    softwareXcalibur->softwareParamVersion = rawfile.value(InstSoftwareVersion);
    msd.softwarePtrs.push_back(softwareXcalibur);

    SoftwarePtr softwarePwiz(new Software);
    softwarePwiz->id = "pwiz::msdata::Reader_Thermo";
    softwarePwiz->softwareParam = MS_pwiz;
    softwarePwiz->softwareParamVersion = "1.0"; 
    msd.softwarePtrs.push_back(softwarePwiz);

    DataProcessingPtr dpPwiz(new DataProcessing);
    dpPwiz->id = "pwiz::msdata::Reader_Thermo conversion";
    dpPwiz->softwarePtr = softwarePwiz;
    dpPwiz->processingMethods.push_back(ProcessingMethod());
    dpPwiz->processingMethods.back().cvParams.push_back(MS_Conversion_to_mzML);
    msd.dataProcessingPtrs.push_back(dpPwiz);

    CVTranslator cvTranslator;

    InstrumentPtr instrument(new Instrument);
    string model = rawfile.value(InstModel);
    CVID cvidModel = cvTranslator.translate(model);
    if (cvidModel != CVID_Unknown) 
    {
        instrument->cvParams.push_back(cvidModel);
        instrument->id = cvinfo(cvidModel).name;
    }
    else
    {
        instrument->userParams.push_back(UserParam("instrument model", model));
        instrument->id = model;
    }
    instrument->cvParams.push_back(CVParam(MS_instrument_serial_number, 
                                           rawfile.value(InstSerialNumber)));
    instrument->softwarePtr = softwareXcalibur;
    fillInstrumentComponentMetadata(rawfile, msd, instrument);
    msd.instrumentPtrs.push_back(instrument);

    msd.run.id = filename;
    //msd.run.startTimeStamp = rawfile.getCreationDate(); // TODO format: 2007-06-27T15:23:45.00035
    msd.run.instrumentPtr = instrument;
}

} // namespace


bool Reader_Thermo::accept(const string& filename, const string& head) const
{
    return hasRAWHeader(head);
}


void Reader_Thermo::read(const string& filename, 
                      const string& head,
                      MSData& result) const
{
    // initialize RawFileLibrary

	if (!rawFileLibrary_.get()) {
		rawFileLibrary_.reset(new RawFileLibrary());
	}

    // instantiate RawFile, share ownership with SpectrumList_Thermo

    shared_ptr<RawFile> rawfile(RawFile::create(filename).release());
    rawfile->setCurrentController(Controller_MS, 1);
    result.run.spectrumListPtr = SpectrumListPtr(new SpectrumList_Thermo(result, rawfile));

    fillInMetadata(filename, *rawfile, result);
}


} // namespace msdata
} // namespace pwiz


#else // PWIZ_NO_READER_RAW /////////////////////////////////////////////////////////////////////////////

//
// non-MSVC implementation
//

#include "Reader_Thermo.hpp"
#include <stdexcept>

namespace pwiz {
namespace msdata {

using namespace std;

bool Reader_Thermo::accept(const string& filename, const string& head) const
{
    return false;
}

void Reader_Thermo::read(const string& filename, const string& head, MSData& result) const
{
    throw runtime_error("[Reader_Thermo::read()] Not implemented."); 
}

bool Reader_Thermo::hasRAWHeader(const string& head)
{   
    return _hasRAWHeader(head);
}

} // namespace msdata
} // namespace pwiz

#endif // PWIZ_NO_READER_RAW /////////////////////////////////////////////////////////////////////////////

