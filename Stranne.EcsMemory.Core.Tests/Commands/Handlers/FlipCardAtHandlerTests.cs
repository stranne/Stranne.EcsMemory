using Arch.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stranne.EcsMemory.Core.Commands;
using Stranne.EcsMemory.Core.Commands.Base;
using Stranne.EcsMemory.Core.Commands.Handlers;
using Stranne.EcsMemory.Core.Components.Singleton;
using Stranne.EcsMemory.Core.Components.Tags;
using Stranne.EcsMemory.Core.Extensions;
using Stranne.EcsMemory.Core.Tests.Common;

namespace Stranne.EcsMemory.Core.Tests.Commands.Handlers;
[NotInParallel]
internal sealed class FlipCardAtHandlerTests
{
    private static readonly ILogger Logger = new NullLogger<FlipCardAtHandlerTests>();

    [Test]
    public async Task Execute_ValidCardPosition_ReturnsSuccess()
    {
        using var world = TestWorldFactory.Create();
        world.CreateCard(cardId: 1, x: 2, y: 1);
        var command = new FlipCardAt(2, 1);

        var actual = FlipCardAtHandler.Execute(command, world, Logger);

        await Assert.That(actual).IsEqualTo(CommandResult.Success);
    }

    [Test]
    public async Task Execute_ValidCardPosition_AddsRevealedTag()
    {
        using var world = TestWorldFactory.Create();
        var card = world.CreateCard(cardId: 1, x: 2, y: 1);
        var command = new FlipCardAt(2, 1);

        FlipCardAtHandler.Execute(command, world, Logger);

        await Assert.That(world.Has<Revealed>(card)).IsTrue();
    }

    [Test]
    public async Task Execute_GameLocked_ReturnsSkippedAndDoesNotFlipCard()
    {
        using var world = TestWorldFactory.Create(isLocked: true);
        var card = world.CreateCard(cardId: 1, x: 2, y: 1);
        var command = new FlipCardAt(2, 1);

        var actual = FlipCardAtHandler.Execute(command, world, Logger);

        using (Assert.Multiple())
        {
            await Assert.That(actual).IsEqualTo(CommandResult.Skipped);
            await Assert.That(world.Has<Revealed>(card)).IsFalse();
        }
    }

    [Test]
    public async Task Execute_CardAlreadyRevealed_ReturnsSkipped()
    {
        using var world = TestWorldFactory.Create();
        var card = world.CreateCard(cardId: 1, x: 2, y: 1, revealed: true);
        var command = new FlipCardAt(2, 1);

        var actual = FlipCardAtHandler.Execute(command, world, Logger);

        using (Assert.Multiple())
        {
            await Assert.That(actual).IsEqualTo(CommandResult.Skipped);
            await Assert.That(world.Has<Revealed>(card)).IsTrue();
        }
    }

    [Test]
    public async Task Execute_CardAlreadyMatched_ReturnsSkippedAndDoesNotFlipCard()
    {
        using var world = TestWorldFactory.Create();
        var card = world.CreateCard(cardId: 1, x: 2, y: 1, matched: true);
        var command = new FlipCardAt(2, 1);

        var actual = FlipCardAtHandler.Execute(command, world, Logger);

        using (Assert.Multiple())
        {
            await Assert.That(actual).IsEqualTo(CommandResult.Skipped);
            await Assert.That(world.Has<Revealed>(card)).IsFalse();
            await Assert.That(world.Has<Matched>(card)).IsTrue();
        }
    }

    [Test]
    public async Task Execute_InvalidPosition_ReturnsSkipped()
    {
        using var world = TestWorldFactory.Create();
        world.CreateCard(cardId: 1, x: 0, y: 0);
        var command = new FlipCardAt(5, 5);

        var actual = FlipCardAtHandler.Execute(command, world, Logger);

        await Assert.That(actual).IsEqualTo(CommandResult.Skipped);
    }

    [Test]
    public async Task Execute_FirstCardFlip_DoesNotLockGame()
    {
        using var world = TestWorldFactory.Create();
        world.CreateCard(cardId: 1, x: 0, y: 0);
        var command = new FlipCardAt(0, 0);

        FlipCardAtHandler.Execute(command, world, Logger);

        var gameState = world.GetSingletonRef<GameState>();
        await Assert.That(gameState.IsLocked).IsFalse();
    }

    [Test]
    public async Task Execute_SecondCardFlip_LocksGame()
    {
        using var world = TestWorldFactory.Create();
        world.CreateCard(cardId: 1, pairKey: 0, x: 0, y: 0, revealed: true);
        world.CreateCard(cardId: 2, pairKey: 1, x: 1, y: 0);
        var command = new FlipCardAt(1, 0);

        FlipCardAtHandler.Execute(command, world, Logger);

        var gameState = world.GetSingletonRef<GameState>();
        await Assert.That(gameState.IsLocked).IsTrue();
    }

    [Test]
    public async Task Execute_SecondCardFlipWithMatch_UnlocksGameImmediately()
    {
        using var world = TestWorldFactory.Create();
        world.CreateCard(cardId: 1, pairKey: 0, x: 0, y: 0, revealed: true);
        world.CreateCard(cardId: 2, pairKey: 0, x: 1, y: 0);
        var command = new FlipCardAt(1, 0);

        FlipCardAtHandler.Execute(command, world, Logger);

        using (Assert.Multiple())
        {
            var gameState = world.GetSingletonRef<GameState>();
            await Assert.That(gameState.IsLocked).IsFalse();
            await Assert.That(gameState.Moves).IsEqualTo(1);
            await Assert.That(gameState.MatchedCount).IsEqualTo(2);
        }
    }
}