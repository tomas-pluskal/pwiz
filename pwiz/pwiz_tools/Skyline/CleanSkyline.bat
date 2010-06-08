@echo off
setlocal
@echo off

REM # Get the location of quickbuild.bat and drop trailing slash
set PWIZ_ROOT=%~dp0
set PWIZ_ROOT=%PWIZ_ROOT:~0,-1%
pushd %PWIZ_ROOT%

IF EXIST bin rmdir /s /q bin
IF EXIST obj rmdir /s /q obj
IF EXIST Microsoft.VC90.MFC rmdir /s /q Microsoft.VC90.MFC
IF EXIST ClearCore.dll del /q ClearCore.dll
IF EXIST ClearCore.Storage.dll del /q ClearCore.Storage.dll
IF EXIST Interop.EDAL.SxS.manifest del /q Interop.EDAL.SxS.manifest
IF EXIST MassLynxRaw.dll del /q MassLynxRaw.dll
IF EXIST Test\bin rmdir /s /q Test\bin
IF EXIST Test\obj rmdir /s /q Test\obj
IF EXIST TestFunctional\bin rmdir /s /q TestFunctional\bin
IF EXIST TestFunctional\obj rmdir /s /q TestFunctional\obj
IF EXIST TestResults rmdir /s /q TestResults
IF EXIST ..\Shared\ProteomeDb\bin rmdir /s /q ..\Shared\ProteomeDb\bin
IF EXIST ..\Shared\ProteomeDb\obj rmdir /s /q ..\Shared\ProteomeDb\obj
IF EXIST ..\Shared\ProteowizardWrapper\Interop.EDAL.SxS.manifest del /q ..\Shared\ProteowizardWrapper\Interop.EDAL.SxS.manifest
IF EXIST ..\Shared\ProteowizardWrapper\bin rmdir /s /q ..\Shared\ProteowizardWrapper\bin
IF EXIST ..\Shared\ProteowizardWrapper\obj rmdir /s /q ..\Shared\ProteowizardWrapper\obj
IF EXIST ..\Shared\ProteowizardWrapper\Microsoft.VC90.MFC rmdir /s /q ..\Shared\ProteowizardWrapper\Microsoft.VC90.MFC
IF EXIST ..\Shared\MSGraph\bin rmdir /s /q ..\Shared\MSGraph\bin
IF EXIST ..\Shared\MSGraph\obj rmdir /s /q ..\Shared\MSGraph\obj
IF EXIST ..\Shared\Crawdad\obj rmdir /s /q ..\Shared\Crawdad\obj

popd