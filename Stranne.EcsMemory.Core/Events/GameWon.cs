namespace Stranne.EcsMemory.Core.Events;

public readonly record struct GameWon(int Moves, int TotalCards, int StateVersion);
