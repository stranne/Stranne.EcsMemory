using Stranne.EcsMemory.Core.Commands.Abstractions;

namespace Stranne.EcsMemory.Core.Commands;
internal sealed record FlipCardAt(int X, int Y) : GameCommand;
