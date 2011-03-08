//
// $Id$
//
//
// Original author: Matt Chambers <matt.chambers .@. vanderbilt.edu>
//
// Copyright 2010 Vanderbilt University - Nashville, TN 37232
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


#ifndef _SERIALIZER_PEPXML_HPP_
#define _SERIALIZER_PEPXML_HPP_

#include "pwiz/utility/misc/Export.hpp"
#include "MzIdentML.hpp"
#include "pwiz/utility/misc/IterationListener.hpp"


namespace pwiz {
namespace mziddata {

/// MZIDData <-> pepXML stream serialization
class PWIZ_API_DECL Serializer_pepXML
{
    public:

    /// Serializer_pepXML configuration
    struct PWIZ_API_DECL Config
    {
        bool readSpectrumQueries;

        Config(bool readSpectrumQueries = true) : readSpectrumQueries(readSpectrumQueries) {}
    };

    Serializer_pepXML(const Config& config = Config()) : config_(config) {}

    /// write MZIDData object to ostream as pepXML
    void write(std::ostream& os, const MzIdentML& mzid, const std::string& filepath,
               const pwiz::util::IterationListenerRegistry* = 0) const;

    /// read in MZIDData object from a pepXML istream
    void read(boost::shared_ptr<std::istream> is, MzIdentML& mzid,
              const pwiz::util::IterationListenerRegistry* = 0) const;

    private:
    const Config config_;
    Serializer_pepXML(Serializer_pepXML&);
    Serializer_pepXML& operator=(Serializer_pepXML&);
};

} // namespace pwiz 
} // namespace mziddata 

#endif // _SERIALIZER_PEPXML_HPP_
