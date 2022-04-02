using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;

namespace azuredevops_export_wiki
{
    internal class SingleFileScanner : IWikiDirectoryScanner
    {
        private readonly string singleFilePath;
        private ILogger logger;

        public SingleFileScanner(string filePath, ILogger logger)
        {
            this.singleFilePath = filePath;
            this.logger = logger;
        }

        public IList<MarkdownFile> Scan()
        {
            var filePath = Path.GetFullPath(singleFilePath);
            var directory = new DirectoryInfo(Path.GetFullPath(filePath));

            if (!File.Exists(filePath))
            {
                logger.Log($"Single-File [-s] {filePath} specified not found" + filePath, LogLevel.Error);
                throw new ArgumentException($"{singleFilePath} not found");
            }

            var relativePath = filePath.Substring(directory.FullName.Length);

            return new List<MarkdownFile>()
                        {
                            new MarkdownFile() {
                                AbsolutePath = filePath,
                                RelativePath = relativePath,
                                Level = 0 // root level
                            }
                        };
        }

    }
}
