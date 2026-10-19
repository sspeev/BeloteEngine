using BeloteEngine.Application.DTOs;

namespace BeloteEngine.Presentation.Models;

public sealed class LobbyResponse
{
    public LobbyResponseModel? Lobby { get; init; }

    public LobbyInfoModel[]? Lobbies { get; init; }

    public bool? IsHostHere { get; init; }
}
