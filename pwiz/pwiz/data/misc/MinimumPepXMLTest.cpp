//
// MinimumPepXMLTest.cpp
//
//
// Original author: Kate Hoff <katherine.hoff@proteowizard.org>
//
// Copyright 2009 Spielberg Family Center for Applied Proteomics
//   Cedars-Sinai Medical Cnter, Los Angeles, California  90048
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

#include "MinimumPepXML.hpp"
#include "pwiz/utility/misc/unit.hpp"
#include <iostream>
#include <fstream>

using namespace std;
using namespace pwiz::data::pepxml;
using namespace pwiz::util;

ostream* os_ = 0;

Specificity makeSpecificity()
{
    Specificity specificity;
    specificity.cut = "theCake";
    specificity.noCut = "notTheCake";
    specificity.sense = "non";

    return specificity;

}

SampleEnzyme makeSampleEnzyme()
{
    SampleEnzyme sampleEnzyme;
    sampleEnzyme.name = "oxiclean";
   
    Specificity specificity = makeSpecificity();
    sampleEnzyme.specificity = specificity;

    return sampleEnzyme;

}

SearchDatabase makeSearchDatabase()
{
    SearchDatabase searchDatabase;
    searchDatabase.localPath = "http://www.eharmony.com";
    searchDatabase.type = "online dating service";

    return searchDatabase;

}

XResult makeXResult()
{
    XResult xResult;
    xResult.probability = 0.98;

    xResult.allNttProb.push_back(0.0000);
    xResult.allNttProb.push_back(0.0000);
    xResult.allNttProb.push_back(0.780);

    return xResult;

}

AnalysisResult makeAnalysisResult()
{
    AnalysisResult analysisResult;
    analysisResult.analysis = "real";
    
    XResult xResult = makeXResult();
    analysisResult.xResult = xResult;

    return analysisResult;

}

AlternativeProtein makeAlternativeProtein()
{
    AlternativeProtein alternativeProtein;
    alternativeProtein.protein = "Dos Pinos";
    alternativeProtein.proteinDescr = "leche";
    alternativeProtein.numTolTerm = "5";

    return alternativeProtein;

}

ModAminoAcidMass makeModAminoAcidMass()
{
    ModAminoAcidMass modAminoAcidMass;
    modAminoAcidMass.position = 2;
    modAminoAcidMass.mass = 12.345;

    return modAminoAcidMass;

}

ModificationInfo makeModificationInfo()
{
    ModificationInfo modificationInfo;
    modificationInfo.modifiedPeptide = "GATO";
    modificationInfo.modAminoAcidMass = makeModAminoAcidMass();

    return modificationInfo;
}

SearchHit makeSearchHit()
{
    SearchHit searchHit;
    searchHit.hitRank = 1;
    searchHit.peptide = "RAGMALLICK";
    searchHit.peptidePrevAA = "R";
    searchHit.peptideNextAA = "V";
    searchHit.protein = "PA";
    searchHit.proteinDescr = "Bioinfomagicist";
    searchHit.numTotalProteins = 1;
    searchHit.numMatchedIons = 9;
    searchHit.calcNeutralPepMass = 4.21399;
    searchHit.massDiff = .0004;
    searchHit.numTolTerm = 2;
    searchHit.numMissedCleavages = 3;
    searchHit.isRejected = 0;
    
    AnalysisResult analysisResult = makeAnalysisResult();
    searchHit.analysisResult = analysisResult;

    AlternativeProtein alternativeProtein = makeAlternativeProtein();
    searchHit.alternativeProteins.push_back(alternativeProtein);

    searchHit.modificationInfo = makeModificationInfo();

    return searchHit;

}

EnzymaticSearchConstraint makeEnzymaticSearchConstraint()
{
    EnzymaticSearchConstraint enzymaticSearchConstraint;
    
    enzymaticSearchConstraint.enzyme = "emyzne";
    enzymaticSearchConstraint.maxNumInternalCleavages = 1;
    enzymaticSearchConstraint.minNumTermini = 1;

    return enzymaticSearchConstraint;

}

AminoAcidModification makeAminoAcidModification()
{
    AminoAcidModification aminoAcidModification;
    
    aminoAcidModification.aminoAcid = "pm";
    aminoAcidModification.massDiff = 9.63333;
    aminoAcidModification.mass = 82.65;
    aminoAcidModification.variable = "c";
    aminoAcidModification.symbol = "r";

    return aminoAcidModification;

}

SearchSummary makeSearchSummary()
{
    SearchSummary searchSummary;
    searchSummary.baseName = "mseharmony";
    searchSummary.searchEngine = "yahooooo";
    searchSummary.precursorMassType = "A";
    searchSummary.fragmentMassType = "B";
    searchSummary.searchID = "ego";

    EnzymaticSearchConstraint enzymaticSearchConstraint = makeEnzymaticSearchConstraint();
    searchSummary.enzymaticSearchConstraint = enzymaticSearchConstraint;

    AminoAcidModification aminoAcidModification = makeAminoAcidModification();
    searchSummary.aminoAcidModifications.push_back(aminoAcidModification);
    searchSummary.aminoAcidModifications.push_back(aminoAcidModification);

    SearchDatabase searchDatabase = makeSearchDatabase();
    searchSummary.searchDatabase = searchDatabase;

    return searchSummary;

}


SearchResult makeSearchResult()
{
    SearchResult searchResult;
    SearchHit searchHit = makeSearchHit();
    searchResult.searchHit = searchHit;

    return searchResult;

}

SpectrumQuery makeSpectrumQuery()
{
    SpectrumQuery spectrumQuery;
    spectrumQuery.spectrum = "ultraviolet";
    spectrumQuery.startScan = 19120414;
    spectrumQuery.endScan = 19120415;
    spectrumQuery.precursorNeutralMass = 46328;
    spectrumQuery.assumedCharge = 1;
    spectrumQuery.index = 3547;
    spectrumQuery.retentionTimeSec = 432000; 
    
    SearchResult searchResult = makeSearchResult();
    spectrumQuery.searchResult = searchResult;

    return spectrumQuery;

}


MSMSRunSummary makeMSMSRunSummary()
{
    MSMSRunSummary msmsRunSummary;

    SampleEnzyme sampleEnzyme = makeSampleEnzyme();
    msmsRunSummary.sampleEnzyme = makeSampleEnzyme();

    SearchSummary searchSummary = makeSearchSummary();
    msmsRunSummary.searchSummary = searchSummary;

    SpectrumQuery spectrumQuery = makeSpectrumQuery();
    msmsRunSummary.spectrumQueries.push_back(spectrumQuery);
    msmsRunSummary.spectrumQueries.push_back(spectrumQuery);

    return msmsRunSummary;

}

Feature makeFeature()
{
    Feature feature;
    feature.mzMonoisotopic = 1.234;
    feature.retentionTime = 5.678;

    return feature;
}

void testSpecificity()
{
    if (os_) *os_ << "\ntestSpecificity() ... \n";

    Specificity specificity = makeSpecificity();

    ostringstream oss;
    XMLWriter writer(oss);
    specificity.write(writer);

    Specificity readSpecificity;
    istringstream iss(oss.str());
    readSpecificity.read(iss);

    unit_assert(specificity == readSpecificity);

    if (os_) *os_ << oss.str() << endl;

}

void testSampleEnzyme()
{
    if (os_) *os_ << "\ntestSampleEnzyme() ... \n";

    SampleEnzyme sampleEnzyme = makeSampleEnzyme();

    ostringstream oss;
    XMLWriter writer(oss);
    sampleEnzyme.write(writer);

    SampleEnzyme readSampleEnzyme;
    istringstream iss(oss.str());
    readSampleEnzyme.read(iss);

    unit_assert(sampleEnzyme == readSampleEnzyme);

    if (os_) *os_ << oss.str() << endl;

}

void testSearchDatabase()
{
    if (os_) *os_ << "\ntestSearchDatabase() ... \n";

    SearchDatabase searchDatabase = makeSearchDatabase();

    ostringstream oss;
    XMLWriter writer(oss);
    searchDatabase.write(writer);

    SearchDatabase readSearchDatabase;
    istringstream iss(oss.str());
    readSearchDatabase.read(iss);

    unit_assert(searchDatabase == readSearchDatabase);

    if (os_) *os_ << oss.str() << endl;

}

void testXResult()
{
    if (os_) *os_ << "\ntestXResult() ... \n";

    XResult xResult = makeXResult();

    ostringstream oss;
    XMLWriter writer(oss);
    xResult.write(writer);

    XResult readXResult;
    istringstream iss(oss.str());
    readXResult.read(iss);

    unit_assert(xResult == readXResult);

    if (os_) *os_ << oss.str() << endl;

}

void testAnalysisResult()
{
    if (os_) *os_ << "\ntestAnalysisResult() ...\n";

    AnalysisResult analysisResult = makeAnalysisResult();

    ostringstream oss;
    XMLWriter writer(oss);
    analysisResult.write(writer);

    AnalysisResult readAnalysisResult;
    istringstream iss(oss.str());
    readAnalysisResult.read(iss);

    unit_assert(analysisResult == readAnalysisResult);
    
    if(os_) *os_ << oss.str() << endl;

}

void testAlternativeProtein()
{

    if (os_) *os_ << "\ntestAlternativeProtein() ...\n";

    AlternativeProtein alternativeProtein = makeAlternativeProtein();

    ostringstream oss;
    XMLWriter writer(oss);
    alternativeProtein.write(writer);

    AlternativeProtein readAlternativeProtein;
    istringstream iss(oss.str());
    readAlternativeProtein.read(iss);

    unit_assert(alternativeProtein == readAlternativeProtein);

    if(os_) *os_ << oss.str() << endl;

}

void testModAminoAcidMass()
{
    if (os_) *os_ << "\ntestModAminoAcidMass() ...\n";

    ModAminoAcidMass modAminoAcidMass = makeModAminoAcidMass();

    ostringstream oss;
    XMLWriter writer(oss);
    modAminoAcidMass.write(writer);

    ModAminoAcidMass readModAminoAcidMass;
    istringstream iss(oss.str());
    readModAminoAcidMass.read(iss);

    unit_assert(modAminoAcidMass == readModAminoAcidMass);

    if(os_) *os_ << oss.str() << endl;
}

void testModificationInfo()
{
    if (os_) *os_ << "\ntestModificationInfo() ...\n";

    ModificationInfo modificationInfo = makeModificationInfo();

    ostringstream oss;
    XMLWriter writer(oss);
    modificationInfo.write(writer);

    ModificationInfo readModificationInfo;
    istringstream iss(oss.str());
    readModificationInfo.read(iss);

    unit_assert(modificationInfo == readModificationInfo);

    if(os_) *os_ << oss.str() << endl;

}

void testSearchHit()
{
    if (os_) *os_ << "\ntestSearchHit() ...\n";

    SearchHit searchHit = makeSearchHit();

    ostringstream oss;
    XMLWriter writer(oss);
    searchHit.write(writer);

    SearchHit readSearchHit;
    istringstream iss(oss.str());
    readSearchHit.read(iss);

    unit_assert(searchHit == readSearchHit);
    
    if(os_) *os_ << oss.str() << endl;
}

void testSearchResult()
{
    if(os_) *os_ << "\ntestSearchResult() ... \n";

    SearchResult searchResult = makeSearchResult();

    ostringstream oss;
    XMLWriter writer(oss);
    searchResult.write(writer);

    SearchResult readSearchResult;
    istringstream iss(oss.str());
    readSearchResult.read(iss);

    unit_assert(searchResult == readSearchResult);

    if(os_) *os_ << oss.str() << endl;


}

void testEnzymaticSearchConstraint()
{
    if (os_) *os_ << "\ntestEnzymaticSearchConstraint() ... \n";

    EnzymaticSearchConstraint enzymaticSearchConstraint = makeEnzymaticSearchConstraint();

    ostringstream oss;
    XMLWriter writer(oss);
    enzymaticSearchConstraint.write(writer);

    EnzymaticSearchConstraint readEnzymaticSearchConstraint;
    istringstream iss(oss.str());
    readEnzymaticSearchConstraint.read(iss);

    unit_assert(enzymaticSearchConstraint == readEnzymaticSearchConstraint);

    if(os_) *os_ << oss.str() << endl;

}

void testAminoAcidModification()
{
    if (os_) *os_ << "\ntestAminoAcidModification() ... \n";

    AminoAcidModification aminoAcidModification = makeAminoAcidModification();

    ostringstream oss;
    XMLWriter writer(oss);
    aminoAcidModification.write(writer);

    AminoAcidModification readAminoAcidModification;
    istringstream iss(oss.str());
    readAminoAcidModification.read(iss);

    unit_assert(aminoAcidModification == readAminoAcidModification);

    if(os_) *os_ << oss.str() << endl;

}

void testSearchSummary()
{
    if(os_) *os_ << "\ntestSearchSummary() ... \n";
    
    SearchSummary searchSummary = makeSearchSummary();

    ostringstream oss;
    XMLWriter writer(oss);
    searchSummary.write(writer);

    SearchSummary readSearchSummary;
    istringstream iss(oss.str());
    readSearchSummary.read(iss);

    unit_assert(searchSummary == readSearchSummary);

    if(os_) *os_ << oss.str() << endl;

}

void testSpectrumQuery()
{
    if(os_) *os_ << "\ntestSpectrumQuery() ... \n";
    
    SpectrumQuery spectrumQuery = makeSpectrumQuery();

    ostringstream oss;
    XMLWriter writer(oss);
    spectrumQuery.write(writer);

    SpectrumQuery readSpectrumQuery;
    istringstream iss(oss.str());
    readSpectrumQuery.read(iss);

    unit_assert(spectrumQuery == readSpectrumQuery);

    if(os_) *os_ << oss.str() << endl;

}

void testMSMSRunSummary()
{
    if(os_) *os_ << "\ntestMSMSRunSummary() ... \n";

    MSMSRunSummary msmsRunSummary = makeMSMSRunSummary();

    ostringstream oss;
    XMLWriter writer(oss);
    msmsRunSummary.write(writer);

    MSMSRunSummary readMSMSRunSummary;
    istringstream iss(oss.str());
    readMSMSRunSummary.read(iss);

    unit_assert(msmsRunSummary == readMSMSRunSummary);

    if(os_) *os_ << oss.str() << endl;

}

void testMSMSPipelineAnalysis()
{
    if(os_) *os_ << "\ntestMSMSPipelineAnalysis() ... \n";

    MSMSPipelineAnalysis msmsPipelineAnalysis;
    msmsPipelineAnalysis.date = "20000101";
    msmsPipelineAnalysis.summaryXML = "/2000/01/20000101/20000101.xml";
    msmsPipelineAnalysis.xmlns = "http://regis-web.systemsbiology.net/pepXML";
    msmsPipelineAnalysis.xmlnsXSI = "aruba";
    msmsPipelineAnalysis.XSISchemaLocation = "jamaica";
    
    MSMSRunSummary msrs = makeMSMSRunSummary();
    msmsPipelineAnalysis.msmsRunSummary = msrs;

    ostringstream oss;
    XMLWriter writer(oss);
    msmsPipelineAnalysis.write(writer);

    MSMSPipelineAnalysis readMSMSPipelineAnalysis;
    istringstream iss(oss.str());
    readMSMSPipelineAnalysis.read(iss);

    unit_assert(msmsPipelineAnalysis == readMSMSPipelineAnalysis);

    if(os_) *os_ << oss.str() << endl;

}


int main(int argc, char* argv[])
{

    try
        {
            if (argc>1 && !strcmp(argv[1],"-v")) os_ = &cout;
            if (os_) *os_ << "MinimumPepXMLTest ... \n";

            testSpecificity();
            testSampleEnzyme();
            testSearchDatabase();
            testXResult();
            testAnalysisResult();
            testAlternativeProtein();
            testModAminoAcidMass();
            testModificationInfo();
            testSearchHit();
            testSearchResult();
            testEnzymaticSearchConstraint();
            testAminoAcidModification();
            testSearchSummary();
            testSpectrumQuery();
            testMSMSRunSummary();
            testMSMSPipelineAnalysis();

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


