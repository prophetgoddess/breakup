#!/bin/bash

rm -rf ./src/bin/Release

# linux

dotnet publish ./src/Ball.csproj -r:linux-x64 -c:Release --self-contained -p:PublishTrimmed=true -p:PublishSingleFile=true
cp -R ./moonlibs/lib64/. ./src/bin/Release/net9.0/linux-x64/publish/
cd ./src/bin/Release/net9.0/linux-x64/publish/
# zip -rXq breakup-linux.zip "."
open .

cd ../../../../../../

# windows

dotnet publish ./src/Ball.csproj -r:win-x64 -c:Release --self-contained -p:PublishTrimmed=true -p:PublishSingleFile=true
cp -R ./moonlibs/win64/. ./src/bin/Release/net9.0/win-x64/publish/
cd ./src/bin/Release/net9.0/win-x64/publish/
# zip -rXq breakup-win.zip "."
open .