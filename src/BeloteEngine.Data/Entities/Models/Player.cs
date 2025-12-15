using BeloteEngine.Data.Entities.Enums;

namespace BeloteEngine.Data.Entities.Models
{
    public class Player
    {
        public int? LobbyId { get; set; }

        public required string Name { get; init; }

        public bool Hoster { get; init; }

        public Status Status { get; set; } = Status.Disconnected;

        public bool LastSplitter { get; set; }

        /// <summary>
        /// True if the player plays card first after the cards are dealt.
        /// </summary>
        public bool IsStarter { get; set; }
        public Announces AnnounceOffer { get; set; } = 0;

        public List<Card> Hand { get; set; } = [];

        //public int Combinations { get; set; }
    }
}
