//
// $Id$
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


#define PWIZ_SOURCE

#include "Serializer_mzid.hpp"
#include "IO.hpp"
#include "pwiz/utility/minimxml/XMLWriter.hpp"
#include "pwiz/utility/minimxml/SAXParser.hpp"
#include <stdexcept>
#include <iostream>

namespace pwiz {
namespace mziddata {

using namespace std;
using minimxml::XMLWriter;
using boost::shared_ptr;
using boost::lexical_cast;
using boost::iostreams::stream_offset;
using namespace pwiz::minimxml;


void Serializer_mzIdentML::write(ostream& os, const MzIdentML& mzid) const
{
    // instantiate XMLWriter

    XMLWriter::Config xmlConfig;
    XMLWriter xmlWriter(os, xmlConfig);

    string xmlData = "version=\"1.0\" encoding=\"ISO-8859-1\"";
    xmlWriter.processingInstruction("xml", xmlData);

    IO::write(xmlWriter, mzid);
}


void Serializer_mzIdentML::read(shared_ptr<istream> is, MzIdentML& mzid) const
{
    if (!is.get() || !*is)
        throw runtime_error("[Serializer_mzIdentML::read()] Bad istream.");

    is->seekg(0);

    IO::read(*is, mzid);
}

} // namespace pwiz 
} // namespace mziddata 

