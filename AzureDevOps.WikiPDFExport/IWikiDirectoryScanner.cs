using System.Collections.Generic;

namespace AzureDevOps.WikiPdfExport;

internal interface IWikiDirectoryScanner
{
	IList<MarkdownFile> Scan();
}
