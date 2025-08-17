namespace Stranne.EcsMemory.Core.Components.Singleton;

using Arch.Core;

internal struct GameState
{
    public int Moves;
    public bool IsLocked;
    public Entity? FirstFlipped;
}
