using Markdig.Parsers;
using Markdig.Syntax;

namespace AzureDevOps.WikiPdfExport;

internal class OffsetHeadingBlockParser(int offset) : HeadingBlockParser
{
	private readonly int _offset = offset;

	public override bool Close(BlockProcessor processor, Block block)
	{
		if (block is HeadingBlock hb)
		{
			hb.Level += _offset;
		}
		return base.Close(processor, block);
	}
}
