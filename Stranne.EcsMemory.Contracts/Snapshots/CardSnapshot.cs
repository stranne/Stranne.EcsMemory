namespace Stranne.EcsMemory.Contracts.Snapshots;
public sealed record CardSnapshot
{
    public required int Id { get; init; }
    public required int X { get; init; }
    public required int Y { get; init; }

    public required bool IsFacedUp { get; init; }
    public required bool IsMatched { get; init; }
    /// <summary>
    /// Not <see langword="null"/> when <see cref="IsFacedUp"/> is <see langword="true"/>.
    /// </summary>
    public int? PairKey { get; init; }

    public required uint StateVersion { get; init; }
    public bool HasChanged { get; set; }
}