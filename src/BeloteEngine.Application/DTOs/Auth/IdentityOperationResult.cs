namespace BeloteEngine.Application.DTOs.Auth;

public sealed record IdentityOperationResult(
    bool Succeeded,
    string? Token,
    IReadOnlyCollection<string> Errors)
{
    public static IdentityOperationResult Success(string token) =>
        new(true, token, []);

    public static IdentityOperationResult Failure(IEnumerable<string> errors) =>
        new(false, null, [.. errors]);
}
