using BeloteEngine.Api.Services;

namespace BeloteEngine.Api.Contracts;

public interface ISessionService
{
    void IssueSessionCookie(HttpRequest request, HttpResponse response);

    void SetSessionCookie(HttpRequest request, HttpResponse response, string playerName);

    void ClearSessionCookie(HttpRequest request, HttpResponse response);

    bool TryReadSession(HttpRequest request, out SessionIdentity? session);
}
