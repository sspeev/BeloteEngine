using BeloteEngine.Data.Entities.Enums;
using BeloteEngine.Data.Entities.Models;

namespace BeloteEngine.Services.Rules;

public class PlayValidator : IPlayValidator
{
    public bool IsValidPlay(Card card, Player player, Trick currentTrick, Announces trump)
    {
        var playable = GetPlayableCards(player, currentTrick, trump);
        return playable.Any(c => c.Suit == card.Suit && c.Rank == card.Rank);
    }

    public List<Card> GetPlayableCards(Player player, Trick currentTrick, Announces trump)
    {
        // First card in trick — can play anything
        if (currentTrick.PlayedCards.Count == 0)
            return player.Hand.ToList();

        var leadSuit = currentTrick.LeadSuit;
        var trumpSuit = GetTrumpSuit(trump);
        var hand = player.Hand;

        var sameSuitCards = hand.Where(c => c.Suit == leadSuit).ToList();

        switch (trump)
        {
            case Announces.NoTrump:
                // Must follow suit if possible, otherwise play anything
                return sameSuitCards.Count > 0 ? sameSuitCards : hand.ToList();

            case Announces.AllTrumps:
                // Must follow suit; if following, must play higher if possible
                return GetAllTrumpsPlayable(hand, currentTrick, leadSuit);

            default:
                // Suit trump (Clubs/Diamonds/Hearts/Spades)
                return GetSuitTrumpPlayable(hand, currentTrick, leadSuit, trumpSuit!.Value, trump);
        }
    }

    private static List<Card> GetAllTrumpsPlayable(
        List<Card> hand, Trick currentTrick, Suit leadSuit)
    {
        var sameSuit = hand.Where(c => c.Suit == leadSuit).ToList();

        if (sameSuit.Count == 0)
            return hand.ToList(); // Can't follow suit

        // Must play higher if possible (every suit is trump in AllTrumps)
        var highestPlayed = currentTrick.PlayedCards
            .Where(pc => pc.Card.Suit == leadSuit)
            .Max(pc => pc.Card.Power);

        var higherCards = sameSuit.Where(c => c.Power > highestPlayed).ToList();
        return higherCards.Count > 0 ? higherCards : sameSuit;
    }

    private static List<Card> GetSuitTrumpPlayable(
        List<Card> hand, Trick currentTrick, Suit leadSuit,
        Suit trumpSuit, Announces trump)
    {
        var sameSuit = hand.Where(c => c.Suit == leadSuit).ToList();
        var trumpCards = hand.Where(c => c.Suit == trumpSuit).ToList();

        // Can follow suit
        if (sameSuit.Count > 0)
        {
            if (leadSuit == trumpSuit)
            {
                // Leading suit IS trump — must play higher trump if possible
                var highestTrumpPlayed = currentTrick.PlayedCards
                    .Where(pc => pc.Card.Suit == trumpSuit)
                    .Max(pc => pc.Card.Power);

                var higherTrumps = sameSuit.Where(c => c.Power > highestTrumpPlayed).ToList();
                return higherTrumps.Count > 0 ? higherTrumps : sameSuit;
            }
            return sameSuit; // Follow non-trump lead suit
        }

        // Can't follow suit — must trump if possible
        if (trumpCards.Count > 0)
        {
            // Check if partner is currently winning
            if (IsPartnerWinning(currentTrick, trumpSuit))
                return hand.ToList(); // Partner winning → play anything

            // Must play higher trump if possible
            var highestTrumpInTrick = currentTrick.PlayedCards
                .Where(pc => pc.Card.Suit == trumpSuit)
                .Select(pc => pc.Card.Power)
                .DefaultIfEmpty(0)
                .Max();

            var higherTrumps = trumpCards.Where(c => c.Power > highestTrumpInTrick).ToList();
            return higherTrumps.Count > 0 ? higherTrumps : trumpCards;
        }

        // Can't follow suit or trump — play anything
        return hand.ToList();
    }

    private static bool IsPartnerWinning(Trick currentTrick, Suit trumpSuit)
    {
        if (currentTrick.PlayedCards.Count < 2) return false;

        // Partner is 2 positions back from current player
        var trickWinner = DetermineTrickWinnerSoFar(currentTrick, trumpSuit);
        var currentPlayerIndex = currentTrick.PlayedCards.Count; // next to play
        var partnerIndex = currentPlayerIndex - 2;

        return partnerIndex >= 0 &&
               currentTrick.PlayedCards[partnerIndex].Player == trickWinner;
    }

    private static Player DetermineTrickWinnerSoFar(Trick trick, Suit trumpSuit)
    {
        var leadSuit = trick.LeadSuit;
        var trumpPlays = trick.PlayedCards.Where(pc => pc.Card.Suit == trumpSuit).ToList();

        if (trumpPlays.Count > 0)
            return trumpPlays.OrderByDescending(pc => pc.Card.Power).First().Player;

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