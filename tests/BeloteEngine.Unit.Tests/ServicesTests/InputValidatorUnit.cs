using BeloteEngine.Services.Security;

namespace BeloteEngine.Unit.Tests.Services;

public class InputValidatorUnit
{
    // ── SanitizePlayerName ──────────────────────────────────────────────

    [Fact]
    public void SanitizePlayerName_ShouldReturnCleanName_WhenInputIsValid()
    {
        //Act
        var result = InputValidator.SanitizePlayerName("Alice");

        //Assert
        Assert.Equal("Alice", result);
    }

    [Fact]
    public void SanitizePlayerName_ShouldThrow_WhenInputIsEmpty()
    {
        //Act & Assert
        Assert.Throws<ArgumentException>(
            () => InputValidator.SanitizePlayerName(""));
    }

    [Fact]
    public void SanitizePlayerName_ShouldThrow_WhenInputIsWhitespace()
    {
        //Act & Assert
        Assert.Throws<ArgumentException>(
            () => InputValidator.SanitizePlayerName("   "));
    }

    [Fact]
    public void SanitizePlayerName_ShouldStripHtmlTags()
    {
        //Act
        var result = InputValidator.SanitizePlayerName("<script>alert('xss')</script>Alice");

        //Assert
        Assert.Equal("alertxssAlice", result);
    }

    [Fact]
    public void SanitizePlayerName_ShouldStripSpecialCharacters()
    {
        //Act
        var result = InputValidator.SanitizePlayerName("Al!ce@#$%");

        //Assert
        Assert.Equal("Alce", result);
    }

    [Fact]
    public void SanitizePlayerName_ShouldThrow_WhenOnlyInvalidCharacters()
    {
        //Act & Assert
        Assert.Throws<ArgumentException>(
            () => InputValidator.SanitizePlayerName("!@#$%^&*()"));
    }

    [Fact]
    public void SanitizePlayerName_ShouldTruncateTo20Characters()
    {
        //Arrange
        var longName = new string('A', 30);

        //Act
        var result = InputValidator.SanitizePlayerName(longName);

        //Assert
        Assert.Equal(20, result.Length);
    }

    [Fact]
    public void SanitizePlayerName_ShouldTrimWhitespace()
    {
        //Act
        var result = InputValidator.SanitizePlayerName("  Bob  ");

        //Assert
        Assert.Equal("Bob", result);
    }

    [Fact]
    public void SanitizePlayerName_ShouldAllowHyphensAndUnderscores()
    {
        //Act
        var result = InputValidator.SanitizePlayerName("Player-One_2");

        //Assert
        Assert.Equal("Player-One_2", result);
    }

    // ── SanitizeLobbyName ──────────────────────────────────────────────

    [Fact]
    public void SanitizeLobbyName_ShouldReturnCleanName_WhenInputIsValid()
    {
        //Act
        var result = InputValidator.SanitizeLobbyName("My Lobby");

        //Assert
        Assert.Equal("My Lobby", result);
    }

    [Fact]
    public void SanitizeLobbyName_ShouldReturnDefault_WhenInputIsEmpty()
    {
        //Act
        var result = InputValidator.SanitizeLobbyName("");

        //Assert
        Assert.Equal("Unnamed Lobby", result);
    }

    [Fact]
    public void SanitizeLobbyName_ShouldReturnDefault_WhenInputIsWhitespace()
    {
        //Act
        var result = InputValidator.SanitizeLobbyName("   ");

        //Assert
        Assert.Equal("Unnamed Lobby", result);
    }

    [Fact]
    public void SanitizeLobbyName_ShouldStripHtmlTags()
    {
        //Act
        var result = InputValidator.SanitizeLobbyName("<b>Bold</b> Lobby");

        //Assert
        Assert.Equal("Bold Lobby", result);
    }

    [Fact]
    public void SanitizeLobbyName_ShouldTruncateTo50Characters()
    {
        //Arrange
        var longName = new string('L', 60);

        //Act
        var result = InputValidator.SanitizeLobbyName(longName);

        //Assert
        Assert.Equal(50, result.Length);
    }

    [Fact]
    public void SanitizeLobbyName_ShouldReturnDefault_WhenOnlySpecialCharacters()
    {
        //Act
        var result = InputValidator.SanitizeLobbyName("!@#$%^&*()");

        //Assert
        Assert.Equal("Unnamed Lobby", result);
    }
}
