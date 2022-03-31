using azuredevops_export_wiki;
using Xunit;
using AzureDevOps.WikiPDFExport.Test.Helpers;

namespace AzureDevOps.WikiPDFExport.Test
{
    public class WikiPDFExporterTests
    {
        const string BASE_PATH = "../../../IntegrationTests-Data/";

        [Fact]
        public async void givenWikiPDFExporter_whenGetProperties_thenNoneAreBlank()
        {
            var options = new Options {
                Path = BASE_PATH + "Inputs/Dis-ordered",
                BreakPage = true,
                DisableTelemetry = true,
                Debug = true,
                Output = BASE_PATH + "Outputs/Dis-ordered",
            };
            var export = new WikiPDFExporter(options);

            await export.Export();

            string expectedHtmlPath = BASE_PATH + "Expected/expected.Dis-ordered.html";
            string outputHtmlPath = options.Output + ".html";
            Assert.True(FileComparer.SameContent(expectedHtmlPath, outputHtmlPath));
        }
    }
}
