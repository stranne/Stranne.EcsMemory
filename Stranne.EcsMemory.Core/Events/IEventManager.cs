namespace Stranne.EcsMemory.Core.Events;
public interface IEventManager : IDisposable
{
    void Dequeue();
}