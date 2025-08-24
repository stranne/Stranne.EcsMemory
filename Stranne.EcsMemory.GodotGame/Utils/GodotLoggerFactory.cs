using Microsoft.Extensions.Logging;

namespace Stranne.EcsMemory.GodotGame.Utils;
/// <summary>
/// ILoggerFactory that forwards logs to Godot's GD.* methods.
/// </summary>
public sealed class GodotLoggerFactory(LogLevel minLevel = LogLevel.Information) : ILoggerFactory
{
    public static GodotLoggerFactory Instance { get; } = new();

    public ILogger CreateLogger(string categoryName) =>
        new GodotLogger(categoryName, minLevel);

    public void AddProvider(ILoggerProvider provider)
    {
        // No external providers supported in this minimal implementation.
    }

    public void Dispose()
    {
        // Nothing to dispose.
    }
}