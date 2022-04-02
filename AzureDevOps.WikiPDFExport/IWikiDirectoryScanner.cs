using System.Collections.Generic;

namespace azuredevops_export_wiki
{
    internal interface IWikiDirectoryScanner
    {
        IList<MarkdownFile> Scan();
    }
}
