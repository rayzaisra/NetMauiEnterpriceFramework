using __PROJECT__.Domain.Entities;

namespace __PROJECT__.Application.Interfaces.Repositories;

public interface ITbNotificationRepository
{
    Task<tb_notification?> GetByIdForUserAsync(long id, string userId, CancellationToken cancellationToken = default);
    Task<List<tb_notification>> GetUnseenByUserAsync(string userId, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default);
    Task AddAsync(tb_notification entity, CancellationToken cancellationToken = default);
}
