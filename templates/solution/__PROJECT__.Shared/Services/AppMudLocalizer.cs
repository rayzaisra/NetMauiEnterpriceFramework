using Microsoft.Extensions.Localization;
using MudBlazor;

namespace __PROJECT__.Shared.Services;

public sealed class AppMudLocalizer : MudLocalizer
{
    private readonly LanguageService _language;

    public AppMudLocalizer(LanguageService language)
    {
        _language = language;
    }

    public override LocalizedString this[string key]
    {
        get
        {
            var value = _language.Get(key);
            var found = !string.Equals(value, key, StringComparison.Ordinal);
            return new LocalizedString(key, value, !found);
        }
    }

    public override LocalizedString this[string key, params object[] arguments]
    {
        get
        {
            var value = _language.Format(key, arguments);
            var found = !string.Equals(value, key, StringComparison.Ordinal);
            return new LocalizedString(key, value, !found);
        }
    }
}
