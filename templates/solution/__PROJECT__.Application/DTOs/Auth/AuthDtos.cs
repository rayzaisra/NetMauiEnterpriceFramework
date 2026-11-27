namespace __PROJECT__.Application.DTOs.Auth;

public class LoginRequestDto
{
    public string UserId { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool RememberMe { get; set; }
}

public class LoginResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime AccessTokenExpiry { get; set; }
    public DateTime RefreshTokenExpiry { get; set; }
    public UserResponseDto User { get; set; } = new();
    public string UrlDestination { get; set; } = "login";
    public bool IsPasswordExpiry { get; set; }
    public bool RequiresPasswordChange { get; set; }
    public string PasswordWarning { get; set; } = string.Empty;
}

public class FirstLoginPasswordDto
{
    public string UserId { get; set; } = string.Empty;
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmNewPassword { get; set; } = string.Empty;
}

public class RefreshTokenRequestDto
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class LogoutRequestDto
{
    public string? RefreshToken { get; set; }
}

public class ChangePasswordDto
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmNewPassword { get; set; } = string.Empty;
}

public class UpdateProfileDto
{
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? GenderId { get; set; }
}

public class UserResponseDto
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? RoleId { get; set; }
    public string? RoleName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? GenderId { get; set; }
    public DateTime LastLoginDate { get; set; }
    public DateTime? LastPasswordChangeDate { get; set; }
    public string? LastIpAddress { get; set; }
    public bool CanAddEdit { get; set; }
    public bool CanDelete { get; set; }
    public bool CanReadOnly { get; set; }
    public bool IsActive { get; set; }
}

public class SessionTimeResponseDto
{
    public int RemainingSeconds { get; set; }
    public int IdleRemainingSeconds { get; set; }
    public int IdleTimeoutMinutes { get; set; }
}
