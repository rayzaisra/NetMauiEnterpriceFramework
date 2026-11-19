using Microsoft.EntityFrameworkCore;

using __PROJECT__.Application.DTOs.General;
using __PROJECT__.Application.Interfaces;
using __PROJECT__.Infrastructure.Data;

namespace __PROJECT__.Infrastructure.Queries;

public class DropDownQueryService : IDropDownService
{
    private readonly __PROJECT__DbContext _context;

    public DropDownQueryService(__PROJECT__DbContext context) => _context = context;

    public Task<List<DropdownItemDto>> GetGroupsAsync()
        => _context.um_group_app
            .Where(e => e.isActive != false && e.isDeleted != true)
            .OrderBy(e => e.group_name)
            .Select(e => new DropdownItemDto { Value = e.group_id, Text = e.group_name ?? e.group_id })
            .ToListAsync();

    public Task<List<DropdownItemDto>> GetGendersAsync()
        => _context.enum_gender
            .OrderBy(e => e.gender_code)
            .Select(e => new DropdownItemDto { Value = e.gender_code, Text = e.gender_name ?? e.gender_code })
            .ToListAsync();

    public Task<List<DropdownItemDto>> GetUsersAsync()
        => _context.um_user_app
            .Where(e => e.isActive != false && e.isDeleted != true)
            .OrderBy(e => e.user_id)
            .Select(e => new DropdownItemDto { Value = e.user_id, Text = e.full_name ?? e.user_id })
            .ToListAsync();

    public Task<List<DropdownItemDto>> GetMenuIconsAsync()
        => _context.enum_menu_icon
            .OrderBy(e => e.menu_icon_code)
            .Select(e => new DropdownItemDto
            {
                Value = e.menu_icon_code,
                Text = e.menu_icon_name ?? e.menu_icon_code,
            })
            .ToListAsync();

    public Task<List<DropdownItemDto>> GetMenuParentLevelsAsync()
        => _context.enum_menu_parent_level
            .OrderBy(e => e.parent_level_code)
            .Select(e => new DropdownItemDto
            {
                Value = e.parent_level_code,
                Text = e.parent_level_name ?? e.parent_level_code,
            })
            .ToListAsync();
}
