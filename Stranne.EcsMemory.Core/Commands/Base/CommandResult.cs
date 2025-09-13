namespace Stranne.EcsMemory.Core.Commands.Base;
/// <summary>
/// Result of command processing
/// </summary>
internal enum CommandResult
{
    Success,
    Failed,
    Skipped,
    Deferred
}