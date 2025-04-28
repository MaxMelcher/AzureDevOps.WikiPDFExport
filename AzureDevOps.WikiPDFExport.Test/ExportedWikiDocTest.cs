using System;
using System.IO;
using Xunit;

namespace AzureDevOps.WikiPdfExport.Test;

public class ExportedWikiDocTest
{
	private const string WIKI_PATH = "../../../test-data/Inputs/Code";
	private readonly DirectoryInfo codeWiki = new DirectoryInfo(Path.GetFullPath(WIKI_PATH, Environment.CurrentDirectory));

	[Fact]
	public void GivenValidWikiBaseLocationWhenCtorThenBaseIsFound()
	{
		var exportedWiki = ExportedWikiDoc.New(WIKI_PATH);
		Assert.Equal(codeWiki.FullName, exportedWiki.BaseDirectory.FullName);
	}

	[Fact]
	public void GivenValidWikiSubLocationWhenCtorThenBaseIsFound()
	{
		var exportedWiki = ExportedWikiDoc.New($"{WIKI_PATH}/Another-Page");
		Assert.Equal(codeWiki.FullName, exportedWiki.BaseDirectory.FullName);
	}

	[Fact]
	public void GivenValidWikiSubSubLocationWhenCtorThenBaseIsFound()
	{
		var exportedWiki = ExportedWikiDoc.New($"{WIKI_PATH}/Another-Page/Sub-Page2");
		Assert.Equal(codeWiki.FullName, exportedWiki.BaseDirectory.FullName);
	}

	[Fact]
	public void GivenNoAttachmentsFolderWhenCtorThenExceptionIsThrown()
	{
		_ = Assert.Throws<WikiPdfExportException>(() => ExportedWikiDoc.New("./"));
	}

	[Fact]
	public void GivenNonExistingLocationWhenCtorThenExceptionIsThrown()
	{
		_ = Assert.Throws<WikiPdfExportException>(() => ExportedWikiDoc.New("X:/this/hopefully/does/not/exist"));
	}

	[Fact]
	public void GivenAbsoluteValidLocationWhenCtorThenSuccessful()
	{
		Assert.NotEmpty(ExportedWikiDoc.New(Path.GetFullPath(WIKI_PATH,
			Environment.CurrentDirectory)).BaseDirectory.FullName);
	}

	[Fact]
	public void GivenValidDirInfoWhenCtorThenSuccessful()
	{
		Assert.NotEmpty(ExportedWikiDoc.New(codeWiki).BaseDirectory.FullName);
	}

	[Fact]
	public void GivenExportDocWhenGetPropertiesThenNoneAreBlank()
	{
		var export = ExportedWikiDoc.New(codeWiki);
		AssertExistingValidDirectory(export.ExportDirectory);
		AssertExistingValidDirectory(export.BaseDirectory);
	}

	private static void AssertExistingValidDirectory(DirectoryInfo dir)
	{
		Assert.NotEmpty(dir.FullName);
		Assert.DoesNotMatch("^/s+$", dir.FullName);
		Assert.True(dir.Exists, $"{dir.Name} does not exist");
		Assert.True(dir.Extension.Length == 0 || dir.Extension == dir.Name,
			$"{dir.Name} has extension: {dir.Extension}");
	}
}
