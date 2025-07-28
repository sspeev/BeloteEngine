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
        await HandleWebSocketConnection(webSocket, playerId);
    }
    else
    {
        context.Response.StatusCode = 400;
    }
});

app.MapFallbackToFile("index.html");

await app.RunAsync();

// WebSocket handler implementation
//this should be somewhere else
static async Task HandleWebSocketConnection(WebSocket webSocket, string playerId)
{
    var buffer = new byte[1024 * 4];
    
    try
    {
        while (webSocket.State == WebSocketState.Open)
        {
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            
            if (result.MessageType == WebSocketMessageType.Text)
            {
                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                // Handle the message
                Console.WriteLine($"Received from {playerId}: {message}");
                
                // Echo back
                var responseMessage = $"Echo: {message}";
                var responseBytes = Encoding.UTF8.GetBytes(responseMessage);
                await webSocket.SendAsync(
                    new ArraySegment<byte>(responseBytes),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None);
            }
            else if (result.MessageType == WebSocketMessageType.Close)
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"WebSocket error: {ex.Message}");
    }
}
