using Markdig.Parsers;
using Markdig.Syntax;

namespace azuredevops_export_wiki
{
    class OffsetHeadingBlockParser : HeadingBlockParser
    {
        public OffsetHeadingBlockParser(int offset)
        {
            _offset = offset;
        }
        private int _offset;

        public override bool Close(BlockProcessor processor, Block block)
        {
            if (block is HeadingBlock hb)
            {
                hb.Level += _offset;
            }
            return base.Close(processor, block);
        }
    }
}