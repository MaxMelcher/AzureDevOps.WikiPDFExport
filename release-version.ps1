# handled in github actions
$version = "4.0.0-beta5"
dotnet publish -r win-x64 -c Release -p:Version=$version -o output/win-x64 --self-contained

#linux version
dotnet publish -r linux-x64 --configuration Release -p:PublishReadyToRun=false -p:PublishSingleFile=true  -p:Version=$version -o output/linux-x64 --no-self-contained
