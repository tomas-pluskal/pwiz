//
// $Id$
//
//
// Original author: Darren Kessner <darren@proteowizard.org>
//
// Copyright 2009 Center for Applied Molecular Medicine
//   University of Southern California, Los Angeles, CA
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
                                                                                                     
#ifndef _MZTOLERANCE_HPP_
#define _MZTOLERANCE_HPP_


#include "pwiz/utility/misc/Export.hpp"
#include <iosfwd>


namespace pwiz {
namespace analysis {


///
/// struct for expressing m/z tolerance in either amu or ppm
///
struct PWIZ_API_DECL MZTolerance
{
    enum Units {MZ, PPM};
    double value;
    Units units;

    MZTolerance(double _value = 0, Units _units = MZ)
    :   value(_value), units(_units)
    {}
};


std::ostream& PWIZ_API_DECL operator<<(std::ostream& os, const MZTolerance& mzt);
std::istream& PWIZ_API_DECL operator>>(std::istream& is, MZTolerance& mzt);
bool PWIZ_API_DECL operator==(const MZTolerance& a, const MZTolerance& b);


double& PWIZ_API_DECL operator+=(double& d, const MZTolerance& tolerance);
double& PWIZ_API_DECL operator-=(double& d, const MZTolerance& tolerance);
double PWIZ_API_DECL operator+(double d, const MZTolerance& tolerance);
double PWIZ_API_DECL operator-(double d, const MZTolerance& tolerance);


/// returns true iff a is in (b-tolerance, b+tolerance)
bool PWIZ_API_DECL isWithinTolerance(double a, double b, const MZTolerance& tolerance);
/// returns true iff b - a is greater than the value in tolerance (useful for matching sorted mass lists)
bool PWIZ_API_DECL lessThanTolerance(double a, double b, const MZTolerance& tolerance);


} // namespace analysis
} // namespace pwiz


#endif // _MZTOLERANCE_HPP_

