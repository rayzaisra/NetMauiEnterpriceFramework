using __PROJECT__.Application.DTOs.Dashboard;
using __PROJECT__.Application.DTOs.General;

namespace __PROJECT__.Shared.Services;

public sealed class DashboardApiService
{
    private readonly ApiService _api;

    public DashboardApiService(ApiService api)
        => _api = api;

    public async Task<List<DashboardWidgetDto>> GetWidgetsAsync()
        => await _api.GetFromJsonAsync<List<DashboardWidgetDto>>(
               "api/dashboard/widgets",
               unwrap: true)
           ?? new List<DashboardWidgetDto>();
}
