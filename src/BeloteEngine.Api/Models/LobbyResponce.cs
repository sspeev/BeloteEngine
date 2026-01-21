using BeloteEngine.Data.Entities.Models;
using BeloteEngine.Services.Models;

namespace BeloteEngine.Api.Models
{
    public class LobbyResponse
    {
        public Lobby? Lobby { get; set; }

        public LobbyInfoModel[]? Lobbies { get; set; }

        public bool? IsHostHere { get; set; }
    }
}
