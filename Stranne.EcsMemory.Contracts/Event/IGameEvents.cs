namespace Stranne.EcsMemory.Contracts.Event;
public interface IGameEvents
{
    void OnGameWon(int moves, int totalCards);
}
