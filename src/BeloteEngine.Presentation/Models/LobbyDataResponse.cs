using BeloteEngine.Domain.Entities.Models;

namespace BeloteEngine.Presentation.Models
{
    public class LobbyDataResponse
    {
        public Lobby Lobby { get; set; } = null!;

        public Game Game { get; set; } = null!;
    }
}
