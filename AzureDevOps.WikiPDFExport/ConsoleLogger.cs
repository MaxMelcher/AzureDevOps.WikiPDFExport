using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace AzureDevOps.WikiPdfExport;

internal class ConsoleLogger(Options options) : ILoggerExtended
{
	private readonly Stopwatch stopwatch = Stopwatch.StartNew();

	public void Log(string message, LogLevel logLevel = LogLevel.Information, int indent = 0)
	{
		var prefix = string.Empty;
		if (stopwatch is not null)
		{
			stopwatch.Stop();
			prefix = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff} ";
			stopwatch.Start();
		}
		var indentString = new string(' ', indent * 2);
		indentString = prefix + indentString;
		if (options.Debug && logLevel == LogLevel.Debug)
		{
			Console.WriteLine(indentString + message);
		}

		if (options.Verbose && logLevel == LogLevel.Information)
		{
			Console.WriteLine(indentString + message);
		}

		if (logLevel == LogLevel.Warning)
		{
			var color = Console.ForegroundColor;
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine(indentString + $"WARN: {message}");
			Console.ForegroundColor = color;
		}

		if (logLevel == LogLevel.Error)
		{
			var color = Console.ForegroundColor;
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine(indentString + $"ERR: {message}");
			Console.ForegroundColor = color;
		}
	}

	public void LogMeasure(string msg)
	{
		var color = Console.ForegroundColor;
		Console.ForegroundColor = ConsoleColor.Green;
		Console.WriteLine();
		Console.WriteLine(msg);
		Console.ForegroundColor = color;
	}
}
