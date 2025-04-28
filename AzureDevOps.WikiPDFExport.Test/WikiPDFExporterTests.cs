using System.IO;
using System.Threading.Tasks;
using NSubstitute;
using Xunit;

namespace AzureDevOps.WikiPdfExport.Test;

[Trait("Category", "Integration")]
public class WikiPdfExporterTests
{
	private const string BASE_PATH = "../../../test-data/";
	private readonly ILoggerExtended _dummyLogger = Substitute.For<ILoggerExtended>();

	[Theory]
	[InlineData("SingleFileNoOrder")]
	[InlineData("Code")]
	[InlineData("DeepLink")]
	[InlineData("Dis-ordered")]
	[InlineData("Emoticons")]
	[InlineData("EmptyOrderFile")]
	[InlineData("Flat")]
	[InlineData("WellFormed")]
	[InlineData("PngSvgExport")]
	public async Task ExportWikiIncludeUnlistedPagesSucceeds(string wikiToExport)
	{
		var options = new Options
		{
			Path = BASE_PATH + $"Inputs/{wikiToExport}",
			Css = BASE_PATH + "Inputs/void.css",
			DisableTelemetry = true,
			Debug = true, // generates HTML
			IncludeUnlistedPages = true,
			Output = BASE_PATH + $"Outputs/{wikiToExport}.pdf",
		};
		var export = new WikiPdfExporter(options, _dummyLogger);

		var ok = await export.Export();

		Assert.True(ok);
		var expectedHtmlPath = BASE_PATH + $"Expected/IncludeUnlistedPages/{wikiToExport}.pdf.html";
		var outputHtmlPath = options.Output + ".html";
		Assert.True(File.Exists(outputHtmlPath));
		var expected = await File.ReadAllTextAsync(expectedHtmlPath);
		var actual = await File.ReadAllTextAsync(outputHtmlPath);
		Assert.Equal(expected, actual);
	}

	[Theory]
	[InlineData("Code")]
	[InlineData("DeepLink")]
	[InlineData("Dis-ordered")]
	[InlineData("Emoticons")]
	[InlineData("EmptyOrderFile")]
	[InlineData("Flat")]
	[InlineData("WellFormed")]
	[InlineData("SingleFileNoOrder")]
	[InlineData("PngSvgExport")]
	public async Task ExportWikiOnlyOrderListedPagesSucceeds(string wikiToExport)
	{
		var options = new Options
		{
			Path = BASE_PATH + $"Inputs/{wikiToExport}",
			Css = BASE_PATH + "Inputs/void.css",
			DisableTelemetry = true,
			Debug = true, // generates HTML
			IncludeUnlistedPages = false,
			Output = BASE_PATH + $"Outputs/{wikiToExport}.pdf",
		};
		var export = new WikiPdfExporter(options, _dummyLogger);

		var ok = await export.Export();

		Assert.True(ok);
		var expectedHtmlPath = BASE_PATH + $"Expected/OrderListedPages/{wikiToExport}.pdf.html";
		var outputHtmlPath = options.Output + ".html";
		Assert.True(File.Exists(outputHtmlPath));
		var expected = await File.ReadAllTextAsync(expectedHtmlPath);
		var actual = await File.ReadAllTextAsync(outputHtmlPath);
		Assert.Equal(expected, actual);
	}
}
