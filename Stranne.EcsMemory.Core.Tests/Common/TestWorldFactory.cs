using Arch.Core;
using Stranne.EcsMemory.Core.Components.Singleton;
using Stranne.EcsMemory.Core.Extensions;

namespace Stranne.EcsMemory.Core.Tests.Common;
internal static class TestWorldFactory
{
    public static World Create(int cols = 4, int rows = 3, int evalDelayTicks = 10, int seed = 123, bool isLocked = false)
    {
        var world = World.Create();
        world.SetOrCreateSingleton(new GameState
        {
            IsLocked = isLocked,
            FirstFlipped = null,
            Moves = 0
        });
        world.SetOrCreateSingleton(new Config(cols, rows, evalDelayTicks, seed));
        return world;
    }
}
