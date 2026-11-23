using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using __PROJECT__.Api.Authorization;
using __PROJECT__.Api.Helpers;
using __PROJECT__.Application.Common;
using __PROJECT__.Application.DTOs.General;
using __PROJECT__.Application.DTOs.Role;
using __PROJECT__.Application.Interfaces;

namespace __PROJECT__.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[RequireMenuAccess(PermissionAction.Load, "MNT-ROLE-LST")]
public class UserGroupController : ControllerBase
{
    private readonly IRoleManagementService _roleService;

    public UserGroupController(IRoleManagementService roleService) => _roleService = roleService;

    [HttpPost("list")]
    public async Task<IActionResult> GetList([FromBody] DataTableRequestDto request)
    {
        var result = await _roleService.GetListAsync(request);
        return Ok(ApiResponseFactory.Page(result.Data, request, result.TotalCount, "Roles retrieved successfully"));
    }

    [HttpGet("{userId}")]
    public async Task<IActionResult> Get(string userId)
    {
        var role = await _roleService.GetByIdAsync(userId);
        return role == null
            ? NotFound(ApiResponseFactory.NotFound<RoleEditDto>("Role not found."))
            : Ok(ApiResponseFactory.Success(role, "Role retrieved successfully"));
    }

    [HttpPost]
    [RequireMenuAccess(PermissionAction.AddEdit, "MNT-ROLE-LST")]
    public async Task<IActionResult> Create([FromBody] RoleEditDto dto)
        => ToActionResult(await _roleService.CreateAsync(dto));

    [HttpPut("{userId}")]
    [RequireMenuAccess(PermissionAction.AddEdit, "MNT-ROLE-LST")]
    public async Task<IActionResult> Update(string userId, [FromBody] RoleEditDto dto)
        => ToActionResult(await _roleService.UpdateAsync(userId, dto));

    [HttpDelete("{userId}")]
    [RequireMenuAccess(PermissionAction.Delete, "MNT-ROLE-LST")]
    public async Task<IActionResult> Delete(string userId)
        => ToActionResult(await _roleService.DeleteAsync(userId));

    private IActionResult ToActionResult(BaseGeneralResponse result)
    {
        var response = ApiResponseFactory.FromResult(result);
        return result.Success ? Ok(response) : BadRequest(response);
    }
}
