@echo off
pushd Client
dotnet publish -c Release
popd

pushd Server
dotnet publish -c Release
popd

copy /y fxmanifest.lua ..\
xcopy /y /e Client\bin\Release\net452\publish ..\Client\
xcopy /y /e Server\bin\Release\netstandard2.0\publish ..\Server\