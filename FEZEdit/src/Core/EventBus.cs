using System;
using System.Diagnostics.CodeAnalysis;
using Godot;
using Serilog;

namespace FEZEdit.Core;

public static class EventBus
{
    public enum EventType {
        Information,
        Progress,
        Success,
        Warning,
        Error
    }

    private static readonly ILogger Logger = LoggerFactory.Create("Events");

    public static event Action<EventType, string> MessageSent;
    
    public static event Action<ProgressValue, string> ProgressUpdated;
    
    public static void Info(
        [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string messageTemplate,
        params object[] args)
    {
        SendMessage(EventType.Information, messageTemplate, args);
    }
    
    public static void Success(
        [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string messageTemplate,
        params object[] args)
    {
        SendMessage(EventType.Success, messageTemplate, args);
    }
    
    public static void Warning(
        [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string messageTemplate,
        params object[] args)
    {
        SendMessage(EventType.Warning, messageTemplate, args);
    }
    
    public static void Error(
        [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string messageTemplate,
        params object[] args)
    {
        SendMessage(EventType.Error, messageTemplate, args);
    }
    
    public static void Error(Exception exception, params object[] args)
    {
        SendMessage(EventType.Error, exception.Message, args);
    }
    
    public static void Progress(
        ProgressValue progress,
        [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string messageTemplate = "",
        params object[] args)
    {
        var translatedMessageTemplate = TranslationServer.Translate(messageTemplate).ToString();
        var formattedMessage = string.Format(translatedMessageTemplate, args);
        ProgressUpdated?.Invoke(progress, formattedMessage);
        if (!string.IsNullOrEmpty(formattedMessage))
        {
            SendMessage(EventType.Progress, formattedMessage);
            Logger.Information("({0}/{1}) {2}", progress.Value, progress.Maximum, formattedMessage);
        }
    }

    private static void SendMessage(EventType type, string messageTemplate, params object[] args)
    {
        var translatedMessageTemplate = TranslationServer.Translate(messageTemplate).ToString();
        MessageSent?.Invoke(type, string.Format(translatedMessageTemplate, args));
    }
}