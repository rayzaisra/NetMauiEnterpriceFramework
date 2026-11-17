using __PROJECT__.Application.DTOs.General;
using __PROJECT__.Application.DTOs.User;

namespace __PROJECT__.Application.Interfaces.Queries;

public interface IUserQueryService
{
    Task<GeneralListResponseDto<UserListItemDto>> GetListAsync(
        DataTableRequestDto request,
        CancellationToken cancellationToken = default);

    Task<GeneralListResponseDto<UserFlagDto>> GetUserFlagListAsync(
        DataTableRequestDto request,
        CancellationToken cancellationToken = default);
}
