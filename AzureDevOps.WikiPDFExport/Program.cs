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

        [Option("footer-left", Required = false, HelpText = "Text in the footer on the left, supports placeholders")]
        public string FooterLeft { get; set; }
        [Option("footer-center", Required = false, HelpText = "Text in the footer on the center, supports placeholders")]
        public string FooterCenter { get; set; }
        [Option("footer-right", Required = false, HelpText = "Text in the footer on the right, supports placeholders")]
        public string FooterRight { get; set; }
        [Option("footer-url", Required = false, HelpText = "URL to an html file containing the footer")]
        public string FooterUrl { get; set; }        
        [Option("footer-hide-line", Required = false, HelpText = "Hide the line below the footer")]
        public bool HideFooterLine { get; set; }

        [Option("header-left", Required = false, HelpText = "Text in the header on the left, supports placeholders")]
        public string HeaderLeft { get; set; }
        [Option("header-center", Required = false, HelpText = "Text in the header on the center, supports placeholders")]
        public string HeaderCenter { get; set; }
        [Option("header-right", Required = false, HelpText = "Text in the header on the right, supports placeholders")]
        public string HeaderRight { get; set; }
        [Option("header-url", Required = false, HelpText = "URL to an html file containing the header, does not work together with header-right, header-left or header-center")]
        public string HeaderUrl { get; set; }
        [Option("header-hide-line", Required = false, HelpText = "Hide the line below the header")]
        public bool HideHeaderLine { get; set; }

        [Option("css", Required = false, HelpText = "Path to a css file that is used for styling the PDF")]
        public string CSS { get; set; }

        [Option('m', "mermaid", Required = false, HelpText = "Convert mermaid diagrams to SVG. Will download latest chromium, if chrome-path is not defined")]
        public bool ConvertMermaid { get; set; }

        [Option("mermaidjs-path", Required = false, HelpText = "Path of the mermaid.js file. It'll be used if mermaid diagrams support is turned on (-m/--mermaid). If not specified, 'https://cdnjs.cloudflare.com/ajax/libs/mermaid/8.6.4/mermaid.min.js' will be used.")]
        public string MermaidJsPath { get;set; }

        [Option("chrome-path", Required = false, HelpText = "Path of the chrome or chromium executable. It'll be used if mermaid diagrams support is turned on (-m/--mermaid). If not specified, a headless version will be downloaded.")]
        public string ChromeExecutablePath { get; set; }

        [Option("disableTelemetry", Required = false, HelpText = "Disable telemetry - page count and runtime are collected to detect performance degradation")]
        public bool DisableTelemetry { get; set; }
    }
}
