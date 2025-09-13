using Arch.Core;
using Stranne.EcsMemory.Core.Components.Singleton;
using Stranne.EcsMemory.Core.Components.Tags;
using Stranne.EcsMemory.Core.Components.Value;

namespace Stranne.EcsMemory.Core.Extensions;
internal static class WorldChangeTrackingExtensions
{
    public static void MarkChanged(this World world, params Entity[] entities)
    {
        var gameState = world.GetSingletonRef<GameState>();
        foreach (var entity in entities)
        {
            // Skip entities with Matched components to avoid Arch ECS bug
            if (!world.Has<Matched>(entity))
                world.Set(entity, new LastChangedStateVersion(gameState.StateVersion));
        }
    }
}
