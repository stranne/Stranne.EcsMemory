using Arch.Core;
using Stranne.EcsMemory.Core.Components.Singleton;
using Stranne.EcsMemory.Core.Components.Tags;
using Stranne.EcsMemory.Core.Components.Value;
using Stranne.EcsMemory.Core.Extensions;

namespace Stranne.EcsMemory.Core.Utils;
internal static class BoardSetupUtil
{
    public static void BuildBoard(World world, Config config)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(config.Cols);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(config.Rows);

        var totalCards = config.Cols * config.Rows;
        if ((totalCards & 1) != 0)
            throw new ArgumentException("Board size must be even (Cols * Rows).", nameof(config));

        CreateNewCards(world, config, totalCards);
        ResetGameState(world, totalCards);
    }

    private static void CreateNewCards(World world, Config config, int totalCards)
    {
        DestroyAnyExistingCards(world);

        var deck = new PairKey[totalCards];
        for (int cardIndex = 0, pairIndex = 0; pairIndex < totalCards / 2; pairIndex++)
        {
            deck[cardIndex++] = new PairKey(pairIndex);
            deck[cardIndex++] = new PairKey(pairIndex);
        }

        var random = new Random(config.Seed);
        ShuffleInPlace(deck, random);

        var id = 0;
        for (var index = 0; index < deck.Length; index++)
        {
            var x = index % config.Cols;
            var y = index / config.Cols;

            _ = world.Create(
                new CardId(id++),
                deck[index],
                new GridPosition(x, y),
                new Selectable());
        }
    }

    private static void DestroyAnyExistingCards(World world)
    {
        var queryCards = new QueryDescription().WithAll<CardId>();
        var entitiesToDestroy = new List<Entity>();
        world.Query(in queryCards, entitiesToDestroy.Add);

        foreach (var entity in entitiesToDestroy)
            world.Destroy(entity);
    }

    /// <summary>
    /// In-place Fisher–Yates shuffle. Deterministic based on <param name="random" />.
    /// </summary>
    internal static void ShuffleInPlace<T>(IList<T> list, Random random)
    {
        for (var index = list.Count - 1; index > 0; index--)
        {
            var swapIndex = random.Next(0, index + 1);
            (list[index], list[swapIndex]) = (list[swapIndex], list[index]);
        }
    }

    private static void ResetGameState(World world, int totalCards) =>
        world.SetOrCreateSingleton(new GameState
        {
            FirstFlipped = null,
            IsLocked = false,
            Moves = 0,
            IsWon = false,
            TotalCards = totalCards,
            MatchedCount = 0
        });
}
