using Arch.Core;
using Stranne.EcsMemory.Core.Commands.Base;
using Stranne.EcsMemory.Core.Components.Singleton;
using Stranne.EcsMemory.Core.Extensions;
using Stranne.EcsMemory.Core.Utils;

namespace Stranne.EcsMemory.Core.Commands.Handlers;
internal static class StartNewGameHandler
{
    // TODO make configurable
    private const int EvalDelayUpdates = 30;

    internal static CommandResult Execute(StartNewGame command, World world)
    {
        var config = new Config(command.Columns, command.Rows, EvalDelayUpdates, command.Seed);
        world.SetOrCreateSingleton(config);
        BoardSetupUtil.BuildBoard(world, config);
        return CommandResult.Success;
    }
}