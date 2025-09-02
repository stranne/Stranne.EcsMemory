namespace Stranne.EcsMemory.Core.Components.Singleton;
internal struct GameState
{
    public int Moves;
    public bool IsLocked;
    public bool IsWon;
    public int TotalCards;
    public int MatchedCount;
    public uint StateVersion;
}
