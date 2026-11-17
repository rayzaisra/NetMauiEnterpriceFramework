using __PROJECT__.Application.DTOs.Email;
using __PROJECT__.Application.DTOs.General;

namespace __PROJECT__.Application.Interfaces.Queries;

public interface IEmailProviderQueryService
{
    Task<(List<EmailProviderDto> Data, int TotalCount)> GetListAsync(
        DataTableRequestDto request,
        CancellationToken cancellationToken = default);
}
