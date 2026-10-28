namespace BeloteEngine.Presentation.Common;

public sealed record ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
    public string? Message { get; init; }

    public static ApiResponse<T> Ok(T data, string? message = null) =>
        new() { Success = true, Data = data, Message = message };

    public static ApiResponse<T> Fail(IEnumerable<string> errors, string? message = null) =>
        new() { Success = false, Errors = [.. errors], Message = message };

    public static ApiResponse<T> Fail(string error, string? message = null) =>
        Fail([error], message);
}

// Non-generic version for endpoints with no data payload
public sealed record ApiResponse
{
    public bool Success { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
    public string? Message { get; init; }

    public static ApiResponse Ok(string? message = null) =>
        new() { Success = true, Message = message };

    public static ApiResponse Fail(IEnumerable<string> errors, string? message = null) =>
        new() { Success = false, Errors = [.. errors], Message = message };

    public static ApiResponse Fail(string error, string? message = null) =>
        Fail([error], message);
}