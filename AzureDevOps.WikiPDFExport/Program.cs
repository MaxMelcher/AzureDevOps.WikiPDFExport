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

        [Option('d', "date", Required = false, HelpText = "Adds the current date in the footer")]
        public bool Date { get; set; }

        [Option('s', "single", Required = false, HelpText = "Path to a single markdown file that should be converted.")]
        public string Single { get; set; }

        [Option('p', "path", Required = false, HelpText = "Path to the wiki folder")]
        public string Path { get; set; }

        [Option('o', "output", Required = false, HelpText = "Path and Filename of the export file, e.g. c:\\export.pdf ")]
        public string Output { get; set; }

        [Option('g', "debug", Required = false, HelpText = "Debug mode that exports the converted html file")]
        public bool Debug { get; set; }

        [Option('h', "heading", Required = false, HelpText = "Add heading per page")]
        public bool Heading { get; set; }
    }
}
