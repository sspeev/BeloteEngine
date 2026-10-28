using BeloteEngine.Application.User.Commands.Register;
using BeloteEngine.Application.User.Commands.Login;
using BeloteEngine.Application.User.Commands.RefreshToken;
using BeloteEngine.Application.User.Commands.RevokeToken;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using BeloteEngine.Presentation.Common;
using BeloteEngine.Presentation.Models;

namespace BeloteEngine.Presentation.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class AuthController(
    ISender sender
    ) : ControllerBase
{

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new RegisterUserCommand(request.Username, request.Email, request.Password),
            cancellationToken);

        if (result.Succeeded)
        {
            // Set refresh token in httpOnly cookie
            SetRefreshTokenCookie(result.RefreshToken!, result.RefreshTokenExpiresAt!.Value);

            var authResponse = new AuthResponse
            {
                AccessToken = result.AccessToken!,
                AccessTokenExpiresAt = result.AccessTokenExpiresAt!.Value,
                RefreshToken = string.Empty, // Not returned in body
                RefreshTokenExpiresAt = result.RefreshTokenExpiresAt!.Value
            };
            return Ok(ApiResponse<AuthResponse>.Ok(authResponse, "Registration successful"));
        }

        return BadRequest(ApiResponse<AuthResponse>.Fail(result.Errors, "Registration failed"));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new LoginUserCommand(request.Email, request.Password),
            cancellationToken);

        if (!result.Succeeded)
        {
            return Unauthorized(ApiResponse<AuthResponse>.Fail(result.Errors, "Invalid credentials"));
        }

        // Set refresh token in httpOnly cookie
        SetRefreshTokenCookie(result.RefreshToken!, result.RefreshTokenExpiresAt!.Value);

        var authResponse = new AuthResponse
        {
            AccessToken = result.AccessToken!,
            AccessTokenExpiresAt = result.AccessTokenExpiresAt!.Value,
            RefreshToken = string.Empty, // Not returned in body
            RefreshTokenExpiresAt = result.RefreshTokenExpiresAt!.Value
        };
        return Ok(ApiResponse<AuthResponse>.Ok(authResponse, "Login successful"));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Refresh(
        CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var refreshToken = Request.Cookies["refreshToken"];

        if (string.IsNullOrEmpty(refreshToken))
        {
            return Unauthorized(ApiResponse<AuthResponse>.Fail("Refresh token not found"));
        }

        var result = await sender.Send(
            new RefreshTokenCommand(refreshToken, ipAddress),
            cancellationToken);

        if (!result.Succeeded)
        {
            return Unauthorized(ApiResponse<AuthResponse>.Fail(result.Errors, "Invalid or expired refresh token"));
        }

        // Set new refresh token in httpOnly cookie (rotation)
        SetRefreshTokenCookie(result.RefreshToken!, result.RefreshTokenExpiresAt!.Value);

        var authResponse = new AuthResponse
        {
            AccessToken = result.AccessToken!,
            AccessTokenExpiresAt = result.AccessTokenExpiresAt!.Value,
            RefreshToken = string.Empty, // Not returned in body
            RefreshTokenExpiresAt = result.RefreshTokenExpiresAt!.Value
        };
        return Ok(ApiResponse<AuthResponse>.Ok(authResponse, "Token refreshed"));
    }

    [HttpPost("revoke")]
    public async Task<ActionResult<ApiResponse>> Revoke(
        CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var refreshToken = Request.Cookies["refreshToken"];

        if (string.IsNullOrEmpty(refreshToken))
        {
            return BadRequest(ApiResponse.Fail("Refresh token not found"));
        }

        var result = await sender.Send(
            new RevokeTokenCommand(refreshToken, ipAddress),
            cancellationToken);

        if (!result)
        {
            return BadRequest(ApiResponse.Fail("Invalid or already revoked token"));
        }

        // Clear the refresh token cookie
        ClearRefreshTokenCookie();

        return Ok(ApiResponse.Ok("Token revoked successfully"));
    }

    private void SetRefreshTokenCookie(string refreshToken, DateTime expiresAt)
    {
        Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = expiresAt,
            Path = "/api/auth/refresh"
        });
    }

    private void ClearRefreshTokenCookie()
    {
        Response.Cookies.Delete("refreshToken", new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/api/auth/refresh"
        });
    }
}

public record LoginRequest(string Email, string Password);

public record RegisterRequest(string Username, string Email, string Password);