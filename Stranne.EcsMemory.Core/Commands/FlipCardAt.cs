namespace Stranne.EcsMemory.Core.Commands;

using Abstractions;

internal sealed record FlipCardAt(int X, int Y) : GameCommand;
