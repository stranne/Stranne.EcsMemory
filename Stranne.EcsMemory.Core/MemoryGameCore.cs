using Arch.Core;
using Microsoft.Extensions.Logging;
using Stranne.EcsMemory.Contracts;
using Stranne.EcsMemory.Core.Commands;
using Stranne.EcsMemory.Core.Commands.Abstractions;
using Stranne.EcsMemory.Core.Systems;

namespace Stranne.EcsMemory.Core;
public sealed class MemoryGameCore
    : IDisposable
{
    private readonly World _world;
    private readonly SystemManager _systemManager;
    private readonly ICommandBuffer _commandBuffer;

    internal MemoryGameCore(World world, SystemManager systemManager, ICommandBuffer commandBuffer)
    {
        _world = world;
        _systemManager = systemManager;
        _commandBuffer = commandBuffer;
    }

    public static MemoryGameCore Create(ILoggerFactory loggerFactory)
    {
        var world = World.Create();
        var commandBuffer = new CommandBuffer();
        var systemGraph = new SystemManager(world, commandBuffer, loggerFactory);

        return new MemoryGameCore(world, systemGraph, commandBuffer);
    }

    public void StartNewGame(int columns, int rows, int seed) =>
        _commandBuffer.Enqueue(new StartNewGame(columns, rows, seed));

    public void FlipCardAt(int x, int y) =>
        _commandBuffer.Enqueue(new FlipCardAt(x, y));

    public void Tick(float deltaTime) =>
        _systemManager.Tick(deltaTime);

    public RenderModel RenderModel =>
        _systemManager.RenderModel;

    public void Dispose()
    {
        _world.Dispose();
        _systemManager.Dispose();
    }
}