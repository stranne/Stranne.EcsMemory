using Arch.Core;
using Stranne.EcsMemory.Core.Components.Singleton;
using Stranne.EcsMemory.Core.Components.Tags;
using Stranne.EcsMemory.Core.Components.Value;

namespace Stranne.EcsMemory.Core.Tests.Common;
internal static class WorldExtensions
{
    public static Entity CreateCard(
        this World world,
        int cardId = 0,
        int pairKey = 0,
        bool revealed = false,
        bool matched = false,
        int x = 0,
        int y = 0)
    {
        var entity = world.Create(
            new CardId(cardId),
            new PairKey(pairKey),
            new GridPosition(x, y),
            new Selectable());

        if (revealed)
            world.Add<Revealed>(entity);
        if (matched)
            world.Add<Matched>(entity);

        return entity;
    }

    public static Entity CreatePending(this World world, int ticksLeft = 1) =>
        world.Create(new PendingEvaluation { TicksLeft = ticksLeft });

    public static bool HasAny<T>(this World world) where T : struct
    {
        var any = false;
        world.Query(in Cache<T>.Query, _ => any = true);
        return any;
    }

    private static class Cache<T> where T : struct
    {
        public static readonly QueryDescription Query =
            new QueryDescription().WithAll<T>();
    }
}
