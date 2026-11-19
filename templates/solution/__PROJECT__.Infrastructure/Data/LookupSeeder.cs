using System.Reflection;
using System.Text;
using System.Text.Json;

using Microsoft.EntityFrameworkCore;

using __PROJECT__.Domain.Entities;
using __PROJECT__.Domain.Enums;
using __PROJECT__.Infrastructure.Data;

namespace __PROJECT__.Infrastructure.Data;

public static class LookupSeeder
{
    public static async Task SeedAsync(__PROJECT__DbContext context, CancellationToken cancellationToken = default)
    {
        await SeedMenuIconsAsync(context, cancellationToken);
        await SeedStatusApprovalsAsync(context, cancellationToken);
        await SeedApprovalTypesAsync(context, cancellationToken);
        await SeedGendersAsync(context, cancellationToken);
        await SeedTrackListAsync(context, cancellationToken);
        await SeedFileUploadTypesAsync(context, cancellationToken);
    }

    private static async Task SeedMenuIconsAsync(__PROJECT__DbContext context, CancellationToken cancellationToken)
    {
        var source = ReadSeed("enum_menu_icon");
        var catalog = source.RootElement.EnumerateArray()
            .Select(item => new enum_menu_icon
            {
                menu_icon_code = GetString(item, "menu_icon_code") ?? string.Empty,
                menu_icon_name = GetString(item, "menu_icon_name"),
                menu_icon_name_alternate = null,
            })
            .Where(e => !string.IsNullOrWhiteSpace(e.menu_icon_code))
            .GroupBy(e => e.menu_icon_code, StringComparer.Ordinal)
            .Select(e => e.First())
            .ToList();

        var catalogCodes = catalog.Select(e => e.menu_icon_code).ToHashSet(StringComparer.Ordinal);
        var existing = await context.enum_menu_icon.ToListAsync(cancellationToken);
        var legacy = existing
            .Where(e => !catalogCodes.Contains(e.menu_icon_code)
                && (IsLegacyIconCode(e.menu_icon_code) || !string.IsNullOrWhiteSpace(e.menu_icon_name_alternate)))
            .ToList();

        var map = BuildLegacyIconMap(legacy, catalogCodes);
        await NormalizeIconReferencesAsync(context, map, catalogCodes, cancellationToken);

        if (legacy.Count > 0)
        {
            context.enum_menu_icon.RemoveRange(legacy);
            await context.SaveChangesAsync(cancellationToken);
        }

        var existingByCode = (await context.enum_menu_icon.ToListAsync(cancellationToken))
            .ToDictionary(e => e.menu_icon_code, StringComparer.Ordinal);

        foreach (var icon in catalog)
        {
            if (existingByCode.TryGetValue(icon.menu_icon_code, out var row))
            {
                row.menu_icon_name = icon.menu_icon_name;
                row.menu_icon_name_alternate = null;
            }
            else
            {
                await context.enum_menu_icon.AddAsync(icon, cancellationToken);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static bool IsLegacyIconCode(string code)
        => code.StartsWith("fa ", StringComparison.OrdinalIgnoreCase)
            || code.StartsWith("fa-", StringComparison.OrdinalIgnoreCase);

    private static Dictionary<string, string> BuildLegacyIconMap(
        IEnumerable<enum_menu_icon> legacy,
        IReadOnlySet<string> catalogCodes)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var icon in legacy)
        {
            map[icon.menu_icon_code] = LegacyIconCodeAliases.TryGetValue(icon.menu_icon_code, out var direct)
                && catalogCodes.Contains(direct)
                    ? direct
                    : ResolveCatalogIcon(icon.menu_icon_name_alternate, catalogCodes)
                        ?? ResolveCatalogIcon(icon.menu_icon_name, catalogCodes)
                        ?? "Circle";
        }

        return map;
    }

    private static string? ResolveCatalogIcon(string? candidate, IReadOnlySet<string> catalogCodes)
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return null;
        }

        var code = candidate.Trim();
        if (catalogCodes.Contains(code))
        {
            return code;
        }

        return LegacyIconAliases.TryGetValue(code, out var alias) && catalogCodes.Contains(alias)
            ? alias
            : null;
    }

    private static async Task NormalizeIconReferencesAsync(
        __PROJECT__DbContext context,
        IReadOnlyDictionary<string, string> legacyMap,
        IReadOnlySet<string> catalogCodes,
        CancellationToken cancellationToken)
    {
        string Resolve(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "Circle";
            }

            var code = value.Trim();
            if (catalogCodes.Contains(code))
            {
                return code;
            }

            if (LegacyIconCodeAliases.TryGetValue(code, out var direct) && catalogCodes.Contains(direct))
            {
                return direct;
            }

            if (legacyMap.TryGetValue(code, out var mapped))
            {
                return mapped;
            }

            if (LegacyIconAliases.TryGetValue(code, out var alias) && catalogCodes.Contains(alias))
            {
                return alias;
            }

            return IsLegacyIconCode(code) ? "Circle" : code;
        }

        var menus = await context.um_menu
            .Where(e => e.menu_icon != null)
            .ToListAsync(cancellationToken);
        foreach (var menu in menus)
        {
            menu.menu_icon = Resolve(menu.menu_icon);
        }

        var cards = await context.tb_parameter_system
            .Where(e => e.icon != null)
            .ToListAsync(cancellationToken);
        foreach (var card in cards)
        {
            card.icon = Resolve(card.icon);
        }

        var payloads = await context.tb_parameter_sytem_temporary_detail
            .Where(e => e.column_name == "menu_icon")
            .ToListAsync(cancellationToken);
        foreach (var payload in payloads)
        {
            payload.value = Resolve(payload.value);
            payload.value_before = payload.value_before == null ? null : Resolve(payload.value_before);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static readonly Dictionary<string, string> LegacyIconCodeAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["fa fa-gear"] = "Settings",
        ["fa fa-user-circle"] = "EmojiPeople",
        ["fa fa-group"] = "Groups",
        ["fa fa-user-o"] = "Hail",
        ["fa fa-pagelines"] = "MenuBook",
        ["fa fa-cc-jcb"] = "AdminPanelSettings",
        ["fa fa-list-ul"] = "FormatListBulleted",
        ["fa fa-th-list"] = "Checklist",
        ["fa fa-cc-mastercard"] = "AddModerator",
        ["fa fa-cc-paypal"] = "GppGood",
        ["fa fa-flag"] = "Flag",
        ["fa fa-map-pin"] = "PinDrop",
        ["fa fa-flask"] = "Science",
        ["fa fa-envelope"] = "Email",
        ["fa fa-envelope-o"] = "Email",
    };

    private static readonly Dictionary<string, string> LegacyIconAliases = new(StringComparer.Ordinal)
    {
        ["AddSquare"] = "AddBox",
        ["Ambulance"] = "MedicalServices",
        ["ArrowDropLeft"] = "ArrowLeft",
        ["ArrowDropRight"] = "ArrowRight",
        ["Asterisk"] = "Star",
        ["Beer"] = "SportsBar",
        ["Bicycle"] = "DirectionsBike",
        ["Bitcoin"] = "CurrencyBitcoin",
        ["Bus"] = "DirectionsBus",
        ["CameraRetro"] = "PhotoCamera",
        ["Car"] = "DirectionsCar",
        ["CheckSquare"] = "CheckBox",
        ["CloseCircle"] = "Cancel",
        ["Commenting"] = "InsertComment",
        ["Comments"] = "Forum",
        ["Compass"] = "Explore",
        ["CurrencyWon"] = "Paid",
        ["Desktop"] = "DesktopWindows",
        ["EditSquare"] = "Edit",
        ["FlagCheckered"] = "SportsScore",
        ["Gbp"] = "CurrencyPound",
        ["H"] = "Circle",
        ["Labels"] = "Label",
        ["LifeRing"] = "Support",
        ["Lifebuoy"] = "Support",
        ["MailOpen"] = "MarkEmailRead",
        ["MailSquare"] = "Email",
        ["Motorcycle"] = "TwoWheeler",
        ["PhoneSquare"] = "Phone",
        ["RemoveSquare"] = "IndeterminateCheckBox",
        ["Road"] = "AddRoad",
        ["SearchMinus"] = "ZoomOut",
        ["SearchPlus"] = "ZoomIn",
        ["Ship"] = "DirectionsBoat",
        ["SortAscending"] = "Sort",
        ["SortDescending"] = "Sort",
        ["Taxi"] = "LocalTaxi",
        ["Unlock"] = "LockOpen",
        ["UnlockAlternative"] = "LockOpen",
    };

    private static async Task SeedStatusApprovalsAsync(__PROJECT__DbContext context, CancellationToken cancellationToken)
    {
        var existing = await context.enum_status_approval.Select(e => e.status_code).ToHashSetAsync(cancellationToken);
        var source = ReadSeed("enum_status_approval");
        foreach (var item in source.RootElement.EnumerateArray())
        {
            var code = GetInt(item, "status_code");
            if (code == null || existing.Contains(code.Value)) continue;
            await context.enum_status_approval.AddAsync(new enum_status_approval
            {
                status_code = code.Value,
                status_name = GetString(item, "status_name"),
            }, cancellationToken);
            existing.Add(code.Value);
        }
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedApprovalTypesAsync(__PROJECT__DbContext context, CancellationToken cancellationToken)
    {
        var existing = await context.enum_approval_type.Select(e => e.approval_type_code).ToHashSetAsync(cancellationToken);
        var source = ReadSeed("enum_approval_type");
        foreach (var item in source.RootElement.EnumerateArray())
        {
            var code = GetString(item, "approval_type_code");
            if (string.IsNullOrWhiteSpace(code) || existing.Contains(code)) continue;
            await context.enum_approval_type.AddAsync(new enum_approval_type
            {
                approval_type_code = code,
                approval_type_name = GetString(item, "approval_type_name"),
                is_active = GetBool(item, "is_active") ?? true,
            }, cancellationToken);
            existing.Add(code);
        }
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedGendersAsync(__PROJECT__DbContext context, CancellationToken cancellationToken)
    {
        var existing = await context.enum_gender.Select(e => e.gender_code).ToHashSetAsync(cancellationToken);
        var source = ReadSeed("enum_gender");
        foreach (var item in source.RootElement.EnumerateArray())
        {
            var code = GetString(item, "gender_code");
            if (string.IsNullOrWhiteSpace(code) || existing.Contains(code)) continue;
            await context.enum_gender.AddAsync(new enum_gender
            {
                gender_code = code,
                gender_name = GetString(item, "gender_name"),
                gender_T24_code = GetString(item, "gender_T24_code"),
                gender_dukcapil_code = GetString(item, "gender_dukcapil_code"),
            }, cancellationToken);
            existing.Add(code);
        }
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedTrackListAsync(__PROJECT__DbContext context, CancellationToken cancellationToken)
    {
        var existing = await context.enum_track_list.Select(e => e.tr_code).ToHashSetAsync(cancellationToken);
        var source = ReadSeed("enum_track_list");
        foreach (var item in source.RootElement.EnumerateArray())
        {
            var code = GetString(item, "tr_code");
            if (string.IsNullOrWhiteSpace(code) || existing.Contains(code)) continue;
            await context.enum_track_list.AddAsync(new enum_track_list
            {
                tr_code = code,
                tr_name = GetString(item, "tr_name"),
                is_active = GetBool(item, "is_active") ?? true,
                level_seq = GetInt(item, "level_seq"),
                tr_short_name = GetString(item, "tr_short_name"),
                tr_filter_name = GetString(item, "tr_filter_name"),
            }, cancellationToken);
            existing.Add(code);
        }
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedFileUploadTypesAsync(__PROJECT__DbContext context, CancellationToken cancellationToken)
    {
        var existing = await context.rf_files_upload_type.Select(e => e.files_upload_type_code).ToHashSetAsync(cancellationToken);
        var source = ReadSeed("rf_files_upload_type");
        var now = DateTime.Now;
        foreach (var item in source.RootElement.EnumerateArray())
        {
            var code = GetString(item, "files_upload_type_code");
            if (string.IsNullOrWhiteSpace(code) || existing.Contains(code)) continue;
            await context.rf_files_upload_type.AddAsync(new rf_files_upload_type
            {
                files_upload_type_code = code,
                files_upload_type_T24_code = GetString(item, "files_upload_type_T24_code"),
                files_upload_type_name = GetString(item, "files_upload_type_name"),
                statusApproval = Const.StatusApproval.APPROVED,
                isActive = true,
                isDeleted = false,
                createdBy = "system",
                createdDate = now,
            }, cancellationToken);
            existing.Add(code);
        }
        await context.SaveChangesAsync(cancellationToken);
    }

    private static JsonDocument ReadSeed(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resource = assembly.GetManifestResourceNames()
            .FirstOrDefault(e => e.EndsWith($".Seeds.{name}.json", StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Seed resource not found: {name}");

        using var stream = assembly.GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException($"Seed stream not found: {name}");
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var json = reader.ReadToEnd().TrimStart('\uFEFF');
        return JsonDocument.Parse(json);
    }

    private static string? GetString(JsonElement item, string name)
    {
        if (!item.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null) return null;
        return value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
    }

    private static int? GetInt(JsonElement item, string name)
    {
        if (!item.TryGetProperty(name, out var value)) return null;
        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number)) return number;
        return int.TryParse(value.ToString(), out var parsed) ? parsed : null;
    }

    private static bool? GetBool(JsonElement item, string name)
    {
        if (!item.TryGetProperty(name, out var value)) return null;
        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => bool.TryParse(value.ToString(), out var parsed) ? parsed : null,
        };
    }
}
