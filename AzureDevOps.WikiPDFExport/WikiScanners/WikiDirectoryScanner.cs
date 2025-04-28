using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace AzureDevOps.WikiPdfExport;

internal class WikiDirectoryScanner(string wikiPath, Options options, ILogger logger) : WikiScannerBase(wikiPath, options, logger), IWikiDirectoryScanner, ILogger
{
	public IList<MarkdownFile> Scan()
	{
		return ReadPagesInOrderImpl(wikiPath, 0, excludeRegexes);
	}

	private List<MarkdownFile> ReadPagesInOrderImpl(string path, int level, IList<Regex> excludeRegexes)
	{
		Debug.Print($"{level} scanning {path}");

		var result = new List<MarkdownFile>();

		var directory = new DirectoryInfo(Path.GetFullPath(path));
		Log($"Reading .md files in directory {path}");
		var pages = directory.GetFiles("*.md", SearchOption.TopDirectoryOnly).ToList();
		Log($"Total pages in dir: {pages.Count}", LogLevel.Information, 1);

		var subDirs = directory.GetDirectories("*", SearchOption.TopDirectoryOnly)
			.SkipWhile(d => d.Name.StartsWith('.'))
			.ToList();

		pages.Sort((a, b) => string.Compare(a.Name, b.Name, System.StringComparison.Ordinal));

		Log($"Reading .order file in directory {path}");
		var orderFile = Path.Combine(directory.FullName, ".order");
		var pagesInOrder = File.Exists(orderFile) ? ReorderPages(pages, orderFile) : pages;

		foreach (var page in pagesInOrder)
		{
			var pageRelativePath = page.FullName[BasePath.Length..].Replace('\\', '/');

			var mf = new MarkdownFile(page, "/", level, pageRelativePath);
			if (mf.PartialMatches(excludeRegexes))
			{
				Log($"Skipping page: {mf.AbsolutePath}", LogLevel.Information, 2);
			}
			else
			{
				result.Add(mf);
				Log($"Adding page: {mf.AbsolutePath}", LogLevel.Information, 2);
			}

			var matchingDir = subDirs.FirstOrDefault(
				d => string.Equals(d.Name, Path.GetFileNameWithoutExtension(page.Name), System.StringComparison.OrdinalIgnoreCase));
			if (matchingDir is not null)
			{
				// depth first recursion to be coherent with previous versions
				result.AddRange(
					ReadPagesInOrderImpl(matchingDir.FullName, level + 1, excludeRegexes));
				_ = subDirs.Remove(matchingDir);
			}
		}

		// any leftover
		result.AddRange(
			subDirs
				.SelectMany(d => ReadPagesInOrderImpl(d.FullName, level + 1, excludeRegexes)));

		return result;
	}

	private List<FileInfo> ReorderPages(IList<FileInfo> pages, string orderFile)
	{
		var result = new List<FileInfo>();

		Log("Order file found", LogLevel.Debug, 1);
		var orders = File.ReadAllLines(Path.GetFullPath(orderFile));
		Log($"Pages in order: {orders.Length}", LogLevel.Information, 2);

		// sort pages according to order file
		// NOTE some may not match or partially match
		foreach (var order in orders)
		{
			// TODO linear search... not very optimal
			var p = pages.FirstOrDefault(p => string.Equals(Path.GetFileNameWithoutExtension(p.Name), order, System.StringComparison.OrdinalIgnoreCase));
			if (p != null)
			{
				result.Add(p);
			}
		}
		var notInOrderFile = pages.Except(result);
		result.AddRange(notInOrderFile);

		return result;
	}

	public void Log(string msg, LogLevel logLevel = LogLevel.Information, int indent = 0)
	{
		logger.Log(msg, logLevel, indent);
	}
}
