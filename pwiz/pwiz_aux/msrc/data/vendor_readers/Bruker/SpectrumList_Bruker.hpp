//
// SpectrumList_Bruker.hpp
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


#ifndef _SPECTRUMLIST_BRUKER_HPP_
#define _SPECTRUMLIST_BRUKER_HPP_

#include "pwiz/utility/misc/Export.hpp"
#include "pwiz/data/msdata/SpectrumListBase.hpp"
#include "pwiz/utility/misc/IntegerSet.hpp"
#include "pwiz/utility/misc/Filesystem.hpp"
#include "Reader_Bruker_Detail.hpp"
#include <map>


using namespace std;
using boost::shared_ptr;
using boost::lexical_cast;
using boost::bad_lexical_cast;


namespace pwiz {
namespace msdata {
namespace detail {

using namespace pwiz::vendor_api::Bruker;

//
// SpectrumList_Bruker
//
class PWIZ_API_DECL SpectrumList_Bruker : public SpectrumListBase
{
    public:

    SpectrumList_Bruker(MSData& msd,
                        const string& rootpath,
                        Reader_Bruker_Format format,
                        CompassDataPtr compassDataPtr);

    virtual size_t size() const;
    virtual const SpectrumIdentity& spectrumIdentity(size_t index) const;
    virtual size_t find(const string& id) const;
    virtual SpectrumPtr spectrum(size_t index, bool getBinaryData) const;
    SpectrumPtr spectrum(size_t index, bool getBinaryData, const pwiz::util::IntegerSet& msLevelsToCentroid) const;

    MSSpectrumPtr getMSSpectrumPtr(size_t index) const;

    private:

    MSData& msd_;
    bfs::path rootpath_;
    Reader_Bruker_Format format_;
    mutable CompassDataPtr compassDataPtr_;
    size_t size_;
    vector<bfs::path> sourcePaths_;

    struct IndexEntry : public SpectrumIdentity
    {
        size_t declaration;
        long collection; // -1 for an MS spectrum
        long scan;
    };

    vector<IndexEntry> index_;

    // idToIndexMap_["scan=<#>" or "file=<sourceFile::id>"] == index
    map<string, size_t> idToIndexMap_;

    void fillSourceList();
    void createIndex();
    //string findPrecursorID(int precursorMsLevel, size_t index) const;
};

} // detail
} // msdata
} // pwiz

#endif // _SPECTRUMLIST_BRUKER_HPP_
