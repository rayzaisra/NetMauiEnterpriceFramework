using System.Text;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using __PROJECT__.Api.Helpers;
using __PROJECT__.Application.Common;
using __PROJECT__.Application.DTOs.Dynamic;
using __PROJECT__.Application.DTOs.General;
using __PROJECT__.Application.Interfaces;

namespace __PROJECT__.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DynamicController : ControllerBase
{
    private readonly IDynamicEngineService _dynamicService;
    private readonly IErrorLogService _errorLog;
    private readonly IPermissionService _permissions;

    public DynamicController(
        IDynamicEngineService dynamicService,
        IErrorLogService errorLog,
        IPermissionService permissions)
    {
        _dynamicService = dynamicService;
        _errorLog = errorLog;
        _permissions = permissions;
    }

    [HttpGet("screen/{screenId}")]
    public async Task<IActionResult> GetScreen(string screenId)
    {
        if (!await _dynamicService.CanAccessScreenAsync(screenId))
        {
            return Forbid();
        }

        var screen = await _dynamicService.GetScreenAsync(screenId);
        return screen == null
            ? NotFound(ApiResponseFactory.NotFound<DynamicScreenDto>("Screen not found."))
            : Ok(ApiResponseFactory.Success(screen, "Screen retrieved successfully"));
    }

    [HttpPost("screen/{screenId}/load")]
    public async Task<IActionResult> LoadScreen(string screenId, [FromBody] DynamicLoadRequestDto request)
    {
        if (!await _dynamicService.CanAccessScreenAsync(screenId))
        {
            return Forbid();
        }

        try
        {
            var result = await _dynamicService.LoadScreenAsync(screenId, request);
            return Ok(ApiResponseFactory.Success(result, "Screen loaded successfully"));
        }
        catch (InvalidOperationException ex)
        {
            await _errorLog.LogAsync("Dynamic", "ERROR", ex);
            return BadRequest(ApiResponseFactory.Error<DynamicLoadResultDto>(ex.Message));
        }
    }

    [HttpGet("screen/{screenId}/options/{fieldId}")]
    public async Task<IActionResult> GetOptions(string screenId, string fieldId)
    {
        if (!await _dynamicService.CanAccessScreenAsync(screenId))
        {
            return Forbid();
        }

        try
        {
            var options = await _dynamicService.GetOptionsAsync(screenId, fieldId);
            return Ok(ApiResponseFactory.Success(options, "Options retrieved successfully"));
        }
        catch (InvalidOperationException ex)
        {
            await _errorLog.LogAsync("Dynamic", "ERROR", ex);
            return BadRequest(ApiResponseFactory.Error<List<DynamicOptionDto>>(ex.Message));
        }
    }

    [HttpGet("screen/{screenId}/child/detail")]
    public async Task<IActionResult> GetChildDetail(string screenId, [FromQuery] string key)
    {
        if (!await _permissions.HasScreenAccessAsync(screenId, PermissionAction.ViewDetail))
        {
            return Forbid();
        }

        try
        {
            var row = await _dynamicService.GetChildDetailAsync(screenId, key);
            return row == null
                ? NotFound(ApiResponseFactory.NotFound<Dictionary<string, object?>>("Data not found."))
                : Ok(ApiResponseFactory.Success(row, "Data retrieved successfully"));
        }
        catch (InvalidOperationException ex)
        {
            await _errorLog.LogAsync("Dynamic", "ERROR", ex);
            return BadRequest(ApiResponseFactory.Error<Dictionary<string, object?>>(ex.Message));
        }
    }

    [HttpPost("screen/{screenId}/child/save")]
    public async Task<IActionResult> SaveChild(string screenId, [FromBody] DynamicMutationRequestDto request)
    {
        if (!await _permissions.HasScreenAccessAsync(screenId, PermissionAction.AddEdit))
        {
            return Forbid();
        }

        return ToActionResult(await _dynamicService.SaveChildAsync(screenId, request));
    }

    [HttpPost("screen/{screenId}/process")]
    public async Task<IActionResult> Process(string screenId, [FromBody] DynamicMutationRequestDto request)
    {
        if (!await _permissions.HasScreenAccessAsync(screenId, PermissionAction.AddEdit))
        {
            return Forbid();
        }

        return ToActionResult(await _dynamicService.ProcessAsync(screenId, request));
    }

    [HttpPost("screen/{screenId}/import-excel")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> ImportExcel(string screenId, IFormFile file)
    {
        if (!await _permissions.HasScreenAccessAsync(screenId, PermissionAction.Import))
        {
            return Forbid();
        }

        if (file == null || file.Length == 0)
        {
            return BadRequest(ApiResponseFactory.Error<string>("Excel file is required."));
        }

        await using var stream = file.OpenReadStream();
        return ToActionResult(await _dynamicService.ImportExcelAsync(screenId, stream, file.FileName));
    }

    [HttpPost("screen/{screenId}/delete")]
    public async Task<IActionResult> Delete(string screenId, [FromBody] DynamicMutationRequestDto request)
    {
        if (!await _permissions.HasScreenAccessAsync(screenId, PermissionAction.Delete))
        {
            return Forbid();
        }

        return ToActionResult(await _dynamicService.DeleteAsync(screenId, request));
    }

    [HttpGet("screen/{screenId}/export-txt")]
    public async Task<IActionResult> ExportTxt(string screenId, [FromQuery] string? searchTerm, [FromQuery] string? sortColumn, [FromQuery] string? sortDirection)
    {
        if (!await _permissions.HasScreenAccessAsync(screenId, PermissionAction.Export))
        {
            return Forbid();
        }

        try
        {
            var result = await _dynamicService.ExportTxtAsync(screenId, new DynamicLoadRequestDto
            {
                SearchTerm = searchTerm,
                SortColumn = sortColumn,
                SortDirection = sortDirection,
            });
            return File(Encoding.UTF8.GetBytes(result.Content), "text/plain; charset=utf-8", result.FileName);
        }
        catch (InvalidOperationException ex)
        {
            await _errorLog.LogAsync("Dynamic", "ERROR", ex);
            return BadRequest(ApiResponseFactory.Error<string>(ex.Message));
        }
    }

    [HttpGet("report/{reportId}")]
    public async Task<IActionResult> GetReport(string reportId)
    {
        var report = await _dynamicService.GetReportAsync(reportId);
        return report == null
            ? NotFound(ApiResponseFactory.NotFound<DynamicReportDto>("Report not found."))
            : Ok(ApiResponseFactory.Success(report, "Report retrieved successfully"));
    }

    [HttpPost("report/{reportId}/load")]
    public async Task<IActionResult> LoadReport(string reportId, [FromBody] DynamicLoadRequestDto request)
    {
        try
        {
            var result = await _dynamicService.LoadReportAsync(reportId, request);
            return Ok(ApiResponseFactory.Success(result, "Report loaded successfully"));
        }
        catch (UnauthorizedAccessException ex)
        {
            await _errorLog.LogAsync("Dynamic", "ERROR", ex);
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            await _errorLog.LogAsync("Dynamic", "ERROR", ex);
            return BadRequest(ApiResponseFactory.Error<DynamicLoadResultDto>(ex.Message));
        }
    }

    private IActionResult ToActionResult(BaseGeneralResponse result)
    {
        var response = ApiResponseFactory.FromResult(result);
        return result.Success ? Ok(response) : BadRequest(response);
    }
}
