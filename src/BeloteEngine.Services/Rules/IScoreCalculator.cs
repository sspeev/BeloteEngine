using BeloteEngine.Data.Entities.Models;

namespace BeloteEngine.Services.Rules;

public interface IScoreCalculator
{
    /// <summary>
    /// Calculates final round scores applying Belote rules:
    /// completed (изкарана), set (вкарана), or hanging (висяща).
    /// Points are divided by 10 and rounded (MidpointRounding.AwayFromZero).
    /// </summary>
    /// <param name="round">The completed round.</param>
    /// <param name="teams">The two game teams (teams[0] and teams[1]).</param>
    /// <param name="pendingPoints">Raw points carried over from previous hanging rounds.</param>
    /// <returns>
    /// Scored points added to Team1 and Team2 this round, plus a flag indicating
    /// whether this was a hanging round (so the caller can accumulate pending points).
    /// </returns>
    (int Team1Score, int Team2Score, bool IsHanging) CalculateRoundScore(
        Round round, Team[] teams, int pendingPoints);
}