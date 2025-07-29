using BeloteEngine.Api.Hubs;
using BeloteEngine.Data.Entities.Models;
using BeloteEngine.Services.Contracts;
using BeloteEngine.Services.Services;
using Microsoft.AspNetCore.SignalR;
using System.Net.WebSockets;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSignalR();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy => policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()); // Added for SignalR
});

builder.Services.AddSingleton<IWebSocketService, WebSocketService>();
builder.Services.AddSingleton<ILobbyService, LobbyService>();
builder.Services.AddSingleton<IGameService, GameService>();

var app = builder.Build();

app.Logger.LogInformation("Starting BeloteEngine API...");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.EnableTryItOutByDefault();
        c.InjectStylesheet("/swagger-ui/custom.css");
    });
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// CORS must come before routing for SignalR
app.UseCors("AllowFrontend");
app.UseWebSockets();
app.UseRouting();
app.UseAuthorization();

// Map endpoints
app.MapControllers();
app.MapHub<BeloteHub>("/beloteHub");

// WebSocket endpoint (if you really need it alongside SignalR)
app.Map("/ws/lobby", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        var playerId = context.Request.Query["playerId"];
        var webSocketService = context.RequestServices.GetRequiredService<IWebSocketService>();
        await webSocketService.HandleConnectionAsync(webSocket, playerId);
    }
    else
    {
        context.Response.StatusCode = 400;
    }
});

app.MapFallbackToFile("index.html");

await app.RunAsync();
