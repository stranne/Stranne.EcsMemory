using NSubstitute;
using Stranne.EcsMemory.Contracts.Event;
using Stranne.EcsMemory.Core.Events;

namespace Stranne.EcsMemory.Core.Tests.Common;
internal sealed class EventTestHelper : IDisposable
{
    private readonly EventManager _eventManager;
    private readonly IGameEvents _gameEvents;

    public EventTestHelper()
    {
        _gameEvents = Substitute.For<IGameEvents>();
        _eventManager = new EventManager(_gameEvents);
    }

    public IGameEvents ProcessAndGetEvents()
    {
        _eventManager.Dequeue();
        return _gameEvents;
    }

    public void Dispose()
    {
        _eventManager.Dispose();
    }
}
