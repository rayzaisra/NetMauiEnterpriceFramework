using Microsoft.EntityFrameworkCore;

using __PROJECT__.Application.Interfaces.Repositories;
using __PROJECT__.Domain.Entities;
using __PROJECT__.Domain.Enums;
using __PROJECT__.Infrastructure.Data;

namespace __PROJECT__.Infrastructure.Repositories.Auth;

public class UmUserAppRepository : IUmUserAppRepository
{
    private readonly __PROJECT__DbContext _context;

    public UmUserAppRepository(__PROJECT__DbContext context) => _context = context;

    public Task<um_user_app?> GetByIdAsync(string userId)
        => _context.um_user_app.FirstOrDefaultAsync(e => e.user_id == userId);

    public Task<um_user_app?> FindByIdOrEmailAsync(
        string identifier,
        CancellationToken cancellationToken = default)
        => _context.um_user_app.FirstOrDefaultAsync(
            e => e.user_id == identifier || (e.email != null && e.email == identifier),
            cancellationToken);

    public Task<um_user_app?> GetActiveByIdAsync(string userId, CancellationToken cancellationToken = default)
        => _context.um_user_app.FirstOrDefaultAsync(
            e => e.user_id == userId && e.isDeleted != true,
            cancellationToken);

    public Task<bool> ExistsAnyAsync(string userId, CancellationToken cancellationToken = default)
        => _context.um_user_app.AnyAsync(e => e.user_id == userId, cancellationToken);

    public Task<bool> ExistsActiveAsync(string userId, CancellationToken cancellationToken = default)
        => _context.um_user_app.AnyAsync(
            e => e.user_id == userId && e.isDeleted != true,
            cancellationToken);

    public Task AddAsync(um_user_app entity)
        => _context.um_user_app.AddAsync(entity).AsTask();

    public Task UpdateAsync(um_user_app entity)
    {
        _context.um_user_app.Update(entity);
        return Task.CompletedTask;
    }
}

public class UmUserGroupRepository : IUmUserGroupRepository
{
    private readonly __PROJECT__DbContext _context;

    public UmUserGroupRepository(__PROJECT__DbContext context) => _context = context;

    public Task<um_user_group?> GetByIdAsync(string userId)
        => _context.um_user_group.FirstOrDefaultAsync(e => e.user_id == userId);

    public Task<um_user_group?> GetActiveByIdAsync(string userId, CancellationToken cancellationToken = default)
        => _context.um_user_group.FirstOrDefaultAsync(
            e => e.user_id == userId && e.isDeleted != true,
            cancellationToken);

    public Task<bool> ExistsActiveByUserAsync(string userId, CancellationToken cancellationToken = default)
        => _context.um_user_group.AnyAsync(
            e => e.user_id == userId && e.isDeleted != true,
            cancellationToken);

    public Task<bool> ExistsActiveByGroupAsync(string groupId, CancellationToken cancellationToken = default)
        => _context.um_user_group.AnyAsync(
            e => e.group_id == groupId && e.isDeleted != true,
            cancellationToken);

    public Task<List<string>> GetActiveAdministratorUserIdsAsync(
        CancellationToken cancellationToken = default)
        => (from userGroup in _context.um_user_group
            join appUser in _context.um_user_app on userGroup.user_id equals appUser.user_id
            where userGroup.group_id == Const.DEFAULT_GROUP_ID_ADMINISTRATOR_CODE
                  && appUser.isActive == true
                  && appUser.isDeleted != true
            select userGroup.user_id)
            .Distinct()
            .ToListAsync(cancellationToken);

    public Task<string?> GetGroupNameAsync(string groupId)
        => _context.um_group_app
            .Where(e => e.group_id == groupId && e.isDeleted != true)
            .Select(e => e.group_name)
            .FirstOrDefaultAsync();

    public Task AddAsync(um_user_group entity, CancellationToken cancellationToken = default)
        => _context.um_user_group.AddAsync(entity, cancellationToken).AsTask();

    public Task UpdateAsync(um_user_group entity)
    {
        _context.um_user_group.Update(entity);
        return Task.CompletedTask;
    }
}

public class UmUserAppSessionRepository : IUmUserAppSessionRepository
{
    private readonly __PROJECT__DbContext _context;

    public UmUserAppSessionRepository(__PROJECT__DbContext context) => _context = context;

    public Task<um_user_app_session?> GetByUserIdAsync(string userId)
        => _context.um_user_app_session.FirstOrDefaultAsync(e => e.user_id == userId);

    public async Task UpsertAsync(string userId, string? sessionId)
    {
        var session = await _context.um_user_app_session.FirstOrDefaultAsync(e => e.user_id == userId);
        if (session == null)
        {
            _context.um_user_app_session.Add(new um_user_app_session
            {
                user_id = userId,
                session_id = sessionId,
                last_activity = DateTime.UtcNow,
            });
            return;
        }

        session.session_id = sessionId;
        session.last_activity = DateTime.UtcNow;
    }

    public async Task RemoveAsync(string userId)
    {
        var session = await _context.um_user_app_session.FirstOrDefaultAsync(e => e.user_id == userId);
        if (session != null)
        {
            _context.um_user_app_session.Remove(session);
        }
    }
}

public class UmUserAppFlagRepository : IUmUserAppFlagRepository
{
    private readonly __PROJECT__DbContext _context;

    public UmUserAppFlagRepository(__PROJECT__DbContext context) => _context = context;

    public Task<um_user_app_flag?> GetByUserIdAsync(string userId)
        => _context.um_user_app_flag.FirstOrDefaultAsync(e => e.user_id == userId);

    public Task AddAsync(um_user_app_flag entity)
        => _context.um_user_app_flag.AddAsync(entity).AsTask();

    public Task UpdateAsync(um_user_app_flag entity)
    {
        _context.um_user_app_flag.Update(entity);
        return Task.CompletedTask;
    }
}

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly __PROJECT__DbContext _context;

    public RefreshTokenRepository(__PROJECT__DbContext context) => _context = context;

    public Task<um_user_refresh_token?> GetByTokenHashAsync(string tokenHash)
        => _context.um_user_refresh_token
            .FirstOrDefaultAsync(e => e.refresh_token == tokenHash && !e.is_revoked);

    public Task AddAsync(um_user_refresh_token entity)
        => _context.um_user_refresh_token.AddAsync(entity).AsTask();

    public Task UpdateAsync(um_user_refresh_token entity)
    {
        _context.um_user_refresh_token.Update(entity);
        return Task.CompletedTask;
    }

    public async Task RevokeAllUserTokensAsync(string userId)
    {
        var tokens = await _context.um_user_refresh_token
            .Where(e => e.user_id == userId && !e.is_revoked)
            .ToListAsync();

        foreach (var token in tokens)
        {
            token.is_revoked = true;
            token.modifiedDate = DateTime.Now;
        }
    }
}
