using Arch.Core;
using Stranne.EcsMemory.Core.Components.Singleton;
using Stranne.EcsMemory.Core.Components.Tags;
using Stranne.EcsMemory.Core.Components.Value;

namespace Stranne.EcsMemory.Core.Extensions;

/// <summary>
/// Extensions for tracking entity changes across state versions in the ECS world.
/// Contains workarounds for known Arch ECS framework limitations.
/// </summary>
internal static class WorldChangeTrackingExtensions
{
    /// <summary>
    /// Marks entities as changed by setting their <see cref="LastChangedStateVersion"/> component
    /// to the current game state version for UI change detection and rendering optimization.
    /// </summary>
    /// <param name="world">The ECS world containing the entities.</param>
    /// <param name="entities">Entities to mark as changed.</param>
    /// <remarks>
    /// <para><strong>⚠️ ARCH ECS BUG WORKAROUND (Arch 2.0.0)</strong></para>
    /// <para>
    /// This method skips entities that have <see cref="Matched"/> tag components due to a bug in Arch ECS 2.0.0
    /// where calling <c>world.Set()</c> on entities with certain tag components causes failures during archetype changes.
    /// </para>
    ///
    /// <para><strong>Technical Details:</strong></para>
    /// <list type="bullet">
    /// <item><description><strong>Root Cause:</strong> Archetype transitions fail when adding components to entities that already have specific tag components</description></item>
    /// <item><description><strong>Failing Operation:</strong> <c>world.Set()</c> calls on entities with <see cref="Matched"/> tags</description></item>
    /// <item><description><strong>Confirmed in:</strong> Arch ECS 2.0.0 (project cannot upgrade due to Arch.Persistence 2.0.0 dependency)</description></item>
    /// <item><description><strong>Resolution:</strong> Test with newer Arch versions when dependency constraints allow</description></item>
    /// </list>
    ///
    /// <para><strong>Alternative Workaround Pattern:</strong></para>
    /// <para>
    /// When entities must be marked as changed after receiving <see cref="Matched"/> components,
    /// use the pattern in <see cref="Utils.MatchEvaluationUtil.ApplyMatchResult"/> where
    /// <see cref="LastChangedStateVersion"/> is set <em>before</em> adding <see cref="Matched"/> tags.
    /// </para>
    ///
    /// <para><strong>Educational Value:</strong></para>
    /// <para>
    /// This demonstrates how to handle ECS framework limitations gracefully while maintaining
    /// system functionality and providing clear documentation for future maintenance.
    /// </para>
    /// </remarks>
    public static void MarkChanged(this World world, params Entity[] entities)
    {
        var gameState = world.GetSingletonRef<GameState>();
        foreach (var entity in entities)
        {
            // Skip entities with Matched components to avoid Arch ECS bug (see XML docs above)
            if (!world.Has<Matched>(entity))
                world.Set(entity, new LastChangedStateVersion(gameState.StateVersion));
        }
    }
}
