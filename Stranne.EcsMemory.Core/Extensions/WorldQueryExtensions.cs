using Arch.Core;
using Stranne.EcsMemory.Core.Components.Tags;

namespace Stranne.EcsMemory.Core.Extensions;
internal static class WorldQueryExtensions
{
    public static bool TryGetFirst(this World world, QueryDescription query, out Entity entity)
    {
        Entity innerEntity = default;
        var found = false;

        world.Query(in query, entity =>
        {
            if (found)
                return;

            found = true;
            innerEntity = entity;
        });

        entity = innerEntity;
        return found;
    }
}
