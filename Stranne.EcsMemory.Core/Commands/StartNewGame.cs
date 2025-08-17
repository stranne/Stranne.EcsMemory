namespace Stranne.EcsMemory.Core.Commands;

using Abstractions;

public sealed record StartNewGame(int Cols, int Rows, int? Seed) : GameCommand;
