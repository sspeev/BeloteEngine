using BeloteEngine.Application.Contracts;
using BeloteEngine.Application.Rules;
using BeloteEngine.Application.Services;
using BeloteEngine.Infrastructure.Session;
using BeloteEngine.Presentation.Services;

namespace BeloteEngine.Presentation.Extensions;

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
