using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace azuredevops_export_wiki
{
    public class MarkdownFile
    {
        public string AbsolutePath;
        public string RelativePath;
        public string FileRelativePath;
        public int Level;
        public string Content;


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
