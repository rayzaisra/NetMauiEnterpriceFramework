using __PROJECT__.Domain.Entities;

namespace __PROJECT__.Application.Interfaces.Repositories;

public interface IEmailTemplateRepository
{
    Task<rf_email_template?> GetActiveByKeyAsync(
        string templateKey,
        CancellationToken cancellationToken = default);
}
