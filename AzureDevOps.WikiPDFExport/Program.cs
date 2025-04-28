using System.Threading.Tasks;
using AzureDevOps.WikiPdfExport;
using CommandLine;

var returnCode = await Parser.Default.ParseArguments<Options>(args)
				.MapResult(
					ExecuteWikiPdfExporter,
					e => Task.FromResult(-1)).ConfigureAwait(false);
return returnCode;

static async Task<int> ExecuteWikiPdfExporter(Options options)
{
	var logger = new ConsoleLogger(options);
	var exporter = new WikiPdfExporter(options, logger);
	var succeeded = await exporter.Export().ConfigureAwait(false);

	return succeeded ? 0 : 1;
}
