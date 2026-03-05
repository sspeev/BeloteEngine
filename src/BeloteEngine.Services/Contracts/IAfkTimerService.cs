namespace BeloteEngine.Services.Contracts;

/// <summary>
/// Manages per-player AFK timers during active gameplay turns.
/// The implementation lives in the API layer where SignalR is available.
/// </summary>
public interface IAfkTimerService
{
    /// <summary>Registers a player's connection so their context can be aborted when AFK.</summary>
    void Register(string connectionId, object context);

    /// <summary>Cancels and removes any timer for this connection, then unregisters the context.</summary>
    void Unregister(string connectionId);

    /// <summary>Starts (or resets) the AFK countdown for the given connection.</summary>
    void Start(string? connectionId);

    /// <summary>Cancels the AFK countdown for the given connection.</summary>
    void Cancel(string? connectionId);

    /// <summary>
    /// Cancels <paramref name="fromConnectionId"/>'s timer and starts one for
    /// <paramref name="toConnectionId"/>. Pass null for either to skip that side.
    /// </summary>
    void Transfer(string? fromConnectionId, string? toConnectionId);
}
