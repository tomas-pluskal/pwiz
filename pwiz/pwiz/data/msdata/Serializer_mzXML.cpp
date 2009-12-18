//
// $Id$
//
//
// Original author: Darren Kessner <darren@proteowizard.org>
//
// Copyright 2007 Spielberg Family Center for Applied Proteomics
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

#include "Serializer_mzXML.hpp"
#include "SpectrumList_mzXML.hpp"
#include "Diff.hpp"
#include "SHA1OutputObserver.hpp"
#include "LegacyAdapter.hpp"
#include "CVTranslator.hpp"
#include "pwiz/utility/minimxml/XMLWriter.hpp"
#include "pwiz/utility/minimxml/SAXParser.hpp"
#include "pwiz/utility/misc/Filesystem.hpp"
#include "pwiz/utility/misc/String.hpp"
#include <stdexcept>


namespace pwiz {
namespace msdata {


using namespace std;
using minimxml::XMLWriter;
using boost::shared_ptr;
using boost::lexical_cast;
using boost::iostreams::stream_offset;
using namespace pwiz::util;
using namespace pwiz::minimxml;


class Serializer_mzXML::Impl
{
    public:

    Impl(const Config& config)
    :   config_(config)
    {}

    void write(ostream& os, const MSData& msd,
               const pwiz::util::IterationListenerRegistry* iterationListenerRegistry) const;

    void read(shared_ptr<istream> is, MSData& msd) const;

    private:
    Config config_; 
    CVTranslator cvTranslator_;
};


//
// write() implementation
//


namespace {


void start_mzXML(XMLWriter& xmlWriter)
{
    XMLWriter::Attributes attributes; 
    attributes.push_back(make_pair("xmlns", 
        "http://sashimi.sourceforge.net/schema_revision/mzXML_3.1"));
    attributes.push_back(make_pair("xmlns:xsi", 
        "http://www.w3.org/2001/XMLSchema-instance"));
    attributes.push_back(make_pair("xsi:schemaLocation", 
        "http://sashimi.sourceforge.net/schema_revision/mzXML_3.1 http://sashimi.sourceforge.net/schema_revision/mzXML_3.1/mzXML_idx_3.1.xsd"));

    xmlWriter.pushStyle(XMLWriter::StyleFlag_AttributesOnMultipleLines);
    xmlWriter.startElement("mzXML", attributes);
    xmlWriter.popStyle();
}


string getRetentionTime(const Scan& scan)
{
    ostringstream oss;
    oss << "PT" << scan.cvParam(MS_scan_start_time).timeInSeconds() << "S";
    return oss.str();
}


void start_msRun(XMLWriter& xmlWriter, const MSData& msd)
{
    string scanCount, startTime, endTime;

    if (msd.run.spectrumListPtr.get())
    {
        const SpectrumList& sl = *msd.run.spectrumListPtr;
        scanCount = lexical_cast<string>(sl.size());

        if (!sl.empty())
        {
            SpectrumPtr spectrum = sl.spectrum(0);
            if (!spectrum->scanList.scans.empty())
                startTime = getRetentionTime(spectrum->scanList.scans[0]);

            spectrum = sl.spectrum(sl.size()-1);
            if (!spectrum->scanList.scans.empty())
                endTime = getRetentionTime(spectrum->scanList.scans[0]);
        }
    }

    XMLWriter::Attributes attributes; 
    attributes.push_back(make_pair("scanCount", scanCount));
    attributes.push_back(make_pair("startTime", startTime));
    attributes.push_back(make_pair("endTime", endTime));
    xmlWriter.startElement("msRun", attributes);
}


void write_parentFile(XMLWriter& xmlWriter, const MSData& msd)
{
    SourceFilePtr sourceFilePtr = msd.run.defaultSourceFilePtr;
    if (!sourceFilePtr.get() && !msd.fileDescription.empty())
        sourceFilePtr = msd.fileDescription.sourceFilePtrs[0];

    if (!sourceFilePtr.get())
        return;

    const SourceFile& sf = *sourceFilePtr;

    CVID nativeIdFormat = sf.cvParamChild(MS_nativeID_format).cvid;
    if (nativeIdFormat == CVID_Unknown || nativeIdFormat == MS_no_nativeID_format)
        return;

    string fileName, fileType, fileSha1;

    fileName = sf.location + "/" + sf.name;
    switch (nativeIdFormat)
    {
        // nativeID formats from processed data file types
        case MS_scan_number_only_nativeID_format:
        case MS_spectrum_identifier_nativeID_format:
        case MS_multiple_peak_list_nativeID_format:
        case MS_single_peak_list_nativeID_format:
            fileType = "processedData";
            break;

        // consider other formats to be raw
        default:
            fileType = "RAWData";
            break;
    }
    fileSha1 = sf.cvParam(MS_SHA_1).value;

    XMLWriter::Attributes attributes;
    attributes.push_back(make_pair("fileName", fileName));
    attributes.push_back(make_pair("fileType", fileType));
    attributes.push_back(make_pair("fileSha1", fileSha1));
    xmlWriter.pushStyle(XMLWriter::StyleFlag_AttributesOnMultipleLines);
    xmlWriter.startElement("parentFile", attributes, XMLWriter::EmptyElement);
    xmlWriter.popStyle();
}


void writeCategoryValue(XMLWriter& xmlWriter, const string& category, const string& value)
{
    XMLWriter::Attributes attributes; 
    attributes.push_back(make_pair("category", category));
    attributes.push_back(make_pair("value", value));
    xmlWriter.startElement(category, attributes, XMLWriter::EmptyElement);
}


void writeSoftware(XMLWriter& xmlWriter, SoftwarePtr software, 
                   const MSData& msd, const CVTranslator& cvTranslator)
{
    LegacyAdapter_Software adapter(software, const_cast<MSData&>(msd), cvTranslator);
    XMLWriter::Attributes attributes; 

    attributes.push_back(make_pair("type", adapter.type()));
    attributes.push_back(make_pair("name", adapter.name()));
    attributes.push_back(make_pair("version", adapter.version()));

    xmlWriter.startElement("software", attributes, XMLWriter::EmptyElement);
}


void write_msInstrument(XMLWriter& xmlWriter, const InstrumentConfiguration& instrumentConfiguration, 
                        const MSData& msd, const CVTranslator& cvTranslator)
{
    const LegacyAdapter_Instrument adapter(
        const_cast<InstrumentConfiguration&>(instrumentConfiguration), cvTranslator);
    
    XMLWriter::Attributes attributes; 
    attributes.push_back(make_pair("id", instrumentConfiguration.id));
    xmlWriter.startElement("msInstrument", attributes);
        writeCategoryValue(xmlWriter, "msManufacturer", adapter.manufacturer());
        writeCategoryValue(xmlWriter, "msModel", adapter.model());
        try { writeCategoryValue(xmlWriter, "msIonisation", adapter.ionisation()); } catch (std::out_of_range&) {}
        try { writeCategoryValue(xmlWriter, "msMassAnalyzer", adapter.analyzer()); } catch (std::out_of_range&) {}
        try { writeCategoryValue(xmlWriter, "msDetector", adapter.detector()); } catch (std::out_of_range&) {}
    if (instrumentConfiguration.softwarePtr.get()) writeSoftware(xmlWriter, 
                                                    instrumentConfiguration.softwarePtr,
                                                    msd, cvTranslator);
    xmlWriter.endElement(); // msInstrument
}


void write_msInstruments(XMLWriter& xmlWriter, const MSData& msd,
                        const CVTranslator& cvTranslator)
{
    for (vector<InstrumentConfigurationPtr>::const_iterator it=msd.instrumentConfigurationPtrs.begin();
         it!=msd.instrumentConfigurationPtrs.end(); ++it)
        if (it->get()) write_msInstrument(xmlWriter, **it, msd, cvTranslator);
}


void write_dataProcessing(XMLWriter& xmlWriter, const MSData& msd, const CVTranslator& cvTranslator)
{
    xmlWriter.startElement("dataProcessing");

    for (vector<DataProcessingPtr>::const_iterator it=msd.dataProcessingPtrs.begin();
         it!=msd.dataProcessingPtrs.end(); ++it)
    {
        if (!it->get()) continue;

        for (vector<ProcessingMethod>::const_iterator jt=(*it)->processingMethods.begin();
             jt!=(*it)->processingMethods.end(); ++jt)

        if (jt->softwarePtr.get()) 
            writeSoftware(xmlWriter, jt->softwarePtr, msd, cvTranslator);
    }

    xmlWriter.endElement(); // dataProcessing
}


struct IndexEntry
{
    int scanNumber;
    stream_offset offset;
};


string getPolarity(const Spectrum& spectrum)
{
    string result = "";
    CVParam paramPolarity = spectrum.cvParamChild(MS_polarity);
    if (paramPolarity.cvid == MS_positive_scan) result = "+";
    if (paramPolarity.cvid == MS_negative_scan) result = "-";
    return result;
}


struct PrecursorInfo
{
    string scanNum;
    string mz;
    string intensity;
    string charge;
    string collisionEnergy;
    string activation;

    bool empty() const 
    {
        return scanNum.empty() && mz.empty() && intensity.empty() && 
               charge.empty() && collisionEnergy.empty() && activation.empty();
    }
};


vector<PrecursorInfo> getPrecursorInfo(const Spectrum& spectrum, 
                                       const SpectrumListPtr spectrumListPtr)
{
    vector<PrecursorInfo> result;

    for (vector<Precursor>::const_iterator it=spectrum.precursors.begin();
         it!=spectrum.precursors.end(); ++it)
    {
        PrecursorInfo info;
        if (!it->spectrumID.empty())
            info.scanNum = id::value(it->spectrumID, "scan");
        if (!it->selectedIons.empty())
        { 
            info.mz = it->selectedIons[0].cvParam(MS_selected_ion_m_z).value;
            info.intensity = it->selectedIons[0].cvParam(MS_peak_intensity).value;
            info.charge = it->selectedIons[0].cvParam(MS_charge_state).value;
        }
        if (!it->activation.empty())
        {
            switch (it->activation.cvParamChild(MS_dissociation_method).cvid)
            {
                case MS_CID: info.activation = "CID"; break;
                case MS_ETD: info.activation = "ETD"; break;
                case MS_ECD: info.activation = "ECD"; break;
                default: /* TODO: log an error, an exception would be too harsh here */
            }
            info.collisionEnergy = it->activation.cvParam(MS_collision_energy).value;
        }
        if (!info.empty()) result.push_back(info);
    }

    return result;
}


void write_precursors(XMLWriter& xmlWriter, const vector<PrecursorInfo>& precursorInfo)
{
    xmlWriter.pushStyle(XMLWriter::StyleFlag_InlineInner);

    for (vector<PrecursorInfo>::const_iterator it=precursorInfo.begin(); 
         it!=precursorInfo.end(); ++it)
    {    
        XMLWriter::Attributes attributes;
        if (!it->scanNum.empty())
            attributes.push_back(make_pair("precursorScanNum", it->scanNum));
        if (it->intensity.empty())
            attributes.push_back(make_pair("precursorIntensity", "0")); // required attribute
        else
            attributes.push_back(make_pair("precursorIntensity", it->intensity));
        if (!it->charge.empty())
            attributes.push_back(make_pair("precursorCharge", it->charge));
        xmlWriter.startElement("precursorMz", attributes);
        xmlWriter.characters(it->mz);
        xmlWriter.endElement();
    }

    xmlWriter.popStyle();
}


void write_peaks(XMLWriter& xmlWriter, const vector<MZIntensityPair>& mzIntensityPairs,
                 const Serializer_mzXML::Config& config)
{
    BinaryDataEncoder::Config bdeConfig = config.binaryDataEncoderConfig;
    bdeConfig.byteOrder = BinaryDataEncoder::ByteOrder_BigEndian; // mzXML always big endian

    BinaryDataEncoder encoder(bdeConfig);
    string encoded;
    size_t binaryByteCount; // size before base64 encoding

    if (!mzIntensityPairs.empty())
        encoder.encode(reinterpret_cast<const double*>(&mzIntensityPairs[0]), 
                       mzIntensityPairs.size()*2, encoded, &binaryByteCount);

    XMLWriter::Attributes attributes;
    string precision = bdeConfig.precision == BinaryDataEncoder::Precision_32 ? "32" : "64";
    if (bdeConfig.compression == BinaryDataEncoder::Compression_Zlib)
    {
        attributes.push_back(make_pair("compressionType", "zlib"));
        attributes.push_back(make_pair("compressedLen", lexical_cast<string>(binaryByteCount)));
    }
    else
        attributes.push_back(make_pair("compressedLen", "0"));

    attributes.push_back(make_pair("precision", precision));
    attributes.push_back(make_pair("byteOrder", "network"));
    attributes.push_back(make_pair("pairOrder", "m/z-int"));

    xmlWriter.pushStyle(XMLWriter::StyleFlag_InlineInner |
                        XMLWriter::StyleFlag_AttributesOnMultipleLines);
    xmlWriter.startElement("peaks", attributes);
    xmlWriter.characters(encoded);
    xmlWriter.endElement();
    xmlWriter.popStyle();
}


IndexEntry write_scan(XMLWriter& xmlWriter,
                      CVID nativeIdFormat,
                      const Spectrum& spectrum,
                      const SpectrumListPtr spectrumListPtr,
                      const Serializer_mzXML::Config& config)
{
    IndexEntry result;
    result.offset = xmlWriter.positionNext();

    // mzXML scanNumber takes a different form depending on the source's nativeID format
    string scanNumberStr = id::translateNativeIDToScanNumber(nativeIdFormat, spectrum.id);
    if (scanNumberStr.empty())
        result.scanNumber = spectrum.index+1; // scanNumber is a 1-based index for some nativeID formats
    else
        result.scanNumber = lexical_cast<int>(scanNumberStr);

    // get info

    Scan dummy;
    const Scan& scan = spectrum.scanList.scans.empty() ? dummy : spectrum.scanList.scans[0];

    CVParam spectrumTypeParam = spectrum.cvParamChild(MS_spectrum_type);

    string scanType;
    switch (spectrumTypeParam.cvid)
    {
        case MS_MSn_spectrum:
        case MS_MS1_spectrum:
            scanType = "FULL";
            break;

        case MS_CRM_spectrum: scanType = "CRM"; break;
        case MS_SIM_spectrum: scanType = "SIM"; break;
        case MS_SRM_spectrum: scanType = "SRM"; break;
        case MS_precursor_ion_spectrum: scanType = "Q1"; break;
        default: break;
    }

    string scanEvent = scan.cvParam(MS_preset_scan_configuration).value;
    string msLevel = spectrum.cvParam(MS_ms_level).value;
    string polarity = getPolarity(spectrum);
    string retentionTime = getRetentionTime(scan);
    string lowMz = spectrum.cvParam(MS_lowest_observed_m_z).value;
    string highMz = spectrum.cvParam(MS_highest_observed_m_z).value;
    string basePeakMz = spectrum.cvParam(MS_base_peak_m_z).value;
    string basePeakIntensity = spectrum.cvParam(MS_base_peak_intensity).value;
    string totIonCurrent = spectrum.cvParam(MS_total_ion_current).value;
    bool isCentroided = spectrum.hasCVParam(MS_centroid_spectrum);

    vector<PrecursorInfo> precursorInfo = getPrecursorInfo(spectrum, spectrumListPtr);

    vector<MZIntensityPair> mzIntensityPairs;
    spectrum.getMZIntensityPairs(mzIntensityPairs);

    // write out xml

    XMLWriter::Attributes attributes;
    attributes.push_back(make_pair("num", lexical_cast<string>(result.scanNumber)));
    if (!scanEvent.empty())
        attributes.push_back(make_pair("scanEvent", scanEvent));
    if (!scanType.empty())
        attributes.push_back(make_pair("scanType", scanType));
    if (isCentroided)
        attributes.push_back(make_pair("centroided", "1"));
    attributes.push_back(make_pair("msLevel", msLevel));
    attributes.push_back(make_pair("peaksCount", lexical_cast<string>(mzIntensityPairs.size())));
    if (!polarity.empty())
        attributes.push_back(make_pair("polarity", polarity));
    attributes.push_back(make_pair("retentionTime", retentionTime));
    if (!precursorInfo.empty())
    {
        if(!precursorInfo[0].collisionEnergy.empty())
            attributes.push_back(make_pair("collisionEnergy", precursorInfo[0].collisionEnergy));
        if(!precursorInfo[0].activation.empty())
            attributes.push_back(make_pair("activationMethod", precursorInfo[0].activation));
    }
    if (!lowMz.empty())
        attributes.push_back(make_pair("lowMz", lowMz));
    if (!highMz.empty())
        attributes.push_back(make_pair("highMz", highMz));
    if (!basePeakMz.empty())
        attributes.push_back(make_pair("basePeakMz", basePeakMz));
    if (!basePeakIntensity.empty())
        attributes.push_back(make_pair("basePeakIntensity", basePeakIntensity));
    if (!totIonCurrent.empty())
        attributes.push_back(make_pair("totIonCurrent", totIonCurrent));

    if (scan.instrumentConfigurationPtr.get())
        attributes.push_back(make_pair("msInstrumentID", scan.instrumentConfigurationPtr->id));

    xmlWriter.pushStyle(XMLWriter::StyleFlag_AttributesOnMultipleLines);
    xmlWriter.startElement("scan", attributes);
    xmlWriter.popStyle();

    write_precursors(xmlWriter, precursorInfo);
    write_peaks(xmlWriter, mzIntensityPairs, config);

    xmlWriter.endElement(); // scan

    return result;
}


void write_scans(XMLWriter& xmlWriter, const MSData& msd, 
                 const Serializer_mzXML::Config& config, vector<IndexEntry>& index,
                 const pwiz::util::IterationListenerRegistry* iterationListenerRegistry)
{
    SpectrumListPtr sl = msd.run.spectrumListPtr;
    if (!sl.get()) return;

    CVID defaultNativeIdFormat = id::getDefaultNativeIDFormat(msd);

    for (size_t i=0; i<sl->size(); i++)
    {
        // send progress updates, handling cancel

        IterationListener::Status status = IterationListener::Status_Ok;

        if (iterationListenerRegistry)
            status = iterationListenerRegistry->broadcastUpdateMessage(
                IterationListener::UpdateMessage(i, sl->size()));

        if (status == IterationListener::Status_Cancel)
            break;

        SpectrumPtr spectrum = sl->spectrum(i, true);

        // Thermo spectra not from "controllerType=0 controllerNumber=1" are ignored
        if (defaultNativeIdFormat == MS_Thermo_nativeID_format &&
            spectrum->id.find("controllerType=0 controllerNumber=1") != 0)
            continue;

        // scans from a source file other than the default are ignored;
        // note: multiple parentFile elements in mzXML are intended to represent
        //       the data processing history of a single source file
        if (spectrum->sourceFilePtr.get() &&
            spectrum->sourceFilePtr != msd.run.defaultSourceFilePtr)
            continue;

        // write the spectrum
        index.push_back(write_scan(xmlWriter, defaultNativeIdFormat, *spectrum, msd.run.spectrumListPtr, config));

    }
}


void write_index(XMLWriter& xmlWriter, const vector<IndexEntry>& index)
{
    XMLWriter::Attributes attributes;
    attributes.push_back(make_pair("name", "scan"));
    xmlWriter.startElement("index", attributes);

    xmlWriter.pushStyle(XMLWriter::StyleFlag_InlineInner);
    for (vector<IndexEntry>::const_iterator it=index.begin(); it!=index.end(); ++it)
    {
        XMLWriter::Attributes entryAttributes;
        entryAttributes.push_back(make_pair("id", lexical_cast<string>(it->scanNumber)));
        xmlWriter.startElement("offset", entryAttributes);
        xmlWriter.characters(lexical_cast<string>(it->offset));
        xmlWriter.endElement(); // offset
    }
    xmlWriter.popStyle();
     
    xmlWriter.endElement(); // index
}


} // namespace


void Serializer_mzXML::Impl::write(ostream& os, const MSData& msd,
    const pwiz::util::IterationListenerRegistry* iterationListenerRegistry) const
{
    SHA1OutputObserver sha1OutputObserver;
    XMLWriter::Config config;
    config.outputObserver = &sha1OutputObserver;
    XMLWriter xmlWriter(os, config);

    string xmlData = "version=\"1.0\" encoding=\"ISO-8859-1\""; // TODO: UTF-8 ?
    xmlWriter.processingInstruction("xml", xmlData);

    start_mzXML(xmlWriter);

    start_msRun(xmlWriter, msd);
    write_parentFile(xmlWriter, msd);  
    write_msInstruments(xmlWriter, msd, cvTranslator_);
    write_dataProcessing(xmlWriter, msd, cvTranslator_);
    vector<IndexEntry> index;
    write_scans(xmlWriter, msd, config_, index, iterationListenerRegistry);
    xmlWriter.endElement(); // msRun 

    stream_offset indexOffset = xmlWriter.positionNext();

    if (config_.indexed)
    {
        write_index(xmlWriter, index);

        xmlWriter.pushStyle(XMLWriter::StyleFlag_InlineInner);
        xmlWriter.startElement("indexOffset");
        xmlWriter.characters(lexical_cast<string>(indexOffset));
        xmlWriter.endElement();
        xmlWriter.popStyle();
    }

    xmlWriter.pushStyle(XMLWriter::StyleFlag_InlineInner);
    xmlWriter.startElement("sha1");
    xmlWriter.characters(sha1OutputObserver.hash());
    xmlWriter.endElement();
    xmlWriter.popStyle();

    xmlWriter.endElement(); // mzXML
}


//
// read() implementation
//


namespace {


void splitFilename(const string& fullpath, string& path, string& basename)
{
    string::size_type lastSlash = fullpath.find_last_of("/\\");
    if (lastSlash==string::npos || lastSlash==fullpath.size()-1)
    {
        path.clear();
        basename = fullpath; 
        return;
    }

    path = fullpath.substr(0, lastSlash);
    basename = fullpath.substr(lastSlash+1);
}


CVID translate_parentFilenameToSourceFileType(const string& name)
{
    string fileExtension = bal::to_lower_copy(bfs::extension(name));

    // check for known vendor formats
    if (fileExtension == ".raw")
    {
        // hack: only way to tell the difference between thermo and waters
        //       is Thermo uses .RAW (and a file) and Waters uses .raw (and a directory)
        //       there is no way to tell file v. dirctory here, so casing must
        //       be used.
        string fileExtensionCase = bfs::extension(name);
        if (fileExtensionCase == ".raw")
            return MS_Waters_raw_file;
        
        // Use Thermo for anything but all lowercase
        return MS_Thermo_RAW_file;
    }
    else if (fileExtension == ".wiff")                          return MS_ABI_WIFF_file;
    else if (fileExtension == ".yep")                           return MS_Bruker_Agilent_YEP_file;
    else if (fileExtension == ".baf")                           return MS_Bruker_BAF_file;
    // TODO(mchambers): This case cannot throw, but MS_Agilent_MassHunter_file doesn't appear to exist
    else if (fileExtension == ".d")                             return MS_Thermo_RAW_file; // MS_Agilent_MassHunter_file;
    else if (name == "fid")                                     return MS_Bruker_FID_file;
    //else if (name == "msprofile.bin" || name == "mspeak.bin") return MS_Agilent_MassHunter_file);

    // check for known open formats
    else if (fileExtension == ".mzdata")                        return MS_PSI_mzData_file;
    else if (fileExtension == ".mgf")                           return MS_Mascot_MGF_file;
    else if (fileExtension == ".mzxml" ||
             (fileExtension == ".xml" && /* TPP uses ".mz.xml" */
              bal::to_lower_copy(bfs::extension(bfs::basename(name))) == ".mz"))
    {
        return MS_ISB_mzXML_file;
    }
    else if (fileExtension == ".mzml")
    {
        throw runtime_error("[Serializer_mzXML::translate_parentFileExtensionToSourceFileType] mzML parentFile not implemented");
    }
    else
        throw runtime_error("[Serializer_mzXML::translate_parentFileExtensionToSourceFileType] unknown file extension for parentFile \"" + name + "\"");
}


CVID translateSourceFileTypeToNativeIdFormat(CVID sourceFileType)
{
    switch (sourceFileType)
    {
        // for these sources we treat the scan number as the nativeID
        case MS_Thermo_RAW_file:            return MS_Thermo_nativeID_format;
        case MS_Waters_raw_file:            return MS_Waters_nativeID_format;
        case MS_Bruker_Agilent_YEP_file:    return MS_Bruker_Agilent_YEP_nativeID_format;
        case MS_Bruker_BAF_file:            return MS_Bruker_BAF_nativeID_format;
        case MS_ISB_mzXML_file:             return MS_scan_number_only_nativeID_format;
        case MS_PSI_mzData_file:            return MS_spectrum_identifier_nativeID_format;
        case MS_Mascot_MGF_file:            return MS_multiple_peak_list_nativeID_format;
        case MS_Agilent_MassHunter_file:    return MS_Agilent_MassHunter_nativeID_format;

        // for these sources we must assume the scan number came from the index
        case MS_ABI_WIFF_file:
        case MS_Bruker_FID_file:
            return MS_scan_number_only_nativeID_format;

        default:
            throw runtime_error("[Serializer_mzXML::translateSourceFileTypeToNativeIdFormat] unknown file type");
    }
}


void process_parentFile(const string& fileName, const string& fileType,
                        const string& fileSha1, MSData& msd)
{
    string name, location;
    splitFilename(fileName, location, name);
    
    msd.fileDescription.sourceFilePtrs.push_back(SourceFilePtr(new SourceFile));
    SourceFile& sf = *msd.fileDescription.sourceFilePtrs.back();

    sf.id = name;
    sf.name = name;
    sf.location = location;

    if (fileType == "RAWData" || fileType == "processedData")
    {
        CVID sourceFileType = translate_parentFilenameToSourceFileType(name);
        sf.set(sourceFileType);
        sf.set(translateSourceFileTypeToNativeIdFormat(sourceFileType));
    }
    else
        throw runtime_error("[Serializer_mzXML::process_parentFile] invalid value for fileType attribute");

    sf.set(MS_SHA_1, fileSha1);

    // the file level IDs can't be left empty so we set them to be the filename
    if (msd.id.empty() || msd.run.id.empty())
    {
        msd.id = bfs::basename(sf.name);
        msd.run.id = msd.id;
    }
}


SoftwarePtr registerSoftware(MSData& msd, 
                             const string& type, const string& name, const string& version, 
                             const CVTranslator& cvTranslator)
{
    SoftwarePtr result;

    // see if we already registered this Software 
    for (vector<SoftwarePtr>::const_iterator it=msd.softwarePtrs.begin();
         it!=msd.softwarePtrs.end(); ++it)
    {
        CVParam softwareParam = (*it)->cvParamChild(MS_software);

        if (softwareParam.cvid == cvTranslator.translate(name) &&
            (*it)->version == version)
            result = *it;
    }

    // create a new entry
    if (!result.get()) 
    {
        result = SoftwarePtr(new Software);
        msd.softwarePtrs.push_back(result); 
    }

    result->id = name + " software";
    LegacyAdapter_Software adapter(result, msd, cvTranslator);
    adapter.name(name);
    adapter.version(version);
    adapter.type(type);

    return result;
}


struct Handler_msInstrument : public SAXParser::Handler
{
    InstrumentConfiguration* instrumentConfiguration;

    Handler_msInstrument(MSData& msd, const CVTranslator& cvTranslator)
    :   instrumentConfiguration(0), msd_(msd), cvTranslator_(cvTranslator)
    {}

    virtual Status startElement(const string& name, 
                                const Attributes& attributes,
                                stream_offset position)
    {
        if (!instrumentConfiguration)
            throw runtime_error("[Serializer_mzXML::Handler_msInstrument] Null instrumentConfiguration.");

        string value;
        getAttribute(attributes, "value", value);

        if (name=="msInstrument")
        {
            manufacturer_ = model_ = ionisation_ = analyzer_ = detector_ = "";
            return Status::Ok;
        }
        else if (name == "instrument") // older mzXML
        {
            manufacturer_ = model_ = ionisation_ = analyzer_ = detector_ = "";
            getAttribute(attributes, "manufacturer", manufacturer_);
            getAttribute(attributes, "model", model_);
            getAttribute(attributes, "ionisation", ionisation_);
            getAttribute(attributes, "msType", analyzer_);
            return Status::Ok;
        }
        else if (name == "msManufacturer")
        {
            manufacturer_ = value;
            return Status::Ok;
        }
        else if (name == "msModel")
        {
            model_ = value;
            return Status::Ok;
        }
        else if (name == "msIonisation")
        {
            ionisation_ = value;
            return Status::Ok;
        }
        else if (name == "msMassAnalyzer")
        {
            analyzer_ = value;
            return Status::Ok;
        }
        else if (name == "msDetector")
        {
            detector_ = value;
            return Status::Ok;
        }
        else if (name == "software")
        {
            string type, name, version;
            getAttribute(attributes, "type", type);
            getAttribute(attributes, "name", name);
            getAttribute(attributes, "version", version);
            instrumentConfiguration->softwarePtr = registerSoftware(msd_, type, name, version, cvTranslator_);
            return Status::Ok;
        }

        throw runtime_error(("[SpectrumList_mzML::Handler_msInstrument] Unexpected element name: " + name).c_str());
    }

    virtual Status endElement(const string& name, 
                              stream_offset position)
    {
        if (name=="msInstrument" || name=="instrument")
        {
            if (!instrumentConfiguration)
                throw runtime_error("[Serializer_mzXML::Handler_msInstrument] Null instrumentConfiguration.");

            instrumentConfiguration->componentList.push_back(Component(ComponentType_Source, 1));
            instrumentConfiguration->componentList.push_back(Component(ComponentType_Analyzer, 1));
            instrumentConfiguration->componentList.push_back(Component(ComponentType_Detector, 1));

            LegacyAdapter_Instrument adapter(*instrumentConfiguration, cvTranslator_);
            adapter.manufacturerAndModel(manufacturer_, model_); 
            
	    
            if(adapter.model() == "LTQ Orbitrap XL" && analyzer_ == "FTMS") 
                {
  		    analyzer_ = "orbitrap"; // hack to set analyzer_ correctly for LTQ ORBI
                 
                }
            
            adapter.ionisation(ionisation_);
            adapter.analyzer(analyzer_);
            adapter.detector(detector_);
        }

        return Status::Ok;
    }
 
    private:

    MSData& msd_;
    const CVTranslator& cvTranslator_;

    string manufacturer_;
    string model_;
    string ionisation_;
    string analyzer_;
    string detector_;
};


struct Handler_dataProcessing : public SAXParser::Handler
{
    Handler_dataProcessing(MSData& msd, const CVTranslator& cvTranslator)
    :   msd_(msd), cvTranslator_(cvTranslator)
    {}

    virtual Status startElement(const string& name, 
                                const Attributes& attributes,
                                stream_offset position)
    {
        if (name == "dataProcessing")
        {
            string centroided, deisotoped;
            getAttribute(attributes, "centroided", centroided);
            getAttribute(attributes, "deisotoped", deisotoped);
            if (centroided == "1")
                msd_.fileDescription.fileContent.set(MS_centroid_spectrum);
            else // if 0 or absent, assume profile
                msd_.fileDescription.fileContent.set(MS_profile_spectrum);

            // TODO: terms for deisotoped and charge-deconvoluted spectra?

            return Status::Ok;
        }
        else if (name == "software")
        {
            string type, name, version;
            getAttribute(attributes, "type", type);
            getAttribute(attributes, "name", name);
            getAttribute(attributes, "version", version);
            registerSoftware(msd_, type, name, version, cvTranslator_);
            return Status::Ok;
        }
        else if (name == "processingOperation")
        {   // ignore its only attribute, which is "comment"
            return Status::Ok;
        }

        throw runtime_error(("[SpectrumList_mzXML::Handler_dataProcessing] Unexpected element name: " + name).c_str());
    }

    private:
    MSData& msd_;
    const CVTranslator& cvTranslator_;
};


class Handler_mzXML : public SAXParser::Handler
{
    public:

    Handler_mzXML(MSData& msd, const CVTranslator& cvTranslator)
    :   msd_(msd), 
        handler_msInstrument_(msd, cvTranslator), 
        handler_dataProcessing_(msd, cvTranslator)
    {}

    virtual Status startElement(const string& name, 
                                const Attributes& attributes,
                                stream_offset position)
    {
        if (name == "mzXML" || name == "msRun")
        {
            return Status::Ok;
        }
        else if (name == "parentFile")
        {
            string fileName, fileType, fileSha1;
            getAttribute(attributes, "fileName", fileName);
            getAttribute(attributes, "fileType", fileType);
            getAttribute(attributes, "fileSha1", fileSha1);
            process_parentFile(fileName, fileType, fileSha1, msd_);
            return Status::Ok;
        }
        else if (name=="msInstrument" || name=="instrument")
        {
            string id;
            getAttribute(attributes, "msInstrumentID", id);
            if (id.empty()) getAttribute(attributes, "id", id);
            if (id.empty()) getAttribute(attributes, "ID", id); // hack: id or ID
            if (id.empty()) id = "IC1"; // xml:ID cannot be empty
            msd_.instrumentConfigurationPtrs.push_back(InstrumentConfigurationPtr(new InstrumentConfiguration(id)));
            handler_msInstrument_.instrumentConfiguration = msd_.instrumentConfigurationPtrs.back().get();
            return Status(Status::Delegate, &handler_msInstrument_);
        }
        else if (name == "dataProcessing")
        {
            return Status(Status::Delegate, &handler_dataProcessing_);
        }
        else if (name == "scan")
        {
            return Status::Done;
        }

        throw runtime_error(("[SpectrumList_mzXML::Handler_mzXML] Unexpected element name: " + name).c_str());
    }

    private:
    MSData& msd_;
    Handler_msInstrument handler_msInstrument_;
    Handler_dataProcessing handler_dataProcessing_;
};

} // namespace


void Serializer_mzXML::Impl::read(shared_ptr<istream> is, MSData& msd) const
{
    if (!is.get() || !*is)
        throw runtime_error("[Serializer_mzXML::read()] Bad istream.");

    Handler_mzXML handler(msd, cvTranslator_);
    SAXParser::parse(*is, handler); 
    msd.run.spectrumListPtr = SpectrumList_mzXML::create(is, msd, config_.indexed);
}


//
// Serializer_mzXML
//


PWIZ_API_DECL Serializer_mzXML::Serializer_mzXML(const Config& config)
:   impl_(new Impl(config))
{}


PWIZ_API_DECL void Serializer_mzXML::write(ostream& os, const MSData& msd,
   const pwiz::util::IterationListenerRegistry* iterationListenerRegistry) const
{
    return impl_->write(os, msd, iterationListenerRegistry);
}


PWIZ_API_DECL void Serializer_mzXML::read(shared_ptr<istream> is, MSData& msd) const
{
    return impl_->read(is, msd);
}


PWIZ_API_DECL ostream& operator<<(ostream& os, const Serializer_mzXML::Config& config)
{
    os << config.binaryDataEncoderConfig 
       << " indexed=\"" << boolalpha << config.indexed << "\"";
    return os;
}


} // namespace msdata
} // namespace pwiz


