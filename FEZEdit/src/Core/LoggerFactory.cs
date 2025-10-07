using Serilog;

namespace FEZEdit.Core;

public static class LoggerFactory
{
    private static readonly ILogger BaseLogger;

    static LoggerFactory()
    {
        BaseLogger = new LoggerConfiguration()
            .WriteTo.Godot()
            .CreateLogger();
    }

    public static ILogger Create<T>() => BaseLogger.ForContext<T>();

    public static ILogger Create(string context) => BaseLogger.ForContext("SourceContext", context);
}