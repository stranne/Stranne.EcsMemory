using Stranne.EcsMemory.Core.Components.Singleton;
using Stranne.EcsMemory.Core.Extensions;
using Stranne.EcsMemory.Core.Tests.Common;

namespace Stranne.EcsMemory.Core.Tests.Extensions;
internal sealed class WorldStateExtensionsTest
{
    [Test]
    [Arguments(0, 1)]
    [Arguments(5, 6)]
    [Arguments(42, 43)]
    public async Task IncrementStateVersion_WithInitialValue_IncrementsCorrectly(uint initialValue, uint expectedValue)
    {
        using var world = TestWorldFactory.Create(stateVersion: initialValue);

        world.IncrementStateVersion();

        var gameState = world.GetSingletonRef<GameState>();
        await Assert.That(gameState.StateVersion).IsEqualTo(expectedValue);
    }
}
