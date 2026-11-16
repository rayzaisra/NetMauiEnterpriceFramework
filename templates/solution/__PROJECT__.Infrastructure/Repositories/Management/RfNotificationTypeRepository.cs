using Microsoft.EntityFrameworkCore;

using __PROJECT__.Application.Interfaces.Repositories;
using __PROJECT__.Domain.Entities;
using __PROJECT__.Infrastructure.Data;

namespace __PROJECT__.Infrastructure.Repositories.Management;

public class RfNotificationTypeRepository : IRfNotificationTypeRepository
{
    private readonly __PROJECT__DbContext _context;

    public RfNotificationTypeRepository(__PROJECT__DbContext context) => _context = context;

    public Task<rf_notification_type?> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default)
        => _context.rf_notification_type.FirstOrDefaultAsync(
            e => e.id == id && e.isDeleted != true,
            cancellationToken);

    public Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default)
        => _context.rf_notification_type.AnyAsync(
            e => e.id == id && e.isDeleted != true,
            cancellationToken);

    public Task AddAsync(rf_notification_type entity, CancellationToken cancellationToken = default)
        => _context.rf_notification_type.AddAsync(entity, cancellationToken).AsTask();
}
