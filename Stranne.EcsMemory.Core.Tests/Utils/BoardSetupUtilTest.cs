using Arch.Core;
using Stranne.EcsMemory.Core.Components.Singleton;
using Stranne.EcsMemory.Core.Components.Tags;
using Stranne.EcsMemory.Core.Components.Value;
using Stranne.EcsMemory.Core.Extensions;
using Stranne.EcsMemory.Core.Tests.Common;
using Stranne.EcsMemory.Core.Utils;
using System.Runtime.InteropServices;

namespace Stranne.EcsMemory.Core.Tests.Utils;
[NotInParallel]
internal sealed class BoardSetupUtilTest
{
    [Test]
    public void BuildBoard_Throws_OnOddCellCount()
    {
        using var world = TestWorldFactory.Create(3, 3);
        var config = world.GetSingletonRef<Config>();

        Assert.Throws<ArgumentException>(() => BoardSetupUtil.BuildBoard(world, config));
    }

    [Test]
    public async Task BuildBoard_Creates_Exactly_ColsTimesRows_Cards()
    {
        using var world = TestWorldFactory.Create();
        var config = world.GetSingletonRef<Config>();
        var expectedCardCount = config.Columns * config.Rows;

        BoardSetupUtil.BuildBoard(world, config);

        using (Assert.Multiple())
        {
            var queryCards = new QueryDescription().WithAll<CardId>();
            var cardCount = world.CountEntities(in queryCards);
            await Assert.That(cardCount).IsEqualTo(expectedCardCount);

            var gameState = world.GetSingletonRef<GameState>();
            await Assert.That(gameState.TotalCards).IsEqualTo(expectedCardCount);
        }
    }

    [Test]
    public async Task BuildBoard_Assigns_Unique_GridPositions_WithinBounds()
    {
        using var world = TestWorldFactory.Create();
        var config = world.GetSingletonRef<Config>();

        BoardSetupUtil.BuildBoard(world, config);

        var gridPositionQuery = new QueryDescription().WithAll<GridPosition>();
        var seen = new HashSet<(int x, int y)>();
        var withinBounds = true;

        world.Query(in gridPositionQuery, (Entity _, ref GridPosition gridPosition) =>
        {
            withinBounds &= gridPosition.X >= 0 && gridPosition.X < config.Columns && gridPosition.Y >= 0 && gridPosition.Y < config.Rows;
            seen.Add((gridPosition.X, gridPosition.Y));
        });

        using (Assert.Multiple())
        {
            await Assert.That(withinBounds).IsTrue();
            await Assert.That(seen.Count).IsEqualTo(config.Columns * config.Rows);
        }
    }

    [Test]
    public async Task BuildBoard_Ensures_Exact_Two_Of_Each_PairKey()
    {
        using var world = TestWorldFactory.Create();
        var config = world.GetSingletonRef<Config>();

        BoardSetupUtil.BuildBoard(world, config);

        var gridPositionQuery = new QueryDescription().WithAll<GridPosition>();
        var counts = new Dictionary<PairKey, int>();
        world.Query(in gridPositionQuery, (Entity _, ref PairKey pairKey) =>
        {
            ref var slot = ref CollectionsMarshal.GetValueRefOrAddDefault(counts, pairKey, out var exists);
            if (!exists) slot = 0;
            slot++;
        });

        using (Assert.Multiple())
        {
            await Assert.That(counts.Values).All().Satisfy(x => x.IsEqualTo(2));
            await Assert.That(counts.Count).IsEqualTo(config.Columns * config.Rows / 2);
        }
    }

    [Test]
    public async Task BuildBoard_IsDeterministic_WithSameSeed()
    {
        using var world1 = TestWorldFactory.Create();
        using var world2 = TestWorldFactory.Create();

        var gridPositionQuery = new QueryDescription().WithAll<GridPosition>();
        var map1 = new Dictionary<GridPosition, PairKey>();
        var map2 = new Dictionary<GridPosition, PairKey>();

        world1.Query(in gridPositionQuery, (Entity _, ref GridPosition gridPosition, ref PairKey pairKey) =>
        {
            map1[gridPosition] = pairKey;
        });
        world2.Query(in gridPositionQuery, (Entity _, ref GridPosition gridPosition, ref PairKey pairKey) =>
        {
            map2[gridPosition] = pairKey;
        });

        await Assert.That(map1).IsEquivalentTo(map2);
    }

    [Test]
    public async Task BuildBoard_StartsClean_NoMatched_NoRevealed_AllSelectable_AndGameStateReset()
    {
        using var world = TestWorldFactory.Create();
        var config = world.GetSingletonRef<Config>();

        BoardSetupUtil.BuildBoard(world, config);

        using (Assert.Multiple())
        {
            var cardQuery = new QueryDescription().WithAll<CardId>();
            bool anyMatched = false, anyRevealed = false;

            world.Query(in cardQuery, entity =>
            {
                anyMatched |= world.Has<Matched>(entity);
                anyRevealed |= world.Has<Revealed>(entity);
            });

            await Assert.That(anyMatched).IsFalse();
            await Assert.That(anyRevealed).IsFalse();

            var gameStateQuery = new QueryDescription().WithAll<GameState>();
            var found = false;
            GameState gameState = default;
            world.Query(in gameStateQuery, (Entity _, ref GameState state) =>
            {
                found = true;
                gameState = state;
            });

            await Assert.That(found).IsTrue();
            await Assert.That(gameState.IsLocked).IsFalse();
            await Assert.That(gameState.Moves).IsEqualTo(0);
        }
    }

    [Test]
    public async Task BuildBoard_Rebuilds_ClearsPreviousCards_AndKeepsDeterminism()
    {
        using var world = TestWorldFactory.Create();
        var config = world.GetSingletonRef<Config>();

        BoardSetupUtil.BuildBoard(world, config);
        BoardSetupUtil.BuildBoard(world, config);

        var cardQuery = new QueryDescription().WithAll<CardId>();
        var cardCount = world.CountEntities(in cardQuery);
        await Assert.That(cardCount).IsEqualTo(config.Columns * config.Rows);
    }

    [Test]
    [Arguments(new int[0], 0, new int[0])]
    [Arguments(new[] { 1, 2, 3 }, 0, new[] { 1, 2, 3 })]
    [Arguments(new[] { 1, 2, 3 }, 42, new[] { 2, 1, 3 })]
    [Arguments(new[] { 1, 2, 3, 4 }, 7, new[] { 1, 4, 3, 2 })]
    [Arguments(new[] { 1, 1, 1 }, 99, new[] { 1, 1, 1 })]
    public async Task ShuffleInPlace(IList<int> originalList, int seed, IList<int> expectedList)
    {
        var random = new Random(seed);

        BoardSetupUtil.ShuffleInPlace(originalList, random);

        await Assert.That(originalList).IsEquivalentTo(expectedList);
    }
}
