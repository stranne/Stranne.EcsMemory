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
internal sealed class MatchEvaluationUtilTests
{
    private static readonly ILogger Logger = new NullLoggerFactory().CreateLogger(nameof(MatchEvaluationUtilTests));

    [Test]
    public async Task TryGetTwoRevealedUnmatched_NoRevealedCards_ReturnsFalse()
    {
        using var world = TestWorldFactory.Create();
        world.CreateCard(0, 0, revealed: false);
        world.CreateCard(1, 1, revealed: false);

        var result = MatchEvaluationUtil.TryGetTwoRevealedUnmatched(world, out var entity1, out var entity2);

        using (Assert.Multiple())
        {
            await Assert.That(result).IsFalse();
            await Assert.That(entity1).IsEqualTo(default);
            await Assert.That(entity2).IsEqualTo(default);
        }
    }

    [Test]
    public async Task TryGetTwoRevealedUnmatched_OneRevealedCard_ReturnsFalse()
    {
        using var world = TestWorldFactory.Create();
        world.CreateCard(0, 0, revealed: true);
        world.CreateCard(1, 1, revealed: false);

        var result = MatchEvaluationUtil.TryGetTwoRevealedUnmatched(world, out var entity1, out var entity2);

        using (Assert.Multiple())
        {
            await Assert.That(result).IsFalse();
            await Assert.That(entity1).IsEqualTo(default);
            await Assert.That(entity2).IsEqualTo(default);
        }
    }

    [Test]
    public async Task TryGetTwoRevealedUnmatched_TwoRevealedCards_ReturnsTrue()
    {
        using var world = TestWorldFactory.Create();
        var cardA = world.CreateCard(0, 0, revealed: true);
        var cardB = world.CreateCard(1, 1, revealed: true);

        var result = MatchEvaluationUtil.TryGetTwoRevealedUnmatched(world, out var entity1, out var entity2);

        using (Assert.Multiple())
        {
            await Assert.That(result).IsTrue();
            await Assert.That(entity1).IsNotEqualTo(default);
            await Assert.That(entity2).IsNotEqualTo(default);
            await Assert.That(entity1).IsNotEqualTo(entity2);
        }
    }

    [Test]
    public async Task TryGetTwoRevealedUnmatched_ThreeRevealedCards_ReturnsTwoInCardIdOrder()
    {
        using var world = TestWorldFactory.Create();
        var cardC = world.CreateCard(2, 2, revealed: true);
        var cardA = world.CreateCard(0, 0, revealed: true);
        var cardB = world.CreateCard(1, 1, revealed: true);

        var result = MatchEvaluationUtil.TryGetTwoRevealedUnmatched(world, out var entity1, out var entity2);

        using (Assert.Multiple())
        {
            await Assert.That(result).IsTrue();
            var cardId1 = world.Get<CardId>(entity1);
            var cardId2 = world.Get<CardId>(entity2);
            await Assert.That(cardId1.Value).IsEqualTo(0);
            await Assert.That(cardId2.Value).IsEqualTo(1);
        }
    }

    [Test]
    public async Task TryGetTwoRevealedUnmatched_IgnoresMatchedCards()
    {
        using var world = TestWorldFactory.Create();
        var matchedCard = world.CreateCard(0, 0, revealed: true, matched: true);
        _ = world.CreateCard(1, 1, revealed: true);
        _ = world.CreateCard(2, 2, revealed: true);

        var result = MatchEvaluationUtil.TryGetTwoRevealedUnmatched(world, out var entity1, out var entity2);

        using (Assert.Multiple())
        {
            await Assert.That(result).IsTrue();
            await Assert.That(entity1).IsNotEqualTo(matchedCard);
            await Assert.That(entity2).IsNotEqualTo(matchedCard);

            var cardId1 = world.Get<CardId>(entity1);
            var cardId2 = world.Get<CardId>(entity2);
            await Assert.That(cardId1.Value).IsEqualTo(1);
            await Assert.That(cardId2.Value).IsEqualTo(2);
        }
    }

    [Test]
    public async Task DoesMatch_SamePairKey_ReturnsTrue()
    {
        using var world = TestWorldFactory.Create();
        var entity1 = world.Create(new PairKey(42));
        var entity2 = world.Create(new PairKey(42));

        var result = MatchEvaluationUtil.DoesMatch(world, entity1, entity2);

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task DoesMatch_DifferentPairKeys_ReturnsFalse()
    {
        using var world = TestWorldFactory.Create();
        var entity1 = world.Create(new PairKey(42));
        var entity2 = world.Create(new PairKey(99));

        var result = MatchEvaluationUtil.DoesMatch(world, entity1, entity2);

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task ApplyMatchResult_Match_AddsMatchedTagsAndUpdatesGameState()
    {
        using var world = TestWorldFactory.Create();
        world.SetOrCreateSingleton(new GameState { MatchedCount = 4, TotalCards = 20 });

        var entity1 = world.Create(new PairKey(42));
        var entity2 = world.Create(new PairKey(42));

        MatchEvaluationUtil.ApplyMatchResult(world, entity1, entity2, true, Logger);

        using (Assert.Multiple())
        {
            await Assert.That(world.Has<Matched>(entity1)).IsTrue();
            await Assert.That(world.Has<Matched>(entity2)).IsTrue();

            var gameState = world.GetSingletonRef<GameState>();
            await Assert.That(gameState.MatchedCount).IsEqualTo(6);
        }
    }

    [Test]
    public async Task ApplyMatchResult_NoMatch_RemovesRevealedTags()
    {
        using var world = TestWorldFactory.Create();
        world.SetOrCreateSingleton(new GameState { MatchedCount = 4, TotalCards = 20 });

        var entity1 = world.Create(new PairKey(42), new Revealed());
        var entity2 = world.Create(new PairKey(99), new Revealed());

        MatchEvaluationUtil.ApplyMatchResult(world, entity1, entity2, false, Logger);

        using (Assert.Multiple())
        {
            await Assert.That(world.Has<Matched>(entity1)).IsFalse();
            await Assert.That(world.Has<Matched>(entity2)).IsFalse();
            await Assert.That(world.Has<Revealed>(entity1)).IsFalse();
            await Assert.That(world.Has<Revealed>(entity2)).IsFalse();

            var gameState = world.GetSingletonRef<GameState>();
            await Assert.That(gameState.MatchedCount).IsEqualTo(4);
        }
    }

    [Test]
    public async Task TryHandleImmediateMatch_NoRevealedCards_ReturnsFalse()
    {
        using var world = TestWorldFactory.Create();
        world.CreateCard(0, 0, revealed: false);

        var result = MatchEvaluationUtil.TryHandleImmediateMatch(world, Logger);

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task TryHandleImmediateMatch_OneRevealedCard_ReturnsFalse()
    {
        using var world = TestWorldFactory.Create();
        world.CreateCard(0, 0, revealed: true);

        var result = MatchEvaluationUtil.TryHandleImmediateMatch(world, Logger);

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task TryHandleImmediateMatch_TwoNonMatchingCards_ReturnsFalse()
    {
        using var world = TestWorldFactory.Create();
        world.CreateCard(0, 0, revealed: true);
        world.CreateCard(1, 1, revealed: true);

        var result = MatchEvaluationUtil.TryHandleImmediateMatch(world, Logger);

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task TryHandleImmediateMatch_TwoMatchingCards_ReturnsTrueAndUpdatesGameState()
    {
        using var world = TestWorldFactory.Create();
        world.SetOrCreateSingleton(new GameState { IsLocked = true, Moves = 5, MatchedCount = 2, TotalCards = 20 });

        const int pairKey = 0;
        var card1 = world.CreateCard(0, pairKey, revealed: true);
        var card2 = world.CreateCard(1, pairKey, revealed: true);

        var result = MatchEvaluationUtil.TryHandleImmediateMatch(world, Logger);

        using (Assert.Multiple())
        {
            await Assert.That(result).IsTrue();

            await Assert.That(world.Has<Matched>(card1)).IsTrue();
            await Assert.That(world.Has<Matched>(card2)).IsTrue();

            var gameState = world.GetSingletonRef<GameState>();
            await Assert.That(gameState.IsLocked).IsFalse();
            await Assert.That(gameState.Moves).IsEqualTo(6);
            await Assert.That(gameState.MatchedCount).IsEqualTo(4);
        }
    }

    [Test]
    public async Task TryHandleImmediateMatch_NoLogger_DoesNotThrow()
    {
        using var world = TestWorldFactory.Create();
        const int pairKey = 0;
        world.CreateCard(0, pairKey, revealed: true);
        world.CreateCard(1, pairKey, revealed: true);

        var result = MatchEvaluationUtil.TryHandleImmediateMatch(world, null);

        await Assert.That(result).IsTrue();
    }
}