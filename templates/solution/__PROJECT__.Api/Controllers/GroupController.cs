using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using __PROJECT__.Api.Authorization;
using __PROJECT__.Api.Helpers;
using __PROJECT__.Application.Common;
using __PROJECT__.Application.DTOs.General;
using __PROJECT__.Application.DTOs.Group;
using __PROJECT__.Application.Interfaces;

namespace __PROJECT__.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[RequireMenuAccess(PermissionAction.Load, "MNT-GRP-LST")]
public class GroupController : ControllerBase
{
    private readonly IGroupManagementService _groupService;

    public GroupController(IGroupManagementService groupService) => _groupService = groupService;

    [HttpPost("list")]
    public async Task<IActionResult> GetList([FromBody] DataTableRequestDto request)
    {
        var result = await _groupService.GetListAsync(request);
        return Ok(ApiResponseFactory.Page(result.Data, request, result.TotalCount, "Groups retrieved successfully"));
    }

    [HttpGet("{groupId}")]
    public async Task<IActionResult> Get(string groupId)
    {
        var group = await _groupService.GetByIdAsync(groupId);
        return group == null
            ? NotFound(ApiResponseFactory.NotFound<GroupEditDto>("Group not found."))
            : Ok(ApiResponseFactory.Success(group, "Group retrieved successfully"));
    }

    [HttpPost]
    [RequireMenuAccess(PermissionAction.AddEdit, "MNT-GRP-LST")]
    public async Task<IActionResult> Create([FromBody] GroupEditDto dto)
        => ToActionResult(await _groupService.CreateAsync(dto));

    [HttpPut("{groupId}")]
    [RequireMenuAccess(PermissionAction.AddEdit, "MNT-GRP-LST")]
    public async Task<IActionResult> Update(string groupId, [FromBody] GroupEditDto dto)
        => ToActionResult(await _groupService.UpdateAsync(groupId, dto));

    [HttpDelete("{groupId}")]
    [RequireMenuAccess(PermissionAction.Delete, "MNT-GRP-LST")]
    public async Task<IActionResult> Delete(string groupId)
        => ToActionResult(await _groupService.DeleteAsync(groupId));

    private IActionResult ToActionResult(BaseGeneralResponse result)
    {
        var response = ApiResponseFactory.FromResult(result);
        return result.Success ? Ok(response) : BadRequest(response);
    }
}
