using Arch.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Stranne.EcsMemory.Core.Components.Singleton;
using Stranne.EcsMemory.Core.Components.Tags;
using Stranne.EcsMemory.Core.Components.Value;
using Stranne.EcsMemory.Core.Extensions;
using Stranne.EcsMemory.Core.Utils;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Stranne.EcsMemory.Core.Tests.Utils;
internal sealed class FlipUtilTest
{
    private const int EvalDelayTicks = 10;

    private static readonly GridPosition GridPosition = new(0, 0);
    private static readonly ILogger Logger = new NullLoggerFactory().CreateLogger(nameof(FlipUtilTest));

    private World _world = null!;

    [Before(Test)]
    public void BeforeTest()
    {
        _world = World.Create();
        _world.SetOrCreateSingleton(new GameState
        {
            IsLocked = false,
            FirstFlipped = null,
            Moves = 0
        });
        _world.SetOrCreateSingleton(new Config(4, 3, EvalDelayTicks, 123));
    }

    [Test]
    public async Task TryFlip_GameLocked_DoesNotFlipCard()
    {
        _world.SetOrCreateSingleton(new GameState
        {
            IsLocked = true
        });

        FlipUtil.TryFlip(_world, GridPosition, Logger);

        using (Assert.Multiple())
        {
            await Assert.That(_world.GetSingletonRef<GameState>().IsLocked).IsTrue();
            await Assert.That(_world.GetSingletonRef<GameState>().FirstFlipped).IsNull();
            await Assert.That(_world.GetSingletonRef<GameState>().Moves).IsEqualTo(0);
            await AssertNumberOfRevealed(0);
            await AssertNumberOfPendingEvaluations(0);
        }
    }

    [Test]
    public async Task TryFlip_NoCardAtGridPosition_DoesNothing()
    {
        FlipUtil.TryFlip(_world, GridPosition, Logger);

        await AssertNoChange();
    }

    [Test]
    public async Task TryFlip_AlreadyMatched_DoesNotFlipCard()
    {
        AddCard(new Matched());

        FlipUtil.TryFlip(_world, GridPosition, Logger);

        await AssertNoChange();
    }

    [Test]
    public async Task TryFlip_AlreadyRevealed_DoesNotFlipCard()
    {
        AddCard(new Revealed());

        FlipUtil.TryFlip(_world, GridPosition, Logger);

        using (Assert.Multiple())
        {
            await Assert.That(_world.GetSingletonRef<GameState>().IsLocked).IsFalse();
            await Assert.That(_world.GetSingletonRef<GameState>().FirstFlipped).IsNull();
            await Assert.That(_world.GetSingletonRef<GameState>().Moves).IsEqualTo(0);
            await AssertNumberOfRevealed(1);
            await AssertNumberOfPendingEvaluations(0);
        }
    }

    [Test]
    public async Task TryFlip_FirstCard_FlipsAndSetsGameState()
    {
        AddCard();

        FlipUtil.TryFlip(_world, GridPosition, Logger);

        var gameState = _world.GetSingletonRef<GameState>();
        using (Assert.Multiple())
        {
            await Assert.That(gameState.IsLocked).IsFalse();
            await Assert.That(gameState.FirstFlipped).IsNotNull();
            await Assert.That(gameState.Moves).IsEqualTo(0);
            await AssertNumberOfRevealed(1);
            await AssertNumberOfPendingEvaluations(0);
        }
    }

    [Test]
    public async Task TryFlip_SecondCard_SetsUpPendingEvaluationAndLocksGame()
    {
        AddCard();
        var alreadyFlippedCard = _world.Create(
            new CardId(1),
            new PairKey(0),
            (0, 1),
            new Selectable(),
            new Revealed());
        _world.SetOrCreateSingleton(new GameState
        {
            FirstFlipped = alreadyFlippedCard
        });

        FlipUtil.TryFlip(_world, GridPosition, Logger);

        var gameState = _world.GetSingletonRef<GameState>();
        using (Assert.Multiple())
        {
            await Assert.That(gameState.IsLocked).IsTrue();
            await Assert.That(gameState.FirstFlipped).IsEqualTo(alreadyFlippedCard);
            await Assert.That(gameState.Moves).IsEqualTo(0);
            await AssertNumberOfRevealed(2);
            await AssertNumberOfPendingEvaluations(1);
            await AssertPendingEvaluationDelayEquals(EvalDelayTicks);
        }
    }

    private void AddCard() =>
        _world.Create(
            new CardId(0),
            new PairKey(0),
            GridPosition,
            new Selectable());

    private void AddCard<T>(T attachedComponent) where T : struct =>
        _world.Create(
            new CardId(0),
            new PairKey(0),
            GridPosition,
            new Selectable(),
            attachedComponent);

    private async Task AssertNoChange()
    {
        var gameState = _world.GetSingletonRef<GameState>();

        using (Assert.Multiple())
        {
            await Assert.That(gameState.IsLocked).IsFalse();
            await Assert.That(gameState.FirstFlipped).IsNull();
            await Assert.That(gameState.Moves).IsEqualTo(0);
            await AssertNumberOfRevealed(0);
            await AssertNumberOfPendingEvaluations(0);
        }
    }

    private async Task AssertNumberOfRevealed(int expectedRevealCount)
    {
        var query = new QueryDescription().WithAll<Revealed>();
        var revealedCount = _world.CountEntities(query);
        await Assert.That(revealedCount).IsEqualTo(expectedRevealCount);
    }

    private async Task AssertNumberOfPendingEvaluations(int expectedPendingEvaluationCount)
    {
        var query = new QueryDescription().WithAll<PendingEvaluation>();
        var pendingCount = _world.CountEntities(query);
        await Assert.That(pendingCount).IsEqualTo(expectedPendingEvaluationCount);
    }

    private async Task AssertPendingEvaluationDelayEquals(int expected)
    {
        var query = new QueryDescription().WithAll<PendingEvaluation>();
        var found = false;
        PendingEvaluation pendingEvaluation = default;
        _world.Query(in query, (ref PendingEvaluation innerPendingEvaluation) =>
        {
            pendingEvaluation = innerPendingEvaluation;
            found = true;
        });
        await Assert.That(found).IsTrue();
        await Assert.That(pendingEvaluation.TicksLeft).IsEqualTo(expected);
    }
}
