using Arch.Core;
using Arch.System;
using Stranne.EcsMemory.Core.Components.Singleton;
using Stranne.EcsMemory.Core.Components.Tags;
using Stranne.EcsMemory.Core.Components.Value;
using Stranne.EcsMemory.Core.Extensions;

namespace Stranne.EcsMemory.Core.Systems;
internal sealed class MatchedSystem(World world)
    : BaseSystem<World, float>(world)
{
    public override void Update(in float _)
    {
        if (!TryGetPendingEvaluationEntity(out var pendingEvaluationEntity))
            return;

        ref var pendingEvaluation = ref World.Get<PendingEvaluation>(pendingEvaluationEntity);

        pendingEvaluation.TicksLeft--;
        if (pendingEvaluation.TicksLeft > 0)
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
            }
            else
            {
                World.Remove<Revealed>(firstEntity);
                World.Remove<Revealed>(secondEntity);
            }
        }

        ResetTurn();
        RemovePendingEvaluation(pendingEvaluationEntity);
    }

    private bool TryGetPendingEvaluationEntity(out Entity entity)
    {
        Entity outerEntity = default;
        var found = false;

        World.Query(in Cache.PendingEvaluationQuery, innerEntity =>
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
        Entity lowestKeyIdEntity = default, secondLowestKeyIdEntity = default;
        var lowestId = int.MaxValue;
        var secondLowestId = int.MaxValue;

        World.Query(in Cache.RevealedUnmatchedQuery, (Entity currentEntity, ref CardId cardId) =>
        {
            var currentId = cardId.Value;
            if (currentId < lowestId)
            {
                if (lowestId != int.MaxValue)
                {
                    secondLowestKeyIdEntity = lowestKeyIdEntity;
                    secondLowestId = secondLowestKeyIdEntity.Id;
                }

                lowestKeyIdEntity = currentEntity;
                lowestId = currentId;
            }
            else if (currentId < secondLowestId)
            {
                secondLowestKeyIdEntity = currentEntity;
                secondLowestId = currentId;
            }
        });

        if (secondLowestId == int.MaxValue)
        {
            entity1 = default;
            entity2 = default;
            return false;
        }

        entity1 = lowestKeyIdEntity;
        entity2 = secondLowestKeyIdEntity;
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

    private static class Cache
    {
        internal static readonly QueryDescription PendingEvaluationQuery = new QueryDescription().WithAll<PendingEvaluation>();
        internal static readonly QueryDescription RevealedUnmatchedQuery = new QueryDescription().WithAll<CardId, Revealed>().WithNone<Matched>();
    }
}
