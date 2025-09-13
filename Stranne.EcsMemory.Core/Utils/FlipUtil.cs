using Arch.Core;
using Microsoft.Extensions.Logging;
using Stranne.EcsMemory.Core.Commands.Base;
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


    private static readonly QueryDescription CardByPositionQuery = new QueryDescription()
        .WithAll<GridPosition, PairKey, CardId>();

    public static CommandResult TryFlip(World world, GridPosition flipGridPosition, ILogger logger)
    {
        ref var gameState = ref world.GetSingletonRef<GameState>();

        if (gameState.IsLocked)
            return CommandResult.Skipped;

        var found = TryGetCardToFlipByGridPosition(world, flipGridPosition, out var cardEntity);

        if (!found)
        {
            logger.LogWarning("Tried to flip at {GridPosition}, but no card was found.", flipGridPosition);
            return CommandResult.Skipped;
        }

        if (world.Has<Matched>(cardEntity))
            return CommandResult.Skipped;

        if (world.Has<Revealed>(cardEntity))
            return CommandResult.Skipped;

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

            // Check if the two revealed cards match - if so, skip pending evaluation
            if (MatchEvaluationUtil.TryHandleImmediateMatch(world, logger))
                return CommandResult.Success;

            // No immediate match - proceed with delayed evaluation
            var config = world.GetSingletonRef<Config>();
            world.Create(new PendingEvaluation { UpdatesLeft = config.EvalDelayUpdates });

            logger.LogDebug("Flipped second card at {GridPosition}, evaluation pending.", flipGridPosition);
        }

        return CommandResult.Success;
    }

    private static bool TryGetCardToFlipByGridPosition(World world, GridPosition flipGridPosition, out Entity cardEntity)
    {
        Entity entity = default;
        var found = false;
        world.Query(in CardByPositionQuery, (Entity innerEntity, ref GridPosition cardGridPosition) =>
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
