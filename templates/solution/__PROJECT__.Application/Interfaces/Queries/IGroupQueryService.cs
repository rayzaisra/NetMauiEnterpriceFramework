using __PROJECT__.Application.DTOs.General;
using __PROJECT__.Application.DTOs.Group;

namespace __PROJECT__.Application.Interfaces.Queries;

public interface IGroupQueryService
{
    Task<GeneralListResponseDto<GroupListItemDto>> GetListAsync(
        DataTableRequestDto request,
        CancellationToken cancellationToken = default);
}
