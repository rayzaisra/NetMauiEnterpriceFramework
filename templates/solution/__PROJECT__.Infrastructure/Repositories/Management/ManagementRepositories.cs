using Microsoft.EntityFrameworkCore;

using __PROJECT__.Application.Interfaces.Repositories;
using __PROJECT__.Domain.Entities;
using __PROJECT__.Domain.Enums;
using __PROJECT__.Infrastructure.Data;

namespace __PROJECT__.Infrastructure.Repositories.Management;

public class UmGroupAppRepository : IUmGroupAppRepository
{
    private readonly __PROJECT__DbContext _context;

    public UmGroupAppRepository(__PROJECT__DbContext context) => _context = context;

    public Task<um_group_app?> GetByIdAsync(string groupId, CancellationToken cancellationToken = default)
        => _context.um_group_app.FirstOrDefaultAsync(e => e.group_id == groupId, cancellationToken);

    public Task<um_group_app?> GetActiveByIdAsync(string groupId, CancellationToken cancellationToken = default)
        => _context.um_group_app.FirstOrDefaultAsync(
            e => e.group_id == groupId && e.isDeleted != true,
            cancellationToken);

    public Task<bool> ExistsAnyAsync(string groupId, CancellationToken cancellationToken = default)
        => _context.um_group_app.AnyAsync(e => e.group_id == groupId, cancellationToken);

    public Task<bool> ExistsActiveAsync(string groupId, CancellationToken cancellationToken = default)
        => _context.um_group_app.AnyAsync(
            e => e.group_id == groupId && e.isDeleted != true,
            cancellationToken);

    public Task<bool> HasActiveUsersAsync(string groupId, CancellationToken cancellationToken = default)
        => _context.um_user_group.AnyAsync(
            e => e.group_id == groupId && e.isDeleted != true,
            cancellationToken);

    public Task AddAsync(um_group_app entity, CancellationToken cancellationToken = default)
        => _context.um_group_app.AddAsync(entity, cancellationToken).AsTask();
}

public class TempGroupAppPendingRepository : ITempGroupAppPendingRepository
{
    private readonly __PROJECT__DbContext _context;

    public TempGroupAppPendingRepository(__PROJECT__DbContext context) => _context = context;

    public Task<temp_group_app_pending?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        => _context.temp_group_app_pending.FirstOrDefaultAsync(e => e.id == id, cancellationToken);

    public Task<bool> ExistsAsync(string groupId, CancellationToken cancellationToken = default)
        => _context.temp_group_app_pending.AnyAsync(e => e.group_id == groupId, cancellationToken);

    public Task AddAsync(temp_group_app_pending entity, CancellationToken cancellationToken = default)
        => _context.temp_group_app_pending.AddAsync(entity, cancellationToken).AsTask();

    public void Remove(temp_group_app_pending entity)
        => _context.temp_group_app_pending.Remove(entity);
}

public class TempUserAppPendingRepository : ITempUserAppPendingRepository
{
    private readonly __PROJECT__DbContext _context;

    public TempUserAppPendingRepository(__PROJECT__DbContext context) => _context = context;

    public Task<temp_user_app_pending?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        => _context.temp_user_app_pending.FirstOrDefaultAsync(e => e.id == id, cancellationToken);

    public Task<bool> ExistsAsync(string userId, CancellationToken cancellationToken = default)
        => _context.temp_user_app_pending.AnyAsync(e => e.user_id == userId, cancellationToken);

    public Task AddAsync(temp_user_app_pending entity, CancellationToken cancellationToken = default)
        => _context.temp_user_app_pending.AddAsync(entity, cancellationToken).AsTask();

    public void Remove(temp_user_app_pending entity)
        => _context.temp_user_app_pending.Remove(entity);
}

public class UmMenuRepository : IUmMenuRepository
{
    private readonly __PROJECT__DbContext _context;

    public UmMenuRepository(__PROJECT__DbContext context) => _context = context;

    public Task<um_menu?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        => _context.um_menu.FirstOrDefaultAsync(e => e.id == id, cancellationToken);

    public Task<um_menu?> GetActiveByIdAsync(long id, CancellationToken cancellationToken = default)
        => _context.um_menu.FirstOrDefaultAsync(
            e => e.id == id && e.isDeleted != true,
            cancellationToken);

    public Task<um_menu?> GetByUniqueAsync(string menuUnique, CancellationToken cancellationToken = default)
        => _context.um_menu.FirstOrDefaultAsync(e => e.menu_unique == menuUnique, cancellationToken);

    public Task<bool> AnyByUniqueAsync(string menuUnique, CancellationToken cancellationToken = default)
        => _context.um_menu.AnyAsync(e => e.menu_unique == menuUnique, cancellationToken);

    public Task<bool> ExistsActiveByUniqueAsync(string menuUnique, CancellationToken cancellationToken = default)
        => _context.um_menu.AnyAsync(
            e => e.menu_unique == menuUnique && e.isDeleted != true,
            cancellationToken);

    public Task<bool> ExistsAnyByUniqueExceptIdAsync(
        string menuUnique,
        long id,
        CancellationToken cancellationToken = default)
        => _context.um_menu.AnyAsync(
            e => e.menu_unique == menuUnique && e.id != id,
            cancellationToken);

    public Task<bool> HasActiveChildrenAsync(string menuUnique, CancellationToken cancellationToken = default)
        => _context.um_menu.AnyAsync(
            e => e.menu_unique_parent == menuUnique && e.isDeleted != true,
            cancellationToken);

    public Task<List<um_menu>> GetByParentAsync(string menuUnique, CancellationToken cancellationToken = default)
        => _context.um_menu
            .Where(e => e.menu_unique_parent == menuUnique)
            .ToListAsync(cancellationToken);

    public Task<List<um_menu>> GetActiveMenusAsync(CancellationToken cancellationToken = default)
        => _context.um_menu
            .AsNoTracking()
            .Where(e => e.isDeleted != true)
            .ToListAsync(cancellationToken);

    public Task AddAsync(um_menu entity, CancellationToken cancellationToken = default)
        => _context.um_menu.AddAsync(entity, cancellationToken).AsTask();
}

public class UmGroupMenuRepository : IUmGroupMenuRepository
{
    private readonly __PROJECT__DbContext _context;

    public UmGroupMenuRepository(__PROJECT__DbContext context) => _context = context;

    public Task<List<um_group_menu>> GetByGroupAsync(
        string groupId,
        CancellationToken cancellationToken = default)
        => _context.um_group_menu
            .Where(e => e.group_id == groupId)
            .ToListAsync(cancellationToken);

    public Task<List<um_group_menu>> GetByMenuIdAsync(
        long menuId,
        CancellationToken cancellationToken = default)
        => _context.um_group_menu
            .Where(e => e.menu_id == menuId)
            .ToListAsync(cancellationToken);

    public Task AddAsync(um_group_menu entity, CancellationToken cancellationToken = default)
        => _context.um_group_menu.AddAsync(entity, cancellationToken).AsTask();

    public void RemoveRange(IEnumerable<um_group_menu> entities)
        => _context.um_group_menu.RemoveRange(entities);
}

public class ParameterStagingRepository : IParameterStagingRepository
{
    private readonly __PROJECT__DbContext _context;

    public ParameterStagingRepository(__PROJECT__DbContext context) => _context = context;

    public Task<tb_parameter_sytem_temporary?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => _context.tb_parameter_sytem_temporary.FirstOrDefaultAsync(e => e.id == id, cancellationToken);

    public Task<List<tb_parameter_sytem_temporary_detail>> GetDetailsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => _context.tb_parameter_sytem_temporary_detail
            .Where(e => e.tb_parameter_sytem_temporary_id == id)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(
        tb_parameter_sytem_temporary temporary,
        IEnumerable<tb_parameter_sytem_temporary_detail> details,
        CancellationToken cancellationToken = default)
    {
        await _context.tb_parameter_sytem_temporary.AddAsync(temporary, cancellationToken);
        await _context.tb_parameter_sytem_temporary_detail.AddRangeAsync(details, cancellationToken);
    }

    public Task<bool> HasPendingAsync(
        string tableName,
        string keyColumn,
        string keyValue,
        CancellationToken cancellationToken = default)
        => (from temporary in _context.tb_parameter_sytem_temporary
            join detail in _context.tb_parameter_sytem_temporary_detail
                on temporary.id equals detail.tb_parameter_sytem_temporary_id
            where temporary.table_name == tableName
                  && (temporary.type == Const.ApprovalType.ADD
                      || temporary.type == Const.ApprovalType.MODIFIED
                      || temporary.type == Const.ApprovalType.DELETE)
                  && detail.column_name == keyColumn
                  && detail.value == keyValue
            select temporary.id).AnyAsync(cancellationToken);

    public Task<bool> HasPendingKeyAsync(
        string tableName,
        string keyColumn,
        string keyValue,
        CancellationToken cancellationToken = default)
        => (from temporary in _context.tb_parameter_sytem_temporary
            join detail in _context.tb_parameter_sytem_temporary_detail
                on temporary.id equals detail.tb_parameter_sytem_temporary_id
            where temporary.table_name == tableName
                  && detail.column_name == keyColumn
                  && detail.value == keyValue
            select temporary.id).AnyAsync(cancellationToken);

    public void Remove(
        tb_parameter_sytem_temporary temporary,
        IEnumerable<tb_parameter_sytem_temporary_detail> details)
    {
        _context.tb_parameter_sytem_temporary_detail.RemoveRange(details);
        _context.tb_parameter_sytem_temporary.Remove(temporary);
    }
}

public class ScreenMenuRepository : IScreenMenuRepository
{
    private readonly __PROJECT__DbContext _context;

    public ScreenMenuRepository(__PROJECT__DbContext context) => _context = context;

    public Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default)
        => _context.tb_screen_menu.AnyAsync(e => e.id == id, cancellationToken);

    public Task<bool> ChildExistsAsync(string childId, CancellationToken cancellationToken = default)
        => _context.tb_screen_menu_child.AnyAsync(
            e => e.screen_menu_child_id == childId,
            cancellationToken);

    public Task<bool> ImportExistsAsync(string importId, CancellationToken cancellationToken = default)
        => _context.tb_screen_menu_import_excel_process.AnyAsync(
            e => e.screen_menu_import_excel_process_id == importId,
            cancellationToken);
}

public class TbNotificationRepository : ITbNotificationRepository
{
    private readonly __PROJECT__DbContext _context;

    public TbNotificationRepository(__PROJECT__DbContext context) => _context = context;

    public Task<tb_notification?> GetByIdForUserAsync(
        long id,
        string userId,
        CancellationToken cancellationToken = default)
        => _context.tb_notification.FirstOrDefaultAsync(
            e => e.id == id && e.receiver_user_id == userId,
            cancellationToken);

    public Task<List<tb_notification>> GetUnseenByUserAsync(
        string userId,
        CancellationToken cancellationToken = default)
        => _context.tb_notification
            .Where(e => e.receiver_user_id == userId && e.is_seen != true)
            .ToListAsync(cancellationToken);

    public Task AddAsync(tb_notification entity, CancellationToken cancellationToken = default)
        => _context.tb_notification.AddAsync(entity, cancellationToken).AsTask();
}

public class TbNotificationTypeRepository : ITbNotificationTypeRepository
{
    private readonly __PROJECT__DbContext _context;

    public TbNotificationTypeRepository(__PROJECT__DbContext context) => _context = context;

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.tb_notification_type.AnyAsync(e => e.id == id, cancellationToken);

    public Task AddAsync(tb_notification_type entity, CancellationToken cancellationToken = default)
        => _context.tb_notification_type.AddAsync(entity, cancellationToken).AsTask();
}

public class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly __PROJECT__DbContext _context;

    public PasswordResetTokenRepository(__PROJECT__DbContext context) => _context = context;

    public Task<List<tb_password_reset_token>> GetUnusedByUserAsync(
        string userId,
        CancellationToken cancellationToken = default)
        => _context.tb_password_reset_token
            .Where(e => e.user_id == userId && e.used_at == null)
            .ToListAsync(cancellationToken);

    public Task<tb_password_reset_token?> GetValidByHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
        => _context.tb_password_reset_token.FirstOrDefaultAsync(
            e => e.token_hash == tokenHash
                 && e.used_at == null
                 && e.expires_at > DateTime.UtcNow,
            cancellationToken);

    public Task<bool> AnyValidByHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
        => _context.tb_password_reset_token.AnyAsync(
            e => e.token_hash == tokenHash
                 && e.used_at == null
                 && e.expires_at > DateTime.UtcNow,
            cancellationToken);

    public Task AddAsync(tb_password_reset_token entity, CancellationToken cancellationToken = default)
        => _context.tb_password_reset_token.AddAsync(entity, cancellationToken).AsTask();
}

public class EmailTemplateRepository : IEmailTemplateRepository
{
    private readonly __PROJECT__DbContext _context;

    public EmailTemplateRepository(__PROJECT__DbContext context) => _context = context;

    public Task<rf_email_template?> GetActiveByKeyAsync(
        string templateKey,
        CancellationToken cancellationToken = default)
        => _context.rf_email_template.FirstOrDefaultAsync(
            e => e.template_key == templateKey && e.isActive != false,
            cancellationToken);
}

public class EmailOutboxRepository : IEmailOutboxRepository
{
    private readonly __PROJECT__DbContext _context;

    public EmailOutboxRepository(__PROJECT__DbContext context) => _context = context;

    public Task AddAsync(tb_email_outbox entity, CancellationToken cancellationToken = default)
        => _context.tb_email_outbox.AddAsync(entity, cancellationToken).AsTask();

    public Task<List<tb_email_outbox>> GetPendingAsync(
        DateTime now,
        int maxItems,
        CancellationToken cancellationToken = default)
        => _context.tb_email_outbox
            .Where(e => (e.status == "Pending" || e.status == "Failed")
                        && (e.next_attempt_at == null || e.next_attempt_at <= now)
                        && e.attempt_count < 3)
            .OrderBy(e => e.created_date)
            .Take(maxItems)
            .ToListAsync(cancellationToken);
}

public class EmailProviderRepository : IEmailProviderRepository
{
    private readonly __PROJECT__DbContext _context;

    public EmailProviderRepository(__PROJECT__DbContext context) => _context = context;

    public Task<rf_email_provider?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _context.rf_email_provider.FirstOrDefaultAsync(
            e => e.id == id && e.isDeleted != true,
            cancellationToken);

    public Task<rf_email_provider?> GetActiveProviderAsync(CancellationToken cancellationToken = default)
        => _context.rf_email_provider
            .Where(e => e.isActive != false && e.isDeleted != true && e.provider_type != "NONE")
            .OrderBy(e => e.priority ?? 100)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<List<rf_email_provider>> GetActiveProvidersAsync(
        CancellationToken cancellationToken = default)
        => _context.rf_email_provider
            .Where(e => e.isActive != false && e.isDeleted != true && e.provider_type != "NONE")
            .OrderBy(e => e.priority ?? 100)
            .ToListAsync(cancellationToken);

    public Task AddAsync(rf_email_provider entity, CancellationToken cancellationToken = default)
        => _context.rf_email_provider.AddAsync(entity, cancellationToken).AsTask();

    public Task UpdateAsync(rf_email_provider entity)
    {
        _context.rf_email_provider.Update(entity);
        return Task.CompletedTask;
    }
}
