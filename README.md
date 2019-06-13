## Quickstart
This tool exports a Azure DevOps wiki as PDF. Therefore, you need to git clone the target wiki to a computer. You can get the clone link of the wiki in the top right of the wiki homepage:
![Clone a wiki](images/CloneWiki.png)

To clone this wiki, use the following command:
`git clone <wiki git url>`

The result should look like this: 
![Cloned wiki repository](images/Clone.png)
 
Once you have cloned the wiki, you must download the Azure DevOps WikiPDFExport tool.
**[azuredevops-export-wiki.exe](https://dev.azure.com/mmelcher/8036eca1-fd9e-4c0f-8bef-646b32fbda0b/_apis/git/repositories/e08d1ada-7794-4b89-a3ea-cb64a26683c3/Items?path=%2Fazuredevops-export-wiki.exe&versionDescriptor%5BversionOptions%5D=0&versionDescriptor%5BversionType%5D=0&versionDescriptor%5Bversion%5D=master&download=true&resolveLfs=true&%24format=octetStream&api-version=5.0-preview.1)** (~40MB)

You can drop it right into the cloned folder and execute it there. 
Launched without parameters, the tool will detect all wiki files next to it and convert it to a PDF called export.pdf right next to it. Similar to this [pdf](https://dev.azure.com/mmelcher/8036eca1-fd9e-4c0f-8bef-646b32fbda0b/_apis/git/repositories/e08d1ada-7794-4b89-a3ea-cb64a26683c3/Items?path=%2Fexport.pdf&versionDescriptor%5BversionOptions%5D=0&versionDescriptor%5BversionType%5D=0&versionDescriptor%5Bversion%5D=master&download=true&resolveLfs=true&%24format=octetStream&api-version=5.0-preview.1).

If you need more control over the output, please see the Configuration Options below or by launching the tool with --help parameter.

## Features

The tool currently supports the following:
* Export all wiki pages (and sub pages) in the correct order including styles and formatting.
* Includes pictures (remote and relative urls)
* Creates PDF bookmarks to all pages for easier navigation within the PDF
* If you link to other wiki pages, the link in the PDF will work, too. 
* Everything self-contained. Download the .exe file, run it, done.
* Tool can be used as part of a build, see [Build Task](/AzureDevOps.WikiPDFExport/Build-Task)
* It is fast. A PDF with 160 pages is created in less than a second.

## Requirements

The tool is developed as .NET Core 2.2 application, therefore you need to have the runtime installed. Download is available [here](https://dotnet.microsoft.com/download).

## Download

The download is available [here](https://dev.azure.com/mmelcher/8036eca1-fd9e-4c0f-8bef-646b32fbda0b/_apis/git/repositories/e08d1ada-7794-4b89-a3ea-cb64a26683c3/Items?path=%2Fazuredevops-export-wiki.exe&versionDescriptor%5BversionOptions%5D=0&versionDescriptor%5BversionType%5D=0&versionDescriptor%5Bversion%5D=master&download=true&resolveLfs=true&%24format=octetStream&api-version=5.0-preview.1)

## Configuration Options

### -o / --output
The path to the export file including the filename, e.g. c:\export.pdf

### -d / --date 
The current date will be added to the footer

### -b / --breakPage
For every wiki page a new page in the PDF will be created

### -t / --heading
For every wiki page create a heading in the PDF. If the file is called Home.md a new #Home-heading is added to PDF.

### -s / --single
Path to a single markdown file to convert to PDF. If you want to write your changelog in the wiki, this is your parameter to only convert a single page. 
-p parameter is required, too.

### -p / --path
Path to the wiki folder. If not provided, the current folder of the executable is used.

### -v / --verbose
Verbose mode. Logging will added to the console window

### -g / --debug
Debug mode. Logs tons of stuff and even exports the intermediate html file

### -h / --help
Help - outputs the parameters that can be used

## Limitations

So far the following limitations are known:
* TOC (Table of Contents) tag is not supported and will exported as tag
* The tool, sometimes shows an error "Qt: Could not initialize OLE (error 80010106)" - this can be ignored.
* If headers are not formatted properly (#Header instead of # Header), they are rendered incorrectly. I might fix that in the future.
* The tool lacks proper testing because I only have two wikis available

## License
See [license](/AzureDevOps.WikiPDFExport/License)

## Telemetry
The tool uses Application Insights for basic telemetry:
- The duration of the export and the count of wiki pages is tracked and submitted to Azure. 
- In the case of an error, the exception is submitted. 
- No wiki data/content is submitted.

## Thanks

In this tool uses open source libraries that do the actual work - I just combined them to get the export as PDF:
1. [CommandLineParser](https://github.com/commandlineparser/commandline) to parse the command line
1. [MarkDig](https://github.com/lunet-io/markdig/) to parse markdown files to HTML.
1. [DinkToPdf](https://github.com/rdvojmoc/DinkToPdf) to export HTML to PDF
1. [dotnet-warp](https://github.com/Hubert-Rybak/dotnet-warp) to release a self-contained exe file
