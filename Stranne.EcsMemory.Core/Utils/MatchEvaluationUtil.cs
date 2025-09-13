using Arch.Core;
using Microsoft.Extensions.Logging;
using Stranne.EcsMemory.Core.Components.Singleton;
using Stranne.EcsMemory.Core.Components.Tags;
using Stranne.EcsMemory.Core.Components.Value;
using Stranne.EcsMemory.Core.Extensions;

namespace Stranne.EcsMemory.Core.Utils;

internal static class MatchEvaluationUtil
{
    private static readonly QueryDescription RevealedUnmatchedQuery = new QueryDescription()
        .WithAll<CardId, Revealed>()
        .WithNone<Matched>();

    public static bool TryGetTwoRevealedUnmatched(World world, out Entity entity1, out Entity entity2)
    {
        Entity firstEntity = default;
        Entity secondEntity = default;
        int firstCardId = int.MaxValue;
        int secondCardId = int.MaxValue;
        int foundCount = 0;

        world.Query(in RevealedUnmatchedQuery, (Entity currentEntity, ref CardId cardId) =>
        {
            var currentCardId = cardId.Value;

            if (foundCount == 0)
            {
                firstEntity = currentEntity;
                firstCardId = currentCardId;
                foundCount++;
                return;
            }

            if (currentCardId < firstCardId)
            {
                secondEntity = firstEntity;
                secondCardId = firstCardId;
                firstEntity = currentEntity;
                firstCardId = currentCardId;
            }
            else if (currentCardId < secondCardId)
            {
                secondEntity = currentEntity;
                secondCardId = currentCardId;
            }

            if (foundCount == 1)
                foundCount++;
        });

        if (foundCount < 2)
        {
            entity1 = default;
            entity2 = default;
            return false;
        }

        entity1 = firstEntity;
        entity2 = secondEntity;
        return true;
    }

    public static bool DoesMatch(World world, Entity entity1, Entity entity2)
    {
        if (!world.Has<PairKey>(entity1) || !world.Has<PairKey>(entity2))
            return false;

        var pairKeyA = world.Get<PairKey>(entity1);
        var pairKeyB = world.Get<PairKey>(entity2);
        return pairKeyA == pairKeyB;
    }

    public static void ApplyMatchResult(World world, Entity entity1, Entity entity2, bool isMatch, ILogger? logger = null)
    {
        if (isMatch)
        {
            // Synchronize state versions BEFORE adding Matched tags to avoid Arch ECS bug
            ref var gameState = ref world.GetSingletonRef<GameState>();
            var currentStateVersion = new LastChangedStateVersion(gameState.StateVersion);
            world.Set(entity1, currentStateVersion);
            world.Set(entity2, currentStateVersion);

            world.Add<Matched>(entity1);
            world.Add<Matched>(entity2);

            gameState.MatchedCount += 2;

            if (world.Has<PairKey>(entity1))
            {
                var pairKey = world.Get<PairKey>(entity1);
                logger?.LogDebug("Match found: pair {PairKey} ({MatchedCount}/{TotalCards})",
                    pairKey.Value, gameState.MatchedCount, gameState.TotalCards);
            }
        }
        else
        {
            world.Remove<Revealed>(entity1);
            world.Remove<Revealed>(entity2);

            if (world.Has<PairKey>(entity1) && world.Has<PairKey>(entity2))
            {
                var pairKeyA = world.Get<PairKey>(entity1);
                var pairKeyB = world.Get<PairKey>(entity2);
                logger?.LogDebug("No match: {PairKeyA} != {PairKeyB}", pairKeyA.Value, pairKeyB.Value);
            }
        }

        world.IncrementStateVersion();
        world.MarkChanged(entity1, entity2);
    }

    public static bool TryHandleImmediateMatch(World world, ILogger? logger = null)
    {
        if (!TryGetTwoRevealedUnmatched(world, out var firstEntity, out var secondEntity))
            return false;

        var isMatch = DoesMatch(world, firstEntity, secondEntity);
        if (!isMatch)
            return false;

        ApplyMatchResult(world, firstEntity, secondEntity, true, logger);

        ref var gameState = ref world.GetSingletonRef<GameState>();
        gameState.Moves++;
        gameState.IsLocked = false;

        return true;
    }
}