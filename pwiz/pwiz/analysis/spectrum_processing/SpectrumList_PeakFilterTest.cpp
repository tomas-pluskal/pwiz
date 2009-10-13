//
// $Id: SpectrumList_ThresholderTest.cpp 1191 2009-08-14 19:33:05Z chambm $
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

#include "pwiz/data/msdata/MSData.hpp"
#include "pwiz/analysis/common/IDataFilter.hpp"
#include "PrecursorMassFilter.hpp"
#include "ThresholdFilter.hpp"
#include "SpectrumList_PeakFilter.hpp"
#include "pwiz/utility/misc/unit.hpp"
#include <vector>
#include <iostream>
#include <iterator>
#include "pwiz/data/msdata/examples.hpp"
#include "pwiz/data/msdata/TextWriter.hpp"
#include "pwiz/utility/misc/String.hpp"


using namespace std;
using namespace pwiz::util;
using namespace pwiz;
using namespace pwiz::msdata;
using namespace pwiz::analysis;

ostream* os_ = 0;

ostream& operator<< (ostream& os, const vector<double>& v)
{
    os << "(";
    for (size_t i=0; i < v.size(); ++i)
        os << " " << v[i];
    os << " )";
    return os;
}

vector<double> parseDoubleArray(const string& doubleArray)
{
    vector<double> doubleVector;
    vector<string> tokens;
    bal::split(tokens, doubleArray, bal::is_space());
    if (!tokens.empty() && !tokens[0].empty())
        for (size_t i=0; i < tokens.size(); ++i)
            doubleVector.push_back(lexical_cast<double>(tokens[i]));
    return doubleVector;
}


struct TestETDMassFilter
{
    // space-delimited doubles
    const char* inputMZArray;
    const char* inputIntensityArray;
    const char* outputMZArray;
    const char* outputIntensityArray;

    double matchingTolerance;
    bool usePPM;
    bool removePrecursor;
    bool removeReducedChargePrecursors;
    bool removeNeutralLossPrecursors;
    bool selectiveRemovalofPrecursors;
    bool blanketRemovalofNeutralLoss;
};

#define PRECURSOR_CHARGE 2
#define PRECURSOR_MASS 445.34

TestETDMassFilter testETDMassFilterData[] =
{
    {    
        "100 110 120 130 415.8215 422.337 422.8295 423.3215 427.3295 427.8215 428.3135 429.327 431.3425 436.3345 445.34 831.632 844.674 845.659 846.643 854.659 855.643 856.627 858.654 862.685 872.669 873.653 890.68 1000.0", 
        "10 20 30 40 50 60 70 80 90 100 110 120 130 140 150 160 170 180 190 200 210 220 230 240 250 260 270 280", 
        "100 110 120 130 415.8215 422.337 422.8295 423.3215 427.3295 427.8215 428.3135 429.327 431.3425 436.3345 445.34 831.632 844.674 845.659 846.643 854.659 855.643 856.627 858.654 862.685 872.669 873.653 890.68 1000.0", 
        "10 20 30 40 50 60 70 80 90 100 110 120 130 140 150 160 170 180 190 200 210 220 230 240 250 260 270 280", 
        0.1234, false, false, false, false, false, false
    }, // do nothing

    {    
        "100 110 120 130 415.8215 422.337 422.8295 423.3215 427.3295 427.8215 428.3135 429.327 431.3425 436.3345 445.34 445.35 831.632 844.674 845.659 846.643 854.659 855.643 856.627 858.654 862.685 872.669 873.653 890.68 1000.0", 
        "10 20 30 40 50 60 70 80 90 100 110 120 130 140 150 160 170 180 190 200 210 220 230 240 250 260 270 280 290", 
        "100 110 120 130 415.8215 422.337 422.8295 423.3215 427.3295 427.8215 428.3135 429.327 431.3425 436.3345 831.632 844.674 845.659 846.643 854.659 855.643 856.627 858.654 862.685 872.669 873.653 890.68 1000.0", 
        "10 20 30 40 50 60 70 80 90 100 110 120 130 140 170 180 190 200 210 220 230 240 250 260 270 280 290", 
        0.1234, false, true, false, false, false, false
    }, // remove precursor only

    {    
        "100 110 120 130 415.8215 422.337 422.8295 423.3215 427.3295 427.8215 428.3135 429.327 431.3425 436.3345 445.34 831.632 844.674 845.659 846.643 854.659 855.643 856.627 858.654 862.685 872.669 873.653 890.68 1000.0", 
        "10 20 30 40 50 60 70 80 90 100 110 120 130 140 150 160 170 180 190 200 210 220 230 240 250 260 270 280", 
        "100 110 120 130 415.8215 422.337 422.8295 423.3215 427.3295 427.8215 428.3135 429.327 431.3425 436.3345 445.34 831.632 844.674 845.659 846.643 854.659 855.643 856.627 858.654 862.685 872.669 873.653 1000.0", 
        "10 20 30 40 50 60 70 80 90 100 110 120 130 140 150 160 170 180 190 200 210 220 230 240 250 260 280", 
        0.1234, false, false, true, false, false, false
    }, // remove charge reduced precursors only

    {    
        "100 110 120 130 415.8215 422.337 422.8295 423.3215 427.3295 427.8215 428.3135 429.327 431.3425 436.3345 445.34 831.632 844.674 845.659 846.643 854.659 855.643 856.627 858.654 862.685 872.669 873.653 890.68 1000.0", 
        "10 20 30 40 50 60 70 80 90 100 110 120 130 140 150 160 170 180 190 200 210 220 230 240 250 260 270 280", 
        "100 110 120 130 415.8215 422.337 422.8295 423.3215 427.3295 427.8215 428.3135 429.327 431.3425 436.3345 831.632 844.674 845.659 846.643 854.659 855.643 856.627 858.654 862.685 872.669 873.653 1000.0", 
        "10 20 30 40 50 60 70 80 90 100 110 120 130 140 160 170 180 190 200 210 220 230 240 250 260 280", 
        0.1234, false, true, true, false, false, false
    }, // remove precursor and charge reduced precursors

    {    
        "100 110 120 130 415.8215 422.337 422.8295 423.3215 427.3295 427.8215 428.3135 429.327 431.3425 436.3345 445.34 831.632 844.674 845.659 846.643 854.659 855.643 856.627 858.654 862.685 872.669 873.653 890.68 1000.0", 
        "10 20 30 40 50 60 70 80 90 100 110 120 130 140 150 160 170 180 190 200 210 220 230 240 250 260 270 280", 
        "100 110 120 130 415.8215 422.337 422.8295 423.3215 427.3295 427.8215 428.3135 429.327 431.3425 436.3345 1000.0", 
        "10 20 30 40 50 60 70 80 90 100 110 120 130 140 280", 
        0.1234, false, true, true, true, false, false
    }, // remove precursor charge reduced precursors, and neutral losses

    {    
        "100 110 120 130 415.8215 422.337 422.8295 423.3215 427.3295 427.8215 428.3135 429.327 431.3425 436.3345 445.34 445.35 831.632 844.674 845.659 846.643 854.659 855.643 856.627 858.654 862.685 872.669 873.653 890.68 890.69 1000.0", 
        "10 20 30 40 50 60 70 80 90 100 110 120 130 140 15000 150 160 170 180 190 200 210 220 230 240 250 260 2700 280 290", 
        "100 110 120 130 415.8215 422.337 422.8295 423.3215 427.3295 427.8215 428.3135 429.327 431.3425 436.3345 445.35 890.69 1000.0", 
        "10 20 30 40 50 60 70 80 90 100 110 120 130 140 150 280 290", 
        0.3, false, true, true, true, true, false
    }, // remove precursor charge reduced precursors, and neutral losses -- most intense peak near precursor and charge reduced

    {    
        "100 110 120 130 415.8215 422.337 422.8295 423.3215 427.3295 427.8215 428.3135 429.327 431.3425 436.3345 445.34 445.35 831.632 844.674 845.659 846.643 854.659 855.643 856.627 858.654 862.685 872.669 873.653 890.68 1000.0", 
        "10 20 30 40 50 60 70 80 90 100 110 120 130 140 15000 150 160 170 180 190 200 210 220 230 240 250 260 270 280", 
        "100 110 120 130 415.8215 422.337 422.8295 423.3215 427.3295 427.8215 428.3135 429.327 431.3425 436.3345 1000.0", 
        "10 20 30 40 50 60 70 80 90 100 110 120 130 140 280", 
        1.0, false, true, true, true, false, true
    }, // remove precursor charge reduced precursors, and neutral losses -- blanket removal of neutral losses (60 Da window)

};

const size_t testETDMassFilterDataSize = sizeof(testETDMassFilterData) / sizeof(TestETDMassFilter);

void testMassRemoval()
{
    for (size_t i=0; i < testETDMassFilterDataSize; ++i)
    {
        SpectrumListSimple* sl = new SpectrumListSimple;
        SpectrumListPtr originalList(sl);
        SpectrumPtr s(new Spectrum);
        sl->spectra.push_back(s);

        TestETDMassFilter& t = testETDMassFilterData[i];

        vector<double> inputMZArray = parseDoubleArray(t.inputMZArray);
        vector<double> inputIntensityArray = parseDoubleArray(t.inputIntensityArray);
        s->set(MS_MSn_spectrum);
        s->set(MS_ms_level, 2);

        s->setMZIntensityArrays(inputMZArray, inputIntensityArray, MS_number_of_counts);
        s->precursors.resize(1);
        s->precursors[0].selectedIons.resize(1);
        s->precursors[0].selectedIons[0].set(MS_selected_ion_m_z, PRECURSOR_MASS, MS_m_z);
        s->precursors[0].selectedIons[0].set(MS_charge_state, PRECURSOR_CHARGE);

        MZTolerance tol(t.matchingTolerance, t.usePPM ? MZTolerance::PPM : MZTolerance::MZ);
        PrecursorMassFilter* filter = 0;
        if (t.removeNeutralLossPrecursors)
        {
            PrecursorMassFilter::Config params(tol, t.removePrecursor, t.removeReducedChargePrecursors, true, t.selectiveRemovalofPrecursors, t.blanketRemovalofNeutralLoss);
            filter = new PrecursorMassFilter(params);
        }
        else
        {
            PrecursorMassFilter::Config params(tol, t.removePrecursor, t.removeReducedChargePrecursors, true, t.selectiveRemovalofPrecursors, t.blanketRemovalofNeutralLoss, 0);
            filter = new PrecursorMassFilter(params);
        }
        SpectrumListPtr peakFilter(
            new SpectrumList_PeakFilter(originalList, filter));

        SpectrumPtr pFiltered = peakFilter->spectrum(0, true);

        vector<double> outputMZArray = parseDoubleArray(t.outputMZArray);
        vector<double> outputIntensityArray = parseDoubleArray(t.outputIntensityArray);

        unit_assert(pFiltered->getMZArray()->data.size() == outputMZArray.size());
        unit_assert(pFiltered->getIntensityArray()->data.size() == outputIntensityArray.size());
        for (size_t ii=0; ii < outputMZArray.size(); ++ii)
        {
            unit_assert_equal(pFiltered->getMZArray()->data[ii], outputMZArray[ii], 0.001);
            unit_assert_equal(pFiltered->getIntensityArray()->data[ii], outputIntensityArray[ii], 0.001);
        }
    }
}

struct TestThresholder
{
    // space-delimited doubles
    const char* inputMZArray;
    const char* inputIntensityArray;
    const char* outputMZArray;
    const char* outputIntensityArray;

    ThresholdingParams::ThresholdingBy_Type byType;
    double threshold;
    ThresholdingParams::ThresholdingOrientation orientation;
};

TestThresholder testThresholders[] =
{
    // absolute thresholding, keeping the most intense points
    { "1 2 3 4 5", "10 20 30 20 10", "1 2 3 4 5", "10 20 30 20 10", ThresholdingParams::ThresholdingBy_AbsoluteIntensity, 5, ThresholdingParams::Orientation_MostIntense },
    { "1 2 3 4 5", "10 20 30 20 10", "2 3 4", "20 30 20", ThresholdingParams::ThresholdingBy_AbsoluteIntensity, 10, ThresholdingParams::Orientation_MostIntense },
    { "1 2 3 4 5", "10 20 30 20 10", "2 3 4", "20 30 20", ThresholdingParams::ThresholdingBy_AbsoluteIntensity, 15, ThresholdingParams::Orientation_MostIntense },
    { "1 2 3 4 5", "10 20 30 20 10", "", "", ThresholdingParams::ThresholdingBy_AbsoluteIntensity, 30, ThresholdingParams::Orientation_MostIntense },

    // absolute thresholding, keeping the least intense points
    { "1 2 3 4 5", "10 20 30 20 10", "", "", ThresholdingParams::ThresholdingBy_AbsoluteIntensity, 5, ThresholdingParams::Orientation_LeastIntense },
    { "1 2 3 4 5", "10 20 30 20 10", "", "", ThresholdingParams::ThresholdingBy_AbsoluteIntensity, 10, ThresholdingParams::Orientation_LeastIntense },
    { "1 2 3 4 5", "10 20 30 20 10", "1 5", "10 10", ThresholdingParams::ThresholdingBy_AbsoluteIntensity, 15, ThresholdingParams::Orientation_LeastIntense },
    { "1 2 3 4 5", "10 20 30 20 10", "1 2 4 5", "10 20 20 10", ThresholdingParams::ThresholdingBy_AbsoluteIntensity, 30, ThresholdingParams::Orientation_LeastIntense },
    { "1 2 3 4 5", "10 20 30 20 10", "1 2 3 4 5", "10 20 30 20 10", ThresholdingParams::ThresholdingBy_AbsoluteIntensity, 50, ThresholdingParams::Orientation_LeastIntense },

    // relative thresholding to the base peak, keeping the most intense peaks
    { "1 2 3 4 5", "10 20 30 20 10", "1 2 3 4 5", "10 20 30 20 10", ThresholdingParams::ThresholdingBy_FractionOfBasePeakIntensity, 0.1, ThresholdingParams::Orientation_MostIntense },
    { "1 2 3 4 5", "10 20 30 20 10", "2 3 4", "20 30 20", ThresholdingParams::ThresholdingBy_FractionOfBasePeakIntensity, 0.34, ThresholdingParams::Orientation_MostIntense },
    { "1 2 3 4 5", "10 20 30 20 10", "2 3 4", "20 30 20", ThresholdingParams::ThresholdingBy_FractionOfBasePeakIntensity, 0.65, ThresholdingParams::Orientation_MostIntense },
    { "1 2 3 4 5", "10 20 30 20 10", "3", "30", ThresholdingParams::ThresholdingBy_FractionOfBasePeakIntensity, 0.67, ThresholdingParams::Orientation_MostIntense },
    { "1 2 3 4 5", "10 20 30 20 10", "", "", ThresholdingParams::ThresholdingBy_FractionOfBasePeakIntensity, 1.0, ThresholdingParams::Orientation_MostIntense },

    // relative thresholding to the base peak, keeping the least intense peaks
    { "1 2 3 4 5", "10 20 30 20 10", "", "", ThresholdingParams::ThresholdingBy_FractionOfBasePeakIntensity, 0.1, ThresholdingParams::Orientation_LeastIntense },
    { "1 2 3 4 5", "10 20 30 20 10", "", "", ThresholdingParams::ThresholdingBy_FractionOfBasePeakIntensity, 0.32, ThresholdingParams::Orientation_LeastIntense },
    { "1 2 3 4 5", "10 20 30 20 10", "1 5", "10 10", ThresholdingParams::ThresholdingBy_FractionOfBasePeakIntensity, 0.34, ThresholdingParams::Orientation_LeastIntense },
    { "1 2 3 4 5", "10 20 30 20 10", "1 2 4 5", "10 20 20 10", ThresholdingParams::ThresholdingBy_FractionOfBasePeakIntensity, 0.67, ThresholdingParams::Orientation_LeastIntense },
    { "1 2 3 4 5", "10 20 30 20 10", "1 2 4 5", "10 20 20 10", ThresholdingParams::ThresholdingBy_FractionOfBasePeakIntensity, 1.0, ThresholdingParams::Orientation_LeastIntense },

    // relative thresholding to total intensity, keeping the most intense peaks
    { "1 2 3 4 5", "10 20 30 20 10", "1 2 3 4 5", "10 20 30 20 10", ThresholdingParams::ThresholdingBy_FractionOfTotalIntensity, 0.1, ThresholdingParams::Orientation_MostIntense },
    { "1 2 3 4 5", "10 20 30 20 10", "2 3 4", "20 30 20", ThresholdingParams::ThresholdingBy_FractionOfTotalIntensity, 0.12, ThresholdingParams::Orientation_MostIntense },
    { "1 2 3 4 5", "10 20 30 20 10", "2 3 4", "20 30 20", ThresholdingParams::ThresholdingBy_FractionOfTotalIntensity, 0.21, ThresholdingParams::Orientation_MostIntense },
    { "1 2 3 4 5", "10 20 30 20 10", "3", "30", ThresholdingParams::ThresholdingBy_FractionOfTotalIntensity, 0.23, ThresholdingParams::Orientation_MostIntense },
    { "1 2 3 4 5", "10 20 30 20 10", "", "", ThresholdingParams::ThresholdingBy_FractionOfTotalIntensity, 0.34, ThresholdingParams::Orientation_MostIntense },

    // relative thresholding to total intensity, keeping the least intense peaks
    { "1 2 3 4 5", "10 20 30 20 10", "", "", ThresholdingParams::ThresholdingBy_FractionOfTotalIntensity, 0.1, ThresholdingParams::Orientation_LeastIntense },
    { "1 2 3 4 5", "10 20 30 20 10", "1 5", "10 10", ThresholdingParams::ThresholdingBy_FractionOfTotalIntensity, 0.12, ThresholdingParams::Orientation_LeastIntense },
    { "1 2 3 4 5", "10 20 30 20 10", "1 5", "10 10", ThresholdingParams::ThresholdingBy_FractionOfTotalIntensity, 0.21, ThresholdingParams::Orientation_LeastIntense },
    { "1 2 3 4 5", "10 20 30 20 10", "1 2 4 5", "10 20 20 10", ThresholdingParams::ThresholdingBy_FractionOfTotalIntensity, 0.23, ThresholdingParams::Orientation_LeastIntense },
    { "1 2 3 4 5", "10 20 30 20 10", "1 2 3 4 5", "10 20 30 20 10", ThresholdingParams::ThresholdingBy_FractionOfTotalIntensity, 0.34, ThresholdingParams::Orientation_LeastIntense },

    // threshold against cumulative total intensity fraction, keeping the most intense peaks
    { "1 2 3 4 5", "10 20 30 20 10", "", "", ThresholdingParams::ThresholdingBy_FractionOfTotalIntensityCutoff, 0.32, ThresholdingParams::Orientation_MostIntense },
    { "1 2 3 4 5", "10 20 30 20 10", "3", "30", ThresholdingParams::ThresholdingBy_FractionOfTotalIntensityCutoff, 0.34, ThresholdingParams::Orientation_MostIntense },
    { "1 2 3 4 5", "10 20 30 20 10", "3", "30", ThresholdingParams::ThresholdingBy_FractionOfTotalIntensityCutoff, 0.76, ThresholdingParams::Orientation_MostIntense },
    { "1 2 3 4 5", "10 20 30 20 10", "2 3 4", "20 30 20", ThresholdingParams::ThresholdingBy_FractionOfTotalIntensityCutoff, 0.78, ThresholdingParams::Orientation_MostIntense },
    { "1 2 3 4 5", "10 20 30 20 10", "1 2 3 4 5", "10 20 30 20 10", ThresholdingParams::ThresholdingBy_FractionOfTotalIntensityCutoff, 1.0, ThresholdingParams::Orientation_MostIntense },

    // threshold against cumulative total intensity fraction, keeping the least intense peaks
    { "1 2 3 4 5", "10 20 30 20 10", "", "", ThresholdingParams::ThresholdingBy_FractionOfTotalIntensityCutoff, 0.21, ThresholdingParams::Orientation_LeastIntense },
    { "1 2 3 4 5", "10 20 30 20 10", "1 5", "10 10", ThresholdingParams::ThresholdingBy_FractionOfTotalIntensityCutoff, 0.23, ThresholdingParams::Orientation_LeastIntense },
    { "1 2 3 4 5", "10 20 30 20 10", "1 5", "10 10", ThresholdingParams::ThresholdingBy_FractionOfTotalIntensityCutoff, 0.65, ThresholdingParams::Orientation_LeastIntense },
    { "1 2 3 4 5", "10 20 30 20 10", "1 2 4 5", "10 20 20 10", ThresholdingParams::ThresholdingBy_FractionOfTotalIntensityCutoff, 0.67, ThresholdingParams::Orientation_LeastIntense },
    { "1 2 3 4 5", "10 20 30 20 10", "1 2 3 4 5", "10 20 30 20 10", ThresholdingParams::ThresholdingBy_FractionOfTotalIntensityCutoff, 1.0, ThresholdingParams::Orientation_LeastIntense },

    // keep the <threshold> most intense points, excluding ties
    { "1 2 3 4 5", "10 20 30 20 10", "3", "30", ThresholdingParams::ThresholdingBy_Count, 1, ThresholdingParams::Orientation_MostIntense },
    { "1 2 3 4 5", "10 20 30 20 10", "3", "30", ThresholdingParams::ThresholdingBy_Count, 2, ThresholdingParams::Orientation_MostIntense },
    { "1 2 3 4 5", "10 20 30 20 10", "2 3 4", "20 30 20", ThresholdingParams::ThresholdingBy_Count, 3, ThresholdingParams::Orientation_MostIntense },
    { "1 2 3 4 5", "10 20 30 20 10", "2 3 4", "20 30 20", ThresholdingParams::ThresholdingBy_Count, 4, ThresholdingParams::Orientation_MostIntense },

    // keep the <threshold> least intense points, excluding ties
    { "1 2 3 4 5", "10 20 30 20 10", "", "", ThresholdingParams::ThresholdingBy_Count, 1, ThresholdingParams::Orientation_LeastIntense },
    { "1 2 3 4 5", "10 20 30 20 10", "1 5", "10 10", ThresholdingParams::ThresholdingBy_Count, 2, ThresholdingParams::Orientation_LeastIntense },
    { "1 2 3 4 5", "10 20 30 20 10", "1 5", "10 10", ThresholdingParams::ThresholdingBy_Count, 3, ThresholdingParams::Orientation_LeastIntense },
    { "1 2 3 4 5", "10 20 30 20 10", "1 2 4 5", "10 20 20 10", ThresholdingParams::ThresholdingBy_Count, 4, ThresholdingParams::Orientation_LeastIntense },

    // keep the <threshold> most intense points, including ties
    { "1 2 3 4 5", "10 20 30 20 10", "3", "30", ThresholdingParams::ThresholdingBy_CountAfterTies, 1, ThresholdingParams::Orientation_MostIntense },
    { "1 2 3 4 5", "10 20 30 20 10", "2 3 4", "20 30 20", ThresholdingParams::ThresholdingBy_CountAfterTies, 2, ThresholdingParams::Orientation_MostIntense },
    { "1 2 3 4 5", "10 20 30 20 10", "2 3 4", "20 30 20", ThresholdingParams::ThresholdingBy_CountAfterTies, 3, ThresholdingParams::Orientation_MostIntense },
    { "1 2 3 4 5", "10 20 30 20 10", "1 2 3 4 5", "10 20 30 20 10", ThresholdingParams::ThresholdingBy_CountAfterTies, 4, ThresholdingParams::Orientation_MostIntense },

    // keep the <threshold> least intense points, including ties
    { "1 2 3 4 5", "10 20 30 20 10", "1 5", "10 10", ThresholdingParams::ThresholdingBy_CountAfterTies, 1, ThresholdingParams::Orientation_LeastIntense },
    { "1 2 3 4 5", "10 20 30 20 10", "1 5", "10 10", ThresholdingParams::ThresholdingBy_CountAfterTies, 2, ThresholdingParams::Orientation_LeastIntense },
    { "1 2 3 4 5", "10 20 30 20 10", "1 2 4 5", "10 20 20 10", ThresholdingParams::ThresholdingBy_CountAfterTies, 3, ThresholdingParams::Orientation_LeastIntense },
    { "1 2 3 4 5", "10 20 30 20 10", "1 2 4 5", "10 20 20 10", ThresholdingParams::ThresholdingBy_CountAfterTies, 4, ThresholdingParams::Orientation_LeastIntense }
};

const size_t testThresholdersSize = sizeof(testThresholders) / sizeof(TestThresholder);

void testIntensityThresholding()
{
    for (size_t i=0; i < testThresholdersSize; ++i)
    {

        SpectrumListSimple* sl = new SpectrumListSimple;
        SpectrumListPtr originalList(sl);
        SpectrumPtr s(new Spectrum);
        sl->spectra.push_back(s);

        TestThresholder& t = testThresholders[i];

        vector<double> inputMZArray = parseDoubleArray(t.inputMZArray);
        vector<double> inputIntensityArray = parseDoubleArray(t.inputIntensityArray);
        s->setMZIntensityArrays(inputMZArray, inputIntensityArray, MS_number_of_counts);

        ThresholdingFilter* pFilter = new ThresholdingFilter(ThresholdingParams(t.byType, t.threshold, t.orientation));
        SpectrumListPtr thresholder(
            new SpectrumList_PeakFilter(originalList, pFilter));

        vector<double> outputMZArray = parseDoubleArray(t.outputMZArray);
        vector<double> outputIntensityArray = parseDoubleArray(t.outputIntensityArray);

        SpectrumPtr thresholdedSpectrum = thresholder->spectrum(0, true);
        //if (os_) cout << s1->defaultArrayLength << ": " << s1->getMZArray()->data << " " << s1->getIntensityArray()->data << endl;
        unit_assert(thresholdedSpectrum->defaultArrayLength == outputMZArray.size());
        for (size_t i=0; i < outputMZArray.size(); ++i)
        {
            unit_assert(thresholdedSpectrum->getMZArray()->data[i] == outputMZArray[i]);
            unit_assert(thresholdedSpectrum->getIntensityArray()->data[i] == outputIntensityArray[i]);
        }
    }
}

void test()
{
    testIntensityThresholding();
    testMassRemoval();
}

int main(int argc, char* argv[])
{
    try
    {
        if (argc>1 && !strcmp(argv[1],"-v")) os_ = &cout;
        test();
        return 0;
    }
    catch (exception& e)
    {
        cerr << e.what() << endl;
    }
    
    return 1;
}
