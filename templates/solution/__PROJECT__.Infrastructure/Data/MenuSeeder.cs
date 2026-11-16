using Microsoft.EntityFrameworkCore;

using __PROJECT__.Domain.Entities;
using __PROJECT__.Domain.Enums;

namespace __PROJECT__.Infrastructure.Data;

public static class MenuSeeder
{
    private static readonly (string Code, string Name, string Alternate)[] Icons =
    {
        ("fa fa-gear", "gear", "Settings"),
        ("fa fa-user-circle", "user-circle", "EmojiPeople"),
        ("fa fa-group", "group", "Groups"),
        ("fa fa-user-o", "user-o", "Hail"),
        ("fa fa-pagelines", "pagelines", "MenuBook"),
        ("fa fa-cc-jcb", "cc-jcb", "AdminPanelSettings"),
        ("fa fa-list-ul", "list-ul", "FormatListBulleted"),
        ("fa fa-th-list", "th-list", "Checklist"),
        ("fa fa-cc-mastercard", "cc-mastercard", "AddModerator"),
        ("fa fa-cc-paypal", "cc-paypal", "GppGood"),
        ("fa fa-flag", "flag", "Flag"),
        ("fa fa-map-pin", "map-pin", "PinDrop"),
        ("fa fa-envelope", "envelope", "Email"),
    };

    private static readonly (string Unique, string Name, string? Url, string Level, string Parent, string Icon, int Order, string? Component)[] Menus =
    {
        ("MNT", "Setting", null, "A", "", "fa fa-gear", 1, null),

        ("MNT-USER", "User Maintenance", null, "B", "MNT", "fa fa-user-circle", 1, null),
        ("MNT-GRP", "Group Maintenance", null, "B", "MNT", "fa fa-group", 2, null),
        ("MNT-ROLE", "Role Maintenance", null, "B", "MNT", "fa fa-user-o", 3, null),
        ("MNT-MENU", "Menu Maintenance", null, "B", "MNT", "fa fa-pagelines", 4, null),
        ("MNT-PARAM", "Parameter Maintenance", null, "B", "MNT", "fa fa-cc-jcb", 5, null),
        ("MNT-DEMO", "Demo Screens", null, "B", "MNT", "fa fa-flask", 6, null),

        ("MNT-USER-LST", "List", "/dashboard/maintenance/user-list", "D", "MNT-USER", "fa fa-list-ul", 1, "__PROJECT__.Shared.Pages.Maintenance.UserList, __PROJECT__.Shared"),
        ("MNT-USER-APR", "Approval", "/dashboard/maintenance/user-approval", "D", "MNT-USER", "fa fa-th-list", 2, "__PROJECT__.Shared.Pages.Maintenance.UserApproval, __PROJECT__.Shared"),
        ("MNT-USER-FLAG", "User Flag", "/dashboard/maintenance/user-flag", "D", "MNT-USER", "fa fa-flag", 3, "__PROJECT__.Shared.Pages.Maintenance.UserFlag, __PROJECT__.Shared"),

        ("MNT-GRP-LST", "List", "/dashboard/maintenance/group-list", "D", "MNT-GRP", "fa fa-list-ul", 1, "__PROJECT__.Shared.Pages.Maintenance.GroupList, __PROJECT__.Shared"),
        ("MNT-GRP-APR", "Approval", "/dashboard/maintenance/group-approval", "D", "MNT-GRP", "fa fa-th-list", 2, "__PROJECT__.Shared.Pages.Maintenance.GroupApproval, __PROJECT__.Shared"),

        ("MNT-ROLE-LST", "List", "/dashboard/maintenance/role-list", "D", "MNT-ROLE", "fa fa-list-ul", 1, "__PROJECT__.Shared.Pages.Maintenance.RoleList, __PROJECT__.Shared"),
        ("MNT-ROLE-APR", "Approval", "/dashboard/maintenance/role-approval", "D", "MNT-ROLE", "fa fa-th-list", 2, "__PROJECT__.Shared.Pages.Maintenance.RoleApproval, __PROJECT__.Shared"),

        ("MNT-MENU-LST", "List", "/dashboard/maintenance/menu-list", "D", "MNT-MENU", "fa fa-list-ul", 1, "__PROJECT__.Shared.Pages.Maintenance.MenuList, __PROJECT__.Shared"),
        ("MNT-MENU-APR", "Approval", "/dashboard/maintenance/menu-approval", "D", "MNT-MENU", "fa fa-th-list", 2, "__PROJECT__.Shared.Pages.Maintenance.MenuApproval, __PROJECT__.Shared"),
        ("MNT-MENU-MAP", "Mapping Menu", "/dashboard/maintenance/menu-map", "D", "MNT-MENU", "fa fa-map-pin", 3, "__PROJECT__.Shared.Pages.Maintenance.MenuMap, __PROJECT__.Shared"),

        ("MNT-PARAM-MKR", "Maker", "/dashboard/maintenance/parameter-system/maker", "D", "MNT-PARAM", "fa fa-cc-mastercard", 1, "__PROJECT__.Shared.Pages.Maintenance.Parameter.ParameterSystem, __PROJECT__.Shared"),
        ("MNT-PARAM-APR", "Approval", "/dashboard/maintenance/parameter-system/approval", "D", "MNT-PARAM", "fa fa-cc-paypal", 2, "__PROJECT__.Shared.Pages.Maintenance.Parameter.ParameterSystem, __PROJECT__.Shared"),

        ("MNT-DEMO-VAL", "Default Value", "SM-DEFAULT-VALUE", "D", "MNT-DEMO", "fa fa-list-ul", 1, "DynamicScreen"),
        ("MNT-DEMO-URL", "URL Demo", "SM-URL-DEMO", "D", "MNT-DEMO", "fa fa-map-pin", 2, "DynamicScreen"),
    };

    public static async Task SeedAsync(__PROJECT__DbContext context, CancellationToken cancellationToken = default)
    {
        if (await context.um_menu.AnyAsync(e => e.menu_unique == "MNT", cancellationToken))
        {
            await RemoveEmailMenusAsync(context, cancellationToken);
            return;
        }

        foreach (var icon in Icons)
        {
            if (!await context.enum_menu_icon.AnyAsync(e => e.menu_icon_code == icon.Code, cancellationToken))
            {
                await context.enum_menu_icon.AddAsync(new enum_menu_icon
                {
                    menu_icon_code = icon.Code,
                    menu_icon_name = icon.Name,
                    menu_icon_name_alternate = icon.Alternate,
                }, cancellationToken);
            }
        }

        var now = DateTime.Now;
        var menuEntities = new Dictionary<string, um_menu>();

        foreach (var item in Menus)
        {
            var entity = new um_menu
            {
                menu_unique = item.Unique,
                menu_name = item.Name,
                menu_url = item.Url,
                parent_level_id = item.Level,
                menu_unique_parent = item.Parent,
                menu_icon = item.Icon,
                order_seq = item.Order,
                component_type = item.Component,
                statusApproval = Const.StatusApproval.APPROVED,
                isActive = true,
                isDeleted = false,
                isLocked = false,
                isDefault = false,
                createdBy = "system",
                createdDate = now,
            };

            menuEntities[item.Unique] = entity;
            await context.um_menu.AddAsync(entity, cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);

        foreach (var menu in menuEntities.Values)
        {
            await context.um_group_menu.AddAsync(new um_group_menu
            {
                group_id = Const.DEFAULT_GROUP_ID_ADMINISTRATOR_CODE,
                menu_id = menu.id,
                statusApproval = Const.StatusApproval.APPROVED,
                isActive = true,
                isDeleted = false,
                isLocked = false,
                isDefault = false,
                createdBy = "system",
                createdDate = now,
            }, cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task RemoveEmailMenusAsync(
        __PROJECT__DbContext context,
        CancellationToken cancellationToken)
    {
        var root = await context.um_menu
            .FirstOrDefaultAsync(e => e.menu_unique == "MNT-EMAIL", cancellationToken);
        if (root == null)
        {
            return;
        }

        var menuIds = new List<long> { root.id };
        var parentCodes = new List<string> { "MNT-EMAIL" };

        while (parentCodes.Count > 0)
        {
            var children = await context.um_menu
                .Where(e => e.menu_unique_parent != null && parentCodes.Contains(e.menu_unique_parent))
                .ToListAsync(cancellationToken);
            if (children.Count == 0)
            {
                break;
            }

            menuIds.AddRange(children.Select(e => e.id));
            parentCodes = children
                .Where(e => !string.IsNullOrWhiteSpace(e.menu_unique))
                .Select(e => e.menu_unique!)
                .ToList();
        }

        var mappings = await context.um_group_menu
            .Where(e => e.menu_id.HasValue && menuIds.Contains(e.menu_id.Value))
            .ToListAsync(cancellationToken);
        context.um_group_menu.RemoveRange(mappings);

        var menus = await context.um_menu
            .Where(e => menuIds.Contains(e.id))
            .ToListAsync(cancellationToken);
        context.um_menu.RemoveRange(menus);

        await context.SaveChangesAsync(cancellationToken);
    }
}
