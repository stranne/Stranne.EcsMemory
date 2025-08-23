using System;
using Godot;
using Microsoft.Extensions.Logging;
using Environment = System.Environment;

namespace Stranne.EcsMemory.GodotGame.Utils;

internal sealed class GodotLogger(string category, LogLevel minLevel) : ILogger
{
    public IDisposable BeginScope<TState>(TState state) where TState : notnull => 
        GodotNullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => 
        logLevel >= minLevel;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string>? formatter)
    {
        if (!IsEnabled(logLevel) || formatter is null)
            return;

        var message = formatter(state, exception);
        if (string.IsNullOrEmpty(message) && exception is null)
            return;

        var composed = $"{DateTime.Now:HH:mm:ss.fff} [{logLevel}] {category}: {message}";
        if (exception is not null)
            composed += Environment.NewLine + exception;

        switch (logLevel)
        {
            case LogLevel.Critical:
            case LogLevel.Error:
                GD.PushError(composed);
                break;
            case LogLevel.Warning:
                GD.PushWarning(composed);
                break;
            case LogLevel.None:
                break;
            case LogLevel.Trace:
            case LogLevel.Debug:
            case LogLevel.Information:
            default:
                GD.Print(composed);
                break;
        }
    }
}