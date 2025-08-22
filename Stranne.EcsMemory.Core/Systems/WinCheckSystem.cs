using Arch.Core;
using Arch.System;
using Stranne.EcsMemory.Core.Components.Singleton;
using Stranne.EcsMemory.Core.Extensions;

namespace Stranne.EcsMemory.Core.Systems;
internal sealed class WinCheckSystem(World world)
    : BaseSystem<World, int>(world)
{
    public override void Update(in int _)
    {
        ref var gameState = ref World.GetSingletonRef<GameState>();
        if (gameState.IsWon ||
            gameState.TotalCards == 0 ||
            gameState.MatchedCount < gameState.TotalCards)
            return;

        gameState.IsLocked = true;
        gameState.IsWon = true;
    }
}
