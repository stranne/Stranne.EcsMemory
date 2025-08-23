using Arch.Core;
using Arch.System;
using Microsoft.Extensions.Logging;
using Stranne.EcsMemory.Core.Commands;
using Stranne.EcsMemory.Core.Commands.Abstractions;
using Stranne.EcsMemory.Core.Components.Singleton;
using Stranne.EcsMemory.Core.Components.Value;
using Stranne.EcsMemory.Core.Extensions;
using Stranne.EcsMemory.Core.Utils;

namespace Stranne.EcsMemory.Core.Systems;
internal sealed class CommandProcessingSystem(World world, ICommandBuffer buffer, ILogger<CommandProcessingSystem> logger)
    : BaseSystem<World, float>(world)
{
    private const int EvalDelayTicks = 10;

    public override void Update(in float _)
    {
        while (buffer.TryDequeue(out var command))
        {
            switch (command)
            {
                case StartNewGame startNewGame:
                    var config = new Config(startNewGame.Columns, startNewGame.Rows, EvalDelayTicks, startNewGame.Seed);
                    World.SetOrCreateSingleton(config);

                    BoardSetupUtil.BuildBoard(World, config);
                    break;
                case FlipCardAt flipCardAt:
                    var gridPosition = new GridPosition(flipCardAt.X, flipCardAt.Y);
                    FlipUtil.TryFlip(World, gridPosition, logger);
                    break;
                default:
                    throw new NotImplementedException($"Command {command.GetType().Name} isn't supported.");
            }
        }
    }
}
