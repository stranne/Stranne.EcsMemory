using Arch.Core;
using Microsoft.Extensions.Logging;
using Stranne.EcsMemory.Contracts;
using Stranne.EcsMemory.Core.Commands.Handlers;

namespace Stranne.EcsMemory.Core.Commands.Base;
internal class GameCommandQueue(GameConfiguration gameConfiguration, ILogger<GameCommandQueue> logger) : IGameCommandQueue
{
    private const int MaxBoardDimension = 10;
    private readonly Queue<GameCommand> _buffer = new();

    public CommandResult ProcessCommand(GameCommand command, World world)
    {
        if (!ValidateCommand(command))
        {
            logger.LogWarning("Command validation failed: {CommandType}", command.GetType().Name);
            return CommandResult.Failed;
        }

        if (command is IImmediateCommand)
        {
            return ExecuteCommand(command, world);
        }

        _buffer.Enqueue(command);
        return CommandResult.Deferred;
    }

    public bool TryDequeue(out GameCommand command) =>
        _buffer.TryDequeue(out command!);

    internal static bool ValidateCommand(GameCommand command)
    {
        return command switch
        {
            StartNewGame startGame => startGame is
                                      {
                                          Columns: > 0 and <= MaxBoardDimension,
                                          Rows: > 0 and <= MaxBoardDimension
                                      } &&
                                      startGame.Columns * startGame.Rows % 2 == 0,
            FlipCardAt flipCard => flipCard is { X: >= 0 and <= MaxBoardDimension, Y: >= 0 and <= MaxBoardDimension },
            _ => true
        };
    }

    public CommandResult ExecuteCommand(GameCommand command, World world)
    {
        var commandType = command.GetType().Name;
        logger.LogDebug("Executing command: {CommandType}", commandType);
        
        try
        {
            var result = command switch
            {
                StartNewGame startNewGame => StartNewGameHandler.Execute(startNewGame, world, gameConfiguration),
                FlipCardAt flipCardAt => FlipCardAtHandler.Execute(flipCardAt, world, logger),
                _ => CommandResult.Failed
            };
            
            switch (result)
            {
                case CommandResult.Success:
                    logger.LogDebug("Command executed successfully: {CommandType}", commandType);
                    break;
                case CommandResult.Skipped:
                    logger.LogDebug("Command skipped: {CommandType}", commandType);
                    break;
                case CommandResult.Failed:
                    logger.LogWarning("Command execution failed: {CommandType}", commandType);
                    break;
            }
            
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Command execution threw exception: {CommandType}", commandType);
            return CommandResult.Failed;
        }
    }
}
