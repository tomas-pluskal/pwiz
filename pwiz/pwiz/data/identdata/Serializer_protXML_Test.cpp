// TODO this is just a copy of the pepXML work, not yet populated for protXML
//
// $Id$
//
// Original author: Brian Pratt <brian.pratt .@. insilicos.com>
//  after Serializer_pepXML_Test by Matt Chambers <matt.chambers .@. vanderbilt.edu>
//
// Copyright 2012 Spielberg Family Center for Applied Proteomics
//   University of Southern California, Los Angeles, California  90033
//
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
#include "pwiz/utility/misc/Std.hpp"
#include "pwiz/data/identdata/DefaultReaderList.hpp"
#include "pwiz/data/identdata/IdentDataFile.hpp"
#include "pwiz/utility/misc/unit.hpp"
#include "Serializer_protXML.hpp"
#include "Diff.hpp"
#include "References.hpp"
#include "examples.hpp"
#include "pwiz/utility/misc/unit.hpp"
#include "pwiz/utility/misc/Std.hpp"
#include "pwiz/utility/misc/Filesystem.hpp"
#include "pwiz/data/proteome/Digestion.hpp"
#include "TextWriter.hpp"
#include "boost/range/adaptor/transformed.hpp"
#include "boost/range/algorithm/max_element.hpp"
#include "boost/range/algorithm/min_element.hpp"
#include <cstring>


using namespace pwiz::identdata;
using namespace pwiz::identdata::examples;
using namespace pwiz::util;
namespace proteome = pwiz::proteome;

ostream* os_ = 0;

void testSerialize(const string &example_data_dir)
{
    DefaultReaderList readers;
    Reader::Config readerConfig;

    {
    // verify that loading protXML with reachable pepXML source files gives more 
    // (well, different) data than pepXML alone
    IdentData mzid0,mzid1;
    readers.read(example_data_dir+"/example.pep.xml", mzid0, readerConfig);
    readers.read(example_data_dir+"/example.prot.xml", mzid1, readerConfig);
    Diff<IdentData, DiffConfig> diff0(mzid0, mzid1);
    unit_assert(diff0);
    }

    {
    // verify that adding protxml to pepXML is equivalent to loading protXML with reachable
    // pepXML source files 
    IdentData mzid0,mzid1;
    readers.read(example_data_dir+"/example.pep.xml", mzid0, readerConfig);
    readers.read(example_data_dir+"/example.prot.xml", mzid0, readerConfig);
    readers.read(example_data_dir+"/example.prot.xml", mzid1, readerConfig);
    Diff<IdentData, DiffConfig> diff1(mzid0, mzid1);
    if (os_ && diff1) *os_ << diff1 << endl; 
    unit_assert(!diff1);
    }

    {
    // verify that loading protXML is the same as loading a known-good mzIdentML file
    IdentData mzid0,mzid1;
    readers.read(example_data_dir+"/example.prot.xml", mzid0, readerConfig);
    readers.read(example_data_dir+"/example.prot.mzid", mzid1, readerConfig);
    Diff<IdentData, DiffConfig> diff2(mzid0, mzid1);
    if (os_ && diff2) *os_ << diff2 << endl; 
    unit_assert(!diff2);
    }
}


int main(int argc, char** argv)
{
    try
    {
        if (argc>1 && !strcmp(argv[1],"-v")) os_ = &cout;
        std::string buildparent(argv[0]);
        size_t pos = buildparent.find("build");
        if (string::npos==pos)
        {
            buildparent = __FILE__; // nonstandard build, maybe?  try using source file name
            // something like \ProteoWizard\pwiz\pwiz\data\msdata\RAMPAdapterTest.cpp
            pos = buildparent.rfind("pwiz");
        }
        buildparent.resize(pos);
        string example_data_dir = buildparent + "example_data/";
        testSerialize(example_data_dir);
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
