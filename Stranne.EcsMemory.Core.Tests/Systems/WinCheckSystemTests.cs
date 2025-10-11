using Arch.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Stranne.EcsMemory.Core.Components.Singleton;
using Stranne.EcsMemory.Core.Extensions;
using Stranne.EcsMemory.Core.Systems;
using Stranne.EcsMemory.Core.Tests.Common;

namespace Stranne.EcsMemory.Core.Tests.Systems;
[NotInParallel]
internal sealed class WinCheckSystemTests
{
    private static readonly ILogger<WinCheckSystem> Logger = new NullLogger<WinCheckSystem>();

    [Test]
    public async Task WinCheck_SetsIsWonAndTriggersGameWonEvent_WhenAllCardsMatched()
    {
        using var eventManager = new EventTestHelper();
        using var world = TestWorldFactory.Create(totalCards: 10, matchedCount: 10, moves: 14);
        using var sut = new WinCheckSystem(world, Logger);

        sut.Update(0);

        await AssertState(world, true, true);
        eventManager.ProcessAndGetEvents().Received(1).OnGameWon(14, 10);
    }

    [Test]
    public async Task WinCheck_Skip_WhenAlreadyWon()
    {
        using var eventManager = new EventTestHelper();
        using var world = TestWorldFactory.Create(totalCards: 10, matchedCount: 10, isWon: true);
        using var sut = new WinCheckSystem(world, Logger);

        sut.Update(0);

        await AssertState(world, true);
        eventManager.ProcessAndGetEvents().DidNotReceive().OnGameWon(Arg.Any<int>(), Arg.Any<int>());
    }

    [Test]
    public async Task WinCheck_Skip_WhenTotalCardsAreZero()
    {
        using var eventManager = new EventTestHelper();
        using var world = TestWorldFactory.Create(totalCards: 0, matchedCount: 0);
        using var sut = new WinCheckSystem(world, Logger);

        sut.Update(0);

        await AssertState(world);
        eventManager.ProcessAndGetEvents().DidNotReceive().OnGameWon(Arg.Any<int>(), Arg.Any<int>());
    }

    [Test]
    public async Task WinCheck_Skip_WhenNotAllCardsAreMatched()
    {
        using var eventManager = new EventTestHelper();
        using var world = TestWorldFactory.Create(totalCards: 10, matchedCount: 8);
        using var sut = new WinCheckSystem(world, Logger);

        sut.Update(0);
        
        await AssertState(world);
        eventManager.ProcessAndGetEvents().DidNotReceive().OnGameWon(Arg.Any<int>(), Arg.Any<int>());
    }

    private static async Task AssertState(World world, bool isWon = false, bool isLocked = false)
    {
        using (Assert.Multiple())
        {
            var gameState = world.GetSingletonRef<GameState>();
            await Assert.That(gameState.IsWon).IsEqualTo(isWon);
            await Assert.That(gameState.IsLocked).IsEqualTo(isLocked);
        }
    }
}
