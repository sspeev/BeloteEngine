namespace BeloteEngine.Presentation.Models;

public sealed record AuthResponse
{
    public string Token { get; init; } = string.Empty;
}