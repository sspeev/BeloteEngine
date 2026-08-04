
using Microsoft.AspNetCore.Http;

namespace BeloteEngine.Infrastructure.Session;

public interface ISessionService
{
    void IssueSessionCookie(HttpRequest request, HttpResponse response);

    void SetSessionCookie(HttpRequest request, HttpResponse response, string playerName);

    void ClearSessionCookie(HttpRequest request, HttpResponse response);

    bool TryReadSession(HttpRequest request, out SessionIdentity? session);
}
