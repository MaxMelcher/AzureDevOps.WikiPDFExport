# dotnet tool install --global dotnet-warp

$version = "4.0.0-beta4"
dotnet warp AzureDevOps.WikiPDFExport/azuredevops-export-wiki.csproj -p:Version=$version
git add azuredevops-export-wiki.exe

#linux version
dotnet publish -r linux-x64 --configuration Release -p:PublishReadyToRun=false -p:PublishSingleFile=true

git commit -m "v$version"
git tag -a "v$version" -m "v$version"
git push origin "v$version"