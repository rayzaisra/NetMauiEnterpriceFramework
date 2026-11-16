using Microsoft.EntityFrameworkCore;

using __PROJECT__.Application.Interfaces.Repositories;
using __PROJECT__.Domain.Entities;
using __PROJECT__.Infrastructure.Data;

namespace __PROJECT__.Infrastructure.Repositories.Management;

public class RfDefaultValueRepository : IRfDefaultValueRepository
{
    private readonly __PROJECT__DbContext _context;

    public RfDefaultValueRepository(__PROJECT__DbContext context) => _context = context;

    public Task<rf_default_value?> GetByCodeAsync(
        string code,
        CancellationToken cancellationToken = default)
        => _context.rf_default_value.FirstOrDefaultAsync(
            e => e.def_code == code && e.isDeleted != true,
            cancellationToken);
}
