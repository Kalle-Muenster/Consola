@if "%ECHO_STATE%"=="" ( @echo off ) else ( @echo %ECHO_STATE% )

set _name_=Consola
set _call_=%CD%
cd %~dp0
set _here_=%CD%
cd..
set _root_=%CD%


:: Set Version
set ConsolaVersionNumber=00000108
set ConsolaVersionString=0.0.1.8
set DotNetVersionString=core5
set _tool_=v142

:: Set Dependency locations
echo No dependencies!

:: Set parameters and solution files
call %_root_%\Args "%~1" "%~2" "%~3" "%~4" Consola.sln Consola.Test.sln

:: Do the build
cd %_here_%
call MsBuild %_target_% %_args_%
cd %_call_%

:: Cleanup Environment
call %_root_%\Args ParameterCleanUp
