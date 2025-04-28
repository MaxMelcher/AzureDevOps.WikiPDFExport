using System.Diagnostics;
using System.IO;

namespace AzureDevOps.WikiPdfExport;

/// <summary>
/// Information about an export from an Azure Devops Wiki
/// </summary>
internal record ExportedWikiDoc
{
	private ExportedWikiDoc(DirectoryInfo exportDirectory, DirectoryInfo baseDirectory)
	{
		ExportDirectory = exportDirectory;
		BaseDirectory = baseDirectory;
	}

	/// <summary>
	/// Create a wiki export from a path to the base of the export (not the wiki).
	/// </summary>
	public static ExportedWikiDoc New(DirectoryInfo exportBase)
	{
		if (!exportBase.Exists)
		{
			throw new WikiPdfExportException($"The wiki export location {exportBase} does not exist");
		}
		return new(exportBase, FindNearestParentAttachmentsDirectory(exportBase));
	}

	/// <summary>
	/// Create a wiki export from a path to the base of the export (not the wiki).
	/// </summary>
	public static ExportedWikiDoc New(string exportBase)
	{
		var directoryInfo = new DirectoryInfo(exportBase);
		return New(directoryInfo);
	}

	/// <summary>
	/// Search from the given existing directory upwards until a folder containing
	/// an 'attachment' folder is identified.
	/// </summary>
	/// <returns>
	/// A valid existing directory which contains an attachments folder.
	/// </returns>
	/// <throws>
	/// WikiPdfExportException if an attachments folder is not found before hitting the root.
	/// </throws>
	public static DirectoryInfo FindNearestParentAttachmentsDirectory(DirectoryInfo exportBase)
	{
		var attachmentDirectories = exportBase.GetDirectories("./.attachments");
		if (attachmentDirectories.Length > 0)
		{
			return attachmentDirectories[0].Parent ?? throw new UnreachableException("`./.attachments` has a parent, by definition.");
		}
		if (exportBase.Parent is not null)
		{
			return FindNearestParentAttachmentsDirectory(exportBase.Parent);
		}

		// Return the base path and hope for the best.
		return exportBase;
	}

	public DirectoryInfo ExportDirectory { get; }
	public DirectoryInfo BaseDirectory { get; }

	public string ExportPath()
	{
		return ExportDirectory.FullName;
	}

	public string BasePath()
	{
		return BaseDirectory.FullName;
	}
}
