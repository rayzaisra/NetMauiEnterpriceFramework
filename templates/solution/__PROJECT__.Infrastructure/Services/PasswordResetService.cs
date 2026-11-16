using System.Security.Cryptography;
using System.Text;

using Microsoft.Extensions.Configuration;

using __PROJECT__.Application.Common;
using __PROJECT__.Application.DTOs.Auth;
using __PROJECT__.Application.DTOs.General;
using __PROJECT__.Application.Interfaces;
using __PROJECT__.Domain.Entities;
using __PROJECT__.Domain.Enums;

namespace __PROJECT__.Infrastructure.Services;

public class PasswordResetService : IPasswordResetService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasherService _passwordHasher;
    private readonly INotificationService _notificationService;
    private readonly IConfiguration _configuration;

    public PasswordResetService(
        IUnitOfWork unitOfWork,
        IPasswordHasherService passwordHasher,
        INotificationService notificationService,
        IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _notificationService = notificationService;
        _configuration = configuration;
    }

    public async Task<PasswordResetRequestResult> RequestAsync(string userIdOrEmail, string? ipAddress, string? publicBaseUrl = null)
    {
        var result = new PasswordResetRequestResult();
        var identifier = (userIdOrEmail ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(identifier))
        {
            return result;
        }

        var user = await _unitOfWork.UmUserApp.FindByIdOrEmailAsync(identifier);

        if (user == null || user.isActive != true || user.isDeleted == true)
        {
            return result;
        }

        result.UserFound = true;

        var now = DateTime.Now;
        var oldTokens = await _unitOfWork.PasswordResetTokens.GetUnusedByUserAsync(user.user_id);
        foreach (var oldToken in oldTokens)
        {
            oldToken.used_at = now;
        }

        var rawToken = GenerateToken();
        var expiresAt = DateTime.UtcNow.AddMinutes(30);
        await _unitOfWork.PasswordResetTokens.AddAsync(new tb_password_reset_token
        {
            id = Guid.NewGuid(),
            user_id = user.user_id,
            token_hash = HashToken(rawToken),
            expires_at = expiresAt,
            created_by_ip = ipAddress,
            created_date = now,
        });

        var link = BuildResetLink(rawToken, publicBaseUrl);
        var template = await _unitOfWork.EmailTemplates.GetActiveByKeyAsync("FORGOT_PASSWORD");
        var subject = template?.subject ?? "Reset your password";
        var body = template?.body_html
                   ?? "<p>Hello {{full_name}},</p><p>Use the link below to reset your password.</p><p><a href=\"{{link}}\">Reset Password</a></p><p>This link expires in 30 minutes.</p>";
        body = body
            .Replace("{{full_name}}", user.full_name ?? user.user_id, StringComparison.OrdinalIgnoreCase)
            .Replace("{{user_id}}", user.user_id, StringComparison.OrdinalIgnoreCase)
            .Replace("{{link}}", link, StringComparison.OrdinalIgnoreCase);
        var text = $"Reset your password by opening this link:\n\n{link}\n\nThis link expires in 30 minutes. If you did not request this, ignore this message.";

        var provider = await _unitOfWork.EmailProviders.GetActiveProviderAsync();

        var canEmail = provider != null && !string.IsNullOrWhiteSpace(user.email);
        var outbox = new tb_email_outbox
        {
            id = Guid.NewGuid(),
            provider_type = canEmail ? provider!.provider_type : null,
            recipient = user.email ?? string.Empty,
            subject = subject,
            body_html = body,
            body_text = text,
            reference_type = "PASSWORD_RESET",
            reference_id = user.user_id,
            status = canEmail ? "Pending" : "AdminFallback",
            next_attempt_at = canEmail ? DateTime.UtcNow : null,
            created_date = now,
        };
        await _unitOfWork.EmailOutbox.AddAsync(outbox);

        result = new PasswordResetRequestResult
        {
            OutboxId = canEmail ? outbox.id : null,
            DeliveryConfigured = canEmail,
            UserFound = true,
        };

        await _unitOfWork.SaveChangesAsync();

        if (!canEmail)
        {
            await NotifyAdminsAsync(user, link);
        }

        return result;
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var hash = HashToken(token);
        return await _unitOfWork.PasswordResetTokens.AnyValidByHashAsync(hash);
    }

    public async Task<BaseGeneralResponse> ResetAsync(string token, string newPassword, string confirmNewPassword)
    {
        var policyError = PasswordPolicy.Validate(newPassword);
        if (policyError != null)
        {
            return Fail(policyError);
        }

        if (!string.Equals(newPassword, confirmNewPassword, StringComparison.Ordinal))
        {
            return Fail("New password and confirmation do not match.");
        }

        var hash = HashToken(token);
        var resetToken = await _unitOfWork.PasswordResetTokens.GetValidByHashAsync(hash);

        if (resetToken == null)
        {
            return Fail("Reset token is invalid or has expired.");
        }

        var user = await _unitOfWork.UmUserApp.GetByIdAsync(resetToken.user_id);
        if (user == null || user.isActive != true || user.isDeleted == true)
        {
            return Fail("User is not available.");
        }

        var now = DateTime.Now;
        user.password = _passwordHasher.HashPassword(newPassword);
        user.last_password_change_date = now;
        user.modifiedDate = now;

        var flag = await _unitOfWork.UmUserAppFlag.GetByUserIdAsync(user.user_id);
        if (flag != null)
        {
            flag.is_first_login = false;
            flag.is_login = false;
            flag.session_id = null;
            flag.last_password_changed_date = now;
            flag.failed_password_attempt_count = 0;
            flag.failed_password_attempt_date = null;
        }

        await _unitOfWork.RefreshTokens.RevokeAllUserTokensAsync(user.user_id);
        await _unitOfWork.UmUserAppSession.RemoveAsync(user.user_id);

        resetToken.used_at = now;
        await _unitOfWork.SaveChangesAsync();
        return Success("Password reset successfully. Please sign in with your new password.");
    }

    private async Task NotifyAdminsAsync(um_user_app user, string link)
    {
        var adminIds = await _unitOfWork.UmUserGroup.GetActiveAdministratorUserIdsAsync();

        foreach (var adminId in adminIds)
        {
            await _notificationService.CreateAsync(
                adminId,
                "Password reset request",
                $"User {user.user_id} requested a password reset. Link: {link}",
                link,
                NotificationTypeIds.PasswordReset);
        }
    }

    private string BuildResetLink(string token, string? publicBaseUrl)
    {
        var baseUrl = !string.IsNullOrWhiteSpace(publicBaseUrl)
            ? publicBaseUrl.TrimEnd('/')
            : (_configuration["PasswordReset:PublicBaseUrl"] ?? "https://localhost:44353").TrimEnd('/');
        return $"{baseUrl}/reset-password?token={Uri.EscapeDataString(token)}";
    }

    private static string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static string HashToken(string token)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();

    private static BaseGeneralResponse Success(string message) => new()
    {
        Success = true,
        Code = Const.SUCCESS_CODE,
        Message = message,
    };

    private static BaseGeneralResponse Fail(string message) => new()
    {
        Success = false,
        Code = Const.ERROR_CODE,
        Message = message,
    };
}
