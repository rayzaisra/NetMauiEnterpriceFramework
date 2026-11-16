using __PROJECT__.Application.DTOs.Dashboard;
using __PROJECT__.Application.Interfaces;

namespace __PROJECT__.Infrastructure.Services;

public sealed class DashboardWidgetService : IDashboardWidgetService
{
    private readonly ICurrentUserService _currentUser;

    public DashboardWidgetService(ICurrentUserService currentUser)
        => _currentUser = currentUser;

    public Task<List<DashboardWidgetDto>> GetForCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_currentUser.UserId))
        {
            return Task.FromResult(new List<DashboardWidgetDto>());
        }

        // Demo source. Replace this implementation with a database-backed
        // user/group widget query when the widget maintenance screen is added.
        var widgets = new List<DashboardWidgetDto>
        {
            new()
            {
                Id = "portfolio",
                Title = "Saldo Efektif",
                Subtitle = "Lihat Portofolio",
                Value = "Rp ********",
                Icon = "AccountBalanceWallet",
                ActionUrl = "/profile",
                BackgroundColor = "#f7931e",
                SortOrder = 1,
            },
            new()
            {
                Id = "activity",
                Title = "Aktivitas Hari Ini",
                Subtitle = "Ringkasan aktivitas akun",
                Value = "-",
                Icon = "ReceiptLong",
                ActionUrl = "/notifications",
                BackgroundColor = "#76d7cf",
                SortOrder = 2,
            },
        };

        return Task.FromResult(widgets.OrderBy(widget => widget.SortOrder).ToList());
    }
}
