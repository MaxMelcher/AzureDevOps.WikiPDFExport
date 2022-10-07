using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Client;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Process = System.Diagnostics.Process;

[assembly: InternalsVisibleTo("AzureDevOps.WikiPDFExport.Test")]

namespace azuredevops_export_wiki
{
    public class WikiPDFExporter : IWikiPDFExporter, ILogger
    {
        private readonly ILoggerExtended _logger;
        private readonly Options _options;
        private readonly TelemetryClient _telemetryClient;
        private ExportedWikiDoc _wiki;
        private readonly Dictionary<string, string> _iconClass;

        public WikiPDFExporter(Options options, ILoggerExtended logger)
        {
            _options = options;
            _logger = logger;

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

        public async Task<bool> Export()
        {
            bool succeeded = true;
            try
            {
                using (var operation = _telemetryClient.StartOperation<RequestTelemetry>("export"))
                {
                    var timer = Stopwatch.StartNew();

                    if (_options.Path == null)
                    {
                        Log("Using current folder for export, -path is not set.");
                        _wiki = new ExportedWikiDoc(Directory.GetCurrentDirectory());
                    }
                    _wiki = new ExportedWikiDoc(_options.Path);

                    IWikiDirectoryScanner scanner = !string.IsNullOrEmpty(_options.Single)
                        ? new SingleFileScanner(_options.Single, _logger)
                        : (_options.IncludeUnlistedPages
                            ? new WikiDirectoryScanner(_wiki.exportPath(), _options, _logger)
                            : new WikiOptionFilesScanner(_wiki.exportPath(), _options, _logger));

                    IList<MarkdownFile> files = scanner.Scan();

                    Log($"Found {files.Count} total pages to process");

                    _telemetryClient.TrackEvent("Pages", null, new Dictionary<string, double>() { { "Pages", files.Count } });

                    var converter = new MarkdownConverter(_wiki, _options, _logger);
                    var html = converter.ConvertToHTML(files);

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
                                                                     + this.GenerateWorkItemLink(match.Groups[2].Value, witClient).Result
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
                    html = $"{htmlStart}{headStart}{string.Concat(header)}{headEnd}<body>{html}<footer>{string.Concat(footer)}</footer></body>{htmlEnd}";

#if HTML_IN_MEMORY
                    if (_options.Debug)
                    {
                        var htmlPath = string.Concat(_options.Output, ".html");
                        Log($"Writing converted html to path: {htmlPath}");
                        File.WriteAllText(htmlPath, html);
                    }
                    var generator = new PDFGenerator(_options, _logger);
                    var path = await generator.ConvertHTMLToPDFAsync(html);
#else
                    using var tempHtmlFile = new SelfDeletingTemporaryFile(html.Length, "html");
                    File.WriteAllText(tempHtmlFile.FilePath, html);
                    html = string.Empty;
                    var generator = new PDFGenerator(_options, _logger);
                    var path = await generator.ConvertHTMLToPDFAsync(tempHtmlFile);
                    if (_options.Debug)
                    {
                        var htmlPath = string.Concat(_options.Output, ".html");
                        Log($"Writing converted html to path: {htmlPath}");
                        tempHtmlFile.KeepAs(htmlPath);
                        Log($"HTML saved.");
                    }
#endif

                    _logger.LogMeasure($"Export done in {timer.Elapsed}");

                    _telemetryClient.StopOperation(operation);

                    if (_options.Open)
                    {
                        Process fileopener = new Process();
                        fileopener.StartInfo.FileName = "explorer";
                        fileopener.StartInfo.Arguments = "\"" + Path.GetFullPath(path) + "\"";
                        fileopener.Start();
                    }
                }
            }
            catch (Exception ex)
            {
                succeeded = false;
                Log($"Something bad happend.\n{ex}", LogLevel.Error);
                _telemetryClient.TrackException(ex);
            }
            finally
            {
                _telemetryClient.Flush();
                Thread.Sleep(TimeSpan.FromSeconds(5));
            }

            return succeeded;
        }

        async private Task<string> GenerateWorkItemLink(string stringId, WorkItemTrackingHttpClient witClient)
        {
            int id;
            try
            {
                id = Int32.Parse(stringId.Replace("#", ""));
            }
            catch (FormatException)
            {
                Log($"Unable to parse work item id '{stringId}'", LogLevel.Warning);
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

        public void Log(string msg, LogLevel logLevel = LogLevel.Information, int indent = 0)
        {
            _logger.Log(msg, logLevel, indent);
        }
    }
}
