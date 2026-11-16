using Microsoft.EntityFrameworkCore;

using __PROJECT__.Application.DTOs.Notification;
using __PROJECT__.Application.Interfaces.Queries;
using __PROJECT__.Infrastructure.Data;

namespace __PROJECT__.Infrastructure.Queries;

public class NotificationQueryService : INotificationQueryService
{
    private readonly __PROJECT__DbContext _context;

    public NotificationQueryService(__PROJECT__DbContext context) => _context = context;

    public async Task<List<NotificationDto>> GetRecentAsync(
        string userId,
        int top = 20,
        CancellationToken cancellationToken = default)
    {
        var rows = await (from notification in _context.tb_notification
                          join type in _context.rf_notification_type
                              on notification.notification_type_id equals type.id into typeJoin
                          from type in typeJoin.DefaultIfEmpty()
                          where notification.receiver_user_id == userId
                                && notification.isDeleted != true
                          orderby notification.createdDate descending
                          select new { notification, type })
            .Take(Math.Clamp(top, 1, 100))
            .ToListAsync(cancellationToken);

        return rows.Select(row => new NotificationDto
        {
            Id = row.notification.id,
            NotificationTypeId = row.notification.notification_type_id,
            Type = row.type != null ? row.type.short_text : null,
            ShortText = Render(row.type?.short_text, row.notification.reff_id),
            LongText = Render(row.type?.long_text, row.notification.reff_id),
            ReffId = row.notification.reff_id,
            Href = !string.IsNullOrWhiteSpace(row.notification.reff_id)
                ? row.notification.reff_id
                : Render(row.type?.href, row.notification.reff_id),
            IsSeen = row.notification.is_seen == true,
            CreatedDate = row.notification.createdDate,
        }).ToList();
    }

    private static string? Render(string? template, string? reffId)
        => template?.Replace("@[tboId]", reffId ?? string.Empty, StringComparison.OrdinalIgnoreCase);

    public Task<int> GetUnreadCountAsync(
        string userId,
        CancellationToken cancellationToken = default)
        => _context.tb_notification.CountAsync(
            e => e.receiver_user_id == userId
                 && e.is_seen != true
                 && e.isDeleted != true,
            cancellationToken);
}
