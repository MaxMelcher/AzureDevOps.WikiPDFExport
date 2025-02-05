using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace azuredevops_export_wiki
{
    internal static class MarkdownPreprocessor
    {
        // Temporary marker for WorkItem references
        private const string WorkItemMarker = "§§WORKITEM§§";

        // Temporary marker for line breaks that need to be added after WorkItem processing
        private const string BrMarker = "§§BR§§";

        // Regex pattern to match WorkItem references like #123
        private const string WorkItemPattern = @"(^|[^#])(#\d+)($|[^#])";

        // Regex pattern to match headers and ensure a space after the # characters
        private const string HeaderPattern = @"^(#+)([^#\s])(.*)$";

        // Preprocesses the markdown content by:
        // 1. Temporarily marking WorkItem references.
        // 2. Ensuring spaces after headers.
        // 3. Converting single line breaks to <br> tags where appropriate.
        public static string PreprocessMarkdown(string markdown)
        {
            // Temporarily mark WorkItem references to avoid interference with other processing
            var markedText = Regex.Replace(markdown, WorkItemPattern,
                match => match.Groups[1].Value + WorkItemMarker + match.Groups[2].Value + match.Groups[3].Value);

            // Ensure a space after # characters in headers
            var processedText = Regex.Replace(markedText, HeaderPattern, "$1 $2$3", RegexOptions.Multiline);

            // Convert single line breaks to <br> tags, but skip:
            // - Code blocks (between ```)
            // - Header lines
            // - Blank lines
            // - Tables
            var lines = processedText.Split('\n');
            var processedLines = new List<string>();
            var isInCodeBlock = false;
            var isInTable = false;

            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i].TrimEnd();
                var nextLine = i < lines.Length - 1 ? lines[i + 1].TrimEnd() : "";

                // Toggle code block state if the line starts or ends a code block
                isInCodeBlock = ToggleCodeBlockState(line, isInCodeBlock);

                // Toggle table state if the line starts or ends a table
                isInTable = ToggleTableState(line, isInTable);

                // Skip processing if the line is inside a code block, table, or is a header/blank line
                if (ShouldSkipLineProcessing(line, isInCodeBlock, isInTable))
                {
                    processedLines.Add(line);
                    continue;
                }

                // Add a <br> tag or a worItem marker if the line meets the criteria
                if (ShouldAddLineBreak(line, nextLine))
                {
                    line += line.Contains(WorkItemMarker) ? BrMarker : "<br>";
                }

                processedLines.Add(line);
            }

            // Combine the processed lines and replace temporary markers
            var result = new StringBuilder(string.Join("\n", processedLines))
                .Replace(WorkItemMarker, "") // Remove WorkItem markers
                .Replace(BrMarker, "<br>") // Replace BR markers with <br> tags
                .ToString();

            return result;
        }

        // Determines if current line is in code section
        private static bool ToggleCodeBlockState(string line, bool isInCodeBlock)
        {
            if (line.Trim().StartsWith("```"))
            {
                return !isInCodeBlock;
            }
            return isInCodeBlock;
        }

        // Determines if current line is in table
        private static bool ToggleTableState(string line, bool isInTable)
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

        // Determines whether the current line should be skipped during processing.
        private static bool ShouldSkipLineProcessing(string line, bool isInCodeBlock, bool isInTable)
        {
            return isInCodeBlock || isInTable || string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#");
        }

        // Determines whether a <br> tag should be added to the current line
        private static bool ShouldAddLineBreak(string line, string nextLine)
        {
            return !string.IsNullOrWhiteSpace(nextLine) &&
                   !nextLine.TrimStart().StartsWith("#") &&
                   !nextLine.StartsWith("|") &&
                   !Regex.IsMatch(line, @"^\|[\s\-]*\|$");
        }
    }
}