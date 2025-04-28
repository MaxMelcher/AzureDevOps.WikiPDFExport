using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace AzureDevOps.WikiPdfExport;

internal class WikiScannerBase
{
	protected readonly string wikiPath;
	protected readonly Options options;
	protected readonly ILogger logger;
	protected readonly IList<Regex> excludeRegexes;
	protected readonly string BasePath = string.Empty;

	public WikiScannerBase(string wikiPath, Options options, ILogger logger)
	{
		this.wikiPath = wikiPath;
		this.options = options;
		this.logger = logger;
		excludeRegexes = [.. (options.ExcludePaths ?? new List<string>()).Select(exclude => new Regex($".*{exclude}.*", RegexOptions.IgnoreCase))];
		var directory = new DirectoryInfo(Path.GetFullPath(wikiPath));
		BasePath = directory.FullName; // to compute relative path
	}
}
