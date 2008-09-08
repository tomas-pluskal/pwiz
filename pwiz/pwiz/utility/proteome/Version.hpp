//
// Version.hpp
//
//
// Original author: Matt Chambers <matt.chambers .@. vanderbilt.edu>
//
// Copyright 2008 Spielberg Family Center for Applied Proteomics
//   Cedars Sinai Medical Center, Los Angeles, California  90048
// Copyright 2008 Vanderbilt University - Nashville, TN 37232
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


#ifndef _PWIZ_PROTEOME_VERSION_HPP_
#define _PWIZ_PROTEOME_VERSION_HPP_

#include <string>

namespace pwiz {
namespace proteome {

struct Version
{
    static int Major()                {return 1;}
    static int Minor()                {return 4;}
    static int Revision()             {return 1;}
    static std::string str()          {return "1.4.1";}
    static std::string LastModified() {return "9/8/2008";}
};

} // namespace proteome
} // namespace pwiz

#endif // _PWIZ_PROTEOME_VERSION_HPP_
