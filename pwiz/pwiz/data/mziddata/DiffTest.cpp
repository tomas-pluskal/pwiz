//
// DiffTest.cpp
//
//
// Original author: Robert Burke <robetr.burke@proteowizard.org>
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
#include "examples.hpp"
#include "pwiz/utility/misc/unit.hpp"
#include <iostream>
#include <cstring>


using namespace std;
using namespace pwiz::util;
using namespace pwiz;
using namespace pwiz::mziddata;
using boost::shared_ptr;


ostream* os_ = 0;

void testString()
{
    if (os_) *os_ << "testString()\n";

    Diff<string> diff("goober", "goober");
    unit_assert(diff.a_b.empty() && diff.b_a.empty());
    unit_assert(!diff);

    diff("goober", "goo");
    unit_assert(diff);
    if (os_) *os_ << diff << endl;
}

void testIdentifiableType()
{
    if (os_) *os_ << "testIdentifiableType()\n";

    IdentifiableType a, b;
    a.id="a";
    a.name="a_name";
    b = a;

    Diff<IdentifiableType> diff(a, b);
    if (diff && os_) *os_ << diff << endl;
    unit_assert(!diff);

    b.id="b";
    b.name="b_name";
    
    diff(a, b);
    if (os_) *os_ << diff << endl;
    unit_assert(diff);
}


void testParamContainer()
{
    if (os_) *os_ << "testParamContainer()\n";

    ParamContainer a, b;
    a.userParams.push_back(UserParam("common"));
    b.userParams.push_back(UserParam("common"));
    a.cvParams.push_back(MS_m_z);
    b.cvParams.push_back(MS_m_z);

    Diff<ParamContainer> diff(a, b);
    unit_assert(!diff);

    a.userParams.push_back(UserParam("different", "1"));
    b.userParams.push_back(UserParam("different", "2"));
    a.cvParams.push_back(MS_charge_state);
    b.cvParams.push_back(MS_intensity);

    diff(a, b);
    if (os_) *os_ << diff << endl;
    unit_assert(diff);

    unit_assert(diff.a_b.userParams.size() == 1);
    unit_assert(diff.a_b.userParams[0] == UserParam("different","1"));
    unit_assert(diff.b_a.userParams.size() == 1);
    unit_assert(diff.b_a.userParams[0] == UserParam("different","2"));

    unit_assert(diff.a_b.cvParams.size() == 1);
    unit_assert(diff.a_b.cvParams[0] == MS_charge_state);
    unit_assert(diff.b_a.cvParams.size() == 1);
    unit_assert(diff.b_a.cvParams[0] == MS_intensity);
}

void testFragmentArray()
{
    if (os_) *os_ << "testFragmentArray()\n";

    FragmentArray a, b;

    a.values.push_back(1.0);
    a.Measure_ref = "Measure_ref";
    b = a;

    Diff<FragmentArray> diff(a, b);
    unit_assert(!diff);
    if (os_) *os_ << diff << endl;

    b.values.push_back(2.0);
    diff(a, b);
    unit_assert(diff);

    vector<float> values;
    values.push_back(1.0);
    b.setValues(values);

    diff(a, b);
    unit_assert(!diff);

    const char* value_str = "1 2.1 ";
    a.values.push_back(2.1);
    b.setValues(value_str);
    diff(a, b);
    unit_assert(!diff);

    unit_assert(a.getValues() == value_str);
}

void testIonType()
{
    if (os_) *os_ << "testIonType()\n";

    IonType a, b;
    a.index.push_back(1);
    a.charge = 1;
    a.paramGroup.set(MS_frag__a_ion);
    a.fragmentArray.push_back(FragmentArrayPtr(new FragmentArray));

    b = a;

    Diff<IonType> diff(a, b);
    if (os_) *os_ << diff << endl;
    unit_assert(!diff);

    b.index.push_back(2);
    diff(a, b);

    unit_assert(diff);

    vector<int> indices;
    indices.push_back(1);
    b.setIndex(indices);

    diff(a, b);
    unit_assert(!diff);

    b.charge = 2;
    diff(a, b);
    unit_assert(!diff);

    b.charge = 1;

    const char* indexStr = "1 ";
    b.setIndex(indexStr);
    diff(a, b);
    unit_assert(!diff);

    b.paramGroup.set(MS_frag__z_ion);
    diff(a, b);
    unit_assert(diff);
}


void testMaterial()
{
    Material a, b;

    a.contactRole.Contact_ref = "Contact_ref";
    //a.cvParam.set();
}


void testMeasure()
{
}


void testModParam()
{
}


void testPeptideEvidence()
{
}


void testProteinAmbiguityGroup()
{
}


void testProteinDetectionHypothesis()
{
}


void testSpectrumIdentificationList()
{
}


void testProteinDetectionList()
{
}


void testAnalysisData()
{
}


void testSearchDatabase()
{
}


void testSpectraData()
{
}


void testSourceFile()
{
}


void testInputs()
{
}


void testEnzyme()
{
}


void testEnzymes()
{
}


void testMassTable()
{
}


void testResidue()
{
}


void testAmbiguousResidue()
{
}


void testFilter()
{
}


void testSpectrumIdentificationProtocol()
{
}


void testProteinDetectionProtocol()
{
}


void testAnalysisProtocolCollection()
{
}


void testContact()
{
}


void testAffiliations()
{
}


void testPerson()
{
}


void testOrganization()
{
}


void testBibliographicReference()
{
}


void testProteinDetection()
{
}


void testSpectrumIdentification()
{
}


void testAnalysisCollection()
{
}


void testDBSequence()
{
}


void testModification()
{
}


void testSubstitutionModification()
{
}


void testPeptide()
{
}


void testSequenceCollection()
{
}


void testSampleComponent()
{
}


void testSample()
{
}


void testSearchModification()
{
}


void testSpectrumIdentificationItem()
{
}


void testSpectrumIdentificationResult()
{
}


void testAnalysisSampleCollection()
{
}


void testProvider()
{
}


void testContactRole()
{
}


void testAnalysisSoftware()
{
    if (os_) *os_ << "testAnalysisSoftware()\n";

    AnalysisSoftware a, b;

    Diff<AnalysisSoftware> diff(a,b);
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
    Diff<DataCollection> diff(a, b);
    unit_assert(!diff);

    // a.inputs
    a.inputs.sourceFile.push_back(SourceFilePtr(new SourceFile()));
    b.inputs.searchDatabase.push_back(SearchDatabasePtr(new SearchDatabase()));
    a.inputs.spectraData.push_back(SpectraDataPtr(new SpectraData()));
    
    // b.analysisData
    b.analysisData.spectrumIdentificationList.push_back(SpectrumIdentificationListPtr(new SpectrumIdentificationList()));
        
    diff(a, b);
    if (os_) *os_ << diff << endl;
    
}


void testMzIdentML()
{
    if (os_) *os_ << "testMzIdentML()\n";

    MzIdentML a, b;

    examples::initializeTiny(a);
    examples::initializeTiny(b);


    Diff<MzIdentML> diff(a, b);
    unit_assert(!diff);

    b.version = "version";
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
    if (os_) *os_ << diff << endl;

    unit_assert(diff);

    unit_assert(diff.a_b.version.empty());
    unit_assert(diff.b_a.version == "version");

    unit_assert(diff.a_b.cvs.size() == 1);
    unit_assert(diff.b_a.cvs.empty());
}

void test()
{
    testString();
    testParamContainer();
    testFragmentArray();
    testIonType();
    testMaterial();
    testMeasure();
    testModParam();
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
    testContact();
    testAffiliations();
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
    testContactRole();
    testAnalysisSoftware();
    testAnalysisSoftware();
    testMzIdentML();
    testIdentifiableType();
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

