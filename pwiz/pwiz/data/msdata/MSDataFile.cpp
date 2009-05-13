//
// MSDataFile.cpp
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


#define PWIZ_SOURCE

#include "MSDataFile.hpp"
#include "TextWriter.hpp"
#include "Serializer_mzML.hpp"
#include "Serializer_mzXML.hpp"
#include "Serializer_MGF.hpp"
#include "DefaultReaderList.hpp"
#include "pwiz/utility/misc/Filesystem.hpp"
#include "pwiz/utility/misc/random_access_compressed_ifstream.hpp"
#include "pwiz/utility/misc/SHA1Calculator.hpp"
#include "pwiz/utility/minimxml/XMLWriter.hpp" // for charcounter defn
#include "boost/iostreams/device/file.hpp"
#include "boost/iostreams/filtering_stream.hpp" 
#include "boost/iostreams/filter/gzip.hpp" 


#include <fstream>
#include <stdexcept>


namespace pwiz {
namespace msdata {


using namespace std;
using namespace pwiz::util;
using boost::shared_ptr;


namespace {


void readFile(const string& filename, MSData& msd, const Reader& reader, const string& head)
{
    if (!reader.accept(filename, head))
        throw runtime_error("[MSDataFile::readFile()] Unsupported file format.");

    reader.read(filename, head, msd);
}


shared_ptr<DefaultReaderList> defaultReaderList_;


} // namespace


PWIZ_API_DECL MSDataFile::MSDataFile(const string& filename, const Reader* reader,
                                     bool calculateSourceFileChecksum)
{
    // peek at head of file 
    string head = read_file_header(filename, 512);

    if (reader)
    {
        readFile(filename, *this, *reader, head); 
    }
    else
    {
        if (!defaultReaderList_.get())
            defaultReaderList_ = shared_ptr<DefaultReaderList>(new DefaultReaderList);
        readFile(filename, *this, *defaultReaderList_, head);
    }

    if (calculateSourceFileChecksum && !fileDescription.sourceFilePtrs.empty())
        calculateSourceFileSHA1(*fileDescription.sourceFilePtrs.back());
}


PWIZ_API_DECL
void MSDataFile::write(const string& filename,
                       const WriteConfig& config,
                       const IterationListenerRegistry* iterationListenerRegistry)
{
    write(*this, filename, config, iterationListenerRegistry); 
}


namespace {


shared_ptr<ostream> openFile(const string& filename, bool gzipped)
{
	if (gzipped) 
	{   // use boost's filter stack to count outgoing bytes, and gzip them
		boost::iostreams::filtering_ostream *filt = new boost::iostreams::filtering_ostream();
		shared_ptr<ostream> result(filt);
		if (filt)
		{
		filt->push(pwiz::minimxml::charcounter()); // for counting bytes before compression
		filt->push(boost::iostreams::gzip_compressor(9)); // max compression
		filt->push(boost::iostreams::file_sink(filename.c_str(), ios::binary));
		}
		if (!result.get() || !*result || !filt->good())
			throw runtime_error(("[MSDataFile::openFile()] Unable to open file " + filename).c_str());
	    return result; 
	} else 
	{
		shared_ptr<ostream> result(new ofstream(filename.c_str(), ios::binary));

		if (!result.get() || !*result)
			throw runtime_error(("[MSDataFile::openFile()] Unable to open file " + filename).c_str());

		return result; 		
	}
}


void writeStream(ostream& os, const MSData& msd, const MSDataFile::WriteConfig& config,
                 const IterationListenerRegistry* iterationListenerRegistry)
{
    switch (config.format)
    {
        case MSDataFile::Format_Text:
        {
            TextWriter(os,0)(msd);
            break;
        }
        case MSDataFile::Format_mzML:
        {
            Serializer_mzML::Config serializerConfig;
            serializerConfig.binaryDataEncoderConfig = config.binaryDataEncoderConfig;
            serializerConfig.indexed = config.indexed;
            Serializer_mzML serializer(serializerConfig);
            serializer.write(os, msd, iterationListenerRegistry);
            break;
        }
        case MSDataFile::Format_mzXML:
        {
            Serializer_mzXML::Config serializerConfig;
            serializerConfig.binaryDataEncoderConfig = config.binaryDataEncoderConfig;
            serializerConfig.indexed = config.indexed;
            Serializer_mzXML serializer(serializerConfig);
            serializer.write(os, msd, iterationListenerRegistry);
            break;
        }
        case MSDataFile::Format_MGF:
        {
            Serializer_MGF serializer;
            serializer.write(os, msd, iterationListenerRegistry);
            break;
        }
        default:
        {
            throw runtime_error("[MSDataFile::write()] Format not implemented.");
        }
    }
}


} // namespace


PWIZ_API_DECL
void MSDataFile::write(const MSData& msd,
                       const string& filename,
                       const WriteConfig& config,
                       const IterationListenerRegistry* iterationListenerRegistry)
{
    shared_ptr<ostream> os = openFile(filename,config.gzipped);
    writeStream(*os, msd, config, iterationListenerRegistry);
}


PWIZ_API_DECL void calculateSourceFileSHA1(SourceFile& sourceFile)
{
    if (sourceFile.hasCVParam(MS_SHA_1)) return;

    const string uriPrefix = "file:///";
    if (sourceFile.location.substr(0, uriPrefix.size()) != uriPrefix) return;
    bfs::path p(sourceFile.location.substr(uriPrefix.size()));
    p /= sourceFile.name;

    string sha1 = SHA1Calculator::hashFile(p.string());
    sourceFile.set(MS_SHA_1, sha1); 
}


PWIZ_API_DECL ostream& operator<<(ostream& os, MSDataFile::Format format)
{
    switch (format)
    {
        case MSDataFile::Format_Text:
            os << "Text";
            return os;
        case MSDataFile::Format_mzML:
            os << "mzML";
            return os;
        case MSDataFile::Format_mzXML:
            os << "mzXML";
            return os;
        case MSDataFile::Format_MGF:
            os << "MGF";
            return os;
        default:
            os << "Unknown";
            return os;
    }
}


PWIZ_API_DECL ostream& operator<<(ostream& os, const MSDataFile::WriteConfig& config)
{
    os << config.format;
    if (config.format == MSDataFile::Format_mzML ||
        config.format == MSDataFile::Format_mzXML)
        os << " " << config.binaryDataEncoderConfig
           << " indexed=\"" << boolalpha << config.indexed << "\"";
    return os;
}


} // namespace msdata
} // namespace pwiz


