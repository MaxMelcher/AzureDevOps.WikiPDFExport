using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace azuredevops_export_wiki
{
    public class MarkdownFile
    {
        public FileInfo FileInfo { get; }
        public string AbsolutePath { get; }
        public string RelativePath { get; }
        public string FileRelativePath { get; }
        public int Level { get; }
        public string Content { get; }

        internal MarkdownFile(FileInfo file, string relativePath, int level, string pageRelativePath, string content = null)
        {
            FileInfo = file;
            AbsolutePath = file.FullName;
            RelativePath = relativePath;
            FileRelativePath = pageRelativePath;
            Level = level;
            Content = content ?? (
                file.Exists
                    ? File.ReadAllText(file.FullName)
                    : string.Empty
                );
        }

        public override string ToString()
        {
            return $"[{Level}] {AbsolutePath}";
        }

        internal bool PartialMatches(IList<Regex> excludeRegexes)
        {
            var normalizedPath = FileRelativePath.Replace('\\', '/');
            return excludeRegexes.Any(
                regex => regex.Match(normalizedPath).Success
                );
        }
    }
}
