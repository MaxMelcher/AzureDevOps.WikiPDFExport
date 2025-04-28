using Microsoft.Extensions.Logging;

namespace AzureDevOps.WikiPdfExport;

#pragma warning disable CA1515 // Consider making public types internal
public interface ILogger
{
	void Log(string msg, LogLevel logLevel = LogLevel.Information, int indent = 0);
}

public interface ILoggerExtended : ILogger
{
	void LogMeasure(string msg);
}
#pragma warning restore CA1515 // Consider making public types internal
