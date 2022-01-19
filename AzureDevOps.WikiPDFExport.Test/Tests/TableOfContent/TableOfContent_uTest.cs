using azuredevops_export_wiki;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace AzureDevOps.WikiPDFExport
{
    public class TableOfContent_uTest
    {
        [Fact]
        public void CreateGlobalTableOfContent_ShouldReturnTOCandSingleHeaderLine()
        {
            // Arrange
            var wikiPDFExporter = new WikiPDFExporter(new Options());
            var mdContent1 = "\n# SomeHeader\n"
                + "SomeText";

            // Act
            var result = wikiPDFExporter.CreateGlobalTableOfContent(new List<string> { mdContent1 });

            Assert.Equal("[TOC]", result[0]);
            Assert.Equal("# SomeHeader", result[1]);
        }

        [Fact]
        public void CreateGlobalTableOfContent_ShouldNotReturnTOC_WhenNoHeaderFound()
        {
            // Arrange
            var wikiPDFExporter = new WikiPDFExporter(new Options());
            var mdContent1 = "\nOnly boring text\n"
                + "No header here";

            // Act
            var result = wikiPDFExporter.CreateGlobalTableOfContent(new List<string> { mdContent1 });

            Assert.False(result.Any());
        }

        [Fact]
        public void CreateGlobalTableOfContent_ShouldReturnTOCandMultipleHeaderLines()
        {
            // Arrange
            var wikiPDFExporter = new WikiPDFExporter(new Options());
            var mdContent1 = "\n# SomeHeader\n"
                + "SomeText";
            var mdContent2 = "    ## SomeOtherHeader   \n"
                + " []() #Some very interesting text in wrong header format #";

            // Act
            var result = wikiPDFExporter.CreateGlobalTableOfContent(new List<string> { mdContent1, mdContent2 });

            Assert.Equal("[TOC]", result[0]);
            Assert.Equal("# SomeHeader", result[1]);
            Assert.Equal("## SomeOtherHeader", result[2]);
        }

        [Fact]
        public void RemoveDuplicatedHeadersFromGlobalTOC()
        {
            // Arrange
            var wikiPDFExporter = new WikiPDFExporter(new Options());
            var htmlContent = "<h1>SomeHeader</h1>\n"
                + "<h2>SomeOtherHeader</h2>\n";

            // Act
            var result = wikiPDFExporter.RemoveDuplicatedHeadersFromGlobalTOC(htmlContent);

            Assert.Equal("", result);
        }

        [Fact]
        public void RemoveDuplicatedHeadersFromGlobalTOC_WhenIdsDefined()
        {
            // Arrange
            var wikiPDFExporter = new WikiPDFExporter(new Options());
            var htmlContent = "<h1 id='interestingID'>SomeHeader</h1>\n"
                + "<h2>SomeOtherHeader</h2>\n";

            // Act
            var result = wikiPDFExporter.RemoveDuplicatedHeadersFromGlobalTOC(htmlContent);

            Assert.Equal("", result);
        }

        [Fact]
        public void RemoveDuplicatedHeadersFromGlobalTOC_ExceptNavTag()
        {
            // Arrange
            var wikiPDFExporter = new WikiPDFExporter(new Options());
            var nav = "<nav>Some cool nav content</nav>\n";
            var htmlContent = nav
                + "<h1>SomeHeader</h1>\n"
                + "<h2>SomeOtherHeader</h2>\n";

            // Act
            var result = wikiPDFExporter.RemoveDuplicatedHeadersFromGlobalTOC(htmlContent);

            Assert.Equal(nav.Trim('\n'), result);
        }
    }
}
