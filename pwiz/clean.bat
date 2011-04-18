@echo off
setlocal
@echo off

REM # Get the location of quickbuild.bat and drop trailing slash
set PWIZ_ROOT=%~dp0
set PWIZ_ROOT=%PWIZ_ROOT:~0,-1%
pushd %PWIZ_ROOT%

echo Cleaning project...
IF EXIST build-nt-x86 rmdir /s /q build-nt-x86
IF EXIST build-nt-x86_64 rmdir /s /q build-nt-x86_64
IF EXIST libraries\boost-build\jam_src\bin.ntx86 rmdir /s /q libraries\boost-build\jam_src\bin.ntx86
IF EXIST libraries\boost-build\jam_src\bootstrap rmdir /s /q libraries\boost-build\jam_src\bootstrap
IF EXIST libraries\boost_1_43_0 rmdir /s /q libraries\boost_1_43_0
IF EXIST libraries\gd-2.0.33 rmdir /s /q libraries\gd-2.0.33
IF EXIST libraries\zlib-1.2.3 rmdir /s /q libraries\zlib-1.2.3
IF EXIST libraries\fftw-3.1.2 rmdir /s /q libraries\fftw-3.1.2

del /q libraries\libfftw3-3.d* > nul 2>&1

del /q pwiz\Version.cpp > nul 2>&1
del /q pwiz\data\msdata\Version.cpp > nul 2>&1
del /q pwiz\data\mziddata\Version.cpp > nul 2>&1
del /q pwiz\data\tradata\Version.cpp > nul 2>&1
del /q pwiz\data\proteome\Version.cpp > nul 2>&1
del /q pwiz\analysis\Version.cpp > nul 2>&1

del /q pwiz_aux\msrc\utility\vendor_api\ABI\*.dll > nul 2>&1
del /q pwiz_aux\msrc\utility\vendor_api\Agilent\*.dll > nul 2>&1
del /q pwiz_aux\msrc\utility\vendor_api\Bruker\*.dll > nul 2>&1
del /q pwiz_aux\msrc\utility\vendor_api\Thermo\*.dll > nul 2>&1
del /q /s pwiz_aux\msrc\utility\vendor_api\Waters\*.dll > nul 2>&1
del /q /s pwiz_aux\msrc\utility\vendor_api\Waters\*.lib > nul 2>&1
del /q pwiz_aux\msrc\utility\vendor_api\Waters\*.h > nul 2>&1

rmdir /s /q pwiz\data\vendor_readers\Thermo\Reader_Thermo_Test.data > nul 2>&1
rmdir /s /q pwiz\data\vendor_readers\Agilent\Reader_Agilent_Test.data > nul 2>&1
rmdir /s /q pwiz\data\vendor_readers\ABI\Reader_ABI_Test.data > nul 2>&1
rmdir /s /q pwiz\data\vendor_readers\ABI\T2D\Reader_ABI_T2D_Test.data > nul 2>&1
rmdir /s /q pwiz\data\vendor_readers\Waters\Reader_Waters_Test.data > nul 2>&1
rmdir /s /q pwiz\data\vendor_readers\Bruker\Reader_Bruker_Test.data > nul 2>&1

IF EXIST pwiz_tools\SeeMS\CleanSeeMS.bat call pwiz_tools\SeeMS\CleanSeeMS.bat
IF EXIST pwiz_tools\Skyline\CleanSkyline.bat call pwiz_tools\Skyline\CleanSkyline.bat
IF EXIST pwiz_tools\Topograph\CleanTopograph.bat call pwiz_tools\Topograph\CleanTopograph.bat

popd
