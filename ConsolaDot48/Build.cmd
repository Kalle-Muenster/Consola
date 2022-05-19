@echo off

set _name_=Consola
set _call_=%CD%
cd %~dp0
set _here_=%CD%
cd..
set _root_=%CD%


:: Set Version
set ConsolaVersionNumber=00000107
set ConsolaVersionString=0.0.1.7
set DotNetVersionString=dot48

:: Set Dependency locations
echo No dependencies!

:: Set parameters and solution files
call .\..\Args "%~1" "%~2" "%~3" "%~4" Consola.sln Consola.Test.sln

:: Do the build
cd %_here_%
call MsBuild %_target_% %_args_%
cd %_call_%

:: Cleanup Environment
call .\..\Args ParameterCleanUp
