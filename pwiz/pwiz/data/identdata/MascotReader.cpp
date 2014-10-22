//
// $Id$
//
//
// Origional author: Robert Burke <robert.burke@proteowizard.org>
//
// Copyright 2010 Spielberg Family Center for Applied Proteomics
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

#include "MascotReader.hpp"
#include "pwiz/data/identdata/MzidPredicates.hpp"
#include "pwiz/utility/misc/Std.hpp"
#include "pwiz/utility/misc/Filesystem.hpp"
#include "pwiz/utility/misc/DateTime.hpp"
#include "pwiz/utility/misc/Singleton.hpp"
#include "boost/xpressive/xpressive.hpp"
#include "pwiz/data/common/cv.hpp"
#include "pwiz/data/identdata/TextWriter.hpp"
#include <boost/tokenizer.hpp>
#include <boost/regex.hpp>

// msparser assumes these are defined
#ifndef INT64
#define INT64 __int64_t
#endif // INT64

#ifndef UINT64
#define UINT64 __uint64_t
#endif // UINT64

#include "msparser.hpp"


namespace {

static const char* modMappings[] = {""};

struct terminfo_name_p
{
    string mine;
    terminfo_name_p(const string mine) : mine(mine){}
    bool operator()(const pwiz::cv::CVTermInfo* yours)
    {
        return yours && mine == yours->name;
    }
};

struct Indices
{
    Indices()
        : enzyme(0)
    {
    }
    
    string makeIndex(const string& prefix, size_t& index)
    {
        ostringstream oss;
        oss << prefix << index++;

        return oss.str();
    }

    // Will there ever be more than one enzyme?
    size_t enzyme;
};


template<class T>
struct id_p
{
    const string a;
    
    id_p(const string& _a) : a(_a) {}

    bool operator()(const T& b) {return a == b.id;}
    bool operator()(const shared_ptr<T>& b) {return a == b->id;}
};


struct NativeIdTranslator : public boost::singleton<NativeIdTranslator>
{
    NativeIdTranslator(boost::restricted)
    {
        using namespace boost::xpressive;
        using namespace pwiz::cv;

        BOOST_FOREACH(CVID cvid, pwiz::cv::cvids())
        {
            if (!cvIsA(cvid, MS_native_spectrum_identifier_format))
                continue;

            string format = cvTermInfo(cvid).def;
            if (!bal::icontains(format, "xsd"))
                continue;

            sregex nativeIdFormatRegex = sregex::compile(".*?(\\S+=\\S+( \\S+=\\S+)*)\\.?");
            smatch what;
            if (!regex_match(format, what, nativeIdFormatRegex))
                continue;

            format = what[1].str();
            bal::trim_right_if(format, bal::is_any_of("."));
            bal::ireplace_all(format, "xsd:nonNegativeInteger", "\\d+");
            bal::ireplace_all(format, "xsd:positiveInteger", "\\d+");
            bal::ireplace_all(format, "xsd:Long", "\\d+");
            bal::ireplace_all(format, "xsd:string", "\\S+");
            bal::ireplace_all(format, "xsd:IDREF", "\\S+");
            nativeIdRegexAndFormats.push_back(make_pair(sregex::compile(format), cvid));
        }
    }

    pwiz::cv::CVID translate(const string& id)
    {
        using namespace boost::xpressive;

        smatch what;
        BOOST_FOREACH(const RegexFormatPair& pair, nativeIdRegexAndFormats)
            if (regex_match(id, what, pair.first))
                return pair.second;
        return pwiz::cv::CVID_Unknown;
    }

    private:
    typedef pair<boost::xpressive::sregex, pwiz::cv::CVID> RegexFormatPair;
    vector<RegexFormatPair> nativeIdRegexAndFormats;
};

bool hasValidFlankingSymbols(const pwiz::identdata::PeptideEvidence& pe)
{
    static string invalidAlphabet("BJOZ");
    return ((pe.pre >= 'A' && pe.pre <= 'Z' && invalidAlphabet.find(pe.pre) == string::npos) || pe.pre == '-' || (pe.isDecoy && pe.pre == '?')) &&
           ((pe.post >= 'A' && pe.post <= 'Z' && invalidAlphabet.find(pe.post) == string::npos) || pe.post == '-' || (pe.isDecoy && pe.post == '?'));
}

} // anonymous namespace


namespace pwiz {
namespace identdata {

using pwiz::cv::CVTermInfo;
using namespace matrix_science;
using namespace pwiz::util;

//
// MascotReader::Impl
//
class MascotReader::Impl
{
public:
    Impl()
        :varmodPattern("(.*) \\((Protein)?\\s*([NC]-term)?\\s*(.*?)\\)"),
         varmodListOfChars("([A-Z]+)")
    {}
    
    void read(const string& filename, const string& head, IdentData& result, const Reader::Config& config)
    {
        if (config.iterationListenerRegistry && config.iterationListenerRegistry->broadcastUpdateMessage(IterationListener::UpdateMessage(0, 0, "opening Mascot DAT file")) == IterationListener::Status_Cancel)
            return;

        ms_mascotresfile file(filename.c_str());

        if (file.isValid())
        {
            // We get this for free just by being in this function.
            addMzid(file, result);
            addMascotSoftware(file, result);
            addSearchDatabases(file, result);
            inputData(file, result, filename);
            searchParameters(file, result);

            if (!config.ignoreSequenceCollectionAndAnalysisData)
                fillSpectrumIdentificationList(file, result, config.iterationListenerRegistry);

            searchInformation(file, result);
        }
    }

    void addMzid(ms_mascotresfile& file, IdentData& result)
    {
        result.id = "MZID";
        result.name = file.params().getCOM();
    }
    
    /**
     * Sets the measure cvParams used later in fillFragmentation().
     */
    void fillFragmentationTable(vector<MeasurePtr>& fragmentationTable)
    {
        MeasurePtr measure(new Measure(mz_id));
        measure->set(MS_product_ion_m_z);
        fragmentationTable.push_back(measure);

        measure = MeasurePtr(new Measure(intensity_id));
        measure->set(MS_product_ion_intensity);
        fragmentationTable.push_back(measure);

        measure = MeasurePtr(new Measure(error_id));
        measure->set(MS_product_ion_m_z_error);
        fragmentationTable.push_back(measure);
    }
    
    bool fillFragmentation(vector< pair<double, double> > peaks,
                           vector<MeasurePtr>& measures,
                           IonTypePtr ionType)
    {
		if (peaks.empty())
            return false;
        
        FragmentArrayPtr mzFa(new FragmentArray());
        FragmentArrayPtr intFa(new FragmentArray());
        vector<double> mzArray, intensityArray;

        // Add the Measure_ref that indicates what kind of a list each
        // FragmentArray is.
        typedef vector<MeasurePtr>::const_iterator measure_iterator;
        measure_iterator mit = find_if(measures.begin(),
                                       measures.end(), id_p<Measure>(mz_id));
        if (mit != measures.end())
            mzFa->measurePtr = *mit;
        
        mit = find_if(measures.begin(), measures.end(),
                      id_p<Measure>(intensity_id));

        if (mit != measures.end())
            intFa->measurePtr = *mit;

        // For each pair, split them into m/z and intensity.
        typedef pair<double, double> peak_pair;
        BOOST_FOREACH(peak_pair peak, peaks)
        {
            mzArray.push_back(peak.first);
            intensityArray.push_back(peak.second);
        }

        // Once filled, each FragmentArray is put into the ionType's
        // fragmentArray vector.

        //copy(mzArray.begin(), mzArray.end(), mzFa->values.begin());
		mzFa->values.insert(mzFa->values.begin(), mzArray.begin(), mzArray.end());
        ionType->fragmentArray.push_back(mzFa);

        //copy(intensityArray.begin(), intensityArray.end(), intFa->values.begin());
		intFa->values.insert(intFa->values.begin(), intensityArray.begin(), intensityArray.end());
        ionType->fragmentArray.push_back(intFa);

        return true;
    }
    
    // Add Mascot to the analysis software
    void addMascotSoftware(ms_mascotresfile & file, IdentData& mzid)
    {
        AnalysisSoftwarePtr as(new AnalysisSoftware("AS_0"));
        as->version = file.getMascotVer();
        as->softwareName.set(MS_Mascot);
        mzid.analysisSoftwareList.push_back(as);
    }

    SpectrumIdentificationProtocolPtr getSpectrumIdentificationProtocol(IdentData& mzid)
    {
        SpectrumIdentificationProtocolPtr sip;
        
        if (mzid.analysisProtocolCollection.
            spectrumIdentificationProtocol.size())
        {
            sip = mzid.analysisProtocolCollection.
                spectrumIdentificationProtocol.at(0);
        }
        else
        {
            sip = SpectrumIdentificationProtocolPtr(
                new SpectrumIdentificationProtocol("SIP"));

            // Assume we've already called the addMascot method
            sip->analysisSoftwarePtr =
                mzid.analysisSoftwareList.at(0);
            
            mzid.analysisProtocolCollection.
                spectrumIdentificationProtocol.
                push_back(sip);
        }

        return sip;
    }
    
    // Add the FASTA file search database
    void addSearchDatabases(ms_mascotresfile & file, IdentData& mzid)
    {
        for (int i=1; i<=file.params().getNumberOfDatabases();i++)
        {
            SearchDatabasePtr sd(new SearchDatabase(file.params().getDB(i)));
            sd->location = file.getFastaPath(i);
            sd->version = file.getFastaVer(i);
            sd->releaseDate = file.getFastaVer(i);
            sd->numDatabaseSequences = file.getNumSeqs(i);
            sd->numResidues = file.getNumResidues(i);
            // TODO add a CVParam/UserParam w/ the name of the database.
            mzid.dataCollection.inputs.searchDatabase.push_back(sd);
        }
    }

    CVID getToleranceUnits(const string& units)
    {
        CVID cvid = CVID_Unknown;
        
        if (bal::istarts_with(units, "da") || bal::iequals(units, "u") || bal::iequals(units, "unified atomic mass unit"))
            cvid = UO_dalton;
        else if (bal::iequals(units, "kda"))
            cvid = UO_kilodalton;
        else if (bal::iequals(units, "ppm"))
            cvid = UO_parts_per_million;
        return cvid;
    }

    EnzymePtr getEnzyme(ms_searchparams& msp)
    {
        EnzymePtr ez(new Enzyme(indices.makeIndex("EZ_", indices.enzyme)));

        ez->missedCleavages = msp.getPFA();
        
        // TODO add other enzymes
        if (msp.getCLE() == "Trypsin")
        {
            ez->enzymeName.set(MS_Trypsin);
        }
        else
            cerr << "[MascotReader::Impl::getEnzyme()] Unhandled enzyme "
                 << msp.getCLE() << endl;
        
        return ez;
    }
    
    void addUser(ms_searchparams& msp, IdentData& mzid)
    {
        PersonPtr user(new Person(owner_person_id));
        user->lastName = msp.getUSERNAME();

        if (!msp.getUSEREMAIL().empty())
            user->set(MS_contact_email,msp.getUSEREMAIL());

        mzid.auditCollection.push_back(user);

        mzid.provider.id = provider_id;
        mzid.provider.contactRolePtr = ContactRolePtr(new ContactRole());
        mzid.provider.contactRolePtr->contactPtr = user;
        mzid.provider.contactRolePtr->cvid = MS_researcher;
    }

    void addMassTable(ms_searchparams& p, IdentData& mzid)
    {
        SpectrumIdentificationProtocolPtr sip =
            getSpectrumIdentificationProtocol(mzid);

        MassTablePtr massTable(new MassTable("MT"));

        for (char ch='A'; ch <= 'Z'; ch++)
        {
            ResiduePtr residue(new Residue());
            residue->code = ch;
            residue->mass = p.getResidueMass(ch);
            massTable->residues.push_back(residue);
        }
        sip->massTable.push_back(massTable);
        
    }

    void parseTaxonomy(const string& mascot_tax, string& scientific, string& common)
    {
        const char* pattern = "[ \\.]*([\\w ]+)[ ]*\\((\\w+)\\).*";

        boost::regex namesPattern(pattern);

        boost::cmatch what;
        if (boost::regex_match(mascot_tax.c_str(), what, namesPattern))
        {
            scientific.assign(what[1].first, what[1].second);
            common.assign(what[2].first, what[2].second);
        }
    }
    
    void addAnalysisProtocol(ms_mascotresfile & file, IdentData& mzid)
    {
        ms_searchparams& p = file.params();
        
        SpectrumIdentificationProtocolPtr sip =
            getSpectrumIdentificationProtocol(mzid);
        
        sip->parentTolerance.set(MS_search_tolerance_plus_value, p.getTOL(), getToleranceUnits(p.getTOLU()));
        sip->parentTolerance.set(MS_search_tolerance_minus_value, p.getTOL(), getToleranceUnits(p.getTOLU()));

        sip->fragmentTolerance.set(MS_search_tolerance_plus_value, p.getITOL(), getToleranceUnits(p.getITOLU()));
        sip->fragmentTolerance.set(MS_search_tolerance_minus_value, p.getITOL(), getToleranceUnits(p.getITOLU()));

        EnzymePtr ez = getEnzyme(p);
        
        sip->enzymes.enzymes.push_back(ez);

        if (file.anyMSMS())
            sip->searchType = MS_ms_ms_search;

        if (file.anyPMF())
            sip->searchType = MS_pmf_search;

        // TODO Is SQ == MIS as documented?
        if (file.anySQ())
            sip->searchType = MS_ms_ms_search;

        // TODO add taxonomy search mod
        if (p.getTAXONOMY().size()>0)
        {
            string scientific, common;
            parseTaxonomy(p.getTAXONOMY(), scientific, common);

            if (!scientific.empty() || !common.empty())
            {
                FilterPtr taxFilter(new Filter());
                taxFilter->filterType.set(MS_DB_filter_taxonomy);
                if (!scientific.empty()) taxFilter->include.set(MS_taxonomy__scientific_name, scientific);
                if (!common.empty()) taxFilter->include.set(MS_taxonomy__common_name, common);
                sip->databaseFilters.push_back(taxFilter);
            }
        }
    }

    void addSpectraData(ms_searchparams& p, IdentData& mzid)
    {
        SpectraDataPtr spectraData(new SpectraData());
        spectraData->id = "SD_1";
        mzid.dataCollection.inputs.spectraData.push_back(spectraData);

        // if there is no input filename, make do with the placeholder created above
        if (p.getFILENAME().empty())
            return;

        bfs::path searchedFilepath = p.getFILENAME();

        spectraData->name = searchedFilepath.filename().string();
        spectraData->location = searchedFilepath.has_parent_path() ? searchedFilepath.parent_path().string() : spectraData->name;

        if (p.getFORMAT() == "Mascot generic")
            spectraData->fileFormat = MS_Mascot_MGF_format;

        // set the main document's id according to the input file name
        mzid.id = bfs::basename(searchedFilepath.filename());
    }

    void decryptMod(const string& mod, double mdelta, ms_searchparams& p, IdentData& mzid)
    {
        SpectrumIdentificationProtocolPtr sip = getSpectrumIdentificationProtocol(mzid);

        boost::cmatch what, where;
        if (boost::regex_match(mod.c_str(), what, varmodPattern))
        {
            bool proteinTerminal = what[2].matched;
            bool nTerminal = what[3].matched && what[3].str() == "N-term";
            bool cTerminal = !nTerminal && what[3].matched && what[3].str() == "C-term";
            string residues = what[4].matched ? what[4].str() : "";
            
            const CVTermInfo* cvt = getTermInfoByName(what[1].str());
            
            SearchModificationPtr sm(new SearchModification());
            sm->fixedMod = false;
            if (mdelta)
                sm->massDelta = mdelta;

            if (cvt)
                sm->cvParams.push_back(CVParam(cvt->cvid));

            if (boost::regex_match(residues.c_str(), where, varmodListOfChars))
                sm->residues.assign(where[1].first, where[1].second);

            if (proteinTerminal)
            {
                if (nTerminal) sm->specificityRules = CVParam(MS_modification_specificity_protein_N_term);
                else if (cTerminal) sm->specificityRules = CVParam(MS_modification_specificity_protein_C_term);
                else throw runtime_error("[MascotReader::decryptMod] parsed protein terminal mod but could not determine which terminus: " + mod);
            }
            else
            {
                if (nTerminal) sm->specificityRules = CVParam(MS_modification_specificity_peptide_N_term);
                else if (cTerminal) sm->specificityRules = CVParam(MS_modification_specificity_peptide_C_term);
            }

            sip->modificationParams.push_back(sm);
        }
    }
    
    void addModifications(ms_searchparams& p, IdentData& mzid)
    {
        vector<string> mods;

        SpectrumIdentificationProtocolPtr sip =
            getSpectrumIdentificationProtocol(mzid);

        // Adding variable modifications
        int i=1;
        while (p.getVarModsName(i).length())
        {
            // Variable mod name
            string mod = p.getVarModsName(i);
            // Variable mod delta
            double mdelta = p.getVarModsDelta(i);
            // Variable mod neutral
            //double neutral = p.getVarModsNeutralLoss(i);
            i++;

            decryptMod(mod, mdelta, p, mzid);
        }

        // Add fixed modifications
        typedef boost::tokenizer< boost::char_separator<char> > tokenizer;
        boost::char_separator<char> sep(",");
		string s(p.getMODS());
        tokenizer tokens(s, sep);
        for (tokenizer::iterator tok_iter = tokens.begin();
			tok_iter != tokens.end(); ++tok_iter)
            decryptMod(*tok_iter, 0, p, mzid);
    }

    void searchInformation(ms_mascotresfile & file, IdentData& mzid)
    {
        mzid.creationDate = pwiz::util::encode_xml_datetime(bpt::second_clock::universal_time());

        mzid.analysisCollection.spectrumIdentification.push_back(SpectrumIdentificationPtr(new SpectrumIdentification("SI")));
        SpectrumIdentification& si = *mzid.analysisCollection.spectrumIdentification.back();

        si.activityDate = pwiz::util::encode_xml_datetime(bpt::from_time_t((time_t)file.getDate()));
        si.inputSpectra = mzid.dataCollection.inputs.spectraData;
        si.searchDatabase = mzid.dataCollection.inputs.searchDatabase;
        si.spectrumIdentificationListPtr = getSpectrumIdentificationList(mzid);
        si.spectrumIdentificationProtocolPtr = getSpectrumIdentificationProtocol(mzid);
    }

    /**
     * Handles all the input parameters.
     */
    void searchParameters(ms_mascotresfile & file, IdentData& mzid)
    {
        addUser(file.params(), mzid);
        addMassTable(file.params(), mzid);
        addAnalysisProtocol(file, mzid);
        addSpectraData(file.params(), mzid);
        addModifications(file.params(), mzid);
        guessTitleFormatRegex(file, mzid);
    }

    void getModifications(PeptidePtr peptide, const string& peptideMods, ms_searchparams& searchparam, ms_peptide* pep)
    {
        for (size_t i = 0; i < peptideMods.size(); i++)
        {
            int mod_idx=0;
            
            char m = peptideMods[i];

            // Find out if there's a modification
            if (m>='A' && m<='Z')
                mod_idx = 10 + ((int)m-'A');
            else if (m>='0' && m<='9')
                mod_idx = (int)m-'0';

            if (mod_idx == 0)
                continue;

            if (searchparam.getVarModsDelta(mod_idx) == 0)
                continue;
            
            // Find the modification and add it.
            ModificationPtr modification(new Modification());
            modification->location = i;
            modification->monoisotopicMassDelta = searchparam.getVarModsDelta(mod_idx);
            if (i > 0 && i < peptide->peptideSequence.length())
                modification->residues.push_back(peptide->peptideSequence[i-1]);
            if (!modification->empty())
                peptide->modification.push_back(modification);
        }
    }
    
    SpectrumIdentificationListPtr getSpectrumIdentificationList(IdentData& mzid)
    {
        if (mzid.dataCollection.analysisData.spectrumIdentificationList.empty())
            mzid.dataCollection.analysisData.spectrumIdentificationList.push_back(SpectrumIdentificationListPtr(new SpectrumIdentificationList("SIL")));
        
        return mzid.dataCollection.analysisData.spectrumIdentificationList.back();
    }

    void guessTitleFormatRegex(ms_mascotresfile& file, IdentData& mzid)
    {
        string firstSpectrumID = ms_inputquery(file, 1).getStringTitle(true);
        boost::smatch what;
        boost::regex testRegexes[] =
        {
            boost::regex(".*?NativeID:\"(.+?)\".*"),
            boost::regex(".*?\\.(\\d+)\\.\\d+(\\.\\d)?.*"),
            boost::regex("(.+)")
        };
        BOOST_FOREACH(boost::regex& re, testRegexes)
            if (boost::regex_match(firstSpectrumID, what, re))
            {
                titleRegex = re;
                break;
            }

        if (titleRegex.empty())
            throw runtime_error("[MascotReader::guessTitleFormatRegex] title parsed from Mascot DAT is empty or does not correspond to a supported id format");

        string firstNativeID(what[1].first, what[1].second);

        // now try to parse the nativeID into a known nativeID format and set it accordingly in spectraData
        CVID defaultNativeIDFormat = file.params().getFORMAT() == "Mascot generic" ? MS_single_peak_list_nativeID_format : MS_scan_number_only_nativeID_format;
        CVID parsedNativeIDFormat = NativeIdTranslator::instance->translate(firstNativeID);
        if (parsedNativeIDFormat == CVID_Unknown)
            parsedNativeIDFormat = defaultNativeIDFormat;

        mzid.dataCollection.inputs.spectraData.back()->spectrumIDFormat = parsedNativeIDFormat;

    }

    void fillSpectrumIdentificationList(ms_mascotresfile& file, IdentData& mzid, const IterationListenerRegistry* ilr)
    {
        if (ilr && ilr->broadcastUpdateMessage(IterationListener::UpdateMessage(0, 0, "creating peptide summary")) == IterationListener::Status_Cancel)
            return;
        ms_peptidesummary results(file, ms_peptidesummary::MSRES_GROUP_PROTEINS | ms_peptidesummary::MSRES_SHOW_SUBSETS, 0, INT_MAX, 0, 0, 0, 0, 0);
        ms_searchparams searchparam(file);
        ms_peptide* peptideHit;
        boost::smatch titleWhat;

        int numQueries = file.getNumQueries();
        int maxRank = results.getMaxRankValue();

        map<string, PeptidePtr> peptideIndex;
        map<string, DBSequencePtr> dbSequenceIndex;
        map<string, PeptideEvidencePtr> peptideEvidenceIndex;
        map<string, vector<SpectrumIdentificationItemPtr> > siiByProtein;

        SpectrumIdentificationListPtr silp = getSpectrumIdentificationList(mzid);
        SpectrumIdentificationList& sil = *silp;

        int iterationIndex = 0;
        int iterationCount = numQueries;

        for (int i = 1; i <= numQueries; ++i)
        {
            if (ilr && ilr->broadcastUpdateMessage(IterationListener::UpdateMessage(iterationIndex++, iterationCount, "reading spectrum queries")) == IterationListener::Status_Cancel)
                return;

            SpectrumIdentificationResultPtr sirp(new SpectrumIdentificationResult);
            SpectrumIdentificationResult& sir = *sirp;

            string sirIndex = lexical_cast<string>(sil.spectrumIdentificationResult.size()+1);
            sir.id = "SIR_" + sirIndex;

            ms_inputquery query(file, i);
            string scans = query.getScanNumbers(); if (!scans.empty()) sir.set(MS_peak_list_scans, scans);
            string rawscans = query.getRawScans(); if (!rawscans.empty()) sir.set(MS_peak_list_raw_scans, rawscans);

            string title = query.getStringTitle(true);
            if (boost::regex_match(title, titleWhat, titleRegex))
                sir.spectrumID.assign(titleWhat[1].first, titleWhat[1].second);
            else
                throw runtime_error("[MascotReader::fillSpectrumIdentificationList] unable to parse spectrum title: " + title);

            string rtInSeconds = query.getRetentionTimes();
            if (!rtInSeconds.empty())
                sir.set(MS_scan_start_time, rtInSeconds, UO_second);

            for (int j = 0; j <= maxRank; ++j)
            {
                if (!results.getPeptide(i, j, peptideHit) || !peptideHit)
                    continue;

                string peptideSequence = peptideHit->getPeptideStr();
                if (peptideSequence.empty())
                    continue;

                SpectrumIdentificationItemPtr siip(new SpectrumIdentificationItem);
                SpectrumIdentificationItem& sii = *siip;

                sii.id = "SII_" + sirIndex + "_" + lexical_cast<string>(sir.spectrumIdentificationItem.size() + 1);
                sii.chargeState = peptideHit->getCharge();
                sii.experimentalMassToCharge = peptideHit->getMrExperimental() / sii.chargeState;
                sii.calculatedMassToCharge = peptideHit->getMrCalc() / sii.chargeState;
                sii.rank = peptideHit->getPrettyRank();
                sii.passThreshold = true;

                double ionsScore = peptideHit->getIonsScore();
                sii.set(MS_Mascot_score, peptideHit->getIonsScore());
                sii.set(MS_Mascot_expectation_value, results.getPeptideExpectationValue(ionsScore, i));
                sii.set(MS_Mascot_identity_threshold, results.getPeptideIdentityThreshold(i, 20));
                sii.set(MS_Mascot_homology_threshold, results.getHomologyThreshold(i, 20));

                int totalIons = peptideHit->getPeaksUsedFromIons1() + peptideHit->getPeaksUsedFromIons2() + peptideHit->getPeaksUsedFromIons3();
                sii.set(MS_number_of_unmatched_peaks, totalIons - peptideHit->getNumIonsMatched());
                sii.set(MS_number_of_matched_peaks, peptideHit->getNumIonsMatched());

                string peptideMods = peptideHit->getVarModsStr();

                PeptidePtr& peptide = peptideIndex.insert(make_pair(peptideSequence + "_" + peptideMods, PeptidePtr())).first->second;

                bool newPeptide = false;

                // if peptide is null, the new PeptidePtr was inserted
                if (!peptide)
                {
                    newPeptide = true;
                    peptide.reset(new Peptide);
                    peptide->peptideSequence = peptideSequence;
                    peptide->id = "PEP_" + lexical_cast<string>(mzid.sequenceCollection.peptides.size() + 1);
                    getModifications(peptide, peptideMods, searchparam, peptideHit);
                }

                sii.peptidePtr = peptide;

                int numProteins = peptideHit->getNumProteins();
                for (int k = 0; k <= numProteins; ++k)
                {
                    const ms_protein* proteinHit = peptideHit->getProtein(k);
                    if (!proteinHit)
                        continue;

                    std::string accession = proteinHit->getAccession();
                    siiByProtein[accession].push_back(siip);

                    DBSequencePtr& dbSequence = dbSequenceIndex.insert(make_pair(accession, DBSequencePtr())).first->second;

                    // if dbSequence is null, the new DBSequencePtr was inserted
                    if (!dbSequence)
                    {
                        dbSequence.reset(new DBSequence);
                        mzid.sequenceCollection.dbSequences.push_back(dbSequence);
                        dbSequence->accession = accession;
                        dbSequence->id = "DBSeq_" + lexical_cast<string>(mzid.sequenceCollection.dbSequences.size());
                        string description = results.getProteinDescription(accession.c_str());
                        if (!description.empty())
                            dbSequence->set(MS_protein_description, description);
                        dbSequence->searchDatabasePtr = mzid.dataCollection.inputs.searchDatabase.back();
                    }

                    PeptideEvidencePtr& peptideEvidence = peptideEvidenceIndex.insert(make_pair(accession + "_" + peptideSequence, PeptideEvidencePtr())).first->second;

                    // if peptideEvidence is null, the new PeptideEvidencePtr was inserted
                    if (!peptideEvidence)
                    {
                        peptideEvidence.reset(new PeptideEvidence);
                        peptideEvidence->dbSequencePtr = dbSequence;
                        peptideEvidence->peptidePtr = peptide;
                        peptideEvidence->id = "PE_" + dbSequence->id + "_" + peptide->id;
                        
                        int pepNumber = proteinHit->getPepNumber(i, j);
                        peptideEvidence->pre = proteinHit->getPeptideResidueBefore(pepNumber);
                        peptideEvidence->post = proteinHit->getPeptideResidueAfter(pepNumber);

                        if (!hasValidFlankingSymbols(*peptideEvidence))
                        {
                            peptideEvidence.reset();
                            continue;
                        }

                        peptideEvidence->start = proteinHit->getPeptideStart(pepNumber);
                        peptideEvidence->end = proteinHit->getPeptideEnd(pepNumber);
                        mzid.sequenceCollection.peptideEvidence.push_back(peptideEvidence);
                    }

                    sii.peptideEvidencePtr.push_back(peptideEvidence);
                }

                // only add the Peptide and SpectrumIdentificationItem if there were protein hits
                if (!sii.peptideEvidencePtr.empty())
                {
                    sir.spectrumIdentificationItem.push_back(siip);
                    if (newPeptide)
                        mzid.sequenceCollection.peptides.push_back(peptide);
                }
            }

            if (!sir.spectrumIdentificationItem.empty())
                sil.spectrumIdentificationResult.push_back(sirp);
        } // end query loop

        // final update
        if (ilr && ilr->broadcastUpdateMessage(IterationListener::UpdateMessage(iterationCount-1, iterationCount, "reading spectrum queries")) == IterationListener::Status_Cancel)
            return;


        // fill ProteinDetectionList (uses the indexes created by filling the SpectrumIdentificationList above)
        ProteinDetectionListPtr& pdl = mzid.dataCollection.analysisData.proteinDetectionListPtr;
        if (!pdl)
            pdl.reset(new ProteinDetectionList("PDL_1"));

        int numProteinHits = results.getNumberOfHits();
        for (int i = 1; i <= numProteinHits; ++i)
        {
            if (ilr && ilr->broadcastUpdateMessage(IterationListener::UpdateMessage(i-1, numProteinHits, "reading protein groups")) == IterationListener::Status_Cancel)
                return;

            string pagIndex = lexical_cast<string>(pdl->proteinAmbiguityGroup.size() + 1);
            ProteinAmbiguityGroupPtr pag(new ProteinAmbiguityGroup("PAG_" + pagIndex));
            pdl->proteinAmbiguityGroup.push_back(pag);

            // FIXME: this iteration through memberNumber does not enumerate the "sameset" proteins as intended; only the "lead" protein is being created
            int memberNumber = 1;
            ms_protein* proteinHit;
            while ((proteinHit = results.getHit(i, memberNumber)) != NULL) // returns null when there are no memberNumbers
            {
                string accession = proteinHit->getAccession();
                ProteinDetectionHypothesisPtr pdh(new ProteinDetectionHypothesis("PDH_" + pagIndex + "_" + lexical_cast<string>(pag->proteinDetectionHypothesis.size() + 1), accession));
                pag->proteinDetectionHypothesis.push_back(pdh);

                pdh->set(MS_Mascot_score, proteinHit->getScore());
                //pdh->set(MS_sequence_coverage, proteinHit->getCoverage() / /* TODO: how to get length? */);

                map<string, PeptideEvidencePtr>::const_iterator itr = peptideEvidenceIndex.lower_bound(accession);
                while (itr != peptideEvidenceIndex.end() && bal::starts_with(itr->first, accession))
                {
                    pdh->peptideHypothesis.push_back(PeptideHypothesis());
                    PeptideHypothesis& ph = pdh->peptideHypothesis.back();
                    ph.peptideEvidencePtr = itr->second;
                    ph.spectrumIdentificationItemPtr = siiByProtein[accession];
                    ++itr;
                }
                ++memberNumber;
            }
        }

        // final update
        if (ilr && ilr->broadcastUpdateMessage(IterationListener::UpdateMessage(numProteinHits-1, numProteinHits, "reading protein groups")) == IterationListener::Status_Cancel)
            return;
    }

    void inputData(ms_mascotresfile & file, IdentData& mzid, const string& filename)
    {
        // add source file
        SourceFilePtr sourceFile(new SourceFile());
        sourceFile->id = "SF_1";
        sourceFile->location = bfs::path(filename).generic_string();

        //display input data via inputquery get functions

        /*SpectrumIdentificationItemPtr sii(new SpectrumIdentificationItem());
        
        ms_inputquery q(file, 1);
        
        vector<MeasurePtr> fragmentationTable;
        fillFragmentationTable(fragmentationTable);
        
        for (int j=0; j<q.getNumVals(); j++)
        {
            IonTypePtr ionType(new IonType());
            if (fillFragmentation(q.getPeakList(j), fragmentationTable, ionType))
                sii->fragmentation.push_back(ionType);
        }*/
    }


    const CVTermInfo* getTermInfoByName(const string& name)
    {
        if (unimods.empty())
            generateUNIMOD(unimods);

        terminfo_name_p tn_p(name);

        typedef vector<const CVTermInfo*>::const_iterator terminfo_cit;
        terminfo_cit ci= find_if(unimods.begin(), unimods.end(), tn_p);

        if (ci != unimods.end())
            return *ci;

        return NULL;
    }
    
    void generateUNIMOD(vector<const CVTermInfo*> &cvInfos)
    {
        typedef vector<CVID>::const_iterator cvid_iterator;
        
        for(cvid_iterator ci=cvids().begin(); ci!=cvids().end(); ci++)
        {
            if (cvIsA(*ci, UNIMOD_unimod_root_node))
                cvInfos.push_back(&(cvTermInfo(*ci)));
        }
    }
    
private:
    Indices indices;
    vector<const CVTermInfo*> unimods;
    
    boost::regex varmodPattern;
    boost::regex varmodListOfChars;
    boost::regex titleRegex;
    
    static const char* owner_person_id;
    static const char* provider_id;
    static const char* mz_id;
    static const char* intensity_id;
    static const char* error_id;
};

const char* MascotReader::Impl::owner_person_id = "doc_owner_person";
const char* MascotReader::Impl::provider_id = "provider";
const char* MascotReader::Impl::mz_id = "mz_id";
const char* MascotReader::Impl::intensity_id = "intensity_id";
const char* MascotReader::Impl::error_id = "error_id";

//
// MascotReader::MascotReader
//
MascotReader::MascotReader()
    : pimpl(new MascotReader::Impl())
{
}

//
// MascotReader::identify
//
string MascotReader::identify(const string& filename,
                              const string& head) const
{
    ms_datfile file(filename.c_str());
    if (file.isValid())
        return getType();

    return "";
}

//
// MascotReader::read
//
void MascotReader::read(const string& filename,
                        const string& head,
                        IdentData& result,
                        const Reader::Config& config) const
{
    pimpl->read(filename, head, result, config);
}

//
// MascotReader::read
//
void MascotReader::read(const string& filename,
                        const string& head,
                        IdentDataPtr& result,
                        const Reader::Config& config) const
{
    if (result.get())
        read(filename, head, *result, config);
}

//
// MascotReader::read
//
void MascotReader::read(const string& filename,
                        const string& head,
                        vector<IdentDataPtr>& results,
                        const Reader::Config& config) const
{
    results.push_back(IdentDataPtr(new IdentData));
    read(filename, head, results.back(), config);
}


} // namespace pwiz 
} // namespace identdata 

