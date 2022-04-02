using azuredevops_export_wiki;
using NSubstitute;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace AzureDevOps.WikiPDFExport.Test
{
    public class TableOfContent_uTest
    {
        ILoggerExtended _voidLogger = Substitute.For<ILoggerExtended>();
        const string BASE_PATH = "../../../test-data/";
        ExportedWikiDoc _dummyWiki = new ExportedWikiDoc(BASE_PATH + "Inputs/Empty-With-Attachments/");
        Options _noOptions = new Options();

        [Fact]
        public void CreateGlobalTableOfContent_ShouldReturnTOCandSingleHeaderLine()
        {
            // Arrange
            var MarkdownConverter = new MarkdownConverter(_dummyWiki, _noOptions, _voidLogger);
            var mdContent1 = "\n# SomeHeader\n"
                + "SomeText";

            // Act
            var result = MarkdownConverter.CreateGlobalTableOfContent(new List<string> { mdContent1 });

            Assert.Equal("[TOC]", result[0]);
            Assert.Equal("# SomeHeader", result[1]);
        }

        [Fact]
        public void CreateGlobalTableOfContent_ShouldNotReturnTOC_WhenNoHeaderFound()
        {
            // Arrange
            var MarkdownConverter = new MarkdownConverter(_dummyWiki, _noOptions, _voidLogger);
            var mdContent1 = "\nOnly boring text\n"
                + "No header here";

            // Act
            var result = MarkdownConverter.CreateGlobalTableOfContent(new List<string> { mdContent1 });

            Assert.False(result.Any());
        }

        [Fact]
        public void CreateGlobalTableOfContent_ShouldReturnTOCandMultipleHeaderLines()
        {
            // Arrange
            var MarkdownConverter = new MarkdownConverter(_dummyWiki, _noOptions, _voidLogger);
            var mdContent1 = @"
                # SomeHeader
                SomeText";
            var mdContent2 = @"    ## SomeOtherHeader   
                []() #Some very interesting text in wrong header format #";

            // Act
            var result = MarkdownConverter.CreateGlobalTableOfContent(new List<string> { mdContent1, mdContent2 });

            Assert.Equal("[TOC]", result[0]);
            Assert.Equal("# SomeHeader", result[1]);
            Assert.Equal("## SomeOtherHeader", result[2]);
        }

        [Fact]
        public void CreateGlobalTableOfContent_ShouldIgnoreCodeSectionsSingle()
        {
            // Arrange
            var MarkdownConverter = new MarkdownConverter(_dummyWiki, _noOptions, _voidLogger);
            var mdContent1 = @"
                ``` code section
                # SomeHeader
                ```";

            // Act
            var result = MarkdownConverter.CreateGlobalTableOfContent(new List<string> { mdContent1 });

            Assert.False(result.Any());
        }

        [Fact]
        public void CreateGlobalTableOfContent_ShouldIgnoreCodeSectionsSingleTilde()
        {
            // Arrange
            var MarkdownConverter = new MarkdownConverter(_dummyWiki, _noOptions, _voidLogger);
            var mdContent1 = @"
                ~~~ code section
                # SomeHeader
                ~~~";

            // Act
            var result = MarkdownConverter.CreateGlobalTableOfContent(new List<string> { mdContent1 });

            Assert.False(result.Any());
        }

        [Fact]
        public void CreateGlobalTableOfContent_ShouldIgnoreCodeSectionsMultiple()
        {
            // Arrange
            var MarkdownConverter = new MarkdownConverter(_dummyWiki, _noOptions, _voidLogger);
            var mdContent1 = @"
                ``` code section
                ## SomeHeader
                Some other text
                ```
                ```
                ## Another header ```
                # A valid header";

            // Act
            var result = MarkdownConverter.CreateGlobalTableOfContent(new List<string> { mdContent1 });

            Assert.Equal(2, result.Count);
            Assert.Equal("[TOC]", result[0]);
            Assert.Equal("# A valid header", result[1]);
        }

        [Fact]
        public void CreateGlobalTableOfContent_ShouldNotIgnoreInvalidCodeSections()
        {
            // Arrange
            var MarkdownConverter = new MarkdownConverter(_dummyWiki, _noOptions, _voidLogger);
            var mdContent1 = @"
                Not at the beginning ```
                # A valid header
                ```
                ";

            // Act
            var result = MarkdownConverter.CreateGlobalTableOfContent(new List<string> { mdContent1 });

            Assert.Equal(2, result.Count);
            Assert.Equal("[TOC]", result[0]);
            Assert.Equal("# A valid header", result[1]);
        }

        [Fact]
        public void CreateGlobalTableOfContent_ShouldNotIgnoreUnclosedCodeSections()
        {
            // Arrange
            var MarkdownConverter = new MarkdownConverter(_dummyWiki, _noOptions, _voidLogger);
            var mdContent1 = @"
                ``` unclosed comment
                # A valid header
                ";

            // Act
            var result = MarkdownConverter.CreateGlobalTableOfContent(new List<string> { mdContent1 });

            Assert.Equal(2, result.Count);
            Assert.Equal("[TOC]", result[0]);
            Assert.Equal("# A valid header", result[1]);
        }

        [Fact]
        public void RemoveDuplicatedHeadersFromGlobalTOC()
        {
            // Arrange
            var MarkdownConverter = new MarkdownConverter(_dummyWiki, _noOptions, _voidLogger);
            var htmlContent = "<h1>SomeHeader</h1>\n"
                + "<h2>SomeOtherHeader</h2>\n";

            // Act
            var result = MarkdownConverter.RemoveDuplicatedHeadersFromGlobalTOC(htmlContent);

            Assert.Equal("", result);
        }

        [Fact]
        public void RemoveDuplicatedHeadersFromGlobalTOC_WhenIdsDefined()
        {
            // Arrange
            var MarkdownConverter = new MarkdownConverter(_dummyWiki, _noOptions, _voidLogger);
            var htmlContent = "<h1 id='interestingID'>SomeHeader</h1>\n"
                + "<h2>SomeOtherHeader</h2>\n";

            // Act
            var result = MarkdownConverter.RemoveDuplicatedHeadersFromGlobalTOC(htmlContent);

            Assert.Equal("", result);
        }

        [Fact]
        public void RemoveDuplicatedHeadersFromGlobalTOC_ExceptNavTag()
        {
            // Arrange
            var MarkdownConverter = new MarkdownConverter(_dummyWiki, _noOptions, _voidLogger);
            var nav = "<nav>Some cool nav content</nav>\n";
            var htmlContent = nav
                + "<h1>SomeHeader</h1>\n"
                + "<h2>SomeOtherHeader</h2>\n";

            // Act
            var result = MarkdownConverter.RemoveDuplicatedHeadersFromGlobalTOC(htmlContent);

            Assert.Equal(nav.Trim('\n'), result);
        }
    }
}
