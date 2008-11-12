//
// SpectrumList_Thresholder.cpp
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


#include "SpectrumList_Thresholder.hpp"
#include "SavitzkyGolaySmoother.hpp"
#include "pwiz/utility/misc/Container.hpp"
#include "pwiz/utility/misc/String.hpp"
#include "pwiz/utility/math/round.hpp"
#include <iterator>
#include <numeric>


// workaround for MSVC's ADL gimpiness
namespace std
{
    bool operator< (const pwiz::msdata::MZIntensityPair& lhs, const pwiz::msdata::MZIntensityPair& rhs)
    {
        return lhs.intensity < rhs.intensity;
    }

    bool operator> (const pwiz::msdata::MZIntensityPair& lhs, const pwiz::msdata::MZIntensityPair& rhs)
    {
        return lhs.intensity > rhs.intensity;
    }

    struct MZIntensityPairIntensitySum
    {
        double operator() (double lhs, const pwiz::msdata::MZIntensityPair& rhs)
        {
            return lhs + rhs.intensity;
        }
    };

    struct MZIntensityPairIntensityFractionLessThan
    {
        MZIntensityPairIntensityFractionLessThan(double denominator)
            : denominator_(denominator)
        {
        }

        bool operator() (const pwiz::msdata::MZIntensityPair& lhs, const pwiz::msdata::MZIntensityPair& rhs)
        {
            return (lhs.intensity / denominator_) < (rhs.intensity / denominator_);
        }

        private:
        double denominator_;
    };

    struct MZIntensityPairIntensityFractionGreaterThan
    {
        MZIntensityPairIntensityFractionGreaterThan(double denominator)
            : denominator_(denominator)
        {
        }

        bool operator() (const pwiz::msdata::MZIntensityPair& lhs, const pwiz::msdata::MZIntensityPair& rhs)
        {
            return (lhs.intensity / denominator_) > (rhs.intensity / denominator_);
        }

        private:
        double denominator_;
    };
}


namespace pwiz {
namespace analysis {


using namespace std;
using namespace msdata;
using namespace pwiz::util;


namespace
{
    const char* byTypeMostIntenseName[] = {"most intense count (excluding ties at the threshold)",
                                           "most intense count (including ties at the threshold)",
                                           "absolute intensity greater than",
                                           "with greater intensity relative to BPI",
                                           "with greater intensity relative to TIC",
                                           "most intense TIC cutoff"};

    const char* byTypeLeastIntenseName[] = {"least intense count (excluding ties at the threshold)",
                                            "least intense count (including ties at the threshold)",
                                            "absolute intensity less than",
                                            "with less intensity relative to BPI",
                                            "with less intensity relative to TIC",
                                            "least intense TIC cutoff"};

    void threshold(const SpectrumPtr s,
                   ThresholdingBy_Type byType,
                   double threshold,
                   ThresholdingOrientation orientation)
    {
        if (byType == ThresholdingBy_Count ||
            byType == ThresholdingBy_CountAfterTies)
        {
            // if count threshold is greater than number of data points, return as is
            if (s->defaultArrayLength <= threshold)
                return;
            else if (threshold == 0)
            {
                s->getMZArray()->data.clear();
                s->getIntensityArray()->data.clear();
                return;
            }
        }

        vector<MZIntensityPair> mzIntensityPairs;
        s->getMZIntensityPairs(mzIntensityPairs);

        greater<MZIntensityPair> orientationMore_Predicate;
        less<MZIntensityPair> orientationLess_Predicate;

        if (orientation == Orientation_MostIntense)
            sort(mzIntensityPairs.begin(), mzIntensityPairs.end(), orientationMore_Predicate);
        else if (orientation == Orientation_LeastIntense)
            sort(mzIntensityPairs.begin(), mzIntensityPairs.end(), orientationLess_Predicate);
        else
            throw runtime_error("[threshold()] invalid orientation type");

        double tic = accumulate(mzIntensityPairs.begin(), mzIntensityPairs.end(), 0.0, MZIntensityPairIntensitySum());
        double bpi = orientation == Orientation_MostIntense ? mzIntensityPairs.front().intensity
                                                            : mzIntensityPairs.back().intensity;

        vector<MZIntensityPair>::iterator thresholdItr;
        switch (byType)
        {
            case ThresholdingBy_Count:
                // no need to check bounds on thresholdItr because it gets checked above
                thresholdItr = mzIntensityPairs.begin() + (size_t) threshold;

                // iterate backward until a non-tie is found
                while (true)
                {
                    const double& i = thresholdItr->intensity;
                    if (thresholdItr == mzIntensityPairs.begin())
                        break;
                    else if (i != (--thresholdItr)->intensity)
                    {
                        ++thresholdItr;
                        break;
                    }
                }
                break;

            case ThresholdingBy_CountAfterTies:
                // no need to check bounds on thresholdItr because it gets checked above
                thresholdItr = mzIntensityPairs.begin() + ((size_t) threshold)-1;

                // iterate forward until a non-tie is found
                while (true)
                {
                    const double& i = thresholdItr->intensity;
                    if (++thresholdItr == mzIntensityPairs.end() ||
                        i != thresholdItr->intensity)
                        break;
                }
                break;

            case ThresholdingBy_AbsoluteIntensity:
                if (orientation == Orientation_MostIntense)
                    thresholdItr = lower_bound(mzIntensityPairs.begin(),
                                               mzIntensityPairs.end(),
                                               MZIntensityPair(0, threshold),
                                               orientationMore_Predicate);
                else
                    thresholdItr = lower_bound(mzIntensityPairs.begin(),
                                               mzIntensityPairs.end(),
                                               MZIntensityPair(0, threshold),
                                               orientationLess_Predicate);
                break;

            case ThresholdingBy_FractionOfBasePeakIntensity:
                if (orientation == Orientation_MostIntense)
                    thresholdItr = lower_bound(mzIntensityPairs.begin(),
                                               mzIntensityPairs.end(),
                                               MZIntensityPair(0, threshold*bpi),
                                               MZIntensityPairIntensityFractionGreaterThan(bpi));
                else
                    thresholdItr = lower_bound(mzIntensityPairs.begin(),
                                               mzIntensityPairs.end(),
                                               MZIntensityPair(0, threshold*bpi),
                                               MZIntensityPairIntensityFractionLessThan(bpi));
                break;

            case ThresholdingBy_FractionOfTotalIntensity:
                if (orientation == Orientation_MostIntense)
                    thresholdItr = lower_bound(mzIntensityPairs.begin(),
                                               mzIntensityPairs.end(),
                                               MZIntensityPair(0, threshold*tic),
                                               MZIntensityPairIntensityFractionGreaterThan(tic));
                else
                    thresholdItr = lower_bound(mzIntensityPairs.begin(),
                                               mzIntensityPairs.end(),
                                               MZIntensityPair(0, threshold*tic),
                                               MZIntensityPairIntensityFractionLessThan(tic));
                break;

            case ThresholdingBy_FractionOfTotalIntensityCutoff:
                {
                    // starting at the (most/least intense point)/TIC fraction,
                    // calculate the running sum
                    vector<double> cumulativeIntensityFraction(1, mzIntensityPairs[0].intensity / tic);
                    size_t i=0;
                    while (cumulativeIntensityFraction.back() <= threshold &&
                           ++i < mzIntensityPairs.size())
                    {
                        cumulativeIntensityFraction.push_back(
                            cumulativeIntensityFraction[i-1] +
                            mzIntensityPairs[i].intensity / tic);
                    }

                    thresholdItr = mzIntensityPairs.begin() + i;

                    // iterate backward until a non-tie is found
                    while (thresholdItr != mzIntensityPairs.end())
                    {
                        const double& i = thresholdItr->intensity;
                        if (thresholdItr == mzIntensityPairs.begin())
                            break;
                        else if (i != (--thresholdItr)->intensity)
                        {
                            ++thresholdItr;
                            break;
                        }
                    }
                }
                break;

            default:
                throw runtime_error("[threshold()] invalid thresholding type");
        }

        s->setMZIntensityPairs(&mzIntensityPairs[0], thresholdItr - mzIntensityPairs.begin(),
            s->getIntensityArray()->cvParam(MS_intensity_array).units);
    }
} // namespace


PWIZ_API_DECL
SpectrumList_Thresholder::SpectrumList_Thresholder(const msdata::SpectrumListPtr& inner,
                                                   ThresholdingBy_Type byType,
                                                   double threshold,
                                                   ThresholdingOrientation orientation)
:   SpectrumListWrapper(inner),
    byType_(byType),
    threshold_(threshold),
    orientation_(orientation)
{
    if (byType_ == ThresholdingBy_Count ||
        byType_ == ThresholdingBy_CountAfterTies)
        threshold_ = round(threshold_);

    // add processing methods to the copy of the inner SpectrumList's data processing
    ProcessingMethod method;
    method.order = dp_->processingMethods.size();
    method.set(MS_thresholding);
    string name = orientation == Orientation_MostIntense ? byTypeMostIntenseName[byType_]
                                                         : byTypeLeastIntenseName[byType_];
    method.userParams.push_back(UserParam(name, lexical_cast<string>(threshold_)));
    dp_->processingMethods.push_back(method);
}


PWIZ_API_DECL SpectrumPtr SpectrumList_Thresholder::spectrum(size_t index, bool getBinaryData) const
{
    if (!getBinaryData)
        return inner_->spectrum(index, false);

    SpectrumPtr s = inner_->spectrum(index, true);
    threshold(s, byType_, threshold_, orientation_);
    s->dataProcessingPtr = dp_;
    return s;
}


} // namespace analysis 
} // namespace pwiz
