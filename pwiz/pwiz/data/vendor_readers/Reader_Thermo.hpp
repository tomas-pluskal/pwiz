//
// Reader_Thermo.hpp
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


#ifndef _READER_THERMO_HPP_ 
#define _READER_THERMO_HPP_ 


#include "utility/misc/Export.hpp"
#include "data/msdata/Reader.hpp"

namespace pwiz {
namespace msdata {


class PWIZ_API_DECL Reader_Thermo : public Reader
{
    public:

    virtual std::string identify(const std::string& filename, 
                        const std::string& head) const; 

    virtual void read(const std::string& filename, 
                      const std::string& head, 
                      MSData& result) const;

	virtual const char *getType() const {return "Thermo";}

    /// checks header for "Finnigan" wide char string
	static bool hasRAWHeader(const std::string& head); 
};


} // namespace msdata
} // namespace pwiz


#endif // _READER_THERMO_HPP_ 

