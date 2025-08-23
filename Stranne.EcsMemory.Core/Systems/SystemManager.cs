using Arch.Core;
using Arch.System;
using Microsoft.Extensions.Logging;
using Stranne.EcsMemory.Contracts;
using Stranne.EcsMemory.Core.Commands.Abstractions;

namespace Stranne.EcsMemory.Core.Systems;
internal sealed class SystemManager : IDisposable
{
    private readonly Group<float> _systems;
    private readonly CommandProcessingSystem _commandProcessingSystem;
    private readonly MatchedSystem _matchedSystem;
    private readonly WinCheckSystem _winCheckSystem;
    private readonly RenderSystem _renderSystem;

    public SystemManager(World world, ICommandBuffer commandBuffer, ILoggerFactory loggerFactory)
    {
        _commandProcessingSystem = new CommandProcessingSystem(world, commandBuffer, loggerFactory.CreateLogger<CommandProcessingSystem>());
        _matchedSystem = new MatchedSystem(world);
        _winCheckSystem = new WinCheckSystem(world);
        _renderSystem = new RenderSystem(world);

        _systems = new Group<float>(
            typeof(SystemManager).FullName,
            _commandProcessingSystem,
            _matchedSystem,
            _winCheckSystem,
            _renderSystem);

        _systems.Initialize();
    }

    public RenderModel RenderModel => _renderSystem.RenderModel;

    public void Update(float deltaTime)
    {
        _systems.BeforeUpdate(in deltaTime);
        _systems.Update(in deltaTime);
        _systems.AfterUpdate(in deltaTime);
    }

    public void Dispose()
    {
        _systems.Dispose();
        _commandProcessingSystem.Dispose();
        _matchedSystem.Dispose();
        _winCheckSystem.Dispose();
        _renderSystem.Dispose();
    }
}