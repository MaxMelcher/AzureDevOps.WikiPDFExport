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
using Microsoft.Extensions.Logging;
using System.Web;
using Microsoft.ApplicationInsights;
using System.Threading;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.DataContracts;
using System.Globalization;
using PuppeteerSharp;
using System.Threading.Tasks;
using azuredevops_export_wiki.MermaidContainer;
using Markdig.Parsers;

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
            var config = TelemetryConfiguration.CreateDefault();
            config.InstrumentationKey = "ba33d2f5-1137-446b-8624-3ad0af50a7be";
            
            if (_options.DisableTelemetry)
            {
                config.DisableTelemetry = true;
            }
            _telemetryClient = new TelemetryClient(config);


        }

        public async Task Export()
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
                                RelativePath = relativePath,
                                Level = 0 // root level
                            }
                        };
                    }
                    else
                    {

                        files = ReadOrderFiles(_path, 0); // root level
                    }

                    _telemetryClient.TrackEvent("Pages", null, new Dictionary<string, double>() { { "Pages", files.Count } });

                    var html = ConvertMarkdownToHTML(files);

                    if (!string.IsNullOrEmpty(_options.CSS))
                    {
                        html = AddCssStyles(html);
                    }

                    var htmlStart = "<html>";
                    var htmlEnd = "</html>";
                    var head = "<head><meta http-equiv=Content-Type content=\"text/html; charset=utf-8\"></head>";

                    if (!_options.ConvertMermaid)
                    {
                        // adding the correct charset for unicode smileys and all that fancy stuff
                        html = $"{htmlStart}{head}{html}{htmlEnd}";
                    }
                    else
                    {
                        string mermaid = !string.IsNullOrEmpty(_options.MermaidJsPath) ?
                            $"<script>{File.ReadAllText(_options.MermaidJsPath)}</script>"
                            : @"<script src=""https://cdnjs.cloudflare.com/ajax/libs/mermaid/8.6.4/mermaid.min.js""></script>";

                        var mermaidInitialize = "<script>mermaid.initialize({ startOnLoad:true });</script>";

                        // adding the correct charset for unicode smileys and all that fancy stuff, and include mermaid.js
                        html = $"{htmlStart}{head}{html}{mermaid}{mermaidInitialize}{htmlEnd}";

                        if (string.IsNullOrEmpty(_options.ChromeExecutablePath))
                        {
                            RevisionInfo revisionInfo = await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultRevision);
                        }

                        var launchOptions = new LaunchOptions
                        {
                            ExecutablePath = _options.ChromeExecutablePath ?? string.Empty,
                            Headless = true
                        };

                        using (var browser = await Puppeteer.LaunchAsync(launchOptions))
                        {
                            var page = await browser.NewPageAsync();
                            await page.SetContentAsync(html);
                            html = await page.GetContentAsync();
                        }
                    }

                    if (_options.Debug)
                    {
                        var htmlPath = Path.Combine(_path, "html.html");
                        Log($"Writing converted html to path: {htmlPath}");
                        File.WriteAllText(htmlPath, html);
                    }

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

        private string AddCssStyles(string html)
        {
            var path = Path.GetFullPath(_options.CSS);

            if (!File.Exists(path))
            {
                Log("CSS file does not exist", LogLevel.Warning);
                return html;
            }

            var styles = File.ReadAllText(path);

            var start = $"<style>";
            var stop = $"</style>";

            html = start + styles + stop + html;

            return html;
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

            //somehow the options HeaderSettings.Left/Right/Center don't work in combination with HeaderSettings.HtmlURL
            var headerSettings = new HeaderSettings
            {
                FontSize = 9,
                Line = !_options.HideHeaderLine,
                Spacing = 2.812,
            };
            if (string.IsNullOrEmpty(_options.HeaderUrl))
            {
                headerSettings.Left = _options.HeaderLeft;
                headerSettings.Center = _options.HeaderCenter;
                headerSettings.Right = _options.HeaderRight;
            }
            else
            {
                headerSettings.HtmUrl = _options.HeaderUrl;
            }

            var footerSettings = new FooterSettings
            {
                Line = !_options.HideFooterLine
            };
            if (string.IsNullOrEmpty(_options.FooterUrl))
            {
                footerSettings.Left = _options.FooterLeft;
                footerSettings.Center = _options.FooterCenter;
                footerSettings.Right = _options.FooterRight;
            }
            else
            {
                footerSettings.HtmUrl = _options.FooterUrl;
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
                        HeaderSettings = headerSettings,
                        FooterSettings = footerSettings,
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

                if (!File.Exists(file.FullName))
                {
                    Log($"File {file.FullName} specified in the order file was not found and will be skipped!", LogLevel.Error);
                    continue;
                }

                var md = File.ReadAllText(file.FullName);

                // remove scalings from image links, width & height: file.png =600x500
                var regexImageScalings = @"\((.[^\)]*?[png|jpg|jpeg]) =(\d+)x(\d+)\)";
                md = Regex.Replace(md, regexImageScalings, @"($1){width=$2 height=$3}");

                // remove scalings from image links, width only: file.png =600x
                regexImageScalings = @"\((.[^\)]*?[png|jpg|jpeg]) =(\d+)x\)";
                md = Regex.Replace(md, regexImageScalings, @"($1){width=$2}");

                // remove scalings from image links, height only: file.png =x600
                regexImageScalings = @"\((.[^\)]*?[png|jpg|jpeg]) =x(\d+)\)";
                md = Regex.Replace(md, regexImageScalings, @"($1){height=$2}");

                //setup the markdown pipeline to support tables
                var pipelineBuilder = new MarkdownPipelineBuilder()
                    .UsePipeTables()
                    .UseEmojiAndSmiley()
                    .UseAdvancedExtensions();

                // determine the correct nesting of pages and related chapters
                pipelineBuilder.BlockParsers.Replace<HeadingBlockParser>(new OffsetHeadingBlockParser(mf.Level + 1));

                if (_options.ConvertMermaid)
                {
                    pipelineBuilder = pipelineBuilder.UseMermaidContainers();
                }

                var pipeline = pipelineBuilder.Build();

                //parse the markdown document so we can alter it later
                var document = (MarkdownObject)Markdown.Parse(md, pipeline);

                //adjust the links
                CorrectLinksAndImages(document, file, mf);

                string html = null;
                var builder = new StringBuilder();
                using (var writer = new System.IO.StringWriter(builder))
                {
                    // write the HTML output
                    var renderer = new HtmlRenderer(writer);
                    pipeline.Setup(renderer);
                    renderer.Render(document);
                }
                html = builder.ToString();

                //add html anchor
                var anchorPath = file.FullName.Substring(_path.Length);
                anchorPath = anchorPath.Replace("\\", "");
                anchorPath = anchorPath.ToLower();
                anchorPath = anchorPath.Replace(".md", "");

                var relativePath = file.FullName.Substring(_path.Length);

                var anchor = $"<a id=\"{anchorPath}\">&nbsp;</a>";

                Log($"\tAnchor: {anchorPath}");

                html = anchor + html;

                if (_options.PathToHeading)
                {
                    var filename = file.Name;
                    filename = HttpUtility.UrlDecode(relativePath);
                    var heading = $"<b>{filename}</b>";
                    html = heading + html;
                }

                if (_options.Heading)
                {
                    var filename = file.Name.Replace(".md", "");
                    filename = HttpUtility.UrlDecode(filename);
                    var filenameEscapes = new Dictionary<string, string>
                    {
                        {"%3A", ":"},
                        {"%3C", "<"},
                        {"%3E", ">"},
                        {"%2A", "*"},
                        {"%3F", "?"},
                        {"%7C", "|"},
                        {"%2D", "-"},
                        {"%22", "\""},
                        {"-", " "}
                    };

                    var title = new StringBuilder(filename);
                    foreach (var filenameEscape in filenameEscapes)
                    {
                        title.Replace(filenameEscape.Key, filenameEscape.Value);
                    }

                    var heading = $"<h{mf.Level + 1}>{title}</h{mf.Level + 1}>";
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


            return result;
        }

        public void CorrectLinksAndImages(MarkdownObject document, FileInfo file, MarkdownFile mf)
        {
            Log("Correcting Links and Images");
            // walk the document node tree and replace relative image links
            // and relative links to markdown pages
            foreach (var link in document.Descendants().OfType<LinkInline>())
            {
                if (link.Url != null)
                {
                    if (!link.Url.StartsWith("http"))
                    {
                        string absPath = null;

                        //handle --attachments-path case
                        if (!string.IsNullOrEmpty(this._options.AttachmentsPath) && link.Url.StartsWith("/.attachments") || link.Url.StartsWith(".attachments"))
                        {
                            var linkUrl = link.Url.Split('/').Last();

                            //urls could be encoded and contain spaces - they are then not found on disk
                            linkUrl = HttpUtility.UrlDecode(linkUrl);

                            absPath = Path.GetFullPath(Path.Combine(this._options.AttachmentsPath, linkUrl));
                        }
                        else if (link.Url.StartsWith("/"))
                        {
                            //urls could be encoded and contain spaces - they are then not found on disk
                            var linkUrl = HttpUtility.UrlDecode(link.Url);
                            absPath = Path.GetFullPath(_path + linkUrl);
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
                }
                CorrectLinksAndImages(link, file, mf);
            }
        }

        private List<MarkdownFile> ReadOrderFiles(string path, int level)
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
                var relativePath = orderFile.Directory.FullName.Length > directory.FullName.Length ?
                    orderFile.Directory.FullName.Substring(directory.FullName.Length) :
                    "/";
                foreach (var order in orders)
                {
                    //skip empty lines
                    if (string.IsNullOrEmpty(order))
                    {
                        continue;
                        //todo add log entry that we skipped an empty line
                    }

                    MarkdownFile mf = new MarkdownFile();
                    mf.AbsolutePath = $"{orderFile.Directory.FullName}\\{order}.md";
                    mf.RelativePath = $"{relativePath}";
                    mf.Level = level;
                    result.Add(mf);

                    var childPath = Path.Combine(orderFile.Directory.FullName, order);
                    if (Directory.Exists(childPath))
                    {
                        //recursion
                        result.AddRange(ReadOrderFiles(childPath, level + 1));
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
            public int Level;

            public override string ToString()
            {
                return $"[{Level}] {AbsolutePath}";
            }
        }
    }


}
