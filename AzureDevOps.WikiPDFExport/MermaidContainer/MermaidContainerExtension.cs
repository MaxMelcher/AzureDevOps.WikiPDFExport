using Markdig;
using Markdig.Renderers;

namespace azuredevops_export_wiki.MermaidConainer
{
    /// <summary>
    /// Mermaid container extension to setup the markdown pipeline builder and the markdown pipeline.
    /// </summary>
    public class MermaidConainerExtension : IMarkdownExtension
    {
        public void Setup(MarkdownPipelineBuilder pipeline)
        {
            if (!pipeline.BlockParsers.Contains<MermaidContainerParser>())
            {
                // Insert the parser before any other parsers
                pipeline.BlockParsers.Insert(0, new MermaidContainerParser());
            }
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
            if (renderer is HtmlRenderer htmlRenderer)
            {
                if (!htmlRenderer.ObjectRenderers.Contains<MermaidContainerRenderer>())
                {
                    var mermaidContainerRender = new MermaidContainerRenderer();
                    mermaidContainerRender.BlocksAsDiv.Add("mermaid");

                    // Must be inserted before CodeBlockRenderer
                    htmlRenderer.ObjectRenderers.Insert(0, mermaidContainerRender);
                }
            }
        }
    }
}
