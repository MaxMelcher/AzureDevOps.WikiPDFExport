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

        private string ConvertMarkdownToHTML(List<MarkdownFile> files)
        {
            StringBuilder sb = new StringBuilder();
            for (var i = 0; i < files.Count; i++)
            {
                var mf = files[i];
                var file = new FileInfo(files[i].AbsolutePath);
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
                        //if the link is not a link pointing to a web resource, 
                        //try to resolve it within the wiki repository
                        if (!node.Inline.TargetUrl.StartsWith("http"))
                        {
                            string absPath = file.Directory.FullName + "/" + node.Inline.TargetUrl.Replace("/", "\\");

                            //the file is a markdown file, create a link to it
                            var isMarkdown = false;
                            var fileInfo = new FileInfo(absPath);
                            if (fileInfo.Exists && fileInfo.Extension.Equals(".md", StringComparison.InvariantCultureIgnoreCase))
                            {
                                isMarkdown = true;
                            }

                            fileInfo = new FileInfo($"{absPath}.md");
                            if (fileInfo.Exists && fileInfo.Extension.Equals(".md", StringComparison.InvariantCultureIgnoreCase))
                            {
                                isMarkdown = true;
                            }

                            //only markdown files get a pdf internal link
                            if (isMarkdown)
                            {
                                var relPath = mf.RelativePath + "\\" + node.Inline.TargetUrl;
                                relPath = relPath.Replace("/", "\\");
                                relPath = relPath.Replace("\\", "");

                                relPath = relPath.ToLower();

                                node.Inline.TargetUrl = $"#{relPath}";
                            }
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

                relativePath = relativePath.ToLower();
                var anchor = $"<span id=\"{relativePath}\">{relativePath}</span><h1>{file.Name.Replace(".md", "")}</h1>";
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



        private List<MarkdownFile> ReadOrderFiles(string path)
        {
            var directory = new DirectoryInfo(Path.GetFullPath(path));
            var orderFiles = directory.GetFiles(".order", SearchOption.AllDirectories);

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

        public class MarkdownFile
        {
            public string AbsolutePath;
            public string RelativePath;
        }
    }


}
