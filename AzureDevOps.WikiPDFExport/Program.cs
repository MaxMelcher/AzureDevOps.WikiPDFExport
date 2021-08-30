using System.Threading.Tasks;
using CommandLine;

namespace azuredevops_export_wiki
{
    partial class Program
    {
        static async Task Main(string[] args)
        {
            await Parser.Default.ParseArguments<Options>(args)
                .MapResult(
                    ExecuteWikiPDFExporter,
                    e => Task.FromResult(-1));
        }

        static async Task<int> ExecuteWikiPDFExporter(Options options)
        {
            WikiPDFExporter exporter = new WikiPDFExporter(options);
            await exporter.Export();

            return 0;
        }
    }

    public class Options
    {
        [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
        public bool Verbose { get; set; }

        [Option('b', "breakPage", Required = false, HelpText = "Creates a new PDF page for every wiki page")]
        public bool BreakPage { get; set; }

        [Option('s', "single", Required = false, HelpText = "Path to a single markdown file that should be converted.")]
        public string Single { get; set; }

        [Option('p', "path", Required = false, HelpText = "Path to the wiki folder")]
        public string Path { get; set; }

        [Option("attachments-path", Required = false, HelpText = "Optional path to the folder containing the attachment files, such as images.")]
        public string AttachmentsPath { get; set; }

        [Option('o', "output", Required = false, HelpText = "Path and Filename of the export file, e.g. c:\\export.pdf ")]
        public string Output { get; set; }

        [Option('d', "debug", Required = false, HelpText = "Debug mode that exports the converted html file")]
        public bool Debug { get; set; }

        [Option('h', "heading", Required = false, HelpText = "Add heading per page")]
        public bool Heading { get; set; }

        [Option("pathToHeading", Required = false, HelpText = "Add path of the file to the header")]
        public bool PathToHeading { get; set; }

        [Option("footer-template", Required = false, HelpText = "A HTML template for the footer. Will be added on every page.")]
        public string FooterTemplate { get; set; }

        [Option("header-template", Required = false, HelpText = "A HTML template for the header. Will be added on every page.")]
        public string HeaderTemplate { get; set; }

        [Option("footer-template-path", Required = false, HelpText = "Path to an html file containing the footer")]
        public string FooterTemplatePath { get; set; }
        [Option("header-template-path", Required = false, HelpText = "Path to an html file containing the header, does not work together with header-right, header-left or header-center")]
        public string HeaderTemplatePath { get; set; }

        [Option("css", Required = false, HelpText = "Path to a css file that is used for styling the PDF")]
        public string CSS { get; set; }

        [Option("no-frontmatter", Required = false, HelpText = "Remove Frontmatter from pages")]
        public bool NoFrontmatter { get; set; }

        [Option("math", Required = false, HelpText = "Convert math/latex formulas")]
        public bool Math { get; set; }

        [Option('m', "mermaid", Required = false, HelpText = "Convert mermaid diagrams to SVG. Will download latest chromium, if chrome-path is not defined")]
        public bool ConvertMermaid { get; set; }

        [Option("mermaidjs-path", Required = false, HelpText = "Path of the mermaid.js file. It'll be used if mermaid diagrams support is turned on (-m/--mermaid). If not specified, 'https://cdnjs.cloudflare.com/ajax/libs/mermaid/8.6.4/mermaid.min.js' will be used.")]
        public string MermaidJsPath { get; set; }

        [Option("chrome-path", Required = false, HelpText = "Path of the chrome or chromium executable. It'll be used if mermaid diagrams support is turned on (-m/--mermaid). If not specified, a headless version will be downloaded.")]
        public string ChromeExecutablePath { get; set; }

        [Option("disableTelemetry", Required = false, HelpText = "Disable telemetry - page count and runtime are collected to detect performance degradation")]
        public bool DisableTelemetry { get; set; }

        [Option("open", Required = false, HelpText = "Opens the PDF after conversion")]
        public bool Open { get; set; }

        [Option("filter", Required = false, HelpText = "Only export if page has a tag with the specified filter. E.g. tags:service, category:azure would only export pages with both frontmatter tags present")]
        public string Filter { get; set; }

        [Option('c', "highlight-code", Required = false, HelpText = "Highlight code blocks using highlight.js")]
        public bool HighlightCode { get; set; }

        [Option("hightlight-style", Required = false, HelpText = "hightlight.js style used for code blocks. Defaults to 'vs'. See https://github.com/highlightjs/highlight.js/tree/main/src/styles for a full list.")]
        public string HighlightStyle { get; set; }

        [Option("pat", Required = false, HelpText = "Personal access token used to access your Azure Devops Organization. If no token is provided and organization and project parameters are provided, it will start a prompt asking you to login.")]
        public string AzureDevopsPAT { get; set; }

        [Option("organization", Required = false, HelpText = "Azure Devops organization URL used to convert work item references to work item links. Ex: https://dev.azure.com/MyOrganizationName/")]
        public string AzureDevopsOrganization { get; set; }
    }
}
