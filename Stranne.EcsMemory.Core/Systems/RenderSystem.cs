using Arch.Core;
using Arch.System;
using Stranne.EcsMemory.Contracts.Snapshots;
using Stranne.EcsMemory.Core.Components.Singleton;
using Stranne.EcsMemory.Core.Components.Tags;
using Stranne.EcsMemory.Core.Components.Value;
using Stranne.EcsMemory.Core.Extensions;

namespace Stranne.EcsMemory.Core.Systems;
internal sealed class RenderSystem(World world)
    : BaseSystem<World, float>(world)
{
    private int _lastStateVersion = -1;

    private static readonly QueryDescription CardsQuery = new QueryDescription().WithAll<CardId, GridPosition>();

    public GameSnapshot GameSnapshot { get; private set; } = new()
    {
        Cards = [],
        Rows = 0,
        Columns = 0,
        TotalCards = 0,

        Moves = 0,
        MatchedCards = 0,

        IsLocked = false,
        IsWon = false,

        Version = -1
    };

    public override void Update(in float _)
    {
        var gameState = World.GetSingletonRef<GameState>();
        if (_lastStateVersion == gameState.StateVersion)
            return;


        var cards = new List<CardSnapshot>(gameState.TotalCards);

        World.Query(in CardsQuery, (Entity entity, ref CardId cardId, ref GridPosition gridPosition) =>
        {
            var isRevealed = World.Has<Revealed>(entity);
            var isMatched = World.Has<Matched>(entity);
            var isFacedUp = isRevealed || isMatched;
            var pairKey = isFacedUp
                ? World.Get<PairKey>(entity).Value
                : (int?)null;

            cards.Add(new CardSnapshot
            {
                Id = cardId.Value,
                X = gridPosition.X,
                Y = gridPosition.Y,
                IsFacedUp = isFacedUp,
                IsMatched = isMatched,
                PairKey = pairKey
            });
        });
        cards.Sort((a, b) => a.Y != b.Y ? a.Y.CompareTo(b.Y) : a.X.CompareTo(b.X));

        var config = World.GetSingletonRef<Config>();
        GameSnapshot = new GameSnapshot
        {
            Cards = cards,
            Rows = config.Rows,
            Columns = config.Cols,
            TotalCards = gameState.TotalCards,

            Moves = gameState.Moves,
            MatchedCards = gameState.MatchedCount,

            IsLocked = gameState.IsLocked,
            IsWon = gameState.IsWon,

            Version = gameState.StateVersion
        };

        _lastStateVersion = gameState.StateVersion;
    }
}
