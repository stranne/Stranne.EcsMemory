namespace Stranne.EcsMemory.Core.Commands;

using Abstractions;

public sealed record FlipAtGrid(int X, int Y) : GameCommand;
