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
    private uint _lastStateVersion;

    private static readonly QueryDescription CardsQuery = new QueryDescription().WithAll<CardId, GridPosition, LastChangedStateVersion>();

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

        CurrentStateVersion = 0
    };

    public override void Update(in float _)
    {
        var gameState = World.GetSingletonRef<GameState>();
        if (_lastStateVersion == gameState.StateVersion)
            return;

        var cards = new List<CardSnapshot>(gameState.TotalCards);

        World.Query(in CardsQuery, (Entity entity, ref CardId cardId, ref GridPosition gridPosition, ref LastChangedStateVersion lastChangedStateVersion) =>
        {
            var isRevealed = World.Has<Revealed>(entity);
            var isMatched = World.Has<Matched>(entity);
            var pairKey = isRevealed
                ? World.Get<PairKey>(entity).Value
                : (int?)null;

            cards.Add(new CardSnapshot
            {
                Id = cardId.Value,
                X = gridPosition.X,
                Y = gridPosition.Y,
                IsRevealed = isRevealed,
                IsMatched = isMatched,
                PairKey = pairKey,
                StateVersion = lastChangedStateVersion.StateVersion
            });
        });
        cards.Sort((a, b) => a.Y != b.Y ? a.Y.CompareTo(b.Y) : a.X.CompareTo(b.X));

        var config = World.GetSingletonRef<Config>();
        GameSnapshot = new GameSnapshot
        {
            Cards = cards,
            Rows = config.Rows,
            Columns = config.Columns,
            TotalCards = gameState.TotalCards,

            Moves = gameState.Moves,
            MatchedCards = gameState.MatchedCount,

            IsLocked = gameState.IsLocked,
            IsWon = gameState.IsWon,

            CurrentStateVersion = gameState.StateVersion
        };

        _lastStateVersion = gameState.StateVersion;
    }
}
