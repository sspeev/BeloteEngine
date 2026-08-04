
// using BeloteEngine.Application.Contracts;
// using BeloteEngine.Domain.Entities.Models;
// using MediatR;

// namespace BeloteEngine.Application.Commands.Lobby;

// public class CreateLobbyCommand : IRequest<Lobby>
// {
//     public int Id { get; set; }
//     public string Name { get; set; } = string.Empty;
//     public List<Player> ConnectedPlayers { get; } = [];
//     public bool GameStarted { get; set; }
//     public Game Game { get; set; } = null!;
//     public string GamePhase { get; set; } = "waiting";

//     public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
//     public DateTime LastActivity { get; set; } = DateTime.UtcNow;
// }