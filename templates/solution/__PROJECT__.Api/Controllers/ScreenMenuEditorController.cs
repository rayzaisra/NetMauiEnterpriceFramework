using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using __PROJECT__.Api.Authorization;
using __PROJECT__.Api.Helpers;
using __PROJECT__.Application.Common;
using __PROJECT__.Application.DTOs.General;
using __PROJECT__.Application.DTOs.Screen;
using __PROJECT__.Application.Interfaces;

namespace __PROJECT__.Api.Controllers;

[ApiController]
[Route("api/screen-menu-editor")]
[Authorize]
[RequireMenuAccess(PermissionAction.Load, "MNT-PARAM-MKR")]
public class ScreenMenuEditorController : ControllerBase
{
    private readonly IScreenMenuEditorService _screenMenuService;

    public ScreenMenuEditorController(IScreenMenuEditorService screenMenuService)
        => _screenMenuService = screenMenuService;

    [HttpPost("list")]
    public async Task<IActionResult> GetList([FromBody] DataTableRequestDto request)
    {
        var result = await _screenMenuService.GetListAsync(request);
        return Ok(ApiResponseFactory.Page(result.Data, request, result.TotalCount, "Screens retrieved successfully"));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var screen = await _screenMenuService.GetAsync(id);
        return screen == null
            ? NotFound(ApiResponseFactory.NotFound<ScreenMenuEditDto>("Screen not found."))
            : Ok(ApiResponseFactory.Success(screen, "Screen retrieved successfully"));
    }

    [HttpPost]
    [RequireMenuAccess(PermissionAction.AddEdit, "MNT-PARAM-MKR")]
    public async Task<IActionResult> Stage([FromBody] ScreenMenuEditDto dto)
    {
        var result = await _screenMenuService.StageAsync(dto);
        var response = ApiResponseFactory.FromResult(result);
        return result.Success ? Ok(response) : BadRequest(response);
    }
}
