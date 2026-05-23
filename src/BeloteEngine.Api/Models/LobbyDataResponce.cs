using BeloteEngine.Data.Entities.Models;

namespace BeloteEngine.Api.Models
{
    public class LobbyDataResponce
    {
        public Lobby Lobby { get; set; } = null!;

        public Game Game { get; set; } = null!;
    }
}
