//
// Serializer_MGF.cpp
//
//
// Original author: Matt Chambers <matt.chambers .@. vanderbilt.edu>
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

#include "Serializer_MGF.hpp"
#include "SpectrumList_MGF.hpp"
#include "pwiz/utility/misc/String.hpp"
#include "pwiz/utility/misc/Stream.hpp"
#include "boost/foreach.hpp"
#include "boost/algorithm/string/join.hpp"


namespace pwiz {
namespace msdata {


using namespace std;
using boost::shared_ptr;
using boost::lexical_cast;
using boost::iostreams::stream_offset;
using namespace pwiz::util;


class Serializer_MGF::Impl
{
    public:

    Impl()
    {}

    void write(ostream& os, const MSData& msd,
               const pwiz::util::IterationListenerRegistry* iterationListenerRegistry) const;

    void read(shared_ptr<istream> is, MSData& msd) const;
};


void Serializer_MGF::Impl::write(ostream& os, const MSData& msd,
    const pwiz::util::IterationListenerRegistry* iterationListenerRegistry) const
{
    SpectrumList& sl = *msd.run.spectrumListPtr;
    for (size_t i=0, end=sl.size(); i < end; ++i)
    {
        SpectrumPtr s = sl.spectrum(i, true);

        if (s->cvParam(MS_ms_level).valueAs<int>() > 1)
        {
            os << "BEGIN IONS\n";
            os << "TITLE=" << s->nativeID << '\n';

            CVParam scanTimeParam = s->spectrumDescription.cvParam(MS_scan_time);
            if (!scanTimeParam.empty())
                os << "RTINSECONDS=" << scanTimeParam.valueAs<double>() << '\n';

            if (!s->spectrumDescription.precursors.empty() &&
                !s->spectrumDescription.precursors[0].selectedIons.empty())
            {
                const SelectedIon& si = s->spectrumDescription.precursors[0].selectedIons[0];
                os << "PEPMASS=" << si.cvParam(MS_m_z).valueAs<double>() << '\n';

                CVParam chargeParam = si.cvParam(MS_charge_state);
                if (chargeParam.empty())
                {
                    BOOST_FOREACH(const CVParam& param, si.cvParams)
                    {
                        vector<string> charges;
                        if (param.cvid == MS_possible_charge_state)
                            charges.push_back(param.value);
                        if (!charges.empty())
                            os << "CHARGE=" << bal::join(charges, " and ") << '\n';
                    }
                } else
                    os << "CHARGE=" << chargeParam.value << '\n';
            }

            const BinaryDataArray& mzArray = *s->getMZArray();
            const BinaryDataArray& intensityArray = *s->getIntensityArray();
            for (size_t p=0; p < s->defaultArrayLength; ++p)
                os << mzArray.data[p] << ' ' << intensityArray.data[p] << '\n';

            os << "END IONS\n";
        }

        // update any listeners and handle cancellation
        IterationListener::Status status = IterationListener::Status_Ok;

        if (iterationListenerRegistry)
            status = iterationListenerRegistry->broadcastUpdateMessage(
                IterationListener::UpdateMessage(i, end));

        if (status == IterationListener::Status_Cancel) 
            break;
    }
}


void Serializer_MGF::Impl::read(shared_ptr<istream> is, MSData& msd) const
{
    if (!is.get() || !*is)
        throw runtime_error("[Serializer_MGF::read()] Bad istream.");

    is->seekg(0);

    // we treat all MGF data is MSn (PMF MGFs not currently supported)
    msd.fileDescription.fileContent.set(MS_MSn_spectrum);
    msd.fileDescription.fileContent.set(MS_multiple_peak_list_nativeID_format);
    msd.run.spectrumListPtr = SpectrumList_MGF::create(is, msd);
    msd.run.chromatogramListPtr.reset(new ChromatogramListSimple);
}


//
// Serializer_MGF
//


PWIZ_API_DECL Serializer_MGF::Serializer_MGF()
:   impl_()
{}


PWIZ_API_DECL void Serializer_MGF::write(ostream& os, const MSData& msd,
    const pwiz::util::IterationListenerRegistry* iterationListenerRegistry) const
  
{
    return impl_->write(os, msd, iterationListenerRegistry);
}


PWIZ_API_DECL void Serializer_MGF::read(shared_ptr<istream> is, MSData& msd) const
{
    return impl_->read(is, msd);
}


} // namespace msdata
} // namespace pwiz


