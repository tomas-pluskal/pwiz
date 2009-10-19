//
// $Id$
//
//
// Original author: Robert Burke <robert.burke@proteowizard.org>
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

#include "pwiz/utility/misc/Filesystem.hpp"
#include "pwiz/utility/misc/String.hpp"
#include "pwiz/utility/misc/Stream.hpp"
#include "DefaultReaderList.hpp"
#include "Serializer_mzid.hpp"
//#include "References.hpp"
#include "pwiz/data/msdata/Version.hpp"
#include "boost/regex.hpp"
#include "boost/foreach.hpp"
#include "pwiz/utility/misc/random_access_compressed_ifstream.hpp"

namespace pwiz {
namespace mziddata {


using namespace std;
using boost::shared_ptr;


namespace {

string GetXMLRootElement(const string& fileheader)
{
    const static boost::regex e("<\\?xml.*?>.*?<([^?!]\\S+?)[\\s>]");

    // convert Unicode to ASCII
    string asciiheader;
    asciiheader.reserve(fileheader.size());
    BOOST_FOREACH(char c, fileheader)
    {
        if(c > 0)
            asciiheader.push_back(c);
    }

    boost::smatch m;
    if (boost::regex_search(asciiheader, m, e))
        return m[1];
    throw runtime_error("[GetXMLRootElement] Root element not found (header is not well-formed XML)");
}

string GetXMLRootElement(istream& is)
{
    char buf[513];
    is.read(buf, 512);
    return GetXMLRootElement(buf);
}

string GetXMLRootElementFromFile(const string& filepath)
{
    pwiz::util::random_access_compressed_ifstream file(filepath.c_str());
    if (!file)
        throw runtime_error("[GetXMLRootElementFromFile] Error opening file");
    return GetXMLRootElement(file);
}


class Reader_mzid : public Reader
{
    public:

    virtual std::string identify(const std::string& filename, const std::string& head) const
    {
         istringstream iss(head);
		 return std::string((type(iss) != Type_Unknown)?getType():"");
    }

    virtual void read(const std::string& filename, const std::string& head, MzIdentML& result, int documentIndex = 0) const
    {
        if (documentIndex != 0)
            throw ReaderFail("[Reader_mzid::read] multiple documents not supported");

		shared_ptr<istream> is(new pwiz::util::random_access_compressed_ifstream(filename.c_str()));
        if (!is.get() || !*is)
            throw runtime_error(("[Reader_mzid::read] Unable to open file " + filename).c_str());

       switch (type(*is))
        {
            case Type_mzid:
            {
                Serializer_mzIdentML serializer;
                serializer.read(is, result);
                break;
            }
            case Type_Unknown:
            default:
            {
                throw runtime_error("[Reader_mzid::read] This isn't happening.");
            }
        }
    }

    virtual void read(const std::string& filename,
                      const std::string& head,
                      std::vector<MzIdentMLPtr>& results) const
    {
        results.push_back(MzIdentMLPtr(new MzIdentML));
        read(filename, head, *results.back());
    }

	virtual const char *getType() const {return "mzIdentML";}

    private:

    enum Type { Type_mzid, Type_Unknown };

    Type type(istream& is) const
    {
        try
        {
            string rootElement = GetXMLRootElement(is);
            if (rootElement == "mzIdentML")
                return Type_mzid;
        }
        catch (runtime_error&)
        {
        }
        return Type_Unknown;
    }
};


} // namespace


/// default Reader list
PWIZ_API_DECL DefaultReaderList::DefaultReaderList()
{
    push_back(ReaderPtr(new Reader_mzid));
}


} // namespace mziddata
} // namespace pwiz
