//
// Reader_Bruker.cpp
//
// 
// Original author: Matt Chambers <matt.chambers .@. vanderbilt.edu>
//
// Copyright 2008 Vanderbilt University - Nashville, TN 37232
//
// Licensed under Creative Commons 3.0 United States License, which requires:
//  - Attribution
//  - Noncommercial
//  - No Derivative Works
//
// http://creativecommons.org/licenses/by-nc-nd/3.0/us/
//
// Unless required by applicable law or agreed to in writing, software 
// distributed under the License is distributed on an "AS IS" BASIS, 
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
// See the License for the specific language governing permissions and 
// limitations under the License.
//

#define PWIZ_SOURCE

// CompassXtractMS DLL usage is msvc only - mingw doesn't provide com support
#if (!defined(_MSC_VER) && defined(PWIZ_READER_BRUKER))
#undef PWIZ_READER_BRUKER
#endif


#include "Reader_Bruker.hpp"
#include "Reader_Bruker_Detail.hpp"
#include "pwiz/utility/misc/String.hpp"
#include "pwiz/data/msdata/Version.hpp"
#include <stdexcept>


// A Bruker Analysis source (representing a "run") is actually a directory
// It contains several files related to a single acquisition, e.g.:
// fid, acqu, acqus, Analysis.FAMethod, AnalysisParameter.xml, sptype

PWIZ_API_DECL
std::string pwiz::msdata::Reader_Bruker::identify(const std::string& filename,
                                                  const std::string& head) const
{
    switch (detail::format(filename))
    {
        case pwiz::msdata::detail::Reader_Bruker_Format_FID: return "Bruker FID";
        case pwiz::msdata::detail::Reader_Bruker_Format_YEP: return "Bruker YEP";
        case pwiz::msdata::detail::Reader_Bruker_Format_BAF: return "Bruker BAF";
        case pwiz::msdata::detail::Reader_Bruker_Format_U2: return "Bruker U2";

        case pwiz::msdata::detail::Reader_Bruker_Format_Unknown:
        default:
            return "";
    }
}


#ifdef PWIZ_READER_BRUKER
#include "pwiz/utility/misc/SHA1Calculator.hpp"
#include "boost/shared_ptr.hpp"
#include <boost/foreach.hpp>
//#include "Reader_Bruker_Detail.hpp"
#include "SpectrumList_Bruker.hpp"
#include "ChromatogramList_Bruker.hpp"
#include <iostream>
#include <iomanip>


namespace pwiz {
namespace msdata {


using namespace std;
using boost::shared_ptr;
using boost::lexical_cast;
using boost::bad_lexical_cast;
using namespace pwiz::util;
using namespace pwiz::msdata::detail;


//
// Reader_Bruker
//

namespace {


inline char idref_allowed(char c)
{
    return isalnum(c) || c=='-' ? 
           c : 
           '_';
}


string stringToIDREF(const string& s)
{
    string result = s;
    transform(result.begin(), result.end(), result.begin(), idref_allowed);
    return result;
}


void fillInMetadata(const string& rootpath, MSData& msd, Reader_Bruker_Format format)
{
    msd.cvs = defaultCVList();

    bfs::path p(rootpath);
    msd.id = stringToIDREF(p.leaf());

    SoftwarePtr software(new Software);
    software->id = "CompassXtract";
    software->set(MS_CompassXtract);
    software->version = "1.0";
    msd.softwarePtrs.push_back(software);

    SoftwarePtr softwarePwiz(new Software);
    softwarePwiz->id = "pwiz_Reader_Bruker";
    softwarePwiz->set(MS_pwiz);
    softwarePwiz->version = pwiz::msdata::Version::str();
    msd.softwarePtrs.push_back(softwarePwiz);

    DataProcessingPtr dpPwiz(new DataProcessing);
    dpPwiz->id = "pwiz_Reader_Bruker_conversion";
    dpPwiz->processingMethods.push_back(ProcessingMethod());
    dpPwiz->processingMethods.back().softwarePtr = softwarePwiz;
    dpPwiz->processingMethods.back().cvParams.push_back(MS_Conversion_to_mzML);
    msd.dataProcessingPtrs.push_back(dpPwiz);

    // TODO: read instrument "family" from (first) source

    //initializeInstrumentConfigurationPtrs(msd, rawfile, softwareXcalibur);
    //if (!msd.instrumentConfigurationPtrs.empty())
    //    msd.run.defaultInstrumentConfigurationPtr = msd.instrumentConfigurationPtrs[0];

    msd.run.id = boost::to_lower_copy(stringToIDREF(p.leaf()));
    //msd.run.startTimeStamp = creationDateToStartTimeStamp(rawfile.getCreationDate());
}

} // namespace


PWIZ_API_DECL
void Reader_Bruker::read(const string& filename,
                         const string& head,
                         int sampleIndex, 
                         MSData& result) const
{
    if (sampleIndex != 0)
        throw ReaderFail("[Reader_Bruker::read] multiple samples not supported");

    Reader_Bruker_Format format = detail::format(filename);
    if (format == Reader_Bruker_Format_Unknown)
        throw ReaderFail("[Reader_Bruker::read] Path given is not a recognized Bruker format");


    // trim filename from end of source path if necessary (it's not valid to pass to CompassXtract)
    bfs::path rootpath = filename;
    if (bfs::is_regular_file(rootpath))
        rootpath = rootpath.branch_path();

    CompassXtractWrapperPtr compassXtractWrapperPtr(new CompassXtractWrapper(rootpath, format));

    SpectrumList_Bruker* sl = new SpectrumList_Bruker(result, rootpath.string(), format, compassXtractWrapperPtr);
    ChromatogramList_Bruker* cl = new ChromatogramList_Bruker(result, rootpath.string(), format, compassXtractWrapperPtr);
    result.run.spectrumListPtr = SpectrumListPtr(sl);
    result.run.chromatogramListPtr = ChromatogramListPtr(cl);

    fillInMetadata(filename, result, format);
}


} // namespace msdata
} // namespace pwiz


#else // PWIZ_READER_BRUKER

//
// non-MSVC implementation
//

namespace pwiz {
namespace msdata {

using namespace std;

PWIZ_API_DECL void Reader_Bruker::read(const string& filename, const string& head, MSData& result) const
{
    throw ReaderFail("[Reader_Bruker::read()] Bruker Analysis reader not implemented: "
#ifdef _MSC_VER // should be possible, apparently somebody decided to skip it
        "support was explicitly disabled when program was built"
#elif defined(WIN32) // wrong compiler
        "program was built without COM support and cannot access CompassXtract DLLs - try building with MSVC instead of GCC"
#else // wrong platform
        "requires CompassXtract which only works on Windows"
#endif
		);
}

} // namespace msdata
} // namespace pwiz

#endif // PWIZ_READER_BRUKER

