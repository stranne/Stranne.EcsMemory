using Arch.Core;
using Stranne.EcsMemory.Core.Components.Singleton;
using Stranne.EcsMemory.Core.Extensions;

namespace Stranne.EcsMemory.Core.Tests.Common;
internal static class TestWorldFactory
{
    public static World Create(int cols = 4, int rows = 3, int evalDelayUpdates = 10, int seed = 123, bool isLocked = false, bool isWon = false, int totalCards = 0, int matchedCount = 0, int moves = 0)
    {
        var world = World.Create();
        world.SetOrCreateSingleton(new GameState
        {
            IsLocked = isLocked,
            FirstFlipped = null,
            Moves = moves,
            IsWon = isWon,
            TotalCards = totalCards,
            MatchedCount = matchedCount
        });
        world.SetOrCreateSingleton(new Config(cols, rows, evalDelayUpdates, seed));
        return world;
    }
}
