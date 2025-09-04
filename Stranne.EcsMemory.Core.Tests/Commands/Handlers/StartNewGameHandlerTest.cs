using Arch.Core;
using Stranne.EcsMemory.Contracts;
using Stranne.EcsMemory.Core.Commands;
using Stranne.EcsMemory.Core.Commands.Base;
using Stranne.EcsMemory.Core.Commands.Handlers;
using Stranne.EcsMemory.Core.Components.Singleton;
using Stranne.EcsMemory.Core.Components.Value;
using Stranne.EcsMemory.Core.Extensions;
using Stranne.EcsMemory.Core.Tests.Common;

namespace Stranne.EcsMemory.Core.Tests.Commands.Handlers;
[NotInParallel]
internal sealed class StartNewGameHandlerTest
{
    private static readonly GameConfiguration DefaultConfig = new() { EvaluationDelayUpdates = 30 };

    [Test]
    public async Task Execute_ValidParameters_ReturnsSuccess()
    {
        using var world = TestWorldFactory.Create();
        var command = new StartNewGame(4, 3, 123);

        var result = StartNewGameHandler.Execute(command, world, DefaultConfig);

        await Assert.That(result).IsEqualTo(CommandResult.Success);
    }

    [Test]
    public async Task Execute_ValidParameters_CreatesConfig()
    {
        using var world = TestWorldFactory.Create();
        var command = new StartNewGame(5, 4, 456);

        StartNewGameHandler.Execute(command, world, DefaultConfig);

        using (Assert.Multiple())
        {
            var config = world.GetSingletonRef<Config>();
            await Assert.That(config.Columns).IsEqualTo(5);
            await Assert.That(config.Rows).IsEqualTo(4);
            await Assert.That(config.Seed).IsEqualTo(456);
            await Assert.That(config.EvalDelayUpdates).IsEqualTo(30);
        }
    }

    [Test]
    public async Task Execute_ValidParameters_OverwritesExistingConfig()
    {
        using var world = TestWorldFactory.Create(columns: 2, rows: 2, seed: 111);
        var command = new StartNewGame(6, 5, 789);

        StartNewGameHandler.Execute(command, world, DefaultConfig);

        using (Assert.Multiple())
        {
            var config = world.GetSingletonRef<Config>();
            await Assert.That(config.Columns).IsEqualTo(6);
            await Assert.That(config.Rows).IsEqualTo(5);
            await Assert.That(config.Seed).IsEqualTo(789);
        }
    }

    [Test]
    public async Task Execute_ValidParameters_CreatesBoard()
    {
        using var world = TestWorldFactory.Create();
        var command = new StartNewGame(2, 2, 123);

        StartNewGameHandler.Execute(command, world, DefaultConfig);

        using (Assert.Multiple())
        {
            var cardCount = 0;
            world.Query(new QueryDescription().WithAll<CardId, GridPosition, PairKey>(), 
                (ref CardId _, ref GridPosition _, ref PairKey _) => cardCount++);
            
            await Assert.That(cardCount).IsEqualTo(4);
        }
    }

    [Test]
    public async Task Execute_ValidParameters_UpdatesGameState()
    {
        using var world = TestWorldFactory.Create();
        var command = new StartNewGame(3, 2, 123);

        StartNewGameHandler.Execute(command, world, DefaultConfig);

        using (Assert.Multiple())
        {
            var gameState = world.GetSingletonRef<GameState>();
            await Assert.That(gameState.TotalCards).IsEqualTo(6);
            await Assert.That(gameState.IsLocked).IsFalse();
            await Assert.That(gameState.Moves).IsEqualTo(0);
            await Assert.That(gameState.MatchedCount).IsEqualTo(0);
            await Assert.That(gameState.IsWon).IsFalse();
        }
    }

    [Test]
    public async Task Execute_CustomEvaluationDelay_UsesConfigurationValue()
    {
        using var world = TestWorldFactory.Create();
        var customConfig = new GameConfiguration { EvaluationDelayUpdates = 50 };
        var command = new StartNewGame(2, 2, 123);

        StartNewGameHandler.Execute(command, world, customConfig);

        using (Assert.Multiple())
        {
            var config = world.GetSingletonRef<Config>();
            await Assert.That(config.EvalDelayUpdates).IsEqualTo(50);
        }
    }

    [Test]
    public async Task Execute_OddTotalCards_ThrowsArgumentException()
    {
        using var world = TestWorldFactory.Create();
        var command = new StartNewGame(3, 1, 123);

        await Assert.That(() => StartNewGameHandler.Execute(command, world, DefaultConfig))
            .Throws<ArgumentException>()
            .WithMessage("Board size must be even (Columns * Rows). (Parameter 'config')");
    }

    [Test]
    public async Task Execute_LargeBoard_CreatesAllCards()
    {
        using var world = TestWorldFactory.Create();
        var command = new StartNewGame(8, 6, 123);

        StartNewGameHandler.Execute(command, world, DefaultConfig);

        using (Assert.Multiple())
        {
            var cardCount = 0;
            world.Query(new QueryDescription().WithAll<CardId>(), 
                (ref CardId _) => cardCount++);
            
            await Assert.That(cardCount).IsEqualTo(48);
            
            var gameState = world.GetSingletonRef<GameState>();
            await Assert.That(gameState.TotalCards).IsEqualTo(48);
        }
    }
}