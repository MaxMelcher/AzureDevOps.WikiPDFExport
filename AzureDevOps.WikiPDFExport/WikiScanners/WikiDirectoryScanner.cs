using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace azuredevops_export_wiki
{
    internal class WikiDirectoryScanner : IWikiDirectoryScanner, ILogger
    {
        private readonly string wikiPath;
        private readonly Options options;
        private readonly ILogger logger;
        string BasePath = string.Empty;

        public WikiDirectoryScanner(string wikiPath, Options options, ILogger logger)
        {
            this.wikiPath = wikiPath;
            this.options = options;
            this.logger = logger;
        }

        public IList<MarkdownFile> Scan()
        {
            var directory = new DirectoryInfo(Path.GetFullPath(wikiPath));
            BasePath = directory.FullName; // to compute relative path
            var excludeRegexes = (options.ExcludePaths ?? new List<string>())
                .Select(exclude => new Regex($".*{exclude}.*", RegexOptions.IgnoreCase))
                .ToList();
            return ReadPagesInOrderImpl(wikiPath, 0, excludeRegexes);
        }

        private List<MarkdownFile> ReadPagesInOrderImpl(string path, int level, IList<Regex> excludeRegexes)
        {
            Debug.Print($"{level} scanning {path}");

            var result = new List<MarkdownFile>();

            var directory = new DirectoryInfo(Path.GetFullPath(path));
            Log($"Reading .md files in directory {path}");
            var pages = directory.GetFiles("*.md", SearchOption.TopDirectoryOnly).ToList();
            Log($"Total pages in dir: {pages.Count()}", LogLevel.Information, 1);

            var subDirs = directory.GetDirectories("*", SearchOption.TopDirectoryOnly)
                .SkipWhile(d => d.Name.StartsWith('.'))
                .ToList();

            pages.Sort((a, b) => a.Name.CompareTo(b.Name));

            Log($"Reading .order file in directory {path}");
            string orderFile = Path.Combine(directory.FullName, ".order");
            var pagesInOrder = File.Exists(orderFile) ? ReorderPages(pages, orderFile) : pages;

            foreach (var page in pagesInOrder)
            {
                var pageRelativePath = page.FullName[BasePath.Length..].Replace('\\', '/');

                var mf = new MarkdownFile();
                mf.AbsolutePath = page.FullName;
                mf.RelativePath = "/";
                mf.FileRelativePath = pageRelativePath;
                mf.Level = level;
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
                    d => string.Compare(d.Name,
                            Path.GetFileNameWithoutExtension(page.Name), true) == 0);
                if (matchingDir is not null)
                {
                    // depth first recursion to be coherent with previous versions
                    result.AddRange(
                        ReadPagesInOrderImpl(matchingDir.FullName, level + 1, excludeRegexes));
                    subDirs.Remove(matchingDir);
                }
            }

            // any leftover
            result.AddRange(
                subDirs
                    .SelectMany(d => ReadPagesInOrderImpl(d.FullName, level + 1, excludeRegexes)));

            return result;
        }

        private IEnumerable<FileInfo> ReorderPages(IList<FileInfo> pages, string orderFile)
        {
            var result = new List<FileInfo>();

            Log("Order file found", LogLevel.Debug, 1);
            var orders = File.ReadAllLines(Path.GetFullPath(orderFile));
            Log($"Pages in order: {orders.Count()}", LogLevel.Information, 2);

            // sort pages according to order file
            // NOTE some may not match or partially match
            foreach (var order in orders)
            {
                // TODO linear search... not very optimal
                var p = pages.FirstOrDefault(p => string.Compare(Path.GetFileNameWithoutExtension(p.Name), order, true) == 0);
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
}
