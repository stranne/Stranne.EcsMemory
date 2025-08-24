using Arch.Core;
using Arch.System;
using Microsoft.Extensions.Logging;
using Stranne.EcsMemory.Core.Components.Events;
using Stranne.EcsMemory.Core.Components.Singleton;
using Stranne.EcsMemory.Core.Extensions;

namespace Stranne.EcsMemory.Core.Systems;
internal sealed class WinCheckSystem(World world, ILogger<WinCheckSystem> logger)
    : BaseSystem<World, float>(world)
{
    public override void Update(in float _)
    {
        ref var gameState = ref World.GetSingletonRef<GameState>();
        if (gameState.IsWon ||
            gameState.TotalCards == 0 ||
            gameState.MatchedCount < gameState.TotalCards)
            return;

        gameState.IsLocked = true;
        gameState.IsWon = true;
        gameState.StateVersion++;

        World.Create(
            new EventMetadata(gameState.StateVersion),
            new EventWon(gameState.Moves, gameState.TotalCards));
        logger.LogDebug("Game won in {Moves} moves!", gameState.Moves);
    }
}
