using BeloteEngine.Data.Entities.Enums;
using BeloteEngine.Data.Entities.Models;
using BeloteEngine.Services.Rules;

namespace BeloteEngine.Unit.Tests.Services;

public class PlayValidatorUnit
{
    // ── First card ─────────────────────────────────────────────────────

    [Fact]
    public void GetPlayableCards_ShouldReturnEntireHand_WhenFirstCardInTrick()
    {
        //Arrange
        var validator = new PlayValidator();
        var player = PlayerWithHand(
            (Suit.Hearts, "A", 11, 8),
            (Suit.Clubs, "7", 0, 1),
            (Suit.Diamonds, "K", 4, 7));
        var trick = new Trick();

        //Act
        var playable = validator.GetPlayableCards(player, trick, Announces.Clubs);

        //Assert
        Assert.Equal(3, playable.Count);
    }

    [Fact]
    public void IsValidPlay_ShouldReturnTrue_WhenCardIsPlayable()
    {
        //Arrange
        var validator = new PlayValidator();
        var player = PlayerWithHand((Suit.Hearts, "A", 11, 8));
        var trick = new Trick();

        //Act
        var valid = validator.IsValidPlay(
            new Card(Suit.Hearts, "A", 11, 8), player, trick, Announces.NoTrump);

        //Assert
        Assert.True(valid);
    }

    [Fact]
    public void IsValidPlay_ShouldReturnFalse_WhenCardNotPlayable()
    {
        //Arrange
        var validator = new PlayValidator();
        var player = PlayerWithHand(
            (Suit.Hearts, "A", 11, 8),
            (Suit.Hearts, "K", 4, 7));
        var trick = TrickWithLead(Suit.Hearts, "7", 0, 1);

        //Act — try to play a Clubs card that's not in hand
        var valid = validator.IsValidPlay(
            new Card(Suit.Clubs, "A", 11, 8), player, trick, Announces.NoTrump);

        //Assert
        Assert.False(valid);
    }

    // ── NoTrump ────────────────────────────────────────────────────────

    [Fact]
    public void GetPlayableCards_NoTrump_ShouldReturnSameSuitOnly_WhenPlayerHasLeadSuit()
    {
        //Arrange
        var validator = new PlayValidator();
        var player = PlayerWithHand(
            (Suit.Hearts, "A", 11, 8),
            (Suit.Hearts, "K", 4, 6),
            (Suit.Clubs, "7", 0, 1));
        var trick = TrickWithLead(Suit.Hearts, "10", 10, 7);

        //Act
        var playable = validator.GetPlayableCards(player, trick, Announces.NoTrump);

        //Assert
        Assert.Equal(2, playable.Count);
        Assert.All(playable, c => Assert.Equal(Suit.Hearts, c.Suit));
    }

    [Fact]
    public void GetPlayableCards_NoTrump_ShouldReturnFullHand_WhenPlayerCannotFollowSuit()
    {
        //Arrange
        var validator = new PlayValidator();
        var player = PlayerWithHand(
            (Suit.Clubs, "A", 11, 8),
            (Suit.Diamonds, "7", 0, 1));
        var trick = TrickWithLead(Suit.Hearts, "10", 10, 7);

        //Act
        var playable = validator.GetPlayableCards(player, trick, Announces.NoTrump);

        //Assert
        Assert.Equal(2, playable.Count);
    }

    // ── AllTrumps ──────────────────────────────────────────────────────

    [Fact]
    public void GetPlayableCards_AllTrumps_ShouldRequireHigherCard_WhenFollowingSuit()
    {
        //Arrange
        var validator = new PlayValidator();
        var player = PlayerWithHand(
            (Suit.Hearts, "J", 20, 8),  // higher
            (Suit.Hearts, "7", 0, 1),   // lower
            (Suit.Clubs, "A", 11, 6));
        var trick = TrickWithLead(Suit.Hearts, "9", 14, 7);

        //Act
        var playable = validator.GetPlayableCards(player, trick, Announces.AllTrumps);

        //Assert
        Assert.Single(playable);
        Assert.Equal("J", playable[0].Rank);
    }

    [Fact]
    public void GetPlayableCards_AllTrumps_ShouldAllowLowerCard_WhenNoHigherExists()
    {
        //Arrange
        var validator = new PlayValidator();
        var player = PlayerWithHand(
            (Suit.Hearts, "7", 0, 1),
            (Suit.Hearts, "8", 0, 2),
            (Suit.Clubs, "A", 11, 6));
        var trick = TrickWithLead(Suit.Hearts, "J", 20, 8);

        //Act
        var playable = validator.GetPlayableCards(player, trick, Announces.AllTrumps);

        //Assert
        Assert.Equal(2, playable.Count);
        Assert.All(playable, c => Assert.Equal(Suit.Hearts, c.Suit));
    }

    [Fact]
    public void GetPlayableCards_AllTrumps_ShouldReturnFullHand_WhenCannotFollowSuit()
    {
        //Arrange
        var validator = new PlayValidator();
        var player = PlayerWithHand(
            (Suit.Clubs, "A", 11, 6),
            (Suit.Diamonds, "K", 4, 4));
        var trick = TrickWithLead(Suit.Hearts, "10", 10, 5);

        //Act
        var playable = validator.GetPlayableCards(player, trick, Announces.AllTrumps);

        //Assert
        Assert.Equal(2, playable.Count);
    }

    // ── Suit Trump ─────────────────────────────────────────────────────

    [Fact]
    public void GetPlayableCards_SuitTrump_ShouldFollowLeadSuit_WhenPossible()
    {
        //Arrange
        var validator = new PlayValidator();
        var player = PlayerWithHand(
            (Suit.Hearts, "A", 11, 8),
            (Suit.Hearts, "K", 4, 7),
            (Suit.Spades, "J", 20, 8));
        var trick = TrickWithLead(Suit.Hearts, "10", 10, 5);

        //Act
        var playable = validator.GetPlayableCards(player, trick, Announces.Spades);

        //Assert
        Assert.Equal(2, playable.Count);
        Assert.All(playable, c => Assert.Equal(Suit.Hearts, c.Suit));
    }

    [Fact]
    public void GetPlayableCards_SuitTrump_ShouldRequireHigherTrump_WhenLeadIsTrump()
    {
        //Arrange
        var validator = new PlayValidator();
        var player = PlayerWithHand(
            (Suit.Spades, "J", 20, 8),   // higher trump
            (Suit.Spades, "7", 0, 1),    // lower trump
            (Suit.Hearts, "A", 11, 8));
        var trick = TrickWithLead(Suit.Spades, "9", 14, 7);

        //Act
        var playable = validator.GetPlayableCards(player, trick, Announces.Spades);

        //Assert
        Assert.Single(playable);
        Assert.Equal("J", playable[0].Rank);
    }

    [Fact]
    public void GetPlayableCards_SuitTrump_ShouldRequireTrump_WhenCannotFollowSuit()
    {
        //Arrange
        var validator = new PlayValidator();
        var player = PlayerWithHand(
            (Suit.Spades, "J", 20, 8),
            (Suit.Spades, "9", 14, 7),
            (Suit.Diamonds, "A", 11, 8));
        var trick = TrickWithLead(Suit.Hearts, "A", 11, 8);

        //Act
        var playable = validator.GetPlayableCards(player, trick, Announces.Spades);

        //Assert
        Assert.Equal(2, playable.Count);
        Assert.All(playable, c => Assert.Equal(Suit.Spades, c.Suit));
    }

    [Fact]
    public void GetPlayableCards_SuitTrump_ShouldReturnFullHand_WhenNoSuitOrTrump()
    {
        //Arrange
        var validator = new PlayValidator();
        var player = PlayerWithHand(
            (Suit.Diamonds, "A", 11, 8),
            (Suit.Clubs, "K", 4, 7));
        var trick = TrickWithLead(Suit.Hearts, "10", 10, 5);

        //Act
        var playable = validator.GetPlayableCards(player, trick, Announces.Spades);

        //Assert
        Assert.Equal(2, playable.Count);
    }

    // ── Helpers ─────────────────────────────────────────────────────────

    private static Player PlayerWithHand(
        params (Suit suit, string rank, int value, int power)[] cards)
    {
        var player = new Player { Name = "TestPlayer" };
        foreach (var c in cards)
            player.Hand.Add(new Card(c.suit, c.rank, c.value, c.power));
        return player;
    }

    private static Trick TrickWithLead(Suit suit, string rank, int value, int power)
    {
        var trick = new Trick();
        trick.PlayedCards.Add(new PlayedCard(
            new Player { Name = "Leader" },
            new Card(suit, rank, value, power)));
        return trick;
    }
}
