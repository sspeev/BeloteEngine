using BeloteEngine.Data.Entities.Enums;
using BeloteEngine.Data.Entities.Models;
using BeloteEngine.Services.Rules;
using BeloteEngine.Services.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace BeloteEngine.Unit.Tests.Services;

public class GameServiceUnit
{
    [Fact]
    public void IsGameOver_ShouldReturnFalse_WhenBothTeamsBelowWinningThreshold()
    {
        var service = CreateService();

        var result = service.IsGameOver(150, 120);

        Assert.False(result);
    }

    [Fact]
    public void IsGameOver_ShouldReturnFalse_WhenScoresAreEqualAboveWinningThreshold()
    {
        var service = CreateService();

        var result = service.IsGameOver(151, 151);

        Assert.False(result);
    }

    [Fact]
    public void IsGameOver_ShouldReturnTrue_WhenAtLeastOneTeamPassedThresholdAndScoresDiffer()
    {
        var service = CreateService();

        var result = service.IsGameOver(151, 140);

        Assert.True(result);
    }

    private static GameService CreateService()
    {
        return new GameService(
            NullLogger<GameService>.Instance,
            new StubTrickEvaluator(),
            new StubPlayValidator(),
            new StubScoreCalculator());
    }
}

internal sealed class StubTrickEvaluator : ITrickEvaluator
{
    public Player DetermineWinner(Trick trick, Announces trump) => throw new NotSupportedException();
}

internal sealed class StubPlayValidator : IPlayValidator
{
    public bool IsValidPlay(Card card, Player player, Trick currentTrick, Announces trump) => true;

    public List<Card> GetPlayableCards(Player player, Trick currentTrick, Announces trump) => [.. player.Hand];
}

internal sealed class StubScoreCalculator : IScoreCalculator
{
    public (int Team1Score, int Team2Score, bool IsHanging) CalculateRoundScore(
        Round round,
        Team[] teams,
        int pendingPoints) => throw new NotSupportedException();
}
