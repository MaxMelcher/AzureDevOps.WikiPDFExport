using azuredevops_export_wiki;
using NSubstitute;
using System.IO;
using Xunit;

namespace AzureDevOps.WikiPDFExport.Test
{
    public class WikiPDFExporterTests
    {
        const string BASE_PATH = "../../../IntegrationTests-Data/";
        ILoggerExtended _dummyLogger = Substitute.For<ILoggerExtended>();

        [Fact]
        public async void givenWikiPDFExporter_whenWikiHasPagesOutsideOrderFile_thenTheyAreIncludedAsWell()
        {
            var options = new Options
            {
                Path = BASE_PATH + "Inputs/Dis-ordered",
                CSS = BASE_PATH + "Inputs/void.css",
                DisableTelemetry = true,
                Debug = true,
                Output = BASE_PATH + "Outputs/Dis-ordered",
            };
            var export = new WikiPDFExporter(options, _dummyLogger);

            bool ok = await export.Export();

            Assert.True(ok);
            string expectedHtmlPath = BASE_PATH + "Expected/Dis-ordered.html";
            string outputHtmlPath = options.Output + ".html";
            Assert.True(File.Exists(outputHtmlPath));
            var expected = File.ReadAllText(expectedHtmlPath);
            var actual = File.ReadAllText(outputHtmlPath);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public async void givenWikiPDFExporter_whenAPatternIsExcluded_thenTheFilesAreNotIncluded()
        {
            var options = new Options
            {
                Path = BASE_PATH + "Inputs/Dis-ordered",
                CSS = BASE_PATH + "Inputs/void.css",
                DisableTelemetry = true,
                Debug = true,
                Output = BASE_PATH + "Outputs/Exclude1",
                ExcludePaths = new[] { "Home" }
            };
            var export = new WikiPDFExporter(options, _dummyLogger);

            bool ok = await export.Export();

            Assert.True(ok);
            string expectedHtmlPath = BASE_PATH + "Expected/Exclude1.html";
            string outputHtmlPath = options.Output + ".html";
            var expected = File.ReadAllText(expectedHtmlPath);
            var actual = File.ReadAllText(outputHtmlPath);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public async void givenWikiPDFExporter_whenTwoPatternAreExcluded_thenTheFilesAreNotIncluded()
        {
            var options = new Options
            {
                Path = BASE_PATH + "Inputs/Dis-ordered",
                CSS = BASE_PATH + "Inputs/void.css",
                DisableTelemetry = true,
                Debug = true,
                Output = BASE_PATH + "Outputs/Exclude2",
                ExcludePaths = new[] { "In-.+Section", "Start" }
            };
            var export = new WikiPDFExporter(options, _dummyLogger);

            bool ok = await export.Export();

            Assert.True(ok);
            string expectedHtmlPath = BASE_PATH + "Expected/Exclude2.html";
            string outputHtmlPath = options.Output + ".html";
            var expected = File.ReadAllText(expectedHtmlPath);
            var actual = File.ReadAllText(outputHtmlPath);
            Assert.Equal(expected, actual);
        }
    }
}
