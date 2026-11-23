using Microsoft.EntityFrameworkCore;

using __PROJECT__.Application.Common;
using __PROJECT__.Application.Interfaces;
using __PROJECT__.Domain.Enums;
using __PROJECT__.Infrastructure.Data;

namespace __PROJECT__.Infrastructure.Services;

public class PermissionService : IPermissionService
{
    private readonly __PROJECT__DbContext _context;
    private readonly ICurrentUserService _currentUser;

    public PermissionService(__PROJECT__DbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<bool> HasAccessAsync(PermissionAction action, params string[] menuUniques)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return false;
        }

        var role = await _context.um_user_group
            .FirstOrDefaultAsync(e => e.user_id == userId && e.isActive != false && e.isDeleted != true);
        if (role == null)
        {
            return false;
        }

        var isSuperAdmin = string.Equals(
            role.group_id,
            Const.DEFAULT_GROUP_ID_ADMINISTRATOR_CODE,
            StringComparison.Ordinal);

        if (!PermissionRules.Can(
                isSuperAdmin,
                role.can_add_edit,
                role.can_delete,
                role.can_read_only,
                action))
        {
            return false;
        }

        if (isSuperAdmin || menuUniques.Length == 0)
        {
            return true;
        }

        var codes = menuUniques
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (codes.Length == 0)
        {
            return true;
        }

        return await (from mapping in _context.um_group_menu
                      join menu in _context.um_menu on mapping.menu_id equals menu.id
                      where mapping.group_id == role.group_id
                            && mapping.isActive != false
                            && mapping.isDeleted != true
                            && menu.isActive != false
                            && menu.isDeleted != true
                            && codes.Contains(menu.menu_unique)
                      select mapping.id).AnyAsync();
    }

    public async Task<bool> HasScreenAccessAsync(string screenId, PermissionAction action)
    {
        if (string.IsNullOrWhiteSpace(screenId))
        {
            return false;
        }

        var userId = _currentUser.UserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return false;
        }

        var role = await _context.um_user_group
            .FirstOrDefaultAsync(e => e.user_id == userId && e.isActive != false && e.isDeleted != true);
        if (role == null)
        {
            return false;
        }

        var isSuperAdmin = string.Equals(
            role.group_id,
            Const.DEFAULT_GROUP_ID_ADMINISTRATOR_CODE,
            StringComparison.Ordinal);

        if (!PermissionRules.Can(
                isSuperAdmin,
                role.can_add_edit,
                role.can_delete,
                role.can_read_only,
                action))
        {
            return false;
        }

        if (isSuperAdmin)
        {
            return true;
        }

        return await (from mapping in _context.um_group_menu
                      join menu in _context.um_menu on mapping.menu_id equals menu.id
                      where mapping.group_id == role.group_id
                            && mapping.isActive != false
                            && mapping.isDeleted != true
                            && menu.isActive != false
                            && menu.isDeleted != true
                            && menu.menu_url == screenId
                      select mapping.id).AnyAsync();
    }
}
