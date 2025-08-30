using Arch.Bus;
using Arch.Core;
using Arch.System;
using Microsoft.Extensions.Logging;
using Stranne.EcsMemory.Core.Components.Singleton;
using Stranne.EcsMemory.Core.Events;
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
        World.IncrementStateVersion();

        var gameWon = new GameWon(gameState.Moves, gameState.TotalCards, gameState.StateVersion);
        EventBus.Send(in gameWon);
        logger.LogDebug("Game won in {Moves} moves!", gameState.Moves);
    }
}
