using NSubstitute;
using Xunit;

namespace AzureDevOps.WikiPdfExport.Test;

public class WikiDirectoryScannerTests
{
	private const string BASE_PATH = "../../../test-data/";
	private readonly ILogger _dummyLogger = Substitute.For<ILogger>();

	[Fact]
	public void givenWikiDirectoryScannerWhenWikiHasPagesOutsideOrderFileThenTheyAreIncludedAsWell()
	{
		var options = new Options
		{
			Path = BASE_PATH + "Inputs/Dis-ordered",
			Css = BASE_PATH + "Inputs/void.css",
			DisableTelemetry = true,
			Debug = true,
			IncludeUnlistedPages = true,
			Output = BASE_PATH + "Outputs/Dis-ordered",
		};
		var scanner = new WikiDirectoryScanner(options.Path, options, _dummyLogger);

		var files = scanner.Scan();

		Assert.Collection(files,
			f => Assert.Equal("/Mentioned-Section.md", f.FileRelativePath),
			f => Assert.Equal("/Mentioned-Section/In-Mentioned-Section.md", f.FileRelativePath),
			f => Assert.Equal("/Start-Page.md", f.FileRelativePath),
			f => Assert.Equal("/Mentioned-Section-No-Home/In-Mentioned-Section-No-Home.md", f.FileRelativePath),
			f => Assert.Equal("/Unmentioned-Section/In-Unmentioned-Section.md", f.FileRelativePath));
	}

	[Fact]
	public void givenWikiDirectoryScannerWhenOnePatternIsExcludedThenTheFilesAreNotIncluded()
	{
		var options = new Options
		{
			Path = BASE_PATH + "Inputs/Dis-ordered",
			Css = BASE_PATH + "Inputs/void.css",
			DisableTelemetry = true,
			Debug = true,
			IncludeUnlistedPages = true,
			Output = BASE_PATH + "Outputs/Exclude1",
			ExcludePaths = new[] { "Home" }
		};
		var scanner = new WikiDirectoryScanner(options.Path, options, _dummyLogger);

		var files = scanner.Scan();

		Assert.Collection(files,
			f => Assert.Equal("/Mentioned-Section.md", f.FileRelativePath),
			f => Assert.Equal("/Mentioned-Section/In-Mentioned-Section.md", f.FileRelativePath),
			f => Assert.Equal("/Start-Page.md", f.FileRelativePath),
			f => Assert.Equal("/Unmentioned-Section/In-Unmentioned-Section.md", f.FileRelativePath));
	}

	[Fact]
	public void givenWikiDirectoryScannerWhenTwoPatternAreExcludedThenTheFilesAreNotIncluded()
	{
		var options = new Options
		{
			Path = BASE_PATH + "Inputs/Dis-ordered",
			Css = BASE_PATH + "Inputs/void.css",
			DisableTelemetry = true,
			Debug = true,
			IncludeUnlistedPages = true,
			Output = BASE_PATH + "Outputs/Exclude2",
			ExcludePaths = new[] { "In-.+Section", "Start" }
		};
		var scanner = new WikiDirectoryScanner(options.Path, options, _dummyLogger);

		var files = scanner.Scan();

		Assert.Collection(files,
			f => Assert.Equal("/Mentioned-Section.md", f.FileRelativePath));
	}

	[Fact]
	public void givenWikiDirectoryScannerWhenWikiIsCodeExampleThenNoOrderChangeFromPreviousVersion()
	{
		var options = new Options
		{
			Path = BASE_PATH + "Inputs/Code",
			Css = BASE_PATH + "Inputs/void.css",
			DisableTelemetry = true,
			Debug = true,
			IncludeUnlistedPages = true,
			Output = BASE_PATH + "Outputs/Code",
		};
		var scanner = new WikiDirectoryScanner(options.Path, options, _dummyLogger);

		var files = scanner.Scan();

		Assert.Collection(files,
			f => Assert.Equal("/Another-Page.md", f.FileRelativePath),
			f => Assert.Equal("/Another-Page/Sub-Page1.md", f.FileRelativePath),
			f => Assert.Equal("/Another-Page/Sub-Page2.md", f.FileRelativePath),
			f => Assert.Equal("/Another-Page/Sub-Page2/Sub-Page2.md", f.FileRelativePath),
			f => Assert.Equal("/Admin-Layout-and-Customization.md", f.FileRelativePath));
	}

}
