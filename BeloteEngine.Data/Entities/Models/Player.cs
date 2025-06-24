using BeloteEngine.Data.Entities.Enums;

namespace BeloteEngine.Data.Entities.Models
{
    public class Player
    {
        public Player()
        {
            Cards = new();
        }
        public int ConnectionId { get; set; }

        public string Name { get; set; }

        public bool IsConnected { get; set; }

        public bool LastSplitter { get; set; } = false;
        public Announces AnnounceOffer { get; set; } = 0;

        public Dictionary<string, Dictionary<string, int>> Cards { get; set; }

        //public int Combinations { get; set; }
    }
}
