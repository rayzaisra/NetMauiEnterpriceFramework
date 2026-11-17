using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace __PROJECT__.Shared.Services;

public sealed class MotionService : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private readonly ILogger<MotionService> _logger;
    private bool _unavailable;

    public MotionService(IJSRuntime js, ILogger<MotionService> logger)
    {
        _js = js;
        _logger = logger;
    }

    public Task PlayLoginAsync() => InvokeAsync("initLogin");

    public Task PlayDashboardAsync() => InvokeAsync("initDashboard");

    public Task PlayNotificationsAsync() => InvokeAsync("initNotifications");

    public Task PlayMobilePageAsync() => InvokeAsync("initMobilePage");

    public Task PlayTabPanelAsync() => InvokeAsync("playTabPanel");

    public Task PlayNotificationArrivalAsync() => InvokeAsync("playNotificationArrival");

    public Task InitBusyIndicatorsAsync() => InvokeAsync("initBusyIndicators");

    public async ValueTask DisposeAsync()
    {
        try
        {
            await _js.InvokeVoidAsync("ryzMotionInvoke", "cleanup");
        }
        catch (JSDisconnectedException)
        {
        }
        catch (JSException ex)
        {
            _logger.LogDebug(ex, "Unable to clean up motion animations.");
        }
    }

    private async Task InvokeAsync(string method)
    {
        if (_unavailable)
        {
            return;
        }

        try
        {
            await _js.InvokeVoidAsync("ryzMotionInvoke", method);
        }
        catch (JSDisconnectedException)
        {
        }
        catch (Exception ex) when (ex is JSException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Motion layer is unavailable; the UI remains functional without animation.");
            _unavailable = true;
        }
    }
}
