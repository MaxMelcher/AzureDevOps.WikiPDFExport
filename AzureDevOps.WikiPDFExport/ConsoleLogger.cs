using Microsoft.Extensions.Logging;
using System;

namespace azuredevops_export_wiki
{
    internal class ConsoleLogger : ILoggerExtended
    {
        private Options _options;

        internal ConsoleLogger(Options options) { _options = options; }

        public void Log(string msg, LogLevel logLevel = LogLevel.Information, int indent = 0)
        {
            var indentString = new string(' ', indent * 2);
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
