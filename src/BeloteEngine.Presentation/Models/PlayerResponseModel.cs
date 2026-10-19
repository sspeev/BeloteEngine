using BeloteEngine.Domain.Entities.Models;

namespace BeloteEngine.Presentation.Models;

public sealed class PlayerResponseModel
{
    public string UserId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public bool Hoster { get; init; }
    public string Status { get; init; } = string.Empty;

    public static PlayerResponseModel FromDomain(Player player)
    {
        return new PlayerResponseModel
        {
            UserId = player.UserId,
            Name = player.Name,
            Hoster = player.Hoster,
            Status = player.Status.ToString()
        };
    }
}
