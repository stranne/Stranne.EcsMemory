using Arch.Core;
using Arch.System;
using Microsoft.Extensions.Logging;
using Stranne.EcsMemory.Core.Components.Singleton;
using Stranne.EcsMemory.Core.Components.Tags;
using Stranne.EcsMemory.Core.Components.Value;
using Stranne.EcsMemory.Core.Extensions;
using Stranne.EcsMemory.Core.Utils;

namespace Stranne.EcsMemory.Core.Systems;
internal sealed class MatchedSystem(World world, ILogger<MatchedSystem> logger)
    : BaseSystem<World, float>(world)
{
    internal static readonly QueryDescription PendingEvaluationQuery = new QueryDescription().WithAll<PendingEvaluation>();

    public override void Update(in float _)
    {
        if (!TryGetPendingEvaluationEntity(out var pendingEvaluationEntity))
            return;

        ref var pendingEvaluation = ref World.Get<PendingEvaluation>(pendingEvaluationEntity);

        pendingEvaluation.UpdatesLeft--;
        if (pendingEvaluation.UpdatesLeft > 0)
            return;

        if (MatchEvaluationUtil.TryGetTwoRevealedUnmatched(World, out var firstEntity, out var secondEntity))
        {
            var isMatch = MatchEvaluationUtil.DoesMatch(World, firstEntity, secondEntity);
            MatchEvaluationUtil.ApplyMatchResult(World, firstEntity, secondEntity, isMatch, logger);
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

    private void ResetTurn()
    {
        ref var gameState = ref World.GetSingletonRef<GameState>();

        gameState.Moves++;
        gameState.IsLocked = false;
        World.IncrementStateVersion();
    }

    private void RemovePendingEvaluation(Entity pendingEvaluationEntity) =>
        World.Destroy(pendingEvaluationEntity);
}
