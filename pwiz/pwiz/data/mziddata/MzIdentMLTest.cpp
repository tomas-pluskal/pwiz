//
// MzIdentMLTest.cpp
//
//
// Original author: Matt Chambers <matt.chambers .@. vanderbilt.edu>
//
// Copyright 2009 Vanderbilt University - Nashville, TN 37232
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

#include "MzIdentML.hpp"
#include "Serializer_mzid.hpp"
#include "examples.hpp"

#include <iostream>

using namespace std;
using namespace pwiz::mziddata;
using namespace pwiz::mziddata::examples;


void testCreation()
{
    MzIdentML mzid;
    initializeTiny(mzid);

    Serializer_mzIdentML ser;
    ostringstream oss;
    ser.write(oss, mzid);

    //cout << oss.str() << endl;
    // TODO finish adding something useful
    
}

int main(int argc, char** argv)
{
    testCreation();
    
    return 0;
}
