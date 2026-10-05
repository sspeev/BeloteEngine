using BeloteEngine.Domain.Entities.Models;

namespace BeloteEngine.Application.DTOs;

public class JoinResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public Lobby? Lobby { get; set; }
}
