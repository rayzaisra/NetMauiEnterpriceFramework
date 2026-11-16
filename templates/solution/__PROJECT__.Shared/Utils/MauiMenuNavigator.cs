using Microsoft.AspNetCore.Components;
using MudBlazor;
using __PROJECT__.Application.DTOs.Menu;

namespace __PROJECT__.Shared.Utils;

public static class MauiMenuNavigator
{
    public static void Open(
        NavigationManager navigation,
        MenuItemDto item,
        ISnackbar? snackbar = null)
    {
        if (string.IsNullOrWhiteSpace(item.MenuUrl))
        {
            snackbar?.Add("This menu is not implemented yet.", Severity.Info);
            return;
        }

        var target = item.MenuUrl.Trim();

        if (string.Equals(item.ComponentType, "DynamicScreen", StringComparison.OrdinalIgnoreCase))
        {
            navigation.NavigateTo($"/dynamic/screen/{Uri.EscapeDataString(target)}");
            return;
        }

        if (string.Equals(item.ComponentType, "DynamicReport", StringComparison.OrdinalIgnoreCase))
        {
            navigation.NavigateTo($"/dynamic/report/{Uri.EscapeDataString(target)}");
            return;
        }

        if (target.StartsWith('/'))
        {
            navigation.NavigateTo(target);
            return;
        }

        snackbar?.Add("This menu target is not available in the mobile shell.", Severity.Info);
    }
}
