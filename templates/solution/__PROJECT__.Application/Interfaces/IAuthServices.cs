using System.Security.Claims;

using __PROJECT__.Application.DTOs.Auth;
using __PROJECT__.Domain.Entities;

namespace __PROJECT__.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request);
    Task<LoginResponseDto> RefreshTokenAsync(string refreshToken);
    Task LogoutAsync(string userId, string refreshToken);
    Task<UserResponseDto?> GetUserByIdAsync(string userId);
    Task<UserResponseDto?> UpdateProfileAsync(string userId, UpdateProfileDto request);
    Task ChangeFirstLoginPasswordAsync(FirstLoginPasswordDto request);
    Task ChangePasswordAsync(string userId, ChangePasswordDto request);
    Task<SessionTimeResponseDto> GetSessionTimeRemainingAsync(string userId);
}

public interface IJWTService
{
    string GenerateAccessToken(um_user_app user, IEnumerable<string> roles, string sessionId);
    string GenerateRefreshToken();
    ClaimsPrincipal? ValidateToken(string token);
    string? GetUserIdFromToken(string token);
}

public interface IPasswordHasherService
{
    string HashPassword(string password);
    bool ValidatePassword(string enteredPassword, string storedHash);
}

public interface ICurrentUserService
{
    string? UserId { get; }
    string? UserName { get; }
    string? Email { get; }
    string? IpAddress { get; }
    bool IsAuthenticated { get; }
}
