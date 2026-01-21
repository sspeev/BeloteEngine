using BeloteEngine.Api.Hubs;
using BeloteEngine.Services.Contracts;
using BeloteEngine.Services.Services;
using Microsoft.AspNetCore.RateLimiting;
using System.Text.Json;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 100;
    options.CompactionPercentage = 0.25;
    options.ExpirationScanFrequency = TimeSpan.FromMinutes(5);
});

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("fixed", limiterOptions =>
    {
        limiterOptions.PermitLimit = 100;              // Max 10 requests
        limiterOptions.Window = TimeSpan.FromMinutes(1); // Per 1 minute
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 2;                 // Queue up to 2 requests
    });

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsync(
            "Too many requests.  Please try again later.",
            cancellationToken
        );
    };
});

builder.Services
    .AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.SuppressModelStateInvalidFilter = false;
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = true;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { 
        Title = "Belote Engine API", 
        Version = "v1",
        Description = "API for managing Belote game lobbies and gameplay"
    });
});

builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
    options.KeepAliveInterval = TimeSpan.FromMinutes(2);
    options.MaximumReceiveMessageSize = 102400; // 100 KB
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.SetIsOriginAllowed(_ => true)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
        else
        {
            policy.WithOrigins("https://yourdomain.com", "https://www.yourdomain.com")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
    });
});

builder.Services.AddSingleton<ILobbyService, LobbyService>();
builder.Services.AddSingleton<IGameService, GameService>();
builder.Services.AddSingleton<CachingService>();
builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
    if (builder.Environment.IsDevelopment())
    {
        logging.AddDebug();
        logging.SetMinimumLevel(LogLevel.Debug);
    }
    else
    {
        logging.SetMinimumLevel(LogLevel.Information);
    }
});
builder.Services.AddHealthChecks();

var app = builder.Build();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Starting Belote Engine API v1.0...");
logger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Belote Engine API v1");
        c.RoutePrefix = "swagger";
        c.EnableTryItOutByDefault();
        c.InjectStylesheet("/swagger-ui/custom.css");
        c.DocumentTitle = "Belote Engine API Documentation";
    });
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseRouting();
app.UseAuthorization();
app.MapHealthChecks("/health");

// Global error handling endpoint
app.Map("/error", (HttpContext context) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    logger.LogError("Unhandled exception occurred");
    return Results.Problem("An error occurred while processing your request");
});
app.MapControllers();
app.MapHub<BeloteHub>("/beloteHub", options =>
{
    options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets |
                        Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling;
});

try
{
    logger.LogInformation("Belote Engine API started successfully on {Urls}", 
        string.Join(", ", app.Urls));
    
    await app.RunAsync();
}
catch (Exception ex)
{
    logger.LogCritical(ex, "Application failed to start");
    throw;
}
finally
{
    logger.LogInformation("Belote Engine API shutting down...");
}
