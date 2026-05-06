using BeloteEngine.Data.Entities.Enums;
using BeloteEngine.Data.Entities.Models;
using BeloteEngine.Services.Rules;

namespace BeloteEngine.Unit.Tests.Services;

public class ScoreCalculatorUnit
{
    [Fact]
    public void CalculateRoundScore_ShouldReturnCompletedScore_WhenAnnouncerWins()
    {
        //Arrange
        var calc = new ScoreCalculator();
        var teams = CreateTeams();
        var round = CreateRound(teams, teams[0], Announces.Clubs,
            team1TrickPts: 92, team2TrickPts: 60,
            lastTrickWinner: teams[0].Players[0],
            announcerWinsAll: false, defenderWinsAll: false);

        //Act
        var (t1, t2, hanging) = calc.CalculateRoundScore(round, teams, 0);

        //Assert
        Assert.False(hanging);
        Assert.True(t1 > 0);
        Assert.True(t2 > 0);
    }

    [Fact]
    public void CalculateRoundScore_ShouldReturnSetScore_WhenAnnouncerLoses()
    {
        //Arrange
        var calc = new ScoreCalculator();
        var teams = CreateTeams();
        var round = CreateRound(teams, teams[0], Announces.Clubs,
            team1TrickPts: 50, team2TrickPts: 102,
            lastTrickWinner: teams[1].Players[0],
            announcerWinsAll: false, defenderWinsAll: false);

        //Act
        var (t1, t2, hanging) = calc.CalculateRoundScore(round, teams, 0);

        //Assert
        Assert.False(hanging);
        // Defender gets all points; announcer gets 0
        Assert.True(t2 > 0);
        Assert.Equal(0, t1);
    }

    [Fact]
    public void CalculateRoundScore_ShouldReturnHanging_WhenPointsAreEqual()
    {
        //Arrange
        var calc = new ScoreCalculator();
        var teams = CreateTeams();
        // Both at 76 raw; last trick bonus goes to team1 (announcer) → 86 vs 76
        // Actually for equal we need: ann + bonus == def
        // So: ann=71 + 10 = 81, def=81? No — the last trick bonus is added first.
        // Let's set it so that after last trick bonus they're equal:
        // ann=76, def=76, last trick to defender → ann=76, def=86 → not equal, announcer loses
        // For hanging: ann=81, def=71, last trick to defender → ann=81, def=81 → equal!
        var round = CreateRound(teams, teams[0], Announces.Clubs,
            team1TrickPts: 81, team2TrickPts: 71,
            lastTrickWinner: teams[1].Players[0],
            announcerWinsAll: false, defenderWinsAll: false);

        //Act
        var (t1, t2, hanging) = calc.CalculateRoundScore(round, teams, 0);

        //Assert
        Assert.True(hanging);
        Assert.Equal(0, t1); // Announcer records 0 when hanging
    }

    [Fact]
    public void CalculateRoundScore_ShouldAwardCapot_WhenAnnouncerWinsAllTricks()
    {
        //Arrange
        var calc = new ScoreCalculator();
        var teams = CreateTeams();
        var round = CreateRound(teams, teams[0], Announces.Clubs,
            team1TrickPts: 152, team2TrickPts: 0,
            lastTrickWinner: teams[0].Players[0],
            announcerWinsAll: true, defenderWinsAll: false);

        //Act
        var (t1, t2, hanging) = calc.CalculateRoundScore(round, teams, 0);

        //Assert
        Assert.False(hanging);
        Assert.True(t1 > 0);
        Assert.Equal(0, t2);
    }

    [Fact]
    public void CalculateRoundScore_ShouldAwardCapot_WhenDefenderWinsAllTricks()
    {
        //Arrange
        var calc = new ScoreCalculator();
        var teams = CreateTeams();
        var round = CreateRound(teams, teams[0], Announces.Clubs,
            team1TrickPts: 0, team2TrickPts: 152,
            lastTrickWinner: teams[1].Players[0],
            announcerWinsAll: false, defenderWinsAll: true);

        //Act
        var (t1, t2, hanging) = calc.CalculateRoundScore(round, teams, 0);

        //Assert
        Assert.False(hanging);
        Assert.Equal(0, t1);
        Assert.True(t2 > 0);
    }

    [Fact]
    public void CalculateRoundScore_ShouldDoublePoints_WhenNoTrump()
    {
        //Arrange
        var calc = new ScoreCalculator();
        var teams = CreateTeams();
        var roundNormal = CreateRound(teams, teams[0], Announces.Clubs,
            team1TrickPts: 92, team2TrickPts: 60,
            lastTrickWinner: teams[0].Players[0],
            announcerWinsAll: false, defenderWinsAll: false);
        var roundNoTrump = CreateRound(teams, teams[0], Announces.NoTrump,
            team1TrickPts: 92, team2TrickPts: 60,
            lastTrickWinner: teams[0].Players[0],
            announcerWinsAll: false, defenderWinsAll: false);

        //Act
        var (t1Normal, t2Normal, _) = calc.CalculateRoundScore(roundNormal, teams, 0);
        var (t1NoTrump, t2NoTrump, _) = calc.CalculateRoundScore(roundNoTrump, teams, 0);

        //Assert
        Assert.Equal(t1Normal * 2, t1NoTrump);
        Assert.Equal(t2Normal * 2, t2NoTrump);
    }

    [Fact]
    public void CalculateRoundScore_ShouldDoublePoints_WhenDoubled()
    {
        //Arrange
        var calc = new ScoreCalculator();
        var teams = CreateTeams();
        var roundNormal = CreateRound(teams, teams[0], Announces.Clubs,
            team1TrickPts: 92, team2TrickPts: 60,
            lastTrickWinner: teams[0].Players[0],
            announcerWinsAll: false, defenderWinsAll: false);
        var roundDoubled = CreateRound(teams, teams[0], Announces.Clubs,
            team1TrickPts: 92, team2TrickPts: 60,
            lastTrickWinner: teams[0].Players[0],
            announcerWinsAll: false, defenderWinsAll: false,
            isDoubled: true);

        //Act
        var (t1Normal, _, _) = calc.CalculateRoundScore(roundNormal, teams, 0);
        var (t1Doubled, _, _) = calc.CalculateRoundScore(roundDoubled, teams, 0);

        //Assert
        Assert.Equal(t1Normal * 2, t1Doubled);
    }

    [Fact]
    public void CalculateRoundScore_ShouldQuadruplePoints_WhenReDoubled()
    {
        //Arrange
        var calc = new ScoreCalculator();
        var teams = CreateTeams();
        var roundNormal = CreateRound(teams, teams[0], Announces.Clubs,
            team1TrickPts: 92, team2TrickPts: 60,
            lastTrickWinner: teams[0].Players[0],
            announcerWinsAll: false, defenderWinsAll: false);
        var roundReDoubled = CreateRound(teams, teams[0], Announces.Clubs,
            team1TrickPts: 92, team2TrickPts: 60,
            lastTrickWinner: teams[0].Players[0],
            announcerWinsAll: false, defenderWinsAll: false,
            isDoubled: true, isReDoubled: true);

        //Act
        var (t1Normal, _, _) = calc.CalculateRoundScore(roundNormal, teams, 0);
        var (t1Re, _, _) = calc.CalculateRoundScore(roundReDoubled, teams, 0);

        //Assert
        Assert.Equal(t1Normal * 4, t1Re);
    }

    [Fact]
    public void CalculateRoundScore_ShouldIncludePendingPoints_WhenCompleted()
    {
        //Arrange
        var calc = new ScoreCalculator();
        var teams = CreateTeams();
        var roundWithPending = CreateRound(teams, teams[0], Announces.Clubs,
            team1TrickPts: 92, team2TrickPts: 60,
            lastTrickWinner: teams[0].Players[0],
            announcerWinsAll: false, defenderWinsAll: false);
        var roundNoPending = CreateRound(teams, teams[0], Announces.Clubs,
            team1TrickPts: 92, team2TrickPts: 60,
            lastTrickWinner: teams[0].Players[0],
            announcerWinsAll: false, defenderWinsAll: false);

        //Act
        var (t1With, _, _) = calc.CalculateRoundScore(roundWithPending, teams, 100);
        var (t1Without, _, _) = calc.CalculateRoundScore(roundNoPending, teams, 0);

        //Assert
        Assert.True(t1With > t1Without);
    }

    [Fact]
    public void CalculateRoundScore_ShouldAddLastTrickBonus_ToCorrectTeam()
    {
        //Arrange
        var calc = new ScoreCalculator();
        var teams = CreateTeams();
        var roundAnnWins = CreateRound(teams, teams[0], Announces.Clubs,
            team1TrickPts: 92, team2TrickPts: 60,
            lastTrickWinner: teams[0].Players[0],
            announcerWinsAll: false, defenderWinsAll: false);
        var roundDefWins = CreateRound(teams, teams[0], Announces.Clubs,
            team1TrickPts: 92, team2TrickPts: 60,
            lastTrickWinner: teams[1].Players[0],
            announcerWinsAll: false, defenderWinsAll: false);

        //Act
        var (t1Ann, t2Ann, _) = calc.CalculateRoundScore(roundAnnWins, teams, 0);
        var (t1Def, t2Def, _) = calc.CalculateRoundScore(roundDefWins, teams, 0);

        //Assert — last trick bonus shifts 10 raw points between teams
        Assert.True(t1Ann > t1Def);
        Assert.True(t2Def > t2Ann);
    }

    // ── Helpers ─────────────────────────────────────────────────────────

    private static Team[] CreateTeams()
    {
        return
        [
            new Team { Players = [new Player { Name = "A1" }, new Player { Name = "A2" }], Score = 0 },
            new Team { Players = [new Player { Name = "D1" }, new Player { Name = "D2" }], Score = 0 }
        ];
    }

    private static Round CreateRound(
        Team[] teams, Team announcingTeam, Announces trump,
        int team1TrickPts, int team2TrickPts,
        Player lastTrickWinner,
        bool announcerWinsAll, bool defenderWinsAll,
        bool isDoubled = false, bool isReDoubled = false)
    {
        var defendingTeam = teams.First(t => t != announcingTeam);
        var round = new Round
        {
            Trump = trump,
            AnnouncingTeam = announcingTeam,
            IsDoubled = isDoubled,
            IsReDoubled = isReDoubled,
            Team1TrickPoints = team1TrickPts,
            Team2TrickPoints = team2TrickPts
        };

        // Build 8 completed tricks
        for (int i = 0; i < 8; i++)
        {
            Player winner;
            if (announcerWinsAll)
                winner = announcingTeam.Players[0];
            else if (defenderWinsAll)
                winner = defendingTeam.Players[0];
            else
                winner = i < 4 ? announcingTeam.Players[0] : defendingTeam.Players[0];

            // Last trick gets the specified winner
            if (i == 7) winner = lastTrickWinner;

            var trick = new Trick { Winner = winner };
            trick.PlayedCards.Add(new PlayedCard(teams[0].Players[0], new Card(Suit.Hearts, "7", 0, 1)));
            trick.PlayedCards.Add(new PlayedCard(teams[1].Players[0], new Card(Suit.Hearts, "8", 0, 2)));
            trick.PlayedCards.Add(new PlayedCard(teams[0].Players[1], new Card(Suit.Hearts, "9", 0, 3)));
            trick.PlayedCards.Add(new PlayedCard(teams[1].Players[1], new Card(Suit.Hearts, "10", 10, 4)));
            round.CompletedTricks.Add(trick);
        }

        return round;
    }
}
