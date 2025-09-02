using Arch.Core;

namespace Stranne.EcsMemory.Core.Extensions;
internal static class WorldSingletonExtensions
{
    public static ref T GetSingletonRef<T>(this World world) where T : struct
    {
        if (!TryGetSingleton<T>(world, out var entity))
            throw new InvalidOperationException($"Singleton of type {typeof(T)} not found in the world.");

        return ref world.Get<T>(entity);
    }

    public static void SetOrCreateSingleton<T>(this World world, in T component) where T : struct
    {
        if (TryGetSingleton<T>(world, out var entity))
            world.Set(entity, component);
        else
            world.Create(component);
    }

    private static bool TryGetSingleton<T>(World world, out Entity entity) where T : struct
    {
        var query = Cache<T>.Query;
        var found = world.TryGetFirst(query, out var foundEntity);
        entity = foundEntity;
        return found;
    }

    private static class Cache<T> where T : struct
    {
        internal static readonly QueryDescription Query = new QueryDescription().WithAll<T>();
    }
}
