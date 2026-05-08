using System.Net;
using System.Text.Json;

namespace BeloteEngine.EndToEnd.Tests;

public sealed class SessionFlowTests : EndToEndTestBase
{
    [Fact]
    public async Task SessionFlow_ShouldCreateSetAndDeleteSession()
    {
        // Act — Create session
        using var createResponse = await Client.PostAsync("/api/session", content: null);

        // Assert — session cookie issued
        Assert.Equal(HttpStatusCode.NoContent, createResponse.StatusCode);
        Assert.True(createResponse.Headers.TryGetValues("Set-Cookie", out var createCookies));
        Assert.Contains(createCookies, c => c.Contains("belote_session=", StringComparison.OrdinalIgnoreCase));

        // Act — Set player name
        using var setResponse = await Client.PatchAsync("/api/session/Alice", content: null);

        // Assert — OK with sanitized name
        Assert.Equal(HttpStatusCode.OK, setResponse.StatusCode);
        var json = await setResponse.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("Alice", doc.RootElement.GetProperty("playerName").GetString());

        // Act — Delete session
        using var deleteResponse = await Client.DeleteAsync("/api/session");

        // Assert — NoContent
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task SessionFlow_ShouldRejectInvalidPlayerName_DuringSetSession()
    {
        // Arrange
        await Client.PostAsync("/api/session", content: null);

        // Act — special characters only
        using var response = await Client.PatchAsync("/api/session/%21%40%23%24", content: null);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
