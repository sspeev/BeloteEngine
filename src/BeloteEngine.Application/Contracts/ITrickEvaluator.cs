using BeloteEngine.Domain.Entities.Enums;
using BeloteEngine.Domain.Entities.Models;

namespace BeloteEngine.Application.Rules;

public interface ITrickEvaluator
{
    /// <summary>
    /// Determines the winner of a completed 4-card trick.
    /// </summary>
    Player DetermineWinner(Trick trick, Announces trump);
}