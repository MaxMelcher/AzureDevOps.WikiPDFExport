using System.Globalization;
using Markdig.Parsers;

namespace AzureDevOps.WikiPdfExport.MermaidContainer;

/// <summary>
/// Parses the mermaid containers.
/// </summary>
internal class MermaidContainerParser : FencedCodeBlockParser
{
	/// <summary>
	/// Initializes a new instance of the <see cref="FencedCodeBlockParser"/> class.
	/// </summary>
	public MermaidContainerParser()
	{
		OpeningCharacters = [':'];
		InfoPrefix = DefaultInfoPrefix;
	}

	public override BlockState TryOpen(BlockProcessor processor)
	{
		var line = processor.Line.ToString();
		line = line.TrimStart(':', ' ');

		if (!line.StartsWith("mermaid", true, CultureInfo.InvariantCulture))
		{
			return BlockState.None;
		}

		return base.TryOpen(processor);
	}
}
