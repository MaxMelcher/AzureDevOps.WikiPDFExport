using System;
using System.Collections.Generic;
using System.IO;
using Markdig;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;


namespace AzureDevOps.WikiPdfExport;


internal class DeepLink(BlockParser parser) : HeadingBlock(parser)
{
	public required string FileName { get; set; }
}

internal class DeepLinkRenderer : HtmlObjectRenderer<DeepLink>
{
	protected override void Write(HtmlRenderer renderer, DeepLink obj)
	{
		_ = renderer.Write("<a id='123123123213'>&nbsp;</a>");
	}
}

internal class DeepLinkExtension : IMarkdownExtension
{
	public string? Filename { get; set; }
	private const string DeepLinkKey = "DeepLink";
	private readonly DeepLinkOptions options;
	private readonly StripRendererCache rendererCache = new StripRendererCache();

	public void Setup(MarkdownPipelineBuilder pipeline)
	{
		var headingBlockParser = pipeline.BlockParsers.Find<HeadingBlockParser>();
		if (headingBlockParser != null)
		{
			// Install a hook on the HeadingBlockParser when a HeadingBlock is actually processed
			headingBlockParser.Closed -= HeadingBlockParser_Closed;
			headingBlockParser.Closed += HeadingBlockParser_Closed;
		}
		var paragraphBlockParser = pipeline.BlockParsers.FindExact<ParagraphBlockParser>();
		if (paragraphBlockParser != null)
		{
			// Install a hook on the ParagraphBlockParser when a HeadingBlock is actually processed as a Setex heading
			paragraphBlockParser.Closed -= HeadingBlockParser_Closed;
			paragraphBlockParser.Closed += HeadingBlockParser_Closed;
		}
	}

	public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
	{
		HtmlRenderer? htmlRenderer;
		ObjectRendererCollection? renderers;

		htmlRenderer = renderer as HtmlRenderer;
		renderers = htmlRenderer?.ObjectRenderers;
		if (renderers != null && !renderers.Contains<DeepLinkRenderer>())
		{
			renderers.Add(new DeepLinkRenderer());
		}
	}

	private void HeadingBlockParser_Closed(BlockProcessor processor, Block block)
	{
		// We may have a ParagraphBlock here as we have a hook on the ParagraphBlockParser
		if (block is not HeadingBlock headingBlock)
		{
			return;
		}

		// If the AutoLink options is set, we register a LinkReferenceDefinition at the document level
		if ((options & DeepLinkOptions.AutoLink) != 0)
		{
			var headingLine = headingBlock.Lines.Lines[0];

			var text = headingLine.ToString();

			var linkRef = new HeadingLinkReferenceDefinition(headingBlock)
			{
				CreateLinkInline = CreateLinkInlineForHeading
			};

			var doc = processor.Document;
			if (doc.GetData(this) is not Dictionary<string, HeadingLinkReferenceDefinition> dictionary)
			{
				dictionary = [];
				doc.SetData(this, dictionary);
				doc.ProcessInlinesBegin += DocumentOnProcessInlinesBegin;
			}
			dictionary[text] = linkRef;
		}

		// Then we register after inline have been processed to actually generate the proper #id
		headingBlock.ProcessInlinesEnd += HeadingBlock_ProcessInlinesEnd;
	}

	private void DocumentOnProcessInlinesBegin(InlineProcessor processor, Inline? inline)
	{
		var doc = processor.Document;
		doc.ProcessInlinesBegin -= DocumentOnProcessInlinesBegin;
		var dictionary = (Dictionary<string, HeadingLinkReferenceDefinition>)doc.GetData(this)!;
		foreach (var keyPair in dictionary)
		{
			// Here we make sure that auto-identifiers will not override an existing link definition
			// defined in the document
			// If it is the case, we skip the auto identifier for the Heading
			if (!doc.TryGetLinkReferenceDefinition(keyPair.Key, out var linkDef))
			{
				doc.SetLinkReferenceDefinition(keyPair.Key, keyPair.Value, true);
			}
		}
		// Once we are done, we don't need to keep the intermediate dictionary around
		_ = doc.RemoveData(this);
	}

	private LinkInline CreateLinkInlineForHeading(InlineProcessor inlineState, LinkReferenceDefinition linkRef, Inline? child)
	{
		var headingRef = (HeadingLinkReferenceDefinition)linkRef;
		return new LinkInline()
		{
			// Use GetDynamicUrl to allow late binding of the Url (as a link may occur before the heading is declared and
			// the inlines of the heading are actually processed by HeadingBlock_ProcessInlinesEnd)
			GetDynamicUrl = () => HtmlHelper.Unescape("#" + headingRef.Heading.GetAttributes().Id),
			Title = HtmlHelper.Unescape(linkRef.Title),
		};
	}

	private void HeadingBlock_ProcessInlinesEnd(InlineProcessor processor, Inline? inline)
	{
		if (processor.Document.GetData(DeepLinkKey) is not HashSet<string> identifiers)
		{
			identifiers = [];
			processor.Document.SetData(DeepLinkKey, identifiers);
		}

		var headingBlock = (HeadingBlock)processor.Block!;
		if (headingBlock.Inline is null)
		{
			return;
		}

		// If id is already set, don't try to modify it
		var attributes = processor.Block!.GetAttributes();
		if (attributes.Id != null)
		{
			return;
		}

		// Use internally a HtmlRenderer to strip links from a heading
		var stripRenderer = rendererCache.Get();

		_ = stripRenderer.Render(headingBlock.Inline);
		var headingText = stripRenderer.Writer.ToString()!;
		rendererCache.Release(stripRenderer);

		headingText = $"{Filename}-{headingText}";

		// Urilize the link
		headingText = (options & DeepLinkOptions.GitHub) != 0
			? LinkHelper.UrilizeAsGfm(headingText)
			: LinkHelper.Urilize(headingText, (options & DeepLinkOptions.AllowOnlyAscii) != 0);

		// If the heading is empty, use the word "section" instead
		var baseHeadingId = string.IsNullOrEmpty(headingText) ? "section" : headingText;

		// Add a trailing -1, -2, -3...etc. in case of collision
		var index = 0;
		var headingId = baseHeadingId;
		var headingBuffer = StringBuilderCache.Local();
		while (!identifiers.Add(headingId))
		{
			index++;
			_ = headingBuffer.Append(baseHeadingId);
			_ = headingBuffer.Append('-');
			_ = headingBuffer.Append(index);
			headingId = headingBuffer.ToString();
			headingBuffer.Length = 0;
		}

		attributes.Id = $"{headingId}";
	}



	private sealed class StripRendererCache : ObjectCache<HtmlRenderer>
	{
		protected override HtmlRenderer NewInstance()
		{
			var headingWriter = new StringWriter();
			var stripRenderer = new HtmlRenderer(headingWriter)
			{
				// Set to false both to avoid having any HTML tags in the output
				EnableHtmlForInline = false,
				EnableHtmlEscape = false
			};
			return stripRenderer;
		}

		protected override void Reset(HtmlRenderer instance)
		{
			// do nothing
		}
	}
}

/// <summary>
/// Options for the <see cref="AutoIdentifierExtension"/>.
/// </summary>
[Flags]
internal enum DeepLinkOptions
{
	/// <summary>
	/// No options: does not apply any additional formatting and/or transformations.
	/// </summary>
	None = 0,

	/// <summary>
	/// Default (<see cref="AutoLink"/>)
	/// </summary>
	Default = AutoLink | AllowOnlyAscii,

	/// <summary>
	/// Allows to link to a header by using the same text as the header for the link label. Default is <c>true</c>
	/// </summary>
	AutoLink = 1,

	/// <summary>
	/// Allows only ASCII characters in the url (HTML 5 allows to have UTF8 characters). Default is <c>true</c>
	/// </summary>
	AllowOnlyAscii = 2,

	/// <summary>
	/// Renders auto identifiers like GitHub.
	/// </summary>
	GitHub = 4,
}

/// <summary>
/// A link reference definition to a <see cref="HeadingBlock"/> stored at the <see cref="MarkdownDocument"/> level.
/// </summary>
/// <seealso cref="LinkReferenceDefinition" />
internal class HeadingLinkReferenceDefinition(HeadingBlock headling) : LinkReferenceDefinition
{

	/// <summary>
	/// Gets or sets the heading related to this link reference definition.
	/// </summary>
	public HeadingBlock Heading { get; set; } = headling;
}
