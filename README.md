## Quickstart
This tool exports a Azure DevOps wiki as PDF. Therefore, you need to git clone the target wiki to a computer. You can get the clone link of the wiki in the top right of the wiki homepage:
![Clone a wiki](images/CloneWiki.png)

To clone this wiki, use the following command:
`git clone https://dev.azure.com/mmelcher/AzureDevOps.WikiPDFExport/_git/AzureDevOps.WikiPDFExport.wiki`

The result should look like this: 
![Cloned wiki repository](images/Clone.png)
 
Once you have cloned the wiki, you must download the Azure DevOps WikiPDFExport tool.
**[azuredevops-export-wiki.exe](https://github.com/MaxMelcher/AzureDevOps.WikiPDFExport/releases)** (~40MB)

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
* Tool can be used as part of a build, see [BuildTask](AzureDevOps.WikiPDFExport/Build-Task.md)
* Supports emoticons :) ⚠ ℹ
* It is fast. A PDF with 160 pages is created in less than a second. 1000 pages in ~8 seconds.

## Requirements

The tool is developed as .NET Core 2.2 application, therefore you need to have the runtime installed. Download is available [here](https://dotnet.microsoft.com/download).

## Download

The download is available [here](https://github.com/MaxMelcher/AzureDevOps.WikiPDFExport/releases)

## Configuration Options

### -h / --help
Help - outputs the parameters that can be used

### -o / --output
The path to the export file including the filename, e.g. c:\export.pdf

### -b / --breakPage
For every wiki page a new page in the PDF will be created

### -h / --heading
For every wiki page create a heading in the PDF. If the file is called Home.md a new #Home-heading is added to PDF.

### -s / --single
Path to a single markdown file to convert to PDF. If you want to write your changelog in the wiki, this is your parameter to only convert a single page. 
-p parameter is required, too.

### -p / --path
Path to the wiki folder. If not provided, the current folder of the executable is used.  
If you only want to convert a subfolder and have images, then you must provide the path to the attachments folder with --attachments-path. 

### --attachments-path
Path to the .attachments folder.  If not provided, the .attachments is assumed to be located under the folder of the wiki (-p/--path).

### -m / --mermaid
Convert mermaid diagrams to SVG. Will download latest chromium, if chrome-path is not defined.

### --mermaidjs-path
Path of the mermaid.js file. It'll be used if mermaid diagrams support is turned on (-m/--mermaid). If not specified, 'https://cdnjs.cloudflare.com/ajax/libs/mermaid/8.6.4/mermaid.min.js' will be downloaded.

### --chrome-path
Path of the chrome or chromium executable. It'll be used if mermaid diagrams support is turned on (-m/--mermaid). If not specified, a headless version will be downloaded.

### --css 
Path to the stylesheet to overwrite the look of certain components in the PDF. See [styles.css](styles.css) for examples. To get the html file, use the [--debug flag](#-d----debug) to inspect and style it.

### -v / --verbose
Verbose mode. Logging will added to the console window

### -d / --debug
Debug mode. Logs tons of stuff and even exports the intermediate html file

### --pathToHeading
Add path of the file to the header

### --footer-left, --footer-center, --footer-right, --header-left, --header-center, --header-right,
Headers and footers can be added to the document by the --header-* and
  --footer* arguments respectfully.  In header and footer text string supplied
  to e.g. --header-left, the following variables will be substituted.

   * [page]       Replaced by the number of the pages currently being printed
   * [frompage]   Replaced by the number of the first page to be printed
   * [topage]     Replaced by the number of the last page to be printed
   * [webpage]    Replaced by the URL of the page being printed
   * [section]    Replaced by the name of the current section
   * [subsection] Replaced by the name of the current subsection
   * [date]       Replaced by the current date in system local format
   * [isodate]    Replaced by the current date in ISO 8601 extended format
   * [time]       Replaced by the current time in system local format
   * [title]      Replaced by the title of the of the current page object
   * [doctitle]   Replaced by the title of the output document
   * [sitepage]   Replaced by the number of the page in the current site being converted
   * [sitepages]  Replaced by the number of pages in the current site being converted

### --disableTelemetry
Disables the telemetry tracking, see [Telemetry](#telemetry)
### --open
Opens the PFD file after conversion. Great for development, not great in a build task.

### --filter
Filters the pages depending on the page [yaml tags](https://docs.microsoft.com/en-us/azure/devops/project/wiki/wiki-markdown-guidance?view=azure-devops#yaml-tags).

## Limitations

So far the following limitations are known:
* TOC (Table of Contents) tag is not supported and removed from the pdf.
* The tool, sometimes shows an error "Qt: Could not initialize OLE (error 80010106)" - this can be ignored.
* If headers are not formatted properly (#Header instead of # Header), they are rendered incorrectly. I might fix that in the future.
* The tool lacks proper testing because I only have two realistic wikis available. Want to contribute one?

## License
See [license](/AzureDevOps.WikiPDFExport/License.md)

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
1. [puppeteer-sharp](https://github.com/hardkoded/puppeteer-sharp) to convert mermaid markdown to SVG
