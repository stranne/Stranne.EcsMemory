using Arch.Core;
using Stranne.EcsMemory.Core.Components.Singleton;
using Stranne.EcsMemory.Core.Components.Value;

namespace Stranne.EcsMemory.Core.Extensions;
internal static class WorldChangeTrackingExtensions
{
    public static void MarkChanged(this World world, params Entity[] entities)
    {
        var gameState = world.GetSingletonRef<GameState>();
        foreach (var entity in entities)
            world.Set(entity, new LastChangedStateVersion(gameState.StateVersion));
    }
}
