using BeloteEngine.Data.Entities.Enums;

namespace BeloteEngine.Data.Entities.Models
{
    public class Player
    {
        public int? LobbyId { get; set; }

        public required string Name { get; init; }
        public Status Status { get; set; } = Status.Disconnected;
        public string ConnectionId { get; set; } = string.Empty;
        public bool Hoster { get; init; }
        public bool Splitter { get; set; }
        public bool Dealer { get; set; }
        public bool Announcer { get; set; }
        public bool Starter { get; set; }
        public Announces AnnounceOffer { get; set; } = 0;
        public List<Card> Hand { get; set; } = [];

        //public int Combinations { get; set; }
    }
}