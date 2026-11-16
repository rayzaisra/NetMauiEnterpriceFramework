using __PROJECT__.Application.DTOs.Dashboard;

namespace __PROJECT__.Application.Interfaces;

public interface IDashboardWidgetService
{
    Task<List<DashboardWidgetDto>> GetForCurrentUserAsync(CancellationToken cancellationToken = default);
}
