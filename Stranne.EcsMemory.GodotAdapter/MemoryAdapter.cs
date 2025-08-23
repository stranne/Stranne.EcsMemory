using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stranne.EcsMemory.Contracts;
using Stranne.EcsMemory.Core;

namespace Stranne.EcsMemory.GodotAdapter;
public sealed class MemoryAdapter : IDisposable
{
    public event Action<WinInfo>? Won;

    private readonly MemoryGameCore _memoryGameCore;
    private bool _wasWon;
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

        var model = _memoryGameCore.RenderModel;
        if (!_wasWon && model.IsWon)
            Won?.Invoke(new WinInfo(model.Moves, model.Cards.Count, DateTimeOffset.UtcNow));

        _wasWon = model.IsWon;
    }

    public bool HasRenderModelChanged()
    {
        var hasRenderModelChanged = _memoryGameCore.RenderModel.Version != _lastVersion;
        _lastVersion = _memoryGameCore.RenderModel.Version;
        return hasRenderModelChanged;
    }

    public void Dispose() => 
        _memoryGameCore.Dispose();
}