using BeloteEngine.Data.Entities.Enums;
using BeloteEngine.Data.Entities.Models;
using BeloteEngine.Services.Rules;

namespace BeloteEngine.Unit.Tests.Services;

public class TrickEvaluatorUnit
{
    [Fact]
    public void DetermineWinner_ShouldReturnHighestLeadSuit_WhenNoTrump()
    {
        //Arrange
        var evaluator = new TrickEvaluator();
        var trick = CreateCompleteTrick(
            (Suit.Hearts, "10", 10, 7),
            (Suit.Hearts, "7", 0, 1),
            (Suit.Clubs, "A", 11, 8),
            (Suit.Hearts, "K", 4, 6));

        //Act
        var winner = evaluator.DetermineWinner(trick, Announces.NoTrump);

        //Assert
        Assert.Equal("P0", winner.Name);
    }

    [Fact]
    public void DetermineWinner_ShouldReturnHighestTrump_WhenTrumpIsPlayed()
    {
        //Arrange
        var evaluator = new TrickEvaluator();
        var trick = CreateCompleteTrick(
            (Suit.Hearts, "A", 11, 8),
            (Suit.Spades, "7", 0, 1),
            (Suit.Spades, "J", 20, 8),
            (Suit.Hearts, "K", 4, 7));

        //Act
        var winner = evaluator.DetermineWinner(trick, Announces.Spades);

        //Assert
        Assert.Equal("P2", winner.Name);
    }

    [Fact]
    public void DetermineWinner_ShouldReturnHighestLeadSuit_WhenNoTrumpPlayed_InSuitGame()
    {
        //Arrange
        var evaluator = new TrickEvaluator();
        var trick = CreateCompleteTrick(
            (Suit.Hearts, "10", 10, 5),
            (Suit.Hearts, "Q", 3, 3),
            (Suit.Diamonds, "A", 11, 8),
            (Suit.Hearts, "A", 11, 6));

        //Act
        var winner = evaluator.DetermineWinner(trick, Announces.Spades);

        //Assert
        Assert.Equal("P3", winner.Name);
    }

    [Fact]
    public void DetermineWinner_ShouldReturnHighestLeadSuit_InAllTrumps()
    {
        //Arrange
        var evaluator = new TrickEvaluator();
        var trick = CreateCompleteTrick(
            (Suit.Clubs, "9", 14, 7),
            (Suit.Clubs, "J", 20, 8),
            (Suit.Hearts, "J", 20, 8),
            (Suit.Clubs, "A", 11, 6));

        //Act
        var winner = evaluator.DetermineWinner(trick, Announces.AllTrumps);

        //Assert
        Assert.Equal("P1", winner.Name);
    }

    [Fact]
    public void DetermineWinner_ShouldThrow_WhenTrickIsIncomplete()
    {
        //Arrange
        var evaluator = new TrickEvaluator();
        var trick = new Trick();
        trick.PlayedCards.Add(new PlayedCard(
            new Player { Name = "P0" },
            new Card(Suit.Hearts, "A", 11, 8)));

        //Act & Assert
        Assert.Throws<InvalidOperationException>(
            () => evaluator.DetermineWinner(trick, Announces.NoTrump));
    }

    [Fact]
    public void DetermineWinner_ShouldIgnoreOffSuitCards_WhenNoTrump()
    {
        //Arrange
        var evaluator = new TrickEvaluator();
        var trick = CreateCompleteTrick(
            (Suit.Hearts, "7", 0, 1),
            (Suit.Clubs, "A", 11, 8),
            (Suit.Diamonds, "A", 11, 8),
            (Suit.Spades, "A", 11, 8));

        //Act
        var winner = evaluator.DetermineWinner(trick, Announces.NoTrump);

        //Assert
        Assert.Equal("P0", winner.Name);
    }

    private static Trick CreateCompleteTrick(
        (Suit suit, string rank, int value, int power) c0,
        (Suit suit, string rank, int value, int power) c1,
        (Suit suit, string rank, int value, int power) c2,
        (Suit suit, string rank, int value, int power) c3)
    {
        var trick = new Trick();
        trick.PlayedCards.Add(new PlayedCard(new Player { Name = "P0" }, new Card(c0.suit, c0.rank, c0.value, c0.power)));
        trick.PlayedCards.Add(new PlayedCard(new Player { Name = "P1" }, new Card(c1.suit, c1.rank, c1.value, c1.power)));
        trick.PlayedCards.Add(new PlayedCard(new Player { Name = "P2" }, new Card(c2.suit, c2.rank, c2.value, c2.power)));
        trick.PlayedCards.Add(new PlayedCard(new Player { Name = "P3" }, new Card(c3.suit, c3.rank, c3.value, c3.power)));
        return trick;
    }
}
