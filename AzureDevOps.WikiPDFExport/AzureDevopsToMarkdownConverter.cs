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
        // Temporary marker for WorkItem references
        private const string WorkItemMarker = "§§WORKITEM§§";

        // Temporary marker for line breaks that need to be added after WorkItem processing
        private const string BrMarker = "§§BR§§";

        // Regex pattern to match WorkItem references like #123
        private const string WorkItemPattern = @"(?!(^|[^#]))(#\d+)(?=(\s|$))";

        // Regex pattern to match headers and ensure a space after the # characters
        private const string HeadlinePattern = @"^(#+)([^#\s])(.*)$";

        /// <summary>
        /// Preprocesses the markdown content to ensure consistency by:
        /// - Converting Azure DevOps-specific syntax to standard markdown.
        /// - Adding a space between # and the headline title to ensure Azure DevOps renders headlines correctly in standard Markdown.
        /// - Adding <br> tag after linebreak of Azure DevOps markdown to break line in standard markdown.
        /// </summary>
        /// <param name="markdown">The markdown content to preprocess.</param>
        /// <returns>The processed markdown content.</returns>
        public static string ConvertAzureDevopsToStandardMarkdown(string markdown)
        {
            // Temporarily marks WorkItem references with '§§WORKITEM§§' to prevent unintended modifications during processing,  
            // ensuring that no space is added after the # symbol.  
            string markedWorkItemsText = Regex.Replace(markdown, WorkItemPattern,
                match => match.Groups[1].Value + WorkItemMarker + match.Groups[2].Value + match.Groups[3].Value);

            // Ensures that all headlines have a space between # and the title, so they are correctly recognized as headlines in standard Markdown.  
            string spacedHeadlinesText = Regex.Replace(markedWorkItemsText, HeadlinePattern, "$1 $2$3", RegexOptions.Multiline);

            // Convert single line breaks to <br> tags, but skip:
            // - Code blocks (between ```)
            // - Header lines
            // - Blank lines
            // - Tables
            string[] lines = spacedHeadlinesText.Split('\n');
            List<string> processedLines = new List<string>();
            bool isInCodeBlock = false;
            bool isInTable = false;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].TrimEnd();
                string nextLine = i < lines.Length - 1 ? lines[i + 1].TrimEnd() : "";

                // returns if line is in code block. If yes 
                isInCodeBlock = IsLineInCodeBlock(line, isInCodeBlock);

                // Toggle table state if the line starts or ends a table
                isInTable = IsLineInTable(line, isInTable);

                // Skip processing if the line is inside a code block, table, or is a header/blank line
                if (ShouldSkipLineProcessing(line, isInCodeBlock, isInTable))
                {
                    processedLines.Add(line);
                    continue;
                }

                // Add a <br> tag or a workItem marker  if the next line is not empty, not a Markdown headline, not the start of a table, and the current line is not a table divider.
                if (ShouldAddLineBreak(line, nextLine))
                {
                    line += line.Contains(WorkItemMarker) ? BrMarker : "<br>";
                }

                processedLines.Add(line);
            }

            // Combine the processed lines and replace temporary markers
            string result = new StringBuilder(string.Join("\n", processedLines))
                .Replace(WorkItemMarker, "") // Remove WorkItem markers
                .Replace(BrMarker, "<br>") // Replace BR markers with <br> tags
                .ToString();

            return result;
        }

        /// <summary>
        /// Determines if the line is in a code block.
        /// </summary>
        /// <param name="line">The current line being processed.</param>
        /// <param name="isInCodeBlock">The current state of the code block.</param>
        /// <returns>The updated state of the code block.</returns>
        private static bool IsLineInCodeBlock(string line, bool isInCodeBlock)
        {
            if (line.Trim().StartsWith("```"))
            {
                return !isInCodeBlock;
            }
            return isInCodeBlock;
        }

        /// <summary>
        /// Determines if the line is in a table
        /// </summary>
        /// <param name="line">The current line being processed.</param>
        /// <param name="isInTable">The current state of the table.</param>
        /// <returns>The updated state of the table.</returns>
        private static bool IsLineInTable(string line, bool isInTable)
        {
            if (line.StartsWith("|") && line.EndsWith("|"))
            {
                return true;
            }
            if (isInTable && (!line.StartsWith("|") || !line.EndsWith("|")) && !string.IsNullOrWhiteSpace(line))
            {
                return false;
            }
            return isInTable;
        }

        /// <summary>
        /// Determines whether the current line should be skipped during processing.
        /// A line is skipped if it is inside a code block, inside a table, empty, or a Markdown headline.
        /// </summary>
        /// <param name="line">The current line being processed.</param>
        /// <param name="isInCodeBlock">Whether the line is inside a code block.</param>
        /// <param name="isInTable">Whether the line is inside a table.</param>
        /// <returns>True if the line should be skipped, otherwise false.</returns>
        private static bool ShouldSkipLineProcessing(string line, bool isInCodeBlock, bool isInTable)
        {
            return isInCodeBlock || isInTable || string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#");
        }

        /// <summary>
        /// Determines whether a <br> tag should be added to the current line.
        /// A line break is added if the next line is not empty, not a Markdown headline, not the start of a table, and the current line is not a table divider.
        /// </summary>
        /// <param name="line">The current line being processed.</param>
        /// <param name="nextLine">The next line in the markdown content.</param>
        /// <returns>True if a <br> tag should be added, otherwise false.</returns>
        private static bool ShouldAddLineBreak(string line, string nextLine)
        {
            return !string.IsNullOrWhiteSpace(nextLine) &&
                   !nextLine.TrimStart().StartsWith("#") &&
                   !nextLine.StartsWith("|") &&
                   !Regex.IsMatch(line, @"^\|[\s\-]*\|$");
        }
    }
}