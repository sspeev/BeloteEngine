using BeloteEngine.Domain.Entities.Enums;
using BeloteEngine.Domain.Entities.Models;

namespace BeloteEngine.Application.Rules;

/// <summary>
/// Validates whether a card play is legal given the current trick state.
/// </summary>
public interface IPlayValidator
{
    /// <summary>
    /// Returns true if the player can legally play this card.
    /// </summary>
    bool IsValidPlay(Card card, Player player, Trick currentTrick, Announces trump);

    /// <summary>
    /// Returns the list of cards the player is allowed to play.
    /// </summary>
    List<Card> GetPlayableCards(Player player, Trick currentTrick, Announces trump);
}