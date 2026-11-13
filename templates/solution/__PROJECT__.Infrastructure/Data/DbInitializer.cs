using Microsoft.EntityFrameworkCore;

using __PROJECT__.Application.Interfaces;
using __PROJECT__.Domain.Entities;
using __PROJECT__.Domain.Enums;

namespace __PROJECT__.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(
        __PROJECT__DbContext context,
        IPasswordHasherService passwordHasher,
        string adminUser,
        string adminPassword,
        string adminName,
        string adminEmail,
        CancellationToken cancellationToken = default)
    {
        if (await context.um_user_app.AnyAsync(e => e.user_id == adminUser, cancellationToken))
        {
            await LookupSeeder.SeedAsync(context, cancellationToken);
            await MenuSeeder.SeedAsync(context, cancellationToken);
            await ParameterSeeder.SeedAsync(context, cancellationToken);
            await NotificationSeeder.SeedAsync(context, cancellationToken);
            return;
        }

        var now = DateTime.Now;
        var adminGroup = await context.um_group_app
            .FirstOrDefaultAsync(e => e.group_id == Const.DEFAULT_GROUP_ID_ADMINISTRATOR_CODE, cancellationToken);

        if (adminGroup == null)
        {
            adminGroup = new um_group_app
            {
                group_id = Const.DEFAULT_GROUP_ID_ADMINISTRATOR_CODE,
                group_name = "Administrator",
                statusApproval = Const.StatusApproval.APPROVED,
                isActive = true,
                isDeleted = false,
                isLocked = false,
                isDefault = false,
                createdBy = "system",
                createdDate = now,
            };
            await context.um_group_app.AddAsync(adminGroup, cancellationToken);
        }

        var admin = new um_user_app
        {
            user_id = adminUser,
            full_name = adminName,
            password = passwordHasher.HashPassword(adminPassword),
            email = adminEmail,
            last_password_change_date = now,
            statusApproval = Const.StatusApproval.APPROVED,
            isActive = true,
            isDeleted = false,
            isLocked = false,
            isDefault = false,
            createdBy = "system",
            createdDate = now,
        };
        await context.um_user_app.AddAsync(admin, cancellationToken);

        await context.um_user_group.AddAsync(new um_user_group
        {
            user_id = adminUser,
            group_id = Const.DEFAULT_GROUP_ID_ADMINISTRATOR_CODE,
            can_add_edit = true,
            can_delete = true,
            can_read_only = false,
            statusApproval = Const.StatusApproval.APPROVED,
            isActive = true,
            isDeleted = false,
            isLocked = false,
            isDefault = false,
            createdBy = "system",
            createdDate = now,
        }, cancellationToken);

        await context.um_user_app_flag.AddAsync(new um_user_app_flag
        {
            user_id = adminUser,
            is_first_login = false,
            is_locked = false,
            is_login = false,
            failed_password_attempt_count = 0,
            last_login_date = now,
            last_password_changed_date = now,
        }, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
        await LookupSeeder.SeedAsync(context, cancellationToken);
        await MenuSeeder.SeedAsync(context, cancellationToken);
        await ParameterSeeder.SeedAsync(context, cancellationToken);
        await NotificationSeeder.SeedAsync(context, cancellationToken);
    }
}
