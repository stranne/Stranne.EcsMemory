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

    private static readonly QueryDescription RevealedUnmatchedQuery = new QueryDescription()
        .WithAll<CardId, Revealed>()
        .WithNone<Matched>();

    private static readonly QueryDescription CardByPositionQuery = new QueryDescription()
        .WithAll<GridPosition, PairKey, CardId>();

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

            // Check if the two revealed cards match - if so, skip pending evaluation
            if (TryHandleImmediateMatch(world, flipGridPosition, logger))
                return;

            // No immediate match - proceed with delayed evaluation
            var config = world.GetSingletonRef<Config>();
            world.Create(new PendingEvaluation { UpdatesLeft = config.EvalDelayUpdates });

            logger.LogDebug("Flipped second card at {GridPosition}, evaluation pending.", flipGridPosition);
        }
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

    private static bool TryGetTwoRevealedUnmatched(World world, out Entity entity1, out Entity entity2)
    {
        var candidates = new List<(Entity entity, int cardId)>();

        world.Query(in RevealedUnmatchedQuery, (Entity currentEntity, ref CardId cardId) => 
            candidates.Add((currentEntity, cardId.Value)));

        if (candidates.Count < 2)
        {
            entity1 = default;
            entity2 = default;
            return false;
        }

        candidates.Sort((a, b) => a.cardId.CompareTo(b.cardId));
        entity1 = candidates[0].entity;
        entity2 = candidates[1].entity;
        return true;
    }

    private static bool TryHandleImmediateMatch(World world, GridPosition flipGridPosition, ILogger logger)
    {
        if (!TryGetTwoRevealedUnmatched(world, out var firstEntity, out var secondEntity))
            return false;

        var pairKeyA = world.Get<PairKey>(firstEntity);
        var pairKeyB = world.Get<PairKey>(secondEntity);

        if (pairKeyA != pairKeyB)
            return false;

        world.Add<Matched>(firstEntity);
        world.Add<Matched>(secondEntity);

        ref var gameState = ref world.GetSingletonRef<GameState>();
        gameState.MatchedCount += 2;
        gameState.Moves++;
        gameState.IsLocked = false;

        world.IncrementStateVersion();
        world.MarkChanged(firstEntity, secondEntity);

        logger.LogDebug("Flipped second card at {GridPosition}, match found: pair {PairKey} ({MatchedCount}/{TotalCards})", 
            pairKeyA.Value, flipGridPosition, gameState.MatchedCount, gameState.TotalCards);

        return true;
    }
}
