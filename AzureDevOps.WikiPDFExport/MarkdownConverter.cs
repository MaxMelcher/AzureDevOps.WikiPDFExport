using azuredevops_export_wiki.MermaidContainer;
using Markdig;
using Markdig.Extensions.Yaml;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.Common;
using Microsoft.VisualStudio.Services.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace azuredevops_export_wiki
{
    internal class MarkdownConverter : ILogger
    {
        private readonly ILogger _logger;
        private readonly Options _options;
        private readonly ExportedWikiDoc _wiki;

        internal MarkdownConverter(ExportedWikiDoc wiki, Options options, ILogger logger)
        {
            _wiki = wiki;
            _options = options;
            _logger = logger;
        }

        internal string ConvertToHTML(IList<MarkdownFile> files)
        {
            Log("Converting Markdown to HTML");
            StringBuilder sb = new();

            //setup the markdown pipeline to support tables
            var pipelineBuilder = new MarkdownPipelineBuilder()
                .UsePipeTables()
                .UseEmojiAndSmiley()
                .UseAdvancedExtensions()
                .UseYamlFrontMatter()
                .UseTableOfContent(
                    tocAction: opt => { opt.ContainerTag = "div"; opt.ContainerClass = "toc"; }
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

                if (string.IsNullOrEmpty(md))
                {
                    Log($"File {file.FullName} is empty and will be skipped!", LogLevel.Warning, 1);
                    continue;
                }

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
                    // add break at the end of each page except the last one
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
            }//for

            var result = sb.ToString();

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
            for (var i = 0; i < contents.Count; i++)
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
                            replacements.ForEach(p =>
                            {
                                linkUrl = linkUrl.Replace(p.Key, p.Value);
                            });
                            absPath = Path.GetFullPath(_wiki.basePath() + linkUrl);
                        }
                        else
                        {
                            var split = link.Url.Split("#");
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
                            if (!pathBelowRootWiki.IsNullOrEmpty())
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

        internal string RemoveDuplicatedHeadersFromGlobalTOC(string html)
        {
            var result = Regex.Replace(html, @"^ *<h[123456].*>.*<\/h[123456]> *\n?$", "", RegexOptions.Multiline);
            result = result.Trim('\n');
            return result;
        }

        public void Log(string msg, LogLevel logLevel = LogLevel.Information, int indent = 0)
        {
            _logger.Log(msg, logLevel, indent);
        }
    }
}
