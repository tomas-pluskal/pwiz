//
// FullReaderList.cpp
//
//
// Original author: Darren Kessner <Darren.Kessner@cshs.org>
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


#define PWIZ_SOURCE

#include "FullReaderList.hpp"
#include "pwiz_aux/isb/readers/waters/Reader_Waters.hpp"
#include "pwiz_aux/msrc/data/vendor_readers/Bruker/Reader_Bruker.hpp"
#include "pwiz_aux/msrc/data/vendor_readers/ABI/Reader_ABI.hpp"
#include <iostream>
#include <fstream>


namespace pwiz {
namespace msdata {


using namespace std;
using boost::shared_ptr;


PWIZ_API_DECL FullReaderList::FullReaderList()
{
    #ifdef _MSC_VER // vendor DLL usage is msvc only - mingw doesn't provide com support
    push_back(ReaderPtr(new Reader_Waters)); 
    push_back(ReaderPtr(new Reader_Bruker));
    push_back(ReaderPtr(new Reader_ABI));
    #endif
}


} // namespace msdata
} // namespace pwiz


