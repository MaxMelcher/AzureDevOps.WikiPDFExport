using System.Threading.Tasks;
using CommandLine;

namespace azuredevops_export_wiki
{
    partial class Program
    {
        static async Task<int> Main(string[] args)
        {
            var returnCode = await Parser.Default.ParseArguments<Options>(args)
                .MapResult(
                    ExecuteWikiPDFExporter,
                    e => Task.FromResult(-1));
            return returnCode;
        }

        static async Task<int> ExecuteWikiPDFExporter(Options options)
        {
            var logger = new ConsoleLogger(options);
            var exporter = new WikiPDFExporter(options, logger);
            bool succeeded = await exporter.Export();

            return succeeded ? 0 : 1;
        }
    }
}
