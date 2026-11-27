namespace __PROJECT__.Application.Interfaces;

public interface IUserSessionNotifier
{
    Task NotifySessionRevokedAsync(string userId, string? sessionId);
    Task NotifySessionIdleExpiredAsync(string userId, string? sessionId);
}
