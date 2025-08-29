using Stranne.EcsMemory.Core.Systems;
using Stranne.EcsMemory.Core.Tests.Common;

namespace Stranne.EcsMemory.Core.Tests.Systems;
internal sealed class RenderSystemTest
{
    [Test]
    public async Task RenderQuery_SortsDeterministically()
    {
        using var world = TestWorldFactory.Create();
        var cards = new List<(int id, int x, int y)>
        {
            new(0, 1, 3),
            new(1, 0, 0),
            new(5, 3, 1),
            new(3, 1, 1),
            new(4, 0, 1),
            new(2, 2, 2),
        };
        foreach (var (id, x, y) in cards)
            _ = world.CreateCard(cardId: id, x: x, y: y);
        using var sut = new RenderSystem(world);

        sut.Update(0);

        var renderedCards = sut.RenderModel.Cards;
        var expectedOrder = new[] { 1, 4, 3, 5, 2, 0 };
        await Assert.That(renderedCards.Select(c => c.Id).SequenceEqual(expectedOrder)).IsTrue();
    }

    [Test]
    [MatrixDataSource]
    public async Task RenderQuery_ProjectsFlags(bool isLocked, bool isWon, [MatrixMethod<RenderSystemTest>(nameof(Moves))] int moves)
    {
        using var world = TestWorldFactory.Create(isLocked: isLocked, isWon: isWon, moves: moves);
        using var sut = new RenderSystem(world);

        sut.Update(0);

        var renderModel = sut.RenderModel;
        using (Assert.Multiple())
        {
            await Assert.That(renderModel.IsLocked).IsEqualTo(isLocked);
            await Assert.That(renderModel.IsWon).IsEqualTo(isWon);
            await Assert.That(renderModel.Moves).IsEqualTo(moves);
        }
    }

    [Test]
    [Arguments(false, false, false)]
    [Arguments(false, true, true)]
    [Arguments(true, false, true)]
    [Arguments(true, true, true)]
    public async Task RenderQuery_FacePolicy(bool isRevealed, bool isMatched, bool expectedIsFacedUp)
    {
        using var world = TestWorldFactory.Create();
        _ = world.CreateCard(pairKey: 1, revealed: isRevealed, matched: isMatched);
        using var sut = new RenderSystem(world);

        sut.Update(0);

        var renderedCard = sut.RenderModel.Cards.Single();
        await Assert.That(renderedCard.IsFacedUp).IsEqualTo(expectedIsFacedUp);
        if (expectedIsFacedUp)
            await Assert.That(renderedCard.PairKey).IsEqualTo(1);
        else
            await Assert.That(renderedCard.PairKey).IsNull();
    }

    [Test]
    public async Task RenderQuery_BoardInfo()
    {
        const int columns = 4;
        const int rows = 3;
        const int totalCards = 12;
        const int matchedCount = 4;

        using var world = TestWorldFactory.Create(columns, rows, totalCards: totalCards, matchedCount: matchedCount);
        using var sut = new RenderSystem(world);

        sut.Update(0);

        var boardInfo = sut.RenderModel.Board;
        using (Assert.Multiple())
        {
            await Assert.That(boardInfo.Columns).IsEqualTo(columns);
            await Assert.That(boardInfo.Rows).IsEqualTo(rows);
            await Assert.That(boardInfo.TotalCards).IsEqualTo(totalCards);
            await Assert.That(boardInfo.MatchedCards).IsEqualTo(matchedCount);
        }
    }

    private static IEnumerable<int> Moves()
    {
        yield return 0;
        yield return 5;
        yield return 42;
    }
}
