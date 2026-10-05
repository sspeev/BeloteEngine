namespace BeloteEngine.Application.Contracts;

public interface IUserIdentityService
{
    Task<IdentityOperationResult> CreateAsync(
        string username,
        string email,
        string password,
        CancellationToken cancellationToken);
}

public sealed record IdentityOperationResult(
    bool Succeeded,
    IReadOnlyCollection<string> Errors)
{
    public static IdentityOperationResult Success() =>
        new(true, []);

    public static IdentityOperationResult Failure(IEnumerable<string> errors) =>
        new(false, [.. errors]);
}
