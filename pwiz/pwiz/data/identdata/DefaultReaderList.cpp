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
#include "pwiz/utility/misc/Std.hpp"
#include "DefaultReaderList.hpp"
#include "Serializer_mzid.hpp"
#include "Serializer_pepXML.hpp"
#include "MascotReader.hpp"
//#include "References.hpp"
#include "pwiz/data/identdata/Version.hpp"
#include "boost/regex.hpp"


namespace pwiz {
namespace identdata {




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

AnalysisSoftwarePtr getPwizSoftware(IdentData& mzid)
{
    string version = pwiz::identdata::Version::str();

    AnalysisSoftwarePtr result;

    BOOST_FOREACH(const AnalysisSoftwarePtr& softwarePtr, mzid.analysisSoftwareList)
        if (softwarePtr->softwareName.hasCVParam(MS_pwiz) && softwarePtr->version==version)
        {
            result = softwarePtr;
            if (result->contactRolePtr.get() && result->contactRolePtr->contactPtr.get())
                return result;
        }

    ContactPtr contactPwiz;
    BOOST_FOREACH(const ContactPtr& contactPtr, mzid.auditCollection)
        if (contactPtr->name == "ProteoWizard")
            contactPwiz = contactPtr;

    if (!contactPwiz.get())
    {
        contactPwiz.reset(new Organization("ORG_PWIZ", "ProteoWizard"));
        contactPwiz->set(MS_contact_email, "support@proteowizard.org");
        mzid.auditCollection.push_back(contactPwiz);
    }

    if (!result.get())
    {
        result.reset(new AnalysisSoftware("pwiz_" + version, "ProteoWizard MzIdentML"));
        result->softwareName.set(MS_pwiz);
        result->version = version;
        mzid.analysisSoftwareList.push_back(result);
    }

    if (!result->contactRolePtr.get())
    {
        result->contactRolePtr.reset(new ContactRole);
        result->contactRolePtr->cvid = MS_software_vendor;
    }
    result->contactRolePtr->contactPtr = contactPwiz;

    return result;
}

void fillInCommonMetadata(const string& filename, IdentData& mzid)
{
    mzid.cvs = defaultCVList();

    // add pwiz software and contact metadata
    AnalysisSoftwarePtr softwarePwiz = getPwizSoftware(mzid);
}

class Reader_mzid : public Reader
{
    public:

    virtual std::string identify(const std::string& filename, const std::string& head) const
    {
        istringstream iss(head);
        return std::string((type(iss) != Type_Unknown)?getType():"");
    }

    virtual void read(const std::string& filename, const std::string& head, IdentDataPtr& result, const Config& config) const
    {
        if (!result.get())
            throw ReaderFail("[Reader_mzid::read] NULL valued IdentDataPtr passed in.");
        return read(filename, head, *result, config);
    }
    
    virtual void read(const std::string& filename, const std::string& head, IdentData& result, const Config& config) const
    {
        shared_ptr<istream> is(new pwiz::util::random_access_compressed_ifstream(filename.c_str()));
        if (!is.get() || !*is)
            throw runtime_error(("[Reader_mzid::read] Unable to open file " + filename).c_str());

       switch (type(*is))
        {
            case Type_mzid:
            {
                Serializer_mzIdentML::Config serializerConfig;
                serializerConfig.readSequenceCollection = !config.ignoreSequenceCollectionAndAnalysisData;
                serializerConfig.readAnalysisData = !config.ignoreSequenceCollectionAndAnalysisData;
                Serializer_mzIdentML serializer(serializerConfig);
                serializer.read(is, result, config.iterationListenerRegistry);
                break;
            }
            case Type_Unknown:
            default:
            {
                throw runtime_error("[Reader_mzid::read] This isn't happening.");
            }
        }

        fillInCommonMetadata(filename, result);
    }

    virtual void read(const std::string& filename,
                      const std::string& head,
                      std::vector<IdentDataPtr>& results,
                      const Config& config) const
    {
        results.push_back(IdentDataPtr(new IdentData));
        read(filename, head, *results.back(), config);
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


class Reader_pepXML : public Reader
{
    virtual std::string identify(const std::string& filename, const std::string& head) const
    {
        std::string result;
        try
        {
            string rootElement = GetXMLRootElement(head);
            result = (rootElement == "msms_pipeline_analysis" ? getType() : "");
        }
        catch (runtime_error&)
        {
        }
        return result;
    }

    virtual void read(const std::string& filename, const std::string& head, IdentData& result, const Config& config) const
    {
        shared_ptr<istream> is(new pwiz::util::random_access_compressed_ifstream(filename.c_str()));
        if (!is.get() || !*is)
            throw runtime_error("[Reader_pepXML::read] Unable to open file " + filename);

        Serializer_pepXML serializer(Serializer_pepXML::Config(!config.ignoreSequenceCollectionAndAnalysisData));
        serializer.read(is, result, config.iterationListenerRegistry);
        fillInCommonMetadata(filename, result);
    }

    virtual void read(const std::string& filename, const std::string& head, IdentDataPtr& result, const Config& config) const
    {
        if (!result.get())
            throw ReaderFail("[Reader_pepXML::read] NULL valued IdentDataPtr passed in.");
        return read(filename, head, *result, config);
    }

    virtual void read(const std::string& filename,
                      const std::string& head,
                      std::vector<IdentDataPtr>& results,
                      const Config& config) const
    {
        results.push_back(IdentDataPtr(new IdentData));
        read(filename, head, *results.back(), config);
    }

    virtual const char *getType() const {return "pepXML";}
};


} // namespace


/// default Reader list
PWIZ_API_DECL DefaultReaderList::DefaultReaderList()
{
    push_back(ReaderPtr(new Reader_mzid));
    push_back(ReaderPtr(new Reader_pepXML));
    push_back(ReaderPtr(new MascotReader));
}


} // namespace identdata
} // namespace pwiz
