//
// MSData.cpp
//
//
// Original author: Matt Chambers <matt.chambers .@. vanderbilt.edu>
//
// Copyright 2008 Spielberg Family Center for Applied Proteomics
//   Cedars Sinai Medical Center, Los Angeles, California  90048
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

#include "MSData.hpp"
//#include "../../../data/msdata/MSData.hpp"


using System::Exception;
using System::String;
using boost::shared_ptr;


namespace b = pwiz::msdata;


namespace pwiz {
namespace CLI {


CV::CV()
: base_(new pwiz::CV()), owner_(nullptr)
{}

System::String^ CV::id::get() {return gcnew System::String(base_->id.c_str());}
void CV::id::set(System::String^ value) {base_->id = ToStdString(value);}

System::String^ CV::URI::get() {return gcnew System::String(base_->URI.c_str());}
void CV::URI::set(System::String^ value) {base_->URI = ToStdString(value);}

System::String^ CV::fullName::get() {return gcnew System::String(base_->fullName.c_str());}
void CV::fullName::set(System::String^ value) {base_->fullName = ToStdString(value);}

System::String^ CV::version::get() {return gcnew System::String(base_->version.c_str());}
void CV::version::set(System::String^ value) {base_->version = ToStdString(value);}

bool CV::empty()
{
    return base_->empty();
}


namespace msdata {


UserParam::UserParam()
: base_(new boost::shared_ptr<b::UserParam>(new b::UserParam())), owner_(nullptr)
{value_ = gcnew UserParamValue(base_);}

UserParam::UserParam(System::String^ _name)
: base_(new boost::shared_ptr<b::UserParam>(new b::UserParam(ToStdString(_name)))), owner_(nullptr)
{value_ = gcnew UserParamValue(base_);}

UserParam::UserParam(System::String^ _name, System::String^ _value)
: base_(new boost::shared_ptr<b::UserParam>(new b::UserParam(ToStdString(_name), ToStdString(_value)))), owner_(nullptr)
{value_ = gcnew UserParamValue(base_);}

UserParam::UserParam(System::String^ _name, System::String^ _value, System::String^ _type)
: base_(new boost::shared_ptr<b::UserParam>(new b::UserParam(ToStdString(_name), ToStdString(_value), ToStdString(_type)))), owner_(nullptr)
{value_ = gcnew UserParamValue(base_);}

UserParam::UserParam(System::String^ _name, System::String^ _value, System::String^ _type, CVID _units)
: base_(new boost::shared_ptr<b::UserParam>(new b::UserParam(ToStdString(_name), ToStdString(_value), ToStdString(_type), (pwiz::CVID) _units))), owner_(nullptr)
{value_ = gcnew UserParamValue(base_);}

System::String^ UserParam::name::get() {return gcnew System::String((*base_)->name.c_str());}
void UserParam::name::set(System::String^ value) {(*base_)->name = ToStdString(value);}

System::String^ UserParam::type::get() {return gcnew System::String((*base_)->type.c_str());}
void UserParam::type::set(System::String^ value) {(*base_)->type = ToStdString(value);}

CVID UserParam::units::get() {return (CVID) (*base_)->units;}
void UserParam::units::set(CVID value) {(*base_)->units = (pwiz::CVID) value;}

UserParamValue^ UserParam::value::get() {return value_;}

bool UserParam::empty()
{
    return (*base_)->empty();
}

bool UserParam::operator==(UserParam^ that) {return (*base_) == *that->base_;}
bool UserParam::operator!=(UserParam^ that) {return (*base_) != *that->base_;}


ParamGroupList^ ParamContainer::paramGroups::get() {return gcnew ParamGroupList(&base_->paramGroupPtrs, this);}
CVParamList^ ParamContainer::cvParams::get() {return gcnew CVParamList(&base_->cvParams, this);}
UserParamList^ ParamContainer::userParams::get() {return gcnew UserParamList(&base_->userParams, this);}

CVParam^ ParamContainer::cvParam(CVID cvid)
{
    return gcnew CVParam(new pwiz::msdata::CVParam(base_->cvParam((pwiz::CVID) cvid)));
}

CVParam^ ParamContainer::cvParamChild(CVID cvid)
{
    return gcnew CVParam(new pwiz::msdata::CVParam(base_->cvParamChild((pwiz::CVID) cvid)));
}

bool ParamContainer::hasCVParam(CVID cvid)
{
    return base_->hasCVParam((pwiz::CVID) cvid);
}

bool ParamContainer::hasCVParamChild(CVID cvid)
{
    return base_->hasCVParamChild((pwiz::CVID) cvid);
}

UserParam^ ParamContainer::userParam(System::String^ name)
{
    return gcnew UserParam(new pwiz::msdata::UserParam(base_->userParam(ToStdString(name))));
}

bool ParamContainer::empty()
{
    return base_->empty();
}

void ParamContainer::set(CVID cvid) {base_->set((pwiz::CVID) cvid);}
void ParamContainer::set(CVID cvid, System::String^ value) {base_->set((pwiz::CVID) cvid, ToStdString(value));}
void ParamContainer::set(CVID cvid, System::String^ value, CVID units) {base_->set((pwiz::CVID) cvid, ToStdString(value), (pwiz::CVID) units);}

void ParamContainer::set(CVID cvid, bool value) {set(cvid, (value ? "true" : "false"));}
void ParamContainer::set(CVID cvid, System::Int32 value) {set(cvid, value.ToString());}
void ParamContainer::set(CVID cvid, System::Int64 value) {set(cvid, value.ToString());}
void ParamContainer::set(CVID cvid, System::UInt32 value) {set(cvid, value.ToString());}
void ParamContainer::set(CVID cvid, System::UInt64 value) {set(cvid, value.ToString());}
void ParamContainer::set(CVID cvid, System::Single value) {set(cvid, value.ToString());}
void ParamContainer::set(CVID cvid, System::Double value) {set(cvid, value.ToString());}

void ParamContainer::set(CVID cvid, System::Int32 value, CVID units) {set(cvid, value.ToString(), units);}
void ParamContainer::set(CVID cvid, System::Int64 value, CVID units) {set(cvid, value.ToString(), units);}
void ParamContainer::set(CVID cvid, System::UInt32 value, CVID units) {set(cvid, value.ToString(), units);}
void ParamContainer::set(CVID cvid, System::UInt64 value, CVID units) {set(cvid, value.ToString(), units);}
void ParamContainer::set(CVID cvid, System::Single value, CVID units) {set(cvid, value.ToString(), units);}
void ParamContainer::set(CVID cvid, System::Double value, CVID units) {set(cvid, value.ToString(), units);}


ParamGroup::ParamGroup()
: ParamContainer(new b::ParamGroup())
{base_ = new boost::shared_ptr<b::ParamGroup>(static_cast<b::ParamGroup*>(ParamContainer::base_));}

ParamGroup::ParamGroup(System::String^ _id)
: ParamContainer(new b::ParamGroup(ToStdString(_id)))
{base_ = new boost::shared_ptr<b::ParamGroup>(static_cast<b::ParamGroup*>(ParamContainer::base_));}

System::String^ ParamGroup::id::get() {return gcnew System::String((*base_)->id.c_str());}
void ParamGroup::id::set(System::String^ value) {(*base_)->id = ToStdString(value);}

bool ParamGroup::empty()
{
    return (*base_)->empty();
}


FileContent::FileContent()
: ParamContainer(new b::FileContent())
{owner_ = nullptr; base_ = static_cast<b::FileContent*>(ParamContainer::base_);}


SourceFile::SourceFile()
: ParamContainer(new b::SourceFile())
{base_ = new boost::shared_ptr<b::SourceFile>(static_cast<b::SourceFile*>(ParamContainer::base_));}

SourceFile::SourceFile(System::String^ _id)
: ParamContainer(new b::SourceFile(ToStdString(_id)))
{base_ = new boost::shared_ptr<b::SourceFile>(static_cast<b::SourceFile*>(ParamContainer::base_));}

SourceFile::SourceFile(System::String^ _id, System::String^ _name)
: ParamContainer(new b::SourceFile(ToStdString(_id), ToStdString(_name)))
{base_ = new boost::shared_ptr<b::SourceFile>(static_cast<b::SourceFile*>(ParamContainer::base_));}

SourceFile::SourceFile(System::String^ _id, System::String^ _name, System::String^ _location)
: ParamContainer(new b::SourceFile(ToStdString(_id), ToStdString(_name), ToStdString(_location)))
{base_ = new boost::shared_ptr<b::SourceFile>(static_cast<b::SourceFile*>(ParamContainer::base_));}

System::String^ SourceFile::id::get() {return gcnew System::String((*base_)->id.c_str());}
void SourceFile::id::set(System::String^ value) {(*base_)->id = ToStdString(value);}

System::String^ SourceFile::name::get() {return gcnew System::String((*base_)->name.c_str());}
void SourceFile::name::set(System::String^ value) {(*base_)->name = ToStdString(value);}

System::String^ SourceFile::location::get() {return gcnew System::String((*base_)->location.c_str());}
void SourceFile::location::set(System::String^ value) {(*base_)->location = ToStdString(value);}

bool SourceFile::empty()
{
    return (*base_)->empty();
}


Contact::Contact()
: ParamContainer(new b::Contact())
{owner_ = nullptr; base_ = static_cast<b::Contact*>(ParamContainer::base_);}


FileDescription::FileDescription()
: base_(new b::FileDescription())
{owner_ = nullptr;}

FileContent^ FileDescription::fileContent::get() {return gcnew FileContent(&base_->fileContent, this);}
SourceFileList^ FileDescription::sourceFiles::get() {return gcnew SourceFileList(&base_->sourceFilePtrs, this);}
ContactList^ FileDescription::contacts::get() {return gcnew ContactList(&base_->contacts, this);}

bool FileDescription::empty()
{
    return base_->empty();
}


Sample::Sample()
: ParamContainer(new b::Sample())
{base_ = new boost::shared_ptr<b::Sample>(static_cast<b::Sample*>(ParamContainer::base_));}

Sample::Sample(System::String^ _id)
: ParamContainer(new b::Sample(ToStdString(_id)))
{base_ = new boost::shared_ptr<b::Sample>(static_cast<b::Sample*>(ParamContainer::base_));}

Sample::Sample(System::String^ _id, System::String^ _name)
: ParamContainer(new b::Sample(ToStdString(_id), ToStdString(_name)))
{base_ = new boost::shared_ptr<b::Sample>(static_cast<b::Sample*>(ParamContainer::base_));}

System::String^ Sample::id::get() {return gcnew System::String((*base_)->id.c_str());}
void Sample::id::set(System::String^ value) {(*base_)->id = ToStdString(value);}

System::String^ Sample::name::get() {return gcnew System::String((*base_)->name.c_str());}
void Sample::name::set(System::String^ value) {(*base_)->name = ToStdString(value);}

bool Sample::empty()
{
    return (*base_)->empty();
}


Component::Component()
: ParamContainer(new b::Component())
{owner_ = nullptr; base_ = static_cast<b::Component*>(ParamContainer::base_);}

Component::Component(ComponentType type, int order)
: ParamContainer(new b::Component((b::ComponentType) type, order))
{owner_ = nullptr; base_ = static_cast<b::Component*>(ParamContainer::base_);}

Component::Component(CVID cvid, int order)
: ParamContainer(new b::Component((pwiz::CVID) cvid, order))
{owner_ = nullptr; base_ = static_cast<b::Component*>(ParamContainer::base_);}

ComponentType Component::type::get() {return (ComponentType) base_->type;}
void Component::type::set(ComponentType value) {base_->type = (pwiz::msdata::ComponentType) value;}

int Component::order::get() {return base_->order;}
void Component::order::set(int value) {base_->order = value;}

void Component::define(CVID cvid, int order)
{
    base_->define((pwiz::CVID) cvid, order);
}

bool Component::empty()
{
    return base_->empty();
}


ComponentList::ComponentList()
: ComponentBaseList(new b::ComponentList())
{owner_ = nullptr; base_ = static_cast<b::ComponentList*>(ComponentBaseList::base_);}

Component^ ComponentList::source(int index)
{
    return gcnew Component(&base_->source((size_t) index), this);
}

Component^ ComponentList::analyzer(int index)
{
    return gcnew Component(&base_->analyzer((size_t) index), this);
}

Component^ ComponentList::detector(int index)
{
    return gcnew Component(&base_->detector((size_t) index), this);
}


Software::Software()
: base_(new boost::shared_ptr<b::Software>(new b::Software()))
{}

Software::Software(System::String^ _id)
: base_(new boost::shared_ptr<b::Software>(new b::Software(ToStdString(_id))))
{}

Software::Software(System::String^ _id, CVParam^ _softwareParam, System::String^ _softwareParamVersion)
: base_(new boost::shared_ptr<b::Software>(new b::Software(ToStdString(_id), **_softwareParam->base_, ToStdString(_softwareParamVersion))))
{}

System::String^ Software::id::get() {return gcnew System::String((*base_)->id.c_str());}
void Software::id::set(System::String^ value) {(*base_)->id = ToStdString(value);}

System::String^ Software::version::get() {return gcnew System::String((*base_)->version.c_str());}
void Software::version::set(System::String^ value) {(*base_)->version = ToStdString(value);}

bool Software::empty()
{
    return (*base_)->empty();
}


InstrumentConfiguration::InstrumentConfiguration()
: ParamContainer(new b::InstrumentConfiguration())
{base_ = new boost::shared_ptr<b::InstrumentConfiguration>(static_cast<b::InstrumentConfiguration*>(ParamContainer::base_));}

InstrumentConfiguration::InstrumentConfiguration(System::String^ _id)
: ParamContainer(new b::InstrumentConfiguration(ToStdString(_id)))
{base_ = new boost::shared_ptr<b::InstrumentConfiguration>(static_cast<b::InstrumentConfiguration*>(ParamContainer::base_));}

System::String^ InstrumentConfiguration::id::get() {return gcnew System::String((*base_)->id.c_str());}
void InstrumentConfiguration::id::set(System::String^ value) {(*base_)->id = ToStdString(value);}

ComponentList^ InstrumentConfiguration::componentList::get() {return gcnew ComponentList(&(*base_)->componentList, this);}
Software^ InstrumentConfiguration::software::get() {return NATIVE_SHARED_PTR_TO_CLI(pwiz::msdata::SoftwarePtr, Software, (*base_)->softwarePtr);}

bool InstrumentConfiguration::empty()
{
    return (*base_)->empty();
}


ProcessingMethod::ProcessingMethod()
: ParamContainer(new b::ProcessingMethod())
{owner_ = nullptr; base_ = static_cast<b::ProcessingMethod*>(ParamContainer::base_);}

int ProcessingMethod::order::get() {return base_->order;}
void ProcessingMethod::order::set(int value) {base_->order = value;}
Software^ ProcessingMethod::software::get() {return NATIVE_SHARED_PTR_TO_CLI(pwiz::msdata::SoftwarePtr, Software, base_->softwarePtr);}

bool ProcessingMethod::empty()
{
    return base_->empty();
}


DataProcessing::DataProcessing()
: base_(new boost::shared_ptr<b::DataProcessing>(new b::DataProcessing()))
{}

DataProcessing::DataProcessing(System::String^ _id)
: base_(new boost::shared_ptr<b::DataProcessing>(new b::DataProcessing(ToStdString(_id))))
{}

System::String^ DataProcessing::id::get() {return gcnew System::String((*base_)->id.c_str());}
void DataProcessing::id::set(System::String^ value) {(*base_)->id = ToStdString(value);}

ProcessingMethodList^ DataProcessing::processingMethods::get() {return gcnew ProcessingMethodList(&(*base_)->processingMethods, this);}

bool DataProcessing::empty()
{
    return (*base_)->empty();
}


Target::Target()
: ParamContainer(new b::Target())
{owner_ = nullptr; base_ = static_cast<b::Target*>(ParamContainer::base_);}


ScanSettings::ScanSettings()
: base_(new boost::shared_ptr<b::ScanSettings>(new b::ScanSettings()))
{}

ScanSettings::ScanSettings(System::String^ _id)
: base_(new boost::shared_ptr<b::ScanSettings>(new b::ScanSettings(ToStdString(_id))))
{}

System::String^ ScanSettings::id::get() {return gcnew System::String((*base_)->id.c_str());}
void ScanSettings::id::set(System::String^ value) {(*base_)->id = ToStdString(value);}

SourceFileList^ ScanSettings::sourceFiles::get() {return gcnew SourceFileList(&(*base_)->sourceFilePtrs, this);}

TargetList^ ScanSettings::targets::get() {return gcnew TargetList(&(*base_)->targets, this);}

bool ScanSettings::empty()
{
    return (*base_)->empty();
}


ScanWindowList^ Scan::scanWindows::get() {return gcnew ScanWindowList(&base_->scanWindows, this);}


ScanWindow::ScanWindow()
: ParamContainer(new b::ScanWindow())
{owner_ = nullptr; base_ = static_cast<b::ScanWindow*>(ParamContainer::base_);}

ScanWindow::ScanWindow(double low, double high, CVID unit)
: ParamContainer(new b::ScanWindow(low, high, (pwiz::CVID) unit))
{owner_ = nullptr; base_ = static_cast<b::ScanWindow*>(ParamContainer::base_);}


Scan::Scan()
: ParamContainer(new b::Scan())
{owner_ = nullptr; base_ = static_cast<b::Scan*>(ParamContainer::base_);}

SourceFile^ Scan::sourceFile::get() {return NATIVE_SHARED_PTR_TO_CLI(b::SourceFilePtr, SourceFile, base_->sourceFilePtr);}
void Scan::sourceFile::set(SourceFile^ value) {base_->sourceFilePtr = CLI_TO_NATIVE_SHARED_PTR(b::SourceFilePtr, value);}

System::String^ Scan::spectrumID::get() {return gcnew System::String(base_->spectrumID.c_str());}
void Scan::spectrumID::set(System::String^ value) {base_->spectrumID = ToStdString(value);}

System::String^ Scan::externalSpectrumID::get() {return gcnew System::String(base_->externalSpectrumID.c_str());}
void Scan::externalSpectrumID::set(System::String^ value) {base_->externalSpectrumID = ToStdString(value);}

InstrumentConfiguration^ Scan::instrumentConfiguration::get() {return NATIVE_SHARED_PTR_TO_CLI(pwiz::msdata::InstrumentConfigurationPtr, InstrumentConfiguration, base_->instrumentConfigurationPtr);}
void Scan::instrumentConfiguration::set(InstrumentConfiguration^ value) {base_->instrumentConfigurationPtr = CLI_TO_NATIVE_SHARED_PTR(b::InstrumentConfigurationPtr, value);}

bool Scan::empty()
{
    return base_->empty();
}


ScanList::ScanList()
: ParamContainer(new b::ScanList())
{owner_ = nullptr; base_ = static_cast<b::ScanList*>(ParamContainer::base_);}
	
Scans^ ScanList::scans::get() {return gcnew Scans(&base_->scans, this);}

bool ScanList::empty()
{
    return base_->empty();
}


IsolationWindow::IsolationWindow()
: ParamContainer(new b::IsolationWindow())
{owner_ = nullptr; base_ = static_cast<b::IsolationWindow*>(ParamContainer::base_);}


SelectedIon::SelectedIon()
: ParamContainer(new b::SelectedIon())
{owner_ = nullptr; base_ = static_cast<b::SelectedIon*>(ParamContainer::base_);}


Activation::Activation()
: ParamContainer(new b::Activation())
{owner_ = nullptr; base_ = static_cast<b::Activation*>(ParamContainer::base_);}


Precursor::Precursor()
: ParamContainer(new b::Precursor())
{owner_ = nullptr; base_ = static_cast<b::Precursor*>(ParamContainer::base_);}

SourceFile^ Precursor::sourceFile::get() {return NATIVE_SHARED_PTR_TO_CLI(b::SourceFilePtr, SourceFile, base_->sourceFilePtr);}
void Precursor::sourceFile::set(SourceFile^ value) {base_->sourceFilePtr = CLI_TO_NATIVE_SHARED_PTR(b::SourceFilePtr, value);}

System::String^ Precursor::spectrumID::get() {return gcnew System::String(base_->spectrumID.c_str());}
void Precursor::spectrumID::set(System::String^ value) {base_->spectrumID = ToStdString(value);}

System::String^ Precursor::externalSpectrumID::get() {return gcnew System::String(base_->externalSpectrumID.c_str());}
void Precursor::externalSpectrumID::set(System::String^ value) {base_->externalSpectrumID = ToStdString(value);}

IsolationWindow^ Precursor::isolationWindow::get() {return gcnew IsolationWindow(&base_->isolationWindow, this);}
void Precursor::isolationWindow::set(IsolationWindow^ value) {base_->isolationWindow = *value->base_;}

SelectedIonList^ Precursor::selectedIons::get() {return gcnew SelectedIonList(&base_->selectedIons, this);}

Activation^ Precursor::activation::get() {return gcnew Activation(&base_->activation, this);}
void Precursor::activation::set(Activation^ value) {base_->activation = *value->base_;}

bool Precursor::empty()
{
    return base_->empty();
}


Product::Product()
: base_(new b::Product())
{owner_ = nullptr;}

IsolationWindow^ Product::isolationWindow::get() {return gcnew IsolationWindow(&base_->isolationWindow, this);}
void Product::isolationWindow::set(IsolationWindow^ value) {base_->isolationWindow = *value->base_;}

bool Product::empty()
{
    return base_->empty();
}


BinaryDataArray::BinaryDataArray()
: ParamContainer(new b::BinaryDataArray())
{base_ = new boost::shared_ptr<b::BinaryDataArray>(static_cast<b::BinaryDataArray*>(ParamContainer::base_));}

DataProcessing^ BinaryDataArray::dataProcessing::get() {return NATIVE_SHARED_PTR_TO_CLI(b::DataProcessingPtr, DataProcessing, (*base_)->dataProcessingPtr);}
void BinaryDataArray::dataProcessing::set(DataProcessing^ value) {(*base_)->dataProcessingPtr = CLI_TO_NATIVE_SHARED_PTR(b::DataProcessingPtr, value);}

BinaryData^ BinaryDataArray::data::get() {return gcnew BinaryData(&(*base_)->data, this);}
void BinaryDataArray::data::set(BinaryData^ value) {(*base_)->data = *value->base_;}

bool BinaryDataArray::empty()
{
    return (*base_)->empty();
}


MZIntensityPair::MZIntensityPair()
: base_(new pwiz::msdata::MZIntensityPair()) {}

MZIntensityPair::MZIntensityPair(double mz, double intensity)
: base_(new pwiz::msdata::MZIntensityPair(mz, intensity)) {}

double MZIntensityPair::mz::get() {return base_->mz;}
void MZIntensityPair::mz::set(double value) {base_->mz = value;}

double MZIntensityPair::intensity::get() {return base_->intensity;}
void MZIntensityPair::intensity::set(double value) {base_->intensity = value;}


TimeIntensityPair::TimeIntensityPair()
: base_(new pwiz::msdata::TimeIntensityPair()) {}

TimeIntensityPair::TimeIntensityPair(double mz, double intensity)
: base_(new pwiz::msdata::TimeIntensityPair(mz, intensity)) {}

double TimeIntensityPair::time::get() {return base_->time;}
void TimeIntensityPair::time::set(double value) {base_->time = value;}

double TimeIntensityPair::intensity::get() {return base_->intensity;}
void TimeIntensityPair::intensity::set(double value) {base_->intensity = value;}


SpectrumIdentity::SpectrumIdentity()
: base_(new pwiz::msdata::SpectrumIdentity()) {}

int SpectrumIdentity::index::get() {return (int) base_->index;}
void SpectrumIdentity::index::set(int value) {base_->index = (size_t) value;}

System::String^ SpectrumIdentity::id::get() {return gcnew System::String(base_->id.c_str());}
void SpectrumIdentity::id::set(System::String^ value) {base_->id = ToStdString(value);}

System::String^ SpectrumIdentity::spotID::get() {return gcnew System::String(base_->spotID.c_str());}
void SpectrumIdentity::spotID::set(System::String^ value) {base_->spotID = ToStdString(value);}

System::UInt64 SpectrumIdentity::sourceFilePosition::get() {return (System::UInt64) base_->sourceFilePosition;}
void SpectrumIdentity::sourceFilePosition::set(System::UInt64 value) {base_->sourceFilePosition = (size_t) value;}


ChromatogramIdentity::ChromatogramIdentity()
: base_(new pwiz::msdata::ChromatogramIdentity()) {}

int ChromatogramIdentity::index::get() {return (int) base_->index;}
void ChromatogramIdentity::index::set(int value) {base_->index = (size_t) value;}

System::String^ ChromatogramIdentity::id::get() {return gcnew System::String(base_->id.c_str());}
void ChromatogramIdentity::id::set(System::String^ value) {base_->id = ToStdString(value);}

System::UInt64 ChromatogramIdentity::sourceFilePosition::get() {return (System::UInt64) base_->sourceFilePosition;}
void ChromatogramIdentity::sourceFilePosition::set(System::UInt64 value) {base_->sourceFilePosition = (size_t) value;}


Spectrum::Spectrum()
: ParamContainer(new b::Spectrum())
{base_ = new boost::shared_ptr<b::Spectrum>(static_cast<b::Spectrum*>(ParamContainer::base_));}

int Spectrum::index::get() {return (int) (*base_)->index;}
void Spectrum::index::set(int value) {(*base_)->index = (size_t) value;}

System::String^ Spectrum::id::get() {return gcnew System::String((*base_)->id.c_str());}
void Spectrum::id::set(System::String^ value) {(*base_)->id = ToStdString(value);}

System::String^ Spectrum::spotID::get() {return gcnew System::String((*base_)->spotID.c_str());}
void Spectrum::spotID::set(System::String^ value) {(*base_)->spotID = ToStdString(value);}

System::UInt64 Spectrum::sourceFilePosition::get() {return (System::UInt64) (*base_)->sourceFilePosition;}
void Spectrum::sourceFilePosition::set(System::UInt64 value) {(*base_)->sourceFilePosition = (size_t) value;}

System::UInt64 Spectrum::defaultArrayLength::get() {return (System::UInt64) (*base_)->defaultArrayLength;}
void Spectrum::defaultArrayLength::set(System::UInt64 value) {(*base_)->defaultArrayLength = (size_t) value;}
 
DataProcessing^ Spectrum::dataProcessing::get() {return NATIVE_SHARED_PTR_TO_CLI(b::DataProcessingPtr, DataProcessing, (*base_)->dataProcessingPtr);}
void Spectrum::dataProcessing::set(DataProcessing^ value) {(*base_)->dataProcessingPtr = CLI_TO_NATIVE_SHARED_PTR(b::DataProcessingPtr, value);}

SourceFile^ Spectrum::sourceFile::get() {return NATIVE_SHARED_PTR_TO_CLI(b::SourceFilePtr, SourceFile, (*base_)->sourceFilePtr);}
void Spectrum::sourceFile::set(SourceFile^ value) {(*base_)->sourceFilePtr = CLI_TO_NATIVE_SHARED_PTR(b::SourceFilePtr, value);}

ScanList^ Spectrum::scanList::get() {return gcnew ScanList(&(*base_)->scanList, this);}
PrecursorList^ Spectrum::precursors::get() {return gcnew PrecursorList(&(*base_)->precursors, this);}
ProductList^ Spectrum::products::get() {return gcnew ProductList(&(*base_)->products, this);}

BinaryDataArrayList^ Spectrum::binaryDataArrays::get() {return gcnew BinaryDataArrayList(&(*base_)->binaryDataArrayPtrs, this);}
void Spectrum::binaryDataArrays::set(BinaryDataArrayList^ value) {(*base_)->binaryDataArrayPtrs = *value->base_;}

void Spectrum::getMZIntensityPairs(MZIntensityPairList^% output)
{
    CATCH_AND_FORWARD
    (
        std::vector<b::MZIntensityPair>* p = new std::vector<b::MZIntensityPair>();
        (*base_)->getMZIntensityPairs(*p);
        output = gcnew MZIntensityPairList(p);
    )
}

BinaryDataArray^ Spectrum::getMZArray()
{
    CATCH_AND_FORWARD(return gcnew BinaryDataArray(new b::BinaryDataArrayPtr((*base_)->getMZArray()));)
}

BinaryDataArray^ Spectrum::getIntensityArray()
{
    CATCH_AND_FORWARD(return gcnew BinaryDataArray(new b::BinaryDataArrayPtr((*base_)->getIntensityArray()));)
}

void Spectrum::setMZIntensityPairs(MZIntensityPairList^ input)
{
    (*base_)->setMZIntensityPairs(*input->base_, (pwiz::CVID) CVID::CVID_Unknown);
}

void Spectrum::setMZIntensityPairs(MZIntensityPairList^ input, CVID intensityUnits)
{
    (*base_)->setMZIntensityPairs(*input->base_, (pwiz::CVID) intensityUnits);
}

void Spectrum::setMZIntensityArrays(System::Collections::Generic::List<double>^ mzArray,
                                    System::Collections::Generic::List<double>^ intensityArray)
{
    setMZIntensityArrays(mzArray, intensityArray, CVID::CVID_Unknown);
}

void Spectrum::setMZIntensityArrays(System::Collections::Generic::List<double>^ mzArray,
                                    System::Collections::Generic::List<double>^ intensityArray,
                                    CVID intensityUnits)
{
    std::vector<double> mzVector;
    if (mzArray->Count > 0)
    {
        cli::array<double>^ mzArray2 = mzArray->ToArray();
        pin_ptr<double> mzArrayPinPtr = &mzArray2[0];
        double* mzArrayBegin = (double*) mzArrayPinPtr;
        mzVector.assign(mzArrayBegin, mzArrayBegin + mzArray2->Length);
    }

    std::vector<double> intensityVector;
    if (intensityArray->Count > 0)
    {
        cli::array<double>^ intensityArray2 = intensityArray->ToArray();
        pin_ptr<double> intensityArrayPinPtr = &intensityArray2[0];
        double* intensityArrayBegin = (double*) intensityArrayPinPtr;
        intensityVector.assign(intensityArrayBegin, intensityArrayBegin + intensityArray2->Length);
    }

    (*base_)->setMZIntensityArrays(mzVector, intensityVector, (pwiz::CVID) intensityUnits);
}

bool Spectrum::empty()
{
    return (*base_)->empty();
}


Chromatogram::Chromatogram()
: ParamContainer(new b::Chromatogram())
{base_ = new boost::shared_ptr<b::Chromatogram>(static_cast<b::Chromatogram*>(ParamContainer::base_));}

int Chromatogram::index::get() {return (int) (*base_)->index;}
void Chromatogram::index::set(int value) {(*base_)->index = (size_t) value;}

System::String^ Chromatogram::id::get() {return gcnew System::String((*base_)->id.c_str());}
void Chromatogram::id::set(System::String^ value) {(*base_)->id = ToStdString(value);}

System::UInt64 Chromatogram::sourceFilePosition::get() {return (System::UInt64) (*base_)->sourceFilePosition;}
void Chromatogram::sourceFilePosition::set(System::UInt64 value) {(*base_)->sourceFilePosition = (size_t) value;}

System::UInt64 Chromatogram::defaultArrayLength::get() {return (*base_)->defaultArrayLength;}
void Chromatogram::defaultArrayLength::set(System::UInt64 value) {(*base_)->defaultArrayLength = (size_t) value;}
 
DataProcessing^ Chromatogram::dataProcessing::get()  {return NATIVE_SHARED_PTR_TO_CLI(pwiz::msdata::DataProcessingPtr, DataProcessing, (*base_)->dataProcessingPtr);}
//void set(DataProcessing^ value) {(*base_)->dataProcessingPtr = *value->base_;}

Precursor^ Chromatogram::precursor::get() {return gcnew Precursor(&(*base_)->precursor);}
void Chromatogram::precursor::set(Precursor^ value) {(*base_)->precursor = *value->base_;}

Product^ Chromatogram::product::get() {return gcnew Product(&(*base_)->product);}
void Chromatogram::product::set(Product^ value) {(*base_)->product = *value->base_;}

BinaryDataArrayList^ Chromatogram::binaryDataArrays::get() {return gcnew BinaryDataArrayList(&(*base_)->binaryDataArrayPtrs, this);}
void Chromatogram::binaryDataArrays::set(BinaryDataArrayList^ value) {(*base_)->binaryDataArrayPtrs = *value->base_;}

void Chromatogram::getTimeIntensityPairs(TimeIntensityPairList^% output)
{
    CATCH_AND_FORWARD
    (
        std::vector<b::TimeIntensityPair>* p = new std::vector<b::TimeIntensityPair>();
        (*base_)->getTimeIntensityPairs(*p);
        output = gcnew TimeIntensityPairList(p);
    )
}

void Chromatogram::setTimeIntensityPairs(TimeIntensityPairList^ input, CVID timeUnits, CVID intensityUnits)
{
    CATCH_AND_FORWARD((*base_)->setTimeIntensityPairs(*input->base_, (pwiz::CVID) timeUnits, (pwiz::CVID) intensityUnits);)
}

bool Chromatogram::empty()
{
    return (*base_)->empty();
}


int SpectrumList::size()
{
    return (int) (*base_)->size();
}

bool SpectrumList::empty()
{
    return (*base_)->empty();
}

SpectrumIdentity^ SpectrumList::spectrumIdentity(int index)
{
    CATCH_AND_FORWARD(return gcnew SpectrumIdentity(&const_cast<b::SpectrumIdentity&>((*base_)->spectrumIdentity((size_t) index)), this);)
}

int SpectrumList::find(System::String^ id)
{
    CATCH_AND_FORWARD(return (int) (*base_)->find(ToStdString(id));)
}

IndexList^ SpectrumList::findNameValue(System::String^ name, System::String^ value)
{
    CATCH_AND_FORWARD
    (
        b::IndexList indexList = (*base_)->findNameValue(ToStdString(name), ToStdString(value));
        std::vector<size_t>* ownedIndexListPtr = new std::vector<size_t>();
        ownedIndexListPtr->swap(indexList);
        return gcnew IndexList(ownedIndexListPtr);
    )
}


Spectrum^ SpectrumList::spectrum(int index)
{
    return spectrum(index, false);
}

Spectrum^ SpectrumList::spectrum(int index, bool getBinaryData)
{
    CATCH_AND_FORWARD(return gcnew Spectrum(new b::SpectrumPtr((*base_)->spectrum((size_t) index, getBinaryData)));)
}

DataProcessing^ SpectrumList::dataProcessing()
{
    const shared_ptr<const b::DataProcessing> cdp = (*base_)->dataProcessingPtr();
    if (!cdp.get())
        return nullptr;
    b::DataProcessingPtr dp = boost::const_pointer_cast<b::DataProcessing>(cdp);
    return NATIVE_SHARED_PTR_TO_CLI(b::DataProcessingPtr, DataProcessing, dp);
}


SpectrumListSimple::SpectrumListSimple()
: SpectrumList(new boost::shared_ptr<b::SpectrumList>(new b::SpectrumListSimple()))
{base_ = reinterpret_cast<boost::shared_ptr<b::SpectrumListSimple>*>(SpectrumList::base_);}

Spectra^ SpectrumListSimple::spectra::get() {return gcnew Spectra(&(*base_)->spectra, this);}
void SpectrumListSimple::spectra::set(Spectra^ value) {(*base_)->spectra = *value->base_;}

int SpectrumListSimple::size()
{
    return (*base_)->size();
}

bool SpectrumListSimple::empty()
{
    return (*base_)->empty();
}

SpectrumIdentity^ SpectrumListSimple::spectrumIdentity(int index)
{
    return gcnew SpectrumIdentity(&const_cast<b::SpectrumIdentity&>((*base_)->spectrumIdentity((size_t) index)), this);
}

Spectrum^ SpectrumListSimple::spectrum(int index)
{
    return spectrum(index, false);
}

Spectrum^ SpectrumListSimple::spectrum(int index, bool getBinaryData)
{
    CATCH_AND_FORWARD(return gcnew Spectrum(new b::SpectrumPtr((*base_)->spectrum((size_t) index, getBinaryData)));)
}


int ChromatogramList::size()
{
    return (int) (*base_)->size();
}

bool ChromatogramList::empty()
{
    return (*base_)->empty();
}

ChromatogramIdentity^ ChromatogramList::chromatogramIdentity(int index)
{
    CATCH_AND_FORWARD(return gcnew ChromatogramIdentity(&const_cast<b::ChromatogramIdentity&>((*base_)->chromatogramIdentity((size_t) index)), this);)
}

int ChromatogramList::find(System::String^ id)
{
    CATCH_AND_FORWARD(return (int) (*base_)->find(ToStdString(id));)
}

Chromatogram^ ChromatogramList::chromatogram(int index)
{
    return chromatogram(index, false);
}

Chromatogram^ ChromatogramList::chromatogram(int index, bool getBinaryData)
{
    CATCH_AND_FORWARD(return gcnew Chromatogram(new b::ChromatogramPtr((*base_)->chromatogram((size_t) index, getBinaryData)));)
}

DataProcessing^ ChromatogramList::dataProcessing()
{
    const shared_ptr<const b::DataProcessing> cdp = (*base_)->dataProcessingPtr();
    if (!cdp.get())
        return nullptr;
    b::DataProcessingPtr dp = boost::const_pointer_cast<b::DataProcessing>(cdp);
    return NATIVE_SHARED_PTR_TO_CLI(b::DataProcessingPtr, DataProcessing, dp);
}


ChromatogramListSimple::ChromatogramListSimple()
: ChromatogramList(new boost::shared_ptr<b::ChromatogramList>(new b::ChromatogramListSimple()))
{base_ = reinterpret_cast<boost::shared_ptr<b::ChromatogramListSimple>*>(ChromatogramList::base_);}

Chromatograms^ ChromatogramListSimple::chromatograms::get() {return gcnew Chromatograms(&(*base_)->chromatograms, this);}
void ChromatogramListSimple::chromatograms::set(Chromatograms^ value) {(*base_)->chromatograms = *value->base_;}

int ChromatogramListSimple::size()
{
    return (*base_)->size();
}

bool ChromatogramListSimple::empty()
{
    return (*base_)->empty();
}

ChromatogramIdentity^ ChromatogramListSimple::chromatogramIdentity(int index)
{
    CATCH_AND_FORWARD(return gcnew ChromatogramIdentity(&const_cast<b::ChromatogramIdentity&>((*base_)->chromatogramIdentity((size_t) index)), this);)
}

Chromatogram^ ChromatogramListSimple::chromatogram(int index)
{
    return chromatogram(index, false);
}

Chromatogram^ ChromatogramListSimple::chromatogram(int index, bool getBinaryData)
{
    CATCH_AND_FORWARD(return gcnew Chromatogram(new b::ChromatogramPtr((*base_)->chromatogram((size_t) index, getBinaryData)));)
}


Run::Run()
: ParamContainer(new b::Run())
{owner_ = nullptr; base_ = static_cast<b::Run*>(ParamContainer::base_);}

System::String^ Run::id::get() {return gcnew System::String(base_->id.c_str());}
void Run::id::set(System::String^ value) {base_->id = ToStdString(value);}

InstrumentConfiguration^ Run::defaultInstrumentConfiguration::get() {return NATIVE_SHARED_PTR_TO_CLI(b::InstrumentConfigurationPtr, InstrumentConfiguration, base_->defaultInstrumentConfigurationPtr);}
void Run::defaultInstrumentConfiguration::set(InstrumentConfiguration^ value) {base_->defaultInstrumentConfigurationPtr = CLI_TO_NATIVE_SHARED_PTR(b::InstrumentConfigurationPtr, value);}

Sample^ Run::sample::get() {return NATIVE_SHARED_PTR_TO_CLI(b::SamplePtr, Sample, base_->samplePtr);}
void Run::sample::set(Sample^ value) {base_->samplePtr = CLI_TO_NATIVE_SHARED_PTR(b::SamplePtr, value);}

System::String^ Run::startTimeStamp::get() {return gcnew System::String(base_->startTimeStamp.c_str());}
void Run::startTimeStamp::set(System::String^ value) {base_->startTimeStamp = ToStdString(value);}

SourceFile^ Run::defaultSourceFile::get() {return NATIVE_SHARED_PTR_TO_CLI(b::SourceFilePtr, SourceFile, base_->defaultSourceFilePtr);}
void Run::defaultSourceFile::set(SourceFile^ value) {base_->defaultSourceFilePtr = CLI_TO_NATIVE_SHARED_PTR(b::SourceFilePtr, value);}

SpectrumList^ Run::spectrumList::get() {return NATIVE_OWNED_SHARED_PTR_TO_CLI(b::SpectrumListPtr, SpectrumList, base_->spectrumListPtr, this);}
void Run::spectrumList::set(SpectrumList^ value) {base_->spectrumListPtr = *value->base_;}

ChromatogramList^ Run::chromatogramList::get() {return NATIVE_OWNED_SHARED_PTR_TO_CLI(b::ChromatogramListPtr, ChromatogramList, base_->chromatogramListPtr, this);}
void Run::chromatogramList::set(ChromatogramList^ value) {base_->chromatogramListPtr = *value->base_;}

bool Run::empty()
{
    return base_->empty();
}


MSData::MSData()
: base_(new boost::shared_ptr<b::MSData>(new b::MSData())), owner_(nullptr)
{
}

System::String^ MSData::accession::get() {return gcnew System::String((*base_)->accession.c_str());}
void MSData::accession::set(System::String^ value) {(*base_)->accession = ToStdString(value);}

System::String^ MSData::id::get() {return gcnew System::String((*base_)->id.c_str());}
void MSData::id::set(System::String^ value) {(*base_)->id = ToStdString(value);}

CVList^ MSData::cvs::get() {return gcnew CVList(&(*base_)->cvs, this);}
void MSData::cvs::set(CVList^ value) {(*base_)->cvs = *value->base_;}

FileDescription^ MSData::fileDescription::get() {return gcnew FileDescription(&(*base_)->fileDescription, this);}
void MSData::fileDescription::set(FileDescription^ value) {(*base_)->fileDescription = *value->base_;}

ParamGroupList^ MSData::paramGroups::get() {return gcnew ParamGroupList(&(*base_)->paramGroupPtrs, this);}
void MSData::paramGroups::set(ParamGroupList^ value) {(*base_)->paramGroupPtrs = *value->base_;}

SampleList^ MSData::samples::get() {return gcnew SampleList(&(*base_)->samplePtrs, this);}
void MSData::samples::set(SampleList^ value) {(*base_)->samplePtrs = *value->base_;}

InstrumentConfigurationList^ MSData::instrumentConfigurationList::get() {return gcnew InstrumentConfigurationList(&(*base_)->instrumentConfigurationPtrs, this);}
void MSData::instrumentConfigurationList::set(InstrumentConfigurationList^ value) {(*base_)->instrumentConfigurationPtrs = *value->base_;}

SoftwareList^ MSData::softwareList::get() {return gcnew SoftwareList(&(*base_)->softwarePtrs, this);}
void MSData::softwareList::set(SoftwareList^ value) {(*base_)->softwarePtrs = *value->base_;}

DataProcessingList^ MSData::dataProcessingList::get() {return gcnew DataProcessingList(&(*base_)->dataProcessingPtrs, this);}
void MSData::dataProcessingList::set(DataProcessingList^ value) {(*base_)->dataProcessingPtrs = *value->base_;}

ScanSettingsList^ MSData::scanSettingsList::get() {return gcnew ScanSettingsList(&(*base_)->scanSettingsPtrs, this);}
void MSData::scanSettingsList::set(ScanSettingsList^ value) {(*base_)->scanSettingsPtrs = *value->base_;}

Run^ MSData::run::get()  {return gcnew Run(&(*base_)->run, this);}
//void set(Run^ value) {(*base_)->run = *value->base_;}

bool MSData::empty() {return (*base_)->empty();}
System::String^ MSData::version() {return gcnew System::String((*base_)->version().c_str());}


} // namespace msdata
} // namespace CLI
} // namespace pwiz
