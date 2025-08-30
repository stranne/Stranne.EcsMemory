using Arch.Core;
using Stranne.EcsMemory.Core.Components.Singleton;

namespace Stranne.EcsMemory.Core.Extensions;
internal static class WorldStateExtensions
{
    public static void IncrementStateVersion(this World world) => 
        world.GetSingletonRef<GameState>().StateVersion++;
}
