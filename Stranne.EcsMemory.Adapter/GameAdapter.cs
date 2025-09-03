using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stranne.EcsMemory.Contracts;
using Stranne.EcsMemory.Contracts.Event;
using Stranne.EcsMemory.Contracts.Snapshots;
using Stranne.EcsMemory.Core;

namespace Stranne.EcsMemory.Adapter;
public sealed class GameAdapter : IDisposable
{
    private const string SaveFileName = "save.json";

    private readonly GameCore _gameCore;

    private uint _lastProcessedStateVersion;

    private GameAdapter(GameCore gameCore) =>
        _gameCore = gameCore;

    public static GameAdapter LoadOrCreateNewGame(int columns, int rows, int? seed, GameConfiguration gameConfiguration, IGameEvents gameEvents, ILoggerFactory? loggerFactory = null)
    {
        loggerFactory ??= NullLoggerFactory.Instance;
        return new GameAdapter(LoadGame(columns, rows, seed, gameConfiguration, gameEvents, loggerFactory));
    }

    public bool HasSnapshotChanged() =>
        _gameCore.GameSnapshot.CurrentStateVersion != _lastProcessedStateVersion;

    public GameSnapshot GetGameSnapshot()
    {
        var gameSnapshot = _gameCore.GameSnapshot;
        foreach (var cardSnapshot in gameSnapshot.Cards)
        {
            if (cardSnapshot.StateVersion > _lastProcessedStateVersion)
                cardSnapshot.HasChanged = true;
        }

        _lastProcessedStateVersion = gameSnapshot.CurrentStateVersion;
        return gameSnapshot;
    }

    public void StartNewGame(int columns, int rows, int seed) =>
        _gameCore.StartNewGame(columns, rows, seed);

    public void FlipCardAt(int x, int y) =>
        _gameCore.FlipCardAt(x, y);

    public void Update(float deltaTime) =>
        _gameCore.Update(deltaTime);

    public void SaveGame() => 
        File.WriteAllText(SaveFileName, _gameCore.Serialize());

    private static GameCore LoadGame(int columns, int rows, int? seed, GameConfiguration gameConfiguration, IGameEvents gameEvents, ILoggerFactory loggerFactory)
    {
        if (!File.Exists(SaveFileName))
        {
            var gameCore = GameCore.Create(gameConfiguration, gameEvents, loggerFactory);
            gameCore.StartNewGame(columns, rows, seed ?? Random.Shared.Next());
            return gameCore;
        }

        var text = File.ReadAllText(SaveFileName);
        return GameCore.Create(gameConfiguration, gameEvents, loggerFactory, text);
    }

    public void Dispose() =>
        _gameCore.Dispose();
}