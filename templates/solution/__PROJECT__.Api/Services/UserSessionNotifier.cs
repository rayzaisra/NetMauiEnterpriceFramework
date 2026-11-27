using Microsoft.AspNetCore.SignalR;

using __PROJECT__.Api.Hubs;
using __PROJECT__.Application.Interfaces;

namespace __PROJECT__.Api.Services;

public class UserSessionNotifier : IUserSessionNotifier
{
    private readonly IHubContext<UserHub> _hub;

    public UserSessionNotifier(IHubContext<UserHub> hub) => _hub = hub;

    public Task NotifySessionRevokedAsync(string userId, string? sessionId)
        => SendAsync(userId, sessionId, "SessionRevoked", "This account signed in from another browser.");

    public Task NotifySessionIdleExpiredAsync(string userId, string? sessionId)
        => SendAsync(userId, sessionId, "SessionIdleExpired", "Your session ended because there was no activity.");

    private Task SendAsync(string userId, string? sessionId, string eventName, string message)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return Task.CompletedTask;
        }

        return _hub.Clients
            .Group($"{userId}:{sessionId}")
            .SendAsync(eventName, message);
    }
}
