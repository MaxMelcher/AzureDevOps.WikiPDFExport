using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using AzureDevOps.WikiPdfExport.MermaidContainer;
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

namespace AzureDevOps.WikiPdfExport;

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

	internal string ConvertToHtml(IList<MarkdownFile> files)
	{
		Log("Converting Markdown to HTML");
		StringBuilder sb = new();

		// setup the markdown pipeline to support tables
		var pipelineBuilder = new MarkdownPipelineBuilder()
			.UsePipeTables()
			.UseEmojiAndSmiley()
			.UseAdvancedExtensions()
			.UseYamlFrontMatter()
			.UseTableOfContent(
				tocAction: opt => { opt.ContainerTag = "div"; opt.ContainerClass = "toc"; }
			);

		// must be handled by us to have linking across files
		_ = pipelineBuilder.Extensions.RemoveAll(x => x is Markdig.Extensions.AutoIdentifiers.AutoIdentifierExtension);
		// handled by katex
		_ = pipelineBuilder.Extensions.RemoveAll(x => x is Markdig.Extensions.Mathematics.MathExtension);

		// todo: is this needed? it will stop support of resizing images:
		// this interferes with katex parsing of {} elements.
		// pipelineBuilder.Extensions.RemoveAll(x => x is Markdig.Extensions.GenericAttributes.GenericAttributesExtension);

		var deeplink = new DeepLinkExtension();
		pipelineBuilder.Extensions.Add(deeplink);

		if (_options.ConvertMermaid)
		{
			pipelineBuilder = pipelineBuilder.UseMermaidContainers();
		}

		if (!string.IsNullOrEmpty(_options.GlobalToc))
		{
			if (_options.GlobalTocPosition > files.Count)
			{
				_options.GlobalTocPosition = files.Count;
			}

			var firstMarkdownFileInfo = files[_options.GlobalTocPosition].FileInfo;
			if (firstMarkdownFileInfo.Directory is null)
			{
				throw new UnreachableException("Files are always in directories.");
			}
			var directoryName = firstMarkdownFileInfo.Directory.Name;
			var tocName = string.IsNullOrEmpty(_options.GlobalToc) ? directoryName : _options.GlobalToc;
			var relativePath = "/" + tocName + ".md";
			var tocMarkdownFilePath = firstMarkdownFileInfo.DirectoryName + relativePath;

			var contents = files.Select(x => x.Content).ToList();
			var tocContent = CreateGlobalTableOfContent(contents);
			var tocString = string.Join("\n", tocContent);
			var tocMarkdownFile = new MarkdownFile(new FileInfo(tocMarkdownFilePath), relativePath, 0, relativePath, tocString);

			files.Insert(_options.GlobalTocPosition, tocMarkdownFile);
		}

		for (var i = 0; i < files.Count; i++)
		{
			var mf = files[i];
			var file = mf.FileInfo;

			Log($"{file.Name}", LogLevel.Information, 1);

			var md = mf.Content;

			if (string.IsNullOrEmpty(md))
			{
				Log($"File {file.FullName} is empty and will be skipped!", LogLevel.Warning, 1);
				continue;
			}

			// rename TOC tags to fit to MarkdigToc or delete them from each markdown document
			var newTocString = _options.GlobalToc != null ? "" : "[TOC]";
			md = md.Replace("[[_TOC_]]", newTocString, StringComparison.Ordinal);

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
			_ = pipelineBuilder.BlockParsers.Replace<HeadingBlockParser>(new OffsetHeadingBlockParser(mf.Level + 1));

			// update the deeplinking
			deeplink.Filename = Path.GetFileNameWithoutExtension(file.FullName);

			var pipeline = pipelineBuilder.Build();

			// parse the markdown document so we can alter it later
			var document = Markdown.Parse(md, pipeline);

			if (_options.NoFrontmatter)
			{
				_ = RemoveFrontmatter(document);
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


			// adjust the links
			CorrectLinksAndImages(document, mf);

			string? html = null;
			var builder = new StringBuilder();
			using (var writer = new StringWriter(builder))
			{
				// write the HTML output
				var renderer = new HtmlRenderer(writer);
				pipeline.Setup(renderer);
				_ = renderer.Render(document);
			}
			html = builder.ToString();

			if (!string.IsNullOrEmpty(_options.GlobalToc) && i == _options.GlobalTocPosition)
			{
				html = RemoveDuplicatedHeadersFromGlobalToc(html);
				Log($"Removed duplicated headers from toc html", LogLevel.Information, 1);
			}

			// TODO how is this different from data in MarkdownFile?
			// add html anchor
			var anchorPath = file.FullName[_wiki.ExportPath().Length..];
			anchorPath = anchorPath.Replace("\\", "", StringComparison.Ordinal);
#pragma warning disable CA1308 // Normalize strings to uppercase
			anchorPath = anchorPath.ToLowerInvariant();
#pragma warning restore CA1308 // Normalize strings to uppercase
			anchorPath = anchorPath.Replace(".md", "", StringComparison.Ordinal);

			var relativePath = file.FullName[_wiki.ExportPath().Length..];

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


			if (!string.IsNullOrEmpty(_options.GlobalToc) && i == _options.GlobalTocPosition && !_options.Heading)
			{
				var heading = $"<h1>{_options.GlobalToc}</h1>";
				html = heading + html;
			}

			if (_options.Heading)
			{
				var filename = file.Name.Replace(".md", "", StringComparison.Ordinal);
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
					_ = title.Replace(filenameEscape.Key, filenameEscape.Value);
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
			_ = sb.Append(html);
		}

		var result = sb.ToString();

		return result;
	}

	internal static List<string> CreateGlobalTableOfContent(List<string> contents)
	{
		var headers = new List<string>();
		var filteredContentList = RemoveCodeSections(contents);

		foreach (var content in filteredContentList)
		{
			var headerMatches = Regex.Matches(content, "^ *#+ ?[^#].*$", RegexOptions.Multiline);
			headers.AddRange(headerMatches.Select(x => x.Value.Trim()));
		}

		if (headers.Count == 0)
		{
			return []; // no header -> no toc
		}

		var tocContent = new List<string> { "[TOC]" }; // MarkdigToc style
		tocContent.AddRange(headers);
		return tocContent;
	}

	private static List<string> RemoveCodeSections(List<string> contents)
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
			_ = document.Remove(frontmatter);
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

			if (frontmatter is null)
			{
				Log($"Page has no frontmatter tags", LogLevel.Information, 2);
				return false;
			}

			var frontmatterTags = new List<string>();
			var lastTag = "";
			foreach (var frontmatterline in frontmatter.Lines.Cast<StringLine>())
			{

				var splice = frontmatterline.Slice.ToString();
				var split = splice.Split(":");

				// title:test or <empty>:test2 or tags:<empty>
				if (split.Length == 2)
				{
					// title:
					if (string.IsNullOrEmpty(split[1]))
					{
						lastTag = split[0].Trim();
					}
					// title:test
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
					frontmatterTags.Add($"{lastTag}:{split[0].Trim()[2..]}");
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

	public void CorrectLinksAndImages(MarkdownObject document, MarkdownFile mf)
	{
		Log("Correcting Links and Images", LogLevel.Information, 2);
		var file = mf.FileInfo;
		// walk the document node tree and replace relative image links
		// and relative links to markdown pages
		foreach (var link in document.Descendants().OfType<LinkInline>())
		{
			if (link.Url != null && !link.Url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
			{
				// handle --attachments-path case
				(var absolutePath, var isBase64) = HandleAttachmentsPath(file, link.Url);

				// the file is a markdown file, create a link to it
				var isMarkdown = false;
				if (!isBase64)
				{
					var fileInfo = new FileInfo(absolutePath);
					if (fileInfo.Exists && fileInfo.Extension.Equals(".md", StringComparison.OrdinalIgnoreCase))
					{
						isMarkdown = true;
					}
					else if (fileInfo.Exists)
					{
						// convert images to base64 and embed them in the html. Chrome/Puppeter does not show local files because of security reasons.
						var bytes = File.ReadAllBytes(fileInfo.FullName);
						var base64 = Convert.ToBase64String(bytes);
						link.Url = $"data:image/{(fileInfo.Extension == ".svg" ? "svg+xml" : fileInfo.Extension)};base64,{base64}";
					}

					fileInfo = new FileInfo($"{absolutePath}.md");
					if (fileInfo.Exists && fileInfo.Extension.Equals(".md", StringComparison.OrdinalIgnoreCase))
					{
						isMarkdown = true;
					}
				}

				// only markdown files get a pdf internal link
				if (isMarkdown)
				{
					var relPath = mf.RelativePath + "\\" + link.Url;

					// remove anchor
					relPath = relPath.Split("#")[0];

					relPath = relPath.Replace("/", "\\", StringComparison.Ordinal);
					// remove relative part if we are not exporting from the root of the wiki
					var pathBelowRootWiki = _wiki.ExportPath().Replace(_wiki.BasePath(), "", StringComparison.Ordinal);
					if (!pathBelowRootWiki.IsNullOrEmpty())
					{
						relPath = relPath.Replace(pathBelowRootWiki, "", StringComparison.Ordinal);
					}

					relPath = relPath.Replace("\\", "", StringComparison.Ordinal);
					relPath = relPath.Replace(".md", "", StringComparison.Ordinal);
#pragma warning disable CA1308 // Normalize strings to uppercase
					relPath = relPath.ToLowerInvariant();
#pragma warning restore CA1308 // Normalize strings to uppercase
					Log($"Markdown link: {relPath}", LogLevel.Information, 2);
					link.Url = $"#{relPath}";
				}
			}
			CorrectLinksAndImages(link, mf);
		}
	}

	private (string? absolutePath, bool isBase64) HandleAttachmentsPath(FileInfo file, string url)
	{
		if (IsAttachments(url))
		{
			return (GetAttachmentsPathRaw(url), false);
		}
		else if (IsRoot(url))
		{
			var absolutePath = GetAttachmentsPathRoot(url);
			return (absolutePath, false);
		}
		else if (IsData(url))
		{
			return (null, true);
		}
		else
		{
			var absolutePath = GetAttachmentsPathLocalFile(file, url);
			return (absolutePath, false);
		}
	}

	private static string GetAttachmentsPathLocalFile(FileInfo file, string url)
	{
		if (file.Directory is null)
		{
			throw new UnreachableException("Files are always in directories.");
		}
		var withoutFragment = url.Split("#")[0];
		var relativePath = Path.Combine(file.Directory.FullName, withoutFragment);
		var absolutePath = Path.GetFullPath(relativePath);
		return absolutePath;
	}

	private string GetAttachmentsPathRoot(string url)
	{
		// Add URL replacements here
		var replacements = new Dictionary<string, string>(){
							{":", "%3A"},
							{"#", "-"},
							{"%20", " "}
						};
		var linkUrl = new string(url);
		replacements.ForEach(p =>
		{
			linkUrl = linkUrl.Replace(p.Key, p.Value, StringComparison.Ordinal);
		});
		var relativePath = Path.Combine(_wiki.BasePath(), linkUrl);
		var absolutePath = Path.GetFullPath(relativePath);
		return absolutePath;
	}

	private string GetAttachmentsPathRaw(string url)
	{
		var linkUrl = url.Split('/').Last();

		// URLs could be encoded and contain spaces - they are then not found on disk
		linkUrl = HttpUtility.UrlDecode(linkUrl);

		var relativePath = Path.Combine(this._options.AttachmentsPath, linkUrl);
		var absolutePath = Path.GetFullPath(relativePath);
		return absolutePath;
	}

	private static bool IsData(string uri)
	{
		return uri.StartsWith("data:", StringComparison.Ordinal);
	}

	private static bool IsRoot(string uri)
	{
		return uri.StartsWith('/');
	}

	private bool IsAttachments(string uri)
	{
		return !string.IsNullOrEmpty(this._options.AttachmentsPath)
		&& (uri.StartsWith("/.attachments", StringComparison.Ordinal) || uri.StartsWith(".attachments", StringComparison.Ordinal));
	}

	internal static string RemoveDuplicatedHeadersFromGlobalToc(string html)
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
