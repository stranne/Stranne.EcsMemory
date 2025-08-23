using Arch.Core;
using Arch.System;
using Stranne.EcsMemory.Contracts;
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

    public RenderModel RenderModel { get; private set; } = new()
    {
        Cards = [],
        IsLocked = false,
        IsWon = false,
        Moves = 0,
        Version = -1,
        Board = new()
        {
            Rows = 0,
            Columns = 0,
            TotalCards = 0,
            MatchedCards = 0
        }
    };

    public override void Update(in float _)
    {
        var gameState = World.GetSingletonRef<GameState>();
        if (_lastStateVersion == gameState.StateVersion)
            return;


        var cards = new List<RenderCard>(gameState.TotalCards);

        World.Query(in CardsQuery, (Entity entity, ref CardId cardId, ref GridPosition gridPosition) =>
        {
            var isRevealed = World.Has<Revealed>(entity);
            var isMatched = World.Has<Matched>(entity);

            cards.Add(new RenderCard
            {
                Id = cardId.Value,
                X = gridPosition.X,
                Y = gridPosition.Y,
                IsFacedUp = isRevealed || isMatched,
                IsMatched = isMatched
            });
        });
        cards.Sort((a, b) => a.Y != b.Y ? a.Y.CompareTo(b.Y) : a.X.CompareTo(b.X));

        var config = World.GetSingletonRef<Config>();
        RenderModel = new RenderModel
        {
            Cards = cards,
            IsLocked = gameState.IsLocked,
            IsWon = gameState.IsWon,
            Moves = gameState.Moves,
            Version = gameState.StateVersion,
            Board = new()
            {
                Rows = config.Rows,
                Columns = config.Cols,
                TotalCards = gameState.TotalCards,
                MatchedCards = gameState.MatchedCount
            }
        };

        _lastStateVersion = gameState.StateVersion;
    }
}
