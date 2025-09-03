namespace Stranne.EcsMemory.Contracts;

/// <summary>
/// Immutable configuration settings that control game behavior.
/// These settings are not part of the saved game state and remain constant during gameplay.
/// </summary>
public sealed record GameConfiguration(
    /// <summary>
    /// Number of update cycles to wait before evaluating card matches after two cards are revealed.
    /// This delay allows for visual feedback before cards are either matched or hidden again.
    /// Must be greater than 0. Default is 30 updates (approximately 0.5 seconds at 60 FPS).
    /// </summary>
    int EvaluationDelayUpdates = 30
);