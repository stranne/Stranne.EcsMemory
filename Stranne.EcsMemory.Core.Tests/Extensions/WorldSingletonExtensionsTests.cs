using Arch.Core;
using Stranne.EcsMemory.Core.Extensions;
using Stranne.EcsMemory.Core.Tests.Common;

namespace Stranne.EcsMemory.Core.Tests.Extensions;
internal sealed class WorldSingletonExtensionsTests
{
    private static readonly QueryDescription FooQuery = new QueryDescription().WithAll<Foo>();

    [Test]
    public async Task GetSingletonRef_ReturnsRef_AllowingInPlaceMutation()
    {
        using var world = TestWorldFactory.Create();
        world.Create(new Foo(1));

        var actual = world.GetSingletonRef<Foo>();

        await Assert.That(actual).IsNotDefault();
    }

    [Test]
    public async Task GetSingletonRef_Throws_WhenComponentDoesNotExist()
    {
        using var world = TestWorldFactory.Create();

        await Assert.That(() => world.GetSingletonRef<Foo>()).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task SetOrCreateSingleton_CreatesEntity_WhenComponentDoesNotExist()
    {
        using var world = TestWorldFactory.Create();

        world.SetOrCreateSingleton(new Foo(1));

        await AssertCreatedComponent(world, 1);
    }

    [Test]
    public async Task SetOrCreateSingleton_UpdatesExistingEntity_WhenComponentAlreadyExists()
    {
        using var world = TestWorldFactory.Create();
        world.Create(new Foo(1));

        world.SetOrCreateSingleton(new Foo(2));

        await AssertCreatedComponent(world, 2);
    }

    private static async Task AssertCreatedComponent(World world, int expectedValue)
    {
        using (Assert.Multiple())
        {
            var numbers = world.CountEntities(in FooQuery);
            await Assert.That(numbers).IsEqualTo(1);

            Foo actual = default;
            var found = false;
            world.Query(in FooQuery, (Entity _, ref Foo foo) =>
            {
                actual = foo;
                found = true;
            });
            await Assert.That(found).IsTrue();
            await Assert.That(actual.A).IsEqualTo(expectedValue);
        }
    }

    private readonly record struct Foo(int A);
}
