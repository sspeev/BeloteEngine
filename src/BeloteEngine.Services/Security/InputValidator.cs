using System.Text.RegularExpressions;

namespace BeloteEngine.Services.Security;

public static class InputValidator
{
    public static string SanitizePlayerName(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Player name cannot be empty");

        // Remove HTML/script tags
        input = Regex.Replace(input, @"<[^>]*>", string.Empty);

        // Only allow letters, numbers, spaces, hyphens, underscores
        input = Regex.Replace(input, @"[^\w\s-]", "");

        // Trim and limit length
        input = input.Trim();
        if (input.Length == 0)
            throw new ArgumentException("Player name contains only invalid characters");

        if (input.Length > 20)
            input = input[..20];

        return input;
    }

    public static string SanitizeLobbyName(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "Unnamed Lobby";

        input = Regex.Replace(input, @"<[^>]*>", string.Empty);
        input = Regex.Replace(input, @"[^\w\s-]", "");
        input = input.Trim();

        return string.IsNullOrEmpty(input) ? "Unnamed Lobby" : input[..Math.Min(50, input.Length)];
    }
}