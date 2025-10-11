using System.Diagnostics.CodeAnalysis;
using Stranne.EcsMemory.Core.Commands;
using Stranne.EcsMemory.Core.Commands.Base;

namespace Stranne.EcsMemory.Core.Tests.Commands.Base;
internal sealed class GameCommandQueueTests
{
    [Test]
    [Arguments(0, 0, true, "ValidCoordinates_Zero")]
    [Arguments(5, 3, true, "ValidCoordinates_Positive")]
    [Arguments(10, 10, true, "ValidCoordinates_Max")]
    [Arguments(11, 10, false, "InvalidX_ExceedsMax")]
    [Arguments(10, 11, false, "InvalidY_ExceedsMax")]
    [Arguments(-1, 0, false, "InvalidX_Negative")]
    [Arguments(0, -1, false, "InvalidY_Negative")]
    [Arguments(-1, -1, false, "InvalidCoordinates_BothNegative")]
    [Arguments(-5, 3, false, "InvalidX_LargeNegative")]
    [Arguments(3, -5, false, "InvalidY_LargeNegative")]
    [DisplayName($"${nameof(testName)}")]
    [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Used by TUnit DisplayName")]
    public async Task ValidateCommand_FlipCardAt_ReturnsExpectedResult(int x, int y, bool expected, string testName)
    {
        var command = new FlipCardAt(x, y);

        var result = GameCommandQueue.ValidateCommand(command);

        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    [Arguments(4, 3, 1, true, "ValidBoard_EvenCards_12")]
    [Arguments(2, 2, 1, true, "ValidBoard_EvenCards_4")]
    [Arguments(3, 3, 1, false, "InvalidBoard_OddCards_9")]
    [Arguments(0, 4, 1, false, "InvalidBoard_ZeroColumns")]
    [Arguments(4, 0, 1, false, "InvalidBoard_ZeroRows")]
    [Arguments(11, 2, 1, false, "InvalidBoard_ColumnsExceedsMax")]
    [Arguments(2, 11, 1, false, "InvalidBoard_RowsExceedsMax")]
    [DisplayName($"${nameof(testName)}")]
    [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Used by TUnit DisplayName")]
    public async Task ValidateCommand_StartNewGame_ReturnsExpectedResult(int columns, int rows, int seed, bool expected, string testName)
    {
        var command = new StartNewGame(columns, rows, seed);

        var result = GameCommandQueue.ValidateCommand(command);

        await Assert.That(result).IsEqualTo(expected);
    }
}