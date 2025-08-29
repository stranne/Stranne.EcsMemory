using Arch.Bus;
using Stranne.EcsMemory.Contracts.Event;
using System.Collections.Concurrent;

namespace Stranne.EcsMemory.Core.Events;
public sealed partial class EventManager : IDisposable
{
    private readonly ConcurrentQueue<Action> _actionQueue = new();
    private readonly IGameEvents _gameEvents;

    public EventManager(IGameEvents gameEvents)
    {
        _gameEvents = gameEvents;
        Hook();
    }

    public void Dequeue()
    {
        while (_actionQueue.TryDequeue(out var action))
            action();
    }

    [Event]
    public void OnGameWon(in GameWon gameWon)
    {
        var moves = gameWon.Moves;
        var totalCards = gameWon.TotalCards;
        _actionQueue.Enqueue(() => _gameEvents.OnGameWon(moves, totalCards));
    }

    public void Dispose() => 
        Unhook();
}
