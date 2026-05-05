using BeloteEngine.Api.Contracts;
using BeloteEngine.Api.Services;
using BeloteEngine.Services.Contracts;
using BeloteEngine.Services.Rules;
using BeloteEngine.Services.Services;

namespace BeloteEngine.Api.Extensions;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection service)
    {
        service.AddSingleton<ILobbyService, LobbyService>();
        service.AddSingleton<IGameService, GameService>();
        service.AddSingleton<IConnectionLimiter, ConnectionLimiter>();
        service.AddSingleton<ITrickEvaluator, TrickEvaluator>();
        service.AddSingleton<IPlayValidator, PlayValidator>();
        service.AddSingleton<IScoreCalculator, ScoreCalculator>();
        service.AddSingleton<CachingService>();
        service.AddSingleton<IAfkTimerService, AfkTimerService>();
        service.AddSingleton<ISessionService, SessionService>();

        return service;
    }
}
