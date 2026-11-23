using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using __PROJECT__.Api.Authorization;
using __PROJECT__.Api.Helpers;
using __PROJECT__.Application.Common;
using __PROJECT__.Application.DTOs.General;
using __PROJECT__.Application.DTOs.Menu;
using __PROJECT__.Application.Interfaces;

namespace __PROJECT__.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MenuController : ControllerBase
{
    private readonly IMenuService _menuService;
    private readonly IMenuManagementService _menuManagementService;
    private readonly IUnitOfWork _unitOfWork;

    public MenuController(
        IMenuService menuService,
        IMenuManagementService menuManagementService,
        IUnitOfWork unitOfWork)
    {
        _menuService = menuService;
        _menuManagementService = menuManagementService;
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<IActionResult> GetUserMenu()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var userGroup = await _unitOfWork.UmUserGroup.GetByIdAsync(userId);
        if (userGroup == null || string.IsNullOrEmpty(userGroup.group_id))
        {
            return Ok(ApiResponseFactory.Success(new List<MenuItemDto>(), "Menus retrieved successfully"));
        }

        var menu = await _menuService.GetMenuTreeByGroupIdAsync(userGroup.group_id);
        return Ok(ApiResponseFactory.Success(menu, "Menus retrieved successfully"));
    }

    [HttpPost("list")]
    [RequireMenuAccess(PermissionAction.Load, "MNT-MENU-LST")]
    public async Task<IActionResult> GetList([FromBody] DataTableRequestDto request)
    {
        var result = await _menuManagementService.GetListAsync(request);
        return Ok(ApiResponseFactory.Page(result.Data, request, result.TotalCount, "Menus retrieved successfully"));
    }

    [HttpGet("detail/{id:long}")]
    [RequireMenuAccess(PermissionAction.Load, "MNT-MENU-LST")]
    public async Task<IActionResult> GetDetail(long id)
    {
        var menu = await _menuManagementService.GetByIdAsync(id);
        return menu == null
            ? NotFound(ApiResponseFactory.NotFound<MenuEditDto>("Menu not found."))
            : Ok(ApiResponseFactory.Success(menu, "Menu retrieved successfully"));
    }

    [HttpPost]
    [RequireMenuAccess(PermissionAction.AddEdit, "MNT-MENU-LST")]
    public async Task<IActionResult> Create([FromBody] MenuEditDto dto)
        => ToActionResult(await _menuManagementService.CreateAsync(dto));

    [HttpPut("{id:long}")]
    [RequireMenuAccess(PermissionAction.AddEdit, "MNT-MENU-LST")]
    public async Task<IActionResult> Update(long id, [FromBody] MenuEditDto dto)
        => ToActionResult(await _menuManagementService.UpdateAsync(id, dto));

    [HttpDelete("{id:long}")]
    [RequireMenuAccess(PermissionAction.Delete, "MNT-MENU-LST")]
    public async Task<IActionResult> Delete(long id)
        => ToActionResult(await _menuManagementService.DeleteAsync(id));

    [HttpGet("tree")]
    [RequireMenuAccess(PermissionAction.Load, "MNT-MENU-LST", "MNT-MENU-MAP")]
    public async Task<IActionResult> GetTree()
        => Ok(ApiResponseFactory.Success(await _menuManagementService.GetTreeAsync(), "Menu tree retrieved successfully"));

    [HttpGet("assigned/{groupId}")]
    [RequireMenuAccess(PermissionAction.Load, "MNT-MENU-MAP")]
    public async Task<IActionResult> GetAssigned(string groupId)
        => Ok(ApiResponseFactory.Success(await _menuManagementService.GetAssignedMenuIdsAsync(groupId), "Assigned menus retrieved successfully"));

    [HttpPost("save-map")]
    [RequireMenuAccess(PermissionAction.AddEdit, "MNT-MENU-MAP")]
    public async Task<IActionResult> SaveMap([FromBody] MenuMapDto dto)
        => ToActionResult(await _menuManagementService.SaveMenuMapAsync(dto));

    private IActionResult ToActionResult(BaseGeneralResponse result)
    {
        var response = ApiResponseFactory.FromResult(result);
        return result.Success ? Ok(response) : BadRequest(response);
    }
}
