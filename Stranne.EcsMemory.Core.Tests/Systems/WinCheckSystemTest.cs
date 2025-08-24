using Arch.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stranne.EcsMemory.Core.Components.Events;
using Stranne.EcsMemory.Core.Components.Singleton;
using Stranne.EcsMemory.Core.Extensions;
using Stranne.EcsMemory.Core.Systems;
using Stranne.EcsMemory.Core.Tests.Common;

namespace Stranne.EcsMemory.Core.Tests.Systems;
internal sealed class WinCheckSystemTest
{
    private static readonly QueryDescription WonEventQuery = new QueryDescription().WithAll<EventWon, EventMetadata>();
    private static readonly ILogger<WinCheckSystem> Logger = new NullLogger<WinCheckSystem>();

    [Test]
    public async Task WinCheck_SetsIsWon_WhenAllMatched()
    {
        var world = TestWorldFactory.Create(totalCards: 10, matchedCount: 10);
        var sut = new WinCheckSystem(world, Logger);

        sut.Update(0);

        await AssertState(world, true, true, true);
    }

    [Test]
    public async Task WinCheck_Skip_WhenAlreadyWon()
    {
        var world = TestWorldFactory.Create(totalCards: 10, matchedCount: 10, isWon: true);
        var sut = new WinCheckSystem(world, Logger);

        sut.Update(0);

        await AssertState(world, true);
    }

    [Test]
    public async Task WinCheck_Skip_WhenTotalCardsAreZero()
    {
        var world = TestWorldFactory.Create(totalCards: 0, matchedCount: 0);
        var sut = new WinCheckSystem(world, Logger);

        sut.Update(0);

        await AssertState(world);
    }

    [Test]
    public async Task WinCheck_Skip_WhenNotAllCardsAreMatched()
    {
        var world = TestWorldFactory.Create(totalCards: 10, matchedCount: 8);
        var sut = new WinCheckSystem(world, Logger);
        sut.Update(0);
        
        await AssertState(world);
    }

    private static async Task AssertState(World world, bool isWon = false, bool isLocked = false, bool expectWonEvent = false)
    {
        using (Assert.Multiple())
        {
            var gameState = world.GetSingletonRef<GameState>();
            await Assert.That(gameState.IsWon).IsEqualTo(isWon);
            await Assert.That(gameState.IsLocked).IsEqualTo(isLocked);

            var eventWonCount = world.CountEntities(in WonEventQuery);
            await Assert.That(eventWonCount).IsEqualTo(expectWonEvent ? 1 : 0);
        }
    }
}
