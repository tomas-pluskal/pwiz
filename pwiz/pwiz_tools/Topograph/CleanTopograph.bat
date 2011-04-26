@echo off
setlocal
@echo off

REM # Get the location of quickbuild.bat and drop trailing slash
set PWIZ_ROOT=%~dp0
set PWIZ_ROOT=%PWIZ_ROOT:~0,-1%
pushd %PWIZ_ROOT%

IF EXIST bin rmdir /s /q bin
IF EXIST obj rmdir /s /q obj
IF EXIST topograph.sln.cache rmdir /s /q topograph.sln.cache
IF EXIST turnover\Microsoft.VC90.MFC rmdir /s /q turnover\Microsoft.VC90.MFC
IF EXIST turnover\ClearCore.dll del /q turnover\ClearCore.dll
IF EXIST turnover\ClearCore.Storage.dll del /q turnover\ClearCore.Storage.dll
IF EXIST turnover\EULA.MHDAC del /q turnover\EULA.MHDAC
IF EXIST turnover\EULA.MSFileReader del /q turnover\EULA.MSFileReader
IF EXIST turnover\Interop.EDAL.SxS.manifest del /q turnover\Interop.EDAL.SxS.manifest
IF EXIST turnover\MassLynxRaw.dll del /q turnover\MassLynxRaw.dll
IF EXIST TestResults rmdir /s /q TestResults
IF EXIST TopographTestProject\bin rmdir /s /q TopographTestProject\bin
IF EXIST TopographTestProject\obj rmdir /s /q TopographTestProject\obj
IF EXIST turnover\bin rmdir /s /q turnover\bin
IF EXIST turnover\obj rmdir /s /q turnover\obj
IF EXIST turnover_lib\bin rmdir /s /q turnover_lib\bin
IF EXIST turnover_lib\obj rmdir /s /q turnover_lib\obj
IF EXIST ..\Shared\Common\bin rmdir /s /q ..\Shared\Common\bin
IF EXIST ..\Shared\Common\obj rmdir /s /q ..\Shared\Common\obj
IF EXIST ..\Shared\ProteomeDb\bin rmdir /s /q ..\Shared\ProteomeDb\bin
IF EXIST ..\Shared\ProteomeDb\obj rmdir /s /q ..\Shared\ProteomeDb\obj
IF EXIST ..\Shared\ProteowizardWrapper\Interop.EDAL.SxS.manifest del /q ..\Shared\ProteowizardWrapper\Interop.EDAL.SxS.manifest
IF EXIST ..\Shared\ProteowizardWrapper\bin rmdir /s /q ..\Shared\ProteowizardWrapper\bin
IF EXIST ..\Shared\ProteowizardWrapper\obj rmdir /s /q ..\Shared\ProteowizardWrapper\obj
IF EXIST ..\Shared\ProteowizardWrapper\Microsoft.VC90.MFC rmdir /s /q ..\Shared\ProteowizardWrapper\Microsoft.VC90.MFC
IF EXIST ..\Shared\MSGraph\bin rmdir /s /q ..\Shared\MSGraph\bin
IF EXIST ..\Shared\MSGraph\obj rmdir /s /q ..\Shared\MSGraph\obj

popd