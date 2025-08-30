using Arch.Core;
using Microsoft.Extensions.Logging;
using Stranne.EcsMemory.Contracts.Event;
using Stranne.EcsMemory.Contracts.Snapshots;
using Stranne.EcsMemory.Core.Commands;
using Stranne.EcsMemory.Core.Commands.Abstractions;
using Stranne.EcsMemory.Core.Events;
using Stranne.EcsMemory.Core.Systems;

namespace Stranne.EcsMemory.Core;
public sealed class MemoryGameCore : IDisposable
{
    private readonly World _world;
    private readonly ISystemManager _systemManager;
    private readonly ICommandBuffer _commandBuffer;
    private readonly IEventManager _eventManager;

    internal MemoryGameCore(World world, ISystemManager systemManager, ICommandBuffer commandBuffer, IEventManager eventManager)
    {
        _world = world;
        _systemManager = systemManager;
        _commandBuffer = commandBuffer;
        _eventManager = eventManager;
    }

    public static MemoryGameCore Create(IGameEvents gameEvents, ILoggerFactory loggerFactory)
    {
        var world = World.Create();
        var commandBuffer = new CommandBuffer();
        var systemManager = new SystemManager(world, commandBuffer, loggerFactory);
        var eventManager = new EventManager(gameEvents);

        return new MemoryGameCore(world, systemManager, commandBuffer, eventManager);
    }

    public void StartNewGame(int columns, int rows, int seed) =>
        _commandBuffer.Enqueue(new StartNewGame(columns, rows, seed));

    public void FlipCardAt(int x, int y) =>
        _commandBuffer.Enqueue(new FlipCardAt(x, y));

    public void Update(float deltaTime)
    {
        _systemManager.Update(deltaTime);
        _eventManager.Dequeue();
    }

    public GameSnapshot GameSnapshot =>
        _systemManager.GameSnapshot;

    public void Dispose()
    {
        _world.Dispose();
        _systemManager.Dispose();
        _eventManager.Dispose();
    }
}