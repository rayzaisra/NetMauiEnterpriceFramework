using Microsoft.EntityFrameworkCore;

using __PROJECT__.Application.DTOs.General;
using __PROJECT__.Application.DTOs.Menu;
using __PROJECT__.Application.Interfaces.Queries;
using __PROJECT__.Domain.Enums;
using __PROJECT__.Infrastructure.Data;

namespace __PROJECT__.Infrastructure.Queries;

public class MenuQueryService : IMenuQueryService
{
    private readonly __PROJECT__DbContext _context;

    public MenuQueryService(__PROJECT__DbContext context) => _context = context;

    public async Task<GeneralListResponseDto<MenuListItemDto>> GetListAsync(
        DataTableRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, request.PageNumber);
        var pageSize = request.PageSize <= 0 ? 10 : Math.Min(request.PageSize, 200);

        var query = from m in _context.um_menu
                    join i in _context.enum_menu_icon on m.menu_icon equals i.menu_icon_code into iJoin
                    from i in iJoin.DefaultIfEmpty()
                    join p in _context.um_menu on m.menu_unique_parent equals p.menu_unique into pJoin
                    from p in pJoin.DefaultIfEmpty()
                    where m.isDeleted != true
                    select new MenuListItemDto
                    {
                        Id = m.id,
                        MenuUnique = m.menu_unique,
                        MenuName = m.menu_name,
                        MenuUrl = m.menu_url,
                        ParentLevelId = m.parent_level_id,
                        MenuUniqueParent = m.menu_unique_parent,
                        ParentName = p != null ? p.menu_name : null,
                        MenuIcon = m.menu_icon,
                        MenuIconName = i != null
                            ? (i.menu_icon_name ?? m.menu_icon)
                            : m.menu_icon,
                        OrderSeq = m.order_seq,
                        ComponentType = m.component_type,
                        IsActive = m.isActive == true,
                        StatusApproval = m.statusApproval,
                    };

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.Trim();
            query = query.Where(x =>
                x.MenuUnique.Contains(search)
                || (x.MenuName != null && x.MenuName.Contains(search))
                || (x.MenuUrl != null && x.MenuUrl.Contains(search)));
        }

        var desc = string.Equals(request.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        query = (request.SortColumn ?? string.Empty).ToLowerInvariant() switch
        {
            "id" => desc ? query.OrderByDescending(x => x.Id) : query.OrderBy(x => x.Id),
            "menuname" => desc ? query.OrderByDescending(x => x.MenuName) : query.OrderBy(x => x.MenuName),
            "menuunique" => desc ? query.OrderByDescending(x => x.MenuUnique) : query.OrderBy(x => x.MenuUnique),
            "parentlevelid" => desc ? query.OrderByDescending(x => x.ParentLevelId) : query.OrderBy(x => x.ParentLevelId),
            "menuparent" => desc ? query.OrderByDescending(x => x.ParentName) : query.OrderBy(x => x.ParentName),
            "orderseq" => desc ? query.OrderByDescending(x => x.OrderSeq) : query.OrderBy(x => x.OrderSeq),
            "isactive" => desc ? query.OrderByDescending(x => x.IsActive) : query.OrderBy(x => x.IsActive),
            _ => desc ? query.OrderByDescending(x => x.Id) : query.OrderBy(x => x.Id),
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var data = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new GeneralListResponseDto<MenuListItemDto>
        {
            Data = data,
            TotalCount = totalCount,
        };
    }

    public async Task<List<MenuTreeItemDto>> GetTreeAsync(CancellationToken cancellationToken = default)
    {
        var menus = await _context.um_menu
            .Where(e => e.isDeleted != true)
            .OrderBy(e => e.order_seq)
            .Select(e => new MenuTreeItemDto
            {
                Id = e.id,
                MenuUnique = e.menu_unique,
                MenuName = e.menu_name,
                MenuIcon = e.menu_icon,
                ParentLevelId = e.parent_level_id,
                MenuUniqueParent = e.menu_unique_parent,
                OrderSeq = e.order_seq,
            })
            .ToListAsync(cancellationToken);

        var lookup = menus.ToLookup(e => e.MenuUniqueParent ?? string.Empty);
        foreach (var menu in menus)
        {
            menu.Children = lookup[menu.MenuUnique].OrderBy(e => e.OrderSeq).ToList();
        }

        return menus
            .Where(e => e.ParentLevelId == Const.ParentLevel.FIRST_PARENT && string.IsNullOrEmpty(e.MenuUniqueParent))
            .OrderBy(e => e.OrderSeq)
            .ToList();
    }

    public Task<List<long>> GetAssignedMenuIdsAsync(
        string groupId,
        CancellationToken cancellationToken = default)
        => _context.um_group_menu
            .Where(e => e.group_id == groupId && e.isActive != false && e.isDeleted != true)
            .Select(e => e.menu_id)
            .Where(e => e != null)
            .Select(e => e!.Value)
            .ToListAsync(cancellationToken);

    public async Task<List<MenuItemDto>> GetMenuTreeByGroupIdAsync(
        string groupId,
        CancellationToken cancellationToken = default)
    {
        var menuIds = await _context.um_group_menu
            .Where(gm => gm.group_id == groupId && gm.isActive != false && gm.isDeleted != true)
            .Select(gm => gm.menu_id)
            .Where(id => id != null)
            .Select(id => id!.Value)
            .ToListAsync(cancellationToken);

        if (menuIds.Count == 0)
        {
            return new List<MenuItemDto>();
        }

        var menus = await _context.um_menu
            .Where(m => menuIds.Contains(m.id) && m.isActive != false && m.isDeleted != true)
            .Select(m => new MenuItemDto
            {
                Id = m.id,
                MenuUnique = m.menu_unique,
                MenuName = m.menu_name ?? string.Empty,
                MenuUrl = m.menu_url,
                ParentLevelId = m.parent_level_id,
                MenuUniqueParent = m.menu_unique_parent,
                MenuIcon = string.IsNullOrWhiteSpace(m.menu_icon) ? "Circle" : m.menu_icon,
                OrderSeq = m.order_seq,
                ComponentType = m.component_type,
            })
            .ToListAsync(cancellationToken);

        var lookup = menus.ToLookup(m => m.MenuUniqueParent ?? string.Empty);
        foreach (var menu in menus)
        {
            menu.Children = lookup[menu.MenuUnique].OrderBy(c => c.OrderSeq).ToList();
        }

        return menus
            .Where(m => m.ParentLevelId == "A" && string.IsNullOrEmpty(m.MenuUniqueParent))
            .OrderBy(m => m.OrderSeq)
            .ToList();
    }
}
