using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Markdig;
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
using Markdig.Extensions.Yaml;
using Markdig.Helpers;
using Microsoft.TeamFoundation.Common;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Client;
using Microsoft.VisualStudio.Services.Common;

using Microsoft.VisualStudio.Services.WebApi;
using Process = System.Diagnostics.Process;
using System.Runtime.CompilerServices;

[assembly:InternalsVisibleTo("AzureDevOps.WikiPDFExport.Test")]

namespace azuredevops_export_wiki
{
    public class WikiPDFExporter : IWikiPDFExporter
    {
        private Options _options;
        private TelemetryClient _telemetryClient;
        private ExportedWikiDoc _wiki;
        private Dictionary<string, string> _iconClass;

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

            this._iconClass = new Dictionary<string, string>(){
                {"icon_crown", "bowtie-symbol-crown"},
                {"icon_trophy", "bowtie-symbol-trophy"},
                {"icon_list", "bowtie-symbol-list"},
                {"icon_book", "bowtie-symbol-book"},
                {"icon_sticky_note", "bowtie-symbol-stickynote"},
                {"icon_clipboard", "bowtie-symbol-task"},
                {"icon_insect", "bowtie-symbol-bug"},
                {"icon_traffic_cone", "bowtie-symbol-impediment"},
                {"icon_chat_bubble", "bowtie-symbol-review"},
                {"icon_flame", "bowtie-symbol-flame"},
                {"icon_megaphone", "bowtie-symbol-ask"},
                {"icon_test_plan", "bowtie-test-plan"},
                {"icon_test_suite", "bowtie-test-suite"},
                {"icon_test_case", "bowtie-test-case"},
                {"icon_test_step", "bowtie-test-step"},
                {"icon_test_parameter", "bowtie-test-parameter"},
                {"icon_code_review", "bowtie-symbol-review-request"},
                {"icon_code_response", "bowtie-symbol-review-response"},
                {"icon_review", "bowtie-symbol-feedback-request"},
                {"icon_response", "bowtie-symbol-feedback-response"},
                {"icon_ribbon", "bowtie-symbol-ribbon"},
                {"icon_chart", "bowtie-symbol-finance"},
                {"icon_headphone", "bowtie-symbol-headphone"},
                {"icon_key", "bowtie-symbol-key"},
                {"icon_airplane", "bowtie-symbol-airplane"},
                {"icon_car", "bowtie-symbol-car"},
                {"icon_diamond", "bowtie-symbol-diamond"},
                {"icon_asterisk", "bowtie-symbol-asterisk"},
                {"icon_database_storage", "bowtie-symbol-storage-database"},
                {"icon_government", "bowtie-symbol-government"},
                {"icon_gavel", "bowtie-symbol-decision"},
                {"icon_parachute", "bowtie-symbol-parachute"},
                {"icon_paint_brush", "bowtie-symbol-paint-brush"},
                {"icon_palette", "bowtie-symbol-color-palette"},
                {"icon_gear", "bowtie-settings-gear"},
                {"icon_check_box", "bowtie-status-success-box"},
                {"icon_gift", "bowtie-package-fill"},
                {"icon_test_beaker", "bowtie-test-fill"},
                {"icon_broken_lightbulb", "bowtie-symbol-defect"},
                {"icon_clipboard_issue", "bowtie-symbol-issue"},
                {"icon_github", "bowtie-brand-github"},
                {"icon_pull_request", "bowtie-tfvc-pull-request"},
                {"icon_github_issue", "bowtie-status-error-outline"},
            };
        }

        public async Task Export()
        {
            try
            {
                using (var operation = _telemetryClient.StartOperation<RequestTelemetry>("export"))
                {
                    var timer = Stopwatch.StartNew();

                    if (_options.Path == null)
                    {
                        Log("Using current folder for export, -path is not set.");
                        _wiki =  new ExportedWikiDoc(Directory.GetCurrentDirectory());
                    }
                    _wiki = new ExportedWikiDoc(_options.Path);
                    

                    List<MarkdownFile> files = null;
                    if (!string.IsNullOrEmpty(_options.Single))
                    {
                        var filePath = Path.GetFullPath(_options.Single);
                        var directory = new DirectoryInfo(Path.GetFullPath(filePath));

                        if (!File.Exists(filePath))
                        {
                            Log($"Single-File [-s] {filePath} specified not found" + filePath, LogLevel.Error);
                            return;
                        }

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
                        files = ReadOrderFiles(_wiki.exportPath(), 0); // root level
                    }

                    _telemetryClient.TrackEvent("Pages", null, new Dictionary<string, double>() { { "Pages", files.Count } });

                    var html = ConvertMarkdownToHTML(files);

                    var htmlStart = "<!DOCTYPE html><html>";
                    var htmlEnd = "</html>";
                    var headStart = "<head>";

                    var footer = new List<string>();

                    var header = new List<string>();
                    header.Add("<meta http-equiv=Content-Type content=\"text/html; charset=utf-8\">");
                    var headEnd = "</head>";


                    if (_options.ConvertMermaid)
                    {
                        string mermaid = !string.IsNullOrEmpty(_options.MermaidJsPath) ?
                            $"<script>{File.ReadAllText(_options.MermaidJsPath)}</script>"
                            : @"<script src=""https://cdnjs.cloudflare.com/ajax/libs/mermaid/8.6.4/mermaid.min.js""></script>";

                        var mermaidInitialize = "<script>mermaid.initialize({ startOnLoad:true });</script>";

                        // adding the correct charset for unicode smileys and all that fancy stuff, and include mermaid.js
                        html = $"{html}{mermaid}{mermaidInitialize}";
                        header.Add(mermaid);
                        header.Add(mermaidInitialize);
                    }

                    if (_options.Math)
                    {
                        var katex = "<script src=\"https://cdn.jsdelivr.net/npm/katex@0.13.11/dist/katex.min.js\"></script><script src=\"https://cdn.jsdelivr.net/npm/katex@0.13.11/dist/contrib/auto-render.min.js\" onload=\"renderMathInElement(document.body, {delimiters: [{left: '$$', right: '$$', display: true},{left: '$', right: '$', display: true}]});\"></script>";
                        var katexCss = "<link rel=\"stylesheet\" href=\"https://cdn.jsdelivr.net/npm/katex@0.13.11/dist/katex.min.css\">";

                        header.Add(katexCss);
                        footer.Add(katex);
                    }

                    if (_options.HighlightCode)
                    {
                        string hightlightStyle = _options.HighlightStyle ?? "vs";
                        string hightlight = $@"<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.1.0/styles/{hightlightStyle}.min.css"">
                                                     <script src=""https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.1.0/highlight.min.js""></script>
                                                     ";
                        var hightlightInitialize = @"<script>hljs.highlightAll();</script>";

                        // Avoid default highlight.js style to create a white background
                        if (_options.HighlightStyle == null)
                            hightlightInitialize += @"<style>
                                                        .hljs {
                                                            background: #f0f0f0;
                                                        }
                                                        pre {
                                                            border-radius: 0px;
                                                        }
                                                    </style>";

                        //todo: add offline version of highlightjs 
                        header.Add(hightlight);
                        header.Add(hightlightInitialize);
                    }

                    if (_options.AzureDevopsOrganization != null)
                    {
                        VssCredentials credentials = !string.IsNullOrEmpty(_options.AzureDevopsPAT) ?
                        new VssBasicCredential(string.Empty, _options.AzureDevopsPAT)
                        : new VssClientCredentials();
                        VssConnection connection = new VssConnection(new Uri(_options.AzureDevopsOrganization), credentials);

                        // Create instance of WorkItemTrackingHttpClient using VssConnection
                        WorkItemTrackingHttpClient witClient = connection.GetClient<WorkItemTrackingHttpClient>();

                        string pattern = @"([>\b \r\n])(#[0-9]+)([<\b \r\n])";
                        html = Regex.Replace(html, pattern, match => match.Groups[1].Value
                                                                     + this.generateWorkItemLink(match.Groups[2].Value, witClient).Result
                                                                     + match.Groups[3].Value);
                    }

                    var cssPath = "";
                    if (string.IsNullOrEmpty(_options.CSS))
                    {
                        cssPath = "devopswikistyle.css";
                        Log("No CSS specified, using devopswikistyle.css", LogLevel.Information, 0);
                    }
                    else
                    {
                        cssPath = Path.GetFullPath(_options.CSS);
                        if (!File.Exists(cssPath))
                        {
                            Log($"CSS file does not exist at path {cssPath}", LogLevel.Warning);
                        }
                    }

                    if (File.Exists(cssPath))
                    {
                        var css = File.ReadAllText(cssPath);
                        var style = $"<style>{css}</style>";

                        //adding the css to the footer to overwrite the mermaid, katex, highlightjs styles. 
                        footer.Add(style);
                    }

                    //build the html for rendering
                    html = $"{htmlStart}{headStart}{string.Concat(header)}{headEnd}{html}<footer>{string.Concat(footer)}</footer>{htmlEnd}";

                    if (_options.Debug)
                    {
                        var htmlPath = string.Concat(_options.Output, ".html");
                        Log($"Writing converted html to path: {htmlPath}");
                        File.WriteAllText(htmlPath, html);
                    }

                    var path = await ConvertHTMLToPDFAsync(html);

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine();
                    Console.WriteLine($"Export done in {timer.Elapsed}");

                    _telemetryClient.StopOperation(operation);
                    _telemetryClient.Flush();

                    if (_options.Open)
                    {
                        Process fileopener = new Process();
                        fileopener.StartInfo.FileName = "explorer";
                        fileopener.StartInfo.Arguments = "\"" + path + "\"";
                        fileopener.Start();
                    }

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

        private async Task<string> ConvertHTMLToPDFAsync(string html)
        {
            Log("Converting HTML to PDF");
            var output = _options.Output;

            if (output == null)
            {
                output = Path.Combine(Directory.GetCurrentDirectory(), "export.pdf");
            }

            if (string.IsNullOrEmpty(_options.ChromeExecutablePath))
            {
                RevisionInfo revisionInfo = await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultChromiumRevision);
            }


            var launchOptions = new LaunchOptions
            {
                ExecutablePath = _options.ChromeExecutablePath ?? string.Empty,
                Headless = true, //set to false for easier debugging
                Args = new[] { "--no-sandbox", "--single-process" }, //required to launch in linux
                Devtools = false
            };

            using (var browser = await Puppeteer.LaunchAsync(launchOptions))
            {
                var page = await browser.NewPageAsync();
                await page.SetContentAsync(html);

                //todo load header/footer template from file
                var pdfoptions = new PdfOptions();
                if (!string.IsNullOrEmpty(_options.HeaderTemplate)
                || !string.IsNullOrEmpty(_options.FooterTemplate)
                || !string.IsNullOrEmpty(_options.HeaderTemplatePath)
                || !string.IsNullOrEmpty(_options.FooterTemplatePath))
                {

                    string footerTemplate = null;
                    string headerTemplate = null;
                    if (!string.IsNullOrEmpty(_options.HeaderTemplate))
                    {
                        headerTemplate = _options.HeaderTemplate;
                    }
                    else if (!string.IsNullOrEmpty(_options.HeaderTemplatePath))
                    {
                        headerTemplate = File.ReadAllText(_options.HeaderTemplatePath);
                    }

                    if (!string.IsNullOrEmpty(_options.FooterTemplate))
                    {
                        footerTemplate = _options.FooterTemplate;
                    }
                    else if (!string.IsNullOrEmpty(_options.FooterTemplatePath))
                    {
                        footerTemplate = File.ReadAllText(_options.FooterTemplatePath);
                    }

                    pdfoptions = new PdfOptions()
                    {
                        PrintBackground = true,
                        PreferCSSPageSize = false,
                        DisplayHeaderFooter = true,
                        MarginOptions = {
                        Top = "80px",
                        Bottom = "100px",
                        //left and right do not have an impact
                        Left = "100px",
                        Right = "100px"
                    },

                        Format = PuppeteerSharp.Media.PaperFormat.A4
                    };

                    pdfoptions.FooterTemplate = footerTemplate;
                    pdfoptions.HeaderTemplate = headerTemplate;

                }
                else
                {
                    pdfoptions.PrintBackground = _options.PrintBackground;
                }


                await page.PdfAsync(output, pdfoptions);
                await browser.CloseAsync();
            }

            Log($"PDF created at: {output}");
            return output;
        }

        //Replacing page parameters with dynamic values
        //[PAGE] and [PAGETO] do not need to be replaced because they are handled by the PDF converter
        private string ReplacePageParameters(string input)
        {
            Log("Replacing Page Parameters", LogLevel.Debug, 1);
            Log($"Input: {input}", LogLevel.Debug, 1);
            input = input.Replace("[DATE]", DateTime.Now.ToString("g"), true, CultureInfo.InvariantCulture);

            Log($"Output: {input}", LogLevel.Debug, 1);
            return input;
        }

        private string ConvertMarkdownToHTML(List<MarkdownFile> files)
        {
            Log("Converting Markdown to HTML");
            StringBuilder sb = new StringBuilder();

            //setup the markdown pipeline to support tables
            var pipelineBuilder = new MarkdownPipelineBuilder()
                .UsePipeTables()
                .UseEmojiAndSmiley()
                .UseAdvancedExtensions()
                .UseYamlFrontMatter()
                .UseTableOfContent(
                    tocAction: opt=>{ opt.ContainerTag="div"; opt.ContainerClass="toc"; }
                );

            //must be handled by us to have linking across files
            pipelineBuilder.Extensions.RemoveAll(x => x is Markdig.Extensions.AutoIdentifiers.AutoIdentifierExtension);
            //handled by katex
            pipelineBuilder.Extensions.RemoveAll(x => x is Markdig.Extensions.Mathematics.MathExtension);
            
            //todo: is this needed? it will stop support of resizing images:
            //this interferes with katex parsing of {} elements.
            //pipelineBuilder.Extensions.RemoveAll(x => x is Markdig.Extensions.GenericAttributes.GenericAttributesExtension);

            DeepLinkExtension deeplink = new DeepLinkExtension();
            pipelineBuilder.Extensions.Add(deeplink);

            if (_options.ConvertMermaid)
            {
                pipelineBuilder = pipelineBuilder.UseMermaidContainers();
            }

            for (var i = 0; i < files.Count; i++)
            {
                var mf = files[i];
                var file = new FileInfo(files[i].AbsolutePath);

                Log($"{file.Name}", LogLevel.Information, 1);
                var htmlfile = file.FullName.Replace(".md", ".html");

                if (!File.Exists(file.FullName))
                {
                    Log($"File {file.FullName} specified in the order file was not found and will be skipped!", LogLevel.Error, 1);
                    continue;
                }

                var markdownContent = File.ReadAllText(file.FullName);
                files[i].Content = markdownContent;
            }

            if (!string.IsNullOrEmpty(_options.GlobalTOC))
            {
                var firstMDFileInfo = new FileInfo(files[0].AbsolutePath);
                var directoryName = firstMDFileInfo.Directory.Name;
                var tocName = _options.GlobalTOC == "" ? directoryName : _options.GlobalTOC;
                var relativePath = "/" + tocName + ".md";
                var tocMDFilePath = new FileInfo(files[0].AbsolutePath).DirectoryName + relativePath;

                var contents = files.Select(x => x.Content).ToList();
                var tocContent = CreateGlobalTableOfContent(contents);
                var tocString = string.Join("\n", tocContent);

                var tocMarkdownFile = new MarkdownFile { AbsolutePath = tocMDFilePath, Level = 0, RelativePath = relativePath, Content = tocString };
                files.Insert(0, tocMarkdownFile);
            }

            for (var i = 0; i < files.Count; i++)
            {
                var mf = files[i];
                var file = new FileInfo(files[i].AbsolutePath);

                Log($"{file.Name}", LogLevel.Information, 1);

                var md = mf.Content;

                if (string.IsNullOrEmpty(md)) { 
                    Log($"File {file.FullName} is empty and will be skipped!", LogLevel.Warning, 1);
                    continue;}

                //rename TOC tags to fit to MarkdigToc or delete them from each markdown document
                var newTOCString = _options.GlobalTOC != null ? "" : "[TOC]"; 
                md = md.Replace("[[_TOC_]]", newTOCString);

                // remove scalings from image links, width & height: file.png =600x500
                var regexImageScalings = @"\((.[^\)]*?[png|jpg|jpeg]) =(\d+)x(\d+)\)";
                md = Regex.Replace(md, regexImageScalings, @"($1){width=$2 height=$3}");

                // remove scalings from image links, width only: file.png =600x
                regexImageScalings = @"\((.[^\)]*?[png|jpg|jpeg]) =(\d+)x\)";
                md = Regex.Replace(md, regexImageScalings, @"($1){width=$2}");

                // remove scalings from image links, height only: file.png =x600
                regexImageScalings = @"\((.[^\)]*?[png|jpg|jpeg]) =x(\d+)\)";
                md = Regex.Replace(md, regexImageScalings, @"($1){height=$2}");

                // determine the correct nesting of pages and related chapters
                pipelineBuilder.BlockParsers.Replace<HeadingBlockParser>(new OffsetHeadingBlockParser(mf.Level + 1));

                //update the deeplinking
                deeplink.Filename = Path.GetFileNameWithoutExtension(file.FullName);

                var pipeline = pipelineBuilder.Build();

                //parse the markdown document so we can alter it later
                var document = Markdown.Parse(md, pipeline);

                if (_options.NoFrontmatter)
                {
                    RemoveFrontmatter(document);
                }

                if (!string.IsNullOrEmpty(_options.Filter))
                {
                    if (!PageMatchesFilter(document))
                    {
                        Log("Page does not have correct tags - skip", LogLevel.Information, 3);
                        continue;
                    }
                    else
                    {
                        Log("Page tags match the provided filter", LogLevel.Information, 3);
                    }
                }


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

                if (!string.IsNullOrEmpty(_options.GlobalTOC) && i == 0)
                {
                    html = RemoveDuplicatedHeadersFromGlobalTOC(html);
                    Log($"Removed duplicated headers from toc html", LogLevel.Information, 1);
                }

                //add html anchor
                var anchorPath = file.FullName.Substring(_wiki.exportPath().Length);
                anchorPath = anchorPath.Replace("\\", "");
                anchorPath = anchorPath.ToLower();
                anchorPath = anchorPath.Replace(".md", "");

                var relativePath = file.FullName.Substring(_wiki.exportPath().Length);

                var anchor = $"<a id=\"{anchorPath}\">&nbsp;</a>";

                Log($"Anchor: {anchorPath}", LogLevel.Information, 3);

                html = anchor + html;

                if (_options.PathToHeading)
                {
                    var filename = file.Name;
                    filename = HttpUtility.UrlDecode(relativePath);
                    var heading = $"<b>{filename}</b>";
                    html = heading + html;
                }


                if (!string.IsNullOrEmpty(_options.GlobalTOC) && i == 0 && !_options.Heading)
                {
                    var heading = $"<h1>{_options.GlobalTOC}</h1>";
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
                        Log("----------------------", LogLevel.Information);
                        Log("Adding new page to PDF", LogLevel.Information);
                        Log("----------------------", LogLevel.Information);
                        html = "<div style='page-break-after: always;'>" + html + "</div>";
                    }
                }

                if (_options.Debug)
                {
                    Log($"html:\n{html}", LogLevel.Debug, 1);
                }
                sb.Append(html);
            }

            var result = sb.ToString();

            return result;
        }

        internal string RemoveDuplicatedHeadersFromGlobalTOC(string html)
        {
            var result = Regex.Replace(html, @"^ *<h[123456].*>.*<\/h[123456]> *\n?$", "", RegexOptions.Multiline);
            result = result.Trim('\n');
            return result;
        }

        internal List<string> CreateGlobalTableOfContent(List<string> contents)
        {
            var headers = new List<string>();
            var filteredContentList = RemoveCodeSections(contents);

            foreach (var content in filteredContentList) 
            {
                var headerMatches = Regex.Matches(content, "^ *#+ ?[^#].*$", RegexOptions.Multiline);
                headers.AddRange(headerMatches.Select(x => x.Value.Trim()));
            }

            if (!headers.Any())
                return new List<string>(); // no header -> no toc

            var tocContent = new List<string> { "[TOC]" }; // MarkdigToc style
            tocContent.AddRange(headers);
            return tocContent;
        }

        private List<string> RemoveCodeSections(List<string> contents)
        {
            var contentWithoutCode = new List<string>();
            for(var i=0; i < contents.Count; i++)
            {
                var contentWithoutCodeSection = Regex.Replace(contents[i], "^[ \t]*(```|~~~)[^`]*(```|~~~)", "", RegexOptions.Multiline);
                contentWithoutCode.Add(contentWithoutCodeSection);
            }
            return contentWithoutCode;
        }

        private MarkdownDocument RemoveFrontmatter(MarkdownDocument document)
        {
            var frontmatter = document.Descendants<YamlFrontMatterBlock>().FirstOrDefault();

            if (frontmatter != null)
            {
                document.Remove(frontmatter);
                Log($"Removed Frontmatter/Yaml tags", LogLevel.Information, 1);
            }
            return document;
        }

        private bool PageMatchesFilter(MarkdownObject document)
        {
            if (!string.IsNullOrEmpty(_options.Filter))
            {
                Log($"Filter provided: {_options.Filter}", LogLevel.Information, 2);

                var filters = _options.Filter.Split(",");
                var frontmatter = document.Descendants<YamlFrontMatterBlock>().FirstOrDefault();

                if (frontmatter == null)
                {
                    Log($"Page has no frontmatter tags", LogLevel.Information, 2);
                    return false;
                }

                var frontmatterTags = new List<string>();
                var lastTag = "";
                foreach (StringLine frontmatterline in frontmatter.Lines)
                {

                    var splice = frontmatterline.Slice.ToString();
                    var split = splice.Split(":");

                    //title:test or <empty>:test2 or tags:<empty>
                    if (split.Length == 2)
                    {
                        //title:
                        if (string.IsNullOrEmpty(split[1]))
                        {
                            lastTag = split[0].Trim();
                        }
                        //title:test
                        else if (!string.IsNullOrEmpty(split[0]))
                        {
                            frontmatterTags.Add($"{split[0].Trim()}:{split[1].Trim()}");
                        }
                        //:test2
                        else
                        {
                            frontmatterTags.Add($"{lastTag}:{split[1].Trim()}");
                        }
                    }
                    else if (split.Length == 1 && !string.IsNullOrEmpty(split[0]))
                    {
                        frontmatterTags.Add($"{lastTag}:{split[0].Trim().Substring(2)}");
                    }
                }

                foreach (var filter in filters)
                {
                    if (frontmatterTags.Contains(filter, StringComparer.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
                return false;
            }
            return true;
        }

        private string RenameTableOfContent(string document)
        {
            if (document.Contains("TOC"))
                document = document.Replace("[[_TOC_]]", "[TOC]"); // MarkdigToc styled. See https://github.com/leisn/MarkdigToc
            return document;
        }

        public void CorrectLinksAndImages(MarkdownObject document, FileInfo file, MarkdownFile mf)
        {
            Log("Correcting Links and Images", LogLevel.Information, 2);
            // walk the document node tree and replace relative image links
            // and relative links to markdown pages
            foreach (var link in document.Descendants().OfType<LinkInline>())
            {
                if (link.Url != null)
                {
                    if (!link.Url.StartsWith("http"))
                    {
                        string absPath = null;
                        string anchor = null;

                        //handle --attachments-path case
                        if (
                            !string.IsNullOrEmpty(this._options.AttachmentsPath) && 
                            (link.Url.StartsWith("/.attachments") || link.Url.StartsWith(".attachments"))
                        )
                        {
                            var linkUrl = link.Url.Split('/').Last();

                            //urls could be encoded and contain spaces - they are then not found on disk
                            linkUrl = HttpUtility.UrlDecode(linkUrl);

                            absPath = Path.GetFullPath(Path.Combine(this._options.AttachmentsPath, linkUrl));
                        }
                        else if (link.Url.StartsWith("/"))
                        {
                            // Add URL replacements here
                            var replacements = new Dictionary<string, string>(){
                                {":", "%3A"},
                                {"#", "-"},
                                {"%20", " "}
                            };
                            var linkUrl = link.Url;
                            replacements.ForEach(p => {
                                linkUrl = linkUrl.Replace(p.Key, p.Value);
                            });
                            absPath = Path.GetFullPath(_wiki.basePath() + linkUrl);
                        }
                        else
                        {
                            var split =  link.Url.Split("#");
                            var linkUrl = split[0];
                            anchor = split.Length > 1 ? split[1] : null;
                            absPath = Path.GetFullPath(file.Directory.FullName + "/" + linkUrl);
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
                            //convert images to base64 and embed them in the html. Chrome/Puppeter does not show local files because of security reasons.
                            Byte[] bytes = File.ReadAllBytes(fileInfo.FullName);
                            String base64 = Convert.ToBase64String(bytes);

                            link.Url = $"data:image/{fileInfo.Extension};base64,{base64}";
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

                            //remove anchor
                            relPath = relPath.Split("#")[0];

                            
                            relPath = relPath.Replace("/", "\\");
                            // remove relative part if we are not exporting from the root of the wiki
                            var pathBelowRootWiki = _wiki.exportPath().Replace(_wiki.basePath(), ""); 
                            if( !pathBelowRootWiki.IsNullOrEmpty())
                                relPath = relPath.Replace(pathBelowRootWiki, "");
                            relPath = relPath.Replace("\\", "");
                            relPath = relPath.Replace(".md", "");
                            relPath = relPath.ToLower();
                            Log($"Markdown link: {relPath}", LogLevel.Information, 2);
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
            Log($"Reading .order file in directory {path}");
            var orderFiles = directory.GetFiles(".order", SearchOption.TopDirectoryOnly);

            if (orderFiles.Count() > 0)
            { Log("Order file found", LogLevel.Debug, 1); }

            var result = new List<MarkdownFile>();
            foreach (var orderFile in orderFiles)
            {
                var orders = File.ReadAllLines(orderFile.FullName);
                { Log($"Pages: {orders.Count()}", LogLevel.Information, 2); }
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
                    mf.AbsolutePath = Path.Combine(orderFile.Directory.FullName, $"{order}.md");
                    mf.RelativePath = $"{relativePath}";
                    mf.Level = level;
                    result.Add(mf);

                    { Log($"Adding page: {mf.AbsolutePath}", LogLevel.Information, 2); }

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

        private void Log(string msg, LogLevel logLevel = LogLevel.Information, int indent = 0)
        {
            var indentString = new string(' ', indent * 2);
            if (_options.Debug && logLevel == LogLevel.Debug)
            {
                Console.WriteLine(indentString + msg);
            }

            if (_options.Verbose && logLevel == LogLevel.Information)
            {
                Console.WriteLine(indentString + msg);
            }

            if (logLevel == LogLevel.Warning)
            {
                var color = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(indentString + $"WARN: {msg}");
                Console.ForegroundColor = color;
            }

            if (logLevel == LogLevel.Error)
            {
                var color = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(indentString + $"ERR: {msg}");
                Console.ForegroundColor = color;
            }
        }
        async private Task<string> generateWorkItemLink(string stringId, WorkItemTrackingHttpClient witClient)
        {
            int id;
            try
            {
                id = Int32.Parse(stringId.Replace("#", ""));
            }
            catch (FormatException)
            {
                Console.WriteLine($"Unable to parse '{stringId}'");
                return stringId;
            }

            WorkItem workItem = await witClient.GetWorkItemAsync(id, expand: WorkItemExpand.All);
            WorkItemType type = await witClient.GetWorkItemTypeAsync(workItem.Fields["System.TeamProject"].ToString(),
                                                                        workItem.Fields["System.WorkItemType"].ToString());

            string childColor = type.Color;
            string childIcon = this._iconClass[type.Icon.Id];
            string url = ((ReferenceLink)workItem.Links.Links["html"]).Href;
            string title = workItem.Fields["System.Title"].ToString();
            string state = workItem.Fields["System.State"].ToString();
            string stateColor = type.States.First(s => s.Name == state).Color;

            return $@"
            <span class=""mention-widget-workitem"" style=""border-left-color: #{childColor};"">
                <a class=""mention-link mention-wi-link mention-click-handled"" href=""{url}"">
                    <span class=""work-item-type-icon-host"">
                    <i class=""work-item-type-icon bowtie-icon {childIcon}"" role=""figure"" style=""color: #{childColor};""></i>
                    </span>
                    <span class=""secondary-text"">{workItem.Id}</span>
                    <span class=""mention-widget-workitem-title fontWeightSemiBold"">{title}</span>
                </a>
                <span class=""mention-widget-workitem-state"">
                    <span class=""workitem-state-color"" style=""background-color: #{stateColor};""></span>
                    <span>{state}</span>
                </span>
            </span>
            ";
        }
    }

    public class MarkdownFile
    {
        public string AbsolutePath;
        public string RelativePath;
        public int Level;
        public string Content;

        public override string ToString()
        {
            return $"[{Level}] {AbsolutePath}";
        }
    }
}
