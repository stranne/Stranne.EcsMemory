using Arch.Core;
using Stranne.EcsMemory.Core.Extensions;
using TUnit.Assertions.AssertConditions.Throws;

namespace Stranne.EcsMemory.Core.Tests.Extensions;
internal sealed class WorldSingletonExtensionsTest
{
    private World _world = null!;

    [Before(Test)]
    public void BeforeTest()
    {
        _world = World.Create();
    }

    [Test]
    public async Task GetSingletonRef_ReturnsRef_AllowingInPlaceMutation()
    {
        _world.Create(new Foo(1));

        var actual = _world.GetSingletonRef<Foo>();

        await Assert.That(actual).IsNotDefault();
    }

    [Test]
    public async Task GetSingletonRef_Throws_WhenComponentDoesNotExist() => 
        await Assert.That(() => _world.GetSingletonRef<Foo>()).Throws<InvalidOperationException>();

    [Test]
    public async Task SetOrCreateSingleton_CreatesEntity_WhenComponentDoesNotExist()
    {
        _world.SetOrCreateSingleton(new Foo(42));

        var query = new QueryDescription().WithAll<Foo>();
        var numbers = _world.CountEntities(query);
        await Assert.That(numbers).IsEqualTo(1);

        Foo actual = default;
        var found = false;
        _world.Query(in query, (Entity _, ref Foo foo) =>
        {
            actual = foo;
            found = true;
        });
        await Assert.That(found).IsTrue();
        await Assert.That(actual.A).IsEqualTo(42);
    }

    [Test]
    public async Task SetOrCreateSingleton_UpdatesExistingEntity_WhenComponentAlreadyExists()
    {
        _world.Create(new Foo(1));

        _world.SetOrCreateSingleton(new Foo(42));

        var query = new QueryDescription().WithAll<Foo>();
        var numbers = _world.CountEntities(query);
        await Assert.That(numbers).IsEqualTo(1);

        Foo actual = default;
        var found = false;
        _world.Query(in query, (Entity _, ref Foo foo) =>
        {
            actual = foo;
            found = true;
        });
        await Assert.That(found).IsTrue();
        await Assert.That(actual.A).IsEqualTo(42);
    }

    private readonly record struct Foo(int A);
}
