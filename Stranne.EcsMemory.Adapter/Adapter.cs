using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stranne.EcsMemory.Contracts.Event;
using Stranne.EcsMemory.Contracts.Snapshots;
using Stranne.EcsMemory.Core;

namespace Stranne.EcsMemory.Adapter;
public sealed class GameAdapter : IDisposable
{
    private readonly MemoryGameCore _memoryGameCore;

    private uint _lastProcessedStateVersion;

    public GameAdapter(IGameEvents gameEvents, ILoggerFactory? loggerFactory = null)
    {
        loggerFactory ??= NullLoggerFactory.Instance;
        _memoryGameCore = MemoryGameCore.Create(gameEvents, loggerFactory);
    }

    public bool HasSnapshotChanged() => 
        _memoryGameCore.GameSnapshot.CurrentStateVersion != _lastProcessedStateVersion;

    public GameSnapshot GetGameSnapshot()
    {
        var gameSnapshot = _memoryGameCore.GameSnapshot;
        foreach (var cardSnapshot in gameSnapshot.Cards)
        {
            if (cardSnapshot.StateVersion > _lastProcessedStateVersion)
                cardSnapshot.HasChanged = true;
        }

        _lastProcessedStateVersion = gameSnapshot.CurrentStateVersion;
        return gameSnapshot;
    }

    public void StartNewGame(int columns, int rows, int seed) =>
        _memoryGameCore.StartNewGame(columns, rows, seed);

    public void FlipCardAt(int x, int y) =>
        _memoryGameCore.FlipCardAt(x, y);

    public void Update(float deltaTime) => 
        _memoryGameCore.Update(deltaTime);

    public void Dispose() => 
        _memoryGameCore.Dispose();
}