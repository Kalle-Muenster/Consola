@if "%ECHO_STATE%"=="" ( @echo off) else ( @echo %ECHO_STATE%)
pushd "%~dp0"
set _root_=%CD%
popd

if "%_arch_%"=="" (
 set _arch=x86
) else (
 set _arch=%_arch_%
)
set _conf=Release
if "%~1"=="x64" set _arch=%~1
if "%~2"=="x64" set _arch=%~2
if "%~1"=="x86" set _arch=%~1
if "%~2"=="x86" set _arch=%~2
if "%~1"=="Debug" set _conf=%~1
if "%~2"=="Debug" set _conf=%~2
if "%~1"=="clean" set _conf=%~1
if "%~2"=="clean" set _conf=%~2
if not "%~3"=="" (
  if not "%~3"=="test" (
    set WindowsTargetPlatformVersion=%~3
  )
)
if not "%WindowsTargetPlatformVersion%"=="" (
 set _vers=;WindowsTargetPlatformVersion=%WindowsTargetPlatformVersion%
) else (
 if not "%VisualStudioVersion%"=="14.0" use -VC 14 %_arch%
)

if "%_conf%"=="clean" (
 cd StdStreams
 rd /s /q Win32
 rd /s /q x64
 cd..
 cd StdStreamsTest
 rd /s /q obj
 rd /s /q bin
 cd..
 cd StdStreamsWFtest
 rd /s /q obj
 rd /s /q bin
 cd..
) else if "%~3"=="test" (
 pushd %_root_%\StdStreamsTests
 call msbuild StdStreamsTests.sln /p:Configuration=%_conf%;Platform=%_arch%%_vers%
 popd
) else (
 call msbuild StdStreams.sln /p:Configuration=%_conf%;Platform=%_arch%%_vers%
)
set _arch=
set _conf=
set _vers=
set _root_
