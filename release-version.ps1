# dotnet tool install --global dotnet-warp

$version = "4.0.0-beta3"
dotnet warp AzureDevOps.WikiPDFExport/azuredevops-export-wiki.csproj -p:Version=$version
git tag -a "v$version" -m "v$version"
git push origin "v$version"