@if not "ECHO_STATE"=="" (@echo %ECHO_STATE%) else (@echo off)
set _here_=%CD%
cd /d %~dp0
cd Consola%DotnetVersionString%
cd Consola.Test
dotnet restore Consola.Test.csproj
cd..
cd ConsolaTestGUI
dotnet restore ConsolaTestGUI.csproj
cd /d %_here_%
set _here_=

