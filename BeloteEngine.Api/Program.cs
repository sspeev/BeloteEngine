using BeloteEngine.Api.Hubs;
using BeloteEngine.Services.Contracts;
using BeloteEngine.Services.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net.WebSockets;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// ===== SERVICE CONFIGURATION =====

// Core web services
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        // Custom model validation handling
        options.SuppressModelStateInvalidFilter = false;
    })
    .AddJsonOptions(options =>
    {
        // Configure JSON serialization for consistency
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = true;
    });

// API Documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { 
        Title = "Belote Engine API", 
        Version = "v1",
        Description = "API for managing Belote game lobbies and gameplay"
    });
});

// Real-time communication
builder.Services.AddSignalR(options =>
{
    // Configure SignalR for better performance
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
    options.KeepAliveInterval = TimeSpan.FromSeconds(30);
});

// CORS Configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            // Development: Allow all origins
            policy.SetIsOriginAllowed(_ => true)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
        else
        {
            // Production: Restrict to specific origins
            policy.WithOrigins("https://yourdomain.com", "https://www.yourdomain.com")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
    });
});

// ===== DEPENDENCY INJECTION =====

// Application Services - Using appropriate lifetimes
builder.Services.AddSingleton<ILobbyService, LobbyService>();
builder.Services.AddSingleton<IGameService, GameService>();
builder.Services.AddSingleton<IWebSocketService, WebSocketService>();

// ===== LOGGING CONFIGURATION =====
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

// ===== HEALTH CHECKS =====
builder.Services.AddHealthChecks();

var app = builder.Build();

// ===== STARTUP LOGGING =====
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Starting Belote Engine API v1.0...");
logger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);

// ===== MIDDLEWARE PIPELINE =====

// Development-specific middleware
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
    // Production error handling
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

// Security and performance middleware
app.UseHttpsRedirection();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Add cache headers for static files
        ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=31536000");
    }
});

// CORS (must be before routing for SignalR)
app.UseCors("AllowFrontend");

// WebSocket configuration
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromMinutes(2),
    AllowedOrigins = { "http://localhost:5173", "https://localhost:5173" }
});

// Routing and authorization
app.UseRouting();
app.UseAuthorization();

// ===== API ENDPOINTS =====

// Health check endpoint
app.MapHealthChecks("/health");

// WebSocket endpoint with proper error handling
app.MapGet("/ws/lobby/{lobbyId:int}", async (
    HttpContext context,
    int lobbyId,
    IWebSocketService webSocketService,
    ILogger<Program> logger) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        logger.LogWarning("Non-WebSocket request received at WebSocket endpoint");
        return Results.BadRequest("WebSocket request expected");
    }

    var playerId = context.Request.Query["playerId"].ToString();
    if (string.IsNullOrWhiteSpace(playerId))
    {
        logger.LogWarning("WebSocket connection attempted without playerId");
        return Results.BadRequest("PlayerId is required");
    }

    try
    {
        logger.LogInformation("Accepting WebSocket connection for player {PlayerId} in lobby {LobbyId}", 
            playerId, lobbyId);

        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        await webSocketService.HandleConnectionAsync(webSocket, playerId, lobbyId.ToString());
        
        return Results.Ok();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error handling WebSocket connection for player {PlayerId}", playerId);
        return Results.Problem("Failed to establish WebSocket connection");
    }
});

// Global error handling endpoint
app.Map("/error", (HttpContext context) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    logger.LogError("Unhandled exception occurred");
    return Results.Problem("An error occurred while processing your request");
});

// API Controllers
app.MapControllers();

// SignalR Hub
app.MapHub<BeloteHub>("/beloteHub", options =>
{
    options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets |
                        Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling;
});

// SPA fallback (for React frontend)
app.MapFallbackToFile("index.html");

// ===== APPLICATION STARTUP =====

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
