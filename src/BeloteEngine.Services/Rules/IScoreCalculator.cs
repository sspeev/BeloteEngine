using BeloteEngine.Data.Entities.Models;

namespace BeloteEngine.Services.Rules;

public interface IScoreCalculator
{
    /// <summary>
    /// Calculates final round scores, applying Belote-specific rules
    /// (inside/outside, capot, etc.).
    /// </summary>
    (int Team1Score, int Team2Score) CalculateRoundScore(Round round, Team[] teams);
}