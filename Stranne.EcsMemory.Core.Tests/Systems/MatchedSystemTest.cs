using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stranne.EcsMemory.Core.Components.Singleton;
using Stranne.EcsMemory.Core.Components.Tags;
using Stranne.EcsMemory.Core.Extensions;
using Stranne.EcsMemory.Core.Systems;
using Stranne.EcsMemory.Core.Tests.Common;

namespace Stranne.EcsMemory.Core.Tests.Systems;
internal sealed class MatchedSystemTest
{
    private static readonly ILogger<MatchedSystem> Logger = new NullLogger<MatchedSystem>();

    [Test]
    public async Task MatchSystem_NoPendingEvaluation_DoesNothing()
    {
        using var world = TestWorldFactory.Create();
        using var sut = new MatchedSystem(world, Logger);

        var entityA = world.CreateCard(1, revealed: true);
        var entityB = world.CreateCard(2, revealed: true);

        sut.Update(0);

        using (Assert.Multiple())
        {
            await Assert.That(world.Has<Matched>(entityA)).IsFalse();
            await Assert.That(world.Has<Matched>(entityB)).IsFalse();
            await Assert.That(world.HasAny<PendingEvaluation>()).IsFalse();
        }
    }

    [Test]
    public async Task MatchSystem_UpdatesDown_UntilZero_NoResolveBeforeZero()
    {
        using var world = TestWorldFactory.Create();
        using var sut = new MatchedSystem(world, Logger);

        var card1 = world.CreateCard(0, 0, true);
        var card2 = world.CreateCard(1, 1, true);
        world.CreatePending(3);

        sut.Update(0);
        sut.Update(0);

        using (Assert.Multiple())
        {
            await Assert.That(world.HasAny<PendingEvaluation>()).IsTrue();
            await Assert.That(world.Has<Revealed>(card1)).IsTrue();
            await Assert.That(world.Has<Revealed>(card2)).IsTrue();
        }
    }

    [Test]
    public async Task MatchSystem_OnZeroTwoRevealedSamePair_SetsMatched_UnlocksAndIncrementsMoves()
    {
        using var world = TestWorldFactory.Create();
        using var sut = new MatchedSystem(world, Logger);

        var card1 = world.CreateCard(0, 0, true);
        var card2 = world.CreateCard(1, 0, true);
        world.CreatePending();

        sut.Update(0);

        using (Assert.Multiple())
        {
            await Assert.That(world.Has<Matched>(card1)).IsTrue();
            await Assert.That(world.Has<Matched>(card2)).IsTrue();
            await Assert.That(world.Has<PendingEvaluation>(card1)).IsFalse();
            await Assert.That(world.Has<PendingEvaluation>(card2)).IsFalse();

            var gameState = world.GetSingletonRef<GameState>();
            await Assert.That(gameState.IsLocked).IsFalse();
            await Assert.That(gameState.FirstFlipped).IsNull();
            await Assert.That(gameState.Moves).IsEqualTo(1);
            await Assert.That(gameState.MatchedCount).IsEqualTo(2);
        }
    }

    [Test]
    public async Task MatchSystem_OnZeroTwoRevealedDifferentPair_FlipsBack_UnlocksAndIncrementsMoves()
    {
        using var world = TestWorldFactory.Create();
        using var sut = new MatchedSystem(world, Logger);

        var card1 = world.CreateCard(0, 0, true);
        var card2 = world.CreateCard(1, 1, true);
        world.CreatePending();

        sut.Update(0);

        using (Assert.Multiple())
        {
            await Assert.That(world.Has<Matched>(card1)).IsFalse();
            await Assert.That(world.Has<Matched>(card2)).IsFalse();
            await Assert.That(world.Has<Revealed>(card1)).IsFalse();
            await Assert.That(world.Has<Revealed>(card2)).IsFalse();
            await Assert.That(world.HasAny<PendingEvaluation>()).IsFalse();

            var gameState = world.GetSingletonRef<GameState>();
            await Assert.That(gameState.IsLocked).IsFalse();
            await Assert.That(gameState.FirstFlipped).IsNull();
            await Assert.That(gameState.Moves).IsEqualTo(1);
            await Assert.That(gameState.MatchedCount).IsEqualTo(0);
        }
    }

    [Test]
    public async Task MatchSystem_OnZeroLessThanTwoRevealed_UnlocksAndClearsPending()
    {
        using var world = TestWorldFactory.Create();
        using var sut = new MatchedSystem(world, Logger);

        var card = world.CreateCard(revealed: true);
        world.CreatePending();

        sut.Update(0);

        using (Assert.Multiple())
        {
            await Assert.That(world.HasAny<PendingEvaluation>()).IsFalse();
            await Assert.That(world.Has<Revealed>(card)).IsTrue();

            var gameState = world.GetSingletonRef<GameState>();
            await Assert.That(gameState.IsLocked).IsFalse();
            await Assert.That(gameState.FirstFlipped).IsNull();
            await Assert.That(gameState.Moves).IsEqualTo(1);
            await Assert.That(gameState.MatchedCount).IsEqualTo(0);
        }
    }

    [Test]
    public async Task MatchSystem_PicksDeterministicPair_ByLowestCardId_WhenMoreThanTwoRevealed()
    {
        using var world = TestWorldFactory.Create();
        using var sut = new MatchedSystem(world, Logger);

        var card1 = world.CreateCard(2, 1, true);
        var card2 = world.CreateCard(1, 0, true);
        var card3 = world.CreateCard(0, 0, true);
        world.CreatePending();

        sut.Update(0);

        using (Assert.Multiple())
        {
            await Assert.That(world.Has<Matched>(card1)).IsFalse();
            await Assert.That(world.Has<Matched>(card2)).IsTrue();
            await Assert.That(world.Has<Matched>(card3)).IsTrue();

            await Assert.That(world.Has<Revealed>(card1)).IsTrue();
            await Assert.That(world.Has<Revealed>(card2)).IsTrue();
            await Assert.That(world.Has<Revealed>(card3)).IsTrue();
        }
    }
}
