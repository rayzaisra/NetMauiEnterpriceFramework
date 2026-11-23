using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using __PROJECT__.Api.Authorization;
using __PROJECT__.Api.Helpers;
using __PROJECT__.Application.Common;
using __PROJECT__.Application.DTOs.General;
using __PROJECT__.Application.Interfaces;

namespace __PROJECT__.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ApprovalController : ControllerBase
{
    private readonly IApprovalService _approvalService;

    public ApprovalController(IApprovalService approvalService) => _approvalService = approvalService;

    [HttpPost("users/list")]
    [RequireMenuAccess(PermissionAction.Load, "MNT-USER-APR")]
    public async Task<IActionResult> GetPendingUsers([FromBody] DataTableRequestDto request)
    {
        var result = await _approvalService.GetPendingUsersAsync(request);
        return Ok(ApiResponseFactory.Page(result.Data, request, result.TotalCount, "Users retrieved successfully"));
    }

    [HttpPost("users/{id:long}/approve")]
    [RequireMenuAccess(PermissionAction.Approve, "MNT-USER-APR")]
    public async Task<IActionResult> ApproveUser(long id)
        => ToActionResult(await _approvalService.ApproveUserAsync(id));

    [HttpPost("users/{id:long}/reject")]
    [RequireMenuAccess(PermissionAction.Reject, "MNT-USER-APR")]
    public async Task<IActionResult> RejectUser(long id)
        => ToActionResult(await _approvalService.RejectUserAsync(id));

    [HttpPost("groups/list")]
    [RequireMenuAccess(PermissionAction.Load, "MNT-GRP-APR")]
    public async Task<IActionResult> GetPendingGroups([FromBody] DataTableRequestDto request)
    {
        var result = await _approvalService.GetPendingGroupsAsync(request);
        return Ok(ApiResponseFactory.Page(result.Data, request, result.TotalCount, "Groups retrieved successfully"));
    }

    [HttpPost("groups/{id:long}/approve")]
    [RequireMenuAccess(PermissionAction.Approve, "MNT-GRP-APR")]
    public async Task<IActionResult> ApproveGroup(long id)
        => ToActionResult(await _approvalService.ApproveGroupAsync(id));

    [HttpPost("groups/{id:long}/reject")]
    [RequireMenuAccess(PermissionAction.Reject, "MNT-GRP-APR")]
    public async Task<IActionResult> RejectGroup(long id)
        => ToActionResult(await _approvalService.RejectGroupAsync(id));

    [HttpPost("roles/list")]
    [RequireMenuAccess(PermissionAction.Load, "MNT-ROLE-APR")]
    public async Task<IActionResult> GetPendingRoles([FromBody] DataTableRequestDto request)
    {
        var result = await _approvalService.GetPendingRolesAsync(request);
        return Ok(ApiResponseFactory.Page(result.Data, request, result.TotalCount, "Roles retrieved successfully"));
    }

    [HttpPost("roles/{id:guid}/approve")]
    [RequireMenuAccess(PermissionAction.Approve, "MNT-ROLE-APR")]
    public async Task<IActionResult> ApproveRole(Guid id)
        => ToActionResult(await _approvalService.ApproveRoleAsync(id));

    [HttpPost("roles/{id:guid}/reject")]
    [RequireMenuAccess(PermissionAction.Reject, "MNT-ROLE-APR")]
    public async Task<IActionResult> RejectRole(Guid id)
        => ToActionResult(await _approvalService.RejectRoleAsync(id));

    [HttpPost("menus/list")]
    [RequireMenuAccess(PermissionAction.Load, "MNT-MENU-APR")]
    public async Task<IActionResult> GetPendingMenus([FromBody] DataTableRequestDto request)
    {
        var result = await _approvalService.GetPendingMenusAsync(request);
        return Ok(ApiResponseFactory.Page(result.Data, request, result.TotalCount, "Menus retrieved successfully"));
    }

    [HttpPost("menus/{id:guid}/approve")]
    [RequireMenuAccess(PermissionAction.Approve, "MNT-MENU-APR")]
    public async Task<IActionResult> ApproveMenu(Guid id)
        => ToActionResult(await _approvalService.ApproveMenuAsync(id));

    [HttpPost("menus/{id:guid}/reject")]
    [RequireMenuAccess(PermissionAction.Reject, "MNT-MENU-APR")]
    public async Task<IActionResult> RejectMenu(Guid id)
        => ToActionResult(await _approvalService.RejectMenuAsync(id));

    private IActionResult ToActionResult(BaseGeneralResponse result)
    {
        var response = ApiResponseFactory.FromResult(result);
        return result.Success ? Ok(response) : BadRequest(response);
    }
}
