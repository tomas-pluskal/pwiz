//
// $Id$
//
// The contents of this file are subject to the Mozilla Public License
// Version 1.1 (the "License"); you may not use this file except in
// compliance with the License. You may obtain a copy of the License at
// http://www.mozilla.org/MPL/
//
// Software distributed under the License is distributed on an "AS IS"
// basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
// License for the specific language governing rights and limitations
// under the License.
//
// The Original Code is the IDPicker project.
//
// The Initial Developer of the Original Code is Matt Chambers.
//
// Copyright 2010 Vanderbilt University
//
// Contributor(s):
//


#include "../Lib/SQLite/sqlite3pp.h"
#include "pwiz/utility/misc/Std.hpp"
#include "pwiz/utility/misc/Filesystem.hpp"
#include "pwiz/utility/misc/DateTime.hpp"
#include "pwiz/data/common/diff_std.hpp"
#include "pwiz/data/identdata/IdentDataFile.hpp"
#include "pwiz/data/identdata/TextWriter.hpp"
#include "pwiz/data/proteome/ProteinListCache.hpp"
#include "pwiz/data/proteome/ProteomeDataFile.hpp"
#include "pwiz/utility/chemistry/Ion.hpp"
#include "Parser.hpp"
#include "Qonverter.hpp"
#include "../freicore/AhoCorasickTrie.hpp"
#include "boost/foreach_field.hpp"
#include "boost/thread/thread.hpp"
#include "boost/thread/mutex.hpp"
#include "boost/lambda/lambda.hpp"
#include "boost/lambda/bind.hpp"
#include "boost/atomic.hpp"
#include "boost/exception/all.hpp"


// convenient macro for one-line status and cancellation updates
#define ITERATION_UPDATE(ilr, index, count, message) \
{ \
    if (ilr && ilr->broadcastUpdateMessage(UpdateMessage((index), (count), (message))) == IterationListener::Status_Cancel) \
        {status = IterationListener::Status_Cancel; return;} \
}


using namespace pwiz::identdata;
using namespace pwiz::chemistry;
using namespace pwiz::util;
typedef IterationListener::UpdateMessage UpdateMessage;
namespace proteome = pwiz::proteome;
namespace sqlite = sqlite3pp;
using freicore::AhoCorasickTrie;


BEGIN_IDPICKER_NAMESPACE


typedef boost::shared_ptr<proteome::ProteomeData> ProteomeDataPtr;
typedef Parser::Analysis Analysis;
typedef Parser::AnalysisPtr AnalysisPtr;
typedef Parser::ConstAnalysisPtr ConstAnalysisPtr;


namespace {

struct SharedStringFastLessThan
{
    bool operator() (const boost::shared_ptr<string>& lhs, const boost::shared_ptr<string>& rhs) const
    {
        if (lhs->length() == rhs->length())
            return *lhs < *rhs;
        return lhs->length() < rhs->length();
    }
};

struct AminoAcidTranslator
{
    static int size() {return 26;}
    static int translate(char aa) {return aa - 'A';};
    static char translate(int index) {return static_cast<char>(index) + 'A';}
};

typedef AhoCorasickTrie<AminoAcidTranslator> PeptideTrie;

struct IsNotAnalysisParameter
{
    bool operator() (const pair<string, string>& parameter) const
    {
        return bal::starts_with(parameter.first, "PeakCounts:") ||
               bal::starts_with(parameter.first, "SearchStats:") ||
               bal::starts_with(parameter.first, "SearchTime:") ||
               parameter.first == "Config: WorkingDirectory" ||
               parameter.first == "Config: StatusUpdateFrequency" ||
               parameter.first == "Config: UseMultipleProcessors" ||
               bal::starts_with(parameter.first, "Config: MaxResult") ||
               parameter.first == "Config: OutputSuffix" ||
               parameter.first == "USEREMAIL" ||
               parameter.first == "USERNAME" ||
               parameter.first == "LICENSE" ||
               parameter.first == "COM";
    }
};

void parseAnalysis(const IdentDataFile& mzid, Analysis& analysis)
{
    SpectrumIdentification& si = *mzid.analysisCollection.spectrumIdentification[0];
    SpectrumIdentificationProtocol& sip = *si.spectrumIdentificationProtocolPtr;

    if (!sip.analysisSoftwarePtr.get() || sip.analysisSoftwarePtr->empty())
        throw runtime_error("no analysis software specified");

    // determine analysis software used
    CVParam searchEngine = sip.analysisSoftwarePtr->softwareName.cvParamChild(MS_analysis_software);
    if (!searchEngine.empty())
        analysis.softwareName = searchEngine.name();
    else if (!sip.analysisSoftwarePtr->softwareName.userParams.empty())
        analysis.softwareName = sip.analysisSoftwarePtr->softwareName.userParams[0].name;
    else
        throw runtime_error("[Parser::parseAnalysis()] analysis software could not be determined");

    if (si.searchDatabase.size() > 1)
        throw runtime_error("[Parser::parseAnalysis()] multi-database protocols are not supported");

    // determine search database used
    SearchDatabase& sd = *si.searchDatabase[0];
    analysis.importSettings.proteinDatabaseFilepath = sd.location;

    // flatten params from the SpectrumIdentificationProtocol into single lists
    vector<CVParam> cvParams;
    vector<UserParam> userParams;

    cvParams.insert(cvParams.end(), sip.additionalSearchParams.cvParams.begin(), sip.additionalSearchParams.cvParams.end());
    userParams.insert(userParams.end(), sip.additionalSearchParams.userParams.begin(), sip.additionalSearchParams.userParams.end());

    // fragment/parent tolerance are treated separately since they use the same CV terms
    CVParam tolerance = sip.fragmentTolerance.cvParam(MS_search_tolerance_minus_value);
    userParams.push_back(UserParam("fragment tolerance minus value", tolerance.value + " " + cvTermInfo(tolerance.units).shortName()));
    tolerance = sip.fragmentTolerance.cvParam(MS_search_tolerance_plus_value);
    userParams.push_back(UserParam("fragment tolerance plus value", tolerance.value + " " + cvTermInfo(tolerance.units).shortName()));

    tolerance = sip.parentTolerance.cvParam(MS_search_tolerance_minus_value);
    userParams.push_back(UserParam("parent tolerance minus value", tolerance.value + " " + cvTermInfo(tolerance.units).shortName()));
    tolerance = sip.parentTolerance.cvParam(MS_search_tolerance_plus_value);
    userParams.push_back(UserParam("parent tolerance plus value", tolerance.value + " " + cvTermInfo(tolerance.units).shortName()));

    cvParams.insert(cvParams.end(), sip.threshold.cvParams.begin(), sip.threshold.cvParams.end());
    userParams.insert(userParams.end(), sip.threshold.userParams.begin(), sip.threshold.userParams.end());

    BOOST_FOREACH(const FilterPtr& filter, sip.databaseFilters)
    {
        cvParams.insert(cvParams.end(), filter->filterType.cvParams.begin(), filter->filterType.cvParams.end());
        cvParams.insert(cvParams.end(), filter->include.cvParams.begin(), filter->include.cvParams.end());
        cvParams.insert(cvParams.end(), filter->exclude.cvParams.begin(), filter->exclude.cvParams.end());
    }

    BOOST_FOREACH(const CVParam& cvParam, cvParams)
    {
        // value-less cvParams are keyed by their parent term;
        // e.g. "param: y ion" IS_A "ions series considered in search"
        string key, value;
        if (cvParam.value.empty())
        {
            const CVTermInfo& termInfo = cvTermInfo(cvParam.cvid);
            if (termInfo.parentsIsA.empty())
                key = cvParam.name();
            else
            {
                key = cvTermInfo(termInfo.parentsIsA[0]).shortName();
                value = cvParam.name();
            }
        }
        else
        {
            key = cvParam.name();
            value = cvParam.value;
            if (cvParam.units != CVID_Unknown)
                value += " " + cvTermInfo(cvParam.units).shortName();
        }

        // if key already exists, append the value
        if (!analysis.parameters.insert(make_pair(key, value)).second)
            analysis.parameters[key] += ", " + value;
    }

    // userParams are assumed to be uniquely keyed on name
    BOOST_FOREACH(const UserParam& userParam, userParams)
        analysis.parameters[userParam.name] = userParam.value;
    
    // set analysis name
    analysis.name = analysis.softwareName;

    // TODO: move the translation of the "certain parameters" into pwiz

    // set analysis software version (either from analysisSoftwarePtr or from certain parameters)
    if (!sip.analysisSoftwarePtr->version.empty())
        analysis.softwareVersion = sip.analysisSoftwarePtr->version;
    else if (analysis.parameters.count("SearchEngine: Version") > 0)
        analysis.softwareVersion = analysis.parameters["SearchEngine: Version"];

    // if possible, add software version to analysis name
    if (!analysis.softwareVersion.empty())
        analysis.name += " " + analysis.softwareVersion;

    // set analysis start time (either from activityDate or from certain parameters)
    if (!mzid.analysisCollection.spectrumIdentification[0]->activityDate.empty())
        analysis.startTime = decode_xml_datetime(mzid.analysisCollection.spectrumIdentification[0]->activityDate);
    else if (analysis.parameters.count("SearchTime: Started") > 0)
    {
        blt::local_date_time localTime = parse_date_time("%H:%M:%S on %m-%d-%Y", analysis.parameters["SearchTime: Started"]);
        analysis.startTime = blt::local_date_time(localTime.utc_time(), blt::time_zone_ptr());
    }

    vector<pair<string, string> > parameters(analysis.parameters.begin(), analysis.parameters.end());
    parameters.erase(remove_if(parameters.begin(), parameters.end(), IsNotAnalysisParameter()),
                     parameters.end());
    analysis.parameters.clear();
    analysis.parameters.insert(parameters.begin(), parameters.end());
}

// an analysis is distinct if its name is unique and it has at least one distinct parameter
typedef map<string, AnalysisPtr> DistinctAnalysisMap;
void findDistinctAnalyses(const vector<string>& inputFilepaths, DistinctAnalysisMap& distinctAnalyses)
{
    map<string, vector<AnalysisPtr> > sameNameAnalysesByName;
    BOOST_FOREACH(const string& filepath, inputFilepaths)
    {
        // ignore SequenceCollection and AnalysisData
        IdentDataFile mzid(filepath, 0, 0, true);

        AnalysisPtr analysis(new Analysis);
        parseAnalysis(mzid, *analysis);

        vector<AnalysisPtr>& sameNameAnalyses = sameNameAnalysesByName[analysis->name];
        AnalysisPtr sameAnalysis;

        // do a set union of the current analysis' parameters with every same name analysis;
        // if the union's size equals the current analysis' parameter size, the analysis is not distinct
        BOOST_FOREACH(AnalysisPtr& otherAnalysis, sameNameAnalyses)
        {
            vector<pair<string, string> > parameterUnion;
            std::set_union(otherAnalysis->parameters.begin(), otherAnalysis->parameters.end(),
                           analysis->parameters.begin(), analysis->parameters.end(),
                           std::back_inserter(parameterUnion));

            if (parameterUnion.size() == analysis->parameters.size())
            {
                sameAnalysis = otherAnalysis;
                break;
            }
        }

        if (!sameAnalysis.get())
        {
            sameNameAnalyses.push_back(analysis);
            sameAnalysis = sameNameAnalyses.back();
        }
        sameAnalysis->filepaths.push_back(filepath);
    }

    typedef pair<string, vector<AnalysisPtr> > SameNameAnalysesPair;
    BOOST_FOREACH(const SameNameAnalysesPair& itr, sameNameAnalysesByName)
    BOOST_FOREACH(const AnalysisPtr& analysis, itr.second)
    BOOST_FOREACH(const string& filepath, analysis->filepaths)
        distinctAnalyses[filepath] = analysis;
}


struct ParserImpl
{
    const string& inputFilepath;
    sqlite::database& idpDb;
    const IdentDataFile& mzid;
    const IterationListenerRegistry* ilr;

    map<boost::shared_ptr<string>, sqlite3_int64, SharedStringFastLessThan> distinctPeptideIdBySequence;
    map<double, sqlite3_int64> modIdByDeltaMass;

    ParserImpl(const string& inputFilepath,
               sqlite::database& idpDb,
               const IdentDataFile& mzid,
               const IterationListenerRegistry* ilr)
    : inputFilepath(inputFilepath),
      idpDb(idpDb),
      mzid(mzid),
      ilr(ilr)
    {
        initializeDatabase();
    }

    void initializeDatabase()
    {
        // optimize for bulk insertion
        idpDb.execute("PRAGMA journal_mode=OFF;"
                      "PRAGMA synchronous=OFF;"
                      "PRAGMA automatic_indexing=OFF;"
                      "PRAGMA default_cache_size=500000;"
                      "PRAGMA temp_store=MEMORY"
                     );

        sqlite::transaction transaction(idpDb);

        // initialize the tables
        idpDb.execute("CREATE TABLE SpectrumSource (Id INTEGER PRIMARY KEY, Name TEXT, URL TEXT, Group_ INT, MsDataBytes BLOB);"
                      "CREATE TABLE SpectrumSourceGroup (Id INTEGER PRIMARY KEY, Name TEXT);"
                      "CREATE TABLE SpectrumSourceGroupLink (Id INTEGER PRIMARY KEY, Source INT, Group_ INT);"
                      "CREATE TABLE Spectrum (Id INTEGER PRIMARY KEY, Source INT, Index_ INT, NativeID TEXT, PrecursorMZ NUMERIC);"
                      "CREATE TABLE Analysis (Id INTEGER PRIMARY KEY, Name TEXT, SoftwareName TEXT, SoftwareVersion TEXT, Type INT, StartTime DATETIME);"
                      "CREATE TABLE AnalysisParameter (Id INTEGER PRIMARY KEY, Analysis INT, Name TEXT, Value TEXT);"
                      "CREATE TABLE Modification (Id INTEGER PRIMARY KEY, MonoMassDelta NUMERIC, AvgMassDelta NUMERIC, Formula TEXT, Name TEXT);"
                      "CREATE TABLE Protein (Id INTEGER PRIMARY KEY, Accession TEXT, Cluster INT, ProteinGroup TEXT, Length INT);"
                      "CREATE TABLE ProteinData (Id INTEGER PRIMARY KEY, Sequence TEXT);"
                      "CREATE TABLE ProteinMetadata (Id INTEGER PRIMARY KEY, Description TEXT);"
                      "CREATE TABLE Peptide (Id INTEGER PRIMARY KEY, MonoisotopicMass NUMERIC, MolecularWeight NUMERIC);"
                      "CREATE TABLE PeptideInstance (Id INTEGER PRIMARY KEY, Protein INT, Peptide INT, Offset INT, Length INT, NTerminusIsSpecific INT, CTerminusIsSpecific INT, MissedCleavages INT);"
                      //"CREATE TABLE PeptideSequence (Id INTEGER PRIMARY KEY, Sequence TEXT);"
                      "CREATE TABLE PeptideSpectrumMatch (Id INTEGER PRIMARY KEY, Spectrum INT, Analysis INT, Peptide INT, QValue NUMERIC, MonoisotopicMass NUMERIC, MolecularWeight NUMERIC, MonoisotopicMassError NUMERIC, MolecularWeightError NUMERIC, Rank INT, Charge INT);"
                      "CREATE TABLE PeptideModification (Id INTEGER PRIMARY KEY, PeptideSpectrumMatch INT, Modification INT, Offset INT, Site TEXT);"
                      "CREATE TABLE PeptideSpectrumMatchScore (PsmId INTEGER NOT NULL, Value NUMERIC, ScoreNameId INTEGER NOT NULL, primary key (PsmId, ScoreNameId));"
                      "CREATE TABLE PeptideSpectrumMatchScoreName (Id INTEGER PRIMARY KEY, Name TEXT UNIQUE NOT NULL);"
                      "CREATE TABLE IntegerSet (Value INTEGER PRIMARY KEY);"
                      "CREATE TABLE LayoutProperty (Id INTEGER PRIMARY KEY, Name TEXT, PaneLocations TEXT, HasCustomColumnSettings INT);"
                      "CREATE TABLE ColumnProperty (Id INTEGER PRIMARY KEY, Scope TEXT, Name TEXT, Type TEXT, DecimalPlaces INT, ColorCode INT, Visible INT, Locked INT, Layout INT);"
                      "CREATE TABLE ProteinCoverage (Id INTEGER PRIMARY KEY, Coverage NUMERIC, CoverageMask BLOB);"
                     );
        transaction.commit();
    }

    void insertAnalysisMetadata()
    {
        sqlite::transaction transaction(idpDb);

        if (mzid.analysisProtocolCollection.spectrumIdentificationProtocol.empty())
            throw runtime_error("no spectrum identification protocol");

        if (mzid.analysisProtocolCollection.spectrumIdentificationProtocol.size() > 1)
            throw runtime_error("more than one spectrum identification protocol not supported");

        SpectrumIdentificationProtocol& sip = *mzid.analysisProtocolCollection.spectrumIdentificationProtocol[0];

        Analysis analysis;
        parseAnalysis(mzid, analysis);

        // insert the root group
        sqlite::command(idpDb, "INSERT INTO SpectrumSourceGroup (Id, Name) VALUES (1,'/')").execute();
        sqlite::command(idpDb, "INSERT INTO SpectrumSourceGroupLink (Id, Source, Group_) VALUES (1,1,1)").execute();

        // create commands for inserting file-level metadata (SpectrumSource, Analysis, AnalysisParameter)
        sqlite::command insertSpectrumSource(idpDb, "INSERT INTO SpectrumSource (Id, Name, URL, Group_, MsDataBytes) VALUES (?,?,?,1,null)");
        sqlite::command insertAnalysis(idpDb, "INSERT INTO Analysis (Id, Name, SoftwareName, SoftwareVersion, Type, StartTime) VALUES (?,?,?,?,?,?)");
        sqlite::command insertAnalysisParameter(idpDb, "INSERT INTO AnalysisParameter (Id, Analysis, Name, Value) VALUES (?,?,?,?)");

        string spectraDataName = mzid.dataCollection.inputs.spectraData[0]->name;
        if (spectraDataName.empty())
        {
            spectraDataName = bfs::path(mzid.dataCollection.inputs.spectraData[0]->location).replace_extension("").filename();
            if (spectraDataName.empty())
                throw runtime_error("no spectrum source name or location");
        }

        // insert file-level metadata into the database
        insertSpectrumSource.binder() << 1
                                      << spectraDataName
                                      << mzid.dataCollection.inputs.spectraData[0]->location;
        insertSpectrumSource.execute();

        insertAnalysis.binder() << 1
                                << analysis.name
                                << analysis.softwareName
                                << analysis.softwareVersion
                                << 0;
        if (!analysis.startTime.is_not_a_date_time())
            insertAnalysis.bind(6, format_date_time("%Y-%m-%d %H:%M:%S", analysis.startTime));
        insertAnalysis.execute();

        int analysisParameterId = 0;
        BOOST_FOREACH_FIELD((const string& name)(const string& value), analysis.parameters)
        {
            insertAnalysisParameter.binder() << ++analysisParameterId << 1 << name << value;
            insertAnalysisParameter.execute();
            insertAnalysisParameter.reset();
        }

        transaction.commit();
    }

    void insertScoreNames(SpectrumIdentificationItemPtr& sii)
    {
        sqlite::command insertScoreName(idpDb, "INSERT INTO PeptideSpectrumMatchScoreName (Id, Name) VALUES (?,?)");

        sqlite3_int64 nextScoreId = 0;

        BOOST_FOREACH(CVParam& cvParam, sii->cvParams)
        {
            insertScoreName.binder() << ++nextScoreId << cvParam.name();
            insertScoreName.execute();
            insertScoreName.reset();
        }

        BOOST_FOREACH(UserParam& userParam, sii->userParams)
        {
            insertScoreName.binder() << ++nextScoreId << userParam.name;
            insertScoreName.execute();
            insertScoreName.reset();
        }
    }

    void insertSpectrumResults(IterationListener::Status& status)
    {
        sqlite::transaction transaction(idpDb);

        if (mzid.dataCollection.analysisData.spectrumIdentificationList.empty())
            throw runtime_error("no spectrum identification list");

        // create commands for inserting results
        sqlite::command insertSpectrum(idpDb, "INSERT INTO Spectrum (Id, Source, Index_, NativeID, PrecursorMZ) VALUES (?,1,?,?,?)");
        sqlite::command insertPeptide(idpDb, "INSERT INTO Peptide (Id, MonoisotopicMass, MolecularWeight) VALUES (?,?,?)");
        //sqlite::command insertPeptideSequence(idpDb, "INSERT INTO PeptideSequence (Id, Sequence) VALUES (?,?)");
        sqlite::command insertPSM(idpDb, "INSERT INTO PeptideSpectrumMatch (Id, Spectrum, Analysis, Peptide, QValue, MonoisotopicMass, MolecularWeight, MonoisotopicMassError, MolecularWeightError, Rank, Charge) VALUES (?,?,?,?,?,?,?,?,?,?,?)");
        sqlite::command insertPeptideModification(idpDb, "INSERT INTO PeptideModification (Id, PeptideSpectrumMatch, Modification, Offset, Site) VALUES (?,?,?,?,?)");
        sqlite::command insertModification(idpDb, "INSERT INTO Modification (Id, MonoMassDelta, AvgMassDelta, Formula, Name) VALUES (?,?,?,?,?)");
        sqlite::command insertScore(idpDb, "INSERT INTO PeptideSpectrumMatchScore (PsmId, Value, ScoreNameId) VALUES (?,?,?)");

        map<string, sqlite3_int64> distinctSpectra;
        //map<SearchModificationPtr, sqlite3_int64> modifications;

        SpectrumIdentificationList& sil = *mzid.dataCollection.analysisData.spectrumIdentificationList[0];

        sqlite3_int64 nextSpectrumId = 0, nextPeptideId = 0, nextPSMId = 0, nextPMId = 0, nextModId = 0;
        bool hasScoreNames = false;

        int iterationIndex = 0;
        BOOST_FOREACH(SpectrumIdentificationResultPtr& sir, sil.spectrumIdentificationResult)
        {
            ITERATION_UPDATE(ilr, iterationIndex++, sil.spectrumIdentificationResult.size(), "writing spectrum results");

            // without an SII, precursor m/z is unknown, so empty results are skipped
            if (sir->spectrumIdentificationItem.empty())
                continue;

            // insert distinct spectrum
            nextSpectrumId = distinctSpectra.size() + 1;
            bool spectrumInserted = distinctSpectra.insert(make_pair(sir->spectrumID, nextSpectrumId)).second;
            if (!spectrumInserted)
                throw runtime_error("non-unique spectrumIDs not supported");

            double firstPrecursorMZ = sir->spectrumIdentificationItem[0]->experimentalMassToCharge;
            insertSpectrum.binder() << nextSpectrumId << nextSpectrumId << sir->spectrumID << firstPrecursorMZ;
            insertSpectrum.execute();
            insertSpectrum.reset();

            BOOST_FOREACH(SpectrumIdentificationItemPtr& sii, sir->spectrumIdentificationItem)
            {
                if (!sii->peptidePtr.get() || sii->peptidePtr->empty())
                    throw runtime_error("SII with a missing or empty peptide reference");

                // insert distinct peptide
                const string& sequence = sii->peptidePtr->peptideSequence;
                proteome::Peptide pwizPeptide(sequence);
                boost::shared_ptr<string> sharedSequence(new string(sequence));

                nextPeptideId = distinctPeptideIdBySequence.size() + 1;
                bool peptideInserted = distinctPeptideIdBySequence.insert(make_pair(sharedSequence, nextPeptideId)).second;
                if (peptideInserted)
                {
                    insertPeptide.binder() << nextPeptideId << pwizPeptide.monoisotopicMass() << pwizPeptide.molecularWeight();
                    insertPeptide.execute();
                    insertPeptide.reset();

                    /*insertPeptideSequence.binder() << nextPeptideId << sequence;
                    insertPeptideSequence.execute();
                    insertPeptideSequence.reset();*/
                }
                else
                    nextPeptideId = distinctPeptideIdBySequence[sharedSequence];

                ++nextPSMId;

                // insert modifications
                BOOST_FOREACH(ModificationPtr& mod, sii->peptidePtr->modification)
                {
                    ++nextPMId;

                    double modMass = mod->monoisotopicMassDelta > 0 ? mod->monoisotopicMassDelta
                                                                    : mod->avgMassDelta;

                    pair<map<double, sqlite3_int64>::iterator, bool> insertResult =
                        modIdByDeltaMass.insert(make_pair(modMass, 0));
                    if (insertResult.second)
                    {
                        insertResult.first->second = ++nextModId;
                        insertModification.binder() << nextModId
                                                    << mod->monoisotopicMassDelta
                                                    << mod->avgMassDelta
                                                    << "" // TODO: use Unimod
                                                    << "";
                        insertModification.execute();
                        insertModification.reset();
                    }

                    int offset = mod->location - 1;
                    if (offset < 0)
                        offset = INT_MIN;
                    else if (offset >= (int) sequence.length())
                        offset = INT_MAX;

                    char site;
                    if (offset == INT_MIN)
                        site = '(';
                    else if (offset == INT_MAX)
                        site = ')';
                    else
                        site = sequence[offset];

                    insertPeptideModification.binder() << nextPMId
                                                       << nextPSMId
                                                       << insertResult.first->second // mod id
                                                       << offset
                                                       << string(1, site);
                    insertPeptideModification.execute();
                    insertPeptideModification.reset();

                    pwizPeptide.modifications()[offset].push_back(proteome::Modification(mod->monoisotopicMassDelta, mod->avgMassDelta));
                }

                double precursorMass = Ion::neutralMass(sii->experimentalMassToCharge, sii->chargeState);

                // insert peptide spectrum match
                insertPSM.binder() << nextPSMId
                                   << nextSpectrumId
                                   << 1 // analysis
                                   << nextPeptideId
                                   << 2 // q value
                                   << precursorMass
                                   << precursorMass
                                   << precursorMass - pwizPeptide.monoisotopicMass()
                                   << precursorMass - pwizPeptide.molecularWeight()
                                   << sii->rank
                                   << sii->chargeState;
                insertPSM.execute();
                insertPSM.reset();

                if (!hasScoreNames)
                {
                    hasScoreNames = true;
                    insertScoreNames(sii);
                }

                if (sii->cvParams.empty() && sii->userParams.empty())
                    throw runtime_error("no scores found for SII");

                sqlite3_int64 nextScoreId = 0;

                BOOST_FOREACH(CVParam& cvParam, sii->cvParams)
                {
                    insertScore.binder() << nextPSMId << cvParam.value << ++nextScoreId;
                    insertScore.execute();
                    insertScore.reset();
                }

                BOOST_FOREACH(UserParam& userParam, sii->userParams)
                {
                    insertScore.binder() << nextPSMId << userParam.value << ++nextScoreId;
                    insertScore.execute();
                    insertScore.reset();
                }
            }
        }

        transaction.commit();
    }

    void createIndexes()
    {
        sqlite::transaction transaction(idpDb);
        idpDb.execute("CREATE UNIQUE INDEX Protein_Accession ON Protein (Accession);"
                      "CREATE INDEX PeptideInstance_Peptide ON PeptideInstance (Peptide);"
                      "CREATE INDEX PeptideInstance_Protein ON PeptideInstance (Protein);"
                      "CREATE INDEX PeptideInstance_PeptideProtein ON PeptideInstance (Peptide, Protein);"
                      "CREATE UNIQUE INDEX PeptideInstance_ProteinOffsetLength ON PeptideInstance (Protein, Offset, Length);"
                      "CREATE UNIQUE INDEX SpectrumSourceGroupLink_SourceGroup ON SpectrumSourceGroupLink (Source, Group_);"
                      "CREATE INDEX Spectrum_SourceIndex ON Spectrum (Source, Index_);"
                      "CREATE UNIQUE INDEX Spectrum_SourceNativeID ON Spectrum (Source, NativeID);"
                      "CREATE INDEX PeptideSpectrumMatch_Analysis ON PeptideSpectrumMatch (Analysis);"
                      "CREATE INDEX PeptideSpectrumMatch_Peptide ON PeptideSpectrumMatch (Peptide);"
                      "CREATE INDEX PeptideSpectrumMatch_Spectrum ON PeptideSpectrumMatch (Spectrum);"
                      "CREATE INDEX PeptideSpectrumMatch_QValue ON PeptideSpectrumMatch (QValue);"
                      "CREATE INDEX PeptideSpectrumMatch_Rank ON PeptideSpectrumMatch (Rank);"
                      "CREATE INDEX PeptideModification_PeptideSpectrumMatch ON PeptideModification (PeptideSpectrumMatch);"
                      "CREATE INDEX PeptideModification_Modification ON PeptideModification (Modification);"
                     );
        transaction.commit();
    }

    void applyQValueFilter(const Analysis& analysis, double qValueThreshold)
    {
        const Qonverter::Settings& settings = analysis.importSettings.qonverterSettings;

        // write QonverterSettings for preqonvert;
        // assemble scoreInfo string ("Weight Order NormalizationMethod ScoreName")
        vector<string> scoreInfoStrings;
        BOOST_FOREACH_FIELD((const string& name)(const Qonverter::Settings::ScoreInfo& scoreInfo), settings.scoreInfoByName)
        {
            ostringstream ss;
            ss << scoreInfo.weight << " "
               << scoreInfo.order << " "
               << scoreInfo.normalizationMethod << " "
               << name;
            scoreInfoStrings.push_back(ss.str());
        }
        string scoreInfo = bal::join(scoreInfoStrings, ";");

        idpDb.execute("CREATE TABLE QonverterSettings (Id INTEGER PRIMARY KEY,"
                      "                                QonverterMethod INT,"
                      "                                DecoyPrefix TEXT,"
                      "                                RerankMatches INT,"
                      "                                Kernel INT,"
                      "                                MassErrorHandling INT,"
                      "                                MissedCleavagesHandling INT,"
                      "                                TerminalSpecificityHandling INT,"
                      "                                ChargeStateHandling INT,"
                      "                                ScoreInfoByName TEXT);");

        sqlite::command insertQonverterSettings(idpDb, "INSERT INTO QonverterSettings VALUES (1,?,?,?,?,?,?,?,?,?)");
        insertQonverterSettings.binder() << (int) settings.qonverterMethod.index()
                                         << settings.decoyPrefix
                                         << (settings.rerankMatches ? 1 : 0)
                                         << (int) settings.kernel.index()
                                         << (int) settings.massErrorHandling.index()
                                         << (int) settings.missedCleavagesHandling.index()
                                         << (int) settings.terminalSpecificityHandling.index()
                                         << (int) settings.chargeStateHandling.index()
                                         << scoreInfo;
        insertQonverterSettings.execute();

        Qonverter qonverter;
        //qonverter.logQonversionDetails = true;
        qonverter.settingsByAnalysis[0] = settings;
        qonverter.qonvert(idpDb.connected());

        sqlite::transaction transaction(idpDb);

        const char* sql =
            // Apply a broad QValue filter on top-ranked PSMs
            "DELETE FROM PeptideSpectrumMatch WHERE QValue > %f AND Rank = 1;"

            // Delete all PSMs for a spectrum if the spectrum's top-ranked PSM was deleted above
            "DELETE FROM PeptideSpectrumMatch"
            "      WHERE Rank > 1"
            "        AND Spectrum NOT IN ("
            "                             SELECT DISTINCT Spectrum"
            "                             FROM PeptideSpectrumMatch"
            "                             WHERE Rank = 1"
            "                            );"

            // Delete links to the deleted PSMs
            "DELETE FROM PeptideSpectrumMatchScore WHERE PsmId NOT IN (SELECT Id FROM PeptideSpectrumMatch);"
            "DELETE FROM PeptideModification WHERE PeptideSpectrumMatch NOT IN (SELECT Id FROM PeptideSpectrumMatch);"
            "DELETE FROM Spectrum WHERE Id NOT IN (SELECT DISTINCT Spectrum FROM PeptideSpectrumMatch);"
            "DELETE FROM Peptide WHERE Id NOT IN (SELECT DISTINCT Peptide FROM PeptideSpectrumMatch);"
            //"DELETE FROM PeptideSequence WHERE Id NOT IN (SELECT Id FROM Peptide);"
            "DELETE FROM PeptideInstance WHERE Peptide NOT IN (SELECT Id FROM Peptide);"
            "DELETE FROM Protein WHERE Id NOT IN (SELECT Protein FROM PeptideInstance);"
            "DELETE FROM ProteinData WHERE Id NOT IN (SELECT Protein FROM PeptideInstance);"
            "DELETE FROM ProteinMetadata WHERE Id NOT IN (SELECT Protein FROM PeptideInstance);";

        idpDb.executef(sql, qValueThreshold);

        transaction.commit();

        idpDb.execute("VACUUM");
    }
};


struct ProteinDatabaseTaskGroup
{
    ProteomeDataPtr proteomeDataPtr;
    vector<string> inputFilepaths;
};

vector<ProteinDatabaseTaskGroup> createTasksPerProteinDatabase(const vector<string>& inputFilepaths,
                                                               const DistinctAnalysisMap& distinctAnalysisByFilepath,
                                                               map<string, ProteomeDataPtr> proteinDatabaseByFilepath,
                                                               int maxThreads)
{
    // group input files by their protein database
    map<string, vector<string> > inputFilepathsByProteinDatabase;
    BOOST_FOREACH(const string& inputFilepath, inputFilepaths)
    {
        if (distinctAnalysisByFilepath.count(inputFilepath) == 0)
            throw runtime_error("[Parser::parse()] unable to find analysis for file \"" + inputFilepath + "\"");

        const AnalysisPtr& analysis = distinctAnalysisByFilepath.find(inputFilepath)->second;
        const string& proteinDatabaseFilepath = analysis->importSettings.proteinDatabaseFilepath;

        inputFilepathsByProteinDatabase[proteinDatabaseFilepath].push_back(inputFilepath);
    }

    int processorCount = min(maxThreads, (int) boost::thread::hardware_concurrency());
    vector<ProteinDatabaseTaskGroup> taskGroups;

    int processorsUsed = 0;
    BOOST_FOREACH_FIELD((const string& proteinDatabaseFilepath)(vector<string>& inputFilepaths),
                        inputFilepathsByProteinDatabase)
    {
        taskGroups.push_back(ProteinDatabaseTaskGroup());
        taskGroups.back().proteomeDataPtr = proteinDatabaseByFilepath[proteinDatabaseFilepath];

        // shuffled so that large and small input files get mixed
        random_shuffle(inputFilepaths.begin(), inputFilepaths.end());

        BOOST_FOREACH(const string& inputFilepath, inputFilepaths)
        {
            if (bfs::exists(bfs::path(inputFilepath).replace_extension(".idpDB")))
            {
                // for now, abort; eventually we want to merge? here
                continue;
            }

            taskGroups.back().inputFilepaths.push_back(inputFilepath);
            ++processorsUsed;

            // if out of processors and there are more input files for this database, add another top-level task
            if (processorsUsed == processorCount && &inputFilepath != &inputFilepaths.back())
            {
                taskGroups.push_back(ProteinDatabaseTaskGroup());
                taskGroups.back().proteomeDataPtr = proteinDatabaseByFilepath[proteinDatabaseFilepath];
                processorsUsed = 0;
            }
        }
    }

    return taskGroups;
}

// an iteration listener that prepends the inputFilepath before forwarding the update message
struct ParserForwardingIterationListener : public IterationListener
{
    const IterationListenerRegistry& inner;
    const string& inputFilepath;

    ParserForwardingIterationListener(const IterationListenerRegistry& inner, const string& inputFilepath)
        : inner(inner), inputFilepath(inputFilepath)
    {}

    virtual Status update(const UpdateMessage& updateMessage)
    {
        string specificMessage = inputFilepath + "*" + updateMessage.message;
        return inner.broadcastUpdateMessage(UpdateMessage(updateMessage.iterationIndex,
                                                          updateMessage.iterationCount,
                                                          specificMessage));
    }
};


struct ThreadStatus
{
    bool userCanceled;
    boost::exception_ptr exception;

    ThreadStatus() : userCanceled(false) {}
    ThreadStatus(IterationListener::Status status) : userCanceled(status == IterationListener::Status_Cancel) {}
    ThreadStatus(const boost::exception_ptr& e) : userCanceled(false), exception(e) {}
};


struct ParserTask
{
    ParserTask(const string& inputFilepath = "") : inputFilepath(inputFilepath) {}

    string inputFilepath;
    boost::shared_ptr<sqlite::database> idpDb;
    boost::shared_ptr<IdentDataFile> mzid;
    boost::shared_ptr<ParserImpl> parser;
    AnalysisPtr analysis;
    const IterationListenerRegistry* ilr;
    boost::mutex* ioMutex;
};

typedef boost::shared_ptr<ParserTask> ParserTaskPtr;


void executeParserTask(ParserTaskPtr parserTask, ThreadStatus& status)
{
    const string& inputFilepath = parserTask->inputFilepath;
    const IterationListenerRegistry* ilr = parserTask->ilr;
    //boost::mutex& ioMutex = *peptideFinderTask->ioMutex;

    try
    {
        // create an in-memory database
        parserTask->idpDb.reset(new sqlite::database(":memory:", sqlite::no_mutex));

        IterationListenerRegistry* threadILR;
        if (ilr)
        {
            threadILR = new IterationListenerRegistry();
            threadILR->addListener(IterationListenerPtr(new ParserForwardingIterationListener(*ilr, inputFilepath)), 10);
        }

        // read the mzid document into memory
        ITERATION_UPDATE(ilr, 0, 0, inputFilepath + "*opening file");
        {
            //boost::mutex::scoped_lock ioLock(ioMutex);
            parserTask->mzid.reset(new IdentDataFile(inputFilepath, 0, threadILR));
        }

        // create parser instance
        parserTask->parser.reset(new ParserImpl(inputFilepath, *parserTask->idpDb, *parserTask->mzid, threadILR));

        parserTask->parser->insertAnalysisMetadata();

        IterationListener::Status tmpStatus;
        parserTask->parser->insertSpectrumResults(tmpStatus);
        if (tmpStatus == IterationListener::Status_Cancel)
        {
            status = tmpStatus;
            return;
        }

        //if (parserTask->parser->buildPeptideTrie() == IterationListener::Status_Cancel)
        //    return IterationListener::Status_Cancel;

        parserTask->mzid.reset();

        status = IterationListener::Status_Ok;
    }
    catch (exception& e)
    {
        status = boost::copy_exception(runtime_error("[executeParserTask] error parsing \"" + inputFilepath + "\": " + e.what()));
    }
    catch (...)
    {
        status = boost::copy_exception(runtime_error("[executeParserTask] unknown error parsing \"" + inputFilepath + "\""));
    }
}


struct PeptideFinderTask;
typedef boost::weak_ptr<PeptideFinderTask> PeptideFinderTaskWeakPtr;


struct ProteinReaderTask
{
    ProteomeDataPtr proteomeDataPtr;
    int proteinCount;
    vector<PeptideFinderTaskWeakPtr> peptideFinderTasks;
    boost::mutex queueMutex;
    boost::atomic_uint32_t done;
};

typedef boost::shared_ptr<ProteinReaderTask> ProteinReaderTaskPtr;


struct PeptideFinderTask
{
    ProteinReaderTaskPtr proteinReaderTask;
    deque<proteome::ProteinPtr> proteinQueue;
    ParserTaskPtr parserTask;
    boost::atomic<bool> done;
    const IterationListenerRegistry* ilr;
    boost::mutex* ioMutex;
};

typedef boost::shared_ptr<PeptideFinderTask> PeptideFinderTaskPtr;


void executeProteinReaderTask(ProteinReaderTaskPtr proteinReaderTask, ThreadStatus& status)
{
    try
    {
        const proteome::ProteomeData& pd = *proteinReaderTask->proteomeDataPtr;
        const proteome::ProteinList& pl = *pd.proteinListPtr;

        const size_t batchSize = 100;
        vector<proteome::ProteinPtr> proteinBatch(batchSize);

        boost::mutex::scoped_lock lock(proteinReaderTask->queueMutex, boost::defer_lock);

        // protein database is read over for each peptide batch
        while (true)
        {
            if (proteinReaderTask->done == proteinReaderTask->peptideFinderTasks.size())
            {
                status = IterationListener::Status_Ok;
                return; // ~scoped_lock calls unlock()
            }

            for (size_t i=0; i < pl.size(); ++i)
            {
                proteinBatch.clear();

                for (int j=0; j < batchSize && i+j < pl.size(); ++j)
                    proteinBatch.push_back(pl.protein(i+j));
                i += batchSize - 1;

                while (true)
                {
                    // check for early cancellation
                    if (proteinReaderTask->done == proteinReaderTask->peptideFinderTasks.size())
                    {
                        status = IterationListener::Status_Cancel;
                        return; // ~scoped_lock calls unlock()
                    }

                    lock.lock();

                    size_t maxQueueSize = 0;
                    BOOST_FOREACH(const PeptideFinderTaskWeakPtr& taskPtr, proteinReaderTask->peptideFinderTasks)
                    {
                        PeptideFinderTaskPtr task = taskPtr.lock();
                        maxQueueSize = max(maxQueueSize, task.get() ? task->proteinQueue.size() : 0);
                    }

                    // keep at most 20 batches in the queue
                    if (maxQueueSize > batchSize * 20)
                    {
                        lock.unlock();
                        boost::this_thread::sleep(bpt::milliseconds(100));
                        continue;
                    }
                    else
                        break;
                }

                // lock is still locked

                BOOST_FOREACH(const PeptideFinderTaskWeakPtr& taskPtr, proteinReaderTask->peptideFinderTasks)
                {
                    PeptideFinderTaskPtr task = taskPtr.lock();
                    if (task.get() && !task->done)
                        task->proteinQueue.insert(task->proteinQueue.end(), proteinBatch.begin(), proteinBatch.end());
                }
                lock.unlock();
            }
        }
    }
    catch (exception& e)
    {
        proteinReaderTask->done.store(proteinReaderTask->peptideFinderTasks.size());
        status = boost::copy_exception(runtime_error("[executeProteinReaderTask] error reading proteins: " + string(e.what())));
    }
    catch (...)
    {
        proteinReaderTask->done.store(proteinReaderTask->peptideFinderTasks.size());
        status = boost::copy_exception(runtime_error("[executeProteinReaderTask] unknown error reading proteins"));
    }
}


void executePeptideFinderTask(PeptideFinderTaskPtr peptideFinderTask, ThreadStatus& status)
{
    ProteinReaderTask& proteinReaderTask = *peptideFinderTask->proteinReaderTask;
    deque<proteome::ProteinPtr>& proteinQueue = peptideFinderTask->proteinQueue;
    ParserTask& parserTask = *peptideFinderTask->parserTask;
    ParserImpl& parser = *parserTask.parser;
    sqlite::database& idpDb = *parserTask.idpDb;
    const IterationListenerRegistry* ilr = peptideFinderTask->ilr;
    boost::mutex& ioMutex = *peptideFinderTask->ioMutex;
    map<boost::shared_ptr<string>, sqlite3_int64, SharedStringFastLessThan>& distinctPeptideIdBySequence =
        parser.distinctPeptideIdBySequence;

    try
    {
        sqlite::command insertProtein(idpDb, "INSERT INTO Protein (Id, Accession, Cluster, ProteinGroup, Length) VALUES (?,?,0,0,?)");
        //sqlite::command insertProteinData(idpDb, "INSERT INTO ProteinData (Id, Sequence) VALUES (?,?)");
        //sqlite::command insertProteinMetadata(idpDb, "INSERT INTO ProteinMetadata (Id, Description) VALUES (?,?)");
        sqlite::command insertPeptideInstance(idpDb, "INSERT INTO PeptideInstance (Id, Protein, Peptide, Offset, Length, NTerminusIsSpecific, CTerminusIsSpecific, MissedCleavages) VALUES (?,?,?,?,?,?,?,?)");

        sqlite3_int64 nextProteinId = 0, nextPeptideInstanceId = 0;
        int maxProteinLength = 0;

        boost::mutex::scoped_lock lock(proteinReaderTask.queueMutex, boost::defer_lock);

        /*if (ilr && ilr->broadcastUpdateMessage(UpdateMessage(0, 0, parserTask.inputFilepath + "*opening protein database")) == IterationListener::Status_Cancel)
        {
            lock.lock();
            proteinReaderTask.done = true;
            return IterationListener::Status_Cancel;
        }*/

        // peptide tries are created in batches to ensure scalability
        const size_t peptideBatchSize = 200000;
        int peptideQueries = 0;

        vector<boost::shared_ptr<string> > peptides;
        BOOST_FOREACH_FIELD((const boost::shared_ptr<string>& peptide), distinctPeptideIdBySequence)
            peptides.push_back(peptide);

        // distinctPeptideIdBySequence is sorted on peptide length, which is bad for the trie
        random_shuffle(peptides.begin(), peptides.end());

        // maps proteins indexes to protein ids (in the database)
        map<size_t, sqlite3_int64> proteinIdByIndex;

        vector<boost::shared_ptr<string> > peptideBatch;
        BOOST_FOREACH(const boost::shared_ptr<string>& peptide, peptides)
        {
            peptideBatch.push_back(peptide);

            // only proceed if we're at the batch size or the end of the peptides
            if (peptideBatch.size() < peptideBatchSize && peptide != *peptides.rbegin())
                continue;

            peptideQueries += (int) peptideBatch.size();
            if (ilr && ilr->broadcastUpdateMessage(UpdateMessage(peptideQueries-1, peptides.size(), parserTask.inputFilepath + "*building peptide trie")) == IterationListener::Status_Cancel)
            {
                proteinReaderTask.done.store(proteinReaderTask.peptideFinderTasks.size());
                status = IterationListener::Status_Cancel;
                return;
            }

            PeptideTrie peptideTrie(peptideBatch.begin(), peptideBatch.end());
            peptideBatch.clear();

            int proteinsDigested = 0;

            while (true)
            {
                // dequeue a batch of proteins, or sleep if none are available
                vector<proteome::ProteinPtr> proteinBatch;

                lock.lock();
                size_t queueSize = proteinQueue.size();
                if (queueSize == 0)
                {
                    if (proteinReaderTask.done == proteinReaderTask.peptideFinderTasks.size())
                    {
                        lock.unlock();
                        break;
                    }
                    else
                    {
                        lock.unlock();
                        boost::this_thread::sleep(bpt::milliseconds(100));
                        continue;
                    }
                }

                const size_t maxBatchSize = 100;
                size_t proteinsRemaining = proteinReaderTask.proteinCount - proteinsDigested;
                size_t batchSize = min(queueSize, min(proteinsRemaining, maxBatchSize));

                proteinBatch.assign(proteinQueue.begin(), proteinQueue.begin() + batchSize);
                proteinQueue.erase(proteinQueue.begin(), proteinQueue.begin() + batchSize);
                lock.unlock();

                // move to next peptide batch
                if (proteinBatch.empty())
                    break;

                proteinsDigested += proteinBatch.size();

                if (ilr && ilr->broadcastUpdateMessage(UpdateMessage(proteinsDigested-1, proteinReaderTask.proteinCount, parserTask.inputFilepath + "*finding peptides in proteins")) == IterationListener::Status_Cancel)
                {
                    proteinReaderTask.done.store(proteinReaderTask.peptideFinderTasks.size());
                    status = IterationListener::Status_Cancel;
                    return;
                }

                BOOST_FOREACH(proteome::ProteinPtr& protein, proteinBatch)
                {
                    proteome::Digestion::Config digestionConfig(100000, 0, 100000, proteome::Digestion::NonSpecific);
                    proteome::Digestion digestion(*protein, MS_Trypsin_P, digestionConfig); // TODO: use the right enzyme

                    vector<PeptideTrie::SearchResult> peptideInstances = peptideTrie.find_all(protein->sequence());

                    if (peptideInstances.empty())
                        continue;

                    maxProteinLength = max((int) protein->sequence().length(), maxProteinLength);

                    map<size_t, sqlite3_int64>::iterator itr; bool wasInserted;
                    boost::tie(itr, wasInserted) = proteinIdByIndex.insert(make_pair(protein->index, 0));

                    if (wasInserted)
                    {
                        itr->second = ++nextProteinId;

                        insertProtein.binder() << nextProteinId << protein->id << (int) protein->sequence().length();
                        insertProtein.execute();
                        insertProtein.reset();

                        /*insertProteinData.binder() << nextProteinId << protein->sequence();
                        insertProteinData.execute();
                        insertProteinData.reset();

                        insertProteinMetadata.binder() << nextProteinId << protein->description;
                        insertProteinMetadata.execute();
                        insertProteinMetadata.reset();*/
                    }

                    sqlite3_int64 curProteinId = itr->second;

                    BOOST_FOREACH(PeptideTrie::SearchResult& instance, peptideInstances)
                    {
                        // calculate terminal specificity and missed cleavages
                        proteome::DigestedPeptide peptide = digestion.find_first(*instance.keyword(), instance.offset());

                        insertPeptideInstance.binder() << ++nextPeptideInstanceId
                                                       << curProteinId
                                                       << distinctPeptideIdBySequence[instance.keyword()]
                                                       << (int) instance.offset()
                                                       << (int) instance.keyword()->length()
                                                       << peptide.NTerminusIsSpecific()
                                                       << peptide.CTerminusIsSpecific()
                                                       << (int) peptide.missedCleavages();
                        insertPeptideInstance.execute();
                        insertPeptideInstance.reset();
                    }
                }
            }
        }

        // the protein reader task stops when done == proteinReaderTask.peptideFinderTasks.size()
        ++proteinReaderTask.done;
        peptideFinderTask->done.store(true);
        proteinQueue.clear();

        sqlite::command insertIntegerSet(idpDb, "INSERT INTO IntegerSet (Value) VALUES (?)");
        for (int i=1; i <= maxProteinLength; ++i)
        {
            insertIntegerSet.binder() << i;
            insertIntegerSet.execute();
            insertIntegerSet.reset();
        }

        try
        {
            ITERATION_UPDATE(ilr, 0, 0, parserTask.inputFilepath + "*creating indexes");
            parser.createIndexes();
        }
        catch (exception& e)
        {
            // failure to create indexes is not fatal (need to check the database for the error)
            cerr << "\n[executePeptideFinderTask] thread " << boost::this_thread::get_id() << " failed to create indexes: " << e.what() << endl;
        }

        try
        {
            // run preqonvert if import settings specify it
            ITERATION_UPDATE(ilr, 0, 0, parserTask.inputFilepath + "*qonverting");
            parser.applyQValueFilter(*parserTask.analysis, 0.25);
        }
        catch (exception& e)
        {
            // failure during qonversion is not fatal
            cerr << "\n[executePeptideFinderTask] thread " << boost::this_thread::get_id() << " failed to apply Q value filter: " << e.what() << endl;
        }

        string idpDbFilepath = bfs::path(parserTask.inputFilepath).replace_extension(".idpDB").string();
        {
            sqlite::query filteredProteinIdQuery(idpDb, "SELECT Id FROM Protein");

            set<sqlite3_int64> filteredProteinIds;
            BOOST_FOREACH(sqlite::query::rows row, filteredProteinIdQuery)
                filteredProteinIds.insert(row.get<sqlite3_int64>(0));

            vector<size_t> filteredProteinIndexes;
            BOOST_FOREACH_FIELD((size_t index)(sqlite3_int64 id), proteinIdByIndex)
                if (filteredProteinIds.count(id) > 0)
                    filteredProteinIndexes.push_back(index);

            ITERATION_UPDATE(ilr, 0, 0, parserTask.inputFilepath + "*saving database");
            boost::mutex::scoped_lock ioLock(ioMutex);
            idpDb.save_to_file(idpDbFilepath.c_str());

            sqlite::database idpDbFile(idpDbFilepath, sqlite::no_mutex);
            
            // optimize for bulk insertion
            idpDbFile.execute("PRAGMA journal_mode=OFF;"
                              "PRAGMA synchronous=OFF;"
                              "PRAGMA automatic_indexing=OFF;"
                              "PRAGMA default_cache_size=500000;"
                              "PRAGMA temp_store=MEMORY"
                             );

            sqlite::command insertProteinData(idpDbFile, "INSERT INTO ProteinData (Id, Sequence) VALUES (?,?)");
            sqlite::command insertProteinMetadata(idpDbFile, "INSERT INTO ProteinMetadata (Id, Description) VALUES (?,?)");

            int proteinsWritten = 0;
            BOOST_FOREACH(size_t index, filteredProteinIndexes)
            {
                ITERATION_UPDATE(ilr, proteinsWritten++, filteredProteinIndexes.size(), parserTask.inputFilepath + "*writing protein data");
                proteome::ProteinPtr protein = proteinReaderTask.proteomeDataPtr->proteinListPtr->protein(index);
                const sqlite3_int64& id = proteinIdByIndex[index];

                insertProteinData.binder() << id << protein->sequence();
                insertProteinData.execute();
                insertProteinData.reset();

                insertProteinMetadata.binder() << id << protein->description;
                insertProteinMetadata.execute();
                insertProteinMetadata.reset();
            }
        }

        ITERATION_UPDATE(ilr, 0, 1, parserTask.inputFilepath + "*done");
        status = IterationListener::Status_Ok;
    }
    catch (exception& e)
    {
        boost::mutex::scoped_lock lock(proteinReaderTask.queueMutex);
        proteinReaderTask.done.store(proteinReaderTask.peptideFinderTasks.size());
        status = boost::copy_exception(runtime_error("[executePeptideFinderTask] error finding peptides for \"" + parserTask.inputFilepath + "\": " + e.what()));
    }
    catch (...)
    {
        boost::mutex::scoped_lock lock(proteinReaderTask.queueMutex);
        proteinReaderTask.done.store(proteinReaderTask.peptideFinderTasks.size());
        status = boost::copy_exception(runtime_error("[executePeptideFinderTask] unknown error finding peptides for \"" + parserTask.inputFilepath + "\""));
    }
}


void executeTaskGroup(const ProteinDatabaseTaskGroup& taskGroup,
                      const DistinctAnalysisMap& distinctAnalysisByFilepath,
                      IterationListenerRegistry* ilr)
{
    using boost::thread;
    using boost::lambda::_1;
    using boost::lambda::_2;
    using boost::lambda::_3;
    using boost::lambda::var;
    using boost::lambda::bind;

    boost::mutex ioMutex;

    vector<ParserTaskPtr> parserTasks;

    // parsing stage
    {
        // use list so iterators and references stay valid
        list<pair<boost::shared_ptr<thread>, ThreadStatus> > threads;

        BOOST_FOREACH(const string& inputFilepath, taskGroup.inputFilepaths)
        {
            if (distinctAnalysisByFilepath.count(inputFilepath) == 0)
                throw runtime_error("[Parser::parse()] unable to find analysis for file \"" + inputFilepath + "\"");

            ParserTaskPtr parserTask(new ParserTask(inputFilepath));
            parserTask->analysis = distinctAnalysisByFilepath.find(inputFilepath)->second;
            parserTask->ilr = ilr;
            parserTask->ioMutex = &ioMutex;
            parserTasks.push_back(parserTask);

            threads.push_back(make_pair(boost::shared_ptr<thread>(), ThreadStatus(IterationListener::Status_Ok)));
            threads.back().first.reset(new thread(executeParserTask, parserTasks.back(), threads.back().second));
        }

        set<boost::shared_ptr<thread> > finishedThreads;
        while (finishedThreads.size() < threads.size())
            BOOST_FOREACH_FIELD((boost::shared_ptr<thread>& t)(ThreadStatus& status), threads)
            {
                if (t->timed_join(bpt::seconds(1)))
                    finishedThreads.insert(t);

                if (status.exception.get())
                    boost::rethrow_exception(status.exception);
                else if (status.userCanceled)
                    return;
            }
    }

    // peptide finding stage
    {
        list<pair<boost::shared_ptr<thread>, ThreadStatus> > threads;

        ProteinReaderTaskPtr proteinReaderTask(new ProteinReaderTask);
        proteinReaderTask->proteomeDataPtr = taskGroup.proteomeDataPtr;
        proteinReaderTask->proteinCount = taskGroup.proteomeDataPtr->proteinListPtr->size();
        proteinReaderTask->done.store(0);

        for (size_t i=0; i < taskGroup.inputFilepaths.size(); ++i)
        {
            PeptideFinderTaskPtr peptideFinderTask(new PeptideFinderTask);
            peptideFinderTask->proteinReaderTask = proteinReaderTask;
            peptideFinderTask->parserTask = parserTasks[i];
            peptideFinderTask->done.store(false);
            peptideFinderTask->ilr = ilr;
            peptideFinderTask->ioMutex = &ioMutex;
            proteinReaderTask->peptideFinderTasks.push_back(peptideFinderTask);

            threads.push_back(make_pair(boost::shared_ptr<thread>(), IterationListener::Status_Ok));
            threads.back().first.reset(new thread(executePeptideFinderTask, peptideFinderTask, threads.back().second));
        }

        // threads will free their parserTask
        //parserTasks.clear();

        ThreadStatus status;
        boost::thread proteinReaderThread(executeProteinReaderTask, proteinReaderTask, status);

        proteinReaderThread.join();

        set<boost::shared_ptr<thread> > finishedThreads;
        while (finishedThreads.size() < threads.size())
            BOOST_FOREACH_FIELD((boost::shared_ptr<thread>& t)(ThreadStatus& status), threads)
            {
                if (t->timed_join(bpt::seconds(1)))
                    finishedThreads.insert(t);

                if (status.exception.get())
                    boost::rethrow_exception(status.exception);
                else if (status.userCanceled)
                    return;
            }
    }
    
    // fatal error if an idpDB didn't get saved
    BOOST_FOREACH(const string& inputFilepath, taskGroup.inputFilepaths)
        if (!bfs::exists(bfs::path(inputFilepath).replace_extension(".idpDB")))
            throw runtime_error("\n[executeTaskGroup] no database created for file \"" + inputFilepath + "\"");
}

} // namespace


Parser::Analysis::Analysis() : startTime(bdt::not_a_date_time) {}


void Parser::ImportSettingsCallback::operator() (const vector<ConstAnalysisPtr>& distinctAnalyses, bool& cancel) const
{
    throw runtime_error("[Parser::parse()] no import settings handler set");
}


void Parser::parse(const vector<string>& inputFilepaths, int maxThreads, IterationListenerRegistry* ilr) const
{
    if (inputFilepaths.empty())
        return;

    // get the set of distinct analyses in the input files
    DistinctAnalysisMap distinctAnalysisByFilepath;
    findDistinctAnalyses(inputFilepaths, distinctAnalysisByFilepath);

    vector<ConstAnalysisPtr> distinctAnalyses;
    BOOST_FOREACH(const DistinctAnalysisMap::value_type& nameAnalysisPair, distinctAnalysisByFilepath)
        if (find(distinctAnalyses.begin(), distinctAnalyses.end(), nameAnalysisPair.second) == distinctAnalyses.end())
            distinctAnalyses.push_back(nameAnalysisPair.second);

    // inform the caller about the distinct analyses and ask for databases and qonversion settings
    if (importSettingsCallback.get())
    {
        bool cancel = false;
        (*importSettingsCallback)(distinctAnalyses, cancel);
        if (cancel)
            return;
    }
    else
        throw runtime_error("[Parser::parse()] no import settings handler set");

    map<string, ProteomeDataPtr> proteinDatabaseByFilepath;
    BOOST_FOREACH(const ConstAnalysisPtr& analysis, distinctAnalyses)
    {
        const string& proteinDatabaseFilepath = analysis->importSettings.proteinDatabaseFilepath;
        ProteomeDataPtr& proteomeDataPtr = proteinDatabaseByFilepath[proteinDatabaseFilepath];

        try
        {
            if (!bfs::exists(proteinDatabaseFilepath))
                throw runtime_error("[Parser::parse()] protein database does not exist: \"" + proteinDatabaseFilepath + "\"");
        }
        catch (runtime_error& e)
        {
            throw runtime_error("[Parser::parse()] error checking for database \"" + proteinDatabaseFilepath + "\": " + e.what());
        }

        try
        {
            if (!proteomeDataPtr.get())
            {
                using namespace pwiz::proteome;
                proteomeDataPtr.reset(new ProteomeDataFile(proteinDatabaseFilepath, true));
                if (proteomeDataPtr->proteinListPtr->size() <= 50000)
                    proteomeDataPtr->proteinListPtr.reset(new ProteinListCache(proteomeDataPtr->proteinListPtr,
                                                                               ProteinListCacheMode_MetaDataAndSequence,
                                                                               50000));
            }
        }
        catch (runtime_error& e)
        {
            throw runtime_error("[Parser::parse()] unable to open protein database \"" + proteinDatabaseFilepath + "\": " + e.what());
        }
    }

    vector<ProteinDatabaseTaskGroup> taskGroups = createTasksPerProteinDatabase(inputFilepaths,
                                                                                distinctAnalysisByFilepath,
                                                                                proteinDatabaseByFilepath,
                                                                                maxThreads);

    BOOST_FOREACH(const ProteinDatabaseTaskGroup& taskGroup, taskGroups)
        executeTaskGroup(taskGroup, distinctAnalysisByFilepath, ilr);
}


void Parser::parse(const string& inputFilepath, int maxThreads, IterationListenerRegistry* ilr) const
{
    parse(vector<string>(1, inputFilepath), maxThreads, ilr);
}

string Parser::parseSource(const string& inputFilepath)
{
	IdentDataFile mzid(inputFilepath, 0, 0, true);
	
	string spectraDataName = mzid.dataCollection.inputs.spectraData[0]->name;
        if (spectraDataName.empty())
        {
            spectraDataName = bfs::path(mzid.dataCollection.inputs.spectraData[0]->location).replace_extension("").filename();
            if (spectraDataName.empty())
                throw runtime_error("no spectrum source name or location");
        }

	return spectraDataName;
}


END_IDPICKER_NAMESPACE