set local
set _origin_=%CD%
pushd %~dp0
call build.cmd x86 Debug
popd
call build.cmd x86 Release
popd
call build x64 Debug
popd
call build x64 Release
popd
popd
cd %_origin_%

