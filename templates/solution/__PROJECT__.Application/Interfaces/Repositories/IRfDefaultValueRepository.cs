using __PROJECT__.Domain.Entities;

namespace __PROJECT__.Application.Interfaces.Repositories;

public interface IRfDefaultValueRepository
{
    Task<rf_default_value?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
}
