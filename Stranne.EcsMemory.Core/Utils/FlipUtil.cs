using Arch.Core;
using Microsoft.Extensions.Logging;
using Stranne.EcsMemory.Core.Components.Singleton;
using Stranne.EcsMemory.Core.Components.Tags;
using Stranne.EcsMemory.Core.Components.Value;
using Stranne.EcsMemory.Core.Extensions;

namespace Stranne.EcsMemory.Core.Utils;
internal static class FlipUtil
{
    public static void TryFlip(World world, GridPosition flipGridPosition, ILogger logger)
    {
        ref var gameState = ref world.GetSingletonRef<GameState>();

        if (gameState.IsLocked)
            return;

        var found = TryGetCardToFlipByGridPosition(world, flipGridPosition, out var cardEntity);

        if (!found)
        {
            logger.LogWarning("Tried to flip at {GridPosition}, but no card was found.", flipGridPosition);
            return;
        }

        if (world.Has<Matched>(cardEntity))
            return;

        if (world.Has<Revealed>(cardEntity))
            return;

        world.Add<Revealed>(cardEntity);

        if (gameState.FirstFlipped is null)
        {
            gameState.FirstFlipped = cardEntity;

            logger.LogDebug("Flipped first card at {GridPosition}.", flipGridPosition);
        }
        else
        {
            gameState.IsLocked = true;

            var config = world.GetSingletonRef<Config>();
            world.Create(new PendingEvaluation { UpdatesLeft = config.EvalDelayUpdates });

            logger.LogDebug("Flipped second card at {GridPosition}, evaluation pending.", flipGridPosition);
        }

        gameState.StateVersion++;
    }

    private static bool TryGetCardToFlipByGridPosition(World world, GridPosition flipGridPosition, out Entity cardEntity)
    {
        var cardQuery = new QueryDescription()
            .WithAll<GridPosition, Selectable, PairKey, CardId>();
        Entity entity = default;
        var found = false;
        world.Query(in cardQuery, (Entity innerEntity, ref GridPosition cardGridPosition) =>
        {
            if (cardGridPosition != flipGridPosition)
                return;

            entity = innerEntity;
            found = true;
        });

        cardEntity = entity;
        return found;
    }
}
