using BeloteEngine.Services.Contracts;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace BeloteEngine.Api.Services;

/// <summary>
/// SignalR-backed implementation of <see cref="IAfkTimerService"/>.
/// Singleton — shared across all hub instances.
/// When a timer fires, the player receives an AfkDisconnected notification
/// and their connection is aborted after a short grace period.
/// </summary>
public sealed class AfkTimerService(
    IHubContext<Hubs.BeloteHub, Hubs.IBeloteClient> hubContext,
    IConfiguration configuration)
    : IAfkTimerService
{
    private const int DefaultAfkSeconds = 30;
    private const int GraceMs           = 300;

    private int AfkSeconds =>
        configuration.GetValue<int?>("AfkTimeoutSeconds") ?? DefaultAfkSeconds;

    private static readonly ConcurrentDictionary<string, HubCallerContext> _contexts = new();
    private static readonly ConcurrentDictionary<string, CancellationTokenSource> _timers = new();

    // ── Context lifetime management ───────────────────────────────────────────

    public void Register(string connectionId, object context)
    {
        if (context is HubCallerContext hubContext)
            _contexts[connectionId] = hubContext;
    }

    public void Unregister(string connectionId)
    {
        _contexts.TryRemove(connectionId, out _);
        Cancel(connectionId);
    }

    // ── Timer management ──────────────────────────────────────────────────────

    public void Start(string? connectionId)
    {
        if (connectionId is null) return;

        Cancel(connectionId); // reset any existing timer first

        var cts = new CancellationTokenSource();
        _timers[connectionId] = cts;

        _ = Task.Delay(TimeSpan.FromSeconds(AfkSeconds), cts.Token)
            .ContinueWith(async t =>
            {
                if (t.IsCanceled) return;

                await hubContext.Clients.Client(connectionId).AfkDisconnected();
                await Task.Delay(GraceMs);

                if (_contexts.TryGetValue(connectionId, out var ctx))
                    ctx.Abort();
            }, TaskScheduler.Default);
    }

    public void Cancel(string? connectionId)
    {
        if (connectionId is null) return;

        if (_timers.TryRemove(connectionId, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
        }
    }

    public void Transfer(string? fromConnectionId, string? toConnectionId)
    {
        Cancel(fromConnectionId);
        Start(toConnectionId);
    }
}
