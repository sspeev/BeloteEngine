using BeloteEngine.Data.Entities.Enums;
using BeloteEngine.Data.Entities.Models;

namespace BeloteEngine.Services.Rules;

public class TrickEvaluator : ITrickEvaluator
{
    public Player DetermineWinner(Trick trick, Announces trump)
    {
        if (!trick.IsComplete)
            throw new InvalidOperationException("Cannot evaluate an incomplete trick.");

        var leadSuit = trick.LeadSuit;
        var trumpSuit = GetTrumpSuit(trump);

        // Check for trump cards first (highest trump wins)
        if (trumpSuit.HasValue)
        {
            var trumpPlays = trick.PlayedCards
                .Where(pc => pc.Card.Suit == trumpSuit.Value)
                .ToList();

            if (trumpPlays.Count > 0)
                return trumpPlays.OrderByDescending(pc => pc.Card.Power).First().Player;
        }

        if (trump == Announces.AllTrumps)
        {
            // In AllTrumps, only lead suit matters — highest power of lead suit wins
            return trick.PlayedCards
                .Where(pc => pc.Card.Suit == leadSuit)
                .OrderByDescending(pc => pc.Card.Power)
                .First().Player;
        }

        // No trump played or NoTrump — highest card of lead suit wins
        return trick.PlayedCards
            .Where(pc => pc.Card.Suit == leadSuit)
            .OrderByDescending(pc => pc.Card.Power)
            .First().Player;
    }

    private static Suit? GetTrumpSuit(Announces trump) => trump switch
    {
        Announces.Clubs => Suit.Clubs,
        Announces.Diamonds => Suit.Diamonds,
        Announces.Hearts => Suit.Hearts,
        Announces.Spades => Suit.Spades,
        _ => null
    };
}