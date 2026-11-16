using __PROJECT__.Domain.Entities;

namespace __PROJECT__.Application.Interfaces.Repositories;

public interface IEmailProviderRepository
{
    Task<rf_email_provider?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<rf_email_provider?> GetActiveProviderAsync(CancellationToken cancellationToken = default);
    Task<List<rf_email_provider>> GetActiveProvidersAsync(CancellationToken cancellationToken = default);
    Task AddAsync(rf_email_provider entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(rf_email_provider entity);
}
