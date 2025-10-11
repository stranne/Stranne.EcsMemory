using Arch.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Stranne.EcsMemory.Core.Components.Singleton;
using Stranne.EcsMemory.Core.Components.Tags;
using Stranne.EcsMemory.Core.Components.Value;
using Stranne.EcsMemory.Core.Extensions;
using Stranne.EcsMemory.Core.Tests.Common;
using Stranne.EcsMemory.Core.Utils;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Stranne.EcsMemory.Core.Tests.Utils;
[NotInParallel]
internal sealed class FlipUtilTests
{
    private const int EvalDelayUpdates = 10;

    private static readonly GridPosition GridPosition = new(0, 0);
    private static readonly ILogger Logger = new NullLoggerFactory().CreateLogger(nameof(FlipUtilTests));

    private static readonly QueryDescription RevealedQuery = new QueryDescription().WithAll<Revealed>();
    private static readonly QueryDescription PendingEvaluationQuery = new QueryDescription().WithAll<PendingEvaluation>();
    private static readonly QueryDescription MatchedQuery = new QueryDescription().WithAll<Matched>();

    [Test]
    public async Task TryFlip_GameLocked_DoesNotFlipCard()
    {
        using var world = TestWorldFactory.Create(isLocked: true);

        FlipUtil.TryFlip(world, GridPosition, Logger);

        using (Assert.Multiple())
        {
            var gameState = world.GetSingletonRef<GameState>();
            await Assert.That(gameState.IsLocked).IsTrue();
            await Assert.That(gameState.Moves).IsEqualTo(0);
            await AssertNumberOfRevealed(world, 0);
            await AssertNumberOfPendingEvaluations(world, 0);
        }
    }

    [Test]
    public async Task TryFlip_NoCardAtGridPosition_DoesNothing()
    {
        using var world = TestWorldFactory.Create();

        FlipUtil.TryFlip(world, GridPosition, Logger);

        await AssertNoChange(world);
    }

    [Test]
    public async Task TryFlip_AlreadyMatched_DoesNotFlipCard()
    {
        using var world = TestWorldFactory.Create();
        world.CreateCard(matched: true);

        FlipUtil.TryFlip(world, GridPosition, Logger);

        await AssertNoChange(world);
    }

    [Test]
    public async Task TryFlip_AlreadyRevealed_DoesNotFlipCard()
    {
        using var world = TestWorldFactory.Create();
        world.CreateCard(revealed: true);

        FlipUtil.TryFlip(world, GridPosition, Logger);

        using (Assert.Multiple())
        {
            await Assert.That(world.GetSingletonRef<GameState>().IsLocked).IsFalse();
            await Assert.That(world.GetSingletonRef<GameState>().Moves).IsEqualTo(0);
            await AssertNumberOfRevealed(world, 1);
            await AssertNumberOfPendingEvaluations(world, 0);
        }
    }

    [Test]
    public async Task TryFlip_FirstCard_FlipsAndSetsGameState()
    {
        using var world = TestWorldFactory.Create();
        world.CreateCard();

        FlipUtil.TryFlip(world, GridPosition, Logger);

        var gameState = world.GetSingletonRef<GameState>();
        using (Assert.Multiple())
        {
            await Assert.That(gameState.IsLocked).IsFalse();
            await Assert.That(gameState.Moves).IsEqualTo(0);
            await AssertNumberOfRevealed(world, 1);
            await AssertNumberOfPendingEvaluations(world, 0);
        }
    }

    [Test]
    public async Task TryFlip_SecondCard_SetsUpPendingEvaluationAndLocksGame()
    {
        using var world = TestWorldFactory.Create();
        world.CreateCard(pairKey: 0);
        world.CreateCard(cardId: 1, pairKey: 1, x: 0, y: 1, revealed: true);

        FlipUtil.TryFlip(world, GridPosition, Logger);

        var gameState = world.GetSingletonRef<GameState>();
        using (Assert.Multiple())
        {
            await Assert.That(gameState.IsLocked).IsTrue();
            await Assert.That(gameState.Moves).IsEqualTo(0);
            await AssertNumberOfRevealed(world, 2);
            await AssertNumberOfPendingEvaluations(world, 1);
            await AssertPendingEvaluationDelayEquals(world, EvalDelayUpdates);
        }
    }

    [Test]
    public async Task TryFlip_SecondCardMatching_ImmediatelyMatchesAndUnlocksGame()
    {
        const int pairKey = 5;
        using var world = TestWorldFactory.Create();
        world.CreateCard(pairKey: pairKey);
        world.CreateCard(cardId: 1, pairKey: pairKey, x: 0, y: 1, revealed: true);

        FlipUtil.TryFlip(world, GridPosition, Logger);

        var gameState = world.GetSingletonRef<GameState>();
        using (Assert.Multiple())
        {
            await Assert.That(gameState.IsLocked).IsFalse();
            await Assert.That(gameState.Moves).IsEqualTo(1);
            await Assert.That(gameState.MatchedCount).IsEqualTo(2);
            await AssertNumberOfRevealed(world, 2);
            await AssertNumberOfMatched(world, 2);
            await AssertNumberOfPendingEvaluations(world, 0);
        }
    }

    private static async Task AssertNoChange(World world)
    {
        var gameState = world.GetSingletonRef<GameState>();

        using (Assert.Multiple())
        {
            await Assert.That(gameState.IsLocked).IsFalse();
            await Assert.That(gameState.Moves).IsEqualTo(0);
            await AssertNumberOfRevealed(world, 0);
            await AssertNumberOfPendingEvaluations(world, 0);
        }
    }

    private static async Task AssertNumberOfRevealed(World world, int expectedRevealCount)
    {
        var revealedCount = world.CountEntities(in RevealedQuery);
        await Assert.That(revealedCount).IsEqualTo(expectedRevealCount);
    }

    private static async Task AssertNumberOfPendingEvaluations(World world, int expectedPendingEvaluationCount)
    {
        var pendingCount = world.CountEntities(in PendingEvaluationQuery);
        await Assert.That(pendingCount).IsEqualTo(expectedPendingEvaluationCount);
    }

    private static async Task AssertNumberOfMatched(World world, int expectedMatchedCount)
    {
        var matchedCount = world.CountEntities(in MatchedQuery);
        await Assert.That(matchedCount).IsEqualTo(expectedMatchedCount);
    }

    private static async Task AssertPendingEvaluationDelayEquals(World world, int expected)
    {
        var found = false;
        PendingEvaluation pendingEvaluation = default;
        world.Query(in PendingEvaluationQuery, (ref PendingEvaluation innerPendingEvaluation) =>
        {
            pendingEvaluation = innerPendingEvaluation;
            found = true;
        });
        await Assert.That(found).IsTrue();
        await Assert.That(pendingEvaluation.UpdatesLeft).IsEqualTo(expected);
    }
}
