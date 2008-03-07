//
// CVParam.hpp
//
//
// Darren Kessner <Darren.Kessner@cshs.org>
//
// Copyright 2007 Spielberg Family Center for Applied Proteomics
//   Cedars-Sinai Medical Center, Los Angeles, California  90048
//   Unauthorized use or reproduction prohibited
//


#ifndef _CVPARAM_HPP_
#define _CVPARAM_HPP_


#include "cv.hpp"
#include "boost/lexical_cast.hpp"
#include <string>
#include <iosfwd>
#include <vector>


namespace pwiz {
namespace msdata {


/// represents a tag-value pair, where the tag comes from the controlled vocabulary
struct CVParam
{
    CVID cvid;
    std::string value;
    CVID units;

    /// template constructor performs automatic conversion from value types
    /// that can be lexical_casted to a string
    template <typename value_type>
    CVParam(CVID _cvid, const value_type& _value, CVID _units = CVID_Unknown)
    :   cvid(_cvid), 
        value(boost::lexical_cast<std::string>(_value)),
        units(_units)
    {}

    /// special case for bool (no lexical_cast)
    CVParam(CVID _cvid, bool _value, CVID _units = CVID_Unknown)
    :   cvid(_cvid), value(_value ? "true" : "false"), units(_units)
    {}

    /// constructor for non-valued CVParams
    CVParam(CVID _cvid = CVID_Unknown)
    :   cvid(_cvid), units(CVID_Unknown)
    {}

    /// templated value access with type conversion
    template<typename value_type>
    value_type valueAs() const
    {
        return !value.empty() ? boost::lexical_cast<value_type>(value) 
                              : boost::lexical_cast<value_type>(0);
    } 

    /// convenience function to return string for the cvid 
    std::string name() const;

    /// convenience function to return string for the units 
    std::string unitsName() const;

    /// convenience function to return time in seconds (throws if units not a time unit)
    double timeInSeconds() const;

    /// functor for finding CVParam with specified exact CVID in a collection of CVParams:
    ///
    /// vector<CVParam>::const_iterator it =
    ///     find_if(params.begin(), params.end(), CVParam::Is(MS_software));
    ///
    struct Is 
    {
        Is(CVID cvid) : cvid_(cvid) {}
        bool operator()(const CVParam& param) const {return param.cvid == cvid_;}
        CVID cvid_;
    };

    /// functor for finding children of a specified CVID in a collection of CVParams:
    ///
    /// vector<CVParam>::const_iterator it =
    ///     find_if(params.begin(), params.end(), CVParam::IsChildOf(MS_software));
    ///
    struct IsChildOf
    {
        IsChildOf(CVID cvid) : cvid_(cvid) {}
        bool operator()(const CVParam& param) const {return cvIsA(param.cvid, cvid_);}
        CVID cvid_;
    };

    /// equality operator
    bool operator==(const CVParam& that) const
    {
        return that.cvid==cvid && that.value==value && that.units==units;
    }

    /// inequality operator
    bool operator!=(const CVParam& that) const
    {
        return !operator==(that);
    }

    bool empty() const {return cvid==CVID_Unknown && value.empty() && units==CVID_Unknown;}
};


/// special case for bool (no lexical_cast)
/// (this has to be outside the class for gcc 3.4, inline for msvc)
template<>
inline bool CVParam::valueAs<bool>() const
{
    return value == "true";
}


std::ostream& operator<<(std::ostream& os, const CVParam& param);


} // namespace msdata
} // namespace pwiz


#endif // _CVPARAM_HPP_


