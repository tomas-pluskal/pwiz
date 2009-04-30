//
// Noise.cpp
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


#define PWIZ_SOURCE
#include "Noise.hpp"
#include <cmath>


namespace pwiz {
namespace analysis {


using namespace std;
using namespace pwiz::math;


//
// Noise
//


Noise::Noise()
:   mean(0), variance(0), standardDeviation(0)
{}


namespace {
double normalCDF(double x, double mean, double sd)
{
/*
    double inner = (x-mean)/(sd*sqrt(2.));
    return .5 * (1 + erf(inner));
*/
    return 0;
}
} // namespace


double Noise::pvalue(double value) 
{
    //return 1-normalCDF(value, mean, standardDeviation);
    return 0;
}


//
// NoiseCalculator_2Pass
//


NoiseCalculator_2Pass::NoiseCalculator_2Pass(Config config)
:   config_(config)
{}


Noise NoiseCalculator_2Pass::calculateNoise(const OrderedPairContainerRef& pairs) const
{
    // calculate initial stats

    double sumIntensity = 0;
    double sumIntensity2 = 0;
    
    for (OrderedPairContainerRef::const_iterator it=pairs.begin(); it!=pairs.end(); ++it)
    {
        double value = it->y;
        sumIntensity += value;
        sumIntensity2 += value*value;
    }
    
    size_t count = pairs.size();

    Noise result;
    result.mean = sumIntensity/count;
    result.variance = sumIntensity2/count - result.mean*result.mean;
    result.standardDeviation = sqrt(result.variance);

    // recalculate, excluding extreme values

    sumIntensity = 0;
    sumIntensity2 = 0;
    count = 0;

    for (OrderedPairContainerRef::const_iterator it=pairs.begin(); it!=pairs.end(); ++it)
    {
        double value = it->y;
        if (value > result.mean + result.standardDeviation*config_.zValueCutoff) continue;
        count++;
        sumIntensity += value;
        sumIntensity2 += value*value;
    }

    result.mean = sumIntensity/count;
    result.variance = sumIntensity2/count - result.mean*result.mean;
    result.standardDeviation = sqrt(result.variance);

    return result;
}


} // namespace analysis
} // namespace pwiz


