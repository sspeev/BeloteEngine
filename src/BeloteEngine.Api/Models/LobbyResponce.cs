using BeloteEngine.Data.Entities.Models;
using BeloteEngine.Services.Contracts;

namespace BeloteEngine.Api.Models
{
    public class LobbyResponse
    {
        public Lobby? Lobby { get; set; }

        public LobbyInfo[]? Lobbies { get; set; }

        public bool? IsHostHere { get; set; }
    }
}
