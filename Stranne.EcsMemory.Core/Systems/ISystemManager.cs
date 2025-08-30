using Stranne.EcsMemory.Contracts.Snapshots;

namespace Stranne.EcsMemory.Core.Systems;
internal interface ISystemManager : IDisposable
{
    GameSnapshot GameSnapshot { get; }
    void Update(float deltaTime);
}