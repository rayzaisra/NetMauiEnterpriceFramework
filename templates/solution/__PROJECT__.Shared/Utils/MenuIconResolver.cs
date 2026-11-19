using System.Reflection;

using MudBlazor;

namespace __PROJECT__.Shared.Utils;

public static class MenuIconResolver
{
    private static readonly Type FilledIcons = typeof(Icons.Material.Filled);

    public static string GetIcon(string? iconName)
    {
        if (string.IsNullOrWhiteSpace(iconName))
        {
            return Icons.Material.Filled.Circle;
        }

        var name = iconName.Trim();
        var field = FilledIcons.GetField(name, BindingFlags.Public | BindingFlags.Static);
        if (field?.GetValue(null) is string icon)
        {
            return icon;
        }

        if (char.IsLower(name[0]))
        {
            var pascalCase = char.ToUpperInvariant(name[0]) + name[1..];
            field = FilledIcons.GetField(pascalCase, BindingFlags.Public | BindingFlags.Static);
            if (field?.GetValue(null) is string pascalIcon)
            {
                return pascalIcon;
            }
        }

        return Icons.Material.Filled.Circle;
    }
}
