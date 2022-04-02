using Markdig.Parsers;
using System.Globalization;

namespace azuredevops_export_wiki.MermaidContainer
{
    /// <summary>
    /// Parses the mermaid containers.
    /// </summary>
    public class MermaidContainerParser : FencedCodeBlockParser
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FencedCodeBlockParser"/> class.
        /// </summary>
        public MermaidContainerParser()
        {
            OpeningCharacters = new[] { ':' };
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
}