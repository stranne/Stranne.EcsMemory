using System;

namespace Stranne.EcsMemory.Game.Utils;
internal sealed class GodotNullScope : IDisposable
{
    public static GodotNullScope Instance { get; } = new();
    private GodotNullScope() { }
    public void Dispose() { }
}