//
// RawData.hpp
//
// 
// Original author: Matt Chambers <matt.chambers .@. vanderbilt.edu>
//
// Copyright 2009 Vanderbilt University - Nashville, TN 37232
//
// Licensed under Creative Commons 3.0 United States License, which requires:
//  - Attribution
//  - Noncommercial
//  - No Derivative Works
//
// http://creativecommons.org/licenses/by-nc-nd/3.0/us/
//
// Unless required by applicable law or agreed to in writing, software 
// distributed under the License is distributed on an "AS IS" BASIS, 
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
// See the License for the specific language governing permissions and 
// limitations under the License.
//


#ifndef _RAWDATA_HPP_
#define _RAWDATA_HPP_


#include <string>
#include <vector>
#include <boost/shared_ptr.hpp>
#include "pwiz/utility/misc/Export.hpp"
#include "pwiz/utility/misc/automation_vector.h"


namespace pwiz {
namespace vendor_api {
namespace Waters {


PWIZ_API_DECL enum FunctionType
{
    FunctionType_Unknown,
    FunctionType_Survey,
    FunctionType_Scan,
    FunctionType_MALDI_TOF,
    FunctionType_MS,
    FunctionType_MSMS,
    FunctionType_MSMSMS,
    FunctionType_MRM,
    FunctionType_Daughter
};


PWIZ_API_DECL enum InstrumentType
{
    InstrumentType_Xevo
};


struct PWIZ_API_DECL Precursor
{
    double mz;
    bool hasAccurateMass;
    double collisionEnergy;
    int collisionRF;
};

typedef boost::shared_ptr<Precursor> PrecursorPtr;


struct PWIZ_API_DECL Scan
{
    virtual const int getFunctionNumber() const = 0;
    virtual const int getProcessNumber() const = 0;
    virtual const int getScanNumber() const = 0;

    virtual bool getDataIsContinuous() const = 0;
    virtual const size_t getNumPoints() const = 0;
    virtual const automation_vector<float>& masses() const = 0;
    virtual const automation_vector<float>& intensities() const = 0;

    virtual const PrecursorPtr getPrecursorInfo() const = 0;

    // ???
    /*short GetLinearDetectorVoltage() const = 0;
    short GetLinearSensitivity() const = 0;
    short GetReflectronLensVoltage() const = 0;
    short GetReflectronDetectorVoltage() const = 0;
    short GetReflectronSensitivity() const = 0;*/

    // MALDI
    /*short GetLaserRepetitionRate() const = 0;
    short GetCoarseLaserControl() const = 0;
    short GetFineLaserControl() const = 0;
    float GetLaserAimXPos() const = 0;
    float GetLaserAimYPos() const = 0;
    short GetNumShotsSummed() const = 0;
    short GetNumShotsPerformed() const = 0;*/

    virtual double getStartTime() const = 0;

    virtual double getTIC() const = 0;
    virtual double getBasePeakMZ() const = 0;
    virtual double getBasePeakIntensity() const = 0;
    virtual double getMinMZ() const = 0;
    virtual double getMaxMZ() const = 0;
};

typedef boost::shared_ptr<Scan> ScanPtr;


struct PWIZ_API_DECL SRMTarget { double Q1, Q3; };


struct PWIZ_API_DECL Function
{
    virtual int getFunctionNumber() const = 0;

    virtual FunctionType getFunctionType() const = 0;

    virtual size_t getScanCount() const = 0;
    virtual ScanPtr getScan(int process, int scan) const = 0;

    virtual double getSetMass() const = 0;

    //long GetNumSegments() const = 0;
    //_variant_t GetSIRChannels() const = 0;

    virtual size_t getSRMSize() const = 0;
    virtual void getSRM(size_t index, SRMTarget& target) const = 0;
    virtual void getSIC(size_t index, automation_vector<float>& times, automation_vector<float>& intensities) const = 0;

    virtual double getStartTime() const = 0;
    virtual double getEndTime() const = 0;

    virtual void getTIC(automation_vector<float>& times, automation_vector<float>& intensities) const = 0;
};

typedef boost::shared_ptr<Function> FunctionPtr;
typedef std::vector<FunctionPtr> FunctionList;


class PWIZ_API_DECL RawData
{
    public:
    typedef boost::shared_ptr<RawData> Ptr;
    static Ptr create(const std::string& rawpath);

    virtual int getVersionMajor() const = 0;
    virtual int getVersionMinor() const = 0;

    virtual std::string getAcquisitionName() const = 0;
    virtual std::string getAcquisitionDate() const = 0;
    virtual std::string getAcquisitionTime() const = 0;
    virtual InstrumentType getInstrument() const = 0;

    /*_bstr_t GetJobCode() const = 0;
    _bstr_t GetTaskCode() const = 0;
    _bstr_t GetUserName() const = 0;
    _bstr_t GetLabName() const = 0;
    _bstr_t GetConditions() const = 0;
    _bstr_t GetSampleDesc() const = 0;
    _bstr_t GetSubmitter() const = 0;
    _bstr_t GetSampleID() const = 0;
    _bstr_t GetBottleNumber() const = 0;
    double GetSolventDelay() const = 0;
    long GetResolved() const = 0;
    _bstr_t GetPepFileName() const = 0;
    _bstr_t GetProcess() const = 0;
    long GetEncrypted() const = 0;
    long GetAutosamplerType() const = 0;
    _bstr_t GetGasName() const = 0;
    _bstr_t GetInstrumentType() const = 0;
    _bstr_t GetPlateDesc() const = 0;
    _variant_t GetAnalogOffset() const = 0;
    long GetMuxStream() const = 0;
    long GetReinjections() const = 0;
    _variant_t GetPICMRMFunctions() const = 0;
    _variant_t GetPICScanFunctions() const = 0;
    long GetPICFunctions() const = 0;*/

    /// returns an array of FunctionPtrs, but each function is instantiated on-demand;
    /// some of the FunctionPtrs may be null (if the corresponding _FUNC0xx.DAT is missing)
    virtual const FunctionList& functions() const = 0;

    //virtual ScanPtr getScan(int function, int process, int scan) const = 0;
};

typedef RawData::Ptr RawDataPtr;


} // namespace Waters
} // namespace vendor_api
} // namespace pwiz


#endif // _RAWDATA_HPP_
