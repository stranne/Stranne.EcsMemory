namespace Stranne.EcsMemory.Contracts;

public sealed record RenderCard
{
    public required int Id { get; init; }
    public required int X { get; init; }
    public required int Y { get; init; }
    public required bool IsFacedUp { get; init; }
    public required bool IsMatched { get; init; }
}