using NSubstitute;
using Xunit;

namespace AzureDevOps.WikiPdfExport.Test;

public class TableOfContentUTest
{
	private readonly ILoggerExtended _voidLogger = Substitute.For<ILoggerExtended>();
	private const string BASE_PATH = "../../../test-data/";
	private readonly ExportedWikiDoc _dummyWiki = ExportedWikiDoc.New(BASE_PATH + "Inputs/Empty-With-Attachments/");
	private readonly Options _noOptions = new Options();

	[Fact]
	public void CreateGlobalTableOfContentShouldReturnTOCandSingleHeaderLine()
	{
		// Arrange
		_ = new MarkdownConverter(_dummyWiki, _noOptions, _voidLogger);
		var mdContent1 = @"
			# SomeHeader
			SomeText
		";

		// Act
		var result = MarkdownConverter.CreateGlobalTableOfContent([mdContent1]);

		// Assert
		Assert.Equal("[TOC]", result[0]);
		Assert.Equal("# SomeHeader", result[1]);
	}

	[Fact]
	public void CreateGlobalTableOfContentShouldNotReturnTOCWhenNoHeaderFound()
	{
		// Arrange
		_ = new MarkdownConverter(_dummyWiki, _noOptions, _voidLogger);
		var mdContent1 = @"
			Only boring text
			No header here
		";

		// Act
		var result = MarkdownConverter.CreateGlobalTableOfContent([mdContent1]);

		// Assert
		Assert.False(result.Count != 0);
	}

	[Fact]
	public void CreateGlobalTableOfContentShouldReturnTOCandMultipleHeaderLines()
	{
		// Arrange
		_ = new MarkdownConverter(_dummyWiki, _noOptions, _voidLogger);
		var mdContent1 = @"
			# SomeHeader
			SomeText";
		var mdContent2 = @"
			## SomeOtherHeader
			[]() #Some very interesting text in wrong header format #
		";

		// Act
		var result = MarkdownConverter.CreateGlobalTableOfContent([mdContent1, mdContent2]);

		// Assert
		Assert.Equal("[TOC]", result[0]);
		Assert.Equal("# SomeHeader", result[1]);
		Assert.Equal("## SomeOtherHeader", result[2]);
	}

	[Fact]
	public void CreateGlobalTableOfContentShouldIgnoreCodeSectionsSingle()
	{
		// Arrange
		_ = new MarkdownConverter(_dummyWiki, _noOptions, _voidLogger);
		var mdContent1 = @"
			``` code section
			# SomeHeader
			```
		";

		// Act
		var result = MarkdownConverter.CreateGlobalTableOfContent([mdContent1]);

		// Assert
		Assert.False(result.Count != 0);
	}

	[Fact]
	public void CreateGlobalTableOfContentShouldIgnoreCodeSectionsSingleTilde()
	{
		// Arrange
		_ = new MarkdownConverter(_dummyWiki, _noOptions, _voidLogger);
		var mdContent1 = @"
			~~~ code section
			# SomeHeader
			~~~
		";

		// Act
		var result = MarkdownConverter.CreateGlobalTableOfContent([mdContent1]);

		// Assert
		Assert.False(result.Count != 0);
	}

	[Fact]
	public void CreateGlobalTableOfContentShouldIgnoreCodeSectionsMultiple()
	{
		// Arrange
		_ = new MarkdownConverter(_dummyWiki, _noOptions, _voidLogger);
		var mdContent1 = @"
			``` code section
			## SomeHeader
			Some other text
			```
			```
			## Another header ```
			# A valid header
		";

		// Act
		var result = MarkdownConverter.CreateGlobalTableOfContent([mdContent1]);

		// Assert
		Assert.Equal(2, result.Count);
		Assert.Equal("[TOC]", result[0]);
		Assert.Equal("# A valid header", result[1]);
	}

	[Fact]
	public void CreateGlobalTableOfContentShouldNotIgnoreInvalidCodeSections()
	{
		// Arrange
		_ = new MarkdownConverter(_dummyWiki, _noOptions, _voidLogger);
		var mdContent1 = @"
			Not at the beginning ```
			# A valid header
			```
		";

		// Act
		var result = MarkdownConverter.CreateGlobalTableOfContent([mdContent1]);

		// Assert
		Assert.Equal(2, result.Count);
		Assert.Equal("[TOC]", result[0]);
		Assert.Equal("# A valid header", result[1]);
	}

	[Fact]
	public void CreateGlobalTableOfContentShouldNotIgnoreUnclosedCodeSections()
	{
		// Arrange
		_ = new MarkdownConverter(_dummyWiki, _noOptions, _voidLogger);
		var mdContent1 = @"
			``` unclosed comment
			# A valid header
		";

		// Act
		var result = MarkdownConverter.CreateGlobalTableOfContent([mdContent1]);

		// Assert
		Assert.Equal(2, result.Count);
		Assert.Equal("[TOC]", result[0]);
		Assert.Equal("# A valid header", result[1]);
	}

	[Fact]
	public void RemoveDuplicatedHeadersFromGlobalTOC()
	{
		// Arrange
		_ = new MarkdownConverter(_dummyWiki, _noOptions, _voidLogger);
		var htmlContent = @"
			<h1>SomeHeader</h1>
			<h2>SomeOtherHeader</h2>
		";

		// Act
		var result = MarkdownConverter.RemoveDuplicatedHeadersFromGlobalToc(htmlContent);

		// Assert
		Assert.Equal("", result);
	}

	[Fact]
	public void RemoveDuplicatedHeadersFromGlobalTOCWhenIdsDefined()
	{
		// Arrange
		_ = new MarkdownConverter(_dummyWiki, _noOptions, _voidLogger);
		var htmlContent = @"
			<h1 id='interestingID'>SomeHeader</h1>
			<h2>SomeOtherHeader</h2>
		";

		// Act
		var result = MarkdownConverter.RemoveDuplicatedHeadersFromGlobalToc(htmlContent);

		// Assert
		Assert.Equal("", result);
	}

	[Fact]
	public void RemoveDuplicatedHeadersFromGlobalTOCExceptNavTag()
	{
		// Arrange
		_ = new MarkdownConverter(_dummyWiki, _noOptions, _voidLogger);
		var nav = "<nav>Some cool nav content</nav>\n";
		var htmlContent = nav
			+ @"<h1>SomeHeader</h1>
				<h2>SomeOtherHeader</h2>
			";

		// Act
		var result = MarkdownConverter.RemoveDuplicatedHeadersFromGlobalToc(htmlContent);

		// Assert
		Assert.Equal(nav.Trim('\n'), result);
	}
}
