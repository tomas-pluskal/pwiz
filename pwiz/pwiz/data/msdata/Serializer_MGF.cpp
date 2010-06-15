//
// $Id$
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
#include "pwiz/utility/misc/Filesystem.hpp"
#include "pwiz/utility/misc/Std.hpp"


namespace pwiz {
namespace msdata {


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
    bool titleIsThermoDTA = msd.fileDescription.fileContent.hasCVParam(MS_Thermo_nativeID_format);
    const string& thermoFilename = titleIsThermoDTA ? msd.fileDescription.sourceFilePtrs[0]->name : "";
    string thermoBasename = titleIsThermoDTA ? bfs::basename(thermoFilename) : "";

    os << std::setprecision(10); // 1234.567890
    SpectrumList& sl = *msd.run.spectrumListPtr;
    for (size_t i=0, end=sl.size(); i < end; ++i)
    {
        SpectrumPtr s = sl.spectrum(i, true);
        Scan* scan = !s->scanList.empty() ? &s->scanList.scans[0] : 0;

        if (s->cvParam(MS_ms_level).valueAs<int>() > 1 &&
            !s->precursors.empty() &&
            !s->precursors[0].selectedIons.empty())
        {
            os << "BEGIN IONS\n";

            const SelectedIon& si = s->precursors[0].selectedIons[0];
            CVParam scanTimeParam = scan ? scan->cvParam(MS_scan_start_time) : CVParam();
            CVParam chargeParam = si.cvParam(MS_charge_state);

            if (titleIsThermoDTA)
            {
                string scan = id::value(s->id, "scan");
                os << "TITLE=" << thermoBasename << '.' << scan << '.' << scan << '.' << chargeParam.value << '\n';
            }
            else
                os << "TITLE=" << s->id << '\n';

            if (!scanTimeParam.empty())
                os << "RTINSECONDS=" << scanTimeParam.timeInSeconds() << '\n';

            os << "PEPMASS=" << si.cvParam(MS_selected_ion_m_z).value;
            
            CVParam intensityParam = si.cvParam(MS_peak_intensity);
            if (!intensityParam.empty())
                os << " " << intensityParam.value;
            os << '\n';

            if (chargeParam.empty())
            {
                vector<string> charges;
                BOOST_FOREACH(const CVParam& param, si.cvParams)
                {
                    if (param.cvid == MS_possible_charge_state)
                        charges.push_back(param.value);
                }
                if (!charges.empty())
                    os << "CHARGE=" << bal::join(charges, " and ") << '\n';
            } else
                os << "CHARGE=" << chargeParam.value << '\n';

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

    // read mzML file-level metadata stored in comment tags by the MGF writer like:
    // # fileContent CVParam MS:12345678 (term name)
    // # sourceFile id=foo name=bar location=file:///foo/bar
    /*string lineStr;
    while (is->peek() != (int) 'B')
    {
        getline(*is_, lineStr);
        if (lineStr[0] == '#')
        {
            vector<string> tokens;
            bal::split(tokens, lineStr, bal::is_space());
            if (tokens[1] == "fileContent")
                addParamToContainer(msd.fileDescription.fileContent, */

    // we treat all MGF data is MSn (PMF MGFs not currently supported)
    msd.fileDescription.fileContent.set(MS_MSn_spectrum);
    msd.fileDescription.fileContent.set(MS_centroid_spectrum);
    msd.run.spectrumListPtr = SpectrumList_MGF::create(is, msd);
    msd.run.chromatogramListPtr.reset(new ChromatogramListSimple);
}


//
// Serializer_MGF
//


PWIZ_API_DECL Serializer_MGF::Serializer_MGF()
:   impl_(new Impl())
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


