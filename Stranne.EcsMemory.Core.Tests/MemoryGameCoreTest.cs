using NSubstitute;
using Stranne.EcsMemory.Core.Commands;
using Stranne.EcsMemory.Core.Commands.Abstractions;
using Stranne.EcsMemory.Core.Events;
using Stranne.EcsMemory.Core.Systems;
using Stranne.EcsMemory.Core.Tests.Common;

namespace Stranne.EcsMemory.Core.Tests;
internal sealed class MemoryGameCoreTest
{
    [Test]
    public async Task MemoryGameCore_StartNewGame_EnqueuesSingleCommandWithCorrectValues()
    {
        using var world = TestWorldFactory.Create();
        var commandBuffer = new CommandBuffer();
        using var sut = new MemoryGameCore(world, Substitute.For<ISystemManager>(), commandBuffer, Substitute.For<IEventManager>());

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
    public async Task MemoryGameCore_FlipCardAt_EnqueuesSingleCommandWithCorrectValues()
    {
        using var world = TestWorldFactory.Create();
        var commandBuffer = new CommandBuffer();
        using var sut = new MemoryGameCore(world, Substitute.For<ISystemManager>(), commandBuffer, Substitute.For<IEventManager>());

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
}
