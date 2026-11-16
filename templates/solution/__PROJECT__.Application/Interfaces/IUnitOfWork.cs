using __PROJECT__.Application.Interfaces.Repositories;

namespace __PROJECT__.Application.Interfaces;

public interface IUnitOfWork
{
    IUmUserAppRepository UmUserApp { get; }
    IUmUserGroupRepository UmUserGroup { get; }
    IUmUserAppFlagRepository UmUserAppFlag { get; }
    IUmUserAppSessionRepository UmUserAppSession { get; }
    IRefreshTokenRepository RefreshTokens { get; }
    IUmGroupAppRepository UmGroupApp { get; }
    ITempGroupAppPendingRepository TempGroupAppPending { get; }
    ITempUserAppPendingRepository TempUserAppPending { get; }
    IUmMenuRepository Menus { get; }
    IUmGroupMenuRepository GroupMenus { get; }
    IParameterStagingRepository ParameterStaging { get; }
    IScreenMenuRepository ScreenMenus { get; }
    IPasswordResetTokenRepository PasswordResetTokens { get; }
    IEmailTemplateRepository EmailTemplates { get; }
    IEmailOutboxRepository EmailOutbox { get; }
    ITbNotificationRepository Notifications { get; }
    IRfNotificationTypeRepository NotificationTypes { get; }
    IEmailProviderRepository EmailProviders { get; }
    IRfDefaultValueRepository DefaultValues { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
