using Arch.Core;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Stranne.EcsMemory.Contracts.Event;
using Stranne.EcsMemory.Core.Commands;
using Stranne.EcsMemory.Core.Commands.Abstractions;
using Stranne.EcsMemory.Core.Components.Value;
using Stranne.EcsMemory.Core.Events;
using Stranne.EcsMemory.Core.Systems;
using Stranne.EcsMemory.Core.Tests.Common;

namespace Stranne.EcsMemory.Core.Tests;
[NotInParallel]
internal sealed class GameCoreTest
{
    private static readonly QueryDescription CardIdQuery = new QueryDescription().WithAll<CardId>();

    [Test]
    public async Task GameCore_StartNewGame_EnqueuesSingleCommandWithCorrectValues()
    {
        using var world = TestWorldFactory.Create();
        var commandBuffer = new CommandBuffer();
        using var sut = new GameCore(world, Substitute.For<ISystemManager>(), commandBuffer, Substitute.For<IEventManager>());

        sut.StartNewGame(1, 2, 3);

        using (Assert.Multiple())
        {
            await Assert.That(commandBuffer.TryDequeue(out var command)).IsTrue();
            await Assert.That(command).IsTypeOf<StartNewGame>();
            var startNewGameCommand = (StartNewGame?)command;
            await Assert.That(startNewGameCommand?.Columns).IsEqualTo(1);
            await Assert.That(startNewGameCommand?.Rows).IsEqualTo(2);
            await Assert.That(startNewGameCommand?.Seed).IsEqualTo(3);
            await Assert.That(commandBuffer.TryDequeue(out _)).IsFalse();
        }
    }

    [Test]
    public async Task GameCore_FlipCardAt_EnqueuesSingleCommandWithCorrectValues()
    {
        using var world = TestWorldFactory.Create();
        var commandBuffer = new CommandBuffer();
        using var sut = new GameCore(world, Substitute.For<ISystemManager>(), commandBuffer, Substitute.For<IEventManager>());

        sut.FlipCardAt(1, 2);

        using (Assert.Multiple())
        {
            await Assert.That(commandBuffer.TryDequeue(out var command)).IsTrue();
            await Assert.That(command).IsTypeOf<FlipCardAt>();
            var flipCardAt = (FlipCardAt?)command;
            await Assert.That(flipCardAt?.X).IsEqualTo(1);
            await Assert.That(flipCardAt?.Y).IsEqualTo(2);
            await Assert.That(commandBuffer.TryDequeue(out _)).IsFalse();
        }
    }

    [Test]
    public async Task GameCore_Serialize_EnsureWorldCanBeSavedAndLoaded()
    {
        using var sut = GameCore.Create(Substitute.For<IGameEvents>(), Substitute.For<ILoggerFactory>());
        sut.StartNewGame(4, 3, 1);
        sut.Update(7);
        sut.Update(39);
        sut.Update(3);

        var actualSerializeData = sut.Serialize();
        var actualGameCore = GameCore.Create(Substitute.For<IGameEvents>(), Substitute.For<ILoggerFactory>(), actualSerializeData);

        using (Assert.Multiple())
        {
            await Assert.That(actualSerializeData).IsNotNullOrEmpty();
            await Assert.That(actualGameCore).IsNotNull();
            await Assert.That(actualGameCore).IsNotEqualTo(sut);
            await Assert.That(actualGameCore.World.CountEntities(in CardIdQuery)).IsEqualTo(12);
        }
    }
}
