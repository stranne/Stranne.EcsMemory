using Stranne.EcsMemory.Core.Commands.Base;

namespace Stranne.EcsMemory.Core.Commands;
internal sealed record StartNewGame(int Columns, int Rows, int Seed) : GameCommand, IImmediateCommand;
