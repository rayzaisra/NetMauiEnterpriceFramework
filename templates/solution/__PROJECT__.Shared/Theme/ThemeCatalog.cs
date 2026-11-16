using MudBlazor;

namespace __PROJECT__.Shared.Theme;

public static class ThemeCatalog
{
    private static readonly ThemePalette[] PaletteList =
    [
        new()
        {
            Id = "default",
            Label = "Default",
            LightBackground = "#f9f9ff",
            LightSurface = "#ffffff",
            LightSecondary = "#0f172a",
            LightSoft = "#e7eeff",
            LightAccent = "#004481",
            LightAccentInk = "#ffffff",
            LightTextStrong = "#111c2d",
            LightText = "#263143",
            LightMuted = "#5b6472",
            LightLoginOverlay = "#eef4ff",
            LightLoginPanel = "#edf3ff",
            LightLoginForm = "#f4f7ff",
            LightMudSecondary = "#b6171e",
            DarkBackground = "#111c2d",
            DarkSurface = "#1b2638",
            DarkAppbarBackground = "#162235",
            DarkDrawerBackground = "#162235",
            DarkSecondary = "#e8eef9",
            DarkSoft = "#263143",
            DarkAccent = "#a5c8ff",
            DarkAccentInk = "#101828",
            DarkTextStrong = "#e8eef9",
            DarkText = "#dbe6f5",
            DarkMuted = "#b8c4d9",
            DarkMudSecondary = "#ffb3ac",
            LightNeoInk = "#0f172a",
            DarkNeoInk = "#f1f5f9",
        },
        new()
        {
            Id = "fintech",
            Label = "FinTech",
            LightBackground = "#f8fafc",
            LightSurface = "#ffffff",
            LightSecondary = "#1e293b",
            LightSoft = "#e2e8f0",
            LightAccent = "#10b981",
            LightAccentInk = "#052e16",
            LightTextStrong = "#0f172a",
            LightText = "#334155",
            LightMuted = "#64748b",
            LightLoginOverlay = "#f1f5f9",
            LightLoginPanel = "#e2e8f0",
            LightLoginForm = "#f8fafc",
            LightMudSecondary = "#dc2626",
            DarkBackground = "#0b1120",
            DarkSurface = "#111827",
            DarkAppbarBackground = "#111827",
            DarkDrawerBackground = "#111827",
            DarkSecondary = "#e2e8f0",
            DarkSoft = "#1e293b",
            DarkAccent = "#34d399",
            DarkAccentInk = "#052e16",
            DarkTextStrong = "#f8fafc",
            DarkText = "#cbd5e1",
            DarkMuted = "#94a3b8",
            DarkMudSecondary = "#f87171",
            LightNeoInk = "#0f172a",
            DarkNeoInk = "#e2e8f0",
        },
        new()
        {
            Id = "monochrome",
            Label = "Black & White",
            LightBackground = "#ffffff",
            LightSurface = "#ffffff",
            LightSecondary = "#18181b",
            LightSoft = "#e4e4e7",
            LightAccent = "#09090b",
            LightAccentInk = "#ffffff",
            LightTextStrong = "#18181b",
            LightText = "#3f3f46",
            LightMuted = "#52525b",
            LightLoginOverlay = "#f4f4f5",
            LightLoginPanel = "#e4e4e7",
            LightLoginForm = "#fafafa",
            LightMudSecondary = "#52525b",
            DarkBackground = "#09090b",
            DarkSurface = "#18181b",
            DarkAppbarBackground = "#18181b",
            DarkDrawerBackground = "#18181b",
            DarkSecondary = "#fafafa",
            DarkSoft = "#27272a",
            DarkAccent = "#fafafa",
            DarkAccentInk = "#09090b",
            DarkTextStrong = "#fafafa",
            DarkText = "#d4d4d8",
            DarkMuted = "#a1a1aa",
            DarkMudSecondary = "#d4d4d8",
            LightNeoInk = "#09090b",
            DarkNeoInk = "#fafafa",
        },
        new()
        {
            Id = "creative",
            Label = "Creative Colorful",
            LightBackground = "#fff8f2",
            LightSurface = "#ffffff",
            LightSecondary = "#312e81",
            LightSoft = "#ede9fe",
            LightAccent = "#ec4899",
            LightAccentInk = "#ffffff",
            LightTextStrong = "#1e1b4b",
            LightText = "#4c1d95",
            LightMuted = "#6b7280",
            LightLoginOverlay = "#fff1f2",
            LightLoginPanel = "#fce7f3",
            LightLoginForm = "#fff8f2",
            LightMudSecondary = "#f59e0b",
            DarkBackground = "#1e1b4b",
            DarkSurface = "#2e1065",
            DarkAppbarBackground = "#2e1065",
            DarkDrawerBackground = "#2e1065",
            DarkSecondary = "#f5f3ff",
            DarkSoft = "#4c1d95",
            DarkAccent = "#f472b6",
            DarkAccentInk = "#3b0764",
            DarkTextStrong = "#f5f3ff",
            DarkText = "#ddd6fe",
            DarkMuted = "#c4b5fd",
            DarkMudSecondary = "#fbbf24",
            LightNeoInk = "#1e1b4b",
            DarkNeoInk = "#f5f3ff",
        },
        new()
        {
            Id = "wellness",
            Label = "Wellness",
            LightBackground = "#faf5f0",
            LightSurface = "#ffffff",
            LightSecondary = "#3f4a43",
            LightSoft = "#e8e4da",
            LightAccent = "#d97757",
            LightAccentInk = "#2f1b14",
            LightTextStrong = "#2f3a33",
            LightText = "#4f5d53",
            LightMuted = "#6b7280",
            LightLoginOverlay = "#f2ede5",
            LightLoginPanel = "#e8e4da",
            LightLoginForm = "#faf5f0",
            LightMudSecondary = "#b45309",
            DarkBackground = "#1c2420",
            DarkSurface = "#27332d",
            DarkAppbarBackground = "#27332d",
            DarkDrawerBackground = "#27332d",
            DarkSecondary = "#e8e4da",
            DarkSoft = "#33413a",
            DarkAccent = "#e8a87c",
            DarkAccentInk = "#2f1b14",
            DarkTextStrong = "#f4f1ea",
            DarkText = "#d7ddd5",
            DarkMuted = "#b8c4b8",
            DarkMudSecondary = "#fbbf24",
            LightNeoInk = "#2f3a33",
            DarkNeoInk = "#e8e4da",
        },
    ];

    public static IReadOnlyList<ThemePalette> Palettes => PaletteList;

    public static string NormalizeStyle(string? style)
    {
        if (string.Equals(style, "default", StringComparison.OrdinalIgnoreCase)
            || string.Equals(style, "classic", StringComparison.OrdinalIgnoreCase))
        {
            return "default";
        }

        return "neo";
    }

    public static string NormalizePalette(string? palette)
    {
        var value = (palette ?? string.Empty).Trim().ToLowerInvariant();
        return value switch
        {
            "fintech" => "fintech",
            "monochrome" or "black-white" or "blackwhite" or "bw" => "monochrome",
            "creative" or "colorful" => "creative",
            "wellness" => "wellness",
            _ => "default",
        };
    }

    public static ThemePalette GetPalette(string? palette)
    {
        var id = NormalizePalette(palette);
        return PaletteList.FirstOrDefault(item => item.Id == id) ?? PaletteList[0];
    }

    public static MudTheme CreateTheme(string? paletteId, bool isDark, bool login = false)
    {
        var palette = GetPalette(paletteId);

        if (login)
        {
            return new MudTheme
            {
                PaletteLight = new PaletteLight
                {
                    Primary = palette.LightAccent,
                    PrimaryContrastText = palette.LightAccentInk,
                    Secondary = palette.LightMudSecondary,
                    Background = palette.LightSoft,
                    Surface = palette.LightSurface,
                    AppbarBackground = palette.LightSurface,
                },
                Typography = new Typography
                {
                    Default = new DefaultTypography
                    {
                        FontFamily = new[] { "Plus Jakarta Sans", "sans-serif" },
                    },
                },
            };
        }

        return new MudTheme
        {
            PaletteLight = new PaletteLight
            {
                Primary = palette.LightAccent,
                PrimaryContrastText = palette.LightAccentInk,
                Secondary = palette.LightMudSecondary,
                Background = palette.LightBackground,
                Surface = palette.LightSurface,
                AppbarBackground = palette.LightSurface,
                AppbarText = palette.LightAccent,
                DrawerBackground = palette.LightSurface,
                DrawerText = palette.LightText,
                TextPrimary = palette.LightTextStrong,
                TextSecondary = palette.LightMuted,
                ActionDefault = palette.LightMuted,
            },
            PaletteDark = new PaletteDark
            {
                Primary = palette.DarkAccent,
                PrimaryContrastText = palette.DarkAccentInk,
                Secondary = palette.DarkMudSecondary,
                Background = palette.DarkBackground,
                Surface = palette.DarkSurface,
                AppbarBackground = palette.DarkAppbarBackground,
                AppbarText = palette.DarkTextStrong,
                DrawerBackground = palette.DarkDrawerBackground,
                DrawerText = palette.DarkText,
                TextPrimary = palette.DarkTextStrong,
                TextSecondary = palette.DarkMuted,
                ActionDefault = palette.DarkMuted,
                Divider = "rgba(255, 255, 255, .12)",
            },
            Typography = new Typography
            {
                Default = new DefaultTypography
                {
                    FontFamily = new[] { "Plus Jakarta Sans", "sans-serif" },
                },
            },
        };
    }
}
