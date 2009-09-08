@echo off
call "C:\Program Files (x86)\Microsoft Visual Studio 9.0\VC\vcvarsall.bat"
if %ERRORLEVEL% NEQ 0 set ERROR_TEXT=Error setting up Visual C++ environment variables & goto error
setlocal
@echo off

REM # Get to the pwiz root directory
set SCRIPTS_MISC_ROOT=%~dp0
set SCRIPTS_MISC_ROOT=%SCRIPTS_MISC_ROOT:~0,-1%
pushd %SCRIPTS_MISC_ROOT%\..\..

REM # call clean
echo ##teamcity[message text='Cleaning project...']
echo ##teamcity[progressMessage 'Cleaning project...']
call clean.bat
if %ERRORLEVEL% NEQ 0 set ERROR_TEXT=Error performing clean & goto error

REM # the -p1 argument overrides bjam's default behavior of merging stderr into stdout

REM # call quickbuild to generate SVN revision info
echo ##teamcity[message text='Generating revision info...']
echo ##teamcity[progressMessage 'Generating revision info...']
call quickbuild.bat -j4 -p1 pwiz//svnrev.hpp
if %ERRORLEVEL% NEQ 0 set ERROR_TEXT=Error generating revision info & goto error

REM # call quickbuild to build and run tests
echo ##teamcity[message text='Running quickbuild...']
echo ##teamcity[progressMessage 'Running quickbuild...']
call quickbuild.bat -j4 -p1 ci=teamcity
if %ERRORLEVEL% NEQ 0 set ERROR_TEXT=Error performing quickbuild & goto error

REM # uncomment this to test that test failures and error output are handled properly
call quickbuild.bat -p1 ci=teamcity pwiz/utility/misc//FailTest

popd
goto :EOF

:error
echo "##teamcity[message text='%ERROR_TEXT%' status='ERROR']"
exit /b 1
