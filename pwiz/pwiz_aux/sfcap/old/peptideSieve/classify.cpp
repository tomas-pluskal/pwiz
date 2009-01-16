//
// Original Author: Parag Mallick
//
// Copyright 2009 Spielberg Family Center for Applied Proteomics 
//   Cedars Sinai Medical Center, Los Angeles, California  90048
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



#include "classify.hpp"

  vector<double> ProteotypicClassifier::convertToProperties(const string &peptide,map<char,vector<double> > & propertyMap, double mass){
    map<char,int> composition;
    for(size_t i=0;i<peptide.length();i++){
      composition[peptide[i]]++;
    }
    vector<double> propertyVector;
    string AAs = "ACDEFGHIKLMNPQRSTVWY";
    propertyVector.push_back(peptide.length() - 2);
    propertyVector.push_back(mass);
    for(size_t aa=0;aa<AAs.length();aa++){
      propertyVector.push_back(composition[AAs[aa]]);
    }
    for(size_t i=0;i<494;i++){
      double total = 0;
      for(size_t aa=0;aa<AAs.length();aa++){
	total += composition[AAs[aa]] * propertyMap[AAs[aa]][i];
      }
      propertyVector.push_back(total);
      propertyVector.push_back(total/peptide.length());
    }    
    return propertyVector;
  }


  ProteotypicResult ProteotypicClassifier::classify(vector<double> propertyVector,string experimentalDesign){
    double cz_esiResultA[2];
    double cz_maldiResultA[2];
    double isb_esiResultA[2];
    double isb_icatResultA[2];
    double arrayData[2+20+494+494];	
    for(size_t i=0;i<propertyVector.size();i++){
      arrayData[i]=propertyVector[i];
      //	  cout<<i<<"\t"<<propertyVector[i]<<endl;
    }
    //	cout<<endl;

    ProteotypicResult classifierResult;

    if(experimentalDesign == "ALL"){
      classify_CZ_ESI(arrayData,cz_esiResultA);
      classify_CZ_MALDI(arrayData,cz_maldiResultA);
      classify_ISB_ESI(arrayData,isb_esiResultA);
      classify_ISB_ICAT(arrayData,isb_icatResultA);
      classifierResult._cz_esiResult = cz_esiResultA[0];
      classifierResult._cz_maldiResult = cz_maldiResultA[0];
      classifierResult._isb_esiResult = isb_esiResultA[0];
      classifierResult._isb_icatResult = isb_icatResultA[0];
    }
    else if(experimentalDesign == "PAGE_MALDI"){
      classify_CZ_MALDI(arrayData,cz_maldiResultA);
      classifierResult._cz_maldiResult = cz_maldiResultA[0];
    }
    else if(experimentalDesign == "ICAT_ESI"){
      classify_ISB_ICAT(arrayData,isb_icatResultA);
      classifierResult._isb_icatResult = isb_icatResultA[0];
    }
    else if(experimentalDesign == "PAGE_ESI"){
      classify_CZ_ESI(arrayData,cz_esiResultA);
      classifierResult._cz_esiResult = cz_esiResultA[0];
    }
    else if(experimentalDesign == "MUDPIT_ESI"){
      classify_ISB_ESI(arrayData,isb_esiResultA);
      classifierResult._isb_esiResult = isb_esiResultA[0];
    }
    else if(experimentalDesign == "NONE"){
      //do nothing because you aren't supposed to!
    }
    else{
      throw runtime_error("Unknown classification protocol: options include ALL, PAGE_MALDI, ICAT_ESI, PAGE_ESI, MUDPIT_ESI, NONE");
    }
    return classifierResult;
  }



void ProteotypicClassifier::processFASTAFile(const string& filename, map<char,vector<double> > & propertyMap){

    try{
      cout << "\nProcessing file: " << filename << endl; 
      string fasta_file = filename;
      fasta<string> mf(fasta_file);
      fasta<string>::const_iterator iter = mf.begin();
      fasta<string>::const_iterator stop = mf.end();

      getOutputStream(filename);
      getPropertyStream(filename);

      while( iter != stop ) {
	const fasta_seq<string>* fseq = *iter;
	// fasta headers stored as strings
	const string& header = fseq->get_header();
	// sequence should behave like a basic_string
	const string& str = fseq->get_seq();
	//      cout<<header<<endl<<str<<endl;
	Digest peptides(str,config_);
	size_t gap = header.find_first_of(' ');
	if(gap > 50){gap=50;}
	string shortHeader = header.substr(0,gap);
	vector<ProteotypicResult> classificationResults;
	
	for(size_t pepNdx=0;pepNdx<peptides.numPeptides();pepNdx++){
	  //	cout<<pepNdx<<endl;
	  string peptide = peptides.currentPeptide();
	  //	cout<<peptide<<endl;
	  vector<double> propertyVector = convertToProperties(peptide,propertyMap,peptides.currentMass());
	  printPropertyVector(shortHeader,peptide,propertyVector);
	  ProteotypicResult p = classify(propertyVector, config_._experimentalDesign);
	  p._peptide = peptide;
	  p._protein = shortHeader;
	  classificationResults.push_back(p);
	  peptides.next();
	}	
  outputFASTAResult(classificationResults,config_._experimentalDesign, config_._pValue);
	classificationResults.clear();
	++iter;
      }

      releaseOutputStream();
      releasePropertyStream();
  }
  catch(const std::exception& err) 
    {
      cerr<<err.what()<<endl;
    }
  catch(...) 
    {
      cerr<<"Unhandled exception. Gotta Jet G."<<endl;
    }
}


void ProteotypicClassifier::processTXTFile(const string& filename, map<char,vector<double> > & propertyMap){
  try{
    cout << "\nProcessing file: " << filename << endl; 

    ifstream inFile(filename.c_str());
    if(!inFile){
      throw std::runtime_error("Unable to read input file: " + filename);
    }
    getOutputStream(filename);
    getPropertyStream(filename);

    string peptideName,peptide;
    Digest digestBox;

    while(inFile>>peptideName>>peptide) {
      double peptideMass = digestBox.computeMass(peptide);
      vector<double> propertyVector = convertToProperties(peptide,propertyMap,peptideMass);
      printPropertyVector(peptideName,peptide,propertyVector);
      ProteotypicResult p = classify(propertyVector,config_._experimentalDesign);
      p._peptide = peptide;
      p._protein = peptideName;
      outputTXTResult(p,config_._experimentalDesign, config_._pValue);
    }

    releaseOutputStream();
    releasePropertyStream();
  }
  catch(const std::exception& err) 
    {
      cerr<<"ERROR : "<<err.what()<<endl;
    }
  catch(...) 
    {
      cerr<<"Unhandled exception. Gotta Jet G."<<endl;
    }
}


void ProteotypicClassifier::readPropertyMap(map<char, vector<double> > & propertyMap, string _propertyFile){
  ifstream propertyMapFile(_propertyFile.c_str());
  if(!propertyMapFile){
    throw std::runtime_error("Unable to read propertyFile: " + _propertyFile);
  }
  char AA;
  vector<double> properties(494,0);
  for(size_t i=0;i<20;i++){
    propertyMapFile >> AA;
    for(size_t j=0;j<494;j++){
      propertyMapFile>>properties[j];
    }
    propertyMap[AA] = properties;
  }  
}

int ProteotypicClassifier::normalize_ISB_ICAT(double* inputs){

 int features[NINPUTS_ISB_ICAT] = {
	     8,    160,    196,    510,   1006,  };
  int n;
  double new_inputs[NRAW_ISB_ICAT];
/* normalization parameters for selected features */

 double means[NINPUTS_ISB_ICAT] = {
	  5.147180e-01,   4.696190e+01,   4.063580e+00,   3.302520e+02, 
   6.362510e+02, 
 };

 double sdevs[NINPUTS_ISB_ICAT] = {
	  7.265700e-01,   1.399490e+01,   1.350640e+00,   1.105130e+02, 
   3.010530e+02, 
 };

  for(n = 0; n < NINPUTS_ISB_ICAT; n++){
    new_inputs[n] = (inputs[features[n]]- means[n]) / sdevs[n];
  }

  for(n = 0; n < NINPUTS_ISB_ICAT; n++){
    inputs[n] = new_inputs[n];
  }
  return((int) NINPUTS_ISB_ICAT);
}

int ProteotypicClassifier::normalize_CZ_MALDI(double* inputs){

 int features[NINPUTS_CZ_MALDI] = {
	     8,    196,    197,    787,    877,  };
  int n;
  double new_inputs[NRAW_CZ_MALDI];
/* normalization parameters for selected features */

 double means[NINPUTS_CZ_MALDI] = {
	  3.367830e-01,   3.781260e+00,   2.306190e-01,   8.349500e-01, 
   8.018340e-01, 
 };

 double sdevs[NINPUTS_CZ_MALDI] = {
	  6.033800e-01,   1.289440e+00,   1.012920e-01,   3.477380e-02, 
   5.382990e-03, 
 };

  for(n = 0; n < NINPUTS_CZ_MALDI; n++){
    new_inputs[n] = (inputs[features[n]]- means[n]) / sdevs[n];
  }

  for(n = 0; n < NINPUTS_CZ_MALDI; n++){
    inputs[n] = new_inputs[n];
  }
  return((int) NINPUTS_CZ_MALDI);
}


int ProteotypicClassifier::normalize_CZ_ESI(double* inputs){

 int features[NINPUTS_CZ_ESI] = {
	     8,    180,    196,    197,    877,  };
  int n;
  double new_inputs[NRAW_CZ_ESI];
/* normalization parameters for selected features */

 double means[NINPUTS_CZ_ESI] = {
	  2.888510e-01,   5.961470e+01,   3.785800e+00,   2.399070e-01, 
   8.012930e-01, 
 };

 double sdevs[NINPUTS_CZ_ESI] = {
	  5.464260e-01,   1.883890e+01,   1.289050e+00,   9.622960e-02, 
   5.286110e-03, 
 };

  for(n = 0; n < NINPUTS_CZ_ESI; n++){
    new_inputs[n] = (inputs[features[n]]- means[n]) / sdevs[n];
  }

  for(n = 0; n < NINPUTS_CZ_ESI; n++){
    inputs[n] = new_inputs[n];
  }
  return((int) NINPUTS_CZ_ESI);
}


int ProteotypicClassifier::normalize_ISB_ESI(double* inputs)
{

 int features[NINPUTS_ISB_ESI] = {
	   161,    197,    313,    823,    788,  };
  int n;
  double new_inputs[NRAW_ISB_ESI];
/* normalization parameters for selected features */

 double means[NINPUTS_ISB_ESI] = {
	  2.318510e+00,   2.137330e-01,   7.025440e-02,   6.352420e+00, 
   1.106240e+01, 
 };

 double sdevs[NINPUTS_ISB_ESI] = {
	  6.613420e-01,   9.582820e-02,   1.338730e-01,   5.060050e-01, 
   4.549470e+00, 
 };

  for(n = 0; n < NINPUTS_ISB_ESI; n++){
    new_inputs[n] = (inputs[features[n]]- means[n]) / sdevs[n];
  }

  for(n = 0; n < NINPUTS_ISB_ESI; n++){
    inputs[n] = new_inputs[n];
  }
  return((int) NINPUTS_ISB_ESI);
}



int ProteotypicClassifier::classify_CZ_ESI (double* inputs, double* outputs){

  int n, best, j, k;
  double h1[NH1_CZ_ESI];
  double y[NCLASSES_CZ_ESI];

 double x_h1[NH1_CZ_ESI+1][NINPUTS_CZ_ESI+1] = {
	{  9.664130e-01,  -2.073000e+00,   1.936540e+00,   2.806100e+00, 
   1.969340e-01,   2.363010e+00, },
	{  1.744380e-01,   5.033600e-01,   7.692110e-01,   5.694600e-01, 
   2.917940e-01,  -8.948790e-01, },
	{ -4.449080e-01,  -2.001100e+00,   2.898790e+00,   1.520060e+00, 
  -2.311430e+00,  -3.255820e-01, },
	{  2.320070e+00,  -4.740530e-01,  -8.516830e-01,  -3.447770e-01, 
  -1.663640e+00,  -2.203700e+00, },
	{  8.574430e-03,  -2.646000e-01,  -7.458790e-02,  -4.031550e-01, 
  -6.994400e-01,  -1.778430e+00, },
	{  1.348380e-01,   5.891360e-01,   8.628270e-01,   5.172830e-01, 
   2.339380e-01,  -8.564700e-01, },
	{  1.016750e-01,   8.536780e-01,   1.024100e+00,   6.185990e-01, 
  -8.397410e-02,  -8.472230e-01, },
	{ -1.492170e+00,   2.961060e+00,   9.763140e-01,  -3.390380e-01, 
  -3.365920e-01,  -1.662130e+00, },
	{ -6.763610e-01,   1.454000e+00,  -1.394610e+00,  -1.137930e+00, 
  -7.391850e-01,  -1.269200e+00, },
	{  6.572010e-02,   3.122700e-01,   5.887400e-01,   2.427470e-01, 
   8.571180e-02,  -1.168900e+00, },
	{ -2.958610e-01,   1.977210e+00,   1.538140e+00,   2.227390e-01, 
  -8.530880e-01,  -1.391860e+00, },
	{ -8.815170e-01,   2.365060e+00,   1.069570e+00,  -1.194390e-01, 
  -6.310630e-01,  -1.471860e+00, },
	{ -1.620890e-01,   1.763690e-01,   4.248960e-01,   1.243970e-01, 
  -1.907440e-01,  -1.249020e+00, },
	{  5.768090e-01,   2.020740e-01,   1.460000e+00,   8.403810e-01, 
  -3.250750e-01,  -1.045360e+00, },
	{ -5.786940e-02,   1.298970e-01,   3.231750e-01,   7.460750e-02, 
  -2.640520e-01,  -1.314740e+00, },
	{ -1.254050e+00,   2.126150e+00,   2.413220e+00,   1.769070e-01, 
  -1.529100e+00,  -1.610610e+00, },
	{ -1.441300e+00,   4.319860e+00,   2.004010e+00,  -2.238180e+00, 
  -6.685490e-01,   2.461320e+00, },
	{  4.446830e-01,   3.247470e-01,   5.035780e-01,   5.936180e-01, 
   6.678110e-01,  -1.047960e+00, },
	{  7.478660e-01,  -5.909880e-01,   7.575070e-01,   7.819930e-01, 
   7.605510e-01,  -1.379950e+00, },
	{  2.730730e-01,   2.572910e-01,   9.233000e-01,   6.444330e-01, 
   2.747750e-01,  -9.227570e-01, },
	{ -1.496890e+00,  -2.933890e+00,   1.800200e+00,   1.945870e+00, 
   5.991350e-01,  -1.739720e+00, },
	{  9.505020e-01,   9.165850e-03,   1.710880e+00,   9.994950e-01, 
  -4.132760e-01,  -1.327670e+00, },
	{  2.162920e+00,  -6.344830e-01,  -9.649460e-01,   3.023230e-01, 
  -1.581770e+00,  -1.445210e+00, },
	{  1.704450e-01,  -3.564510e-01,  -4.798970e-01,  -2.177610e-01, 
  -1.304330e+00,  -1.947230e+00, },
	{  3.647720e-01,   1.400550e-01,   7.302540e-01,   5.855990e-01, 
   5.432110e-01,  -9.999970e-01, },
	{  2.789170e-02,   5.194700e-02,  -8.130390e-02,  -7.301950e-02, 
   4.042020e-03,  -8.435360e-02, },
 };

 double h1_y[NCLASSES_CZ_ESI+1][NH1_CZ_ESI+1] = {
	{ -1.231460e+00,  -1.219260e-01,  -1.277330e+00,   9.001370e-01, 
   4.564730e-01,  -3.854360e-03,  -1.810300e-01,  -1.481480e+00, 
   9.530580e-01,  -2.666800e-04,  -8.807170e-01,  -1.081200e+00, 
   1.823110e-01,  -4.635100e-01,   2.410850e-01,  -1.467440e+00, 
  -2.487810e+00,  -2.820110e-01,  -6.884450e-01,  -1.780190e-01, 
  -1.410050e+00,  -7.604860e-01,   8.874690e-01,   6.898720e-01, 
  -3.361540e-01,   1.726620e+00, },
	{  1.228990e+00,   4.250980e-02,   1.288130e+00,  -9.550510e-01, 
  -4.742840e-01,   1.050930e-01,   5.838400e-02,   1.492850e+00, 
  -9.506170e-01,  -1.011820e-02,   9.987710e-01,   1.024790e+00, 
  -2.182940e-01,   4.112790e-01,  -1.395510e-01,   1.450970e+00, 
   2.496030e+00,   4.056200e-01,   7.547990e-01,   1.420890e-01, 
   1.400880e+00,   8.085120e-01,  -8.454600e-01,  -6.893030e-01, 
   2.168470e-01,  -1.733620e+00, },
	{  7.778970e-02,  -8.460110e-02,   2.994140e-02,  -5.039120e-02, 
   2.589590e-02,  -5.417260e-02,   4.012400e-02,  -3.662660e-02, 
  -3.424460e-02,  -5.371440e-02,  -8.516780e-02,   2.661440e-02, 
  -5.526870e-02,   3.022640e-02,   2.137200e-03,   9.429310e-02, 
  -4.399160e-02,   9.221370e-03,   4.385370e-02,  -7.734390e-02, 
  -5.703310e-03,   1.850800e-02,   8.886360e-02,  -9.816490e-03, 
  -3.272980e-02,   6.953690e-02, },
 };
  double x[NRAW_CZ_ESI];
  int i;

  /* load inputs */
  for(i = 0; i < NRAW_CZ_ESI; i++)
    x[i] = inputs[i];

  /* normalize loaded data */
  normalize_CZ_ESI(x);

  for(j = 0; j < NH1_CZ_ESI; j++){
    h1[j] = x_h1[j][NINPUTS_CZ_ESI];
    for(k = 0; k < NINPUTS_CZ_ESI; k++){
      h1[j] += x[k] * x_h1[j][k];
    }
  }
  /* take sigmoids */
  for(j = 0; j < NH1_CZ_ESI; j++){
    h1[j] = 1. / (1. + exp(- 1.000000 * h1[j]));
  }

  /* calculate outputs */
  for(j = 0; j < NCLASSES_CZ_ESI; j++){
    y[j] = h1_y[j][NH1_CZ_ESI];
    for(k = 0; k < NH1_CZ_ESI; k++){
      y[j] += h1[k] * h1_y[j][k];
    }
  }
  /* take output sigmoid */
  for(j = 0; j < NCLASSES_CZ_ESI; j++){
    y[j] = 1. / (1. + exp(- 1.000000 * y[j]));
  }

  /* copy outputs */
  for(n = 0; n < NCLASSES_CZ_ESI; n++)
    outputs[n] = y[n];

  /* find highest output */
  for(best = n = 0; n < NCLASSES_CZ_ESI; n++)
    if(outputs[best] < outputs[n]) best = n;
  return(best);
}



int ProteotypicClassifier::classify_CZ_MALDI (double* inputs, double* outputs){
  int n, best, j, k;
  double h1[NH1_CZ_MALDI];
  double y[NCLASSES_CZ_MALDI];

 double x_h1[NH1_CZ_MALDI+1][NINPUTS_CZ_MALDI+1] = {
	{  6.371930e-01,  -3.301120e-01,   1.359880e-01,   4.101770e+00, 
   1.205360e+00,  -4.222130e+00, },
	{ -2.824050e-01,   9.058520e-01,   1.679870e-01,   1.676960e-01, 
  -3.657280e-02,  -2.258180e+00, },
	{ -1.277170e+00,  -2.533020e+00,   2.967780e+00,   7.940980e-01, 
   8.309670e-01,  -4.861580e+00, },
	{  2.597860e+00,  -5.716960e+00,   3.420400e-02,   1.124580e-01, 
  -1.652020e-01,  -5.651240e+00, },
	{  6.997390e-01,  -2.211390e+00,   1.456670e-01,  -7.303900e-01, 
   1.421260e-01,  -3.375460e+00, },
	{ -2.541260e-01,   6.215040e-01,   2.960510e-01,   1.881510e-01, 
   7.950470e-02,  -2.264760e+00, },
	{ -1.395950e+00,   4.682560e+00,  -4.085990e+00,  -1.134280e+00, 
  -2.130930e+00,  -4.011190e+00, },
	{ -1.910410e-01,   4.916500e+00,  -9.752550e+00,  -5.447370e-01, 
   5.747190e-01,  -1.006310e+01, },
	{  6.233050e-01,  -1.204160e+00,   1.215680e-01,  -1.555130e-01, 
   5.324550e-02,  -2.855370e+00, },
	{  5.673470e-01,  -1.032100e+00,   1.210770e-01,  -1.025440e-01, 
   7.142590e-02,  -2.788390e+00, },
	{ -3.584890e-01,   1.775350e+00,   1.378680e-01,  -5.464990e-02, 
  -5.797630e-01,  -2.482360e+00, },
	{ -3.487840e+00,   8.917690e+00,  -3.290440e+00,  -1.472380e+00, 
  -8.010480e-01,  -3.181740e+00, },
	{  7.550700e-01,  -2.312810e+00,   1.479000e-01,  -7.261880e-01, 
   1.427100e-01,  -3.402950e+00, },
	{ -2.314110e-01,   5.071900e-01,   3.583920e-01,   2.002640e-01, 
   9.428580e-02,  -2.270570e+00, },
	{  2.844360e+00,  -3.169390e+00,   7.325520e-02,  -2.611310e-01, 
   4.473470e-02,  -3.487770e+00, },
	{ -9.955290e-01,   5.413520e-01,   2.512840e+00,   2.636820e+00, 
  -1.791470e+00,  -2.500500e+00, },
	{ -2.919150e+00,   3.823110e+00,   4.858810e+00,   1.329770e+00, 
   2.280650e+00,  -2.000660e+00, },
	{ -1.790790e-01,   4.644990e-01,   2.529570e-01,   2.199800e-01, 
   1.353170e-01,  -2.284430e+00, },
	{  2.185120e-01,  -3.093480e-01,   1.936680e-01,   1.139300e-01, 
   1.407960e-01,  -2.483460e+00, },
	{  3.060380e-02,   5.707250e-04,   2.640450e-01,   1.904210e-01, 
   1.457730e-01,  -2.372520e+00, },
	{ -6.262740e-01,  -2.382830e-02,   1.022550e+00,   6.392960e-01, 
   5.832440e-01,  -2.562310e+00, },
	{ -2.276860e-01,   4.881710e-01,   3.651850e-01,   2.114710e-01, 
   7.941060e-02,  -2.281090e+00, },
	{  2.317510e+00,  -5.097800e+00,   4.476760e-02,   3.141280e-02, 
  -1.175780e-01,  -5.229320e+00, },
	{  7.086520e-01,  -1.755720e+00,   1.135250e-01,  -4.380650e-01, 
   4.358840e-02,  -3.110440e+00, },
	{  7.701700e-01,  -1.611900e+00,   1.233490e-01,  -2.586310e-01, 
   4.035940e-02,  -2.999660e+00, },
	{  2.789170e-02,   5.194700e-02,  -8.130390e-02,  -7.301950e-02, 
   4.042020e-03,  -8.435360e-02, },
 };

 double h1_y[NCLASSES_CZ_MALDI+1][NH1_CZ_MALDI+1] = {
	{ -1.437230e+00,  -2.012620e-01,  -2.037510e+00,   2.817340e+00, 
   5.381560e-01,  -2.022870e-02,  -1.502100e+00,  -4.304460e+00, 
   2.411680e-01,   1.539990e-01,  -4.805910e-01,  -2.306550e+00, 
   5.339540e-01,  -6.839400e-02,   6.042500e-01,  -9.731020e-01, 
  -1.307540e+00,   1.619840e-02,   7.295330e-02,  -2.784320e-02, 
  -2.775910e-01,  -8.201450e-03,   2.280260e+00,   3.768600e-01, 
   2.115660e-01,   1.093020e+00, },
	{  1.437680e+00,   7.000540e-02,   2.040340e+00,  -2.873960e+00, 
  -5.281780e-01,   6.481340e-02,   1.499500e+00,   4.304180e+00, 
  -1.633200e-01,  -1.789570e-01,   5.180990e-01,   2.306980e+00, 
  -5.483220e-01,  -3.063030e-02,  -6.076190e-01,   9.733310e-01, 
   1.311220e+00,   7.619080e-02,  -6.345920e-03,  -4.162090e-02, 
   2.717110e-01,   2.780250e-02,  -2.198660e+00,  -3.394490e-01, 
  -3.456050e-01,  -1.086710e+00, },
	{  7.778970e-02,  -8.460110e-02,   2.994140e-02,  -5.039120e-02, 
   2.589590e-02,  -5.417260e-02,   4.012400e-02,  -3.662660e-02, 
  -3.424460e-02,  -5.371440e-02,  -8.516780e-02,   2.661440e-02, 
  -5.526870e-02,   3.022640e-02,   2.137200e-03,   9.429310e-02, 
  -4.399160e-02,   9.221370e-03,   4.385370e-02,  -7.734390e-02, 
  -5.703310e-03,   1.850800e-02,   8.886360e-02,  -9.816490e-03, 
  -3.272980e-02,   6.953690e-02, },
 };
  double x[NRAW_CZ_MALDI];
  int i;

  /* load inputs */
  for(i = 0; i < NRAW_CZ_MALDI; i++)
    x[i] = inputs[i];

  /* normalize loaded data */
  normalize_CZ_MALDI(x);

  for(j = 0; j < NH1_CZ_MALDI; j++){
    h1[j] = x_h1[j][NINPUTS_CZ_MALDI];
    for(k = 0; k < NINPUTS_CZ_MALDI; k++){
      h1[j] += x[k] * x_h1[j][k];
    }
  }
  /* take sigmoids */
  for(j = 0; j < NH1_CZ_MALDI; j++){
    h1[j] = 1. / (1. + exp(- 1.000000 * h1[j]));
  }

  /* calculate outputs */
  for(j = 0; j < NCLASSES_CZ_MALDI; j++){
    y[j] = h1_y[j][NH1_CZ_MALDI];
    for(k = 0; k < NH1_CZ_MALDI; k++){
      y[j] += h1[k] * h1_y[j][k];
    }
  }
  /* take output sigmoid */
  for(j = 0; j < NCLASSES_CZ_MALDI; j++){
    y[j] = 1. / (1. + exp(- 1.000000 * y[j]));
  }

  /* copy outputs */
  for(n = 0; n < NCLASSES_CZ_MALDI; n++)
    outputs[n] = y[n];

  /* find highest output */
  for(best = n = 0; n < NCLASSES_CZ_MALDI; n++)
    if(outputs[best] < outputs[n]) best = n;
  return(best);
}



int ProteotypicClassifier::classify_ISB_ICAT (double* inputs, double* outputs){
  int n, best, j, k;
  double h1[NH1_ISB_ICAT];
  double y[NCLASSES_ISB_ICAT];

 double x_h1[NH1_ISB_ICAT+1][NINPUTS_ISB_ICAT+1] = {
	{ -1.239370e+00,   1.305730e+00,   2.664790e+00,  -3.292610e-02, 
   4.146290e+00,   4.247930e+00, },
	{ -4.186580e-01,   6.154330e-01,   1.228520e+00,   2.404400e-01, 
   4.802690e-01,  -1.591000e+00, },
	{  1.284060e-02,  -8.614720e-02,   2.356820e+00,  -4.831900e-01, 
   2.070360e+00,  -1.804920e+00, },
	{  2.614550e+00,  -1.168230e+00,  -5.483420e+00,   2.691280e+00, 
   1.993400e+00,  -1.506720e+00, },
	{  1.893410e+00,  -4.332510e-01,  -1.487770e+00,   3.986350e-01, 
  -2.977910e-01,  -3.875200e+00, },
	{ -3.817560e-01,   4.585480e-01,   1.562120e+00,  -3.739430e-02, 
   3.588350e-01,  -1.512000e+00, },
	{ -5.936900e-01,   4.117960e-01,   1.053880e+00,   9.259240e-02, 
   1.543720e+00,  -1.916320e+00, },
	{ -5.048670e-01,  -2.890680e-02,   1.520810e+00,  -1.132490e-01, 
   1.871300e+00,  -1.508460e+00, },
	{  1.237080e+00,   9.185090e-02,  -7.266720e-01,   1.726490e-01, 
  -2.053640e-01,  -2.699570e+00, },
	{  7.459570e-01,   2.542470e-01,  -1.995350e-01,   4.503270e-02, 
  -9.689970e-02,  -2.227780e+00, },
	{ -5.583360e-01,  -5.865340e-01,   2.985800e+00,   4.156410e-01, 
  -1.838690e+00,  -2.800150e+00, },
	{ -3.171680e-01,   6.792140e-02,   8.105510e-01,  -1.723590e+00, 
   2.920260e+00,  -2.461040e+00, },
	{  8.485690e-01,   2.154430e-01,  -3.111520e-01,   1.017230e-01, 
  -1.477630e-01,  -2.301190e+00, },
	{ -5.911410e-01,  -6.297350e-01,   2.371900e+00,  -2.527250e+00, 
  -1.576880e+00,  -5.944010e+00, },
	{  3.613720e+00,   3.313250e-01,  -1.304980e+00,   3.571790e-01, 
   5.593900e-02,  -2.688970e+00, },
	{ -7.420720e-01,  -5.893450e-02,   1.629890e+00,   1.372500e+00, 
   1.725790e+00,  -6.033780e-01, },
	{  1.261030e+00,   1.150070e-01,   3.970180e+00,  -3.562600e-01, 
   2.100550e+00,  -2.289820e+00, },
	{ -2.989230e-01,   7.171870e-01,   7.759060e-01,   3.327880e-01, 
   2.451380e-01,  -1.706620e+00, },
	{ -3.036220e-01,  -2.020380e-01,   1.932240e+00,  -5.825660e-01, 
  -6.196420e-01,  -2.234590e+00, },
	{ -3.930380e-01,   3.802700e-01,   1.587060e+00,   1.374100e-01, 
   2.770770e-02,  -1.550020e+00, },
	{ -7.482550e-01,  -1.102050e+00,   4.630160e-01,  -2.740330e+00, 
   4.092460e+00,  -3.712180e+00, },
	{ -7.451960e-01,  -5.457140e-01,   2.355810e+00,  -1.881110e+00, 
  -2.121270e+00,  -5.717570e+00, },
	{  1.859200e+00,   4.448540e-01,  -5.155050e+00,   2.320590e+00, 
  -4.141370e-01,  -3.778760e+00, },
	{  2.128800e+00,  -3.569310e-01,  -1.846360e+00,   3.892600e-01, 
  -3.453440e-01,  -4.144710e+00, },
	{ -3.709470e-01,   5.879610e-01,   9.118910e-01,   4.186850e-01, 
   2.830210e-01,  -1.673310e+00, },
	{  2.789170e-02,   5.194700e-02,  -8.130390e-02,  -7.301950e-02, 
   4.042020e-03,  -8.435360e-02, },
 };

 double h1_y[NCLASSES_ISB_ICAT+1][NH1_ISB_ICAT+1] = {
	{ -1.645950e+00,  -3.240410e-01,  -6.691180e-01,   9.278530e-01, 
   1.078950e+00,  -1.378390e-01,  -6.122150e-01,  -4.368190e-01, 
   5.671770e-01,   2.696460e-01,  -9.792040e-01,  -1.084090e+00, 
   3.070820e-01,  -1.465580e+00,   1.318140e+00,  -8.096370e-01, 
  -1.636240e+00,  -2.122120e-01,  -3.619200e-01,  -2.345400e-01, 
  -1.562400e+00,  -1.406270e+00,   8.001790e-01,   1.232230e+00, 
  -3.664370e-01,   3.438470e-01, },
	{  1.645810e+00,   2.688730e-01,   7.036790e-01,  -9.272530e-01, 
  -1.095990e+00,   2.091640e-01,   5.454200e-01,   4.782700e-01, 
  -5.013650e-01,  -3.024420e-01,   9.697510e-01,   1.056260e+00, 
  -3.553900e-01,   1.432540e+00,  -1.318590e+00,   8.179340e-01, 
   1.621310e+00,   3.740010e-01,   3.959290e-01,   1.684930e-01, 
   1.571170e+00,   1.432560e+00,  -7.974320e-01,  -1.232850e+00, 
   2.655270e-01,  -3.442430e-01, },
	{  7.778970e-02,  -8.460110e-02,   2.994140e-02,  -5.039120e-02, 
   2.589590e-02,  -5.417260e-02,   4.012400e-02,  -3.662660e-02, 
  -3.424460e-02,  -5.371440e-02,  -8.516780e-02,   2.661440e-02, 
  -5.526870e-02,   3.022640e-02,   2.137200e-03,   9.429310e-02, 
  -4.399160e-02,   9.221370e-03,   4.385370e-02,  -7.734390e-02, 
  -5.703310e-03,   1.850800e-02,   8.886360e-02,  -9.816490e-03, 
  -3.272980e-02,   6.953690e-02, },
 };
  double x[NRAW_ISB_ICAT];
  int i;

  /* load inputs */
  for(i = 0; i < NRAW_ISB_ICAT; i++)
    x[i] = inputs[i];

  /* normalize loaded data */
  normalize_ISB_ICAT(x);

  for(j = 0; j < NH1_ISB_ICAT; j++){
    h1[j] = x_h1[j][NINPUTS_ISB_ICAT];
    for(k = 0; k < NINPUTS_ISB_ICAT; k++){
      h1[j] += x[k] * x_h1[j][k];
    }
  }
  /* take sigmoids */
  for(j = 0; j < NH1_ISB_ICAT; j++){
    h1[j] = 1. / (1. + exp(- 1.000000 * h1[j]));
  }

  /* calculate outputs */
  for(j = 0; j < NCLASSES_ISB_ICAT; j++){
    y[j] = h1_y[j][NH1_ISB_ICAT];
    for(k = 0; k < NH1_ISB_ICAT; k++){
      y[j] += h1[k] * h1_y[j][k];
    }
  }
  /* take output sigmoid */
  for(j = 0; j < NCLASSES_ISB_ICAT; j++){
    y[j] = 1. / (1. + exp(- 1.000000 * y[j]));
  }

  /* copy outputs */
  for(n = 0; n < NCLASSES_ISB_ICAT; n++)
    outputs[n] = y[n];

  /* find highest output */
  for(best = n = 0; n < NCLASSES_ISB_ICAT; n++)
    if(outputs[best] < outputs[n]) best = n;
  return(best);
}




int ProteotypicClassifier::classify_ISB_ESI (double* inputs, double* outputs){
  int n, best, j, k;
  double h1[NH1_ISB_ESI];
  double y[NCLASSES_ISB_ESI];

 double x_h1[NH1_ISB_ESI+1][NINPUTS_ISB_ESI+1] = {
	{  1.422760e+00,   2.465570e-01,   2.474380e+00,  -4.941440e-01, 
   3.650960e+00,  -1.726920e-01, },
	{  5.287450e-01,   5.857940e-01,   7.450250e-01,  -7.658540e-01, 
  -4.155860e-01,  -1.274660e+00, },
	{  3.226640e-01,  -1.243870e-01,   3.903830e+00,   3.393960e-01, 
  -7.682980e-01,  -1.327430e+00, },
	{ -1.580420e-02,  -2.943260e-01,  -1.802090e+00,   2.366500e+00, 
  -5.600290e+00,  -2.303300e+00, },
	{ -5.409660e-01,  -6.661470e-01,  -7.667080e-01,   1.040690e+00, 
   2.453920e-01,  -1.958760e+00, },
	{  6.035760e-01,   8.067470e-01,   9.276670e-01,  -1.284820e+00, 
  -8.521060e-01,  -1.509530e+00, },
	{  8.052130e-01,   9.326160e-01,   1.129410e+00,  -1.673070e+00, 
  -6.980190e-01,  -1.296970e+00, },
	{  2.552000e+00,   1.344100e+00,   6.378970e-01,  -2.239990e+00, 
   1.873720e+00,  -2.014670e+00, },
	{ -1.201390e+00,  -3.263020e+00,  -1.009100e+00,   1.062780e+00, 
  -4.505950e-01,  -4.156420e+00, },
	{  7.832110e-01,   8.411100e-01,   7.234330e-01,  -1.353440e+00, 
  -8.546820e-01,  -1.623290e+00, },
	{  4.564140e-02,   6.391400e-01,   1.289130e+00,  -7.815960e-01, 
  -1.001750e+00,  -1.471780e+00, },
	{  1.960980e-01,   4.215080e-01,   1.715670e+00,  -9.993270e-01, 
  -2.652760e-01,  -1.131830e+00, },
	{ -3.424600e-01,  -4.317700e-02,  -6.337260e-01,   1.223840e+00, 
   3.570750e-01,  -1.550390e+00, },
	{  7.975140e-01,   1.002490e+00,   1.242000e+00,  -1.900990e+00, 
  -8.646660e-01,  -1.431510e+00, },
	{  5.902900e-01,   9.018560e-01,   4.461050e-01,   5.619160e-01, 
   5.110880e-01,  -1.107170e+00, },
	{  1.650250e+00,  -1.527060e-01,   2.379470e+00,  -8.858820e-01, 
   4.253060e+00,   6.867620e-02, },
	{  1.323470e+00,  -3.534890e-01,   3.748450e+00,  -2.760500e-02, 
   2.690940e+00,  -7.067990e-01, },
	{  9.448470e-01,   9.398550e-01,   9.448600e-01,  -1.660320e+00, 
  -6.280030e-01,  -1.383090e+00, },
	{  4.444390e-01,   6.960650e-01,   1.062030e+00,  -1.115140e+00, 
  -8.044330e-01,  -1.451000e+00, },
	{  4.785620e-01,   7.267410e-01,   5.068120e-01,   4.151090e-01, 
   4.236570e-01,  -1.136330e+00, },
	{  5.351810e-01,  -2.148080e-01,   2.657140e+00,  -2.851510e-01, 
   1.317870e+00,  -1.000620e+00, },
	{  3.573060e-01,   8.838790e-01,   1.374890e+00,  -1.453870e+00, 
  -1.033170e+00,  -1.617760e+00, },
	{ -6.505220e-01,  -3.306890e+00,  -8.379360e-01,   1.239460e+00, 
  -3.999940e-01,  -3.689840e+00, },
	{ -1.174760e+00,  -2.470730e+00,  -1.248430e+00,   1.220980e+00, 
  -2.492250e-01,  -3.510490e+00, },
	{  3.412580e-01,   1.952500e+00,   4.855640e-02,   1.461230e+00, 
   1.182740e+00,  -1.065530e+00, },
	{  2.789170e-02,   5.194700e-02,  -8.130390e-02,  -7.301950e-02, 
   4.042020e-03,  -8.435360e-02, },
 };

 double h1_y[NCLASSES_ISB_ESI+1][NH1_ISB_ESI+1] = {
	{ -1.303540e+00,  -4.551370e-01,  -1.510110e+00,   2.112880e+00, 
   4.141910e-01,  -6.303490e-01,  -7.479050e-01,  -1.246280e+00, 
   1.368250e+00,  -6.984290e-01,  -6.818150e-01,  -6.087510e-01, 
   3.620780e-01,  -8.146150e-01,   4.599840e-01,  -1.477370e+00, 
  -1.880260e+00,  -6.019250e-01,  -5.939000e-01,   2.325130e-01, 
  -9.279700e-01,  -8.125020e-01,   1.226690e+00,   1.048790e+00, 
   1.370180e+00,   4.117180e-01, },
	{  1.283050e+00,   3.611060e-01,   1.489330e+00,  -2.117390e+00, 
  -4.366950e-01,   6.971850e-01,   5.974830e-01,   1.253240e+00, 
  -1.358250e+00,   6.742150e-01,   7.571250e-01,   5.371970e-01, 
  -3.783420e-01,   7.431030e-01,  -3.215780e-01,   1.490730e+00, 
   1.890200e+00,   7.276200e-01,   6.694490e-01,  -2.535100e-01, 
   9.426790e-01,   8.406460e-01,  -1.212500e+00,  -1.072600e+00, 
  -1.436120e+00,  -4.000810e-01, },
	{  7.778970e-02,  -8.460110e-02,   2.994140e-02,  -5.039120e-02, 
   2.589590e-02,  -5.417260e-02,   4.012400e-02,  -3.662660e-02, 
  -3.424460e-02,  -5.371440e-02,  -8.516780e-02,   2.661440e-02, 
  -5.526870e-02,   3.022640e-02,   2.137200e-03,   9.429310e-02, 
  -4.399160e-02,   9.221370e-03,   4.385370e-02,  -7.734390e-02, 
  -5.703310e-03,   1.850800e-02,   8.886360e-02,  -9.816490e-03, 
  -3.272980e-02,   6.953690e-02, },
 };
  double x[NRAW_ISB_ESI];
  int i;

  /* load inputs */
  for(i = 0; i < NRAW_ISB_ESI; i++)
    x[i] = inputs[i];

  /* normalize loaded data */
  normalize_ISB_ESI(x);

  for(j = 0; j < NH1_ISB_ESI; j++){
    h1[j] = x_h1[j][NINPUTS_ISB_ESI];
    for(k = 0; k < NINPUTS_ISB_ESI; k++){
      h1[j] += x[k] * x_h1[j][k];
    }
  }
  /* take sigmoids */
  for(j = 0; j < NH1_ISB_ESI; j++){
    h1[j] = 1. / (1. + exp(- 1.000000 * h1[j]));
  }

  /* calculate outputs */
  for(j = 0; j < NCLASSES_ISB_ESI; j++){
    y[j] = h1_y[j][NH1_ISB_ESI];
    for(k = 0; k < NH1_ISB_ESI; k++){
      y[j] += h1[k] * h1_y[j][k];
    }
  }
  /* take output sigmoid */
  for(j = 0; j < NCLASSES_ISB_ESI; j++){
    y[j] = 1. / (1. + exp(- 1.000000 * y[j]));
  }

  /* copy outputs */
  for(n = 0; n < NCLASSES_ISB_ESI; n++)
    outputs[n] = y[n];

  /* find highest output */
  for(best = n = 0; n < NCLASSES_ISB_ESI; n++)
    if(outputs[best] < outputs[n]) best = n;
  return(best);

}
