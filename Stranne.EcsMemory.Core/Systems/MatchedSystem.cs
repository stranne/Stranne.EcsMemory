using Arch.Core;
using Arch.System;
using Microsoft.Extensions.Logging;
using Stranne.EcsMemory.Core.Components.Singleton;
using Stranne.EcsMemory.Core.Components.Tags;
using Stranne.EcsMemory.Core.Components.Value;
using Stranne.EcsMemory.Core.Extensions;

namespace Stranne.EcsMemory.Core.Systems;
internal sealed class MatchedSystem(World world, ILogger<MatchedSystem> logger)
    : BaseSystem<World, float>(world)
{
    internal static readonly QueryDescription PendingEvaluationQuery = new QueryDescription().WithAll<PendingEvaluation>();
    internal static readonly QueryDescription RevealedUnmatchedQuery = new QueryDescription().WithAll<CardId, Revealed>().WithNone<Matched>();

    public override void Update(in float _)
    {
        if (!TryGetPendingEvaluationEntity(out var pendingEvaluationEntity))
            return;

        ref var pendingEvaluation = ref World.Get<PendingEvaluation>(pendingEvaluationEntity);

        pendingEvaluation.UpdatesLeft--;
        if (pendingEvaluation.UpdatesLeft > 0)
            return;

        if (TryGetTwoRevealedUnmatched(out var firstEntity, out var secondEntity))
        {
            var pairKeyA = World.Get<PairKey>(firstEntity);
            var pairKeyB = World.Get<PairKey>(secondEntity);

            if (pairKeyA == pairKeyB)
            {
                World.Add<Matched>(firstEntity);
                World.Add<Matched>(secondEntity);

                ref var gameState = ref World.GetSingletonRef<GameState>();
                gameState.MatchedCount += 2;
                logger.LogDebug("Match found: pair {PairKey} ({MatchedCount}/{TotalCards})", pairKeyA.Value, gameState.MatchedCount, gameState.TotalCards);
            }
            else
            {
                World.Remove<Revealed>(firstEntity);
                World.Remove<Revealed>(secondEntity);
                logger.LogDebug("No match: {PairKeyA} != {PairKeyB}", pairKeyA.Value, pairKeyB.Value);
            }
        }

        ResetTurn();
        RemovePendingEvaluation(pendingEvaluationEntity);
    }

    private bool TryGetPendingEvaluationEntity(out Entity entity)
    {
        Entity outerEntity = default;
        var found = false;

        World.Query(in PendingEvaluationQuery, innerEntity =>
        {
            if (found)
                return;

            outerEntity = innerEntity;
            found = true;
        });

        entity = outerEntity;
        return found;
    }

    private bool TryGetTwoRevealedUnmatched(out Entity entity1, out Entity entity2)
    {
        var candidates = new List<(Entity entity, int cardId)>();

        World.Query(in RevealedUnmatchedQuery, (Entity currentEntity, ref CardId cardId) => 
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

    private void ResetTurn()
    {
        ref var gameState = ref World.GetSingletonRef<GameState>();

        gameState.Moves++;
        gameState.FirstFlipped = null;
        gameState.IsLocked = false;
        gameState.StateVersion++;
    }

    private void RemovePendingEvaluation(Entity pendingEvaluationEntity) =>
        World.Destroy(pendingEvaluationEntity);
}
