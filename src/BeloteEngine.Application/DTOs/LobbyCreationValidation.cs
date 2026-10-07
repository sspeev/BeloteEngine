namespace BeloteEngine.Application.DTOs;

public sealed record LobbyCreationValidation(
    bool IsValid,
    string? ErrorMessage)
{
    public static LobbyCreationValidation Valid() =>
        new(true, null);

    public static LobbyCreationValidation Invalid(string message) =>
        new(false, message);
}