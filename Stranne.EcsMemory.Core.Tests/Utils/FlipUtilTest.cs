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
internal sealed class FlipUtilTest
{
    private const int EvalDelayTicks = 10;

    private static readonly GridPosition GridPosition = new(0, 0);
    private static readonly ILogger Logger = new NullLoggerFactory().CreateLogger(nameof(FlipUtilTest));

    [Test]
    public async Task TryFlip_GameLocked_DoesNotFlipCard()
    {
        var world = TestWorldFactory.Create(isLocked: true);

        FlipUtil.TryFlip(world, GridPosition, Logger);

        using (Assert.Multiple())
        {
            var gameState = world.GetSingletonRef<GameState>();
            await Assert.That(gameState.IsLocked).IsTrue();
            await Assert.That(gameState.FirstFlipped).IsNull();
            await Assert.That(gameState.Moves).IsEqualTo(0);
            await AssertNumberOfRevealed(world, 0);
            await AssertNumberOfPendingEvaluations(world, 0);
        }
    }

    [Test]
    public async Task TryFlip_NoCardAtGridPosition_DoesNothing()
    {
        var world = TestWorldFactory.Create();

        FlipUtil.TryFlip(world, GridPosition, Logger);

        await AssertNoChange(world);
    }

    [Test]
    public async Task TryFlip_AlreadyMatched_DoesNotFlipCard()
    {
        var world = TestWorldFactory.Create();
        world.CreateCard(matched: true);

        FlipUtil.TryFlip(world, GridPosition, Logger);

        await AssertNoChange(world);
    }

    [Test]
    public async Task TryFlip_AlreadyRevealed_DoesNotFlipCard()
    {
        var world = TestWorldFactory.Create();
        world.CreateCard(revealed: true);

        FlipUtil.TryFlip(world, GridPosition, Logger);

        using (Assert.Multiple())
        {
            await Assert.That(world.GetSingletonRef<GameState>().IsLocked).IsFalse();
            await Assert.That(world.GetSingletonRef<GameState>().FirstFlipped).IsNull();
            await Assert.That(world.GetSingletonRef<GameState>().Moves).IsEqualTo(0);
            await AssertNumberOfRevealed(world, 1);
            await AssertNumberOfPendingEvaluations(world, 0);
        }
    }

    [Test]
    public async Task TryFlip_FirstCard_FlipsAndSetsGameState()
    {
        var world = TestWorldFactory.Create();
        world.CreateCard();

        FlipUtil.TryFlip(world, GridPosition, Logger);

        var gameState = world.GetSingletonRef<GameState>();
        using (Assert.Multiple())
        {
            await Assert.That(gameState.IsLocked).IsFalse();
            await Assert.That(gameState.FirstFlipped).IsNotNull();
            await Assert.That(gameState.Moves).IsEqualTo(0);
            await AssertNumberOfRevealed(world, 1);
            await AssertNumberOfPendingEvaluations(world, 0);
        }
    }

    [Test]
    public async Task TryFlip_SecondCard_SetsUpPendingEvaluationAndLocksGame()
    {
        var world = TestWorldFactory.Create();
        world.CreateCard();

        var alreadyFlippedCard = world.Create(
            new CardId(1),
            new PairKey(0),
            (0, 1),
            new Selectable(),
            new Revealed());
        world.SetOrCreateSingleton(new GameState
        {
            FirstFlipped = alreadyFlippedCard
        });

        FlipUtil.TryFlip(world, GridPosition, Logger);

        var gameState = world.GetSingletonRef<GameState>();
        using (Assert.Multiple())
        {
            await Assert.That(gameState.IsLocked).IsTrue();
            await Assert.That(gameState.FirstFlipped).IsEqualTo(alreadyFlippedCard);
            await Assert.That(gameState.Moves).IsEqualTo(0);
            await AssertNumberOfRevealed(world, 2);
            await AssertNumberOfPendingEvaluations(world, 1);
            await AssertPendingEvaluationDelayEquals(world, EvalDelayTicks);
        }
    }

    private static async Task AssertNoChange(World world)
    {
        var gameState = world.GetSingletonRef<GameState>();

        using (Assert.Multiple())
        {
            await Assert.That(gameState.IsLocked).IsFalse();
            await Assert.That(gameState.FirstFlipped).IsNull();
            await Assert.That(gameState.Moves).IsEqualTo(0);
            await AssertNumberOfRevealed(world, 0);
            await AssertNumberOfPendingEvaluations(world, 0);
        }
    }

    private static async Task AssertNumberOfRevealed(World world, int expectedRevealCount)
    {
        var query = new QueryDescription().WithAll<Revealed>();
        var revealedCount = world.CountEntities(query);
        await Assert.That(revealedCount).IsEqualTo(expectedRevealCount);
    }

    private static async Task AssertNumberOfPendingEvaluations(World world, int expectedPendingEvaluationCount)
    {
        var query = new QueryDescription().WithAll<PendingEvaluation>();
        var pendingCount = world.CountEntities(query);
        await Assert.That(pendingCount).IsEqualTo(expectedPendingEvaluationCount);
    }

    private static async Task AssertPendingEvaluationDelayEquals(World world, int expected)
    {
        var query = new QueryDescription().WithAll<PendingEvaluation>();
        var found = false;
        PendingEvaluation pendingEvaluation = default;
        world.Query(in query, (ref PendingEvaluation innerPendingEvaluation) =>
        {
            pendingEvaluation = innerPendingEvaluation;
            found = true;
        });
        await Assert.That(found).IsTrue();
        await Assert.That(pendingEvaluation.TicksLeft).IsEqualTo(expected);
    }
}
