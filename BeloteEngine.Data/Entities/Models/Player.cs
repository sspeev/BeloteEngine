using BeloteEngine.Data.Entities.Enums;

namespace BeloteEngine.Data.Entities.Models
{
    public class Player
    {
        //public int Id { get; set; }
        public Player() => Cards = [];
        public int? LobbyId { get; set; }

        public required string Name { get; set; }

        public bool Hoster { get; set; }

        public Status Status { get; set; } = Status.Disconnected;

        public bool LastSplitter { get; set; } = false;

        /// <summary>
        /// True if the player will play card first after the cards are dealt.
        /// </summary>
        public bool IsStarter { get; set; } = false;
        public Announces AnnounceOffer { get; set; } = 0;

        public Dictionary<string, Dictionary<string, int>> Cards { get; set; }

        //public int Combinations { get; set; }
    }
}
