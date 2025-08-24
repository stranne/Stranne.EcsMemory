using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stranne.EcsMemory.Contracts;
using Stranne.EcsMemory.Contracts.Event;
using Stranne.EcsMemory.Core;

namespace Stranne.EcsMemory.Adapter;
public sealed class MemoryAdapter : IDisposable
{
    public event Action<WonEvent>? Won;

    private readonly MemoryGameCore _memoryGameCore;
    private int _lastVersion;

    public MemoryAdapter(ILoggerFactory? loggerFactory = null)
    {
        loggerFactory ??= NullLoggerFactory.Instance;
        _memoryGameCore = MemoryGameCore.Create(loggerFactory);
    }

    public RenderModel RenderModel => _memoryGameCore.RenderModel;

    public void StartNewGame(int columns, int rows, int seed) =>
        _memoryGameCore.StartNewGame(columns, rows, seed);

    public void FlipCardAt(int x, int y) =>
        _memoryGameCore.FlipCardAt(x, y);

    public void Update(float deltaTime)
    {
        _memoryGameCore.Update(deltaTime);
        ProcessEvents();
    }

    public bool HasRenderModelChanged()
    {
        var hasRenderModelChanged = _memoryGameCore.RenderModel.Version != _lastVersion;
        _lastVersion = _memoryGameCore.RenderModel.Version;
        return hasRenderModelChanged;
    }

    private void ProcessEvents()
    {
        foreach (var @event in _memoryGameCore.PopEvents())
        {
            switch (@event)
            {
                case WonEvent wonEvent:
                    Won?.Invoke(wonEvent);
                    break;
            }
        }
    }

    public void Dispose() => 
        _memoryGameCore.Dispose();
}