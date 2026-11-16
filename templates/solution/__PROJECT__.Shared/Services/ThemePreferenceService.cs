using Microsoft.JSInterop;

using MudBlazor;

using __PROJECT__.Shared.Theme;

namespace __PROJECT__.Shared.Services;

public class ThemePreferenceService
{
    private const string StyleStorageKey = "enterprise-ui-style";
    private const string PaletteStorageKey = "enterprise-ui-palette";
    private const string ThemeModeAttribute = "data-theme-mode";

    private readonly IJSRuntime _js;
    private bool _initialized;
    private bool _isDarkMode;

    public ThemePreferenceService(IJSRuntime js)
    {
        _js = js;
        Style = ThemeCatalog.NormalizeStyle(ThemeDefaults.Style);
        Palette = ThemeCatalog.NormalizePalette(ThemeDefaults.Palette);
    }

    public string Style { get; private set; }
    public string Palette { get; private set; }
    public bool IsNeoBrutalism => Style == "neo";
    public string ShellClass => $"{(IsNeoBrutalism ? "neo-brutalism" : "classic-ui")} palette-{Palette}";
    public string PaletteClass => $"palette-{Palette}";
    public ThemePalette CurrentPalette => ThemeCatalog.GetPalette(Palette);

    public MudTheme GetTheme(bool isDark) => ThemeCatalog.CreateTheme(Palette, isDark);

    public MudTheme LoginTheme => ThemeCatalog.CreateTheme(Palette, false, login: true);

    public async Task InitializeAsync()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;

        try
        {
            var savedStyle = await _js.InvokeAsync<string?>("localStorage.getItem", StyleStorageKey);
            var savedPalette = await _js.InvokeAsync<string?>("localStorage.getItem", PaletteStorageKey);

            if (!string.IsNullOrWhiteSpace(savedStyle))
            {
                Style = ThemeCatalog.NormalizeStyle(savedStyle);
            }

            if (!string.IsNullOrWhiteSpace(savedPalette))
            {
                Palette = ThemeCatalog.NormalizePalette(savedPalette);
            }
        }
        catch
        {
            // Storage can be unavailable during prerender; keep the generated defaults.
        }

        await ApplyDocumentAttributesAsync();
    }

    public async Task ToggleStyleAsync()
    {
        Style = IsNeoBrutalism ? "default" : "neo";
        await PersistAsync(StyleStorageKey, Style);
        await ApplyDocumentAttributesAsync();
    }

    public async Task SetStyleAsync(string? style)
    {
        Style = ThemeCatalog.NormalizeStyle(style);
        await PersistAsync(StyleStorageKey, Style);
        await ApplyDocumentAttributesAsync();
    }

    public async Task SetPaletteAsync(string? palette)
    {
        Palette = ThemeCatalog.NormalizePalette(palette);
        await PersistAsync(PaletteStorageKey, Palette);
        await ApplyDocumentAttributesAsync();
    }

    public async Task SetDarkModeAsync(bool isDark)
    {
        _isDarkMode = isDark;
        await ApplyDocumentAttributesAsync();
    }

    private async Task PersistAsync(string key, string value)
    {
        try
        {
            await _js.InvokeVoidAsync("localStorage.setItem", key, value);
        }
        catch
        {
            // The visual preference still works when storage is unavailable.
        }
    }

    private async Task ApplyDocumentAttributesAsync()
    {
        try
        {
            await _js.InvokeVoidAsync(
                "document.documentElement.setAttribute",
                "data-palette",
                Palette);
            await _js.InvokeVoidAsync(
                "document.documentElement.setAttribute",
                "data-style",
                Style);
            await _js.InvokeVoidAsync(
                "document.documentElement.setAttribute",
                ThemeModeAttribute,
                _isDarkMode ? "dark" : "light");
        }
        catch
        {
            // Attributes are applied again after the first interactive render.
        }
    }
}
