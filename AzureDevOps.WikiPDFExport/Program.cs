using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using CommandLine;
using DinkToPdf;

namespace azuredevops_export_wiki
{
    partial class Program
    {
        static void Main(string[] args)
        {

            Parser.Default.ParseArguments<Options>(args)
                               .WithParsed<Options>(options =>
                               {
                                   WikiPDFExporter exporter = new WikiPDFExporter(options);
                                   exporter.Export();
                               });
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

        [Option("header-left", Required = false, HelpText = "Text in the header on the left, supports placeholders")]
        public string HeaderLeft { get; set; }
        [Option("header-center", Required = false, HelpText = "Text in the header on the center, supports placeholders")]
        public string HeaderCenter { get; set; }
        [Option("header-right", Required = false, HelpText = "Text in the header on the right, supports placeholders")]
        public string HeaderRight { get; set; }
        [Option("header-url", Required = false, HelpText = "URL to an html file containing the header, does not work together with header-right, header-left or header-center")]
        public string HeaderUrl { get; set; }
        [Option("header-line", Required = false, HelpText = "Draw a line below the header")]
        public bool ShowHeaderLine { get; set; }

        [Option("css", Required = false, HelpText = "Path to a css file that is used for styling the PDF")]
        public string CSS { get; set; }
    }
}
