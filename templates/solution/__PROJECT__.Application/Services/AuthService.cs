using System.Security.Cryptography;
using System.Text;

using Microsoft.Extensions.Configuration;

using __PROJECT__.Application.Common;
using __PROJECT__.Application.DTOs.Auth;
using __PROJECT__.Application.Exceptions;
using __PROJECT__.Application.Interfaces;
using __PROJECT__.Domain.Entities;
using __PROJECT__.Domain.Enums;

namespace __PROJECT__.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJWTService _jwtService;
    private readonly IPasswordHasherService _passwordHasher;
    private readonly ICurrentUserService _currentUser;
    private readonly IConfiguration _configuration;
    private readonly IUserSessionNotifier _sessionNotifier;

    public AuthService(
        IUnitOfWork unitOfWork,
        IJWTService jwtService,
        IPasswordHasherService passwordHasher,
        ICurrentUserService currentUser,
        IConfiguration configuration,
        IUserSessionNotifier sessionNotifier)
    {
        _unitOfWork = unitOfWork;
        _jwtService = jwtService;
        _passwordHasher = passwordHasher;
        _currentUser = currentUser;
        _configuration = configuration;
        _sessionNotifier = sessionNotifier;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
    {
        var user = await _unitOfWork.UmUserApp.GetByIdAsync(request.UserId);
        if (user == null || user.isActive != true || user.isDeleted == true)
        {
            throw new AuthenticationException("Invalid Username / Password.");
        }

        var userGroup = await _unitOfWork.UmUserGroup.GetByIdAsync(user.user_id);
        var role = userGroup?.group_id;
        if (string.IsNullOrWhiteSpace(role))
        {
            throw new AuthenticationException("User has no assigned role. Please contact administrator.");
        }

        var flag = await _unitOfWork.UmUserAppFlag.GetByUserIdAsync(user.user_id);
        if (flag == null)
        {
            flag = new um_user_app_flag
            {
                user_id = user.user_id,
                is_first_login = true,
                is_locked = false,
                is_login = false,
                failed_password_attempt_count = 0,
                ip_address = _currentUser.IpAddress,
                last_login_date = DateTime.Now,
            };
            await _unitOfWork.UmUserAppFlag.AddAsync(flag);
            await _unitOfWork.SaveChangesAsync();
        }

        var sessionId = Guid.NewGuid().ToString("N");
        var isValid = _passwordHasher.ValidatePassword(request.Password, user.password ?? string.Empty);

        if (!isValid)
        {
            if (flag.failed_password_attempt_date?.Date != DateTime.Now.Date)
            {
                flag.failed_password_attempt_count = 0;
            }

            flag.failed_password_attempt_count = (flag.failed_password_attempt_count ?? 0) + 1;
            flag.failed_password_attempt_date = DateTime.Now;

            var isAdmin = role == Const.DEFAULT_GROUP_ID_ADMINISTRATOR_CODE;
            if (flag.failed_password_attempt_count >= 5 && !isAdmin)
            {
                flag.is_locked = true;
                flag.last_locked_out_date = DateTime.Now;
            }

            await _unitOfWork.UmUserAppFlag.UpdateAsync(flag);
            await _unitOfWork.SaveChangesAsync();

            throw new AuthenticationException("Invalid Username / Password.");
        }

        if (flag.is_locked == true)
        {
            throw new AuthenticationException("Account is locked. Please contact administrator.");
        }

        if (flag.is_first_login == true)
        {
            flag.failed_password_attempt_count = 0;
            flag.failed_password_attempt_date = null;
            await _unitOfWork.UmUserAppFlag.UpdateAsync(flag);
            await _unitOfWork.SaveChangesAsync();

            return new LoginResponseDto
            {
                User = await MapToUserDto(user),
                UrlDestination = "firstlogin",
                RequiresPasswordChange = true,
                PasswordWarning = "Please change the default password before signing in.",
            };
        }

        var previousSessionId = flag.session_id;
        flag.failed_password_attempt_count = 0;
        flag.failed_password_attempt_date = null;
        flag.last_login_date = DateTime.Now;
        flag.is_login = true;
        flag.ip_address = _currentUser.IpAddress;
        flag.session_id = sessionId;

        await _unitOfWork.UmUserAppFlag.UpdateAsync(flag);
        await _unitOfWork.UmUserAppSession.UpsertAsync(user.user_id, sessionId);
        await _unitOfWork.RefreshTokens.RevokeAllUserTokensAsync(user.user_id);
        await _unitOfWork.SaveChangesAsync();

        if (!string.Equals(previousSessionId, sessionId, StringComparison.Ordinal))
        {
            await _sessionNotifier.NotifySessionRevokedAsync(user.user_id, previousSessionId);
        }

        var accessToken = _jwtService.GenerateAccessToken(user, new[] { role }, sessionId);
        var rawRefreshToken = _jwtService.GenerateRefreshToken();
        var refreshExpiry = request.RememberMe
            ? DateTime.UtcNow.AddDays(GetConfigInt("Jwt:RememberMeExpirationDays", 30))
            : DateTime.UtcNow.AddHours(GetConfigInt("Jwt:SessionRefreshTokenHours", 12));

        var refreshEntity = new um_user_refresh_token
        {
            user_id = user.user_id,
            refresh_token = HashRefreshToken(rawRefreshToken),
            expiry_date = refreshExpiry,
            is_revoked = false,
            remember_me = request.RememberMe,
            createdDate = DateTime.Now,
            isActive = true,
            isDeleted = false,
        };

        await _unitOfWork.RefreshTokens.AddAsync(refreshEntity);
        await _unitOfWork.SaveChangesAsync();

        var response = new LoginResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = rawRefreshToken,
            AccessTokenExpiry = DateTime.UtcNow.AddMinutes(GetConfigInt("Jwt:AccessTokenExpirationMinutes", 15)),
            RefreshTokenExpiry = refreshEntity.expiry_date,
            User = await MapToUserDto(user),
            UrlDestination = "login",
            IsPasswordExpiry = false,
            PasswordWarning = string.Empty,
        };

        ApplyPasswordExpiryPolicy(user, flag, response);
        return response;
    }

    public async Task<LoginResponseDto> RefreshTokenAsync(string refreshToken)
    {
        var tokenHash = HashRefreshToken(refreshToken);
        var storedToken = await _unitOfWork.RefreshTokens.GetByTokenHashAsync(tokenHash);

        if (storedToken == null || storedToken.is_revoked || storedToken.expiry_date < DateTime.UtcNow)
        {
            throw new AuthenticationException("Invalid or expired refresh token.");
        }

        var user = await _unitOfWork.UmUserApp.GetByIdAsync(storedToken.user_id ?? string.Empty);
        if (user == null || user.isActive != true || user.isDeleted == true)
        {
            throw new AuthenticationException("Account is deactivated.");
        }

        var userGroup = await _unitOfWork.UmUserGroup.GetByIdAsync(user.user_id);
        var roles = string.IsNullOrWhiteSpace(userGroup?.group_id)
            ? Array.Empty<string>()
            : new[] { userGroup!.group_id! };

        storedToken.is_revoked = true;
        storedToken.modifiedDate = DateTime.Now;
        await _unitOfWork.RefreshTokens.UpdateAsync(storedToken);

        var flag = await _unitOfWork.UmUserAppFlag.GetByUserIdAsync(user.user_id);
        var sessionId = flag?.session_id ?? Guid.NewGuid().ToString("N");
        await _unitOfWork.UmUserAppSession.UpsertAsync(user.user_id, sessionId);
        var accessToken = _jwtService.GenerateAccessToken(user, roles, sessionId);
        var newRawRefreshToken = _jwtService.GenerateRefreshToken();

        var refreshExpiry = storedToken.remember_me == true
            ? DateTime.UtcNow.AddDays(GetConfigInt("Jwt:RememberMeExpirationDays", 30))
            : DateTime.UtcNow.AddHours(GetConfigInt("Jwt:SessionRefreshTokenHours", 12));
        var newRefreshEntity = new um_user_refresh_token
        {
            user_id = user.user_id,
            refresh_token = HashRefreshToken(newRawRefreshToken),
            expiry_date = refreshExpiry,
            is_revoked = false,
            remember_me = storedToken.remember_me,
            createdDate = DateTime.Now,
            isActive = true,
            isDeleted = false,
        };

        await _unitOfWork.RefreshTokens.AddAsync(newRefreshEntity);
        await _unitOfWork.SaveChangesAsync();

        return new LoginResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = newRawRefreshToken,
            AccessTokenExpiry = DateTime.UtcNow.AddMinutes(GetConfigInt("Jwt:AccessTokenExpirationMinutes", 15)),
            RefreshTokenExpiry = newRefreshEntity.expiry_date,
            User = await MapToUserDto(user),
        };
    }

    public async Task LogoutAsync(string userId, string refreshToken)
    {
        await _unitOfWork.RefreshTokens.RevokeAllUserTokensAsync(userId);
        var flag = await _unitOfWork.UmUserAppFlag.GetByUserIdAsync(userId);
        if (flag != null)
        {
            flag.is_login = false;
            flag.session_id = null;
            await _unitOfWork.UmUserAppFlag.UpdateAsync(flag);
        }

        await _unitOfWork.UmUserAppSession.RemoveAsync(userId);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<UserResponseDto?> GetUserByIdAsync(string userId)
    {
        var user = await _unitOfWork.UmUserApp.GetByIdAsync(userId);
        return user == null ? null : await MapToUserDto(user);
    }

    public async Task<UserResponseDto?> UpdateProfileAsync(string userId, UpdateProfileDto request)
    {
        var user = await _unitOfWork.UmUserApp.GetByIdAsync(userId);
        if (user == null)
        {
            throw new AuthenticationException("User not found.");
        }

        user.full_name = request.FullName;
        user.email = request.Email;
        user.phone_number = request.PhoneNumber;
        user.gender_id = request.GenderId;
        user.modifiedDate = DateTime.Now;
        await _unitOfWork.UmUserApp.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return await MapToUserDto(user);
    }

    public async Task ChangeFirstLoginPasswordAsync(FirstLoginPasswordDto request)
    {
        var user = await _unitOfWork.UmUserApp.GetByIdAsync(request.UserId);
        if (user == null || user.isActive != true || user.isDeleted == true)
        {
            throw new AuthenticationException("User not found.");
        }

        if (!_passwordHasher.ValidatePassword(request.CurrentPassword, user.password ?? string.Empty))
        {
            throw new AuthenticationException("Old password is incorrect.");
        }

        if (!string.Equals(request.NewPassword, request.ConfirmNewPassword, StringComparison.Ordinal))
        {
            throw new AuthenticationException("New password and confirmation do not match.");
        }

        var policyError = PasswordPolicy.Validate(request.NewPassword);
        if (policyError != null)
        {
            throw new AuthenticationException(policyError);
        }

        var flag = await _unitOfWork.UmUserAppFlag.GetByUserIdAsync(user.user_id);
        if (flag?.is_first_login != true)
        {
            throw new AuthenticationException("First login password change is not required.");
        }

        var now = DateTime.Now;
        user.password = _passwordHasher.HashPassword(request.NewPassword);
        user.last_password_change_date = now;
        user.modifiedDate = now;
        await _unitOfWork.UmUserApp.UpdateAsync(user);

        flag.is_first_login = false;
        flag.last_password_changed_date = now;
        flag.failed_password_attempt_count = 0;
        flag.failed_password_attempt_date = null;
        await _unitOfWork.UmUserAppFlag.UpdateAsync(flag);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task ChangePasswordAsync(string userId, ChangePasswordDto request)
    {
        var user = await _unitOfWork.UmUserApp.GetByIdAsync(userId);
        if (user == null)
        {
            throw new AuthenticationException("User not found.");
        }

        if (!_passwordHasher.ValidatePassword(request.CurrentPassword, user.password ?? string.Empty))
        {
            throw new AuthenticationException("Current password is incorrect.");
        }

        if (request.NewPassword != request.ConfirmNewPassword)
        {
            throw new AuthenticationException("New password and confirmation do not match.");
        }

        var policyError = PasswordPolicy.Validate(request.NewPassword);
        if (policyError != null)
        {
            throw new AuthenticationException(policyError);
        }

        user.password = _passwordHasher.HashPassword(request.NewPassword);
        user.last_password_change_date = DateTime.Now;
        user.modifiedDate = DateTime.Now;
        await _unitOfWork.UmUserApp.UpdateAsync(user);

        var flag = await _unitOfWork.UmUserAppFlag.GetByUserIdAsync(userId);
        if (flag != null)
        {
            flag.is_first_login = false;
            flag.last_password_changed_date = DateTime.Now;
            await _unitOfWork.UmUserAppFlag.UpdateAsync(flag);
        }

        await _unitOfWork.SaveChangesAsync();
    }

    private async Task<UserResponseDto> MapToUserDto(um_user_app user)
    {
        var userGroup = await _unitOfWork.UmUserGroup.GetByIdAsync(user.user_id);
        var flag = await _unitOfWork.UmUserAppFlag.GetByUserIdAsync(user.user_id);
        var roleName = userGroup?.group_id == null
            ? null
            : await _unitOfWork.UmUserGroup.GetGroupNameAsync(userGroup.group_id);

        var isSuperAdmin = string.Equals(userGroup?.group_id, "00", StringComparison.Ordinal);

        return new UserResponseDto
        {
            UserId = user.user_id,
            UserName = user.user_id,
            FullName = user.full_name,
            RoleId = userGroup?.group_id,
            RoleName = roleName,
            Email = user.email,
            PhoneNumber = user.phone_number,
            GenderId = user.gender_id,
            LastLoginDate = flag?.last_login_date ?? DateTime.Now,
            LastPasswordChangeDate = user.last_password_change_date,
            LastIpAddress = flag?.ip_address,
            CanAddEdit = isSuperAdmin || userGroup?.can_add_edit == true,
            CanDelete = isSuperAdmin || userGroup?.can_delete == true,
            CanReadOnly = !isSuperAdmin && userGroup?.can_read_only == true,
            IsActive = user.isActive == true,
        };
    }

    private void ApplyPasswordExpiryPolicy(um_user_app user, um_user_app_flag flag, LoginResponseDto response)
    {
        var expiryDays = GetConfigInt("AppSettings:PasswordExpiryDays", 90);
        var warningDays = GetConfigInt("AppSettings:PasswordWarningDays", 10);

        if (expiryDays > 0)
        {
            if (user.last_password_change_date.HasValue)
            {
                var lastChange = user.last_password_change_date.Value;
                var expiryDate = lastChange.AddDays(expiryDays);
                var warningDate = expiryDate.AddDays(-warningDays);
                var forceChangeDate = expiryDate.AddDays(-1);

                if (DateTime.Now >= expiryDate)
                {
                    response.IsPasswordExpiry = true;
                    response.PasswordWarning = "Your password has expired. Please contact support to reset your password.";
                    response.UrlDestination = "login";
                }
                else if (DateTime.Now >= forceChangeDate)
                {
                    response.PasswordWarning = $"Your password will expire in {(expiryDate - DateTime.Now).Days} days. Please change it soon.";
                    response.UrlDestination = "changepassword";
                }
                else if (DateTime.Now >= warningDate)
                {
                    response.PasswordWarning = $"Your password will expire in {(expiryDate - DateTime.Now).Days} days. Please change it soon.";
                }
            }
            else
            {
                response.IsPasswordExpiry = true;
                response.PasswordWarning = "Your password does not have a set expiry. Please change your password.";
                response.UrlDestination = "changepassword";
            }
        }

        if (flag.is_first_login == true)
        {
            response.UrlDestination = "changepassword";
            response.PasswordWarning = "Please change your password on first login.";
        }
    }

    private int GetConfigInt(string key, int defaultValue)
    {
        return int.TryParse(_configuration[key], out var value) ? value : defaultValue;
    }

    private static string HashRefreshToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}
