# https://mcr.microsoft.com/en-us/product/dotnet/runtime/tags
FROM mcr.microsoft.com/dotnet/runtime:8.0-cbl-mariner2.0 AS base
WORKDIR /app

# https://mcr.microsoft.com/en-us/product/dotnet/sdk/tags
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["AzureDevOps.WikiPDFExport/azuredevops-export-wiki.csproj", "."]
RUN dotnet restore "azuredevops-export-wiki.csproj"
COPY . .
WORKDIR "AzureDevOps.WikiPDFExport"
#RUN dotnet build "azuredevops-export-wiki.csproj" -c Release -o ./build

#FROM build AS publish
RUN dotnet publish "azuredevops-export-wiki.csproj" -r linux-x64 --configuration Release -o /src/publish /p:UseAppHost=true /p:PublishReadyToRun=true /p:PublishSingleFile=true

FROM base AS final
WORKDIR /app
COPY --from=build /src/publish .
ENTRYPOINT ["dotnet", "azuredevops-export-wiki"]
