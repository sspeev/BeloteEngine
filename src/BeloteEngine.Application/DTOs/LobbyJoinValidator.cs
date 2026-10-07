namespace BeloteEngine.Application.DTOs;

public sealed record LobbyJoinValidation(bool IsValid, string? ErrorMessage)
{
    public static LobbyJoinValidation Valid() => new(true, null);

    public static LobbyJoinValidation Invalid(string message) =>
        new(false, message);
}