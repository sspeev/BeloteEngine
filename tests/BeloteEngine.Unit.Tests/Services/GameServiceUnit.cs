using BeloteEngine.Data.Entities.Enums;
using BeloteEngine.Data.Entities.Models;
using BeloteEngine.Services.Rules;
using BeloteEngine.Services.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace BeloteEngine.Unit.Tests.Services;

public class GameServiceUnit
{
    // ── IsGameOver ──────────────────────────────────────────────────────

    [Fact]
    public void IsGameOver_ShouldReturnFalse_WhenBothTeamsBelowWinningThreshold()
    {
        //Arrange
        var service = CreateService();

        //Act
        var result = service.IsGameOver(150, 120);

        //Assert
        Assert.False(result);
    }

    [Fact]
    public void IsGameOver_ShouldReturnFalse_WhenScoresAreEqualAboveWinningThreshold()
    {
        //Arrange
        var service = CreateService();

        //Act
        var result = service.IsGameOver(151, 151);

        //Assert
        Assert.False(result);
    }

    [Fact]
    public void IsGameOver_ShouldReturnTrue_WhenAtLeastOneTeamPassedThresholdAndScoresDiffer()
    {
        //Arrange
        var service = CreateService();

        //Act
        var result = service.IsGameOver(151, 140);

        //Assert
        Assert.True(result);
    }

    [Fact]
    public void IsGameOver_ShouldReturnFalse_WhenBothTeamsAtZero()
    {
        //Arrange
        var service = CreateService();

        //Act
        var result = service.IsGameOver(0, 0);

        //Assert
        Assert.False(result);
    }

    [Fact]
    public void IsGameOver_ShouldReturnTrue_WhenOneTeamAt151AndOtherBelow()
    {
        //Arrange
        var service = CreateService();

        //Act
        var result = service.IsGameOver(100, 151);

        //Assert
        Assert.True(result);
    }

    // ── GameInitializer ────────────────────────────────────────────────

    [Fact]
    public void GameInitializer_ShouldCreateTeamsAndDeck_WhenLobbyHasFourPlayers()
    {
        //Arrange
        var service = CreateService();
        var lobby = CreateLobbyWithPlayers();

        //Act
        service.GameInitializer(lobby);

        //Assert
        Assert.NotNull(lobby.Game);
        Assert.NotNull(lobby.Game.Deck);
        Assert.Equal(2, lobby.Game.Teams.Length);
        Assert.True(lobby.GameStarted);
    }

    [Fact]
    public void GameInitializer_ShouldAssignAlternatingPlayersToTeams()
    {
        //Arrange
        var service = CreateService();
        var lobby = CreateLobbyWithPlayers();

        //Act
        service.GameInitializer(lobby);

        //Assert
        Assert.Equal("P0", lobby.Game.Teams[0].Players[0].Name);
        Assert.Equal("P2", lobby.Game.Teams[0].Players[1].Name);
        Assert.Equal("P1", lobby.Game.Teams[1].Players[0].Name);
        Assert.Equal("P3", lobby.Game.Teams[1].Players[1].Name);
    }

    [Fact]
    public void GameInitializer_ShouldSetGameStartedOnLobby()
    {
        //Arrange
        var service = CreateService();
        var lobby = CreateLobbyWithPlayers();

        //Act
        service.GameInitializer(lobby);

        //Assert
        Assert.True(lobby.GameStarted);
    }

    // ── InitialPhase ───────────────────────────────────────────────────

    [Fact]
    public void InitialPhase_ShouldShuffleDeckAndSetSplitter_WhenCalled()
    {
        //Arrange
        var service = CreateService();
        var lobby = CreateInitializedLobby(service);

        //Act
        service.InitialPhase(lobby);

        //Assert
        Assert.NotNull(lobby.Game.CurrentPlayer);
        Assert.Equal("splitting", lobby.GamePhase);
        Assert.Equal(4, lobby.Game.SortedPlayers.Count);
    }

    [Fact]
    public void InitialPhase_ShouldThrow_WhenLobbyIsNull()
    {
        //Arrange
        var service = CreateService();

        //Act & Assert
        Assert.Throws<ArgumentNullException>(() => service.InitialPhase(null!));
    }

    [Fact]
    public void InitialPhase_ShouldThrow_WhenTeamsAreInvalid()
    {
        //Arrange
        var service = CreateService();
        var lobby = new Lobby { Game = new Game() };

        //Act & Assert
        Assert.ThrowsAny<Exception>(() => service.InitialPhase(lobby));
    }

    // ── GetPlayerCards ─────────────────────────────────────────────────

    [Fact]
    public void GetPlayerCards_ShouldDeal8Cards_WhenDeckHasEnoughCards()
    {
        //Arrange
        var service = CreateService();
        var player = new Player { Name = "Test" };
        var deck = new Deck();

        //Act
        service.GetPlayerCards(player, deck);

        //Assert
        Assert.Equal(8, player.Hand.Count);
        Assert.Equal(24, deck.Cards.Count); // 32 - 8
    }

    [Fact]
    public void GetPlayerCards_ShouldDealPartialHand_WhenDeckHasFewerThan8Cards()
    {
        //Arrange
        var service = CreateService();
        var player = new Player { Name = "Test" };
        var deck = new Deck();
        // Pop all but 3 cards
        while (deck.Cards.Count > 3) deck.Cards.Pop();

        //Act
        service.GetPlayerCards(player, deck);

        //Assert
        Assert.Equal(3, player.Hand.Count);
        Assert.Empty(deck.Cards);
    }

    // ── Player queue methods ───────────────────────────────────────────

    [Fact]
    public void RotatePlayerQueue_ShouldCyclePlayersInOrder()
    {
        //Arrange
        var service = CreateService();
        var players = new Queue<Player>();
        var p1 = new Player { Name = "P1" };
        var p2 = new Player { Name = "P2" };
        var p3 = new Player { Name = "P3" };
        var p4 = new Player { Name = "P4" };
        players.Enqueue(p1);
        players.Enqueue(p2);
        players.Enqueue(p3);
        players.Enqueue(p4);

        //Act
        var first = service.GetNextPlayer(players);
        var second = service.GetNextPlayer(players);
        var third = service.GetNextPlayer(players);
        var fourth = service.GetNextPlayer(players);
        var backToFirst = service.GetNextPlayer(players);

        //Assert
        Assert.Same(p1, first);
        Assert.Same(p2, second);
        Assert.Same(p3, third);
        Assert.Same(p4, fourth);
        Assert.Same(p1, backToFirst);
    }

    [Fact]
    public void GetNextBidder_ShouldReturnNextPlayerInQueue()
    {
        //Arrange
        var service = CreateService();
        var lobby = CreateBiddingLobby(service);
        var currentPlayer = lobby.Game.CurrentPlayer;

        //Act
        var nextBidder = service.GetNextBidder(lobby);
        var secondBidder = service.GetNextBidder(lobby);

        //Assert
        Assert.NotNull(nextBidder);
        Assert.NotEqual(currentPlayer.Name, nextBidder.Name);
        Assert.NotEqual(nextBidder.Name, secondBidder.Name);
    }

    // ── MakeBid ────────────────────────────────────────────────────────

    [Fact]
    public void MakeBid_ShouldSetFirstAnnounce_WhenNoPriorBidExists()
    {
        //Arrange
        var service = CreateService();
        var lobby = CreateBiddingLobby(service);
        var playerName = lobby.Game.CurrentPlayer.Name;

        //Act
        service.MakeBid(playerName, "Clubs", lobby);

        //Assert
        Assert.Equal(Announces.Clubs, lobby.Game.CurrentAnnounce);
        Assert.Equal(playerName, lobby.Game.ContractPlayer!.Name);
    }

    [Fact]
    public void MakeBid_ShouldRejectLowerBid_WhenCurrentAnnounceIsHigher()
    {
        //Arrange
        var service = CreateService();
        var lobby = CreateBiddingLobby(service);
        var p1 = lobby.Game.CurrentPlayer.Name;
        service.MakeBid(p1, "Hearts", lobby);
        var p2 = lobby.Game.CurrentPlayer.Name;

        //Act & Assert
        Assert.Throws<InvalidOperationException>(
            () => service.MakeBid(p2, "Clubs", lobby));
    }

    [Fact]
    public void MakeBid_ShouldAcceptHigherBid_AndResetPassCounter()
    {
        //Arrange
        var service = CreateService();
        var lobby = CreateBiddingLobby(service);
        var p1 = lobby.Game.CurrentPlayer.Name;
        service.MakeBid(p1, "Clubs", lobby);
        var p2 = lobby.Game.CurrentPlayer.Name;
        service.MakeBid(p2, "Pass", lobby);
        Assert.Equal(1, lobby.Game.PassCounter);

        //Act
        var p3 = lobby.Game.CurrentPlayer.Name;
        service.MakeBid(p3, "Spades", lobby);

        //Assert
        Assert.Equal(Announces.Spades, lobby.Game.CurrentAnnounce);
        Assert.Equal(0, lobby.Game.PassCounter);
    }

    [Fact]
    public void MakeBid_ShouldIncrementPassCounter_WhenPlayerPasses()
    {
        //Arrange
        var service = CreateService();
        var lobby = CreateBiddingLobby(service);
        var playerName = lobby.Game.CurrentPlayer.Name;

        //Act
        service.MakeBid(playerName, "Pass", lobby);

        //Assert
        Assert.Equal(1, lobby.Game.PassCounter);
    }

    [Fact]
    public void MakeBid_ShouldThrow_WhenPlayerNotFound()
    {
        //Arrange
        var service = CreateService();
        var lobby = CreateBiddingLobby(service);

        //Act & Assert
        Assert.Throws<ArgumentException>(
            () => service.MakeBid("NonExistent", "Clubs", lobby));
    }

    [Fact]
    public void MakeBid_ShouldThrow_WhenBidStringIsInvalid()
    {
        //Arrange
        var service = CreateService();
        var lobby = CreateBiddingLobby(service);
        var playerName = lobby.Game.CurrentPlayer.Name;

        //Act & Assert
        Assert.Throws<ArgumentException>(
            () => service.MakeBid(playerName, "InvalidBid", lobby));
    }

    [Fact]
    public void MakeBid_ShouldAllowDouble_WhenOpponentHasActiveBid()
    {
        //Arrange
        var service = CreateService();
        var lobby = CreateBiddingLobby(service);
        // P0 (team1) bids Clubs
        var p0 = lobby.Game.CurrentPlayer.Name;
        service.MakeBid(p0, "Clubs", lobby);
        // P1 (team2) doubles
        var p1 = lobby.Game.CurrentPlayer.Name;

        //Act
        service.MakeBid(p1, "Double", lobby);

        //Assert
        Assert.True(lobby.Game.IsDoubled);
    }

    [Fact]
    public void MakeBid_ShouldRejectDouble_WhenNoBidExists()
    {
        //Arrange
        var service = CreateService();
        var lobby = CreateBiddingLobby(service);
        var playerName = lobby.Game.CurrentPlayer.Name;

        //Act & Assert
        Assert.Throws<InvalidOperationException>(
            () => service.MakeBid(playerName, "Double", lobby));
    }

    [Fact]
    public void MakeBid_ShouldRejectDouble_WhenSameTeamBid()
    {
        //Arrange
        var service = CreateService();
        var lobby = CreateBiddingLobby(service);
        // P0 (team1) bids
        var p0 = lobby.Game.CurrentPlayer.Name;
        service.MakeBid(p0, "Clubs", lobby);
        // P1 (team2) passes
        var p1 = lobby.Game.CurrentPlayer.Name;
        service.MakeBid(p1, "Pass", lobby);
        // P2 (team1) tries to double own team's bid

        var p2 = lobby.Game.CurrentPlayer.Name;

        //Act & Assert
        Assert.Throws<InvalidOperationException>(
            () => service.MakeBid(p2, "Double", lobby));
    }

    [Fact]
    public void MakeBid_ShouldRejectDouble_WhenAlreadyDoubled()
    {
        //Arrange
        var service = CreateService();
        var lobby = CreateBiddingLobby(service);
        var p0 = lobby.Game.CurrentPlayer.Name;
        service.MakeBid(p0, "Clubs", lobby);
        var p1 = lobby.Game.CurrentPlayer.Name;
        service.MakeBid(p1, "Double", lobby);
        // P2 passes
        var p2 = lobby.Game.CurrentPlayer.Name;
        service.MakeBid(p2, "Pass", lobby);
        // P3 (team2) tries to double again
        var p3 = lobby.Game.CurrentPlayer.Name;

        //Act & Assert
        Assert.Throws<InvalidOperationException>(
            () => service.MakeBid(p3, "Double", lobby));
    }

    [Fact]
    public void MakeBid_ShouldAllowReDouble_WhenOwnTeamBidIsDoubled()
    {
        //Arrange
        var service = CreateService();
        var lobby = CreateBiddingLobby(service);
        // P0 (team1) bids
        var p0 = lobby.Game.CurrentPlayer.Name;
        service.MakeBid(p0, "Clubs", lobby);
        // P1 (team2) doubles
        var p1 = lobby.Game.CurrentPlayer.Name;
        service.MakeBid(p1, "Double", lobby);
        // P2 (team1) redoubles own team's doubled bid
        var p2 = lobby.Game.CurrentPlayer.Name;

        //Act
        service.MakeBid(p2, "ReDouble", lobby);

        //Assert
        Assert.True(lobby.Game.IsReDoubled);
    }

    [Fact]
    public void MakeBid_ShouldRejectReDouble_WhenNotDoubled()
    {
        //Arrange
        var service = CreateService();
        var lobby = CreateBiddingLobby(service);
        var p0 = lobby.Game.CurrentPlayer.Name;
        service.MakeBid(p0, "Clubs", lobby);
        var p1 = lobby.Game.CurrentPlayer.Name;

        //Act & Assert
        Assert.Throws<InvalidOperationException>(
            () => service.MakeBid(p1, "ReDouble", lobby));
    }

    [Fact]
    public void MakeBid_ShouldRejectReDouble_WhenAlreadyReDoubled()
    {
        //Arrange
        var service = CreateService();
        var lobby = CreateBiddingLobby(service);
        var p0 = lobby.Game.CurrentPlayer.Name;
        service.MakeBid(p0, "Clubs", lobby);
        var p1 = lobby.Game.CurrentPlayer.Name;
        service.MakeBid(p1, "Double", lobby);
        var p2 = lobby.Game.CurrentPlayer.Name;
        service.MakeBid(p2, "ReDouble", lobby);
        // P3 passes
        var p3 = lobby.Game.CurrentPlayer.Name;
        service.MakeBid(p3, "Pass", lobby);
        // Back to P0 (team1) — try redouble again
        var p0Again = lobby.Game.CurrentPlayer.Name;

        //Act & Assert
        Assert.Throws<InvalidOperationException>(
            () => service.MakeBid(p0Again, "ReDouble", lobby));
    }

    [Fact]
    public void MakeBid_ShouldRejectReDouble_WhenOpponentTeam()
    {
        //Arrange
        var service = CreateService();
        var lobby = CreateBiddingLobby(service);
        var p0 = lobby.Game.CurrentPlayer.Name;
        service.MakeBid(p0, "Clubs", lobby);
        var p1 = lobby.Game.CurrentPlayer.Name;
        service.MakeBid(p1, "Double", lobby);
        // P2 passes
        var p2 = lobby.Game.CurrentPlayer.Name;
        service.MakeBid(p2, "Pass", lobby);
        // P3 (team2 — opponent of contract team) tries to redouble
        var p3 = lobby.Game.CurrentPlayer.Name;

        //Act & Assert
        Assert.Throws<InvalidOperationException>(
            () => service.MakeBid(p3, "ReDouble", lobby));
    }

    // ── GameReset ──────────────────────────────────────────────────────

    [Fact]
    public void GameReset_ShouldClearHandsAndResetAnnounce_WhenCalled()
    {
        //Arrange
        var service = CreateService();
        var lobby = CreateBiddingLobby(service);
        var p0 = lobby.Game.CurrentPlayer.Name;
        service.MakeBid(p0, "Clubs", lobby);

        //Act
        var game = service.GameReset(lobby);

        //Assert
        Assert.Equal(Announces.None, game.CurrentAnnounce);
        Assert.Equal(0, game.PassCounter);
        Assert.Null(game.CurrentRound);
        Assert.Null(game.CurrentTrick);
        Assert.All(lobby.ConnectedPlayers, p => Assert.Empty(p.Hand));
    }

    [Fact]
    public void GameReset_ShouldSetPhaseToSplitting()
    {
        //Arrange
        var service = CreateService();
        var lobby = CreateBiddingLobby(service);

        //Act
        service.GameReset(lobby);

        //Assert
        Assert.Equal("splitting", lobby.GamePhase);
    }

    [Fact]
    public void GameReset_ShouldRotateRoundQueue_OnSubsequentRounds()
    {
        //Arrange
        var service = CreateService();
        var lobby = CreateBiddingLobby(service);
        var firstSplitter = lobby.Game.RoundQueue.Peek().Name;

        //Act
        service.GameReset(lobby);
        var secondRoundFront = lobby.Game.RoundQueue.Peek().Name;

        //Assert
        Assert.NotEqual(firstSplitter, secondRoundFront);
    }

    // ── Helpers ─────────────────────────────────────────────────────────

    private static GameService CreateService()
    {
        return new GameService(
            NullLogger<GameService>.Instance,
            new StubTrickEvaluator(),
            new StubPlayValidator(),
            new StubScoreCalculator());
    }

    private static Lobby CreateLobbyWithPlayers()
    {
        var lobby = new Lobby { Name = "Test" };
        for (int i = 0; i < 4; i++)
            lobby.ConnectedPlayers.Add(new Player { Name = $"P{i}" });
        return lobby;
    }

    private static Lobby CreateInitializedLobby(GameService service)
    {
        var lobby = CreateLobbyWithPlayers();
        service.GameInitializer(lobby);
        return lobby;
    }

    private static Lobby CreateBiddingLobby(GameService service)
    {
        var lobby = CreateInitializedLobby(service);
        service.InitialPhase(lobby);
        return lobby;
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
