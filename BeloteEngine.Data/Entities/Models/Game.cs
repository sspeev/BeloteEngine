using BeloteEngine.Data.Entities.Enums;
using BeloteEngine.Models;

namespace BeloteEngine.Data.Entities.Models
{
    public class Game : IGame
    {
        public Game()
        {
            players = new List<Player>();
        }

        public IList<Player> players { get; set; }

        public Announces MyProperty { get; set; }
    }
}
