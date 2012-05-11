//
// $Id$
//
//
// Original author: Barbara Frewen <frewen@u.washington.edu>
//
// Copyright 2012 University of Washington - Seattle, WA 98195
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

/**
 * Class definition for PercolatorXmlReader, a class for parsing the
 * xml output from percolator, versions 1.15 and later.
 */

#include "PercolatorXmlReader.h"
#include "BlibMaker.h"

using namespace std;

namespace BiblioSpec {

PercolatorXmlReader::PercolatorXmlReader(BlibBuilder& maker, 
                                         const char* file, 
                                         const ProgressIndicator* progress)
: BuildParser(maker, file, progress), 
  currentState_(START_STATE), 
  qvalueThreshold_(getScoreThreshold(SQT))
{
    this->setFileName(file); // this is done for the saxhandler
    qvalueBuffer_ = new char[256];
    qvalueBuffer_[0] = '\0';
    qvalueBufferPosition_ = qvalueBuffer_;
}

PercolatorXmlReader::~PercolatorXmlReader()
{
    delete [] qvalueBuffer_;
}

/**
 * \brief Implementation of BuildParser virtual function.  Reads the
 * Percolator xml file and populates the vector of PSMs with the
 * information.
 */
bool PercolatorXmlReader::parseFile() {

    parse(); // the saxhandler will read the file and store the psms

    vector<const char*> extensions;
    extensions.push_back(".ms2");
    extensions.push_back(".cms2");
    extensions.push_back(".bms2");
    extensions.push_back(".pms2");

    vector<const char*> dirs;
    dirs.push_back("../");
    dirs.push_back("../../");
    dirs.push_back("../sequest/");

    BlibBuilder tmpBuilder;

    if( fileMap_.size() > 1 )
        initSpecFileProgress((int)fileMap_.size());

    // for each source file, read the .sqt for mods info, add spec to lib
    map<string, vector<PSM*> >::iterator mapAccess = fileMap_.begin();
    for(; mapAccess != fileMap_.end(); ++mapAccess){
        string filename = mapAccess->first;
        setSpecFileName(filename.c_str(), extensions, dirs);

        vector<PSM*>& psms = mapAccess->second;

        // create an SQTReader and get mods
        string fullFilename = getPath(getSpecFileName());
        replaceExtension(filename, "sqt");
        fullFilename += filename;
        SQTreader modsReader(tmpBuilder, fullFilename.c_str(), NULL);
        try{
            modsReader.openRead(false);
        } catch(BlibException& e){ 
            const char* msg = e.what();
            if( strncmp(msg, "Couldn't open", strlen("Couldn't open")) == 0 ){
                e.addMessage(" SQT file required for reading modifications.");
                throw e;
            }// ignore warning that perc wasn't run on the sqt
        }

        applyModifciations(psms, modsReader);

        psms_ = psms; // transfer to BuildParser list

        buildTables(PERCOLATOR_QVALUE);
        clearVector(psms_);
    }

    return true;
}

/**
 * Called when each start tag is read from the file. 
 */
void PercolatorXmlReader::startElement(const XML_Char* name,
                                       const XML_Char** attributes){

    switch(currentState_){
    case START_STATE:
        if(isElement("percolator_output", name)){
            currentState_ = ROOT_STATE;
        } 
        break;

    case ROOT_STATE:
        if(isElement("psms", name)){
            currentState_ = PSMS_STATE;
        } else if(isElement("peptides", name)){
            currentState_ = PEPTIDES_STATE;
        }
        break;

    case PSMS_STATE:
        if(isElement("psm", name)){
            parseId(attributes);
        } else if(isElement("q_value", name)){
            currentState_ = QVALUE_STATE;
            qvalueBufferPosition_ = qvalueBuffer_;//reset ptr into buffer 
        } else if(isElement("peptide_seq", name)){
            parseSequence(attributes);
        }
        break;

    case IGNORE_PSM_STATE:
    case QVALUE_STATE: //shouldn't actually get here
    case PEPTIDES_STATE:
        return;
    }

}

/**
 * Called when each closing tag is read from the file.
 */
void PercolatorXmlReader::endElement(const XML_Char* name) {
    switch(currentState_){
    case PSMS_STATE:
        if(isElement("psms", name)){
            currentState_ = ROOT_STATE;
        } else if(isElement("psm", name)){
            addCurPSM();
        }
        break;

    case IGNORE_PSM_STATE:
        if(isElement("psm", name)){
            currentState_ = PSMS_STATE;
        }
        break;
    case QVALUE_STATE:
        if(isElement("q_value", name)){
            // add the q-value to the curPSM_
            curPSM_->score = atof(qvalueBuffer_);
            if( curPSM_->score > qvalueThreshold_ ){
                currentState_ = IGNORE_PSM_STATE;
            } else {
                currentState_ = PSMS_STATE;
            }
        }
        break;
    default:
        return;
    }
}

/**
 * Given the attributes of a 'psm' tag, start a new PSM in which to
 * store data. Extract the filename, scan number, and charge from the
 * id.
 */
void PercolatorXmlReader::parseId(const XML_Char** attributes){

    const char* isdecoy = getAttrValue("p:decoy", attributes);
    if( isdecoy == NULL || strcmp("true", isdecoy)  == 0 ){
        currentState_ = IGNORE_PSM_STATE;
        return;
    }

    // if we have a current PSM, overwrite it, if not create a new one
    if( curPSM_ == NULL ){
        curPSM_ = new PSM();
    }

    // extract the filename, scan number, and charge from the id
    const char* idStr = getRequiredAttrValue("p:psm_id", attributes);
    char* buffer = new char[strlen(idStr) + 1];
    strcpy(buffer, idStr);

    // collect all tokens and interpret right to left in case filename has _
    vector<string> tokens;
    char* token = strtok(buffer, "_");
    while( token != NULL ){
        tokens.push_back(token);
        token = strtok(NULL, "_");
    }
    delete [] buffer;
    if( tokens.size() < 4 ){
        throw BlibException(false, "Error parsing psm_id '%s'.", idStr);
    }

    // tokens arranged as name, scan, charge, something
    tokens.pop_back();  // pop the last token whose meaning I've never known
    curPSM_->charge = atoi(tokens.back().c_str());  // charge
    tokens.pop_back();
    curPSM_->specKey = atoi(tokens.back().c_str()); // scan number
    tokens.pop_back();
    // reconstruct remaining tokens into a filename
    string filename = tokens.front();
    vector<string>::iterator it = tokens.begin();
    it++; // we already added the first, so skip it
    for(; it < tokens.end(); ++it){
        filename += "_";
        filename += (*it);
    }

    // hijack the specName field to store the filename
    curPSM_->specName = filename;

}

/**
 * Read the seq attribute and add it to the current PSM.
 */
void PercolatorXmlReader::parseSequence(const XML_Char** attributes){
    if(curPSM_ == NULL){
        throw BlibException(false, "Encountered a peptide sequence with no "
                            "spectrum to assign it to.");
    }

    // for now store the modified sequence in the unmod seq slot
    curPSM_->unmodSeq = getRequiredAttrValue("seq", attributes);
}

/**
 * Read all values between tags.  We only want the q-values, so only
 * read if we are in the QVALUES_STATE.
 */
void PercolatorXmlReader::characters(const XML_Char *s, int len){
    if( currentState_ != QVALUE_STATE ){
        return;
    }

    // copy len characters into current position in the qvalue buffer
    strncpy(qvalueBufferPosition_, s, len);
    qvalueBufferPosition_[len] = '\0';
    // move the current position to the end
    qvalueBufferPosition_ += len;
}

/**
 * Evaluate the current PSM and save it with the appropriate filename
 * if it passes the q-value threshold.
 */
void PercolatorXmlReader::addCurPSM(){
    if(curPSM_ == NULL)
        throw BlibException(false, "No PSM was read for this 'psm' tag.");

    if(curPSM_->score < qvalueThreshold_){ // add the psm to the map
        // retrieve and remove the filename from the psm
        string filename = curPSM_->specName;
        curPSM_->specName.clear();

        Verbosity::comment(V_DETAIL, "For file %s adding PSM: "
                           "scan %d, charge %d, sequence '%s'.",
                           filename.c_str(), curPSM_->specKey,
                           curPSM_->charge, curPSM_->unmodSeq.c_str());

        map<string, vector<PSM*> >::iterator mapAccess = 
            fileMap_.find(filename);
        if( mapAccess == fileMap_.end() ){ // not found, add the file
            vector<PSM*> tmpPsms(1, curPSM_);
            fileMap_[filename] = tmpPsms;
        } else {  // add this psm to existing file entry
            (mapAccess->second).push_back(curPSM_);
        }
        curPSM_ = NULL;
    }
}

/**
 * Use the SQTreader to translate the sequences with modification
 * symbols to a vector of mods, a seq with bracketed masses, and an
 * unmodified seq.
 */
void PercolatorXmlReader::applyModifciations(vector<PSM*>& psms, 
                                             SQTreader& modsReader){
    size_t numPsms = psms.size();
    for(size_t i=0; i < numPsms; i++){
        PSM* psm = psms[i];
        // we were temporarily storing the modified seq here
        string modSeq = psm->unmodSeq;
        psm->unmodSeq.clear();
        modsReader.parseModifiedSeq(modSeq.c_str(), psm->unmodSeq, 
                                    psm->mods, false);
    }
}



} // namespace

/*
 * Local Variables:
 * mode: c
 * c-basic-offset: 4
 * End:
 */
