namespace Stranne.EcsMemory.Core.Components.Singleton;

internal readonly record struct Config(int Cols, int Rows, int EvalDelayTicks, int Seed);
