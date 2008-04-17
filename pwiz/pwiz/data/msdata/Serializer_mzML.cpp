//
// Serializer_mzML.cpp
//
//
// Original author: Darren Kessner <Darren.Kessner@cshs.org>
//
// Copyright 2007 Spielberg Family Center for Applied Proteomics
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


#include "Serializer_mzML.hpp"
#include "IO.hpp"
#include "SpectrumList_mzML.hpp"
#include "SHA1OutputObserver.hpp"
#include "utility/minimxml/XMLWriter.hpp"
#include "utility/minimxml/SAXParser.hpp"
#include <stdexcept>
#include <iostream>


namespace pwiz {
namespace msdata {


using namespace std;
using minimxml::XMLWriter;
using boost::shared_ptr;
using boost::lexical_cast;
using boost::iostreams::stream_offset;
using namespace pwiz::util;
using namespace pwiz::minimxml;


class Serializer_mzML::Impl
{
    public:

    Impl(const Config& config)
    :   config_(config)
    {}

    void write(ostream& os, const MSData& msd) const;
    void read(shared_ptr<istream> is, MSData& msd) const;

    private:
    Config config_; 
};


namespace {

void writeIndex(XMLWriter& xmlWriter, 
                const SpectrumListPtr& spectrumListPtr,
                const vector<stream_offset>& positions)
{
    XMLWriter::Attributes indexAttributes;
    indexAttributes.push_back(make_pair("name", "spectrum"));        
    xmlWriter.startElement("index", indexAttributes);

    if (spectrumListPtr.get())
    {
        if (spectrumListPtr->size() != positions.size())
            throw runtime_error("[Serializer_mzML::writeIndex()] Sizes differ.");

        xmlWriter.pushStyle(XMLWriter::StyleFlag_InlineInner);
        for (unsigned int i=0; i<positions.size(); ++i)
        {
            SpectrumPtr spectrum = spectrumListPtr->spectrum(i, false);
            if (!spectrum.get())
                throw runtime_error("[Serializer_mzML::writeIndex()] Error retrieving spectrum index " 
                                    + lexical_cast<string>(i));

            XMLWriter::Attributes attributes;
            attributes.push_back(make_pair("id", spectrum->id));        
            attributes.push_back(make_pair("nativeID", spectrum->nativeID));        

            xmlWriter.startElement("offset", attributes);
            xmlWriter.characters(lexical_cast<string>(positions[i]));
            xmlWriter.endElement();
        }
        xmlWriter.popStyle();
    }

    xmlWriter.endElement(); 
}

} // namespace


void Serializer_mzML::Impl::write(ostream& os, const MSData& msd) const
{
    // instantiate XMLWriter

    SHA1OutputObserver sha1OutputObserver;
    XMLWriter::Config xmlConfig;
    xmlConfig.outputObserver = &sha1OutputObserver;
    XMLWriter xmlWriter(os, xmlConfig);

    string xmlData = "version=\"1.0\" encoding=\"ISO-8859-1\"";
    xmlWriter.processingInstruction("xml", xmlData);

    // <indexedmzML> start

    if (config_.indexed)
    {
        XMLWriter::Attributes attributes; 
        attributes.push_back(make_pair("xmlns", 
            "http://psi.hupo.org/schema_revision/mzML_0.99.1"));
        attributes.push_back(make_pair("xmlns:xsi", 
            "http://www.w3.org/2001/XMLSchema-instance"));
        attributes.push_back(make_pair("xsi:schemaLocation", 
            "http://psi.hupo.org/schema_revision/mzML_0.99.1 mzML0.99.1_idx.xsd"));
        
        xmlWriter.startElement("indexedmzML", attributes);
        attributes.clear();
    }

    // <mzML>

    vector<stream_offset> positions;
    BinaryDataEncoder::Config bdeConfig = config_.binaryDataEncoderConfig;
    bdeConfig.byteOrder = BinaryDataEncoder::ByteOrder_LittleEndian; // mzML always little endian
    IO::write(xmlWriter, msd, bdeConfig, &positions);

    // <indexedmzML> end

    if (config_.indexed)
    {
        stream_offset indexOffset = xmlWriter.position();
        writeIndex(xmlWriter, msd.run.spectrumListPtr, positions);

        xmlWriter.pushStyle(XMLWriter::StyleFlag_InlineInner);

        xmlWriter.startElement("indexOffset");
        xmlWriter.characters(lexical_cast<string>(indexOffset));
        xmlWriter.endElement(); 
        
        xmlWriter.startElement("fileContentType");
        string fileContentType = "Unknown";
        if (msd.run.spectrumListPtr.get() && !msd.run.spectrumListPtr->empty()) 
            fileContentType = msd.run.spectrumListPtr->spectrum(0, false)->
                cvParamChild(MS_spectrum_type).name();
        xmlWriter.characters(fileContentType);
        xmlWriter.endElement(); 

        xmlWriter.startElement("fileChecksum");
        xmlWriter.characters(sha1OutputObserver.hash());
        xmlWriter.endElement(); 

        xmlWriter.popStyle();

        xmlWriter.endElement(); // indexedmzML
    }
}


struct HandlerIndexedMZML : public SAXParser::Handler
{
    virtual Status startElement(const string& name, 
                                const Attributes& attributes,
                                stream_offset position)
    {
        if (name == "indexedmzML")
            return Status::Done;

        throw runtime_error(("[SpectrumList_mzML::HandlerIndexedMZML] Unexpected element name: " + name).c_str());
    }
};


void Serializer_mzML::Impl::read(shared_ptr<istream> is, MSData& msd) const
{
    if (!is.get() || !*is)
        throw runtime_error("[Serializer_mzML::read()] Bad istream.");

    is->seekg(0);

    if (config_.indexed)
    {
        HandlerIndexedMZML handler;
        SAXParser::parse(*is, handler); 
    }

    IO::read(*is, msd, IO::IgnoreSpectrumList);
    msd.run.spectrumListPtr = SpectrumList_mzML::create(is, msd, config_.indexed);
}


//
// Serializer_mzML
//


Serializer_mzML::Serializer_mzML(const Config& config)
:   impl_(new Impl(config))
{}


void Serializer_mzML::write(ostream& os, const MSData& msd) const
{
    return impl_->write(os, msd);
}


void Serializer_mzML::read(shared_ptr<istream> is, MSData& msd) const
{
    return impl_->read(is, msd);
}


ostream& operator<<(ostream& os, const Serializer_mzML::Config& config)
{
    os << config.binaryDataEncoderConfig 
       << " indexed=\"" << boolalpha << config.indexed << "\"";
    return os;
}


} // namespace msdata
} // namespace pwiz


