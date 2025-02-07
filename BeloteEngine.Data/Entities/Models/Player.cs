using BeloteEngine.Data.Entities.Enums;

namespace BeloteEngine.Data.Entities.Models
{
    public class Player
    {
        public Player()
        {
            Cards = new();
        }

        public bool LastSplitter { get; set; } = false;
        public Announces AnnounceOffer { get; set; } = 0;

        public Dictionary<string, Dictionary<string, int>> Cards { get; set; }

        //public int Combinations { get; set; }
    }
}
