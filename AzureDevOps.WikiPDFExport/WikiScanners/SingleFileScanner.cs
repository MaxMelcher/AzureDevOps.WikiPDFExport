using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace AzureDevOps.WikiPdfExport;

internal class SingleFileScanner(string filePath, string wikiPath, Options options, ILogger logger) : WikiOptionFilesScanner(wikiPath, options, logger)
{
	private readonly string singleFilePath = filePath;

	public override IList<MarkdownFile> Scan()
	{
		// handle the case that we have a .order file
		var allFiles = base.Scan();

		// handle the case that we have a single file only by providing a .md file in the -s parameter
		if (singleFilePath.EndsWith(".md", true, System.Globalization.CultureInfo.InvariantCulture))
		{
			var fileInfo = new FileInfo(singleFilePath);
			var file = new MarkdownFile(fileInfo, "", 0, Path.GetRelativePath(wikiPath, fileInfo.FullName));
			return [file];
		}

		var current = allFiles.FirstOrDefault(m => m.FileRelativePath.Contains(singleFilePath, StringComparison.Ordinal));

		if (current is null || (allFiles.IndexOf(current) is var index && index == -1))
		{
			logger.Log($"Single-File [-s] {singleFilePath} specified not found" + singleFilePath, LogLevel.Error);
			throw new ArgumentException($"{singleFilePath} not found");
		}
		var result = new List<MarkdownFile>() { current };

		if (allFiles.Count > index)
		{
			foreach (var item in allFiles.Skip(index + 1))
			{
				if (item.Level > current.Level)
				{
					result.Add(item);
				}
				else
				{
					break;
				}
			}
		}
		return result;
	}

}
