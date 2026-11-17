using __PROJECT__.Application.DTOs.General;
using __PROJECT__.Application.DTOs.Screen;

namespace __PROJECT__.Application.Interfaces.Queries;

public interface IScreenMenuQueryService
{
    Task<GeneralListResponseDto<ScreenMenuListItemDto>> GetListAsync(
        DataTableRequestDto request,
        CancellationToken cancellationToken = default);

    Task<ScreenMenuEditDto?> GetAsync(string id, CancellationToken cancellationToken = default);
}
