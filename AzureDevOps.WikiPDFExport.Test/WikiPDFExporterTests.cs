using azuredevops_export_wiki;
using NSubstitute;
using System.IO;
using Xunit;

namespace AzureDevOps.WikiPDFExport.Test
{
    public class WikiPDFExporterTests
    {
        const string BASE_PATH = "../../../test-data/";
        ILoggerExtended _dummyLogger = Substitute.For<ILoggerExtended>();

        [Theory]
        [InlineData("Code")]
        [InlineData("DeepLink")]
        [InlineData("Dis-ordered")]
        [InlineData("Emoticons")]
        [InlineData("EmptyOrderFile")]
        [InlineData("Flat")]
        [InlineData("WellFormed")]
        public async void Export_Wiki_Succeeds(string wikiToExport)
        {
            var options = new Options
            {
                Path = BASE_PATH + $"Inputs/{wikiToExport}",
                CSS = BASE_PATH + "Inputs/void.css",
                DisableTelemetry = true,
                Debug = true, // generates HTML
                Output = BASE_PATH + $"Outputs/{wikiToExport}.pdf",
            };
            var export = new WikiPDFExporter(options, _dummyLogger);

            bool ok = await export.Export();

            Assert.True(ok);
            string expectedHtmlPath = BASE_PATH + $"Expected/{wikiToExport}.pdf.html";
            string outputHtmlPath = options.Output + ".html";
            Assert.True(File.Exists(outputHtmlPath));
            var expected = File.ReadAllText(expectedHtmlPath);
            var actual = File.ReadAllText(outputHtmlPath);
            Assert.Equal(expected, actual);
        }
    }
}
