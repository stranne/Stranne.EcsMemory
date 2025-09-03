using Stranne.EcsMemory.Core.Commands.Base;

namespace Stranne.EcsMemory.Core.Commands;
internal sealed record FlipCardAt(int X, int Y) : GameCommand;
