using Arch.Core;
using Arch.System;
using Stranne.EcsMemory.Core.Commands;
using Stranne.EcsMemory.Core.Commands.Abstractions;
using Stranne.EcsMemory.Core.Components.Singleton;
using Stranne.EcsMemory.Core.Utils;

namespace Stranne.EcsMemory.Core.Systems;
internal sealed class CommandProcessingSystem(World world, ICommandBuffer buffer)
    : BaseSystem<World, int>(world)
{
    public override void Update(in int _)
    {
        while (buffer.TryDequeue(out var command))
        {
            switch (command)
            {
                case StartNewGame startNewGame:
                    var config = new Config(startNewGame.Cols, startNewGame.Rows, 10, startNewGame.Seed ?? 1);

                    // Remove any existing Config component before setting the new one.
                    var cfgQuery = new QueryDescription().WithAll<Config>();
                    World.Query(in cfgQuery, (ref Entity entity, ref Config _) => World.Destroy(entity));
                    World.Create(config);

                    BoardSetupUtil.BuildBoard(World, config);
                    break;
                case FlipAtGrid flipAtGrid:
                    break;
                default:
                    throw new NotImplementedException($"Command {command.GetType().Name} isn't supported.");
            }
        }
    }
}
