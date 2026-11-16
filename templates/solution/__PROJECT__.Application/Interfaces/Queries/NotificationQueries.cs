using __PROJECT__.Application.DTOs.Notification;

namespace __PROJECT__.Application.Interfaces.Queries;

public interface INotificationQueryService
{
    Task<List<NotificationDto>> GetRecentAsync(
        string userId,
        int top = 20,
        CancellationToken cancellationToken = default);

    Task<int> GetUnreadCountAsync(
        string userId,
        CancellationToken cancellationToken = default);
}
