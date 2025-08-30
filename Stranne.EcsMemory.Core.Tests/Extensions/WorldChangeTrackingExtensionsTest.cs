using Stranne.EcsMemory.Core.Components.Value;
using Stranne.EcsMemory.Core.Extensions;
using Stranne.EcsMemory.Core.Tests.Common;

namespace Stranne.EcsMemory.Core.Tests.Extensions;
internal sealed class WorldChangeTrackingExtensionsTest
{
    [Test]
    public async Task MarkChanged_SingleEntity_SetsLastChangedStateVersionToCurrentGameState()
    {
        using var world = TestWorldFactory.Create(stateVersion: 5);
        var entity = world.CreateCard();

        world.MarkChanged(entity);

        var lastChanged = world.Get<LastChangedStateVersion>(entity);
        await Assert.That(lastChanged.StateVersion).IsEqualTo(5u);
    }

    [Test]
    public async Task MarkChanged_MultipleEntities_SetsAllToSameStateVersion()
    {
        using var world = TestWorldFactory.Create(stateVersion: 3);
        var entity1 = world.CreateCard();
        var entity2 = world.CreateCard();

        world.MarkChanged(entity1, entity2);

        using (Assert.Multiple())
        {
            var lastChanged1 = world.Get<LastChangedStateVersion>(entity1);
            var lastChanged2 = world.Get<LastChangedStateVersion>(entity2);
            await Assert.That(lastChanged1.StateVersion).IsEqualTo(3u);
            await Assert.That(lastChanged2.StateVersion).IsEqualTo(3u);
        }
    }

    [Test]
    public void MarkChanged_EmptyEntityArray_DoesNotThrow()
    {
        using var world = TestWorldFactory.Create();

        world.MarkChanged();
    }
}
