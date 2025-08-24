namespace Stranne.EcsMemory.Contracts.Event;
public readonly record struct WonEvent(int Moves, int TotalCards, int StateVersion) : IGameEvent;
