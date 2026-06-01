using BeloteEngine.Services.Security;
using BeloteEngine.Api.Contracts;
using Microsoft.AspNetCore.DataProtection;
using System.Security.Cryptography;
using System.Text.Json;

namespace BeloteEngine.Api.Services;

/// <summary>
/// Provides functionality for issuing, reading, and clearing secure session cookies for player authentication within
/// the application.
/// </summary>
/// <remarks>SessionService manages session cookies that store player identity and expiration information in a
/// secure, tamper-resistant format. The service ensures that session cookies are protected using data protection APIs
/// and enforces a fixed session lifetime. This class is intended for use in web applications that require secure,
/// stateless session management for players.</remarks>
/// <param name="dataProtectionProvider">The data protection provider used to create a data protector for encrypting and decrypting session cookie payloads.
/// Cannot be null.</param>
public sealed class SessionService(
    IDataProtectionProvider dataProtectionProvider,
    IHostEnvironment environment) : ISessionService
{
    public const string CookieName = "belote_session";
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(1);
    private readonly IDataProtector protector =
        dataProtectionProvider.CreateProtector("BeloteEngine.SessionCookie.v1");
    private readonly IHostEnvironment _environment = environment;

    public void IssueSessionCookie(HttpRequest request, HttpResponse response)
    {
        var expiresAt = DateTimeOffset.UtcNow.Add(SessionLifetime);
        var hasExisting = TryReadPayload(request, out var existingPayload);

        var sessionId = hasExisting && !string.IsNullOrWhiteSpace(existingPayload!.SessionId)
            ? existingPayload.SessionId
            : Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

        var playerName = hasExisting ? existingPayload!.PlayerName : null;

        var payload = new SessionCookiePayload(
            sessionId,
            playerName,
            expiresAt);

        var protectedPayload = protector.Protect(JsonSerializer.Serialize(payload));

        response.Cookies.Append(CookieName, protectedPayload, BuildCookieOptions(request, expiresAt));
        System.Diagnostics.Debug.WriteLine($"✅ Initial session cookie issued. SessionId={sessionId}, PlayerName={playerName}");
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

        if (_environment.IsDevelopment())
            System.Diagnostics.Debug.WriteLine("Initial session cookie issued.");
    }

    public void ClearSessionCookie(HttpRequest request, HttpResponse response)
    {
        response.Cookies.Delete(CookieName, BuildDeleteCookieOptions(request));
    }

    public bool TryReadSession(HttpRequest request, out SessionIdentity? session)
    {
        session = null;

        if (!request.Cookies.TryGetValue(CookieName, out var cookieValue))
        {
            System.Diagnostics.Debug.WriteLine("❌ Session cookie not found");
            return false;
        }

        if (!TryReadPayload(request, out var payload))
        {
            System.Diagnostics.Debug.WriteLine("❌ Failed to read/decrypt session payload");
            return false;
        }

        if (string.IsNullOrWhiteSpace(payload!.PlayerName))
        {
            System.Diagnostics.Debug.WriteLine($"❌ PlayerName is null/empty. SessionId={payload.SessionId}");
            return false;
        }

        session = new SessionIdentity(payload.SessionId, payload.PlayerName!, payload.ExpiresAt);
        System.Diagnostics.Debug.WriteLine($"✅ Session validated. PlayerName={payload.PlayerName}");
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
        {
            System.Diagnostics.Debug.WriteLine($"❌ Cookie '{CookieName}' not found or empty");
            return false;
        }

        try
        {
            System.Diagnostics.Debug.WriteLine("🔓 Attempting to unprotect payload...");
            var json = protector.Unprotect(protectedPayload);
            System.Diagnostics.Debug.WriteLine("✅ Payload unprotected successfully");

            payload = JsonSerializer.Deserialize<SessionCookiePayload>(json);

            if (payload is null)
            {
                System.Diagnostics.Debug.WriteLine("❌ Deserialized payload is null");
                return false;
            }

            if (string.IsNullOrWhiteSpace(payload.SessionId))
            {
                System.Diagnostics.Debug.WriteLine("❌ SessionId is null/empty after deserialization");
                return false;
            }

            if (payload.ExpiresAt <= DateTimeOffset.UtcNow)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Session expired. ExpiresAt={payload.ExpiresAt}");
                return false;
            }

            if (_environment.IsDevelopment())
                System.Diagnostics.Debug.WriteLine("Payload valid.");

            return true;
        }
        catch (CryptographicException ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Cryptographic error: {ex.Message}");
            return false;
        }
        catch (JsonException ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ JSON deserialization error: {ex.Message}");
            return false;
        }
        catch (FormatException ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Format error: {ex.Message}");
            return false;
        }
    }

    private sealed record SessionCookiePayload(
        string SessionId,
        string? PlayerName,
        DateTimeOffset ExpiresAt);

    private CookieOptions BuildCookieOptions(HttpRequest request, DateTimeOffset expiresAt)
    {
        var isSecureRequest = request.IsHttps;

        // In development, use Lax to avoid browser blocking with localhost on different ports
        var sameSite = _environment.IsDevelopment() ? SameSiteMode.Lax : (isSecureRequest ? SameSiteMode.None : SameSiteMode.Lax);

        return new CookieOptions
        {
            HttpOnly = true,
            Secure = isSecureRequest,
            SameSite = sameSite,
            Expires = expiresAt,
            Path = "/",
            IsEssential = true
        };
    }

    private CookieOptions BuildDeleteCookieOptions(HttpRequest request)
    {
        var isSecureRequest = request.IsHttps;
        var sameSite = _environment.IsDevelopment() ? SameSiteMode.Lax : (isSecureRequest ? SameSiteMode.None : SameSiteMode.Lax);

        return new CookieOptions
        {
            Path = "/",
            Secure = isSecureRequest,
            SameSite = sameSite
        };
    }
}

public sealed record SessionIdentity(string SessionId, string PlayerName, DateTimeOffset ExpiresAt);
