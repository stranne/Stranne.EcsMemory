namespace Stranne.EcsMemory.Core.Commands.Base;
/// <summary>
/// Marker interface for commands that can be executed immediately without waiting for the next update cycle.
/// Use this for deterministic operations that don't depend on timing or game state synchronization.
/// </summary>
internal interface IImmediateCommand
{
}
