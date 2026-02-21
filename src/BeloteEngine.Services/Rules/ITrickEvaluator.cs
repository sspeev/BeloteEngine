using BeloteEngine.Data.Entities.Enums;
using BeloteEngine.Data.Entities.Models;

namespace BeloteEngine.Services.Rules;

public interface ITrickEvaluator
{
    /// <summary>
    /// Determines the winner of a completed 4-card trick.
    /// </summary>
    Player DetermineWinner(Trick trick, Announces trump);
}