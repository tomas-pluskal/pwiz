//
// SpectrumList_PeakPicker.cpp
//
//
// Original author: Matt Chambers <matt.chambers <a.t> vanderbilt.edu>
//
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


#define PWIZ_SOURCE


#include "SpectrumList_PeakPicker.hpp"
#include "pwiz/utility/misc/Container.hpp"


namespace pwiz {
namespace analysis {


using namespace msdata;
using namespace pwiz::util;


PWIZ_API_DECL
SpectrumList_PeakPicker::SpectrumList_PeakPicker(
        const msdata::SpectrumListPtr& inner,
        PeakDetectorPtr algorithm,
        const IntegerSet& msLevelsToSmooth)
:   SpectrumListWrapper(inner),
    algorithm_(algorithm),
    msLevelsToSmooth_(msLevelsToSmooth)
{
    // add processing methods to the copy of the inner SpectrumList's data processing
    ProcessingMethod method;
    method.order = dp_->processingMethods.size();
    method.set(MS_peak_picking);
    //method.userParams.push_back(UserParam("Savitzky-Golay smoothing (9 point window)"));
    dp_->processingMethods.push_back(method);
}


PWIZ_API_DECL bool SpectrumList_PeakPicker::accept(const msdata::SpectrumListPtr& inner)
{
    return true;
}


PWIZ_API_DECL SpectrumPtr SpectrumList_PeakPicker::spectrum(size_t index, bool getBinaryData) const
{
    //if (!getBinaryData)
    //    return inner_->spectrum(index, false);

    SpectrumPtr s = inner_->spectrum(index, true);

    vector<CVParam>& cvParams = s->cvParams;
    vector<CVParam>::iterator itr = std::find(cvParams.begin(), cvParams.end(), MS_profile_mass_spectrum);

    // return non-profile spectra as-is
    if (itr == cvParams.end())
        return s;

    // replace profile term with centroid term
    *itr = MS_centroid_mass_spectrum;

    try
    {
        vector<double>& mzs = s->getMZArray()->data;
        vector<double>& intensities = s->getIntensityArray()->data;
        vector<double> xPeakValues, yPeakValues;
        algorithm_->detect(mzs, intensities, xPeakValues, yPeakValues);
        mzs.swap(xPeakValues);
        intensities.swap(yPeakValues);
        s->defaultArrayLength = mzs.size();
    }
    catch(std::exception& e)
    {
        throw std::runtime_error(std::string("[SpectrumList_PeakPicker] Error picking peaks: ") + e.what());
    }

    s->dataProcessingPtr = dp_;
    return s;
}


} // namespace analysis 
} // namespace pwiz
