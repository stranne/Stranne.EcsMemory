using Arch.Core;
using Arch.Core.Utils;
using Arch.Persistence;
using Microsoft.Extensions.Logging;
using Stranne.EcsMemory.Contracts.Event;
using Stranne.EcsMemory.Contracts.Snapshots;
using Stranne.EcsMemory.Core.Commands;
using Stranne.EcsMemory.Core.Commands.Abstractions;
using Stranne.EcsMemory.Core.Components.Singleton;
using Stranne.EcsMemory.Core.Components.Tags;
using Stranne.EcsMemory.Core.Components.Value;
using Stranne.EcsMemory.Core.Events;
using Stranne.EcsMemory.Core.Systems;

namespace Stranne.EcsMemory.Core;
public sealed class GameCore : IDisposable
{
    internal readonly World World;
    private readonly ISystemManager _systemManager;
    private readonly ICommandBuffer _commandBuffer;
    private readonly IEventManager _eventManager;

    internal GameCore(World world, ISystemManager systemManager, ICommandBuffer commandBuffer, IEventManager eventManager)
    {
        World = world;
        _systemManager = systemManager;
        _commandBuffer = commandBuffer;
        _eventManager = eventManager;
    }

    private static GameCore Create(World world, IGameEvents gameEvents, ILoggerFactory loggerFactory)
    {
        RegisterComponents();
        var commandBuffer = new CommandBuffer();
        var systemManager = new SystemManager(world, commandBuffer, loggerFactory);
        var eventManager = new EventManager(gameEvents);

        return new GameCore(world, systemManager, commandBuffer, eventManager);
    }

    public static GameCore Create(IGameEvents gameEvents, ILoggerFactory loggerFactory) =>
        Create(World.Create(), gameEvents, loggerFactory);

    public static GameCore Create(IGameEvents gameEvents, ILoggerFactory loggerFactory, string json) =>
        Create(Deserialize(json), gameEvents, loggerFactory);

    public void StartNewGame(int columns, int rows, int seed) =>
        _commandBuffer.Enqueue(new StartNewGame(columns, rows, seed));

    public void FlipCardAt(int x, int y) =>
        _commandBuffer.Enqueue(new FlipCardAt(x, y));

    public void Update(float deltaTime)
    {
        _systemManager.Update(deltaTime);
        _eventManager.Dequeue();
    }

    public string Serialize()
    {
        var archSerializer = new ArchJsonSerializer();
        var bytes = archSerializer.Serialize(World);
        return Convert.ToBase64String(bytes);
    }

    private static World Deserialize(string data)
    {
        RegisterComponents();
        var archSerializer = new ArchJsonSerializer();
        var bytes = Convert.FromBase64String(data);
        return archSerializer.Deserialize(bytes);
    }

    public GameSnapshot GameSnapshot =>
        _systemManager.GameSnapshot;

    private static void RegisterComponents()
    {
        ComponentRegistry.Add<CardId>();
        ComponentRegistry.Add<GridPosition>();
        ComponentRegistry.Add<PairKey>();
        ComponentRegistry.Add<LastChangedStateVersion>();
        ComponentRegistry.Add<GameState>();
        ComponentRegistry.Add<Config>();
        ComponentRegistry.Add<PendingEvaluation>();
        ComponentRegistry.Add<Matched>();
        ComponentRegistry.Add<Revealed>();
    }

    public void Dispose()
    {
        World.Dispose();
        _systemManager.Dispose();
        _eventManager.Dispose();
    }
}