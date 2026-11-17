using __PROJECT__.Application.DTOs.General;
using __PROJECT__.Application.DTOs.Role;

namespace __PROJECT__.Application.Interfaces.Queries;

public interface IRoleQueryService
{
    Task<GeneralListResponseDto<RoleListItemDto>> GetListAsync(
        DataTableRequestDto request,
        CancellationToken cancellationToken = default);
}
