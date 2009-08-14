//
// $Id$
//
//
// Original author: Kate Hoff <katherine.hoff@proteowizard.org>
//
// Copyright 2009 Spielberg Family Center for Applied Proteomics
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


#include "MinimumPepXML.hpp"
#include "pwiz/utility/minimxml/SAXParser.hpp"
#include "boost/lexical_cast.hpp"
#include <vector>
#include <iterator>
#include <cstring>

using namespace std;
using namespace pwiz;
using namespace pwiz::data::pepxml;
using namespace pwiz::data::peakdata;
using namespace pwiz::minimxml;
using namespace minimxml::SAXParser;

ostream* _log = 0;

void setLogStream(ostream& os)
{
    _log = &os;
    return;

}

namespace{

string stringCastVector(const vector<double>& v)
{
    if ( v.size() == 0 ) return "";
    const char *delimiter = ",";
    const pair<const char*, const char*> bookends = make_pair("(",")");
    string result;
    result += bookends.first;

    vector<double>::const_iterator it = v.begin();
    for(; it < v.end() - 1; ++it)
        {
            result += boost::lexical_cast<string>(*it);
            result += delimiter;

        }

    result += boost::lexical_cast<string>(*it);
    result += bookends.second;

    return result;

}

vector<double> vectorCastString(const string& s)
{
    const char* delimiter = ",";
    const pair<const char*, const char*> bookends = make_pair("(\0",")\0");

    vector<double> result;
    string::const_iterator it = s.begin();
    ++it; // skip first bookend

    while (it != s.end()) // switch to using string::find here
        {
            string vectorEntry = "";
            while(strncmp(&(*it), delimiter, 1) && strncmp(&(*it), bookends.second, 1))
                {
                    vectorEntry += *it;
                    ++it;
                    
                }
            result.push_back(boost::lexical_cast<double>(vectorEntry));
            ++it; // skip the delimiter and eventually the second bookend

        }

    return result;

}
bool operator==(const vector<MatchPtr>& a, const vector<MatchPtr>& b) 
    {
        if (a.size() != b.size()) return false;
        vector<MatchPtr>::const_iterator a_it = a.begin();
        vector<MatchPtr>::const_iterator b_it = b.begin();
        for( ; a_it != a.end(); ++a_it, ++b_it)
            {
                if (!(**a_it == **b_it)) return false;

            }

        return true;

    }


} // anonymous namespace


PWIZ_API_DECL void Specificity::write(XMLWriter& writer) const
{
    XMLWriter::Attributes attributes;

    attributes.push_back(make_pair("cut", cut));
    attributes.push_back(make_pair("no_cut", noCut));
    attributes.push_back(make_pair("sense", sense));
    writer.startElement("specificity", attributes, XMLWriter::EmptyElement);
}

struct HandlerSpecificity : public SAXParser::Handler
{
    Specificity* specificity;
    HandlerSpecificity( Specificity* _specificity = 0 ) : specificity( _specificity ) {}

    virtual Status startElement(const string& name, const Attributes& attributes, stream_offset position)

    {

        if ( name == "specificity" )
            {
                getAttribute(attributes, "cut", specificity->cut);
                getAttribute(attributes, "no_cut", specificity->noCut);
                getAttribute(attributes, "sense", specificity->sense);
                return Handler::Status::Ok;

            }

        else
            {
                throw runtime_error(("[HandlerSpecificity] : Unexpected element name : " + name).c_str());
                return Handler::Status::Done;

            }


    }

};

PWIZ_API_DECL void Specificity::read(istream& is)
{
    HandlerSpecificity handler(this);
    parse(is, handler);

}

PWIZ_API_DECL bool Specificity::operator==(const Specificity& that) const
{
    return cut == that.cut &&
        noCut == that.noCut &&
        sense == that.sense;

}

PWIZ_API_DECL bool Specificity::operator!=(const Specificity& that) const
{
    return !(*this == that);

}

PWIZ_API_DECL void SampleEnzyme::write(XMLWriter& writer) const
{
    XMLWriter::Attributes attributes;

    attributes.push_back(make_pair("name", name));
    writer.startElement("sample_enzyme", attributes);
    specificity.write(writer);
    writer.endElement();

}

struct HandlerSampleEnzyme : public SAXParser::Handler
{
    SampleEnzyme* sampleEnzyme;
    HandlerSampleEnzyme( SampleEnzyme* _sampleEnzyme = 0 ) : sampleEnzyme( _sampleEnzyme ) {}

    virtual Status startElement(const string& name, const Attributes& attributes, stream_offset position)

    {
        if ( name == "sample_enzyme" )
            {

                getAttribute(attributes, "name", sampleEnzyme->name);
                return Handler::Status::Ok;


            }

        else if ( name == "specificity" )
            {

                _handlerSpecificity.specificity= &(sampleEnzyme->specificity);
                return Handler::Status(Status::Delegate, &(_handlerSpecificity));

            }

        else
            {

                throw runtime_error(("[HandlerSampleEnzyme] : Unexpected element name : "+ name).c_str());
                return Handler::Status::Done;

            }


    }

private:

    HandlerSpecificity _handlerSpecificity;

};

PWIZ_API_DECL void SampleEnzyme::read(istream& is)
{
    HandlerSampleEnzyme handler(this);
    parse(is, handler);

}

PWIZ_API_DECL bool SampleEnzyme::operator==(const SampleEnzyme& that) const
{
    return name == that.name &&
        specificity == that.specificity;

}

PWIZ_API_DECL bool SampleEnzyme::operator!=(const SampleEnzyme& that) const
{
    return !(*this == that);

}

PWIZ_API_DECL void SearchDatabase::write(XMLWriter& writer) const
{
    XMLWriter::Attributes attributes;
    attributes.push_back(make_pair("local_path", localPath));
    attributes.push_back(make_pair("type", type));

    writer.startElement("search_database", attributes, XMLWriter::EmptyElement);

}

struct HandlerSearchDatabase : public SAXParser::Handler
{
    SearchDatabase* searchDatabase;
    HandlerSearchDatabase(SearchDatabase* _searchDatabase = 0) : searchDatabase(_searchDatabase) {}

    virtual Status startElement(const string& name,
                                const Attributes& attributes,
                                stream_offset position)

    {
        if (name == "search_database")
            {
                getAttribute(attributes, "local_path", searchDatabase->localPath);
                getAttribute(attributes, "type", searchDatabase->type);
                return Handler::Status::Ok;

            }
        
        else
            {
                throw runtime_error(("[HandlerSearchDatabase] Unexpected element name : " + name).c_str());
                return Handler::Status::Done;

            }

    }

};

PWIZ_API_DECL void SearchDatabase::read(istream& is)
{
    HandlerSearchDatabase handlerSearchDatabase(this);
    parse(is, handlerSearchDatabase);

}

PWIZ_API_DECL bool SearchDatabase::operator==(const SearchDatabase& that) const
{
    return localPath == that.localPath &&
        type == that.type;

}

PWIZ_API_DECL bool SearchDatabase::operator!=(const SearchDatabase& that) const
{
    return !(*this == that);

}

PWIZ_API_DECL void Q3RatioResult::write(XMLWriter& writer) const
{
    XMLWriter::Attributes attributes;
    attributes.push_back(make_pair("light_firstscan", boost::lexical_cast<string>(lightFirstScan)));
    attributes.push_back(make_pair("light_lastscan", boost::lexical_cast<string>(lightLastScan)));
    attributes.push_back(make_pair("light_mass", boost::lexical_cast<string>(lightMass)));
    attributes.push_back(make_pair("heavy_firstscan", boost::lexical_cast<string>(heavyFirstScan)));
    attributes.push_back(make_pair("heavy_lastscan", boost::lexical_cast<string>(heavyLastScan)));
    attributes.push_back(make_pair("heavy_mass", boost::lexical_cast<string>(heavyMass)));
    attributes.push_back(make_pair("light_area", boost::lexical_cast<string>(lightArea)));
    attributes.push_back(make_pair("heavy_area", boost::lexical_cast<string>(heavyArea)));
    attributes.push_back(make_pair("q2_light_area", boost::lexical_cast<string>(q2LightArea)));
    attributes.push_back(make_pair("q2_heavy_area", boost::lexical_cast<string>(q2HeavyArea)));
    attributes.push_back(make_pair("decimal_ratio", boost::lexical_cast<string>(decimalRatio)));

    writer.startElement("q3ratio_result", attributes, XMLWriter::EmptyElement);

}

struct HandlerQ3RatioResult : public SAXParser::Handler
{
    Q3RatioResult* q3RatioResult;
    HandlerQ3RatioResult(Q3RatioResult* _q3RatioResult = 0) : q3RatioResult(_q3RatioResult) {}  

    virtual Status startElement(const string& name,
                              const Attributes& attributes,
                              stream_offset position)

    {
        if (name == "q3ratio_result")
            {
                getAttribute(attributes, "light_firstscan", q3RatioResult->lightFirstScan);
                getAttribute(attributes, "light_lastscan", q3RatioResult->lightLastScan);
                getAttribute(attributes, "light_mass", q3RatioResult->lightMass);
                getAttribute(attributes, "heavy_firstscan", q3RatioResult->heavyFirstScan);
                getAttribute(attributes, "heavy_lastscan", q3RatioResult->heavyLastScan);
                getAttribute(attributes, "heavy_mass", q3RatioResult->heavyMass);
                getAttribute(attributes, "light_area", q3RatioResult->lightArea);
                getAttribute(attributes, "heavy_area", q3RatioResult->heavyArea);
                getAttribute(attributes, "q2_light_area", q3RatioResult->q2LightArea);
                getAttribute(attributes, "q2_heavy_area", q3RatioResult->q2HeavyArea);
                getAttribute(attributes, "decimal_ratio", q3RatioResult->decimalRatio);
                
                return Handler::Status::Ok;

            }
        
        else
            {
                throw runtime_error(("[HandlerQ3RatioResult] Unexpected element name: " + name).c_str());
                return Handler::Status::Done;
            } 
      
    }

};

PWIZ_API_DECL void Q3RatioResult::read(istream& is)
{
    HandlerQ3RatioResult _handlerQ3RatioResult(this);
    parse(is, _handlerQ3RatioResult);

}

PWIZ_API_DECL bool Q3RatioResult::operator==(const Q3RatioResult& that) const
{
    return lightFirstScan == that.lightFirstScan &&
      lightLastScan == that.lightLastScan &&
      lightMass == that.lightMass &&
      heavyFirstScan == that.heavyFirstScan &&
      heavyLastScan == that.heavyLastScan &&
      heavyMass == that.heavyMass &&
      lightArea == that.lightArea &&
      heavyArea == that.heavyArea &&
      q2LightArea == that.q2LightArea &&
      q2HeavyArea == that.q2HeavyArea &&
      decimalRatio == that.decimalRatio;

}

PWIZ_API_DECL bool Q3RatioResult::operator!=(const Q3RatioResult& that) const
{
    return  !(*this == that);

}

PWIZ_API_DECL void PeptideProphetResult::write(XMLWriter& writer) const
{
    const string allNttProbStr  = stringCastVector(allNttProb);
  
    
    XMLWriter::Attributes attributes;
    attributes.push_back(make_pair("probability", boost::lexical_cast<string>(probability)));
    attributes.push_back(make_pair("all_ntt_prob", allNttProbStr));
    
    writer.startElement("peptideprophet_result", attributes);
    writer.endElement();
    
}

struct HandlerPeptideProphetResult : public SAXParser::Handler
{
    PeptideProphetResult* peptideProphetResult;
    HandlerPeptideProphetResult(PeptideProphetResult* _peptideProphetResult = 0) : peptideProphetResult(_peptideProphetResult) {}

    virtual Status startElement(const string& name,
                                const Attributes& attributes,
                                stream_offset position)

    {
        if (name == "peptideprophet_result")
            {
                getAttribute(attributes, "probability", peptideProphetResult->probability);
                getAttribute(attributes, "all_ntt_prob", _allNttProbStr);
                peptideProphetResult->allNttProb = vectorCastString(_allNttProbStr);
                
                return Handler::Status::Ok;

            }

        else
            {
                if (_log) *_log << ("[HandlerPeptideProphetResult] Ignoring non-essential element name : " + name).c_str() << endl;
                return Handler::Status::Ok;

            }

    }

private:

    string _allNttProbStr;

};

PWIZ_API_DECL void PeptideProphetResult::read(istream& is) 
{
    HandlerPeptideProphetResult handlerPeptideProphetResult(this);
    parse(is, handlerPeptideProphetResult);

}

PWIZ_API_DECL bool PeptideProphetResult::operator==(const PeptideProphetResult& that) const
{
    return probability == that.probability &&
        allNttProb == that.allNttProb;

}

PWIZ_API_DECL bool PeptideProphetResult::operator!=(const PeptideProphetResult& that) const
{
    return !(*this == that);

}

PWIZ_API_DECL void AnalysisResult::write(XMLWriter& writer) const
{
    XMLWriter::Attributes attributes;
    attributes.push_back(make_pair("analysis", analysis));

    writer.startElement("analysis_result", attributes);
    if (analysis == "peptideprophet") peptideProphetResult.write(writer);
    if (analysis == "q3") q3RatioResult.write(writer);
    writer.endElement();

}

struct HandlerAnalysisResult : public SAXParser::Handler
{
    AnalysisResult* analysisResult;
    HandlerAnalysisResult(AnalysisResult* _analysisResult = 0) : analysisResult(_analysisResult){}

    
    virtual Status startElement(const string& name,
                                const Attributes& attributes,
                                stream_offset position)

    {
        if ( name == "analysis_result" )
            {
                getAttribute(attributes, "analysis", analysisResult->analysis);
                return Handler::Status::Ok;

            }

        else if (name == "peptideprophet_result")
            {             
	        _handlerPeptideProphetResult.peptideProphetResult = &(analysisResult->peptideProphetResult);
                 return Handler::Status(Status::Delegate, &_handlerPeptideProphetResult);

            }

       else if (name == "q3ratio_result")
            {
	        _handlerQ3RatioResult.q3RatioResult = &(analysisResult->q3RatioResult);
                 return Handler::Status(Status::Delegate, &_handlerQ3RatioResult);

            }

        else 
            {
                if (_log) *_log << ("[HandlerAnalysisResult] Ignoring non-essential element name : " + name).c_str() << endl;
                return Handler::Status::Ok;

            }

    }

private:
    
    HandlerPeptideProphetResult _handlerPeptideProphetResult;
    HandlerQ3RatioResult _handlerQ3RatioResult;

};

PWIZ_API_DECL void AnalysisResult::read(istream& is)
{
    HandlerAnalysisResult handlerAnalysisResult(this);
    parse(is, handlerAnalysisResult);

}

PWIZ_API_DECL bool AnalysisResult::operator==(const AnalysisResult& that) const
{
    return analysis == that.analysis &&
        peptideProphetResult == that.peptideProphetResult &&
        q3RatioResult == that.q3RatioResult ;
}

PWIZ_API_DECL bool AnalysisResult::operator!=(const AnalysisResult& that) const
{
    return !(*this == that);

}

PWIZ_API_DECL void AlternativeProtein::write(XMLWriter& writer) const
{
    XMLWriter::Attributes attributes;
    attributes.push_back(make_pair("protein", protein));    
    attributes.push_back(make_pair("protein_descr", proteinDescr));
    attributes.push_back(make_pair("num_tol_term", numTolTerm));

    writer.startElement("alternative_protein", attributes, XMLWriter::EmptyElement);

}

struct HandlerAlternativeProtein : public SAXParser::Handler
{
    AlternativeProtein* alternativeProtein;
    HandlerAlternativeProtein(AlternativeProtein* _alternativeProtein = 0) : alternativeProtein(_alternativeProtein){}

    virtual Status startElement(const string& name, const Attributes& attributes, stream_offset position)

        {
            if ( name == "alternative_protein" )
                {

                    getAttribute(attributes, "protein", alternativeProtein->protein);
                    getAttribute(attributes, "protein_descr", alternativeProtein->proteinDescr);
                    getAttribute(attributes, "num_tol_term", alternativeProtein->numTolTerm);

                    return Handler::Status::Ok;

                }

            else
                {

                    throw runtime_error(("[HandlerAlternativeProtein] Unexpected element name : " + name).c_str());
                    return Handler::Status::Done;

                }

        }

};

PWIZ_API_DECL void AlternativeProtein::read(istream& is)
{
    HandlerAlternativeProtein handlerAlternativeProtein(this);
    parse(is, handlerAlternativeProtein);

}

PWIZ_API_DECL bool AlternativeProtein::operator==(const AlternativeProtein& that) const
{
    return protein == that.protein &&
        proteinDescr == that.proteinDescr &&
        numTolTerm == that.numTolTerm;

}

PWIZ_API_DECL bool AlternativeProtein::operator!=(const AlternativeProtein& that) const
{
    return !(*this == that);

}

PWIZ_API_DECL void ModAminoAcidMass::write(XMLWriter& writer) const
{
    XMLWriter::Attributes attributes;

    attributes.push_back(make_pair("position", boost::lexical_cast<string>(position)));
    attributes.push_back(make_pair("mass", boost::lexical_cast<string>(mass)));
    writer.startElement("mod_aminoacid_mass", attributes, XMLWriter::EmptyElement);

}

struct HandlerModAminoAcidMass : public SAXParser::Handler
{
    ModAminoAcidMass* modAminoAcidMass;
    HandlerModAminoAcidMass( ModAminoAcidMass* _modAminoAcidMass = 0 ) : modAminoAcidMass( _modAminoAcidMass ) {}

    virtual Status startElement(const string& name, const Attributes& attributes, stream_offset position)

    {
        if ( name == "mod_aminoacid_mass" )
            {
                getAttribute(attributes, "position", modAminoAcidMass->position);
                getAttribute(attributes, "mass", modAminoAcidMass->mass);
                return Handler::Status::Ok;


            }

        else
            {
                throw runtime_error(("[HandlerModAminoAcidMass] : Unexpected element name : " + name).c_str());
                return Handler::Status::Done;

            }

    }

};

PWIZ_API_DECL void ModAminoAcidMass::read(istream& is)
{
    HandlerModAminoAcidMass handler(this);
    parse(is, handler);

}

PWIZ_API_DECL bool ModAminoAcidMass::operator==(const ModAminoAcidMass& that) const
{
    return position == that.position &&
        mass == that.mass;

}

PWIZ_API_DECL bool ModAminoAcidMass::operator!=(const ModAminoAcidMass& that) const
{
    return !(*this == that);

}

PWIZ_API_DECL void ModificationInfo::write(XMLWriter& writer) const
{
    XMLWriter::Attributes attributes;

    attributes.push_back(make_pair("modified_peptide", modifiedPeptide));
    writer.startElement("modification_info", attributes);
    modAminoAcidMass.write(writer);
    writer.endElement();

}

struct HandlerModificationInfo : public SAXParser::Handler
{
    ModificationInfo* modificationInfo;
    HandlerModificationInfo( ModificationInfo* _modificationInfo = 0) : modificationInfo( _modificationInfo ) {}

    virtual Status startElement(const string& name, const Attributes& attributes, stream_offset position)

    {

        if ( name == "modification_info" )
            {

                getAttribute(attributes, "modified_peptide", modificationInfo->modifiedPeptide);
                return Handler::Status::Ok;


            }

        else if ( name == "mod_aminoacid_mass" )
            {

                _handlerModAminoAcidMass.modAminoAcidMass= &(modificationInfo->modAminoAcidMass);
                return Handler::Status(Status::Delegate, &(_handlerModAminoAcidMass));

            }

        else
            {

                throw runtime_error(("[HandlerModificationInfo] Unexpected element name : " + name).c_str());
                return Handler::Status::Done;

            }


    }

private:

    HandlerModAminoAcidMass _handlerModAminoAcidMass;

};

PWIZ_API_DECL void ModificationInfo::read(istream& is)
{
    HandlerModificationInfo handler(this);
    parse(is, handler);

}

PWIZ_API_DECL bool ModificationInfo::operator==(const ModificationInfo& that) const
{
    return modifiedPeptide == that.modifiedPeptide &&
        modAminoAcidMass == that.modAminoAcidMass;

}

PWIZ_API_DECL bool ModificationInfo::operator!=(const ModificationInfo& that) const
{
    return !(*this == that);

}

PWIZ_API_DECL void SearchHit::write(XMLWriter& writer) const
{
    XMLWriter::Attributes attributes;

    attributes.push_back(make_pair("hit_rank", boost::lexical_cast<string>(hitRank)));
    attributes.push_back(make_pair("peptide", peptide));
    attributes.push_back(make_pair("peptide_prev_aa", peptidePrevAA));
    attributes.push_back(make_pair("peptide_next_aa", peptideNextAA));
    attributes.push_back(make_pair("protein", protein));
    attributes.push_back(make_pair("protein_descr", proteinDescr));
    attributes.push_back(make_pair("num_tot_proteins", boost::lexical_cast<string>(numTotalProteins)));
    attributes.push_back(make_pair("num_matched_ions", boost::lexical_cast<string>(numMatchedIons)));
    attributes.push_back(make_pair("tot_num_ions", boost::lexical_cast<string>(totalNumIons)));
    attributes.push_back(make_pair("calc_neutral_pep_mass", boost::lexical_cast<string>(calcNeutralPepMass)));
    attributes.push_back(make_pair("massdiff", boost::lexical_cast<string>(massDiff)));
    attributes.push_back(make_pair("num_tol_term", boost::lexical_cast<string>(numTolTerm)));
    attributes.push_back(make_pair("num_missed_cleavages", boost::lexical_cast<string>(numMissedCleavages)));
    attributes.push_back(make_pair("is_rejected", boost::lexical_cast<string>(isRejected)));

    writer.startElement("search_hit", attributes);
    analysisResult.write(writer);
    vector<AlternativeProtein>::const_iterator it = alternativeProteins.begin();
    for(; it != alternativeProteins.end(); ++it)
        {
            it->write(writer);

        }
    modificationInfo.write(writer);
    writer.endElement();
    
}

struct HandlerSearchHit : public SAXParser::Handler
{
    SearchHit* searchHit;
    HandlerSearchHit( SearchHit* _searchHit = 0) : searchHit( _searchHit ) {}

    virtual Status startElement(const string& name, const Attributes& attributes, stream_offset position)
    {

        if ( name == "search_hit" )
            {
                getAttribute(attributes, "hit_rank", searchHit->hitRank);
                getAttribute(attributes, "peptide", searchHit->peptide);
                getAttribute(attributes, "peptide_prev_aa", searchHit->peptidePrevAA);
                getAttribute(attributes, "peptide_next_aa", searchHit->peptideNextAA);
                getAttribute(attributes, "protein", searchHit->protein);
                getAttribute(attributes, "protein_descr", searchHit->proteinDescr);
                getAttribute(attributes, "num_tot_proteins", searchHit->numTotalProteins);
                getAttribute(attributes, "num_matched_ions", searchHit->numMatchedIons);
                getAttribute(attributes, "tot_num_ions", searchHit->totalNumIons);
                getAttribute(attributes, "calc_neutral_pep_mass", searchHit->calcNeutralPepMass);
                getAttribute(attributes, "massdiff", searchHit->massDiff);
                getAttribute(attributes, "num_tol_term", searchHit->numTolTerm);
                getAttribute(attributes, "num_missed_cleavages", searchHit->numMissedCleavages);
                getAttribute(attributes, "is_rejected", searchHit->isRejected);

                return Handler::Status::Ok;


            }

        else if ( name == "analysis_result" )
            {
                _handlerAnalysisResult.analysisResult = &(searchHit->analysisResult);
                return Handler::Status(Status::Delegate, &(_handlerAnalysisResult));

            }
        
        else if ( name == "alternative_protein" )
            {
                searchHit->alternativeProteins.push_back(AlternativeProtein());
                _handlerAlternativeProtein.alternativeProtein = &(searchHit->alternativeProteins.back());
                return Handler::Status(Status::Delegate, &(_handlerAlternativeProtein));

            }

        else if ( name == "modification_info" )
            {
                _handlerModificationInfo.modificationInfo = &(searchHit->modificationInfo);
                return Handler::Status(Status::Delegate, &(_handlerModificationInfo));

            }

        else
            {
                if (_log) *_log << ("[HandlerSearchHit] Ignoring non-essential element name : " + name).c_str() << endl;
                return Handler::Status::Ok;

            }

    }

private:

    HandlerAnalysisResult _handlerAnalysisResult;
    HandlerAlternativeProtein _handlerAlternativeProtein;   
    HandlerModificationInfo _handlerModificationInfo;

};

PWIZ_API_DECL void SearchHit::read(istream& is)
{
    HandlerSearchHit handler(this);
    parse(is, handler);

}

PWIZ_API_DECL bool SearchHit::operator==(const SearchHit& that) const
{
    return hitRank == that.hitRank&&
        peptide == that.peptide &&
        peptidePrevAA == that.peptidePrevAA &&
        peptideNextAA == that.peptideNextAA &&
        protein == that.protein &&
        proteinDescr == that.proteinDescr &&
        numTotalProteins == that.numTotalProteins &&
        numMatchedIons == that.numMatchedIons &&
        totalNumIons == that.totalNumIons &&
        calcNeutralPepMass == that.calcNeutralPepMass && 
        massDiff == that.massDiff &&
        numTolTerm == that.numTolTerm &&
        numMissedCleavages == that.numMissedCleavages &&
        isRejected == that.isRejected &&
        analysisResult == that.analysisResult &&
        alternativeProteins == that.alternativeProteins &&
        modificationInfo == that.modificationInfo;
      
}

PWIZ_API_DECL bool SearchHit::operator!=(const SearchHit& that) const
{
    return !(*this == that);

}

PWIZ_API_DECL void SearchResult::write(XMLWriter& writer) const
{
    writer.startElement("search_result");
    searchHit.write(writer);
    writer.endElement();

}

struct HandlerSearchResult : public SAXParser::Handler
{
    SearchResult* searchresult;
    HandlerSearchResult( SearchResult* _searchresult = 0) : searchresult( _searchresult ) {}

    virtual Status startElement(const string& name, const Attributes& attributes, stream_offset position)

    {

        if ( name == "search_result" )
            {
                return Handler::Status::Ok;

            }

        else if ( name == "search_hit" )
            {

                _handlerSearchHit.searchHit= &(searchresult->searchHit);
                return Handler::Status(Status::Delegate, &(_handlerSearchHit));

            }

        else
            {
                throw runtime_error(("[HandlerSearchResult] Unexpected element name :" + name).c_str());
                return Handler::Status::Done;

            }


    }

private:

    HandlerSearchHit _handlerSearchHit;

};

PWIZ_API_DECL void SearchResult::read(istream& is)
{
    HandlerSearchResult handler(this);
    parse(is, handler);

}

PWIZ_API_DECL bool SearchResult::operator==(const SearchResult& that) const
{
    return searchHit == that.searchHit;

}

PWIZ_API_DECL bool SearchResult::operator!=(const SearchResult& that) const
{
    return !(*this == that);

}

PWIZ_API_DECL void EnzymaticSearchConstraint::write(XMLWriter& writer) const
{
    XMLWriter::Attributes attributes;

    attributes.push_back(make_pair("enzyme", enzyme));
    attributes.push_back(make_pair("max_num_internal_cleavages", boost::lexical_cast<string>(maxNumInternalCleavages)));
    attributes.push_back(make_pair("min_number_termini", boost::lexical_cast<string>(minNumTermini)));
    writer.startElement("enzymatic_search_constraint", attributes, XMLWriter::EmptyElement);

}

struct HandlerEnzymaticSearchConstraint : public SAXParser::Handler
{
    EnzymaticSearchConstraint* enzymaticSearchConstraint;
    HandlerEnzymaticSearchConstraint( EnzymaticSearchConstraint* _enzymaticSearchConstraint = 0 ) : enzymaticSearchConstraint( _enzymaticSearchConstraint ) {}

    virtual Status startElement(const string& name, const Attributes& attributes, stream_offset position)

    {

        if ( name == "enzymatic_search_constraint" )
            {

                getAttribute(attributes, "enzyme", enzymaticSearchConstraint->enzyme);
                getAttribute(attributes, "max_num_internal_cleavages", enzymaticSearchConstraint->maxNumInternalCleavages);
                getAttribute(attributes, "min_number_termini", enzymaticSearchConstraint->minNumTermini);
                return Handler::Status::Ok;


            }

        else
            {

                throw runtime_error(("[HandlerEnzymaticSearchConstraint] : Unexpected element name : " + name).c_str());
                return Handler::Status::Done;

            }


    }

};

PWIZ_API_DECL void EnzymaticSearchConstraint::read(istream& is)
{
    HandlerEnzymaticSearchConstraint handler(this);
    parse(is, handler);

}

PWIZ_API_DECL bool EnzymaticSearchConstraint::operator==(const EnzymaticSearchConstraint& that) const
{
    return enzyme == that.enzyme &&
        maxNumInternalCleavages == that.maxNumInternalCleavages &&
        minNumTermini == that.minNumTermini;

}

PWIZ_API_DECL bool EnzymaticSearchConstraint::operator!=(const EnzymaticSearchConstraint& that) const
{
    return !(*this == that);

}

PWIZ_API_DECL void AminoAcidModification::write(XMLWriter& writer) const
{
    XMLWriter::Attributes attributes;

    attributes.push_back(make_pair("aminoacid", aminoAcid));
    attributes.push_back(make_pair("massdiff", boost::lexical_cast<string>(massDiff)));
    attributes.push_back(make_pair("mass", boost::lexical_cast<string>(mass)));
    attributes.push_back(make_pair("variable", variable));
    attributes.push_back(make_pair("symbol", symbol));
    writer.startElement("aminoacid_modification", attributes, XMLWriter::EmptyElement);

}

struct HandlerAminoAcidModification : public SAXParser::Handler
{
    AminoAcidModification* aminoAcidModification;
    HandlerAminoAcidModification( AminoAcidModification* _aminoAcidModification = 0) : aminoAcidModification( _aminoAcidModification ) {}

    virtual Status startElement(const string& name, const Attributes& attributes, stream_offset position)

    {

        if ( name == "aminoacid_modification" )
            {
                getAttribute(attributes, "aminoacid", aminoAcidModification->aminoAcid);
                getAttribute(attributes, "massdiff", aminoAcidModification->massDiff);
                getAttribute(attributes, "mass", aminoAcidModification->mass);
                getAttribute(attributes, "variable", aminoAcidModification->variable);
                getAttribute(attributes, "symbol", aminoAcidModification->symbol);
                return Handler::Status::Ok;


            }

        else
            {

                throw runtime_error(("[HandlerAminoAcidModification] : Unexpected element name : "+ name).c_str());
                return Handler::Status::Done;

            }

    }

};

PWIZ_API_DECL void AminoAcidModification::read(istream& is)
{
    HandlerAminoAcidModification handler(this);
    parse(is, handler);

}

PWIZ_API_DECL bool AminoAcidModification::operator==(const AminoAcidModification& that) const
{
    return aminoAcid == that.aminoAcid &&
        massDiff == that.massDiff &&
        mass == that.mass &&
        variable == that.variable &&
        symbol == that.symbol;

}

PWIZ_API_DECL bool AminoAcidModification::operator!=(const AminoAcidModification& that) const
{
    return !(*this == that);

}

PWIZ_API_DECL void SearchSummary::write(XMLWriter& writer) const
{
    XMLWriter::Attributes attributes;

    attributes.push_back(make_pair("base_name", baseName));
    attributes.push_back(make_pair("search_engine", searchEngine));
    attributes.push_back(make_pair("precursor_mass_type", precursorMassType));
    attributes.push_back(make_pair("fragment_mass_type", fragmentMassType));
    attributes.push_back(make_pair("search_id", searchID));

    writer.startElement("search_summary", attributes);
    searchDatabase.write(writer);
    enzymaticSearchConstraint.write(writer);
    vector<AminoAcidModification>::const_iterator it = aminoAcidModifications.begin();
    for(; it != aminoAcidModifications.end(); ++it)
        {
            it->write(writer);

        }
    writer.endElement();

}

struct HandlerSearchSummary : public SAXParser::Handler
{
    SearchSummary* searchsummary;
    HandlerSearchSummary( SearchSummary* _searchsummary = 0) : searchsummary( _searchsummary ) {}

    virtual Status startElement(const string& name, const Attributes& attributes, stream_offset position)

    {

        if ( name == "search_summary" )
            {

                getAttribute(attributes, "base_name", searchsummary->baseName);
                getAttribute(attributes, "search_engine", searchsummary->searchEngine);
                getAttribute(attributes, "precursor_mass_type", searchsummary->precursorMassType);
                getAttribute(attributes, "fragment_mass_type", searchsummary->fragmentMassType);
                getAttribute(attributes, "search_id", searchsummary->searchID);
                return Handler::Status::Ok;


            }

        else if ( name == "search_database" )
            {

                _handlerSearchDatabase.searchDatabase= &(searchsummary->searchDatabase);
                return Handler::Status(Status::Delegate, &(_handlerSearchDatabase));

            }

        else if ( name == "enzymatic_search_constraint" )
            {
                _handlerEnzymaticSearchConstraint.enzymaticSearchConstraint = &(searchsummary->enzymaticSearchConstraint);
                return Handler::Status(Status::Delegate, &(_handlerEnzymaticSearchConstraint));

            }
        
        else if ( name == "aminoacid_modification" )
            {
                searchsummary->aminoAcidModifications.push_back(AminoAcidModification());
                _handlerAminoAcidModification.aminoAcidModification = &(searchsummary->aminoAcidModifications.back());
                return Handler::Status(Status::Delegate, &(_handlerAminoAcidModification));

            }

        else
            {

                if (_log) *_log << ("[HandlerSearchSummary] Ignoring non-essential element name : " + name).c_str() << endl;
                return Handler::Status::Ok;

            }


    }

private:

    HandlerSearchDatabase _handlerSearchDatabase;
    HandlerEnzymaticSearchConstraint _handlerEnzymaticSearchConstraint;
    HandlerAminoAcidModification _handlerAminoAcidModification;

};

PWIZ_API_DECL void SearchSummary::read(istream& is)
{
    HandlerSearchSummary handler(this);
    parse(is, handler);

}

PWIZ_API_DECL bool SearchSummary::operator==(const SearchSummary& that) const
{
    return baseName == that.baseName &&
        searchEngine == that.searchEngine &&
        precursorMassType == that.precursorMassType &&
        fragmentMassType == that.fragmentMassType &&
        searchID == that.searchID &&
        searchDatabase == that.searchDatabase &&
        enzymaticSearchConstraint == that.enzymaticSearchConstraint &&
        aminoAcidModifications == that.aminoAcidModifications;
    
}

PWIZ_API_DECL bool SearchSummary::operator!=(const SearchSummary& that) const
{
    return !(*this == that);

}

PWIZ_API_DECL void SpectrumQuery::write(XMLWriter& writer) const
{
    XMLWriter::Attributes attributes;
   
    attributes.push_back(make_pair("spectrum", spectrum));
    attributes.push_back(make_pair("start_scan", boost::lexical_cast<string>(startScan)));
    attributes.push_back(make_pair("end_scan", boost::lexical_cast<string>(endScan)));
    attributes.push_back(make_pair("precursor_neutral_mass", boost::lexical_cast<string>(precursorNeutralMass)));
    attributes.push_back(make_pair("assumed_charge", boost::lexical_cast<string>(assumedCharge)));
    attributes.push_back(make_pair("index", boost::lexical_cast<string>(index)));
    attributes.push_back(make_pair("retention_time_sec", boost::lexical_cast<string>(retentionTimeSec)));
   
    writer.startElement("spectrum_query", attributes);
    searchResult.write(writer);
    writer.endElement();
    
}

struct HandlerSpectrumQuery : public SAXParser::Handler
{
    SpectrumQuery* spectrumQuery;
    HandlerSpectrumQuery( SpectrumQuery* _spectrumQuery = 0) : spectrumQuery( _spectrumQuery ) {}

    virtual Status startElement(const string& name, const Attributes& attributes, stream_offset position)

    {

        if ( name == "spectrum_query" )
            {

                getAttribute(attributes, "spectrum", spectrumQuery->spectrum);
                getAttribute(attributes, "start_scan", spectrumQuery->startScan);
                getAttribute(attributes, "end_scan", spectrumQuery->endScan);
                getAttribute(attributes, "precursor_neutral_mass", spectrumQuery->precursorNeutralMass);
                getAttribute(attributes, "assumed_charge", spectrumQuery->assumedCharge);
                getAttribute(attributes, "index", spectrumQuery->index);
                getAttribute(attributes, "retention_time_sec", spectrumQuery->retentionTimeSec);
                return Handler::Status::Ok;


            }

        else if ( name == "search_result" )
            {
                _handlerSearchResult.searchresult = &(spectrumQuery->searchResult);
                return Handler::Status(Status::Delegate, &(_handlerSearchResult));

            }

        else
            {
                throw runtime_error(("[HandlerSpectrumQuery] Unexpected element name : " + name).c_str());
                return Handler::Status::Ok;

            }


    }

private:

    HandlerSearchResult _handlerSearchResult;

};

PWIZ_API_DECL void SpectrumQuery::read(istream& is)
{
    HandlerSpectrumQuery handler(this);
    parse(is, handler);

}

PWIZ_API_DECL bool SpectrumQuery::operator==(const SpectrumQuery& that) const
{
    return spectrum == that.spectrum &&
        startScan == that.startScan &&
        endScan == that.endScan &&
        precursorNeutralMass == that.precursorNeutralMass &&
        assumedCharge == that.assumedCharge &&
        index == that.index &&
        retentionTimeSec == that.retentionTimeSec &&
        searchResult == that.searchResult;

}

PWIZ_API_DECL bool SpectrumQuery::operator!=(const SpectrumQuery& that) const
{
    return !(*this == that);

}

PWIZ_API_DECL void MSMSRunSummary::write(XMLWriter& writer) const
{
    XMLWriter::Attributes attributes;

    writer.startElement("msms_run_summary", attributes);    
    sampleEnzyme.write(writer);
    searchSummary.write(writer);
    vector<SpectrumQuery>::const_iterator it = spectrumQueries.begin();
    for(; it != spectrumQueries.end(); ++it)
        {
            it->write(writer);
         
        }
    writer.endElement();

}

struct HandlerMSMSRunSummary : public SAXParser::Handler
{
    MSMSRunSummary* msmsrunsummary;
    HandlerMSMSRunSummary( MSMSRunSummary* _msmsrunsummary = 0 ) : msmsrunsummary( _msmsrunsummary ) {}

    virtual Status startElement(const string& name, const Attributes& attributes, stream_offset position)
    {
        if ( name == "msms_run_summary" )
            {

                return Handler::Status::Ok;


            }

        else if ( name == "sample_enzyme" )
            {
                _handlerSampleEnzyme.sampleEnzyme= &(msmsrunsummary->sampleEnzyme);
                return Handler::Status(Status::Delegate, &(_handlerSampleEnzyme));

            }

        else if ( name == "search_summary" )
            {
                _handlerSearchSummary.searchsummary= &(msmsrunsummary->searchSummary);
                return Handler::Status(Status::Delegate, &(_handlerSearchSummary));

            }

        else if ( name == "spectrum_query" )
            {
                msmsrunsummary->spectrumQueries.push_back(SpectrumQuery());
                _handlerSpectrumQuery.spectrumQuery= &(msmsrunsummary->spectrumQueries.back());
                return Handler::Status(Status::Delegate, &(_handlerSpectrumQuery));

            }

        else
           {
               if (_log) *_log << ("[HandlerMSMSRunSummary] Ignoring non-essential element : " + name).c_str() << endl;
               return Handler::Status::Ok;

           }

    }

private:

    HandlerSampleEnzyme _handlerSampleEnzyme;
    HandlerSearchSummary _handlerSearchSummary;
    HandlerSpectrumQuery _handlerSpectrumQuery;

};

PWIZ_API_DECL void MSMSRunSummary::read(istream& is)
{
    HandlerMSMSRunSummary handler(this);
    parse(is, handler);

}

PWIZ_API_DECL bool MSMSRunSummary::operator==(const MSMSRunSummary& that) const
{
    return searchSummary == that.searchSummary &&
        sampleEnzyme == that.sampleEnzyme &&
        spectrumQueries == that.spectrumQueries;

}

PWIZ_API_DECL bool MSMSRunSummary::operator!=(const MSMSRunSummary& that) const
{
    return !(*this == that);

}

PWIZ_API_DECL void MSMSPipelineAnalysis::write(XMLWriter& writer) const
{
    XMLWriter::Attributes attributes;


    attributes.push_back(make_pair("date", date));
    attributes.push_back(make_pair("summmary_xml", summaryXML));
    attributes.push_back(make_pair("xmlns", xmlns));
    attributes.push_back(make_pair("xmlns:xsi", xmlnsXSI));
    attributes.push_back(make_pair("xsi:schemaLocation", XSISchemaLocation));


    writer.startElement("msms_pipeline_analysis", attributes);
    msmsRunSummary.write(writer);
    writer.endElement();

}

struct HandlerMSMSPipelineAnalysis : public SAXParser::Handler
{
    MSMSPipelineAnalysis* msmspipelineanalysis;
    HandlerMSMSPipelineAnalysis( MSMSPipelineAnalysis* _msmspipelineanalysis = 0) : msmspipelineanalysis( _msmspipelineanalysis ) {}

    virtual Status startElement(const string& name, const Attributes& attributes, stream_offset position)

    {
        if ( name == "msms_pipeline_analysis" )
            {
                getAttribute(attributes, "date", msmspipelineanalysis->date);
                getAttribute(attributes, "summmary_xml", msmspipelineanalysis->summaryXML);
                getAttribute(attributes, "xmlns", msmspipelineanalysis->xmlns);
                getAttribute(attributes, "xmlns:xsi", msmspipelineanalysis->xmlnsXSI);
                getAttribute(attributes, "xsi:schemaLocation", msmspipelineanalysis->XSISchemaLocation);
                return Handler::Status::Ok;


            }

        else if ( name == "msms_run_summary" )
            {

                _handlerMSMSRunSummary.msmsrunsummary= &(msmspipelineanalysis->msmsRunSummary);
                return Handler::Status(Status::Delegate, &(_handlerMSMSRunSummary));

            }

        else
            {

                if (_log) *_log << ("[HandlerMSMSPipelineAnalysis] Ignoring non-essential element : " + name).c_str() << endl;
        //throw runtime_error(("[HandlerMSMSPipelineAnalysis] Unexpected element name : " + name).c_str());
                return Handler::Status::Ok;

            }


    }

private:

    HandlerMSMSRunSummary _handlerMSMSRunSummary;

};

PWIZ_API_DECL void MSMSPipelineAnalysis::read(istream& is)
{
    HandlerMSMSPipelineAnalysis handler(this);
    parse(is, handler);

}

PWIZ_API_DECL bool MSMSPipelineAnalysis::operator==(const MSMSPipelineAnalysis& that) const
{
    return date == that.date &&
        summaryXML == that.summaryXML &&
        xmlns == that.xmlns &&
        xmlnsXSI == that.xmlnsXSI &&
        XSISchemaLocation == that.XSISchemaLocation;

}

PWIZ_API_DECL bool MSMSPipelineAnalysis::operator!=(const MSMSPipelineAnalysis& that) const
{
    return !(*this == that);

}

PWIZ_API_DECL void Match::write(minimxml::XMLWriter& writer) const
{
    XMLWriter::Attributes attributes;
    attributes.push_back(make_pair("score", boost::lexical_cast<string>(score)));

    writer.startElement("match", attributes);

    spectrumQuery.write(writer);
    feature->write(writer);

    writer.endElement();

}

struct HandlerMatch : public SAXParser::Handler
{
    Match* match;
    HandlerMatch(Match* _match = 0) : match(_match){}

    virtual Status startElement(const string& name,
                                const Attributes& attributes,
                                stream_offset position)
    {
        if(name == "match")
            {
                getAttribute(attributes, "score", match->score);
                return Handler::Status::Ok;

            }

        else if (name == "spectrum_query")
            {

                _handlerSpectrumQuery.spectrumQuery = &(match->spectrumQuery);
                return Handler::Status(Status::Delegate, &_handlerSpectrumQuery);

            }

        else if (name == "feature")
            {
                _handlerFeature.feature = (match->feature).get();
                return Handler::Status(Status::Delegate, &_handlerFeature);

            }

        else
            {
                throw runtime_error(("[HandlerMatch] Unexpected element name: " + name).c_str());
                return Handler::Status::Done;

            }

    }

private:

    HandlerFeature _handlerFeature;
    HandlerSpectrumQuery _handlerSpectrumQuery;

};

PWIZ_API_DECL void Match::read(istream& is)
{

    if (!this) throw runtime_error("not this");
    HandlerMatch handlerMatch(this);
    SAXParser::parse(is, handlerMatch);


}

PWIZ_API_DECL bool Match::operator==(const Match& that) const
{
    return score == that.score &&
        spectrumQuery == (that.spectrumQuery) &&
        *feature == *(that.feature);

}

PWIZ_API_DECL bool Match::operator!=(const Match& that) const
{
    return !(*this == that);

}

PWIZ_API_DECL void MatchData::write(minimxml::XMLWriter& writer) const
{

    XMLWriter::Attributes attributes;
    attributes.push_back(make_pair("warpFunctionCalculator", warpFunctionCalculator));
    attributes.push_back(make_pair("searchNbhdCalculator", searchNbhdCalculator));

    writer.startElement("matchData", attributes);

    XMLWriter::Attributes attributes_m;
    attributes_m.push_back(make_pair("count", boost::lexical_cast<string>(matches.size())));
    writer.startElement("matches", attributes_m);

    vector<MatchPtr>::const_iterator match_it = matches.begin();
    for(; match_it != matches.end(); ++match_it)
        {
            (*match_it)->write(writer);

        }

    writer.endElement();
    writer.endElement();

}

struct HandlerMatchData : public SAXParser::Handler
{
    MatchData* matchData;
    HandlerMatchData(){}
    HandlerMatchData(MatchData* _matchData) : matchData(_matchData){}

    virtual Status startElement(const string& name,
                                const Attributes& attributes,
                                stream_offset position)
    {
        if(name == "matchData")
            {
                getAttribute(attributes, "warpFunctionCalculator", matchData->warpFunctionCalculator);
                getAttribute(attributes, "searchNbhdCalculator", matchData->searchNbhdCalculator);
                return Handler::Status::Ok;

            }

        else if (name == "matches")
            {
                getAttribute(attributes, "count", _count);
                return Handler::Status::Ok;

            }

        else
            {
                if (name != "match")
                    {
                        throw runtime_error(("[HandlerMatchData] Unexpected element name : " + name).c_str());
                        return Handler::Status::Done;
                    }

                matchData->matches.push_back(MatchPtr(new Match()));
                _handlerMatch.match = matchData->matches.back().get();
                return Handler::Status(Status::Delegate, &_handlerMatch);

            }

        if (_count != matchData->matches.size())
            {
                throw runtime_error("[HandlerMatchData] <matches count> != matchData._matches.size()");
                return Handler::Status::Done;

            }

    }

private:

    HandlerMatch _handlerMatch;
    size_t _count;

};

PWIZ_API_DECL void MatchData::read(istream& is)
{
    HandlerMatchData handlerMatchData(this);
    parse(is, handlerMatchData);

}


PWIZ_API_DECL bool MatchData::operator==(const MatchData& that) const
{
    return warpFunctionCalculator == that.warpFunctionCalculator &&
        searchNbhdCalculator == that.searchNbhdCalculator &&
        matches == that.matches;

}

PWIZ_API_DECL bool MatchData::operator!=(const MatchData& that) const
{
    return !(*this == that);

}

