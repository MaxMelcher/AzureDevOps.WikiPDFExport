using Microsoft.Extensions.Logging;

namespace azuredevops_export_wiki
{
    public interface ILogger
    {
        void Log(string msg, LogLevel logLevel = LogLevel.Information, int indent = 0);
    }
}
