using BeloteEngine.Domain.Entities.Models;
using BeloteEngine.Application.DTOs;

namespace BeloteEngine.Presentation.Models;

public class LobbyResponse
{
    public Lobby? Lobby { get; set; }

    public LobbyInfoModel[]? Lobbies { get; set; }

    public bool? IsHostHere { get; set; }
}
