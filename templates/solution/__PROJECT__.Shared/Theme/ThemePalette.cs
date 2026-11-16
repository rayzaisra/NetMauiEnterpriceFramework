namespace __PROJECT__.Shared.Theme;

public sealed class ThemePalette
{
    public required string Id { get; init; }
    public required string Label { get; init; }

    public required string LightBackground { get; init; }
    public required string LightSurface { get; init; }
    public required string LightSecondary { get; init; }
    public required string LightSoft { get; init; }
    public required string LightAccent { get; init; }
    public required string LightAccentInk { get; init; }
    public required string LightTextStrong { get; init; }
    public required string LightText { get; init; }
    public required string LightMuted { get; init; }
    public required string LightLoginOverlay { get; init; }
    public required string LightLoginPanel { get; init; }
    public required string LightLoginForm { get; init; }
    public required string LightMudSecondary { get; init; }

    public required string DarkBackground { get; init; }
    public required string DarkSurface { get; init; }
    public required string DarkAppbarBackground { get; init; }
    public required string DarkDrawerBackground { get; init; }
    public required string DarkSecondary { get; init; }
    public required string DarkSoft { get; init; }
    public required string DarkAccent { get; init; }
    public required string DarkAccentInk { get; init; }
    public required string DarkTextStrong { get; init; }
    public required string DarkText { get; init; }
    public required string DarkMuted { get; init; }
    public required string DarkMudSecondary { get; init; }

    public required string LightNeoInk { get; init; }
    public required string DarkNeoInk { get; init; }
}
