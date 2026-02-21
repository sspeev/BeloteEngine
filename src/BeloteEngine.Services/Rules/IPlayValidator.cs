using BeloteEngine.Data.Entities.Enums;
using BeloteEngine.Data.Entities.Models;

namespace BeloteEngine.Services.Rules;

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