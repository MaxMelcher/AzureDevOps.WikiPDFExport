using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace azuredevops_export_wiki
{
    /// <summary>
    /// Provides methods to convert Azure DevOps markdown to standard markdown.
    /// </summary>
    class AzureDevopsToMarkdownConverter
    {
        // Regex pattern to find all headlines without a space between # and the headline, excluding numbers after # (workItems).  
        // Format: #headline
        private const string HeadlinePattern = @"^(#+)(?!\d+|$|#)([^\s])(.*)$";

        // Regex pattern to find all work items. Looks for # followed by a number.  
        // Format: (#12345) 
        private const string WorkItemPattern = @"#\d+";


        /// <summary>
        /// Preprocesses the markdown content to ensure consistency by:
        /// - Converting Azure DevOps-specific syntax to standard markdown.
        /// - Adding a space between # and the headline title to ensure Azure DevOps renders headlines correctly in standard Markdown. Exclude WorkItems
        /// - Adding <br> tag after linebreak of Azure DevOps markdown to break line in standard markdown.
        /// </summary>
        /// <param name="markdown">The markdown content to preprocess.</param>
        /// <returns>The processed markdown content.</returns>
        public static string ConvertAzureDevopsToStandardMarkdown(string markdown)
        {
            // 1. Add a space after # for headlines matched by HeadlinePattern (ignores WorkItems).
            string processedText = Regex.Replace(markdown, HeadlinePattern, "$1 $2$3", RegexOptions.Multiline);

            // 2. Insert <br> tag in all lines that contain a line break, are not headlines, do not contain headlines, and are not inside tables.
            // Initialization
            string[] lines = processedText.Split('\n');
            List<string> processedLines = new List<string>();
            bool isInCodeBlock = false;
            bool isInTable = false;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].TrimEnd();
                string nextLine = i < lines.Length - 1 ? lines[i + 1].TrimEnd() : "";

                // Checks if the line is inside a code block.
                if (line.Trim().StartsWith("```"))
                {
                    isInCodeBlock = !isInCodeBlock;
                }

                // Checks if the line is inside a table
                // If the line starts and ends with "|", it sets isInTable to true
                if (line.StartsWith("|") && line.EndsWith("|"))
                {
                    isInTable = true;
                }
                // If the line no longer starts or ends with "|", and is not empty or whitespace, it sets isInTable to false
                else if (isInTable && (!line.StartsWith("|") || !line.EndsWith("|")) && !string.IsNullOrWhiteSpace(line))
                {
                    isInTable = false;
                }

                // Determine if we should add a <br> tag
                bool shouldAddBreak =
                    !isInCodeBlock && // if line is not in CodeBlock
                    !isInTable && // if line is not in table
                    !string.IsNullOrWhiteSpace(line) && // if line is not empty
                    !line.Contains("[TOC]"); // if line is not in toc

                if (shouldAddBreak)
                {
                    line += "<br>";
                }

                processedLines.Add(line);
            }

            return string.Join("\n", processedLines);
        }
    }
}