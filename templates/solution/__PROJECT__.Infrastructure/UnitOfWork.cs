using Microsoft.EntityFrameworkCore.Storage;

using __PROJECT__.Application.Interfaces;
using __PROJECT__.Application.Interfaces.Repositories;
using __PROJECT__.Infrastructure.Data;
using __PROJECT__.Infrastructure.Repositories.Auth;
using __PROJECT__.Infrastructure.Repositories.Management;

namespace __PROJECT__.Infrastructure;

public class UnitOfWork : IUnitOfWork
{
    private readonly __PROJECT__DbContext _context;

    private IUmUserAppRepository? _umUserApp;
    private IUmUserGroupRepository? _umUserGroup;
    private IUmUserAppFlagRepository? _umUserAppFlag;
    private IUmUserAppSessionRepository? _umUserAppSession;
    private IRefreshTokenRepository? _refreshTokens;
    private IUmGroupAppRepository? _umGroupApp;
    private ITempGroupAppPendingRepository? _tempGroupAppPending;
    private ITempUserAppPendingRepository? _tempUserAppPending;
    private IUmMenuRepository? _menus;
    private IUmGroupMenuRepository? _groupMenus;
    private IParameterStagingRepository? _parameterStaging;
    private IScreenMenuRepository? _screenMenus;
    private IPasswordResetTokenRepository? _passwordResetTokens;
    private IEmailTemplateRepository? _emailTemplates;
    private IEmailOutboxRepository? _emailOutbox;
    private ITbNotificationRepository? _notifications;
    private IRfNotificationTypeRepository? _notificationTypes;
    private IEmailProviderRepository? _emailProviders;
    private IRfDefaultValueRepository? _defaultValues;

    public UnitOfWork(__PROJECT__DbContext context) => _context = context;

    public IUmUserAppRepository UmUserApp => _umUserApp ??= new UmUserAppRepository(_context);
    public IUmUserGroupRepository UmUserGroup => _umUserGroup ??= new UmUserGroupRepository(_context);
    public IUmUserAppFlagRepository UmUserAppFlag => _umUserAppFlag ??= new UmUserAppFlagRepository(_context);
    public IUmUserAppSessionRepository UmUserAppSession => _umUserAppSession ??= new UmUserAppSessionRepository(_context);
    public IRefreshTokenRepository RefreshTokens => _refreshTokens ??= new RefreshTokenRepository(_context);
    public IUmGroupAppRepository UmGroupApp => _umGroupApp ??= new UmGroupAppRepository(_context);
    public ITempGroupAppPendingRepository TempGroupAppPending => _tempGroupAppPending ??= new TempGroupAppPendingRepository(_context);
    public ITempUserAppPendingRepository TempUserAppPending => _tempUserAppPending ??= new TempUserAppPendingRepository(_context);
    public IUmMenuRepository Menus => _menus ??= new UmMenuRepository(_context);
    public IUmGroupMenuRepository GroupMenus => _groupMenus ??= new UmGroupMenuRepository(_context);
    public IParameterStagingRepository ParameterStaging => _parameterStaging ??= new ParameterStagingRepository(_context);
    public IScreenMenuRepository ScreenMenus => _screenMenus ??= new ScreenMenuRepository(_context);
    public IPasswordResetTokenRepository PasswordResetTokens => _passwordResetTokens ??= new PasswordResetTokenRepository(_context);
    public IEmailTemplateRepository EmailTemplates => _emailTemplates ??= new EmailTemplateRepository(_context);
    public IEmailOutboxRepository EmailOutbox => _emailOutbox ??= new EmailOutboxRepository(_context);
    public ITbNotificationRepository Notifications => _notifications ??= new TbNotificationRepository(_context);
    public IRfNotificationTypeRepository NotificationTypes => _notificationTypes ??= new RfNotificationTypeRepository(_context);
    public IEmailProviderRepository EmailProviders => _emailProviders ??= new EmailProviderRepository(_context);
    public IRfDefaultValueRepository DefaultValues => _defaultValues ??= new RfDefaultValueRepository(_context);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);

    public async Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        => new UnitOfWorkTransaction(await _context.Database.BeginTransactionAsync(cancellationToken));
}

internal sealed class UnitOfWorkTransaction : IUnitOfWorkTransaction
{
    private readonly IDbContextTransaction _transaction;

    public UnitOfWorkTransaction(IDbContextTransaction transaction) => _transaction = transaction;

    public Task CommitAsync(CancellationToken cancellationToken = default)
        => _transaction.CommitAsync(cancellationToken);

    public Task RollbackAsync(CancellationToken cancellationToken = default)
        => _transaction.RollbackAsync(cancellationToken);

    public ValueTask DisposeAsync() => _transaction.DisposeAsync();
}
