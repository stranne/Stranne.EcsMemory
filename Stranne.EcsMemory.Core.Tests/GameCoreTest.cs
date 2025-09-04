using Arch.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Stranne.EcsMemory.Contracts;
using Stranne.EcsMemory.Contracts.Event;
using Stranne.EcsMemory.Core.Commands;
using Stranne.EcsMemory.Core.Commands.Base;
using Stranne.EcsMemory.Core.Components.Value;
using Stranne.EcsMemory.Core.Events;
using Stranne.EcsMemory.Core.Systems;
using Stranne.EcsMemory.Core.Tests.Common;

namespace Stranne.EcsMemory.Core.Tests;
[NotInParallel]
internal sealed class GameCoreTest
{
    private static readonly GameConfiguration DefaultConfig = new();
    private static readonly QueryDescription CardIdQuery = new QueryDescription().WithAll<CardId>();

    [Test]
    public async Task GameCore_StartNewGame_ProcessedImmediately()
    {
        using var world = TestWorldFactory.Create();
        var commandQueue = new GameCommandQueue(DefaultConfig, NullLogger<GameCommandQueue>.Instance);
        using var sut = new GameCore(world, Substitute.For<ISystemManager>(), commandQueue, Substitute.For<IEventManager>());

        sut.StartNewGame(2, 2, 3);

        using (Assert.Multiple())
        {
            await Assert.That(commandQueue.TryDequeue(out _)).IsFalse();
            // Verify the game was actually started by checking for cards created
            await Assert.That(world.CountEntities(in CardIdQuery)).IsEqualTo(4);
        }
    }

    [Test]
    public async Task GameCore_FlipCardAt_EnqueuesSingleCommandWithCorrectValues()
    {
        using var world = TestWorldFactory.Create();
        var commandQueue = new GameCommandQueue(DefaultConfig, NullLogger<GameCommandQueue>.Instance);
        using var sut = new GameCore(world, Substitute.For<ISystemManager>(), commandQueue, Substitute.For<IEventManager>());

        sut.FlipCardAt(1, 2);

        using (Assert.Multiple())
        {
            await Assert.That(commandQueue.TryDequeue(out var command)).IsTrue();
            await Assert.That(command).IsTypeOf<FlipCardAt>();
            var flipCardAt = (FlipCardAt?)command;
            await Assert.That(flipCardAt?.X).IsEqualTo(1);
            await Assert.That(flipCardAt?.Y).IsEqualTo(2);
            await Assert.That(commandQueue.TryDequeue(out _)).IsFalse();
        }
    }

    [Test]
    public async Task GameCore_Serialize_EnsureWorldCanBeSavedAndLoaded()
    {
        using var sut = GameCore.Create(DefaultConfig, Substitute.For<IGameEvents>(), Substitute.For<ILoggerFactory>());
        sut.StartNewGame(4, 3, 1);
        sut.Update(7);
        sut.Update(39);
        sut.Update(3);

        var actualSerializeData = sut.Serialize();
        var actualGameCore = GameCore.Create(DefaultConfig, Substitute.For<IGameEvents>(), Substitute.For<ILoggerFactory>(), actualSerializeData);

        using (Assert.Multiple())
        {
            await Assert.That(actualSerializeData).IsNotNullOrEmpty();
            await Assert.That(actualGameCore).IsNotNull();
            await Assert.That(actualGameCore).IsNotEqualTo(sut);
            await Assert.That(actualGameCore.World.CountEntities(in CardIdQuery)).IsEqualTo(12);
        }
    }
}
