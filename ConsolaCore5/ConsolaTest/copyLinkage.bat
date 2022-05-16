@echo off
set _link_=%~dp0linkage
set _arch_=%~1
set _conf_=%~2

cd %~dp0
cd..
cd..
set _self_=%CD%\bin\core5\v142\%_arch_%\%_conf_%

del /f /s /q "%_link_%\*.*"

copy "%_self_%\*.dll" "%_link_%"
copy "%_self_%\*.json" "%_link_%"

if "%_conf_%"=="Debug" (
copy "%_self_%\*.pdb" "%_link_%"
)

:: copy "%_link_%\Consola.dll" "%_link_%\..\Consola.dll"

set _link_=
set _arch_=
set _conf_=
