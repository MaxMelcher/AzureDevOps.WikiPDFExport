using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace azuredevops_export_wiki
{
    internal class WikiOptionFilesScanner : WikiScannerBase, IWikiDirectoryScanner, ILogger
    {

        public WikiOptionFilesScanner(string wikiPath, Options options, ILogger logger)
            : base(wikiPath, options, logger) { }

        public virtual IList<MarkdownFile> Scan()
        {
            return ReadOrderFiles(wikiPath, 0, excludeRegexes); // root level
        }

        private List<MarkdownFile> ReadOrderFiles(string path, int level, IList<Regex> excludeRegexes)
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

                    string absolutePath = Path.Combine(orderFile.Directory.FullName, $"{order}.md");
                    MarkdownFile mf = new MarkdownFile(
                        new FileInfo(absolutePath),
                        $"{relativePath}",
                        level,
                        absolutePath[BasePath.Length..].Replace('\\', '/')
                        );
                    if (mf.PartialMatches(excludeRegexes))
                    {
                        Log($"Skipping page: {mf.AbsolutePath}", LogLevel.Information, 2);
                    }
                    else
                    {
                        result.Add(mf);
                        Log($"Adding page: {mf.AbsolutePath}", LogLevel.Information, 2);
                    }

                    var childPath = Path.Combine(orderFile.Directory.FullName, order);
                    if (Directory.Exists(childPath))
                    {
                        //recursion
                        result.AddRange(ReadOrderFiles(childPath, level + 1, excludeRegexes));
                    }
                }
            }

            return result;
        }

        public void Log(string msg, LogLevel logLevel = LogLevel.Information, int indent = 0)
        {
            logger.Log(msg, logLevel, indent);
        }
    }
}