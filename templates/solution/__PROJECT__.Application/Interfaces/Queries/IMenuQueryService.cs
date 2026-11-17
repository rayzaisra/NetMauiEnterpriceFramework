using __PROJECT__.Application.DTOs.General;
using __PROJECT__.Application.DTOs.Menu;

namespace __PROJECT__.Application.Interfaces.Queries;

public interface IMenuQueryService
{
    Task<GeneralListResponseDto<MenuListItemDto>> GetListAsync(
        DataTableRequestDto request,
        CancellationToken cancellationToken = default);

    Task<List<MenuTreeItemDto>> GetTreeAsync(CancellationToken cancellationToken = default);

    Task<List<long>> GetAssignedMenuIdsAsync(
        string groupId,
        CancellationToken cancellationToken = default);

    Task<List<MenuItemDto>> GetMenuTreeByGroupIdAsync(
        string groupId,
        CancellationToken cancellationToken = default);
}
