using BeloteEngine.Data.Entities.Models;

namespace BeloteEngine.Services.Rules;

public class ScoreCalculator : IScoreCalculator
{
    public (int Team1Score, int Team2Score) CalculateRoundScore(Round round, Team[] teams)
    {
        var announcingTeam = round.AnnouncingTeam;
        var defendingTeam = teams.First(t => t != announcingTeam);

        int announcerPoints = round.AnnouncingTeam == teams[0]
            ? round.Team1TrickPoints
            : round.Team2TrickPoints;
        int defenderPoints = round.AnnouncingTeam == teams[0]
            ? round.Team2TrickPoints
            : round.Team1TrickPoints;

        // Last trick bonus (+10)
        var lastTrick = round.CompletedTricks.Last();
        if (IsOnTeam(lastTrick.Winner!, announcingTeam))
            announcerPoints += 10;
        else
            defenderPoints += 10;

        // Valat (capot) — one team won all 8 tricks
        bool announcerCapot = round.CompletedTricks.All(t => IsOnTeam(t.Winner!, announcingTeam));
        bool defenderCapot = round.CompletedTricks.All(t => IsOnTeam(t.Winner!, defendingTeam));

        if (announcerCapot)
            return announcingTeam == teams[0] ? (252, 0) : (0, 252);

        if (defenderCapot)
            return defendingTeam == teams[0] ? (252, 0) : (0, 252);

        // "Inside" rule — announcer must have more points, otherwise defender gets all
        if (announcerPoints <= defenderPoints)
        {
            // Announcer is "inside" — all points go to defender
            int totalPoints = announcerPoints + defenderPoints;
            return defendingTeam == teams[0]
                ? (totalPoints, 0)
                : (0, totalPoints);
        }

        return announcingTeam == teams[0]
            ? (announcerPoints, defenderPoints)
            : (defenderPoints, announcerPoints);
    }

    private static bool IsOnTeam(Player player, Team team)
        => team.Players.Any(p => p.Name == player.Name);
}