using Arch.Core;
using Microsoft.Extensions.Logging;
using Stranne.EcsMemory.Core.Commands.Base;
using Stranne.EcsMemory.Core.Components.Value;
using Stranne.EcsMemory.Core.Utils;

namespace Stranne.EcsMemory.Core.Commands.Handlers;
internal static class FlipCardAtHandler
{
    internal static CommandResult Execute(FlipCardAt command, World world, ILogger logger)
    {
        var gridPosition = new GridPosition(command.X, command.Y);
        return FlipUtil.TryFlip(world, gridPosition, logger);
    }
}