using __PROJECT__.Domain.Entities;

namespace __PROJECT__.Application.Interfaces.Repositories;

public interface IRfNotificationTypeRepository
{
    Task<rf_notification_type?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default);
    Task AddAsync(rf_notification_type entity, CancellationToken cancellationToken = default);
}
