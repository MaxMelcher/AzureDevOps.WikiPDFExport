using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.TeamFoundation.TestManagement.WebApi;
using Microsoft.TeamFoundation.Wiki.WebApi;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace azuredevops_export_wiki
{
    internal class SingleFileScanner : WikiOptionFilesScanner
    {
        private readonly string singleFilePath;

        public SingleFileScanner(string filePath, string wikiPath, Options options, ILogger logger)
            : base(wikiPath, options, logger) 
        {
            this.singleFilePath = filePath;
        }

        public override IList<MarkdownFile> Scan()
        {
            var allFiles = base.Scan(); 
            var current = allFiles.FirstOrDefault(m => m.FileRelativePath.Contains(singleFilePath));
            var idx = allFiles.IndexOf(current);

            if (current is null || idx == -1)
            {
                logger.Log($"Single-File [-s] {singleFilePath} specified not found" + singleFilePath, LogLevel.Error);
                throw new ArgumentException($"{singleFilePath} not found");
            }
            var result = new List<MarkdownFile>() { current };

            if (allFiles.Count > idx)
            {
                foreach (var item in allFiles.Skip(idx + 1))
                {
                    if (item.Level > current.Level)
                    {
                        result.Add(item);
                    }
                    else
                        break;
                }
            }

            return result;
        }

    }
}
