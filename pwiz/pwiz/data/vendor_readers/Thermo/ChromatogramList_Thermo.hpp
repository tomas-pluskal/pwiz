//
// $Id$
//
//
// Original author: Darren Kessner <darren@proteowizard.org>
//
// Copyright 2008 Spielberg Family Center for Applied Proteomics
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


#ifndef _CHROMATOGRAMLIST_THERMO_
#define _CHROMATOGRAMLIST_THERMO_


#include "pwiz/utility/misc/Export.hpp"
#include "pwiz/data/msdata/ChromatogramListBase.hpp"
#include "pwiz/utility/vendor_api/thermo/RawFile.h"
#include <map>
#include <vector>
#include <boost/thread/once.hpp>

using namespace std;
using boost::shared_ptr;
using boost::lexical_cast;
using boost::bad_lexical_cast;
using namespace pwiz::vendor_api::Thermo;

namespace pwiz {
namespace msdata {
namespace detail {

class PWIZ_API_DECL ChromatogramList_Thermo : public ChromatogramListBase
{
public:

    ChromatogramList_Thermo(const MSData& msd, RawFilePtr rawfile);
    virtual size_t size() const;
    virtual const ChromatogramIdentity& chromatogramIdentity(size_t index) const;
    virtual size_t find(const string& id) const;
    virtual ChromatogramPtr chromatogram(size_t index, bool getBinaryData) const;
    
    private:

    const MSData& msd_;
    shared_ptr<RawFile> rawfile_;

    mutable boost::once_flag indexInitialized_;

    struct IndexEntry : public ChromatogramIdentity
    {
        CVID chromatogramType;
        ControllerType controllerType;
        long controllerNumber;
        string filter;
        double q1, q3;
        double q3Offset;
    };

    mutable vector<IndexEntry> index_;
    mutable map<string, size_t> idMap_;

    void createIndex() const;
};

} // detail
} // msdata
} // pwiz

#endif // _CHROMATOGRAMLIST_THERMO_
