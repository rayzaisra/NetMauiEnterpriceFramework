using __PROJECT__.Application.DTOs.Approval;
using __PROJECT__.Application.DTOs.General;

namespace __PROJECT__.Application.Interfaces.Queries;

public interface IApprovalQueryService
{
    Task<GeneralListResponseDto<UserApprovalListItemDto>> GetPendingUsersAsync(
        DataTableRequestDto request,
        CancellationToken cancellationToken = default);

    Task<GeneralListResponseDto<GroupApprovalListItemDto>> GetPendingGroupsAsync(
        DataTableRequestDto request,
        CancellationToken cancellationToken = default);

    Task<GeneralListResponseDto<RoleApprovalListItemDto>> GetPendingRolesAsync(
        DataTableRequestDto request,
        CancellationToken cancellationToken = default);

    Task<GeneralListResponseDto<MenuApprovalListItemDto>> GetPendingMenusAsync(
        DataTableRequestDto request,
        CancellationToken cancellationToken = default);
}
