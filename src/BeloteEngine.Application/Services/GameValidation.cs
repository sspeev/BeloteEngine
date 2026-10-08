using BeloteEngine.Application.Contracts.Game;
using BeloteEngine.Application.Rules;
using BeloteEngine.Domain.Entities.Enums;
using BeloteEngine.Domain.Entities.Models;
using static BeloteEngine.Domain.Entities.Enums.Announces;

namespace BeloteEngine.Application.Services;

public sealed class GameValidation(IPlayValidator playValidator) : IGameValidation
{
    public void ValidateLobby(Lobby lobby)
    {
        ArgumentNullException.ThrowIfNull(lobby);
        ArgumentNullException.ThrowIfNull(lobby.Game);

        if (lobby.Game.Teams == null || lobby.Game.Teams.Any(team => team.Players.Length != 2))
        {
            throw new ArgumentException(
                "Invalid teams configuration. Each team must have exactly 2 players.");
        }
    }

    public Round ValidateActiveRound(Game game) =>
        game.CurrentRound
        ?? throw new InvalidOperationException("No active round.");

    public Player ValidatePlayer(Lobby lobby, string playerName) =>
        lobby.ConnectedPlayers.FirstOrDefault(player => player.Name == playerName)
        ?? throw new ArgumentException($"Player {playerName} not found in the lobby.");

    public Player ValidatePlayerForCardPlay(Lobby lobby, string playerName) =>
        lobby.ConnectedPlayers.FirstOrDefault(player => player.Name == playerName)
        ?? throw new ArgumentException($"Player {playerName} not found.");

    public void ValidateTurn(Game game, string playerName)
    {
        if (game.CurrentPlayer.Name != playerName)
        {
            throw new InvalidOperationException("It's not your turn.");
        }
    }

    public void ValidateCardPlay(Card card, Player player, Trick trick, Announces trump)
    {
        if (!playValidator.IsValidPlay(card, player, trick, trump))
        {
            throw new InvalidOperationException("Invalid card play.");
        }
    }

    public Announces ParseBid(string bid)
    {
        if (!Enum.TryParse(bid, out Announces announce))
        {
            throw new ArgumentException($"Invalid bid: {bid} or failed to parse");
        }

        return announce;
    }

    public void ValidateBid(Game game, Player player, Announces announce)
    {
        if (announce == Pass)
        {
            return;
        }

        if (announce == Announces.Double)
        {
            if (game.CurrentAnnounce == Announces.None
                || game.CurrentAnnounce == Announces.Double
                || game.CurrentAnnounce == Announces.ReDouble)
            {
                throw new InvalidOperationException(
                    "You can only double an active suit or NoTrump bid!");
            }

            if (game.ContractPlayer == null
                || IsOnTeam(player, game.Teams.First(team => IsOnTeam(game.ContractPlayer, team))))
            {
                throw new InvalidOperationException(
                    "You can only double an opponent's bid!");
            }

            if (game.CurrentRound.IsDoubled)
            {
                throw new InvalidOperationException("This bid is already doubled!");
            }

            return;
        }

        if (announce == ReDouble)
        {
            if (!game.CurrentRound.IsDoubled)
            {
                throw new InvalidOperationException(
                    "You can only redouble a doubled contract!");
            }

            if (game.CurrentRound.IsReDoubled)
            {
                throw new InvalidOperationException("This bid is already redoubled!");
            }

            if (game.ContractPlayer == null
                || !IsOnTeam(player, game.Teams.First(team => IsOnTeam(game.ContractPlayer, team))))
            {
                throw new InvalidOperationException(
                    "You can only redouble your own team's doubled contract!");
            }

            return;
        }

        if (game.CurrentAnnounce != None && game.CurrentAnnounce < announce)
        {
            return;
        }

        if (game.CurrentAnnounce == None)
        {
            return;
        }

        throw new InvalidOperationException("Your bid must be higher than the current announce!");
    }

    private static bool IsOnTeam(Player player, Team team) =>
        team.Players.Any(teamPlayer => teamPlayer.Name == player.Name);
}

