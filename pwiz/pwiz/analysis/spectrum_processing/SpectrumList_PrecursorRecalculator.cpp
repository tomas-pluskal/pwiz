//
// SpectrumList_PrecursorRecalculator.cpp
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


#define PWIZ_SOURCE

#include "SpectrumList_PrecursorRecalculator.hpp"
#include "PrecursorRecalculatorDefault.hpp"
#include "analysis/peakdetect/PeakFamilyDetectorFT.hpp"
#include "analysis/passive/MSDataCache.hpp"
#include <stdexcept>
#include <iostream>


namespace pwiz {
namespace analysis {


using namespace pwiz::msdata;
using namespace pwiz::data;
using namespace std;
using boost::shared_ptr;
using boost::lexical_cast;


//
// SpectrumList_PrecursorRecalculator::Impl
//


struct SpectrumList_PrecursorRecalculator::Impl
{
    shared_ptr<PrecursorRecalculator> precursorRecalculator;  
    MSDataCache cache;
    CVID targetMassAnalyzerType;

    Impl(const MSData& msd);
};


namespace {
shared_ptr<PrecursorRecalculatorDefault> createPrecursorRecalculator_msprefix()
{
    // instantiate PeakFamilyDetector

    PeakFamilyDetectorFT::Config pfdftConfig;
    pfdftConfig.cp = CalibrationParameters::thermo();
    shared_ptr<PeakFamilyDetector> pfd(new PeakFamilyDetectorFT(pfdftConfig));

    // instantiate PrecursorRecalculatorDefault

    PrecursorRecalculatorDefault::Config config;
    config.peakFamilyDetector = pfd;
    config.mzLeftWidth = 3;
    config.mzRightWidth = 1.6;
    return shared_ptr<PrecursorRecalculatorDefault>(new PrecursorRecalculatorDefault(config));
}
} // namespace


SpectrumList_PrecursorRecalculator::Impl::Impl(const MSData& msd)
:   precursorRecalculator(createPrecursorRecalculator_msprefix()),
    targetMassAnalyzerType(CVID_Unknown)
{
    cache.open(msd);

    // choose highest-accuracy mass analyzer for targetMassAnalyzerType

    for (vector<InstrumentConfigurationPtr>::const_iterator it=msd.instrumentConfigurationPtrs.begin(),
         end=msd.instrumentConfigurationPtrs.end(); it!=end; ++it)
    {
        if (!it->get()) continue;
        const InstrumentConfiguration& ic = **it;

        if (targetMassAnalyzerType!=MS_FT_ICR &&
            targetMassAnalyzerType!=MS_orbitrap)
            targetMassAnalyzerType = ic.componentList.analyzer(0).cvParamChild(MS_mass_analyzer_type).cvid;
    }

    if (targetMassAnalyzerType!=MS_FT_ICR && targetMassAnalyzerType!=MS_orbitrap)
        throw runtime_error(("[SpectrumList_PrecursorRecalculator] Mass analyzer not supported: " +
                            cvinfo(targetMassAnalyzerType).name).c_str());
}


//
// SpectrumList_PrecursorRecalculator
//


PWIZ_API_DECL SpectrumList_PrecursorRecalculator::SpectrumList_PrecursorRecalculator(
    const MSData& msd)
:   SpectrumListWrapper(msd.run.spectrumListPtr), impl_(new Impl(msd))
{}


namespace{

PrecursorRecalculator::PrecursorInfo getInitialEstimate(const Spectrum& spectrum)
{
    PrecursorRecalculator::PrecursorInfo result;
    if (spectrum.spectrumDescription.precursors.empty()) return result;

    const Precursor& precursor = spectrum.spectrumDescription.precursors[0];
    if (precursor.selectedIons.empty()) return result;

    const SelectedIon& selectedIon = precursor.selectedIons[0];
    result.mz = selectedIon.cvParam(MS_m_z).valueAs<double>();
    result.charge = selectedIon.cvParam(MS_charge_state).valueAs<int>();
    return result;
}


void encodePrecursorInfo(Spectrum& spectrum, 
                         vector<PrecursorRecalculator::PrecursorInfo> precursorInfos)
{
    if (spectrum.spectrumDescription.precursors.empty()) return;

    Precursor& precursor = spectrum.spectrumDescription.precursors[0];
    precursor.selectedIons.clear();

    for (vector<PrecursorRecalculator::PrecursorInfo>::const_iterator it=precursorInfos.begin(), 
         end=precursorInfos.end(); it!=end; ++it)
    {
        precursor.selectedIons.push_back(SelectedIon()); 
        SelectedIon& selectedIon = precursor.selectedIons.back();
        selectedIon.set(MS_m_z, it->mz);
        selectedIon.set(MS_intensity, it->intensity);
        selectedIon.set(MS_charge_state, it->charge);
        selectedIon.userParams.push_back(UserParam("msprefix score", 
                                                   lexical_cast<string>(it->score), 
                                                   "xsd:float")); 
    } 
}


} // namespace


PWIZ_API_DECL SpectrumPtr SpectrumList_PrecursorRecalculator::spectrum(size_t index, bool getBinaryData) const
{
    SpectrumPtr originalSpectrum = inner_->spectrum(index, getBinaryData);  
    
    // find parent spectrum in cache

    size_t parentIndex = index;

    while (1) 
    {
        if (parentIndex-- == 0)
            return originalSpectrum;

        const SpectrumInfo& info = impl_->cache.spectrumInfo(parentIndex);

        if (info.msLevel==1 && info.massAnalyzerType==impl_->targetMassAnalyzerType)
            break;
    }

    const SpectrumInfo& parent = impl_->cache.spectrumInfo(parentIndex, true);
    if (parent.data.empty())
        return originalSpectrum;

    // run precursorRecalculator

    PrecursorRecalculator::PrecursorInfo initialEstimate = getInitialEstimate(*originalSpectrum);
    if (initialEstimate.mz == 0) 
        return originalSpectrum;

    vector<PrecursorRecalculator::PrecursorInfo> result;
    impl_->precursorRecalculator->recalculate(&parent.data[0], &parent.data[0]+parent.data.size(),
                                              initialEstimate, result);

    // encode result in Spectrum 

    SpectrumPtr newSpectrum(new Spectrum(*originalSpectrum));
    encodePrecursorInfo(*newSpectrum, result);
    return newSpectrum;
}


} // namespace analysis
} // namespace pwiz

