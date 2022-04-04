using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;

namespace azuredevops_export_wiki
{
    internal class ConsoleLogger : ILoggerExtended
    {
        private readonly Options _options;
        private readonly Stopwatch stopwatch;

        internal ConsoleLogger(Options options, bool measureTime = true)//TODO what should be the default value?
        {
            _options = options;
            if (measureTime)
            {
                stopwatch = Stopwatch.StartNew();
            }
        }

        public void Log(string msg, LogLevel logLevel = LogLevel.Information, int indent = 0)
        {
            string prefix = String.Empty;
            if (stopwatch is not null)
            {
                stopwatch.Stop();
                prefix = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff} ";
                stopwatch.Start();
            }
            var indentString = new string(' ', indent * 2);
            indentString = prefix + indentString;
            if (_options.Debug && logLevel == LogLevel.Debug)
            {
                Console.WriteLine(indentString + msg);
            }

            if (_options.Verbose && logLevel == LogLevel.Information)
            {
                Console.WriteLine(indentString + msg);
            }

            if (logLevel == LogLevel.Warning)
            {
                var color = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(indentString + $"WARN: {msg}");
                Console.ForegroundColor = color;
            }

            if (logLevel == LogLevel.Error)
            {
                var color = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(indentString + $"ERR: {msg}");
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
}
