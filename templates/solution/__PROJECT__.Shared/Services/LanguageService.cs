using Microsoft.JSInterop;

namespace __PROJECT__.Shared.Services;

public sealed class LanguageService
{
    private const string StorageKey = "enterprise-ui-language";
    private const string LanguageAttribute = "data-language";

    private readonly IJSRuntime _js;
    private bool _initialized;

    public LanguageService(IJSRuntime js)
    {
        _js = js;
        Language = "en";
    }

    public event Action? OnChange;

    public string Language { get; private set; }

    public bool IsIndonesian => Language == "id";

    public string ToggleLabel => IsIndonesian ? "EN" : "ID";

    public string ToggleTitle => IsIndonesian ? "Switch to English" : "Ganti ke Bahasa Indonesia";

    public string ToggleAriaLabel => IsIndonesian ? "Switch to English" : "Ganti ke Bahasa Indonesia";

    public string this[string key] => Get(key);

    public string Get(string key)
    {
        if (UiStrings.TryGet(Language, key, out var value))
        {
            return value;
        }

        if (UiStrings.TryGet("en", key, out var fallback))
        {
            return fallback;
        }

        return key;
    }

    public string Format(string key, params object?[] args)
    {
        var template = Get(key);
        return args.Length == 0 ? template : string.Format(template, args);
    }

    public string Menu(string? menuUnique, string? fallbackName = null)
    {
        if (!string.IsNullOrWhiteSpace(menuUnique))
        {
            var key = $"Menu.{menuUnique.Trim()}";
            if (UiStrings.TryGet(Language, key, out var value))
            {
                return value;
            }

            if (UiStrings.TryGet("en", key, out var en))
            {
                return en;
            }
        }

        return fallbackName ?? string.Empty;
    }

    public async Task InitializeAsync()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;

        try
        {
            var saved = await _js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
            Language = Normalize(saved);
        }
        catch
        {
            _initialized = false;
        }

        await ApplyDocumentAttributesAsync();
    }

    public async Task ToggleAsync()
    {
        await SetLanguageAsync(IsIndonesian ? "en" : "id");
    }

    public async Task SetLanguageAsync(string? language)
    {
        var normalized = Normalize(language);
        if (string.Equals(Language, normalized, StringComparison.Ordinal))
        {
            return;
        }

        Language = normalized;
        try
        {
            await _js.InvokeVoidAsync("localStorage.setItem", StorageKey, Language);
        }
        catch
        {
            // The visual preference still works when storage is unavailable.
        }

        await ApplyDocumentAttributesAsync();
        OnChange?.Invoke();
    }

    private async Task ApplyDocumentAttributesAsync()
    {
        try
        {
            await _js.InvokeVoidAsync("document.documentElement.setAttribute", LanguageAttribute, Language);
            await _js.InvokeVoidAsync("document.documentElement.setAttribute", "lang", Language == "id" ? "id" : "en");
        }
        catch
        {
            // Attributes are applied again after the first interactive render.
        }
    }

    private static string Normalize(string? language)
        => string.Equals(language?.Trim(), "id", StringComparison.OrdinalIgnoreCase) ? "id" : "en";
}
