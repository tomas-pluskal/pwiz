//
// $Id$
//
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

#include "Diff.hpp"
#include "TextWriter.hpp"
#include "examples.hpp"
#include "pwiz/utility/misc/unit.hpp"
#include "pwiz/utility/misc/Std.hpp"
#include <cstring>


using namespace pwiz::util;
using namespace pwiz::data;
using namespace pwiz::data::diff_impl;
using namespace pwiz::mziddata;


// TODO: Add Identifiable diff to all subclasses of Identifiable

ostream* os_ = 0;
const double epsilon = numeric_limits<double>::epsilon();

void testIdentifiable()
{
    if (os_) *os_ << "testIdentifiable()\n";

    Identifiable a, b;
    a.id="a";
    a.name="a_name";
    b = a;

    Diff<Identifiable, DiffConfig> diff(a, b);
    if (diff && os_) *os_ << diff_string<TextWriter>(diff) << endl;
    unit_assert(!diff);

    b.id="b";
    b.name="b_name";

    diff(a, b);
    if (os_) *os_ << diff_string<TextWriter>(diff) << endl;
    unit_assert(diff);
}

void testFragmentArray()
{
    if (os_) *os_ << "testFragmentArray()\n";

    FragmentArray a, b;

    a.values.push_back(1.0);
    a.measurePtr = MeasurePtr(new Measure("Measure_ref"));
    b = a;

    Diff<FragmentArray, DiffConfig> diff(a, b);
    unit_assert(!diff);
    if (os_) *os_ << diff_string<TextWriter>(diff) << endl;

    a.values.push_back(2.1);
    b.values.push_back(2.0);
    b.measurePtr = MeasurePtr(new Measure("fer_erusaeM"));
    diff(a, b);

    // a diff was found
    unit_assert(diff);

    // the values of the diff are correct
    unit_assert(diff.a_b.userParams.size() == 1);
    unit_assert(diff.b_a.userParams.size() == 1);
    unit_assert(diff.a_b.measurePtr.get());
    unit_assert(diff.a_b.measurePtr->id == "Measure_ref");
    unit_assert(diff.b_a.measurePtr.get());
    unit_assert(diff.b_a.measurePtr->id == "fer_erusaeM");

    if (os_) *os_ << diff_string<TextWriter>(diff) << endl;
}

void testIonType()
{
    if (os_) *os_ << "testIonType()\n";

    IonType a, b;
    a.index.push_back(1);
    a.charge = 1;
    a.set(MS_frag__a_ion);
    a.fragmentArray.push_back(FragmentArrayPtr(new FragmentArray));

    b = a;

    Diff<IonType, DiffConfig> diff(a, b);
    unit_assert(!diff);
    if (os_ && diff) *os_ << diff_string<TextWriter>(diff) << endl;

    b.index.back() = 2;
    b.charge = 2;
    b.set(MS_frag__z_ion);
    b.fragmentArray.push_back(FragmentArrayPtr(new FragmentArray));
    b.fragmentArray.back()->measurePtr = MeasurePtr(new Measure("Graduated_cylinder"));
    diff(a, b);

    // a diff was found
    unit_assert(diff);
    if (os_) *os_ << diff_string<TextWriter>(diff) << endl;

    // and correctly
    unit_assert(diff.a_b.index.size() == 1);
    unit_assert(diff.b_a.index.size() == 1);
    unit_assert_equal(*diff.a_b.index.begin(), 1.0, epsilon);
    unit_assert_equal(*diff.b_a.index.begin(), 2.0, epsilon);
    unit_assert_equal(diff.a_b.charge, 1.0, epsilon);
    unit_assert_equal(diff.b_a.charge, 2.0, epsilon);
    unit_assert(diff.a_b.ParamContainer::empty());
    unit_assert(diff.b_a.hasCVParam(MS_frag__z_ion));
    unit_assert(diff.b_a.fragmentArray.size() == 1);
    unit_assert(diff.b_a.fragmentArray.back()->measurePtr.get());
    unit_assert(diff.b_a.fragmentArray.back()->measurePtr->id == "Graduated_cylinder");
}


void testMeasure()
{
    if (os_) *os_ << "testMeasure()\n";

    Measure a, b;
    a.set(MS_product_ion_m_z, 200);
    b = a;

    Diff<Measure, DiffConfig> diff(a, b);
    unit_assert(!diff);

    b.set(MS_product_ion_intensity, 1);

    diff(a, b);
    if (os_) *os_ << diff_string<TextWriter>(diff) << endl;

    // diff was found
    unit_assert(diff);

    // and correctly
    unit_assert(diff.a_b.cvParams.size() == 0);
    unit_assert(diff.b_a.cvParams.size() == 1);
    unit_assert(diff.b_a.hasCVParam(MS_product_ion_intensity));
}

void testSearchModification()
{
    if (os_) *os_ << "testSearchModification()\n";

    SearchModification a, b;

    a.massDelta = 1;
    a.residues = "ABCD";
    a.set(UNIMOD_Gln__pyro_Glu);
    b = a;

    Diff<SearchModification, DiffConfig> diff(a, b);
    unit_assert(!diff);

    b.massDelta = 10;
    b.residues = "EFG";
    b.cvParams.clear();
    b.set(UNIMOD_Oxidation);

    diff(a, b);
    if (os_) *os_ << diff_string<TextWriter>(diff) << endl;

    // diff was found
    unit_assert(diff);

    // and correctly
    unit_assert_equal(diff.a_b.massDelta, 9, epsilon);
    unit_assert_equal(diff.b_a.massDelta, 9, epsilon);
    unit_assert(diff.a_b.residues == "ABCD");
    unit_assert(diff.b_a.residues == "EFG");
    unit_assert(!diff.a_b.cvParams.empty());
    unit_assert(diff.a_b.cvParams[0].cvid == UNIMOD_Gln__pyro_Glu);
    unit_assert(!diff.b_a.cvParams.empty());
    unit_assert(diff.b_a.cvParams[0].cvid == UNIMOD_Oxidation);
}


void testPeptideEvidence()
{
    if (os_) *os_ << "testPeptideEvidence()\n";

    PeptideEvidence a, b;

    Diff<PeptideEvidence, DiffConfig> diff(a, b);
    if (os_) *os_ << diff_string<TextWriter>(diff) << endl;
    unit_assert(!diff);

    a.dbSequencePtr = DBSequencePtr(new DBSequence("DBSequence_ref"));
    a.start = 1;
    a.end = 6;
    a.pre = "-";
    a.post = "-";
    a.translationTablePtr = TranslationTablePtr(new TranslationTable("TranslationTable_ref"));
    a.frame = 0;
    a.isDecoy = true;
    a.set(MS_Mascot_score, 15.71);
    b = a;

    diff(a,b);
    unit_assert(!diff);

    b.dbSequencePtr = DBSequencePtr(new DBSequence("fer_ecneuqeSBD"));
    b.start = 2;
    b.end = 7;
    b.pre = "A";
    b.post = "A";
    b.translationTablePtr = TranslationTablePtr(new TranslationTable("fer_elbaTnoitalsnarT"));
    b.frame = 1;
    b.isDecoy = false;
    b.set(MS_Mascot_expectation_value, 0.0268534444565851);

    diff(a, b);
    if (os_) *os_ << diff_string<TextWriter>(diff) << endl;

    // a diff was found
    unit_assert(diff);

    // and correctly
    unit_assert(diff.a_b.dbSequencePtr.get());
    unit_assert(diff.a_b.dbSequencePtr->id == "DBSequence_ref");
    unit_assert(diff.b_a.dbSequencePtr.get());
    unit_assert(diff.b_a.dbSequencePtr->id == "fer_ecneuqeSBD");
    unit_assert(diff.a_b.translationTablePtr.get());
    unit_assert(diff.a_b.translationTablePtr->id == "TranslationTable_ref");
    unit_assert(diff.b_a.translationTablePtr.get());
    unit_assert(diff.b_a.translationTablePtr->id == "fer_elbaTnoitalsnarT");
    unit_assert_equal(diff.a_b.start, 1.0, epsilon);
    unit_assert_equal(diff.b_a.start, 2.0, epsilon);
    unit_assert_equal(diff.a_b.end, 6.0, epsilon);
    unit_assert_equal(diff.b_a.end, 7.0, epsilon);
    unit_assert(diff.a_b.pre == "-");
    unit_assert(diff.b_a.pre == "A");
    unit_assert(diff.a_b.post == "-");
    unit_assert(diff.b_a.post == "A");
    unit_assert_equal(diff.a_b.frame, 0.0, epsilon);
    unit_assert_equal(diff.b_a.frame, 1.0, epsilon);
    unit_assert(diff.a_b.isDecoy == true);
    unit_assert(diff.b_a.isDecoy == false);
    unit_assert(diff.a_b.cvParams.size() == 0);
    unit_assert(diff.b_a.cvParams.size() == 1);
    unit_assert(diff.b_a.hasCVParam(MS_Mascot_expectation_value));

}


void testProteinAmbiguityGroup()
{
    if (os_) *os_ << "testProteinAmbiguityGroup()\n";

    ProteinAmbiguityGroup a, b;

    a.proteinDetectionHypothesis.push_back(ProteinDetectionHypothesisPtr(new ProteinDetectionHypothesis));
    a.proteinDetectionHypothesis.back()->dbSequencePtr = DBSequencePtr(new DBSequence("DBSequence_ref"));
    a.set(MS_Mascot_score, 164.4);
    b = a;

    Diff<ProteinAmbiguityGroup, DiffConfig> diff(a, b);
    unit_assert(!diff);

    b.proteinDetectionHypothesis.clear();
    b.set(MS_Mascot_expectation_value, 0.0268534444565851);

    diff(a, b);
    if (os_) *os_ << diff_string<TextWriter>(diff) << endl;

    // a diff was found
    unit_assert(diff);

    // and correctly
    unit_assert(diff.a_b.proteinDetectionHypothesis.size() == 1);
    unit_assert(diff.b_a.proteinDetectionHypothesis.size() == 0);
    unit_assert(diff.a_b.proteinDetectionHypothesis.back()->dbSequencePtr->id == "DBSequence_ref");
    unit_assert(diff.a_b.cvParams.size() == 0);
    unit_assert(diff.b_a.cvParams.size() == 1);
    unit_assert(diff.b_a.hasCVParam(MS_Mascot_expectation_value)); // TODO check vals also?

}


void testPeptideHypothesis()
{
    if (os_) *os_ << "testPeptideHypothesis()\n";

    PeptideHypothesis a, b;
    Diff<PeptideHypothesis, DiffConfig> diff(a,b);
    unit_assert(!diff);

    a.peptideEvidencePtr = PeptideEvidencePtr(new PeptideEvidence("pe_a"));
    a.spectrumIdentificationItemPtr.push_back(SpectrumIdentificationItemPtr(new SpectrumIdentificationItem("sii_a")));
    b.peptideEvidencePtr = PeptideEvidencePtr(new PeptideEvidence("pe_b"));
    b.spectrumIdentificationItemPtr.push_back(SpectrumIdentificationItemPtr(new SpectrumIdentificationItem("sii_b")));

    diff(a,b);
    if (os_) *os_ << diff_string<TextWriter>(diff) << endl;
    
    // a diff was found
    unit_assert(diff);

    // and correctly
    unit_assert(diff.a_b.peptideEvidencePtr.get());
    unit_assert(diff.a_b.peptideEvidencePtr->id =="pe_a");
    unit_assert(diff.b_a.peptideEvidencePtr.get());
    unit_assert(diff.b_a.peptideEvidencePtr->id == "pe_b");
    unit_assert(diff.a_b.spectrumIdentificationItemPtr.size() == 1);
    unit_assert(diff.a_b.spectrumIdentificationItemPtr.back()->id =="sii_a");
    unit_assert(diff.b_a.spectrumIdentificationItemPtr.size() == 1);
    unit_assert(diff.b_a.spectrumIdentificationItemPtr.back()->id == "sii_b");
}


void testProteinDetectionHypothesis()
{
    if (os_) *os_ << "testProteinDetectionHypothesis()\n";

    ProteinDetectionHypothesis a, b;
    Diff<ProteinDetectionHypothesis, DiffConfig> diff(a,b);
    unit_assert(!diff);

    a.dbSequencePtr = DBSequencePtr(new DBSequence("DBSequence_ref"));
    b.dbSequencePtr = DBSequencePtr(new DBSequence("fer_ecneuqeSBD"));
    a.passThreshold = true;
    b.passThreshold = false;
    a.peptideHypothesis.push_back(PeptideHypothesis());
    b.peptideHypothesis.push_back(PeptideHypothesis());
    
    a.peptideHypothesis.back().peptideEvidencePtr = PeptideEvidencePtr(new PeptideEvidence("pe_a"));
    a.peptideHypothesis.back().spectrumIdentificationItemPtr.push_back(SpectrumIdentificationItemPtr(new SpectrumIdentificationItem("sii_a")));
    b.peptideHypothesis.back().peptideEvidencePtr = PeptideEvidencePtr(new PeptideEvidence("pe_b"));
    b.peptideHypothesis.back().spectrumIdentificationItemPtr.push_back(SpectrumIdentificationItemPtr(new SpectrumIdentificationItem("sii_b")));

    a.set(MS_Mascot_expectation_value);

    diff(a,b);
    if (os_) *os_ << diff_string<TextWriter>(diff) << endl;

    // a diff was found
    unit_assert(diff);

    // and correctly
    unit_assert(diff.a_b.dbSequencePtr.get());
    unit_assert(diff.a_b.dbSequencePtr->id =="DBSequence_ref");
    unit_assert(diff.b_a.dbSequencePtr.get());
    unit_assert(diff.b_a.dbSequencePtr->id == "fer_ecneuqeSBD");
    unit_assert(diff.a_b.passThreshold == true);
    unit_assert(diff.b_a.passThreshold == false);
    unit_assert(diff.a_b.peptideHypothesis.size() == 1);
    unit_assert(diff.b_a.peptideHypothesis.size() == 1);
    unit_assert(diff.a_b.peptideHypothesis.back().peptideEvidencePtr->id == "pe_a");
    unit_assert(diff.b_a.peptideHypothesis.back().peptideEvidencePtr->id ==  "pe_b");
    unit_assert(diff.a_b.peptideHypothesis.back().spectrumIdentificationItemPtr.size() == 1);
    unit_assert(diff.a_b.peptideHypothesis.back().spectrumIdentificationItemPtr.back()->id =="sii_a");
    unit_assert(diff.b_a.peptideHypothesis.back().spectrumIdentificationItemPtr.size() == 1);
    unit_assert(diff.b_a.peptideHypothesis.back().spectrumIdentificationItemPtr.back()->id == "sii_b");
    unit_assert(diff.a_b.cvParams.size() == 1);
    unit_assert(diff.b_a.cvParams.size() == 0);
    unit_assert(diff.a_b.hasCVParam(MS_Mascot_expectation_value));

}

void testSpectrumIdentificationList()
{
    if (os_) *os_ << "testSpectrumIdentificationList()\n";

    SpectrumIdentificationList a, b;
    Diff<SpectrumIdentificationList, DiffConfig> diff(a,b);
    unit_assert(!diff);

    a.numSequencesSearched = 9;
    b.numSequencesSearched = 5;

    MeasurePtr testMeasure(new Measure());
    testMeasure->set(MS_Mascot_expectation_value);
    a.fragmentationTable.push_back(testMeasure);

    SpectrumIdentificationResultPtr testSIRPtr(new SpectrumIdentificationResult());
    testSIRPtr->set(MS_Mascot_expectation_value);
    a.spectrumIdentificationResult.push_back(testSIRPtr);

    diff(a,b);
    if (os_) *os_ << diff_string<TextWriter>(diff) << endl;

    // a diff was found
    unit_assert(diff);

    // and correctly
    unit_assert_equal(diff.a_b.numSequencesSearched,9.0,epsilon);
    unit_assert_equal(diff.b_a.numSequencesSearched,5.0,epsilon);
    unit_assert(diff.a_b.fragmentationTable.size() == 1);
    unit_assert(diff.b_a.fragmentationTable.size() == 0);
    unit_assert(diff.a_b.fragmentationTable.back()->hasCVParam(MS_Mascot_expectation_value));
    unit_assert(diff.a_b.spectrumIdentificationResult.size() == 1);
    unit_assert(diff.b_a.spectrumIdentificationResult.size() == 0);
    unit_assert(diff.a_b.spectrumIdentificationResult.back()->hasCVParam(MS_Mascot_expectation_value));

}


void testProteinDetectionList()
{
    if (os_) *os_ << "testProteinDetectionList()\n";

    ProteinDetectionList a,b;
    Diff<ProteinDetectionList, DiffConfig> diff(a,b);
    unit_assert(!diff);

    a.proteinAmbiguityGroup.push_back(ProteinAmbiguityGroupPtr(new ProteinAmbiguityGroup()));
    a.proteinAmbiguityGroup.back()->set(MS_Mascot_expectation_value, 0.0268534444565851);
    a.set(MS_frag__z_ion);
    b.set(MS_frag__b_ion);

    diff(a,b);
    if (os_) *os_ << diff_string<TextWriter>(diff) << endl;

    // a diff was found
    unit_assert(diff);

    // and correctly
    unit_assert(diff.a_b.proteinAmbiguityGroup.size() == 1);
    unit_assert(diff.b_a.proteinAmbiguityGroup.size() == 0);
    unit_assert(diff.a_b.proteinAmbiguityGroup.back()->hasCVParam(MS_Mascot_expectation_value));
    unit_assert(diff.a_b.cvParams.size() == 1);
    unit_assert(diff.b_a.cvParams.size() == 1);
    unit_assert(diff.a_b.hasCVParam(MS_frag__z_ion));
    unit_assert(diff.b_a.hasCVParam(MS_frag__b_ion));

}


void testAnalysisData()
{
    if (os_) *os_ << "testAnalysisData()\n";

    AnalysisData a, b;
    Diff<AnalysisData, DiffConfig> diff(a,b);
    unit_assert(!diff);

    a.spectrumIdentificationList.push_back(boost::shared_ptr<SpectrumIdentificationList>(new SpectrumIdentificationList()));
    a.spectrumIdentificationList.back()->numSequencesSearched = 5;
    b.spectrumIdentificationList.push_back(boost::shared_ptr<SpectrumIdentificationList>(new SpectrumIdentificationList()));
    b.spectrumIdentificationList.back()->numSequencesSearched = 15;

    a.proteinDetectionListPtr = ProteinDetectionListPtr(new ProteinDetectionList("rosemary"));
    b.proteinDetectionListPtr = ProteinDetectionListPtr(new ProteinDetectionList("sage"));

    diff(a,b);
    if (os_) *os_ << diff_string<TextWriter>(diff) << endl;

    // a diff was found
    unit_assert(diff);

    // and correctly
    unit_assert(diff.a_b.spectrumIdentificationList.size() == 1);
    unit_assert(diff.b_a.spectrumIdentificationList.size() == 1);
    unit_assert_equal(diff.a_b.spectrumIdentificationList.back()->numSequencesSearched, 5.0, epsilon);
    unit_assert_equal(diff.b_a.spectrumIdentificationList.back()->numSequencesSearched, 15.0, epsilon);
    unit_assert(diff.a_b.proteinDetectionListPtr.get());
    unit_assert(diff.b_a.proteinDetectionListPtr.get());
    unit_assert(diff.a_b.proteinDetectionListPtr->id == "rosemary");
    unit_assert(diff.b_a.proteinDetectionListPtr->id == "sage");

}


void testSearchDatabase()
{
    if (os_) *os_ << "testSearchDatabase()" << endl;

    SearchDatabase a, b;
    Diff<SearchDatabase, DiffConfig> diff(a,b);
    unit_assert(!diff);

    a.version = "1.0";
    b.version = "1.1";

    a.releaseDate = "20090726";
    b.releaseDate = "20090727";

    a.numDatabaseSequences = 5;
    b.numDatabaseSequences = 15;

    a.numResidues = 3;
    b.numResidues = 13;

    a.fileFormat.cvid = MS_frag__z_ion;
    a.databaseName.set(MS_frag__z_ion);

    diff(a,b);
    if (os_) *os_ << diff_string<TextWriter>(diff) << endl;

    // a diff was found
    unit_assert(diff);

    // and correctly
    unit_assert(diff.a_b.version == "1.0");
    unit_assert(diff.b_a.version == "1.1");
    unit_assert(diff.a_b.releaseDate == "20090726");
    unit_assert(diff.b_a.releaseDate == "20090727");
    unit_assert_equal(diff.a_b.numDatabaseSequences, 5.0, epsilon);
    unit_assert_equal(diff.b_a.numDatabaseSequences, 15.0, epsilon);
    unit_assert_equal(diff.a_b.numResidues, 3.0, epsilon);
    unit_assert_equal(diff.b_a.numResidues, 13.0, epsilon);
    unit_assert(!diff.a_b.fileFormat.empty());
    unit_assert(diff.b_a.fileFormat.empty());
    unit_assert(diff.a_b.fileFormat.cvid == MS_frag__z_ion);
    unit_assert(diff.a_b.databaseName.cvParams.size() == 1);
    unit_assert(diff.b_a.databaseName.cvParams.size() == 0);
    unit_assert(diff.a_b.databaseName.hasCVParam(MS_frag__z_ion));

}


void testSpectraData()
{
    if (os_) *os_ << "testSpectraData()\n" << endl;

    SpectraData a, b;
    Diff<SpectraData, DiffConfig> diff(a,b);
    unit_assert(!diff);

    a.location = "mahtomedi";
    b.location = "white_bear_lake";
    a.externalFormatDocumentation.push_back("wikipedia");
    b.externalFormatDocumentation.push_back("ehow");
    a.fileFormat.cvid = MS_frag__b_ion;

    diff(a,b);
    if (os_) *os_ << diff_string<TextWriter>(diff) << endl;

    // a diff was found
    unit_assert(diff);

    // and correctly
    unit_assert(diff.a_b.location == "mahtomedi");
    unit_assert(diff.b_a.location == "white_bear_lake");
    unit_assert(diff.a_b.externalFormatDocumentation.size() == 1);
    unit_assert(diff.b_a.externalFormatDocumentation.size() == 1);
    unit_assert(diff.a_b.externalFormatDocumentation.back() == "wikipedia");
    unit_assert(diff.b_a.externalFormatDocumentation.back() == "ehow");
    unit_assert(!diff.a_b.fileFormat.empty());
    unit_assert(diff.b_a.fileFormat.empty());
    unit_assert(diff.a_b.fileFormat.cvid == MS_frag__b_ion);

}


void testSourceFile()
{
    if (os_) *os_ << "testSourceFile()\n" << endl;

    SourceFile a,b;
    Diff<SourceFile, DiffConfig> diff(a,b);
    unit_assert(!diff);

    a.location = "madison";
    b.location = "middleton";
    a.fileFormat.cvid = MS_wolf;
    a.externalFormatDocumentation.push_back("The Idiot's Guide to External Formats");
    b.externalFormatDocumentation.push_back("External Formats for Dummies");
    a.set(MS_sample_number);
    b.set(MS_sample_name);

    diff(a,b);
    if (os_) *os_ << diff_string<TextWriter>(diff) << endl;

    // a diff was found
    unit_assert(diff);

    // and correctly
    unit_assert(diff.a_b.location == "madison");
    unit_assert(diff.b_a.location == "middleton");
    unit_assert(!diff.a_b.fileFormat.empty());
    unit_assert(diff.b_a.fileFormat.empty());
    unit_assert(diff.a_b.fileFormat.cvid == MS_wolf);
    unit_assert(diff.a_b.externalFormatDocumentation.size() == 1);
    unit_assert(diff.b_a.externalFormatDocumentation.size() == 1);
    unit_assert(diff.a_b.externalFormatDocumentation.back() == "The Idiot's Guide to External Formats");
    unit_assert(diff.b_a.externalFormatDocumentation.back() == "External Formats for Dummies");
    unit_assert(diff.a_b.cvParams.size() == 1);
    unit_assert(diff.b_a.cvParams.size() == 1);
    unit_assert(diff.a_b.hasCVParam(MS_sample_number));
    unit_assert(diff.b_a.hasCVParam(MS_sample_name));

}


void testInputs()
{

    if (os_) *os_ << "testInputs()\n";

    Inputs a, b;
    Diff<Inputs, DiffConfig> diff(a,b);
    unit_assert(!diff);

    a.sourceFile.push_back(SourceFilePtr(new SourceFile()));
    a.sourceFile.back()->location = "Sector 9";

    a.searchDatabase.push_back(SearchDatabasePtr(new SearchDatabase()));
    a.searchDatabase.back()->numDatabaseSequences = 100;

    a.spectraData.push_back(SpectraDataPtr(new SpectraData()));
    a.spectraData.back()->location = "Cloud 9";

    diff(a,b);
    if (os_) *os_ << diff_string<TextWriter>(diff) << endl;

    // a diff was found
    unit_assert(diff);

    // and correctly
    unit_assert(diff.a_b.sourceFile.size() == 1);
    unit_assert(diff.b_a.sourceFile.size() == 0);
    unit_assert(diff.a_b.sourceFile.back()->location == "Sector 9");
    unit_assert(diff.a_b.searchDatabase.size() == 1);
    unit_assert(diff.b_a.searchDatabase.size() == 0);
    unit_assert_equal(diff.a_b.searchDatabase.back()->numDatabaseSequences, 100.0, epsilon);
    unit_assert(diff.a_b.spectraData.size() == 1);
    unit_assert(diff.b_a.spectraData.size() == 0);
    unit_assert(diff.a_b.spectraData.back()->location == "Cloud 9");

}


void testEnzyme()
{
    if (os_) *os_ << "testEnzyme()\n";

    Enzyme a,b;
    Diff<Enzyme, DiffConfig> diff(a,b);
    if (diff && os_) *os_ << diff_string<TextWriter>(diff) << endl;
    unit_assert(!diff);

    a.id = "Donald Trump";
    b.id = "Donald Duck";
    a.nTermGain = "y";
    b.nTermGain = "n";
    a.cTermGain = "y";
    b.cTermGain = "n";
    a.semiSpecific = true;
    b.semiSpecific = false;
    a.missedCleavages = 1;
    b.missedCleavages = 5;
    a.minDistance = 2;
    b.minDistance = 4;
    a.siteRegexp = "^";
    b.siteRegexp = "$";
    a.enzymeName.set(MS_Trypsin);

    diff(a,b);
    if (os_) *os_ << diff_string<TextWriter>(diff) << endl;

    // a diff was found
    unit_assert(diff);

    // and correctly
    unit_assert(diff.a_b.id == "Donald Trump");
    unit_assert(diff.b_a.id == "Donald Duck");
    unit_assert(diff.a_b.nTermGain == "y");
    unit_assert(diff.b_a.nTermGain == "n");
    unit_assert(diff.a_b.cTermGain == "y");
    unit_assert(diff.a_b.semiSpecific);
    unit_assert(!diff.b_a.semiSpecific);
    unit_assert(diff.b_a.cTermGain == "n");
    unit_assert(diff.a_b.missedCleavages == 1);
    unit_assert(diff.b_a.missedCleavages == 5);
    unit_assert(diff.a_b.minDistance == 2);
    unit_assert(diff.b_a.minDistance == 4);
    unit_assert(diff.a_b.siteRegexp == "^");
    unit_assert(diff.b_a.siteRegexp == "$");
    unit_assert(diff.a_b.enzymeName.cvParams.size() == 1);
    unit_assert(diff.b_a.enzymeName.cvParams.size() == 0);
    unit_assert(diff.a_b.enzymeName.hasCVParam(MS_Trypsin));

}


void testEnzymes()
{
    if (os_) *os_ << "testEnzymes()\n";

    Enzymes a, b;
    Diff<Enzymes, DiffConfig> diff(a, b);
    if (diff && os_) *os_ << diff_string<TextWriter>(diff) << endl;

    a.independent = "indep";
    b.enzymes.push_back(EnzymePtr(new Enzyme()));
}


void testMassTable()
{
    if (os_) *os_ << "testMassTable()\n";

    MassTable a, b;

    a.id = "id";
    a.msLevel = "msLevel";

    ResiduePtr c(new Residue());
    a.residues.push_back(c);

    AmbiguousResiduePtr d(new AmbiguousResidue());
    a.ambiguousResidue.push_back(d);

    b = a;
    Diff<MassTable, DiffConfig> diff(a, b);
    unit_assert(!diff);

    b.id = "b_id";
    diff(a, b);
    unit_assert(diff);

    a.id = "b_id";
    b.msLevel = "b_msLevel";
    diff(a, b);
    unit_assert(diff);

    a.msLevel = "b_msLevel";
    b.residues.clear();
    diff(a, b);
    unit_assert(diff);

    a.residues.clear();
    b.ambiguousResidue.clear();
    diff(a, b);
    unit_assert(diff);
}


void testResidue()
{
    if (os_) *os_ << "testResidue()\n";

    Residue a, b;

    a.Code = "ON";
    a.Mass = 1.0;
    b = a;

    Diff<Residue, DiffConfig> diff(a, b);
    unit_assert(!diff);

    b.Code = "OFF";
    b.Mass = 2.0;

    diff(a, b);
    if (os_) *os_ << diff_string<TextWriter>(diff) << endl;

    unit_assert(diff);

    unit_assert(diff.a_b.Code == "ON");
    unit_assert(diff.b_a.Code == "OFF");
    unit_assert_equal(diff.a_b.Mass, 1.0, epsilon);
    unit_assert_equal(diff.b_a.Mass, 1.0, epsilon);
}


void testAmbiguousResidue()
{
    if (os_) *os_ << "testAmbiguousResidue()\n";

    AmbiguousResidue a, b;

    a.Code = "Z";
    a.set(MS_alternate_single_letter_codes);
    b = a;

    Diff<AmbiguousResidue, DiffConfig> diff(a, b);
    unit_assert(!diff);

    b.Code = "B";
    b.clear();
    b.set(MS_ambiguous_residues);

    diff(a, b);
    if (os_) *os_ << diff_string<TextWriter>(diff) << endl;

    unit_assert(diff);

    unit_assert(diff.a_b.Code == "Z");
    unit_assert(diff.b_a.Code == "B");

    //unit_assert(diff.a_b.cvParams.size() == 1);
    //unit_assert(diff.b_a.cvParams.size() == 1);
    //unit_assert(diff.a_b.hasCVParam(MS_alternate_single_letter_codes));
    //unit_assert(diff.a_b.hasCVParam(MS_ambiguous_residues));
}

void testFilter()
{
    if (os_) *os_ << "testFilter()\n";

    Filter a, b;

    a.filterType.set(MS_DB_filter_taxonomy);
    a.include.set(MS_DB_PI_filter);
    a.exclude.set(MS_translation_table);
    b = a;

    Diff<Filter, DiffConfig> diff(a, b);
    unit_assert(!diff);

    b.filterType.clear();
    b.filterType.set(MS_database_filtering);
    b.include.clear();
    b.include.set(MS_DB_filter_on_accession_numbers);
    b.exclude.clear();
    b.exclude.set(MS_DB_MW_filter);

    diff(a, b);
    if (os_) *os_ << diff_string<TextWriter>(diff) << endl;

    unit_assert(diff);

    unit_assert(diff.a_b.filterType.hasCVParam(MS_DB_filter_taxonomy));
    unit_assert(diff.b_a.filterType.hasCVParam(MS_database_filtering));
    unit_assert(diff.a_b.include.hasCVParam(MS_DB_PI_filter));
    unit_assert(diff.b_a.include.hasCVParam(MS_DB_filter_on_accession_numbers));
    unit_assert(diff.a_b.exclude.hasCVParam(MS_translation_table));
    unit_assert(diff.b_a.exclude.hasCVParam(MS_DB_MW_filter));
}



void testSpectrumIdentificationProtocol()
{
    if (os_) *os_ << "testSpectrumIdentificationProtocol()\n";


}


void testProteinDetectionProtocol()
{
    if (os_) *os_ << "testProteinDetectionProtocol()\n";


}


void testAnalysisProtocolCollection()
{
    if (os_) *os_ << "testAnalysisProtocolCollection()\n";
}


void testContact()
{
    if (os_) *os_ << "testContact()\n";

    Contact a("a_id", "a_name"), b;

    a.set(MS_contact_address, "address");
    a.set(MS_contact_phone_number, "phone");
    a.set(MS_contact_email, "email");
    a.set(MS_contact_fax_number, "fax");
    a.set(MS_contact_toll_free_phone_number, "tollFreePhone");

    b = a;

    Diff<Contact, DiffConfig> diff(a, b);
    unit_assert(!diff);

    b.id = "b_id";
    b.name = "b_name";

    diff(a, b);
    if (os_) *os_ << diff_string<TextWriter>(diff) << endl;

    unit_assert(diff);

    b.set(MS_contact_address, "b_address");
    b.set(MS_contact_phone_number, "b_phone");
    b.set(MS_contact_email, "b_email");
    b.set(MS_contact_fax_number, "b_fax");
    b.set(MS_contact_toll_free_phone_number, "b_tollFreePhone");


    diff(a, b);
    if (os_) *os_ << diff_string<TextWriter>(diff) << endl;

    unit_assert(diff);

}


void testPerson()
{
    if (os_) *os_ << "testPerson()\n";

    Person a, b;

    a.lastName = "last";
    a.firstName = "first";
    a.midInitials = "mi";

    a.affiliations.push_back(OrganizationPtr(new Organization("org")));

    b = a;
    Diff<Person, DiffConfig> diff(a, b);
    unit_assert(!diff);

    b.lastName = "smith";
    diff(a, b);
    unit_assert(diff);

    a.lastName = "smith";
    b.firstName = "john";
    diff(a, b);
    unit_assert(diff);

    a.firstName = "john";
    b.midInitials = "j.j.";
    diff(a, b);
    unit_assert(diff);

    a.midInitials = "j.j.";
    b.affiliations.clear();
    diff(a, b);
    unit_assert(diff);
}


void testOrganization()
{
    if (os_) *os_ << "testOrganization()\n";
}


void testBibliographicReference()
{
    if (os_) *os_ << "testBibliographicReference()\n";
}


void testProteinDetection()
{
    if (os_) *os_ << "testProteinDetection()\n";
}


void testSpectrumIdentification()
{
    if (os_) *os_ << "testSpectrumIdentification()\n";
}


void testAnalysisCollection()
{
    if (os_) *os_ << "testAnalysisCollection()\n";

}


void testDBSequence()
{
    if (os_) *os_ << "testDBSequence()\n";
}


void testModification()
{
    if (os_) *os_ << "testModification()\n";
}


void testSubstitutionModification()
{
    if (os_) *os_ << "testSubstitutionModification()\n";
}


void testPeptide()
{
    if (os_) *os_ << "testPeptide()\n";
}


void testSequenceCollection()
{
    if (os_) *os_ << "testSequenceCollection()\n";
}


void testSampleComponent()
{
    if (os_) *os_ << "testSampleComponent()\n";
}


void testSample()
{
    if (os_) *os_ << "testSample()\n";

    Sample a, b;

    a.contactRole.contactPtr = ContactPtr(new Person("contactPtr"));
    a.set(MS_sample_number);
    b = a;

    Diff<Sample, DiffConfig> diff(a, b);
    if (os_) *os_ << diff_string<TextWriter>(diff) << endl;
    unit_assert(!diff);

    b.contactRole.contactPtr = ContactPtr(new Person("fer_rehto"));
    b.set(MS_sample_name);

    diff(a, b);
    if (os_) *os_ << diff_string<TextWriter>(diff) << endl;

    // a diff was found
    unit_assert(diff);

    // and correctly
    unit_assert(diff.a_b.cvParams.size() == 0);
    unit_assert(diff.b_a.cvParams.size() == 1);
    unit_assert(diff.a_b.contactRole.contactPtr.get());
    unit_assert(diff.b_a.contactRole.contactPtr.get());
    unit_assert(diff.a_b.contactRole.contactPtr->id == "contactPtr");
    unit_assert(diff.b_a.contactRole.contactPtr->id == "fer_rehto");
    unit_assert(diff.b_a.hasCVParam(MS_sample_name));
}


void testSpectrumIdentificationItem()
{
    if (os_) *os_ << "testSpectrumIdentificationItem()\n";
}


void testSpectrumIdentificationResult()
{
    if (os_) *os_ << "testSpectrumIdentificationResult()\n";
}


void testAnalysisSampleCollection()
{
    if (os_) *os_ << "testAnalysisSampleCollection()\n";
}


void testProvider()
{
    if (os_) *os_ << "testProvider()\n";
}


void testContactRole()
{
    if (os_) *os_ << "testContactRole()\n";

    ContactRole a, b;

    a.contactPtr = ContactPtr(new Person("cid", "cname"));
    a.cvid = MS_software_vendor;

    b = a;

    Diff<ContactRole, DiffConfig> diff(a, b);
    if (os_) *os_ << diff_string<TextWriter>(diff) << endl;
    unit_assert(!diff);

    b.contactPtr = ContactPtr(new Organization("cid2", "cname2"));

    diff(a, b);

    if (os_) *os_ << diff_string<TextWriter>(diff) << endl;
    unit_assert(diff);

    unit_assert(diff.b_a.contactPtr.get());
    unit_assert(diff.a_b.contactPtr.get());
    unit_assert(diff.a_b.contactPtr->id == "cid");
    unit_assert(diff.b_a.contactPtr->id == "cid2");
    unit_assert(diff.a_b.contactPtr->name == "cname");
    unit_assert(diff.b_a.contactPtr->name == "cname2");
}


void testAnalysisSoftware()
{
    if (os_) *os_ << "testAnalysisSoftware()\n";

    AnalysisSoftware a, b;

    Diff<AnalysisSoftware, DiffConfig> diff(a,b);
    unit_assert(!diff);

    // a.version
    a.version="version";
    // b.contactRole
    // a.softwareName
    // b.URI
    b.URI="URI";
    // a.customizations
    a.customizations="customizations";

    diff(a, b);
}


void testDataCollection()
{
    if (os_) *os_ << "testDataCollection()\n";

    DataCollection a, b;
    Diff<DataCollection, DiffConfig> diff(a, b);
    unit_assert(!diff);

    // a.inputs
    a.inputs.sourceFile.push_back(SourceFilePtr(new SourceFile()));
    b.inputs.searchDatabase.push_back(SearchDatabasePtr(new SearchDatabase()));
    a.inputs.spectraData.push_back(SpectraDataPtr(new SpectraData()));

    // b.analysisData
    b.analysisData.spectrumIdentificationList.push_back(SpectrumIdentificationListPtr(new SpectrumIdentificationList()));

    diff(a, b);
    if (os_) *os_ << diff_string<TextWriter>(diff) << endl;

}


void testMzIdentML()
{
    if (os_) *os_ << "testMzIdentML()\n";

    MzIdentML a, b;

    examples::initializeTiny(a);
    examples::initializeTiny(b);

    Diff<MzIdentML, DiffConfig> diff(a, b);
    unit_assert(!diff);

    a.cvs.push_back(CV());
    b.analysisSoftwareList.push_back(AnalysisSoftwarePtr(new AnalysisSoftware));
    a.auditCollection.push_back(ContactPtr(new Contact()));
    b.bibliographicReference.push_back(BibliographicReferencePtr(new BibliographicReference));
    // a.analysisSampleCollection
    // b.sequenceCollection
    // a.analysisCollection
    // b.analysisProtocolCollection
    // a.dataCollection
    // b.bibliographicReference

    diff(a, b);
    if (os_) *os_ << diff_string<TextWriter>(diff) << endl;

    unit_assert(diff);

    unit_assert(diff.a_b.cvs.size() == 1);
    unit_assert(diff.b_a.cvs.empty());
}

void test()
{
    testIdentifiable();
    testContact();
    testContactRole();
    testFragmentArray();
    testIonType();
    testMeasure();
    testPeptideEvidence();
    testProteinAmbiguityGroup();
    testProteinDetectionHypothesis();
    testDataCollection();
    testSpectrumIdentificationList();
    testProteinDetectionList();
    testAnalysisData();
    testSearchDatabase();
    testSpectraData();
    testSourceFile();
    testInputs();
    testEnzyme();
    testEnzymes();
    testMassTable();
    testResidue();
    testAmbiguousResidue();
    testFilter();
    testSpectrumIdentificationProtocol();
    testProteinDetectionProtocol();
    testAnalysisProtocolCollection();
    testPerson();
    testOrganization();
    testBibliographicReference();
    testProteinDetection();
    testSpectrumIdentification();
    testAnalysisCollection();
    testDBSequence();
    testModification();
    testSubstitutionModification();
    testPeptide();
    testSequenceCollection();
    testSampleComponent();
    testSample();
    testSearchModification();
    testSpectrumIdentificationItem();
    testSpectrumIdentificationResult();
    testAnalysisSampleCollection();
    testProvider();
    testAnalysisSoftware();
    testAnalysisSoftware();
    testMzIdentML();
}

int main(int argc, char* argv[])
{
    try
    {
        if (argc>1 && !strcmp(argv[1],"-v")) os_ = &cout;
        test();
        return 0;
    }
    catch (exception& e)
    {
        cerr << e.what() << endl;
    }
    catch (...)
    {
        cerr << "Caught unknown exception.\n";
    }

    return 1;
}

