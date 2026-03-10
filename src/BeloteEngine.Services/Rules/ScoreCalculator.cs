using BeloteEngine.Data.Entities.Enums;
using BeloteEngine.Data.Entities.Models;

namespace BeloteEngine.Services.Rules;

public class ScoreCalculator : IScoreCalculator
{
    /// <summary>
    /// Divides raw trick points by 10, rounding half-up (standard Belote practice).
    /// </summary>
    private static int ToRecorded(int raw)
        => (int)Math.Round(raw / 10.0, MidpointRounding.AwayFromZero);

    public (int Team1Score, int Team2Score, bool IsHanging) CalculateRoundScore(
        Round round, Team[] teams, int pendingPoints)
    {
        var announcingTeam = round.AnnouncingTeam;
        var defendingTeam  = teams.First(t => t != announcingTeam);

        // Points each team earned from tricks this round
        int announcerRaw = round.AnnouncingTeam == teams[0]
            ? round.Team1TrickPoints
            : round.Team2TrickPoints;
        int defenderRaw = round.AnnouncingTeam == teams[0]
            ? round.Team2TrickPoints
            : round.Team1TrickPoints;

        // Last-trick bonus (+10 raw points)
        var lastTrick = round.CompletedTricks.Last();
        if (IsOnTeam(lastTrick.Winner!, announcingTeam))
            announcerRaw += 10;
        else
            defenderRaw += 10;

        // Multipliers (NoTrump *2, Double *2, Redouble *4)
        int doubleMulti = round.IsReDoubled ? 4 : (round.IsDoubled ? 2 : 1);
        int multiplier = (round.Trump == Announces.NoTrump ? 2 : 1) * doubleMulti;

        // ── Capot (valat) — one team won all 8 tricks ────────────────────────
        // All raw points go to the winning team; pending carried in too.
        bool announcerCapot = round.CompletedTricks.All(t => IsOnTeam(t.Winner!, announcingTeam));
        bool defenderCapot  = round.CompletedTricks.All(t => IsOnTeam(t.Winner!, defendingTeam));

        if (announcerCapot)
        {
            int total = ToRecorded(announcerRaw + pendingPoints) * multiplier;
            return announcingTeam == teams[0] ? (total, 0, false) : (0, total, false);
        }

        if (defenderCapot)
        {
            int total = ToRecorded(defenderRaw + pendingPoints) * multiplier;
            return defendingTeam == teams[0] ? (total, 0, false) : (0, total, false);
        }

        // ── Hanging (Висяща) — exactly equal trick points ─────────────────────
        // Announcer records 0; pending accumulates; defender records own points.
        if (announcerRaw == defenderRaw)
        {
            int defScore = ToRecorded(defenderRaw) * multiplier;
            return defendingTeam == teams[0]
                ? (defScore, 0, true)
                : (0, defScore, true);
        }

        // ── Set (Вкарана) — announcer has fewer points ───────────────────────
        // Defender records all points (own + announcer + pending); announcer records 0.
        if (announcerRaw < defenderRaw)
        {
            int allPoints = ToRecorded(announcerRaw + defenderRaw + pendingPoints) * multiplier;
            return defendingTeam == teams[0]
                ? (allPoints, 0, false)
                : (0, allPoints, false);
        }

        // ── Completed (Изкарана) — announcer has more points ─────────────────
        // Announcer records own points (+ pending); defender records own points.
        int annScore = ToRecorded(announcerRaw + pendingPoints) * multiplier;
        int defScoreCompleted = ToRecorded(defenderRaw) * multiplier;
        return announcingTeam == teams[0]
            ? (annScore, defScoreCompleted, false)
            : (defScoreCompleted, annScore, false);
    }

    private static bool IsOnTeam(Player player, Team team)
        => team.Players.Any(p => p.Name == player.Name);
}