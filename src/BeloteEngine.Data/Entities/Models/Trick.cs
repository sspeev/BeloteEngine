using BeloteEngine.Data.Entities.Enums;

namespace BeloteEngine.Data.Entities.Models
{
    /// <summary>
    /// Represents a single trick (4 cards played, one per player).
    /// </summary>
    public class Trick
    {
        public List<PlayedCard> PlayedCards { get; set; } = [];

        public Player? Winner { get; set; }

        public Suit LeadSuit => PlayedCards.Count > 0 ? PlayedCards[0].Card.Suit : default;

        public bool IsComplete => PlayedCards.Count == 4;

        public int PointsValue => PlayedCards.Sum(pc => pc.Card.Value);
    }
}
