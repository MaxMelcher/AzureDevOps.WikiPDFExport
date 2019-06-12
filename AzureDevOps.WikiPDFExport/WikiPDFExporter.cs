using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Markdig;
using DinkToPdf;
using Markdig.Syntax.Inlines;
using Markdig.Syntax;
using System.Linq;
using Markdig.Renderers;
using System.Text.RegularExpressions;
using Markdig.Helpers;
using Microsoft.Extensions.Logging;
using System.Web;
using Microsoft.ApplicationInsights;
using System.Threading;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.DataContracts;
using System.Globalization;

namespace azuredevops_export_wiki
{
    public class WikiPDFExporter : IWikiPDFExporter
    {
        private Options _options;
        private TelemetryClient _telemetryClient;
        private string _path;

        public WikiPDFExporter(Options options)
        {
            _options = options;

            //initialize AppInsights
            TelemetryConfiguration.Active.InstrumentationKey = "ba33d2f5-1137-446b-8624-3ad0af50a7be";
            _telemetryClient = new TelemetryClient();
        }

        public void Export()
        {
            try
            {
                using (var operation = _telemetryClient.StartOperation<RequestTelemetry>("export"))
                {
                    var timer = Stopwatch.StartNew();

                    _path = _options.Path;
                    if (_path == null)
                    {
                        Log("Using current folder for export, -path is not set.");
                        _path = Directory.GetCurrentDirectory();
                    }
                    else
                    {
                        _path = Path.GetFullPath(_path);
                    }

                    List<MarkdownFile> files = null;
                    if (!string.IsNullOrEmpty(_options.Single))
                    {

                        var directory = new DirectoryInfo(Path.GetFullPath(_path));
                        var filePath = Path.GetFullPath(_options.Single);

                        var relativePath = filePath.Substring(directory.FullName.Length);

                        files = new List<MarkdownFile>()
                        {
                            new MarkdownFile() {
                                AbsolutePath = filePath,
                                RelativePath = relativePath
                            }
                        };
                    }
                    else
                    {

                        files = ReadOrderFiles(_path);
                    }

                    _telemetryClient.TrackEvent("Pages", null, new Dictionary<string, double>() { { "Pages", files.Count } });

                    var html = ConvertMarkdownToHTML(files);

                    ConvertHTMLToPDF(html);

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Export done in {timer.Elapsed}");

                    _telemetryClient.StopOperation(operation);
                    _telemetryClient.Flush();
                    Thread.Sleep(TimeSpan.FromSeconds(5));
                }
            }
            catch (Exception ex)
            {
                Log($"Something bad happend.\n{ex}", LogLevel.Error);
                _telemetryClient.TrackException(ex);
                _telemetryClient.Flush();
                Thread.Sleep(TimeSpan.FromSeconds(5));
            }
        }

        private void ConvertHTMLToPDF(string html)
        {
            Log("Converting HTML to PDF");
            Log("Ignore errors like 'Qt: Could not initialize OLE (error 80010106)'", LogLevel.Warning);
            var converter = new BasicConverter(new PdfTools());

            var output = _options.Output;

            if (output == null)
            {
                output = Path.Combine(Directory.GetCurrentDirectory(), "export.pdf");
            }

            var doc = new HtmlToPdfDocument()
            {
                GlobalSettings = {
                    ColorMode = ColorMode.Color,
                    Orientation = Orientation.Portrait,
                    PaperSize = PaperKind.A4,
                    Out = output,

                },
                Objects = {
                    new ObjectSettings() {
                        PagesCount = true,
                        HtmlContent = html,
                        WebSettings = { DefaultEncoding = "utf-8" },
                        HeaderSettings = { FontSize = 9, Left = _options.HeaderLeft, Center = _options.HeaderCenter, Right = _options.HeaderRight, Line = true, Spacing = 2.812},
                        FooterSettings = { Left = _options.FooterLeft, Center = _options.FooterCenter, Right = _options.FooterRight },
                        UseLocalLinks = true
                    }
                }
            };

            converter.Convert(doc);
            Log($"PDF created at: {output}");
        }

        //Replacing page parameters with dynamic values
        //[PAGE] and [PAGETO] do not need to be replaced because they are handled by the PDF converter
        private string ReplacePageParameters(string input)
        {
            Log("Replacing Page Parameters", LogLevel.Debug);
            Log($"\tInput: {input}");
            input = input.Replace("[DATE]", DateTime.Now.ToString("g"), true, CultureInfo.InvariantCulture);

            Log($"\tOutput: {input}", LogLevel.Debug);
            return input;
        }

        private string ConvertMarkdownToHTML(List<MarkdownFile> files)
        {
            Log("Converting Markdown to HTML");
            StringBuilder sb = new StringBuilder();
            for (var i = 0; i < files.Count; i++)
            {
                var mf = files[i];
                var file = new FileInfo(files[i].AbsolutePath);

                Log($"parsing file {file.Name}", LogLevel.Debug);
                var htmlfile = file.FullName.Replace(".md", ".html");
                var md = File.ReadAllText(file.FullName);
                var document = (MarkdownObject)Markdown.Parse(md);

                //adjust the links
                CorrectLinksAndImages(document, file, mf);

                string html = null;
                var builder = new StringBuilder();
                using (var writer = new System.IO.StringWriter(builder))
                {
                    // write the HTML output
                    var renderer = new HtmlRenderer(writer);
                    renderer.Render(document);
                }
                html = builder.ToString();

                //add html anchor
                var relativePath = file.FullName.Substring(_path.Length);
                relativePath = relativePath.Replace("\\", "");
                relativePath = relativePath.ToLower();
                relativePath = relativePath.Replace(".md", "");

                var anchor = $"<a id=\"{relativePath}\">&nbsp;</a>";

                Log($"\tAnchor: {relativePath}");

                html = anchor + html;

                if (_options.Heading)
                {
                    var filename = file.Name.Replace(".md", "");
                    filename = HttpUtility.UrlDecode(filename);
                    var heading = $"<h1>Section {filename}</h1>";
                    html = heading + html;
                }

                if (_options.BreakPage)
                {
                    //if not one the last page
                    if (i + 1 < files.Count)
                    {
                        Log("Adding new page to PDF");
                        html = "<div style='page-break-after: always;'>" + html + "</div>";
                    }
                }

                if (_options.Debug)
                {
                    Log($"html:\n{html}");
                }
                sb.Append(html);
            }

            var result = sb.ToString();

            if (_options.Debug)
            {
                var htmlPath = Path.Combine(_path, "html.html");
                Log($"Writing converted html to path: {htmlPath}");
                File.WriteAllText(htmlPath, result);
            }
            return result;
        }

        public void CorrectLinksAndImages(MarkdownObject document, FileInfo file, MarkdownFile mf)
        {
            Log("Correcting Links and Images");
            // walk the document node tree and replace relative image links
            // and relative links to markdown pages
            foreach (var link in document.Descendants().OfType<LinkInline>())
            {
                if (!link.Url.StartsWith("http"))
                {

                    string absPath = null;

                    if (link.Url.StartsWith("/"))
                    {
                        absPath = Path.GetFullPath(_path + link.Url);
                    }
                    else
                    {
                        absPath = Path.GetFullPath(file.Directory.FullName + "/" + link.Url);
                    }
                    //the file is a markdown file, create a link to it
                    var isMarkdown = false;
                    var fileInfo = new FileInfo(absPath);
                    if (fileInfo.Exists && fileInfo.Extension.Equals(".md", StringComparison.InvariantCultureIgnoreCase))
                    {
                        isMarkdown = true;
                    }
                    else if (fileInfo.Exists)
                    {
                        link.Url = $"file:///{absPath}";
                    }

                    fileInfo = new FileInfo($"{absPath}.md");
                    if (fileInfo.Exists && fileInfo.Extension.Equals(".md", StringComparison.InvariantCultureIgnoreCase))
                    {
                        isMarkdown = true;
                    }

                    //only markdown files get a pdf internal link
                    if (isMarkdown)
                    {
                        var relPath = mf.RelativePath + "\\" + link.Url;
                        relPath = relPath.Replace("/", "\\");
                        relPath = relPath.Replace("\\", "");
                        relPath = relPath.Replace(".md", "");
                        relPath = relPath.ToLower();
                        Log($"\tMarkdown link: {relPath}");
                        link.Url = $"#{relPath}";
                    }
                }

                CorrectLinksAndImages(link, file, mf);
            }
        }

        private List<MarkdownFile> ReadOrderFiles(string path)
        {
            //read the .order file
            //if there is an entry and a folder with the same name, dive deeper
            var directory = new DirectoryInfo(Path.GetFullPath(path));
            Log($"Reading .order file in directory {directory.Name}");
            var orderFiles = directory.GetFiles(".order", SearchOption.TopDirectoryOnly);

            var result = new List<MarkdownFile>();
            foreach (var orderFile in orderFiles)
            {
                var orders = File.ReadAllLines(orderFile.FullName);
                var relativePath = orderFile.Directory.FullName.Substring(directory.FullName.Length);
                foreach (var order in orders)
                {
                    MarkdownFile mf = new MarkdownFile();
                    mf.AbsolutePath = $"{orderFile.Directory.FullName}\\{order}.md";
                    mf.RelativePath = $"{relativePath}";
                    result.Add(mf);

                    var childPath = Path.Combine(orderFile.Directory.FullName, order);
                    if (Directory.Exists(childPath))
                    {
                        //recursion
                        result.AddRange(ReadOrderFiles(childPath));
                    }
                }
            }

            return result;
        }

        private void Log(string msg, LogLevel logLevel = LogLevel.Information)
        {
            if (_options.Debug && logLevel == LogLevel.Debug)
            {
                Console.WriteLine(msg);
            }

            if (_options.Verbose && logLevel == LogLevel.Information)
            {
                Console.WriteLine(msg);
            }

            if (logLevel == LogLevel.Warning)
            {
                var color = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"WARN: {msg}");
                Console.ForegroundColor = color;
            }

            if (logLevel == LogLevel.Error)
            {
                var color = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"ERR: {msg}");
                Console.ForegroundColor = color;
            }
        }

        public class MarkdownFile
        {
            public string AbsolutePath;
            public string RelativePath;
        }
    }


}
