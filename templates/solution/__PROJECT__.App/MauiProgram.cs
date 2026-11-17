using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using __PROJECT__.App.Services;
using __PROJECT__.Shared.Handlers;
using __PROJECT__.Shared.Services;
using __PROJECT__.Shared.Services.Interfaces;
using __PROJECT__.Shared.Utils;

namespace __PROJECT__.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMudServices(config =>
        {
            config.SnackbarConfiguration.PositionClass = MudBlazor.Defaults.Classes.Position.TopRight;
            config.SnackbarConfiguration.PreventDuplicates = false;
            config.SnackbarConfiguration.NewestOnTop = false;
        });
        builder.Services.AddMauiBlazorWebView();

        var apiBaseUrl = MauiApiSettings.ResolveApiBaseUrl(CreateHttpHandler);

        builder.Services.AddScoped(_ => new HttpClient(CreateHttpHandler())
        {
            BaseAddress = new Uri(apiBaseUrl),
        });

        builder.Services.AddAuthorizationCore();
        builder.Services.AddCascadingAuthenticationState();

        builder.Services.AddScoped<ITokenStorage, MauiTokenStorage>();
        builder.Services.AddScoped<JwtAuthStateProvider>();
        builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<JwtAuthStateProvider>());
        builder.Services.AddScoped<INotifyAuthenticationState>(sp => sp.GetRequiredService<JwtAuthStateProvider>());

        builder.Services.AddSingleton<DynamicComponentResolver>();
        builder.Services.AddScoped<TabStateService>();
        builder.Services.AddScoped<AuthenticatingHttpHandler>();
        builder.Services.AddScoped<ApiService>();
        builder.Services.AddScoped<AuthService>();
        builder.Services.AddScoped<MenuService>();
        builder.Services.AddScoped<DashboardApiService>();
        builder.Services.AddScoped<SettingApiService>();
        builder.Services.AddScoped<ApprovalApiService>();
        builder.Services.AddScoped<ParameterApiService>();
        builder.Services.AddScoped<EmailProviderApiService>();
        builder.Services.AddScoped<DynamicApiService>();
        builder.Services.AddScoped<NotificationApiService>();
        builder.Services.AddScoped<ThemePreferenceService>();
        builder.Services.AddScoped<MotionService>();
        builder.Services.AddScoped<ExportApiService>();
        builder.Services.AddScoped<ScreenMenuApiService>();
        builder.Services.AddScoped<IExcelFilePicker, MauiExcelFilePicker>();

        builder.Services.AddHttpClient("ApiClient", client => client.BaseAddress = new Uri(apiBaseUrl))
            .ConfigurePrimaryHttpMessageHandler(CreateHttpHandler)
            .AddHttpMessageHandler<AuthenticatingHttpHandler>();
        builder.Services.AddHttpClient("SignalR")
            .ConfigurePrimaryHttpMessageHandler(CreateHttpHandler);

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
        builder.Logging.SetMinimumLevel(LogLevel.Trace);
#endif

        return builder.Build();
    }

    private static HttpClientHandler CreateHttpHandler()
    {
        var handler = new HttpClientHandler
        {
            UseProxy = false,
        };

#if DEBUG
        handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
#endif

        return handler;
    }
}
