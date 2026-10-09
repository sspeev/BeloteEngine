namespace BeloteEngine.Application.DTOs.Auth;

public sealed record IdentityOperationResult(
    bool Succeeded,
    IReadOnlyCollection<string> Errors)
{
    public static IdentityOperationResult Success() =>
        new(true, []);

    public static IdentityOperationResult Failure(IEnumerable<string> errors) =>
        new(false, [.. errors]);
}
