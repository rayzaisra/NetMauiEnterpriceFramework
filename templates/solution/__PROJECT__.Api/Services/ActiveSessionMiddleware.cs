using System.Security.Claims;
using System.Text.Json;

using Microsoft.Extensions.Configuration;

using __PROJECT__.Application.DTOs.General;
using __PROJECT__.Application.Interfaces;
using __PROJECT__.Domain.Enums;

namespace __PROJECT__.Api.Services;

public class ActiveSessionMiddleware
{
    private static readonly TimeSpan LastActivityWriteThrottle = TimeSpan.FromMinutes(1);
    private readonly RequestDelegate _next;

    public ActiveSessionMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(
        HttpContext context,
        IUnitOfWork unitOfWork,
        IConfiguration configuration,
        IUserSessionNotifier sessionNotifier)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var sessionId = context.User.FindFirst("session_id")?.Value;

            if (!string.IsNullOrWhiteSpace(userId) && !string.IsNullOrWhiteSpace(sessionId))
            {
                var flag = await unitOfWork.UmUserAppFlag.GetByUserIdAsync(userId);
                if (flag == null
                    || flag.is_login != true
                    || !string.Equals(flag.session_id, sessionId, StringComparison.Ordinal))
                {
                    await WriteUnauthorizedAsync(
                        context,
                        "Sesi Anda berakhir karena akun ini login dari browser lain.",
                        "Session revoked.");
                    return;
                }

                var session = await unitOfWork.UmUserAppSession.GetByUserIdAsync(userId);
                var idleTimeoutMinutes = GetIdleTimeoutMinutes(configuration);
                var idleTimeout = TimeSpan.FromMinutes(idleTimeoutMinutes);
                var lastActivityUtc = NormalizeUtc(session?.last_activity);
                if (lastActivityUtc.HasValue && DateTime.UtcNow - lastActivityUtc.Value > idleTimeout)
                {
                    await ExpireIdleSessionAsync(unitOfWork, sessionNotifier, userId, sessionId, flag);
                    await WriteUnauthorizedAsync(
                        context,
                        "Sesi Anda berakhir karena tidak ada aktivitas.",
                        "Session idle timeout.");
                    return;
                }

                if (session != null && !IsSessionTimeRequest(context.Request))
                {
                    var shouldTouch = !lastActivityUtc.HasValue
                        || DateTime.UtcNow - lastActivityUtc.Value >= LastActivityWriteThrottle;
                    if (shouldTouch)
                    {
                        session.last_activity = DateTime.UtcNow;
                        await unitOfWork.SaveChangesAsync(context.RequestAborted);
                    }
                }
            }
        }

        await _next(context);
    }

    private static async Task ExpireIdleSessionAsync(
        IUnitOfWork unitOfWork,
        IUserSessionNotifier sessionNotifier,
        string userId,
        string sessionId,
        __PROJECT__.Domain.Entities.um_user_app_flag flag)
    {
        await unitOfWork.RefreshTokens.RevokeAllUserTokensAsync(userId);
        flag.is_login = false;
        flag.session_id = null;
        await unitOfWork.UmUserAppFlag.UpdateAsync(flag);
        await unitOfWork.UmUserAppSession.RemoveAsync(userId);
        await unitOfWork.SaveChangesAsync();
        await sessionNotifier.NotifySessionIdleExpiredAsync(userId, sessionId);
    }

    private static async Task WriteUnauthorizedAsync(HttpContext context, string message, string error)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";
        var response = new ApiResponse<string>
        {
            Success = false,
            Message = message,
            Code = Const.ERROR_CODE,
            Errors = new List<ApiError>
            {
                new() { Field = string.Empty, Message = error },
            },
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(
            response,
            new JsonSerializerOptions(JsonSerializerDefaults.Web)));
    }

    internal static int GetIdleTimeoutMinutes(IConfiguration configuration)
    {
        var value = configuration["Jwt:IdleTimeoutMinutes"];
        return int.TryParse(value, out var minutes) && minutes > 0 ? minutes : 15;
    }

    internal static DateTime? NormalizeUtc(DateTime? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        return value.Value.Kind switch
        {
            DateTimeKind.Utc => value.Value,
            DateTimeKind.Local => value.Value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc),
        };
    }

    private static bool IsSessionTimeRequest(HttpRequest request)
        => request.Path.Value?.EndsWith("/api/auth/session-time", StringComparison.OrdinalIgnoreCase) == true;
}
