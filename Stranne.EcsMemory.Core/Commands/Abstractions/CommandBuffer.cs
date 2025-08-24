namespace Stranne.EcsMemory.Core.Commands.Abstractions;
internal class CommandBuffer : ICommandBuffer
{
    private readonly Queue<GameCommand> _buffer = new();

    public void Enqueue(GameCommand command) => _buffer.Enqueue(command);

    public bool TryDequeue(out GameCommand command) => _buffer.TryDequeue(out command!);
}
