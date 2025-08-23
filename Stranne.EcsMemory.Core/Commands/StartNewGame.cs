namespace Stranne.EcsMemory.Core.Commands;

using Abstractions;

internal sealed record StartNewGame(int Columns, int Rows, int Seed) : GameCommand;
