rm -rf ./src/bin/Release

# macos

dotnet publish ./src/Ball.csproj -r:osx-arm64 -c:Release --self-contained -p:PublishTrimmed=true -p:PublishSingleFile=true
cp -R ./moonlibs/lib64/. ./src/bin/Release/net9.0/osx-arm64/publish/
zip -rXq breakup-macos.zip "./src/bin/Release/net9.0/osx-arm64/publish/."
cd ./src/bin/Release/net9.0/osx-arm64/publish/
zip -rXq breakup-macos.zip "."

cd ../../../../../../

# linux

dotnet publish ./src/Ball.csproj -r:linux-x64 -c:Release --self-contained -p:PublishTrimmed=true -p:PublishSingleFile=true
cp -R ./moonlibs/lib64/. ./src/bin/Release/net9.0/linux-x64/publish/
cd ./src/bin/Release/net9.0/linux-x64/publish/
zip -rXq breakup-linux.zip "."

cd ../../../../../../

# windows

dotnet publish ./src/Ball.csproj -r:win-x64 -c:Release --self-contained -p:PublishTrimmed=true -p:PublishSingleFile=true
cp -R ./moonlibs/win64/. ./src/bin/Release/net9.0/win-x64/publish/
cd ./src/bin/Release/net9.0/win-x64/publish/
zip -rXq breakup-win.zip "."

