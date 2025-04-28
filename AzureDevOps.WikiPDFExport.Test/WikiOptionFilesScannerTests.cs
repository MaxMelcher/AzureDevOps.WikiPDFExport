using NSubstitute;
using Xunit;

namespace AzureDevOps.WikiPdfExport.Test;

public class WikiOptionFilesScannerTests
{
	private const string BASE_PATH = "../../../test-data/";
	private readonly ILogger _dummyLogger = Substitute.For<ILogger>();

	[Fact]
	public void givenWikiOptionFilesScannerWhenWikiHasPagesOutsideOrderFileThenOnlyThoseInOrderAreIncluded()
	{
		var options = new Options
		{
			Path = BASE_PATH + "Inputs/Dis-ordered",
			Css = BASE_PATH + "Inputs/void.css",
			DisableTelemetry = true,
			Debug = true,
			IncludeUnlistedPages = false,
			Output = BASE_PATH + "Outputs/Dis-ordered",
		};
		var scanner = new WikiOptionFilesScanner(options.Path, options, _dummyLogger);

		var files = scanner.Scan();

		Assert.Collection(files,
			f => Assert.Equal("/Mentioned-Section.md", f.FileRelativePath),
			f => Assert.Equal("/Mentioned-Section-No-Home.md", f.FileRelativePath));
	}

	[Fact]
	public void givenWikiOptionFilesScannerWhenOnePatternIsExcludedThenTheFilesAreNotIncluded()
	{
		var options = new Options
		{
			Path = BASE_PATH + "Inputs/Dis-ordered",
			Css = BASE_PATH + "Inputs/void.css",
			DisableTelemetry = true,
			Debug = true,
			IncludeUnlistedPages = false,
			Output = BASE_PATH + "Outputs/Exclude1",
			ExcludePaths = new[] { "Home" }
		};
		var scanner = new WikiOptionFilesScanner(options.Path, options, _dummyLogger);

		var files = scanner.Scan();

		Assert.Collection(files,
			f => Assert.Equal("/Mentioned-Section.md", f.FileRelativePath));
	}

	[Fact]
	public void givenWikiOptionFilesScannerWhenTwoPatternAreExcludedThenTheFilesAreNotIncluded()
	{
		var options = new Options
		{
			Path = BASE_PATH + "Inputs/Code",
			Css = BASE_PATH + "Inputs/void.css",
			DisableTelemetry = true,
			Debug = true,
			IncludeUnlistedPages = false,
			Output = BASE_PATH + "Outputs/Code",
			ExcludePaths = new[] { "Sub-Page2", "Customization" }
		};
		var scanner = new WikiOptionFilesScanner(options.Path, options, _dummyLogger);

		var files = scanner.Scan();

		Assert.Collection(files,
			f => Assert.Equal("/Another-Page.md", f.FileRelativePath),
			f => Assert.Equal("/Another-Page/Sub-Page1.md", f.FileRelativePath));

	}

	[Fact]
	public void givenWikiOptionFilesScannerWhenWikiIsCodeExampleThenNoOrderChangeFromPreviousVersion()
	{
		var options = new Options
		{
			Path = BASE_PATH + "Inputs/Code",
			Css = BASE_PATH + "Inputs/void.css",
			DisableTelemetry = true,
			Debug = true,
			IncludeUnlistedPages = false,
			Output = BASE_PATH + "Outputs/Code",
		};
		var scanner = new WikiOptionFilesScanner(options.Path, options, _dummyLogger);

		var files = scanner.Scan();

		Assert.Collection(files,
			f => Assert.Equal("/Another-Page.md", f.FileRelativePath),
			f => Assert.Equal("/Another-Page/Sub-Page1.md", f.FileRelativePath),
			f => Assert.Equal("/Another-Page/Sub-Page2.md", f.FileRelativePath),
			f => Assert.Equal("/Another-Page/Sub-Page2/Sub-Page2a.md", f.FileRelativePath),
			f => Assert.Equal("/Admin-Layout-and-Customization.md", f.FileRelativePath));
	}

}
