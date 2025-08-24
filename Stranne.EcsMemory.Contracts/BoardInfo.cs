namespace Stranne.EcsMemory.Contracts;
public sealed record BoardInfo
{
    public required int Rows { get; init; }
    public required int Columns { get; init; }
    public required int TotalCards { get; init; }
    public required int MatchedCards { get; init; }
}