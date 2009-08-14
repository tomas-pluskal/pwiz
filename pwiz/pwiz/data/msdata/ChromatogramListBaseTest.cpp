//
// $Id$
//
//
// Original author: Darren Kessner <darren@proteowizard.org>
//
// Copyright 2009 Spielberg Family Center for Applied Proteomics
//   Cedars Sinai Medical Center, Los Angeles, California  90048
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


#include "ChromatogramListBase.hpp"
#include "pwiz/utility/misc/unit.hpp"
#include "boost/lexical_cast.hpp"
#include <iostream>


using namespace pwiz::msdata;
using namespace pwiz::util;
using namespace std;
using boost::lexical_cast;
using boost::shared_ptr;


class MyBase : public ChromatogramListBase
{
    public:
    virtual size_t size() const {return 0;}
    virtual const ChromatogramIdentity& chromatogramIdentity(size_t index) const {throw runtime_error("heh");}
    virtual ChromatogramPtr chromatogram(size_t index, bool getBinaryData = false) const {return ChromatogramPtr();}
};


void test()
{
    MyBase base;
    DataProcessingPtr dp(new DataProcessing("dp"));
    base.setDataProcessingPtr(dp);
    unit_assert(base.dataProcessingPtr().get() == dp.get());
}


int main()
{
    try
    {
        test();
        return 0;
    }
    catch (exception& e)
    {
        cerr << e.what() << endl;
        return 1;
    }
    catch (...)
    {
        cerr << "Caught unknown exception.\n";
        return 1;
    }
}


