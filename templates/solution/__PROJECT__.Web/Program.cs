using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using MudBlazor;
using MudBlazor.Services;
using __PROJECT__.Shared.Handlers;
using __PROJECT__.Shared.Services;
using __PROJECT__.Shared.Services.Interfaces;
using __PROJECT__.Shared.Utils;
using __PROJECT__.Web;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = await ResolveApiBaseUrlAsync(builder.Configuration, builder.HostEnvironment);

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.TopRight;
    config.SnackbarConfiguration.PreventDuplicates = false;
    config.SnackbarConfiguration.NewestOnTop = false;
});

builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddScoped<ITokenStorage, TokenStorage>();
builder.Services.AddScoped<JwtAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<JwtAuthStateProvider>());
builder.Services.AddScoped<INotifyAuthenticationState>(sp => sp.GetRequiredService<JwtAuthStateProvider>());

builder.Services.AddSingleton<DynamicComponentResolver>();
builder.Services.AddScoped<TabStateService>();
builder.Services.AddScoped<AuthenticatingHttpHandler>();
builder.Services.AddScoped<ApiService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<CurrentUserAccessService>();
builder.Services.AddScoped<MenuService>();
builder.Services.AddScoped<DashboardApiService>();
builder.Services.AddScoped<SettingApiService>();
builder.Services.AddScoped<ApprovalApiService>();
builder.Services.AddScoped<ParameterApiService>();
builder.Services.AddScoped<EmailProviderApiService>();
builder.Services.AddScoped<DynamicApiService>();
builder.Services.AddScoped<NotificationApiService>();
builder.Services.AddScoped<ThemePreferenceService>();
builder.Services.AddScoped<LanguageService>();
builder.Services.AddTransient<MudLocalizer, AppMudLocalizer>();
builder.Services.AddScoped<MotionService>();
builder.Services.AddScoped<ExportApiService>();
builder.Services.AddScoped<ScreenMenuApiService>();
builder.Services.AddScoped<IExcelFilePicker, NoopExcelFilePicker>();

builder.Services.AddHttpClient("ApiClient", client => client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<AuthenticatingHttpHandler>();
builder.Services.AddHttpClient("SignalR");

await builder.Build().RunAsync();

static async Task<string> ResolveApiBaseUrlAsync(
    IConfiguration configuration,
    IWebAssemblyHostEnvironment environment)
{
    var configuredUrl = (configuration["ApiBaseUrl"] ?? "https://localhost:7143").TrimEnd('/');
    if (!environment.IsDevelopment())
    {
        return configuredUrl;
    }

    var fallbackUrls = configuration.GetSection("ApiFallbackUrls").Get<string[]>() ?? Array.Empty<string>();
    var useHttps = environment.BaseAddress.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
    var candidates = new[] { configuredUrl }
        .Concat(fallbackUrls)
        .Select(url => url.Trim().TrimEnd('/'))
        .Where(url => Uri.TryCreate(url, UriKind.Absolute, out var uri)
                      && (!useHttps || uri.Scheme == Uri.UriSchemeHttps))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
    foreach (var candidate in candidates)
    {
        try
        {
            using var response = await client.GetAsync($"{candidate}/health");
            if (response.IsSuccessStatusCode)
            {
                return candidate;
            }
        }
        catch
        {
            // Try the next local API address.
        }
    }

    return configuredUrl;
}
