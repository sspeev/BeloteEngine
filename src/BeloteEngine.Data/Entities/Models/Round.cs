using BeloteEngine.Data.Entities.Enums;

namespace BeloteEngine.Data.Entities.Models
{
    /// <summary>
    /// Represents one full round (8 tricks after bidding).
    /// </summary>
    public class Round
    {
        public Announces Trump { get; set; }

        public Team AnnouncingTeam { get; set; } = null!;

        public Trick CurrentTrick { get; set; } = new();

        public List<Trick> CompletedTricks { get; set; } = [];

        public int TrickCount => CompletedTricks.Count;

        public bool IsComplete => CompletedTricks.Count == 8;

        public int Team1TrickPoints { get; set; }

        public int Team2TrickPoints { get; set; }
    }
}