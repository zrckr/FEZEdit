using Godot;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;
using Serilog.Parsing;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using FEZEdit.Core;

namespace FEZEdit.Core;

public class GodotSink(string outputTemplate, IFormatProvider formatProvider) : ILogEventSink
{
    private readonly MessageTemplateTextFormatter _formatter = new(outputTemplate, formatProvider);

    public void Emit(LogEvent logEvent)
    {
        using TextWriter writer = new StringWriter();
        _formatter.Format(logEvent, writer);
        writer.Flush();

        string color = logEvent.Level switch
        {
            LogEventLevel.Debug => Colors.SpringGreen.ToHtml(),
            LogEventLevel.Information => Colors.Cyan.ToHtml(),
            LogEventLevel.Warning => Colors.Yellow.ToHtml(),
            LogEventLevel.Error => Colors.Red.ToHtml(),
            LogEventLevel.Fatal => Colors.Purple.ToHtml(),
            _ => Colors.LightGray.ToHtml(),
        };

        foreach (string line in writer.ToString()?.Split('\n') ?? [])
        {
            GD.PrintRich($"[color=#{color}]{line}[/color]");
        }

        if (logEvent.Exception is null)
        {
            return;
        }

        if (logEvent.Level >= LogEventLevel.Error)
        {
            GD.PushError(logEvent.Exception);
        }
        else
        {
            GD.PushWarning(logEvent.Exception);
        }
    }
}

public static class GodotSinkExtensions
{
    private const string DefaultGodotSinkOutputTemplate =
        "({Timestamp:yyyy-MM-dd HH:mm:ss.fff}) [{Level:u3}] {SourceContext} : {Message:lj}{Exception}";

    public static LoggerConfiguration Godot(this LoggerSinkConfiguration configuration,
        string outputTemplate = DefaultGodotSinkOutputTemplate,
        IFormatProvider formatProvider = null)
    {
        return configuration.Sink(new GodotSink(outputTemplate, formatProvider));
    }
}