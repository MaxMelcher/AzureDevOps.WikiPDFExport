using azuredevops_export_wiki;
using NSubstitute;
using Xunit;

namespace AzureDevOps.WikiPDFExport.Test
{
    public class WikiOptionFilesScannerTests
    {
        const string BASE_PATH = "../../../test-data/";
        ILogger _dummyLogger = Substitute.For<ILogger>();

        [Fact]
        public void givenWikiOptionFilesScanner_whenWikiHasPagesOutsideOrderFile_thenOnlyThoseInOrderAreIncluded()
        {
            var options = new Options
            {
                Path = BASE_PATH + "Inputs/Dis-ordered",
                CSS = BASE_PATH + "Inputs/void.css",
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
        public void givenWikiOptionFilesScanner_whenOnePatternIsExcluded_thenTheFilesAreNotIncluded()
        {
            var options = new Options
            {
                Path = BASE_PATH + "Inputs/Dis-ordered",
                CSS = BASE_PATH + "Inputs/void.css",
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
        public void givenWikiOptionFilesScanner_whenTwoPatternAreExcluded_thenTheFilesAreNotIncluded()
        {
            var options = new Options
            {
                Path = BASE_PATH + "Inputs/Code",
                CSS = BASE_PATH + "Inputs/void.css",
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
        public void givenWikiOptionFilesScanner_whenWikiIsCodeExample_thenNoOrderChangeFromPreviousVersion()
        {
            var options = new Options
            {
                Path = BASE_PATH + "Inputs/Code",
                CSS = BASE_PATH + "Inputs/void.css",
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
}
