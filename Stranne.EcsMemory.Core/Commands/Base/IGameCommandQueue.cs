using Arch.Core;

namespace Stranne.EcsMemory.Core.Commands.Base;
internal interface IGameCommandQueue
{
    /// <summary>
    /// Process a command, either immediately if it implements <see cref="IImmediateCommand"/>, or enqueue for later processing
    /// </summary>
    CommandResult ProcessCommand(GameCommand command, World world);
    
    /// <summary>
    /// Execute a command immediately (used by both immediate processing and deferred processing)
    /// </summary>
    CommandResult ExecuteCommand(GameCommand command, World world);
    
    /// <summary>
    /// Try to dequeue the next command for processing
    /// </summary>
    bool TryDequeue(out GameCommand command);
}
