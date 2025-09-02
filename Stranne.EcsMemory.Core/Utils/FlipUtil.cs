using Arch.Core;
using Microsoft.Extensions.Logging;
using Stranne.EcsMemory.Core.Components.Singleton;
using Stranne.EcsMemory.Core.Components.Tags;
using Stranne.EcsMemory.Core.Components.Value;
using Stranne.EcsMemory.Core.Extensions;

namespace Stranne.EcsMemory.Core.Utils;
internal static class FlipUtil
{
    private static readonly QueryDescription FlippedButNotMatchedQuery = new QueryDescription()
        .WithAll<Revealed>()
        .WithNone<Matched>();

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

        var isFirstFlipped = !world.TryGetFirst(FlippedButNotMatchedQuery, out _);

        world.Add<Revealed>(cardEntity);
        world.IncrementStateVersion();
        world.MarkChanged(cardEntity);

        if (isFirstFlipped)
        {
            logger.LogDebug("Flipped first card at {GridPosition}.", flipGridPosition);
        }
        else
        {
            gameState.IsLocked = true;

            // TODO ignore pending evaluation if they match, as there is no need to wait for feedback in that case

            var config = world.GetSingletonRef<Config>();
            world.Create(new PendingEvaluation { UpdatesLeft = config.EvalDelayUpdates });

            logger.LogDebug("Flipped second card at {GridPosition}, evaluation pending.", flipGridPosition);
        }
    }

    private static bool TryGetCardToFlipByGridPosition(World world, GridPosition flipGridPosition, out Entity cardEntity)
    {
        var cardQuery = new QueryDescription()
            .WithAll<GridPosition, PairKey, CardId>();
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
