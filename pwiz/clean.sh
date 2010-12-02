#!/bin/bash

pwiz_root=$(dirname $0)
pushd $pwiz_root > /dev/null

echo "Cleaning project..."
if (ls build-*-* > /dev/null 2>&1); then rm -fr build-*-*; fi;
if (ls libraries/boost_*_*_? > /dev/null 2>&1); then rm -fr libraries/boost_*_*_?; fi;
if [ -d libraries/boost-build/jam_src/bin ]; then rm -fr libraries/boost-build/jam_src/bin; fi;
if [ -d libraries/boost-build/jam_src/bootstrap ]; then rm -fr libraries/boost-build/jam_src/bootstrap; fi;
if [ -d libraries/gd-2.0.33 ]; then rm -fr libraries/gd-2.0.33; fi;
if [ -d libraries/zlib-1.2.3 ]; then rm -fr libraries/zlib-1.2.3; fi;
if [ -d libraries/fftw-3.1.2 ]; then rm -fr libraries/fftw-3.1.2; fi;
if [ -f libraries/libfftw3-3.def ]; then rm -f libraries/libfftw3-3.def; fi;
if [ -f libraries/libfftw3-3.dll ]; then rm -f libraries/libfftw3-3.dll; fi;

if [ -f pwiz/Version.cpp ]; then rm -f pwiz/Version.cpp; fi;
if [ -f pwiz/data/msdata/Version.cpp ]; then rm -f pwiz/data/msdata/Version.cpp; fi;
if [ -f pwiz/data/tradata/Version.cpp ]; then rm -f pwiz/data/tradata/Version.cpp; fi;
if [ -f pwiz/data/mziddata/Version.cpp ]; then rm -f pwiz/data/mziddata/Version.cpp; fi;
if [ -f pwiz/data/proteome/Version.cpp ]; then rm -f pwiz/data/proteome/Version.cpp; fi;
if [ -f pwiz/analysis/Version.cpp ]; then rm -f pwiz/analysis/Version.cpp; fi;

if [ -d pwiz/data/vendor_readers/Thermo/Reader_Thermo_Test.data ]; then rm -fr pwiz/data/vendor_readers/Thermo/Reader_Thermo_Test.data; fi;
if [ -d pwiz/data/vendor_readers/Agilent/Reader_Agilent_Test.data ]; then rm -fr pwiz/data/vendor_readers/Agilent/Reader_Agilent_Test.data; fi;
if [ -d pwiz/data/vendor_readers/ABI/Reader_ABI_Test.data ]; then rm -fr pwiz/data/vendor_readers/ABI/Reader_ABI_Test.data; fi;
if [ -d pwiz/data/vendor_readers/ABI/T2D/Reader_ABI_T2D_Test.data ]; then rm -fr pwiz/data/vendor_readers/ABI/T2D/Reader_ABI_T2D_Test.data; fi;
if [ -d pwiz/data/vendor_readers/Waters/Reader_Waters_Test.data ]; then rm -fr pwiz/data/vendor_readers/Waters/Reader_Waters_Test.data; fi;
if [ -d pwiz/data/vendor_readers/Bruker/Reader_Bruker_Test.data ]; then rm -fr pwiz/data/vendor_readers/Bruker/Reader_Bruker_Test.data; fi;

popd > /dev/null
