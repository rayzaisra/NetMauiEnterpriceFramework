using __PROJECT__.Application.DTOs.General;
using __PROJECT__.Application.DTOs.Notification;
using __PROJECT__.Application.Interfaces;
using __PROJECT__.Application.Interfaces.Queries;
using __PROJECT__.Domain.Entities;
using __PROJECT__.Domain.Enums;

namespace __PROJECT__.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private const string NotificationEmailSettingCode = "NOTIF_MAIL";
    private const string NotificationEmailTemplateKey = "NOTIFICATION";

    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationQueryService _queryService;
    private readonly INotificationPusher _pusher;
    private readonly IErrorLogService _errorLog;

    public NotificationService(
        IUnitOfWork unitOfWork,
        INotificationQueryService queryService,
        INotificationPusher pusher,
        IErrorLogService errorLog)
    {
        _unitOfWork = unitOfWork;
        _queryService = queryService;
        _pusher = pusher;
        _errorLog = errorLog;
    }

    public Task<List<NotificationDto>> GetRecentAsync(string userId, int top = 20)
        => _queryService.GetRecentAsync(userId, top);

    public Task<int> GetUnreadCountAsync(string userId)
        => _queryService.GetUnreadCountAsync(userId);

    public async Task<BaseGeneralResponse> MarkSeenAsync(string userId, long id)
    {
        var notification = await _unitOfWork.Notifications.GetByIdForUserAsync(id, userId);
        if (notification == null)
        {
            return Fail("Notification not found.");
        }

        notification.is_seen = true;
        notification.modifiedBy = userId;
        notification.modifiedDate = DateTime.Now;
        await _unitOfWork.SaveChangesAsync();
        return Success("Notification marked as read.");
    }

    public async Task<BaseGeneralResponse> MarkAllSeenAsync(string userId)
    {
        var notifications = await _unitOfWork.Notifications.GetUnseenByUserAsync(userId);
        var now = DateTime.Now;

        foreach (var notification in notifications)
        {
            notification.is_seen = true;
            notification.modifiedBy = userId;
            notification.modifiedDate = now;
        }

        await _unitOfWork.SaveChangesAsync();
        return Success("Notifications marked as read.");
    }

    public async Task CreateAsync(
        string userId,
        string shortText,
        string? longText,
        string? href,
        string typeId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return;
        }

        var now = DateTime.Now;
        var actor = "system";

        if (string.IsNullOrWhiteSpace(typeId))
        {
            await _errorLog.LogAsync(
                "Notification",
                "ERROR",
                new InvalidOperationException("Notification type id is required."),
                actor);
            return;
        }

        var type = await _unitOfWork.NotificationTypes.GetByIdAsync(typeId, cancellationToken);
        if (type == null
            || type.isActive != true
            || type.statusApproval != Const.StatusApproval.APPROVED)
        {
            await _errorLog.LogAsync(
                "Notification",
                "ERROR",
                new InvalidOperationException($"Notification type '{typeId}' is not available."),
                actor);
            return;
        }

        var notification = new tb_notification
        {
            receiver_user_id = userId,
            notification_type_id = type.id,
            reff_id = href,
            is_seen = false,
            statusApproval = Const.StatusApproval.APPROVED,
            approvalTypeId = Const.ApprovalType.ADD,
            isLocked = false,
            isDefault = false,
            isActive = true,
            isDeleted = false,
            approvedBy = actor,
            approvedDate = now,
            createdBy = actor,
            createdDate = now,
        };

        await _unitOfWork.Notifications.AddAsync(notification, cancellationToken);

        if (type.is_send_mail == true)
        {
            await QueueEmailAsync(notification, type, shortText, longText, href, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (type.is_send_web == false)
        {
            return;
        }

        try
        {
            await _pusher.PushAsync(userId, new NotificationDto
            {
                Id = notification.id,
                NotificationTypeId = notification.notification_type_id,
                Type = type.short_text,
                ShortText = shortText,
                LongText = longText,
                ReffId = notification.reff_id,
                Href = href,
                IsSeen = false,
                CreatedDate = notification.createdDate,
            });
        }
        catch (Exception ex)
        {
            await _errorLog.LogAsync("Notification", "ERROR", ex);
        }
    }

    private async Task QueueEmailAsync(
        tb_notification notification,
        rf_notification_type type,
        string shortText,
        string? longText,
        string? href,
        CancellationToken cancellationToken)
    {
        try
        {
            var setting = await _unitOfWork.DefaultValues.GetByCodeAsync(
                NotificationEmailSettingCode,
                cancellationToken);
            var emailEnabled = setting != null
                               && setting.isActive != false
                               && string.Equals(setting.def_value, "true", StringComparison.OrdinalIgnoreCase);

            if (!emailEnabled)
            {
                return;
            }

            var user = await _unitOfWork.UmUserApp.GetByIdAsync(notification.receiver_user_id ?? string.Empty);
            if (user == null
                || user.isDeleted == true
                || user.isActive != true
                || string.IsNullOrWhiteSpace(user.email))
            {
                return;
            }

            var provider = await _unitOfWork.EmailProviders.GetActiveProviderAsync(cancellationToken);
            if (provider == null)
            {
                return;
            }

            var template = await _unitOfWork.EmailTemplates.GetActiveByKeyAsync(
                NotificationEmailTemplateKey,
                cancellationToken);
            var link = href ?? string.Empty;
            var subject = template?.subject ?? shortText;
            var bodyHtml = template?.body_html
                           ?? $"<p>{longText ?? shortText}</p>"
                           + (string.IsNullOrWhiteSpace(link) ? string.Empty : $"<p><a href=\"{link}\">Open notification</a></p>");
            var bodyText = template?.body_text
                           ?? $"{longText ?? shortText}"
                           + (string.IsNullOrWhiteSpace(link) ? string.Empty : $"\n\n{link}");

            bodyHtml = ApplyTemplateValues(bodyHtml, user, shortText, longText, link);
            bodyText = ApplyTemplateValues(bodyText, user, shortText, longText, link);
            subject = ApplyTemplateValues(subject, user, shortText, longText, link);

            await _unitOfWork.EmailOutbox.AddAsync(new tb_email_outbox
            {
                id = Guid.NewGuid(),
                provider_type = provider.provider_type,
                recipient = user.email,
                subject = subject,
                body_html = bodyHtml,
                body_text = bodyText,
                reference_type = "NOTIFICATION",
                reference_id = type.id,
                status = "Pending",
                next_attempt_at = DateTime.UtcNow,
                created_date = DateTime.Now,
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            await _errorLog.LogAsync("NotificationEmail", "ERROR", ex);
        }
    }

    private static string ApplyTemplateValues(
        string value,
        um_user_app user,
        string? shortText,
        string? longText,
        string? link)
        => value
            .Replace("{{full_name}}", user.full_name ?? user.user_id, StringComparison.OrdinalIgnoreCase)
            .Replace("{{user_id}}", user.user_id, StringComparison.OrdinalIgnoreCase)
            .Replace("{{short_text}}", shortText ?? string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{{long_text}}", longText ?? string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{{link}}", link ?? string.Empty, StringComparison.OrdinalIgnoreCase);

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
