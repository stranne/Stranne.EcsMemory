using Arch.Core;
using Arch.System;
using Microsoft.Extensions.Logging;
using Stranne.EcsMemory.Core.Commands.Base;

namespace Stranne.EcsMemory.Core.Systems;
internal sealed class GameCommandSystem(World world, IGameCommandQueue buffer, ILogger<GameCommandSystem> logger)
    : BaseSystem<World, float>(world)
{
    public override void Update(in float _)
    {
        while (buffer.TryDequeue(out var command))
        {
            buffer.ExecuteCommand(command, World);
        }
    }
}
