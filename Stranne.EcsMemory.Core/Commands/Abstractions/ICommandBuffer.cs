namespace Stranne.EcsMemory.Core.Commands.Abstractions;

public interface ICommandBuffer {
  public void Enqueue(GameCommand command);
  public bool TryDequeue(out GameCommand command);
}
