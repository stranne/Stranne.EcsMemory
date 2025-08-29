using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stranne.EcsMemory.Contracts;
using Stranne.EcsMemory.Contracts.Event;
using Stranne.EcsMemory.Core;

namespace Stranne.EcsMemory.Adapter;
public sealed class MemoryAdapter : IDisposable
{
    private readonly MemoryGameCore _memoryGameCore;

    private int _lastVersion;

    public MemoryAdapter(IGameEvents gameEvents, ILoggerFactory? loggerFactory = null)
    {
        loggerFactory ??= NullLoggerFactory.Instance;
        _memoryGameCore = MemoryGameCore.Create(gameEvents, loggerFactory);
    }

    public RenderModel RenderModel => _memoryGameCore.RenderModel;

    public void StartNewGame(int columns, int rows, int seed) =>
        _memoryGameCore.StartNewGame(columns, rows, seed);

    public void FlipCardAt(int x, int y) =>
        _memoryGameCore.FlipCardAt(x, y);

    public void Update(float deltaTime) => 
        _memoryGameCore.Update(deltaTime);

    public bool HasRenderModelChanged()
    {
        var hasRenderModelChanged = _memoryGameCore.RenderModel.Version != _lastVersion;
        _lastVersion = _memoryGameCore.RenderModel.Version;
        return hasRenderModelChanged;
    }

    public void Dispose() => 
        _memoryGameCore.Dispose();
}