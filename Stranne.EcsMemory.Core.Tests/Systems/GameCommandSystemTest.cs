using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Stranne.EcsMemory.Core.Commands;
using Stranne.EcsMemory.Core.Commands.Base;
using Stranne.EcsMemory.Core.Systems;
using Stranne.EcsMemory.Core.Tests.Common;

namespace Stranne.EcsMemory.Core.Tests.Systems;
[NotInParallel]
internal sealed class GameCommandSystemTest
{
    private static readonly ILogger<GameCommandSystem> Logger = new NullLogger<GameCommandSystem>();

    [Test]
    public void Update_EmptyQueue_DoesNothing()
    {
        using var world = TestWorldFactory.Create();
        var queue = Substitute.For<IGameCommandQueue>();
        queue.TryDequeue(out Arg.Any<GameCommand>()).Returns(false);
        using var sut = new GameCommandSystem(world, queue, Logger);

        sut.Update(0);

        queue.Received(1).TryDequeue(out Arg.Any<GameCommand>());
        queue.DidNotReceive().ExecuteCommand(Arg.Any<GameCommand>(), Arg.Any<Arch.Core.World>());
    }

    [Test]
    public void Update_SingleCommand_ExecutesCommand()
    {
        using var world = TestWorldFactory.Create();
        var queue = Substitute.For<IGameCommandQueue>();
        var command = new FlipCardAt(1, 1);

        queue.TryDequeue(out Arg.Any<GameCommand>())
            .Returns(x => { x[0] = command; return true; }, x => false);
        queue.ExecuteCommand(command, world).Returns(CommandResult.Success);

        using var sut = new GameCommandSystem(world, queue, Logger);

        sut.Update(0);

        queue.Received(2).TryDequeue(out Arg.Any<GameCommand>());
        queue.Received(1).ExecuteCommand(command, world);
    }

    [Test]
    public void Update_MultipleCommands_ExecutesAllCommands()
    {
        using var world = TestWorldFactory.Create();
        var queue = Substitute.For<IGameCommandQueue>();
        var command1 = new FlipCardAt(1, 1);
        var command2 = new StartNewGame(4, 4, 123);
        var command3 = new FlipCardAt(2, 2);

        queue.TryDequeue(out Arg.Any<GameCommand>())
            .Returns(
                x => { x[0] = command1; return true; },
                x => { x[0] = command2; return true; },
                x => { x[0] = command3; return true; },
                x => false
            );
        queue.ExecuteCommand(Arg.Any<GameCommand>(), world).Returns(CommandResult.Success);

        using var sut = new GameCommandSystem(world, queue, Logger);

        sut.Update(0);

        queue.Received(4).TryDequeue(out Arg.Any<GameCommand>());
        queue.Received(1).ExecuteCommand(command1, world);
        queue.Received(1).ExecuteCommand(command2, world);
        queue.Received(1).ExecuteCommand(command3, world);
    }

    [Test]
    public void Update_StartNewGameCommand_ExecutesCommand()
    {
        using var world = TestWorldFactory.Create();
        var queue = Substitute.For<IGameCommandQueue>();
        var command = new StartNewGame(4, 4, 123);

        queue.TryDequeue(out Arg.Any<GameCommand>())
            .Returns(x => { x[0] = command; return true; }, x => false);
        queue.ExecuteCommand(command, world).Returns(CommandResult.Success);

        using var sut = new GameCommandSystem(world, queue, Logger);

        sut.Update(0);

        queue.Received(1).ExecuteCommand(command, world);
    }

    [Test]
    public void Update_FlipCardAtCommand_ExecutesCommand()
    {
        using var world = TestWorldFactory.Create();
        var queue = Substitute.For<IGameCommandQueue>();
        var command = new FlipCardAt(2, 3);

        queue.TryDequeue(out Arg.Any<GameCommand>())
            .Returns(x => { x[0] = command; return true; }, x => false);
        queue.ExecuteCommand(command, world).Returns(CommandResult.Success);

        using var sut = new GameCommandSystem(world, queue, Logger);

        sut.Update(0);

        queue.Received(1).ExecuteCommand(command, world);
    }

    [Test]
    public void Update_CommandExecutionFails_ContinuesProcessing()
    {
        using var world = TestWorldFactory.Create();
        var queue = Substitute.For<IGameCommandQueue>();
        var command1 = new FlipCardAt(1, 1);
        var command2 = new FlipCardAt(2, 2);

        queue.TryDequeue(out Arg.Any<GameCommand>())
            .Returns(
                x => { x[0] = command1; return true; },
                x => { x[0] = command2; return true; },
                x => false
            );
        queue.ExecuteCommand(command1, world).Returns(CommandResult.Failed);
        queue.ExecuteCommand(command2, world).Returns(CommandResult.Success);

        using var sut = new GameCommandSystem(world, queue, Logger);

        sut.Update(0);

        queue.Received(3).TryDequeue(out Arg.Any<GameCommand>());
        queue.Received(1).ExecuteCommand(command1, world);
        queue.Received(1).ExecuteCommand(command2, world);
    }

    [Test]
    public void Update_ExceptionDuringExecution_ContinuesProcessing()
    {
        using var world = TestWorldFactory.Create();
        var queue = Substitute.For<IGameCommandQueue>();
        var command1 = new FlipCardAt(1, 1);
        var command2 = new FlipCardAt(2, 2);

        queue.TryDequeue(out Arg.Any<GameCommand>())
            .Returns(
                x => { x[0] = command1; return true; },
                x => { x[0] = command2; return true; },
                x => false
            );
        queue.ExecuteCommand(command1, world).Throws(new InvalidOperationException("Test exception"));
        queue.ExecuteCommand(command2, world).Returns(CommandResult.Success);

        using var sut = new GameCommandSystem(world, queue, Logger);

        sut.Update(0);

        queue.Received(3).TryDequeue(out Arg.Any<GameCommand>());
        queue.Received(1).ExecuteCommand(command1, world);
        queue.Received(1).ExecuteCommand(command2, world);
    }
}