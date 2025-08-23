namespace Stranne.EcsMemory.Contracts;

public sealed record RenderModel
{
    public required IReadOnlyList<RenderCard> Cards { get; init; }
    public required bool IsLocked { get; init; }
    public required bool IsWon { get; init; }
    public required int Moves { get; init; }
    public required int Version { get; init; }
    public required BoardInfo Board { get; init; }
}