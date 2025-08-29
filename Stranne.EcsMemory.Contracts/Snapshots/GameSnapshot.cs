namespace Stranne.EcsMemory.Contracts.Snapshots;
public sealed record GameSnapshot
{
    public required IReadOnlyList<CardSnapshot> Cards { get; init; }
    public required int Rows { get; init; }
    public required int Columns { get; init; }
    public required int TotalCards { get; init; }

    public required int Moves { get; init; }
    public required int MatchedCards { get; init; }

    public required bool IsLocked { get; init; }
    public required bool IsWon { get; init; }

    public required int Version { get; init; }
}