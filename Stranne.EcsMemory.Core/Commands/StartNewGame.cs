using Stranne.EcsMemory.Core.Commands.Abstractions;

namespace Stranne.EcsMemory.Core.Commands;
internal sealed record StartNewGame(int Columns, int Rows, int Seed) : GameCommand;
