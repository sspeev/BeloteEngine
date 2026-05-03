using Microsoft.AspNetCore.DataProtection;
using BeloteEngine.Services.Security;
using System.Security.Cryptography;
using System.Text.Json;
using BeloteEngine.Api.Contracts;

namespace BeloteEngine.Api.Services;

public sealed class SessionService(
    IDataProtectionProvider dataProtectionProvider) : ISessionService
{
    public const string CookieName = "belote_session";
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromDays(1);
    private readonly IDataProtector protector =
        dataProtectionProvider.CreateProtector("BeloteEngine.SessionCookie.v1");

    public void IssueSessionCookie(HttpRequest request, HttpResponse response)
    {
        var expiresAt = DateTimeOffset.UtcNow.Add(SessionLifetime);
        var sessionId = TryReadSessionId(request, out var existingSessionId)
            ? existingSessionId
            : Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

        var payload = new SessionCookiePayload(
            sessionId,
            null,
            expiresAt);

        var protectedPayload = protector.Protect(JsonSerializer.Serialize(payload));

        response.Cookies.Append(CookieName, protectedPayload, BuildCookieOptions(request, expiresAt));
    }

    public void SetSessionCookie(HttpRequest request, HttpResponse response, string playerName)
    {
        var normalizedPlayerName = InputValidator.SanitizePlayerName(playerName);
        var expiresAt = DateTimeOffset.UtcNow.Add(SessionLifetime);
        var sessionId = TryReadSessionId(request, out var existingSessionId)
            ? existingSessionId
            : Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

        var payload = new SessionCookiePayload(
            sessionId,
            normalizedPlayerName,
            expiresAt);

        var protectedPayload = protector.Protect(JsonSerializer.Serialize(payload));

        response.Cookies.Append(CookieName, protectedPayload, BuildCookieOptions(request, expiresAt));
    }

    public void ClearSessionCookie(HttpRequest request, HttpResponse response)
    {
        response.Cookies.Delete(CookieName, BuildDeleteCookieOptions(request));
    }

    public bool TryReadSession(HttpRequest request, out SessionIdentity? session)
    {
        session = null;

        if (!TryReadPayload(request, out var payload) ||
            string.IsNullOrWhiteSpace(payload!.PlayerName))
            return false;

        session = new SessionIdentity(payload.SessionId, payload.PlayerName!, payload.ExpiresAt);
        return true;
    }

    private bool TryReadSessionId(HttpRequest request, out string sessionId)
    {
        sessionId = string.Empty;
        if (!TryReadPayload(request, out var payload))
            return false;

        sessionId = payload!.SessionId;
        return true;
    }

    private bool TryReadPayload(HttpRequest request, out SessionCookiePayload? payload)
    {
        payload = null;

        if (!request.Cookies.TryGetValue(CookieName, out var protectedPayload) ||
            string.IsNullOrWhiteSpace(protectedPayload))
            return false;

        try
        {
            var json = protector.Unprotect(protectedPayload);
            payload = JsonSerializer.Deserialize<SessionCookiePayload>(json);

            return payload is not null &&
                   !string.IsNullOrWhiteSpace(payload.SessionId) &&
                   payload.ExpiresAt > DateTimeOffset.UtcNow;
        }
        catch (CryptographicException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private sealed record SessionCookiePayload(
        string SessionId,
        string? PlayerName,
        DateTimeOffset ExpiresAt);

    private static CookieOptions BuildCookieOptions(HttpRequest request, DateTimeOffset expiresAt)
    {
        var isSecureRequest = request.IsHttps;

        return new CookieOptions
        {
            HttpOnly = true,
            Secure = isSecureRequest,
            SameSite = isSecureRequest ? SameSiteMode.None : SameSiteMode.Lax,
            Expires = expiresAt,
            Path = "/",
            IsEssential = true
        };
    }

    private static CookieOptions BuildDeleteCookieOptions(HttpRequest request)
    {
        var isSecureRequest = request.IsHttps;
        return new CookieOptions
        {
            Path = "/",
            Secure = isSecureRequest,
            SameSite = isSecureRequest ? SameSiteMode.None : SameSiteMode.Lax
        };
    }
}

public sealed record SessionIdentity(string SessionId, string PlayerName, DateTimeOffset ExpiresAt);
