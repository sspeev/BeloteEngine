namespace BeloteEngine.Domain.Entities.Models;

public class JoinResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public Lobby? Lobby { get; set; }
}
