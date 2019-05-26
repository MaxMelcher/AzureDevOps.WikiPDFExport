using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using CommonMark;
using CommonMark.Syntax;
using DinkToPdf;

namespace azuredevops_export_wiki
{
    public class WikiPDFExporter : IWikiPDFExporter
    {
        private Options _options;
        private string _path;

        public WikiPDFExporter(Options options)
        {
            _options = options;
        }

        public void Export()
        {
            var timer = Stopwatch.StartNew();

            Console.WriteLine("Reading .order file");

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

            var files = ReadOrderFiles(_path);
            var html = ConvertMarkdownToHTML(files);

            ConvertHTMLToPDF(html);

            Console.WriteLine($"Export done in {timer.Elapsed}");
        }

        private void ConvertHTMLToPDF(string html)
        {
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
                        HeaderSettings = { FontSize = 9, Right = "Page [page] of [toPage]", Line = true, Spacing = 2.812},
                        FooterSettings = { Left = $"{DateTime.Now.ToString("g")}"},
                        UseLocalLinks = true
                    }
                }
            };

            converter.Convert(doc);
            Log($"PDF created at: {output}");
        }

        private string ConvertMarkdownToHTML(List<string> files)
        {
            StringBuilder sb = new StringBuilder();
            for (var i = 0; i < files.Count; i++)
            {
                var file = new FileInfo(files[i]);
                Log($"file {file.Name}");
                var htmlfile = file.FullName.Replace(".md", ".html");

                var md = File.ReadAllText(file.FullName);
                var document = CommonMark.CommonMarkConverter.Parse(md);

                // walk the document node tree and replace relative image links
                //and relative links to markdown pages
                foreach (var node in document.AsEnumerable())
                {
                    if (
                        node.IsOpening
                        && node.Inline != null
                        && node.Inline.Tag == InlineTag.Image)
                    {
                        if (!node.Inline.TargetUrl.StartsWith("http"))
                        {
                            var path = Path.Combine(file.Directory.FullName, node.Inline.TargetUrl);
                            node.Inline.TargetUrl = $"file:///{path}";
                        }
                    }

                    if (
                        node.IsOpening
                        && node.Inline != null
                        && node.Inline.Tag == InlineTag.Link)
                    {
                        if (!node.Inline.TargetUrl.StartsWith("http") && node.Inline.TargetUrl.EndsWith(".md"))
                        {
                            var path = Path.Combine(file.Directory.FullName, node.Inline.TargetUrl);
                            var relPath = path.Substring(_path.Length);
                            relPath = relPath.Replace("/", "\\");
                            relPath = relPath.Replace("\\", "");
                            relPath = relPath.Replace(".", "-");
                            relPath = relPath.ToLower();
                            node.Inline.TargetUrl = $"#{relPath}";
                        }
                    }
                }

                string html = null;
                using (var writer = new System.IO.StringWriter())
                {
                    // write the HTML output
                    CommonMarkConverter.ProcessStage3(document, writer);
                    html = writer.ToString();
                    Log(html);
                }

                //add html anchor
                var relativePath = file.FullName.Substring(_path.Length);
                relativePath = relativePath.Replace("\\", "");
                relativePath = relativePath.Replace(".", "-");
                relativePath = relativePath.ToLower();
                var anchor = $"<span id=\"{relativePath}\">{relativePath}</span><h1>{file.Name.Replace(".md","")}</h1>";
                html = anchor + html;

                if (_options.BreakPage)
                {
                    //if not one the last page
                    if (i + 1 < files.Count)
                    {
                        Log("Adding new page to PDF");
                        html = "<div style='page-break-after: always;'>" + html + "</div>";
                    }
                }

                Log($"html:\n{html}");
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

        private List<string> ReadOrderFiles(string path)
        {
            var directory = new DirectoryInfo(Path.GetFullPath(path));
            var orderFiles = directory.GetFiles(".order", SearchOption.AllDirectories);

            var result = new List<string>();
            foreach (var orderFile in orderFiles)
            {
                var orders = File.ReadAllLines(orderFile.FullName);

                foreach (var order in orders)
                {
                    result.Add($"{orderFile.Directory.FullName}\\{order}.md");
                }
            }

            return result;
        }

        private void Log(string msg)
        {
            if (_options.Verbose)
            {
                Console.WriteLine(msg);
            }
        }
    }
}
