using Microsoft.EntityFrameworkCore;

using __PROJECT__.Domain.Entities;
using __PROJECT__.Domain.Enums;
using __PROJECT__.Infrastructure.Data;

namespace __PROJECT__.Infrastructure.Data;

public static class NotificationSeeder
{
    public static async Task SeedAsync(__PROJECT__DbContext context, CancellationToken cancellationToken = default)
    {
        var now = DateTime.Now;
        await EnsureTypeAsync(
            context,
            NotificationTypeIds.Approval,
            "Approval",
            "There is an approval update.",
            isSendMail: false,
            now,
            cancellationToken);

        await EnsureTypeAsync(
            context,
            NotificationTypeIds.Export,
            "Export ready",
            "Your export is ready to download.",
            isSendMail: false,
            now,
            cancellationToken);

        await EnsureTypeAsync(
            context,
            NotificationTypeIds.PasswordReset,
            "Password reset request",
            "A password reset was requested. Link: @[tboId]",
            isSendMail: false,
            now,
            cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureTypeAsync(
        __PROJECT__DbContext context,
        string id,
        string shortText,
        string longText,
        bool isSendMail,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (await context.rf_notification_type.AnyAsync(e => e.id == id, cancellationToken))
        {
            return;
        }

        await context.rf_notification_type.AddAsync(new rf_notification_type
        {
            id = id,
            short_text = shortText,
            long_text = longText,
            href = null,
            is_send_mail = isSendMail,
            is_send_web = true,
            statusApproval = Const.StatusApproval.APPROVED,
            approvalTypeId = Const.ApprovalType.ADD,
            isLocked = false,
            isDefault = false,
            isActive = true,
            isDeleted = false,
            approvedBy = "system",
            approvedDate = now,
            createdBy = "system",
            createdDate = now,
        }, cancellationToken);
    }
}
