using System.Security.Claims;

using Hangfire;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

using __PROJECT__.Api.Helpers;
using __PROJECT__.Application.DTOs.Auth;
using __PROJECT__.Application.Exceptions;
using __PROJECT__.Application.Interfaces;
using __PROJECT__.Infrastructure.Services;

namespace __PROJECT__.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IPasswordResetService _passwordResetService;
    private readonly IBackgroundJobClient _backgroundJobs;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly IErrorLogService _errorLog;

    public AuthController(
        IAuthService authService,
        IPasswordResetService passwordResetService,
        IBackgroundJobClient backgroundJobs,
        IConfiguration configuration,
        IWebHostEnvironment environment,
        IErrorLogService errorLog)
    {
        _authService = authService;
        _passwordResetService = passwordResetService;
        _backgroundJobs = backgroundJobs;
        _configuration = configuration;
        _environment = environment;
        _errorLog = errorLog;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        try
        {
            var response = await _authService.LoginAsync(request);
            return Ok(response);
        }
        catch (AuthenticationException ex)
        {
            await _errorLog.LogAsync("Auth", "ERROR", ex);
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto request)
    {
        try
        {
            var response = await _authService.RefreshTokenAsync(request.RefreshToken);
            return Ok(response);
        }
        catch (AuthenticationException ex)
        {
            await _errorLog.LogAsync("Auth", "ERROR", ex);
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
    {
        try
        {
            var result = await _passwordResetService.RequestAsync(
                request.UserIdOrEmail,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                ResolveWebBaseUrl());
            if (!result.UserFound)
            {
                return BadRequest(ApiResponseFactory.Error<string>("User not found."));
            }

            if (result.OutboxId.HasValue)
            {
                _backgroundJobs.Enqueue<EmailOutboxProcessor>(job => job.ProcessPendingAsync());
            }

            return Ok(ApiResponseFactory.Success(
                "Reset instructions have been queued. Check your email or contact an administrator."));
        }
        catch (Exception ex)
        {
            await _errorLog.LogAsync("Auth", "ERROR", ex);
            return Ok(ApiResponseFactory.Success(
                "If the account is registered, password reset instructions have been queued. Contact an administrator if no email arrives."));
        }
    }

    [HttpGet("reset-password/validate")]
    [AllowAnonymous]
    public async Task<IActionResult> ValidateResetPasswordToken([FromQuery] string token)
        => Ok(ApiResponseFactory.Success(
            await _passwordResetService.ValidateTokenAsync(token),
            "Reset token validated successfully."));

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request)
    {
        var result = await _passwordResetService.ResetAsync(request.Token, request.NewPassword, request.ConfirmNewPassword);
        var response = ApiResponseFactory.FromResult(result);
        return result.Success ? Ok(response) : BadRequest(response);
    }

    private string? ResolveWebBaseUrl()
    {
        var origin = Request.Headers.Origin.ToString();
        if (IsAllowedWebOrigin(origin))
        {
            return origin.TrimEnd('/');
        }

        var referer = Request.Headers.Referer.ToString();
        if (Uri.TryCreate(referer, UriKind.Absolute, out var refererUri))
        {
            var refererOrigin = $"{refererUri.Scheme}://{refererUri.Authority}";
            if (IsAllowedWebOrigin(refererOrigin))
            {
                return refererOrigin;
            }
        }

        return null;
    }

    private bool IsAllowedWebOrigin(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return false;
        }

        if (_environment.IsDevelopment()
            && (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                || uri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
                || uri.Host.Equals("::1", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        var allowedOrigins = _configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
        return allowedOrigins.Any(allowed =>
            Uri.TryCreate(allowed, UriKind.Absolute, out var allowedUri)
            && string.Equals(allowedUri.GetLeftPart(UriPartial.Authority), uri.GetLeftPart(UriPartial.Authority), StringComparison.OrdinalIgnoreCase));
    }

    [HttpPost("first-login/change-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ChangeFirstLoginPassword([FromBody] FirstLoginPasswordDto request)
    {
        try
        {
            await _authService.ChangeFirstLoginPasswordAsync(request);
            return Ok(ApiResponseFactory.Success("Password changed successfully. Please sign in with your new password."));
        }
        catch (AuthenticationException ex)
        {
            await _errorLog.LogAsync("Auth", "ERROR", ex);
            return BadRequest(ApiResponseFactory.Error<string>(ex.Message));
        }
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] LogoutRequestDto? request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        await _authService.LogoutAsync(userId, request?.RefreshToken ?? string.Empty);
        return Ok(ApiResponseFactory.Success("Logged out successfully"));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var user = await _authService.GetUserByIdAsync(userId);
        return user == null
            ? NotFound(ApiResponseFactory.NotFound<UserResponseDto>("User not found."))
            : Ok(ApiResponseFactory.Success(user, "User profile retrieved successfully"));
    }

    [HttpPut("profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            var user = await _authService.UpdateProfileAsync(userId, request);
            return user == null
                ? NotFound(ApiResponseFactory.NotFound<UserResponseDto>("User not found."))
                : Ok(ApiResponseFactory.Success(user, "Profile updated successfully"));
        }
        catch (AuthenticationException ex)
        {
            await _errorLog.LogAsync("Auth", "ERROR", ex);
            return BadRequest(ApiResponseFactory.Error<UserResponseDto>(ex.Message));
        }
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            await _authService.ChangePasswordAsync(userId, request);
            return Ok(ApiResponseFactory.Success("Password changed successfully"));
        }
        catch (AuthenticationException ex)
        {
            await _errorLog.LogAsync("Auth", "ERROR", ex);
            return BadRequest(ApiResponseFactory.Error<string>(ex.Message));
        }
    }

    [HttpGet("session-time")]
    [Authorize]
    public async Task<IActionResult> GetSessionTimeRemaining()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var sessionTime = await _authService.GetSessionTimeRemainingAsync(userId);
        return Ok(sessionTime);
    }
}
