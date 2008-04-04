//
// PeakDetectorNaive.cpp
//
//
// Original author: Darren Kessner <Darren.Kessner@cshs.org>
//
// Copyright 2006 Louis Warschaw Prostate Cancer Center
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


#include "PeakDetectorNaive.hpp"
#include "data/misc/FrequencyData.hpp"
#include "data/misc/PeakData.hpp"
#include <iostream>


using namespace std;
using namespace pwiz::data;
using namespace pwiz::data::peakdata;


namespace pwiz {
namespace peaks {


class PeakDetectorNaiveImpl : public PeakDetectorNaive
{
    public:

    PeakDetectorNaiveImpl(double noiseFactor, unsigned int detectionRadius);
    
    virtual double noiseFactor() const {return noiseFactor_;}
    virtual unsigned int detectionRadius() const {return detectionRadius_;}
    virtual void findPeaks(const FrequencyData& fd, peakdata::Scan& result) const;

    private:

    double noiseFactor_;
    unsigned int detectionRadius_;
};


auto_ptr<PeakDetectorNaive> PeakDetectorNaive::create(double noiseFactor, 
                                                      unsigned int detectionRadius)
{
    return auto_ptr<PeakDetectorNaive>(new PeakDetectorNaiveImpl(noiseFactor, detectionRadius)); 
}


PeakDetectorNaiveImpl::PeakDetectorNaiveImpl(double noiseFactor, unsigned int detectionRadius)
:   noiseFactor_(noiseFactor),
    detectionRadius_(detectionRadius)
{} 


namespace {
inline double height(FrequencyData::const_iterator it)
{
    return abs(it->y);
}

bool isPeak(const FrequencyData::const_iterator& it,
            const FrequencyData::container& data,
            double threshold,
            unsigned int detectionRadius)
{
    if (it-data.begin()<(int)detectionRadius || data.end()-it<=(int)detectionRadius)
        return false;

    if (height(it) <= threshold)
        return false;

    for (int i=-(int)detectionRadius; i<0; i++)
        if (height(it+i) > height(it+i+1))
            return false;

    for (unsigned int i=0; i<detectionRadius; i++)
        if (height(it+i) < height(it+i+1))
            return false;

    return true;
}
}//namespace


void PeakDetectorNaiveImpl::findPeaks(const FrequencyData& fd, peakdata::Scan& result) const
{
    result.scanNumber = fd.scanNumber(); 
    result.retentionTime = fd.retentionTime(); 
    result.observationDuration = fd.observationDuration();
    result.calibrationParameters = fd.calibrationParameters();
    result.peakFamilies.clear();

    const double noiseLevel = sqrt(fd.variance());
    const double threshold = noiseLevel * noiseFactor_;

    for (FrequencyData::const_iterator it=fd.data().begin(); it!=fd.data().end(); ++it)
    if (isPeak(it, fd.data(), threshold, detectionRadius_))
    {
        result.peakFamilies.push_back(PeakFamily());
        PeakFamily& peakFamily = result.peakFamilies.back();
        peakFamily.peaks.push_back(Peak());
        Peak& peak = peakFamily.peaks.back();

        peak.frequency = it->x;
        peak.intensity = it->y.real();
        peak.phase = it->y.imag();
    }
}


} // namespace peaks
} // namespace pwiz

