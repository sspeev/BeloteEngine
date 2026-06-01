using BeloteEngine.Data.Entities.Models;

namespace BeloteEngine.Api.Models
{
    public class LobbyDataResponse
    {
        public Lobby Lobby { get; set; } = null!;

        public Game Game { get; set; } = null!;
    }
}
