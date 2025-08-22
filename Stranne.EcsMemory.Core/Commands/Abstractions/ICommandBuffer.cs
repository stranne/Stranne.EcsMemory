namespace Stranne.EcsMemory.Core.Commands.Abstractions;

public interface ICommandBuffer
{
    public void Enqueue(GameCommand command);
    internal bool TryDequeue(out GameCommand command);
}
