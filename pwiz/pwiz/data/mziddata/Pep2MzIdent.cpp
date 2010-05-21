//
// $Id$
//
// Original author: Robert Burke <robert.burke@proteowizard.org>
//
// Copyright 2009 Spielberg Family Center for Applied Proteomics
//   University of Southern California, Los Angeles, California  90033
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

#include "Pep2MzIdent.hpp"
#include "pwiz/utility/chemistry/Ion.hpp"
#include "pwiz/data/common/cv.hpp"
#include "boost/regex.hpp"
#include "boost/algorithm/string/join.hpp"
#include "boost/lexical_cast.hpp"
#include "boost/filesystem.hpp"
#include "boost/algorithm/string.hpp"
#include "boost/foreach.hpp"
#include "boost/tokenizer.hpp"

// Debug macro to be used if needed.
#define DOUT(a) do{ if(debug) std::cout << a << endl; } while(0)

using namespace pwiz;
using namespace pwiz::cv;
using namespace pwiz::mziddata;
using namespace pwiz::data::pepxml;
using namespace pwiz::chemistry;

using namespace boost;
using namespace std;

using boost::algorithm::join;

// String constants

const char* PERSON_DOC_OWNER = "PERSON_DOC_OWNER";

namespace pwiz {
namespace mziddata {

// Utility structs
struct Pep2MzIdent::Indices
{
    Indices()
        : dbseq(0), enzyme(0), sip(0), peptide(0),
          peptideEvidence(0), sd(0), sir(0), sii(0), sil(0),
          pdp(0)
    {
    }

    size_t dbseq;
    size_t enzyme;
    size_t sip;
    size_t peptide;
    size_t peptideEvidence;
    size_t sd;
    size_t sir;
    size_t sii;
    size_t sil;
    size_t pdp;
};

} // namespace pwiz 
} // namespace mziddata 

namespace {

using namespace pwiz::mziddata;

// TODO may move this into a file of its own.

//
// XML tree walking
//


/*
  Supported tags:
  
const char* supported_tags[]  =
{
    "mzIdentML",

    "AnalysisSoftware",
    "Provider",
    "AuditCollection",
    "AnalysisSampleCollection",
    "SequenceCollection",
    "AnalysisCollection",
    "AnalysisProtocolCollection",
    "DataCollection",
    "BibliographicReference",

    // Provider and AuditCollection
    "ContactRole",
    "Inputs",
    "AnalysisData",

    // DataCollection
    "SpectrumIdentificationList",
    "ProteinDetectionList",

    // SpectrumIdentificationList
    "FragmentationTable",
    "Measure",
    "SpectrumIdentificationResult",

    // Measure
    // + cvParam or userParam

    // SpectrumIdentificationResult
    "SpectrumIdentificationItem",
    // + cvParam or userParam

    // SpectrumIdentificationItem
    "PeptideEvidence",
    "Fragmentation",
    "IonType",
    // + cvParam or userParam

    // IonType
    "FragmentArray",
    // + cvParam or userParam

    // FragmentArray - nothing

    // PeptideEvidence
    // + cvParam or userParam

    // ProteinDetectionList
    "ProteinAmbiguityGroup",
    // + cvParam or userParam

    // ProteinAmbiguityGroup
    "ProteinDetectionHypothesis",
    // + cvParam or userParam

    // ProteinDetectionHypothesis
    "PeptideHypothesis",
    // + cvParam or userParam

    // Inputs
    "SourceFile",
    "SearchDatabase",

    // SourceFile
    "externalFormatDocumentation",
    "fileFormat", // + cvParam or userParam
    // + cvParam or userParam

    // SearchDatabase
    "fileFormat", // + cvParam or userParam
    "DatabaseName",
    // + cvParam or userParam

    // SpectraData
    "fileFormat", // + cvParam or userParam
    "externalFormatDocumentation",
    "spectrumIDFormat", // + cvParam or userParam

    // AnalysisSampleCollection
    "Sample",

    // AnalysisProtocolCollection
    "SpectrumIdentificationProtocol",
    "ProteinDetectionProtocol",

    // ProteinDetectionProtocol
    "AnalysisParams", // + cvParam or userParam
    "Threshold", // + cvParam or userParam

    // SpectrumIdentificationProtocol
    "SearchType", // + cvParam or userParam
    "AdditionalSearchParams",  // + cvParam or userParam
    "ModificationParams",
    "SearchModification",
    "Enzymes",
    "MassTable",
    "FragmentTolerance", // + cvParam or userParam
    "ParentTolerance", // + cvParam or userParam
    "Threshold", // + cvParam or userParam
    "DatabaseFilters",
    "Filter",
    "DatabaseTranslation",

    // DatabaseTranslation
    "TranslationTable",

    // TranslationTable
    // + cvParam or userParam

    // Filter
    "FilterType", // + cvParam or userParam
    "Include", // + cvParam or userParam
    "Exclude", // + cvParam or userParam

    // SearchModification
    "ModParam",
    "SpecificityRules", // + cvParam or userParam

    // ModParam
    // + cvParam

    // MassTable
    "Residue",
    "AmbiguousResidue",

    // AmbiguousResidue
    // + cvParam or userParam

    // Residue

    // Enzymes
    "Enzyme",

    // Enzyme
    "SiteRegexp",
    "EnzymeName", // + cvParam or userParam

    // AnalysisCollection
    "SpectrumIdentification",
    "ProteinDetection",

    // Sample
    "ContactRole",
    "subSample",
    // + cvParam or userParam

    // ProteinDetection
    "InputSpectrumIdentifications",

    // SpectrumIdentification
    "InputSpectra",
    "SearchDatabase",

    // AnalysisSoftware
    "ContactRole",
    "SoftwareName",
    "Customizations",

    // ContactRole
    "role", // + cvParam or userParam
    // + cvParam or userParam
};
*/

struct organization_p
{
    bool operator()(ContactPtr contact)
    {
        return typeid(contact.get()).name() == typeid(Organization*).name();
    }
};

struct person_p
{
    bool operator()(ContactPtr contact)
    {
        return typeid(contact.get()).name() == typeid(Person*).name();
    }
};

bool addToContactRoleCV(CVParam& param, vector<string>& path,
                  ContactRole& contactRole)
{
    if (path.size() &&
        path.at(0) == "role")
    {
        contactRole.role.cvParams.push_back(param);
        return true;
    }

    return false;
}

bool addToContactCV(CVParam& param, vector<string>& path,
                  vector<ContactPtr>& contacts)
{
    string nextTag = path.at(0);
    path.erase(path.begin());

    vector<ContactPtr>::iterator i = contacts.end();
    if (nextTag == "Organization")
    {
        i=find_if(contacts.begin(), contacts.end(),
                  organization_p());
        if (i == contacts.end())
        {
            // TODO add a meaningful id & name
            contacts.push_back(ContactPtr(new Organization("O_1")));
            i = contacts.end() - 1;
        }
    }
    else if (nextTag == "Person")
    {
        i=find_if(contacts.begin(), contacts.end(),
                  person_p());
        if (i == contacts.end())
        {
            // TODO add a meaningful id & name
            contacts.push_back(ContactPtr(new Person("P_1")));
            i = contacts.end() - 1;
        }
    }
    else
        throw runtime_error(("Unsupported tag "+nextTag).c_str());

    // If we have a place to put the cvParam and no where else to go...
    if (i != contacts.end() && !path.size())
    {
        (*i)->params.cvParams.push_back(param);
        return true;
    }

    // Otherwise, throw our hands up in confusion.
    return false;
}

bool addToAnalysisSoftwareLevel(CVParam& param, vector<string>& path,
                              AnalysisSoftwarePtr as)
{
    if (path.size() == 0 || path.at(0).size() == 0)
        throw runtime_error("[addAnalysisSoftwareLevel] empty path string passed in");

    if (path.at(0) == "ContactRole")
    {
        path.erase(path.begin());
        if (!as->contactRolePtr.get())
            as->contactRolePtr = ContactRolePtr(new ContactRole());
        return addToContactRoleCV(param, path, *(as->contactRolePtr.get()));
    }
    else if (path.at(0) == "SoftwareName")
    {
    }
    else if (path.at(0) == "Customizations")
    {
    }
    
    return false;
}

bool addToSample(CVParam& param, vector<string>& path, SamplePtr sample)
{
    if (!path.size())
    {
        sample->cvParams.cvParams.push_back(param);
        return true;
    }

    if (path.at(0) == "ContactRole")
    {
        path.erase(path.begin());
        return addToContactRoleCV(param, path, sample->contactRole);
    }
    
    return false;
}

bool addToMzIdentMLLevel(CVParam& param, vector<string>& path, MzIdentML& mzid)
{
    /*
      "AnalysisSoftware",
        "Provider",
        "AuditCollection",
        "AnalysisSampleCollection",
        "SequenceCollection",
        "AnalysisCollection",
        "AnalysisProtocolCollection",
        "DataCollection",
        "BibliographicReference",
     */

    if (!path.size() == 0 || path.at(0).size())
        throw runtime_error("[addMzIdentMLLevel] empty path string passed in");

    string tag = path.at(0);
    path.erase(path.begin());
    
    if (tag == "AnalysisSoftware")
    {
        mzid.analysisSoftwareList.push_back(AnalysisSoftwarePtr(new AnalysisSoftware()));
        return addToAnalysisSoftwareLevel(param, path, mzid.analysisSoftwareList.back());
    }
    else if (tag == "Provider")
    {
        // Is this a path to the only ParamContainer in Provider?
        if (path.size() >= 2 &&
            path.at(0) == "ContactRole")
        {
            path.erase(path.begin());
            return addToContactRoleCV(param, path, mzid.provider.contactRole);
        }
        // Otherwise fall through to "false"
    }
    else if (tag == "AuditCollection")
    {
        return addToContactCV(param, path, mzid.auditCollection);
    }
    else if (tag == "AnalysisSampleCollection")
    {
        if (path.size() < 1 ||
            path.at(0) != "Sample")
            return false;

        path.erase(path.begin());
        
        SamplePtr sample;
        if (mzid.analysisSampleCollection.samples.size() == 0)
        {
            mzid.analysisSampleCollection.samples.push_back(SamplePtr(new Sample()));
        }

        sample = mzid.analysisSampleCollection.samples.back();

        return addToSample(param, path, sample);
    }
    else if (tag == "SequenceCollection")
    {
    }
    else if (tag == "AnalysisCollection")
    {
    }
    else if (tag == "AnalysisProtocolCollection")
    {
    }
    else if (tag == "DataCollection")
    {
    }
    else if (tag == "BibliographicReference")
    {
    }
    else
        throw runtime_error(("[addMzIdentMLLevel] Unsupported tag "+
                             tag).c_str());

    return false;
}

// Adds a cvParam to allocation in the mzIdentML tree.
bool addCvByPath(CVParam& param, const std::string& path, MzIdentML& mzid)
{
    vector<string> parts;
    
    char_separator<char> sep("/@");
    tokenizer< char_separator<char> > tokens(path, sep);

    for(tokenizer< char_separator<char> >::const_iterator it=tokens.begin();
        it!=tokens.end(); it++)
    {
        string tag = *it;

        parts.push_back(tag);
    }

    if (parts.at(0) != "mzIdentML")
        throw runtime_error("[addCvByPath] Root element other than mzIdentML in path.");
    
    return addToMzIdentMLLevel(param, parts, mzid);
}

struct sequence_p
{
    const string seq;
    
    sequence_p(const string& seq) : seq(seq) {}

    bool operator()(const PeptidePtr& p) const
    {
        return (p->peptideSequence == seq);
    }
};

struct seq_p
{
    const string seq;
    
    seq_p(const string& seq) : seq(seq) {}

    bool operator()(const DBSequencePtr& dbs) const
    {
        return (dbs->seq == seq);
    }
};

template<typename T>
struct id_p
{
    const string id;

    id_p(const string id) : id(id) {}

    bool operator()(const shared_ptr<T>& t) const { return t->id == id; } 
};

struct parameter_p
{
    const string name;

    parameter_p(const string name) : name(name) {}

    bool operator()(const shared_ptr<Parameter>& p) const
    {
        return p->name == name;
    }
};

// Utility functions
template<typename T>
shared_ptr<T> find_id(vector< shared_ptr<T> >& list, const string& id)
{
    typename vector< shared_ptr<T> >::iterator c =
        find_if(list.begin(), list.end(), id_p<T>(id));

    if (c == list.end())
        return shared_ptr<T>((T*)NULL);

    return *c;
}


struct search_score_p
{
    const string name;

    search_score_p(const string& name) : name(name) {}

    bool operator()(SearchScorePtr ss)
    {
        return iequals(name, ss->name);
    }
};

void resizeSoftware(vector<AnalysisSoftwarePtr>& v, size_t new_size)
{
    size_t start = v.size();
    for (size_t i=start; i<new_size; i++)
        v.push_back(AnalysisSoftwarePtr(new AnalysisSoftware()));
    
}

struct software_p
{
    CVID id;
    
    software_p(const CVID id) : id(id) {}

    bool operator()(const AnalysisSoftwarePtr p)
    {
        bool result = false;

        if (p.get())
            result = p->softwareName.hasCVParam(id);

        return result;
    }
};

AnalysisSoftwarePtr findSoftware(const vector<AnalysisSoftwarePtr>& software,
    CVID cvid)
{
    AnalysisSoftwarePtr as((AnalysisSoftware*)NULL);
    
    vector<AnalysisSoftwarePtr>::const_iterator i =
        find_if(software.begin(), software.end(), software_p(cvid));
    
    if (i != software.end())
        as = *i;
    
    return  as;
}

CVParam guessThreshold(const vector<AnalysisSoftwarePtr>& software)
{
    static const CVID cvids[][2] = {
        {MS_Mascot, MS_mascot_score},
        {MS_Sequest, MS_sequest_probability},
        {MS_Phenyx, MS_Phenyx_Score},
        {CVID_Unknown, CVID_Unknown}
    };
    CVParam cvparam;

    for (size_t idx = 0; cvids[idx][0] != CVID_Unknown; idx++)
    {
        AnalysisSoftwarePtr as = findSoftware(software, cvids[idx][0]);
        
        if (!as.get())
        {
            cvparam = CVParam(cvids[idx][1], "0.5");
            break;
        }
    }

    // TODO put a reasonable default here and a reasonable way to set it.
    if (cvparam.cvid == CVID_Unknown)
    {
        cvparam.cvid = MS_mascot_score;
        cvparam.value = "0.05";
    }
    
    return cvparam;
}

AnalysisSoftwarePtr guessAnalysisSoftware(
    const vector<AnalysisSoftwarePtr>& software)
{
    static const CVID cvids[] = {MS_Mascot, MS_Sequest,
                                 MS_Phenyx, CVID_Unknown};
    AnalysisSoftwarePtr asp(new AnalysisSoftware());

    for (size_t idx = 0; cvids[idx] != CVID_Unknown; idx++)
    {
        AnalysisSoftwarePtr as = findSoftware(software, cvids[idx]);
        
        if (as.get() && !as->empty())
        {
            asp = as;
            break;
        }
    }

    return asp;
}

struct fileInfo
{
    const string ext;
    CVID fileFormat;
    CVID spectrumIDFormat;
    
    fileInfo(const string& ext, CVID fileFormat,CVID spectrumIDFormat)
        : ext(ext), fileFormat(fileFormat), spectrumIDFormat(spectrumIDFormat)
    {}

};

bool fileExtension2Type(const string& file,
                        CVID& fileFormat,
                        CVID& spectrumIDFormat)
{
    // TODO map the following file formats
    //{MS_Waters_raw_file, MS_mass_spectrometer_file_format},
    //{MS_mass_spectrometer_file_format, MS_file_format},
    //{MS_ABI_WIFF_file, MS_mass_spectrometer_file_format},
    //{MS_Thermo_RAW_file, MS_mass_spectrometer_file_format},
    //{MS_PSI_mzData_file, MS_mass_spectrometer_file_format},
    //{MS_Micromass_PKL_file, MS_mass_spectrometer_file_format},
    //{MS_ISB_mzXML_file, MS_mass_spectrometer_file_format},
    //{MS_Bruker_Agilent_YEP_file, MS_mass_spectrometer_file_format},
    //{MS_mzML_file, MS_mass_spectrometer_file_format},
    //{MS_DTA_file, MS_mass_spectrometer_file_format},
    //{MS_ProteinLynx_Global_Server_mass_spectrum_XML_file, MS_mass_spectrometer_file_format},
    //{MS_parameter_file, MS_mass_spectrometer_file_format},
    //{MS_Bioworks_SRF_file, MS_mass_spectrometer_file_format},
    //{MS_Bruker_BAF_file, MS_mass_spectrometer_file_format},
    //{MS_Bruker_U2_file, MS_mass_spectrometer_file_format},
    //{MS_Bruker_FID_file, MS_mass_spectrometer_file_format},
    //{MS_Mascot_MGF_file, MS_mass_spectrometer_file_format},
    //{MS_PerSeptive_PKS_file, MS_mass_spectrometer_file_format},
    //{MS_Sciex_API_III_file, MS_mass_spectrometer_file_format},
    //{MS_Bruker_XML_file, MS_mass_spectrometer_file_format},
    //{MS_text_file, MS_mass_spectrometer_file_format},
    //{MS_Phenyx_XML_format, MS_mass_spectrometer_file_format},
    //{MS_AB_SCIEX_TOF_TOF_database, MS_mass_spectrometer_file_format},
    //{MS_Agilent_MassHunter_file, MS_mass_spectrometer_file_format},
    //{MS_Proteinscape_spectra, MS_mass_spectrometer_file_format},
    //{MS_AB_SCIEX_TOF_TOF_T2D_file, MS_mass_spectrometer_file_format},

    static fileInfo pairs[] = {
        fileInfo(".mzml", MS_mzML_file, MS_mzML_unique_identifier),
        fileInfo(".mzxml", MS_ISB_mzXML_file, MS_scan_number_only_nativeID_format),
        fileInfo(".pkl", MS_Micromass_PKL_file, MS_no_nativeID_format),
        //fileInfo("", CVID_Unknown,CVID_Unknown)
    };

    bool found = false;
    BOOST_FOREACH(fileInfo fi, pairs)
    {
        if (iends_with(file, fi.ext))
        {
            fileFormat = fi.fileFormat;
            spectrumIDFormat = fi.spectrumIDFormat;

            found = true;
            break;
        }
    }

    return found;
}


} // namespace

namespace pwiz {
namespace mziddata {

//
// Pep2MzIdent
//

Pep2MzIdent::Pep2MzIdent(const MSMSPipelineAnalysis& mspa, MzIdentMLPtr mzid)
    : _mspa(&mspa), mzid(mzid), debug(false),
      precursorMonoisotopic(false), fragmentMonoisotopic(false),
      indices(new Indices())
{
    //mzid->cvs.push_back(cv::cv("MS"));
    //mzid->cvs.push_back(cv::cv("UO"));
    //translate();
}

void Pep2MzIdent::setMspa(const MSMSPipelineAnalysis& mspa)
{
    clear();
    
    _mspa = &mspa;
}

bool Pep2MzIdent::operator()(const MSMSPipelineAnalysis& pepxml, MzIdentMLPtr mzid)
{
    _mspa = &pepxml;
    this->mzid = mzid;

    translate();

    // TODO Change this meaningless return into somethingn good.
    return true;
}

void Pep2MzIdent::clear()
{
    indices = shared_ptr<Indices>(new Indices());

    _mspa = NULL;

    mzid = MzIdentMLPtr(new MzIdentML());
    mzid->cvs.push_back(cv::cv("MS"));
    mzid->cvs.push_back(cv::cv("UO"));

    _translated = false;

    precursorMonoisotopic = false;
    fragmentMonoisotopic = false;

    seqPeptidePairs.clear();
    aminoAcidModifications = NULL;
}

void Pep2MzIdent::setDebug(bool debug)
{
    this->debug = debug;
}

bool Pep2MzIdent::getDebug() const
{
    return this->debug;
}

void Pep2MzIdent::setVerbose(bool verbose)
{
    this->verbose = verbose;
}

bool Pep2MzIdent::getVerbose() const
{
    return this->verbose;
}

// pepxml parameter -> cvid mapping methods.

void Pep2MzIdent::addParamMap(vector<CVMapPtr>& map)
{
    parameterMap.insert(parameterMap.begin(),
                        map.begin(), map.end());
}

void Pep2MzIdent::setParamMap(vector<CVMapPtr>& map)
{
    parameterMap.clear();

    parameterMap.assign(map.begin(), map.end());
}

const vector<CVMapPtr>& Pep2MzIdent::getParamMap() const
{
    return parameterMap;
}

// Private methods belong below this line
// ------------------------------------------------------------------------

void Pep2MzIdent::translateRoot()
{
    mzid->creationDate = _mspa->date;

    addSpectraData(_mspa->msmsRunSummary, mzid);
    translateEnzyme(_mspa->msmsRunSummary.sampleEnzyme, mzid);

    earlyMetadata();

    for (vector<SearchSummaryPtr>::const_iterator ss =
             _mspa->msmsRunSummary.searchSummary.begin();
         ss != _mspa->msmsRunSummary.searchSummary.end(); ss++)
    {
        translateSearch(*ss, mzid);
    }

    for (vector<SpectrumQueryPtr>::const_iterator it=_mspa->msmsRunSummary.spectrumQueries.begin(); it!=_mspa->msmsRunSummary.spectrumQueries.end(); it++)
    {
        translateQueries(*it, mzid);
    }

    lateMetadata();
    
    addFinalElements();
}

void Pep2MzIdent::addSpectraData(const MSMSRunSummary& msmsRunSummary,
                                 MzIdentMLPtr result)
{
    SpectraDataPtr sd(new SpectraData(
                          "SD_"+lexical_cast<string>(indices->sd++)));

    // TODO verify that both of these cvid values are legit.
    sd->location = msmsRunSummary.base_name;


    CVID fileFormat, spectrumIDFormat;
    if (fileExtension2Type(msmsRunSummary.raw_data, fileFormat, spectrumIDFormat))
    {
        sd->fileFormat.set(fileFormat);
        sd->spectrumIDFormat.set(spectrumIDFormat);
    }
    else if (fileExtension2Type(msmsRunSummary.raw_data_type, fileFormat, spectrumIDFormat))
    {
        sd->fileFormat.set(fileFormat);
        sd->spectrumIDFormat.set(spectrumIDFormat);
    }
    // In some pepXML files both may be blank. In this case the data
    // may be kept in a parameter named "FILE"
        
    mzid->dataCollection.inputs.spectraData.push_back(sd);
}

void Pep2MzIdent::translateEnzyme(const SampleEnzyme& sampleEnzyme,
                                  MzIdentMLPtr result)
{
    SpectrumIdentificationProtocolPtr sip(
        new SpectrumIdentificationProtocol(
            "SIP_"+lexical_cast<string>(indices->sip++)));

    // DEBUG
    sip->analysisSoftwarePtr = AnalysisSoftwarePtr(new AnalysisSoftware("AS"));
    //sip->analysisSoftwarePtr = result->analysisSoftwareList.back();
    sip->searchType.set(MS_ms_ms_search);
    sip->threshold.cvParams.push_back(guessThreshold(mzid->analysisSoftwareList));
    EnzymePtr enzyme(new Enzyme("E_"+lexical_cast<string>(indices->enzyme++)));

    // Cross fingers and pray that the name enzyme matches a cv name.
    // TODO create a more flexable conversion.
    CVID seCVID = getCVID(sampleEnzyme.name);
    if (seCVID != CVID_Unknown)
        enzyme->enzymeName.set(seCVID);
    else
        enzyme->enzymeName.userParams.
            push_back(UserParam("name", sampleEnzyme.name));

    if (sampleEnzyme.description.size()>0)
        enzyme->enzymeName.userParams.
            push_back(UserParam("description", sampleEnzyme.description));

    if (sampleEnzyme.fidelity == "Semispecific")
        enzyme->semiSpecific = true;
    else if (sampleEnzyme.fidelity == "Nonspecific")
        enzyme->semiSpecific = false;

    enzyme->minDistance = sampleEnzyme.specificity.minSpace;

    // TODO handle sense fields
    // first attempt at regex
    enzyme->siteRegexp = "[^"+sampleEnzyme.specificity.noCut+
        "]["+sampleEnzyme.specificity.cut+"]";
    
    sip->enzymes.enzymes.push_back(enzyme);
    
    // sampleEnzyme.independant is a tribool. If the optional
    // attribute was not set, then don't alter the mzid Enzymes'
    // values.
    if (sampleEnzyme.independent || !sampleEnzyme.independent)
        sip->enzymes.independent = (bool)sampleEnzyme.independent;
        
    result->analysisProtocolCollection.
        spectrumIdentificationProtocol.push_back(sip);
}

CVParam Pep2MzIdent::translateSearchScore(const string& name, const vector<SearchScorePtr>& searchScore)
{
    typedef vector<SearchScorePtr>::const_iterator CIt;
    
    CIt cit = find_if(searchScore.begin(), searchScore.end(),
                      search_score_p(name));

    CVParam cvp;
    if (cit != searchScore.end())
    {
        cvp = getParamForSearchScore(*cit);
    }

    return cvp;
}


CVParam Pep2MzIdent::getParamForSearchScore(const SearchScorePtr searchScore)
{
    CVID id = cvidFromSearchScore(searchScore->name);

    CVParam cvParam(id, searchScore->value);

    return cvParam;
}

CVID Pep2MzIdent::cvidFromSearchScore(const string& name)
{
    CVID id = translator.translate(name);

    if (id == CVID_Unknown)
    {
        if (iequals(name, "ionscore"))
        {
            id = MS_mascot_score;
        }
        else if (iequals(name, "identityscore"))
        {
            id = MS_mascot_identity_threshold;
        }
        else if (iequals(name, "expect"))
        {
            id = MS_mascot_expectation_value;
        }
    }

    return id;
}


void Pep2MzIdent::translateSearch(const SearchSummaryPtr summary,
                                  MzIdentMLPtr result)
{
    namespace fs = boost::filesystem;

    // push SourceFilePtr onto sourceFile
    // in Inputs in DataCollection
    //
    // Removed because sourcefiles are for intermediate file formats,
    // not the source of the data.
    
    /*
    SourceFilePtr sourceFile(new SourceFile());
    sourceFile->id = "SF_1";
    sourceFile->location = summary->baseName;
    sourceFile->fileFormat.set(MS_ISB_mzXML_file);

    result->dataCollection.inputs.sourceFile.push_back(sourceFile);
    */
    
    AnalysisSoftwarePtr as(new AnalysisSoftware());
    as->id = "AS";
    CVID cvid = getCVID(summary->searchEngine);
    if (cvid != CVID_Unknown)
        as->softwareName.set(cvid);
    else
    {
        vector<string> customizations;
        cvid = mapToNearestSoftware(summary->searchEngine, customizations);
        if (cvid == CVID_Unknown)
            throw invalid_argument(("Unknown search software name "+
                                    summary->searchEngine).c_str());

        as->softwareName.set(cvid);
        if (customizations.size())
        {
            as->customizations = join(customizations, ", ");
        }

        // TODO This is invalid, but we need to save the software
        // somewhere.
        /*
        as->softwareName.userParams.push_back(
            UserParam("search_engine full name",
            summary->searchEngine));
        */
    }
    result->analysisSoftwareList.push_back(as);

    // handle precursorMassType/fragmentMassType
    precursorMonoisotopic = summary->precursorMassType == "monoisotopic";
    fragmentMonoisotopic = summary->fragmentMassType == "monoisotopic";
    
    SearchDatabasePtr searchDatabase(new SearchDatabase());
    searchDatabase->id = "SD_1";
    searchDatabase->name = summary->searchDatabase.databaseName;
    searchDatabase->location = summary->searchDatabase.localPath;
    searchDatabase->version = summary->searchDatabase.databaseReleaseIdentifier;
    searchDatabase->numDatabaseSequences = summary->searchDatabase.sizeInDbEntries;
    searchDatabase->numResidues = summary->searchDatabase.sizeOfResidues;

    // Select which type of database is indeicated.
    if (summary->searchDatabase.type == "AA")
        searchDatabase->params.set(MS_database_type_amino_acid);
    else if (summary->searchDatabase.type == "NA")
        searchDatabase->params.set(MS_database_type_nucleotide);

    
    // TODO figure out if this is correct
    CVID dbName = getCVID(summary->searchDatabase.databaseName);
    if (dbName != CVID_Unknown)
        searchDatabase->DatabaseName.set(dbName);
    else
    {
        fs::path localPath(summary->searchDatabase.localPath);
        searchDatabase->DatabaseName.userParams.push_back(
            UserParam("local_path", localPath.filename()));
    }

    // TODO this goes in the analysis software section, I think.
    /*
    if (iends_with(summary->baseName, ".dat") &&
        iequals(summary->searchEngine, "mascot"))
        searchDatabase->fileFormat.set(MS_Mascot_DAT_file);
    else if (iequals(summary->searchEngine, "sequest"))
        searchDatabase->fileFormat.set(MS_Sequest_out_file);
    else if (iequals(summary->searchEngine, "xtandem"))
        searchDatabase->fileFormat.set(MS_xtandem_xml_file);
    */
    
    mzid->dataCollection.inputs.searchDatabase.push_back(searchDatabase);
    
    // Save for later.
    aminoAcidModifications = &summary->aminoAcidModifications;
}


CVID Pep2MzIdent::mapToNearestSoftware(const string& softwareName,
                                       vector<string>& customizations)
{
    // TODO clean this up and move the patterns into a separate class
    // as we get more patterns.
    static regex xtandemMod("[xX][\\!]?[ ]*[Tt]andem[ ]*[\\(]?([^\\)]*)[\\)]?");
    
    CVID cvid = getCVID(softwareName);

    if (cvid == CVID_Unknown)
    {
        smatch what;
        if (regex_match(softwareName, what, xtandemMod))
        {
            cvid = MS_xtandem;

            // HACK: smatch.size() is never 0
            if (what.size()>1 && // If there's at least 1 match
                what[1].matched && // If the first one we're
                                   // interested in matches
                what[1].first != what[1].second // If it's not zero length
                )
            {
                customizations.push_back(what[1]);
                cout << "Customization: " << what[1] << endl;
            }
        }
    }

    return cvid;
}


void Pep2MzIdent::addModifications(
    const vector<AminoAcidModification>& aminoAcidModifications,
    PeptidePtr peptide, MzIdentMLPtr result)
{
    typedef vector<AminoAcidModification>::const_iterator aam_iterator;
    
    for (aam_iterator it=aminoAcidModifications.begin();
         it != aminoAcidModifications.end(); it++)
    {
        ModificationPtr mod(new Modification());

        // If the peptide has modified amino acids in the proper
        // position, Add a Modification element. "nc" is tacked on to
        // both until I find out where it goes.

        // If n terminus mod, check the beginning
        if (
            ((it->peptideTerminus == "n" ||
              it->peptideTerminus == "nc") &&
             peptide->peptideSequence.at(0) == it->aminoAcid.at(0))
            ||
            // If c terminus mod, check the end
            ((it->peptideTerminus == "c" ||
              it->peptideTerminus == "nc") &&
             peptide->peptideSequence.at(peptide->peptideSequence.size()-1)
             == it->aminoAcid.at(0))
            )
        {            
            if(precursorMonoisotopic)
                mod->monoisotopicMassDelta = it->massDiff;
            else
                mod->avgMassDelta = it->massDiff;
            mod->residues = it->aminoAcid;

            // TODO save terminus somewhere
            if (it->peptideTerminus == "c")
            {
                mod->location = peptide->peptideSequence.size();
            }
            else if (it->peptideTerminus == "n")
            {
                mod->location = 0;
            }
            else if (it->peptideTerminus == "nc")
            {
                mod->location = 0;

                // TODO is this right?
                ModificationPtr mod2(new Modification());
                mod2 = mod;
                mod2->location = peptide->peptideSequence.size();
                peptide->modification.push_back(mod2);
            }
        
            peptide->modification.push_back(mod);
        }
    }
}


void Pep2MzIdent::translateQueries(const SpectrumQueryPtr query,
                                   MzIdentMLPtr result)
{
    for(vector<SearchResultPtr>::iterator srit=query->searchResult.begin();
        srit != query->searchResult.end(); srit++)
    {
        for (vector<SearchHitPtr>::iterator shit=(*srit)->searchHit.begin();
             shit != (*srit)->searchHit.end(); shit++)
        {
            const string pid = addPeptide(*shit, result);

            if (find_if(mzid->sequenceCollection.dbSequences.begin(),
                        mzid->sequenceCollection.dbSequences.end(),
                        seq_p((*shit)->peptide)) !=
                mzid->sequenceCollection.dbSequences.end())
                continue;
            
            DBSequencePtr dbs(new DBSequence("DBS_"+lexical_cast<string>(
                                                 indices->dbseq++)));
            dbs->length = (*shit)->peptide.length();
            dbs->seq = (*shit)->peptide;
            dbs->accession = (*shit)->protein;

            // TODO find the best way of setting the protein for
            // DBSequence's MS_protein_description cvParam.
            const string* protein_desc = NULL;
            if ((*shit)->proteinDescr.length()>0)
                protein_desc = &(*shit)->proteinDescr;
            else if ((*shit)->protein.length()>0)
                protein_desc = &(*shit)->protein;
            else
                throw runtime_error(("No protein found for sequence "+
                                     (*shit)->peptide).c_str());
            
            dbs->paramGroup.set(MS_protein_description, *protein_desc);
            if (mzid->dataCollection.inputs.searchDatabase.size()>0)
                dbs->searchDatabasePtr = mzid->dataCollection.inputs.
                    searchDatabase.at(0);
            else
                dbs->searchDatabasePtr = SearchDatabasePtr(
                    new SearchDatabase("SD_1"));

            mzid->sequenceCollection.dbSequences.push_back(dbs);
            
            PeptideEvidencePtr pepEv(new PeptideEvidence(
                                         "PE_"+lexical_cast<string>(
                                             indices->peptideEvidence++)));
            
            // TODO make sure handle the spectrum field
            //pepEv->paramGroup.userParams.push_back(UserParam("spectrum",
            //                                                 query->spectrum));

            pepEv->start = query->startScan;
            pepEv->end = query->endScan;
            pepEv->dbSequencePtr = dbs;
    
            SpectrumIdentificationItemPtr sii(
                new SpectrumIdentificationItem(
                    "SII_"+lexical_cast<string>(indices->sii++)));

            sii->peptideEvidence.push_back(pepEv);
            sii->rank = (*shit)->hitRank;
            
            // TODO find out if this use of assumedCharge is right.
            sii->chargeState = query->assumedCharge;

            // TODO put neutral mass where it belongs

            // TODO find out if pepxml's precursorNeutralMass is
            // experimental or calculated.
            if (query->assumedCharge == 0)
                throw runtime_error(("zero assumed_charge found in spectrum: "+
                                     query->spectrum).c_str());
            
            sii->experimentalMassToCharge = query->precursorNeutralMass /
                query->assumedCharge;

            sii->calculatedMassToCharge = (*shit)->calcNeutralPepMass /
                query->assumedCharge;

            // TODO get search_score(s)
            typedef vector<SearchScorePtr>::const_iterator SSP_cit;
            for (SSP_cit ssit=(*shit)->searchScore.begin();
                 ssit != (*shit)->searchScore.end(); ssit++)
            {
                CVParam cvp;
                cvp = getParamForSearchScore(*ssit);
                if (cvp.cvid != CVID_Unknown)
                    sii->paramGroup.set(cvp.cvid, cvp.value);
                else
                    sii->paramGroup.userParams.
                        push_back(UserParam((*ssit)->name, (*ssit)->value));
                    
            }
            /*
            CVParam cvp = translateSearchScore("ionscore",
                                               (*shit)->searchScore);

            std::cerr << "ionscore=" << cvp.cvid << "\n";
            if (cvp.cvid != CVID_Unknown)
                sii->paramGroup.set(cvp.cvid, cvp.value);
            
            cvp = translateSearchScore("expect", (*shit)->searchScore);
            std::cerr << "expect=" << cvp.cvid << "\n";
            if (cvp.cvid != CVID_Unknown)
                sii->paramGroup.set(cvp.cvid, cvp.value);
            
            
            sii->peptideEvidence.push_back(pepEv);

            // TODO handle precursorNeutralMass
            // TODO handle index/retentionTimeSec fields

            */
            SpectrumIdentificationResultPtr sirp(
                new SpectrumIdentificationResult());
            sirp->id = "SIR_"+lexical_cast<string>(indices->sir++);
            
            sirp->spectrumID = query->spectrum;
            sirp->spectrumIdentificationItem.push_back(sii);
            if (mzid->dataCollection.inputs.spectraData.size()>0)
                sirp->spectraDataPtr = mzid->dataCollection.inputs.
                    spectraData.at(0);
            else
                throw runtime_error("[Pep2MzIdent::translateQueries] no "
                                    "SpectraData");

            // Get the last added SpectrumIdentificationList, or if
            // one's has not been added, add one now.  
            SpectrumIdentificationListPtr sil;
            if (mzid->dataCollection.analysisData.
                spectrumIdentificationList.size() > 0)
            {
                sil = mzid->dataCollection.analysisData.
                    spectrumIdentificationList.back();
            }
            else
            {
                sil = SpectrumIdentificationListPtr(
                    new SpectrumIdentificationList("SIL_"+lexical_cast<string>(
                                                       indices->sil++)));
                mzid->dataCollection.analysisData.spectrumIdentificationList.
                    push_back(sil);
            }

            // Add the SpectrumIdentificationResult to the mzIdentML object.
            if (!sil->empty())
                sil->spectrumIdentificationResult.push_back(sirp);
        }
    }
}


MzIdentMLPtr Pep2MzIdent::translate()
{
    if (mzid.get() == NULL)
        mzid = MzIdentMLPtr(new MzIdentML());
    
    mzid->cvs.push_back(cv::cv("MS"));
    mzid->cvs.push_back(cv::cv("UO"));
   
    translateRoot();

    return mzid;
}

void Pep2MzIdent::earlyParameters(ParameterPtr parameter,
                                        MzIdentMLPtr mzid)
{
    /*
      Parameters handled in this function have a (*):
      
      CHARGE
      CLE
      DB
      FILE(*)
      FORMAT
      FORMVER
      INSTRUMENT
      INTERNALS
      IT_MODS
      ITOL(*)
      ITOLU(*)
      LICENSE
      MASS
      PEAK(handled later)
      PFA
      QUANTITATION
      REPORT
      REPTYPE
      RULES
      SEARCH
      TAXONOMY
      TOL(*)
      TOLU(*)
      USEREMAIL(*)
      USERID
      USERNAME(*)
      _mzML (ignored except for software)
     */
    vector<CVMapPtr>::const_iterator it = find(
        parameterMap.begin(), parameterMap.end(),
        shared_ptr<CVMap>(new StringMatchCVMap(parameter->name)));
    if (it != parameterMap.end())
    {
        // Put the value somewhere useful.
        CVTermInfo info = cvTermInfo((*it)->cvid);
    }
    else if (parameter->name == "USERNAME")
    {
        ContactPtr cp = find_id(mzid->auditCollection,
                                PERSON_DOC_OWNER);

        Person* person;

        if (cp.get() && dynamic_cast<Person*>(cp.get()))
            person = (Person*)cp.get();
        else
        {
            mzid->auditCollection.push_back(
                PersonPtr(new Person(PERSON_DOC_OWNER)));
            person = (Person*)mzid->auditCollection.back().get();

        }

        person->lastName = parameter->value;
    }
    else if (parameter->name == "USEREMAIL")
    {
        ContactPtr cp = find_id(mzid->auditCollection,
                                PERSON_DOC_OWNER);
        
        Person* person;
        
        if (cp.get() && dynamic_cast<Person*>(cp.get()))
            person = (Person*)cp.get();
        else
        {
            mzid->auditCollection.push_back(
                PersonPtr(new Person(PERSON_DOC_OWNER)));
            person = (Person*)mzid->auditCollection.back().get();
        }

        person->email = parameter->value;
    }
    else if (parameter->name == "FILE")
    {
        SpectraDataPtr sd(new SpectraData(
                              "SD_"+lexical_cast<string>(indices->sd++)));
        sd->location = parameter->value;

        CVID fileFormat = CVID_Unknown;
        CVID spectrumIDFormat = CVID_Unknown;
        
        if (fileExtension2Type(sd->location, fileFormat, spectrumIDFormat))
        {
            sd->fileFormat.set(fileFormat);
            sd->spectrumIDFormat.set(spectrumIDFormat);
        }
        mzid->dataCollection.inputs.spectraData.push_back(sd);
    }
    else if (parameter->name == "TOL")
    {
        if (mzid->analysisProtocolCollection.
            spectrumIdentificationProtocol.size()>0)
        {
            SpectrumIdentificationProtocolPtr sip =
                mzid->analysisProtocolCollection.
                spectrumIdentificationProtocol.at(0);

            CVParam cvp = sip->fragmentTolerance.
                cvParam(MS_search_tolerance_plus_value);

            sip->fragmentTolerance.set(MS_search_tolerance_plus_value,
                                       parameter->value,
                                       cvp.units);
            
            cvp = sip->fragmentTolerance.
                cvParam(MS_search_tolerance_minus_value);
            
            sip->fragmentTolerance.set(MS_search_tolerance_minus_value,
                                       parameter->value,
                                       cvp.units);
        }
    }
    else if (parameter->name == "TOLU")
    {
        if (mzid->analysisProtocolCollection.
            spectrumIdentificationProtocol.size()>0)
        {
            SpectrumIdentificationProtocolPtr sip =
                mzid->analysisProtocolCollection.
                spectrumIdentificationProtocol.at(0);

            CVParam cvp = sip->fragmentTolerance.
                cvParam(MS_search_tolerance_plus_value);
            
            sip->fragmentTolerance.set(MS_search_tolerance_plus_value,
                                       cvp.value,
                                       getCVID(parameter->value));

            cvp = sip->fragmentTolerance.
                cvParam(MS_search_tolerance_minus_value);
            
            sip->fragmentTolerance.set(MS_search_tolerance_minus_value,
                                       cvp.value,
                                       getCVID(parameter->value));
        }
    }
    else if (parameter->name == "ITOL")
    {
        if (mzid->analysisProtocolCollection.
            spectrumIdentificationProtocol.size()>0)
        {
            SpectrumIdentificationProtocolPtr sip =
                mzid->analysisProtocolCollection.
                spectrumIdentificationProtocol.at(0);

            CVParam cvp = sip->parentTolerance.
                cvParam(MS_search_tolerance_plus_value);

            sip->parentTolerance.set(MS_search_tolerance_plus_value,
                                       parameter->value,
                                       cvp.units);
            
            cvp = sip->parentTolerance.
                cvParam(MS_search_tolerance_minus_value);
            
            sip->parentTolerance.set(MS_search_tolerance_minus_value,
                                       parameter->value,
                                       cvp.units);
        }
    }
    else if (parameter->name == "ITOLU")
    {
        if (mzid->analysisProtocolCollection.
            spectrumIdentificationProtocol.size()>0)
        {
            SpectrumIdentificationProtocolPtr sip =
                mzid->analysisProtocolCollection.
                spectrumIdentificationProtocol.at(0);

            CVParam cvp = sip->parentTolerance.
                cvParam(MS_search_tolerance_plus_value);
            
            sip->parentTolerance.set(MS_search_tolerance_plus_value,
                                       cvp.value,
                                       getCVID(parameter->value));

            cvp = sip->parentTolerance.
                cvParam(MS_search_tolerance_minus_value);
            
            sip->parentTolerance.set(MS_search_tolerance_minus_value,
                                       cvp.value,
                                     getCVID(parameter->value));
        }
    }
    else if (istarts_with(parameter->name, "_mzML."))
    {
        // TODO make a _mzML parameter parsing tree

        // I don't see how this cuold possibly work.
        if (starts_with(parameter->name, "_mzML.softwareList.") &&
            starts_with(parameter->name, ".count"))
        {
            size_t idx = parameter->value.find_first_of(":");

            if (idx == string::npos)
                return;
        
            istringstream oss(parameter->value.substr(idx));
            size_t id;
            oss >> id;

            size_t start = mzid->analysisSoftwareList.size();
            for (size_t i=start; i<id; i++)
                mzid->analysisSoftwareList.push_back(
                    AnalysisSoftwarePtr(new AnalysisSoftware()));
        }
        else if (starts_with(parameter->name, "_mzML.softwareList.software."))
        {
            if (parameter->name.size() < 31)
                return;
            string number = parameter->name.substr(28, 2);

            istringstream oss(number);
            size_t idx;
            oss >> idx;

            if (idx < 1)
                return;
            else if (mzid->analysisSoftwareList.size() < idx)
                resizeSoftware(mzid->analysisSoftwareList, idx);
        
            if (ends_with(parameter->name, "id"))
            {
                mzid->analysisSoftwareList[idx-1]->id = parameter->value;
                CVID cvid = getCVID(parameter->value);
                if (cvid != CVID_Unknown)
                    mzid->analysisSoftwareList[idx-1]->softwareName.set(cvid);
            }
            else if (ends_with(parameter->name, "version"))
            {
                mzid->analysisSoftwareList[idx-1]->version = parameter->value;
            }
        }
    }
}

void Pep2MzIdent::lateParameters(ParameterPtr parameter,
                                       MzIdentMLPtr mzid)
{
    if (parameter->name == "USERNAME" ||
        parameter->name == "USEREMAIL" || 
        parameter->name == "FILE" ||
        parameter->name == "TOL" ||
        parameter->name == "TOLU" ||
        parameter->name == "ITOL" || 
        parameter->name == "ITOLU" ||
        starts_with(parameter->name, "_mzML.fileDescription.sourceFileList."
                    "sourceFile.cvParam") ||
        starts_with(parameter->name, "_mzML.referenceableParamGroupList."
                    "referenceableParamGroup.cvParam") || 
        starts_with(parameter->name, "_mzML.softwareList.") ||
        starts_with(parameter->name, "_mzML.softwareList.software."))
    {
        // These parameters have already been dealt with in earlyParameters.
        return;
    }
    else if (parameter->name == "PEAK")
    {
        if (mzid->analysisProtocolCollection.proteinDetectionProtocol.size()==0)
            mzid->analysisProtocolCollection.proteinDetectionProtocol.
                push_back(ProteinDetectionProtocolPtr(
                              new ProteinDetectionProtocol(
                                  "PDP_"+lexical_cast<string>(
                                      indices->pdp++))));
        
        ProteinDetectionProtocolPtr pdp = mzid->analysisProtocolCollection.
            proteinDetectionProtocol.back();
        // TODO eventually use guessAnalysisSoftware
        pdp->analysisSoftwarePtr  =AnalysisSoftwarePtr(new AnalysisSoftware("AS"));

        CVParam cvparam = guessThreshold(mzid->analysisSoftwareList);
        pdp->threshold.cvParams.push_back(cvparam);
        pdp->analysisSoftwarePtr = findSoftware(mzid->analysisSoftwareList,
                                                cvparam.cvid);
        pdp->analysisParams.set(MS_mascot_MaxProteinHits, parameter->value);
    }
    
    // TODO stick the rest in UserParam objects somewhere.
}

void Pep2MzIdent::earlyMetadata()
{
    const vector<SearchSummaryPtr>* ss = &_mspa->msmsRunSummary.searchSummary;
    for (vector<SearchSummaryPtr>::const_iterator sit = ss->begin();
         sit != ss->end(); sit++)
    {
        vector<ParameterPtr>& pp = (*sit)->parameters;
        for (vector<ParameterPtr>::const_iterator pit=pp.begin();
             pit != pp.end(); pit++)
        {
            earlyParameters(*pit, mzid);
        }
    }
}

void Pep2MzIdent::lateMetadata()
{
    const vector<SearchSummaryPtr>* ss = &_mspa->msmsRunSummary.searchSummary;
    for (vector<SearchSummaryPtr>::const_iterator sit = ss->begin();
         sit != ss->end(); sit++)
    {
        vector<ParameterPtr>& pp = (*sit)->parameters;
        for (vector<ParameterPtr>::const_iterator pit=pp.begin();
             pit != pp.end(); pit++)
        {
            lateParameters(*pit, mzid);
        }
    }
}

const string Pep2MzIdent::addPeptide(const SearchHitPtr sh, MzIdentMLPtr& mzid)
{
    // If we've already seen this sequence, continue on.
    if (find_if(mzid->sequenceCollection.peptides.begin(),
                mzid->sequenceCollection.peptides.end(),
                sequence_p(sh->peptide)) !=
        mzid->sequenceCollection.peptides.end())
        return "";
    
    PeptidePtr pp(new Peptide("PEP_"+lexical_cast<string>(indices->peptide++)));
    pp->id = sh->peptide;
    pp->peptideSequence = sh->peptide;
    
    mzid->sequenceCollection.peptides.push_back(pp);
    
    addModifications(*aminoAcidModifications, pp, mzid);

    return pp->id;
}


CVID Pep2MzIdent::getCVID(const string& name)
{
    CVID id = translator.translate(name);

    if (id == CVID_Unknown)
    {
        if (iequals(name, "mascot"))
        {
            id = MS_Mascot;
        }
        else if (iequals(name, "sequest"))
        {
            id = MS_Sequest;
        }
        else if (iequals(name, "phenyx"))
        {
            id = MS_Phenyx;
        }
        else if (iequals(name, "da"))
        {
            id = UO_dalton;
        }
    }
    
    return id;
}

void Pep2MzIdent::addFinalElements()
{
    SpectrumIdentificationPtr sip(new SpectrumIdentification("SI"));
    sip->activityDate = mzid->creationDate;

    sip->spectrumIdentificationProtocolPtr = mzid->analysisProtocolCollection.
        spectrumIdentificationProtocol.back();
    sip->spectrumIdentificationListPtr = mzid->dataCollection.
        analysisData.spectrumIdentificationList.back();

    for (vector<SpectraDataPtr>::const_iterator it=mzid->dataCollection.
             inputs.spectraData.begin();
         it != mzid->dataCollection.inputs.spectraData.end(); it++)
    {
        sip->inputSpectra.push_back(*it);
    }

    for (vector<SearchDatabasePtr>::const_iterator it=mzid->dataCollection.
             inputs.searchDatabase.begin();
         it != mzid->dataCollection.inputs.searchDatabase.end(); it++)
    {
        sip->searchDatabase.push_back(*it);
    }

    mzid->analysisCollection.spectrumIdentification.push_back(sip);
}

} // namespace pwiz 
} // namespace mziddata 
