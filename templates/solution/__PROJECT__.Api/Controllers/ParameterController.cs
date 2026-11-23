using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using __PROJECT__.Api.Authorization;
using __PROJECT__.Api.Helpers;
using __PROJECT__.Application.Common;
using __PROJECT__.Application.DTOs.General;
using __PROJECT__.Application.DTOs.Parameter;
using __PROJECT__.Application.Interfaces;
using __PROJECT__.Domain.Enums;

namespace __PROJECT__.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ParameterController : ControllerBase
{
    private readonly IParameterEngineService _parameterService;
    private readonly IErrorLogService _errorLog;

    public ParameterController(IParameterEngineService parameterService, IErrorLogService errorLog)
    {
        _parameterService = parameterService;
        _errorLog = errorLog;
    }

    [HttpGet("systems")]
    [HttpGet("cards")]
    [RequireMenuAccess(PermissionAction.Load, "MNT-PARAM-MKR", "MNT-PARAM-APR")]
    public async Task<IActionResult> GetSystems()
    {
        var cards = await _parameterService.GetCardsAsync();
        return Ok(ApiResponseFactory.Success(cards, "Parameter systems retrieved successfully"));
    }

    [HttpGet("{tableName}/fields")]
    [HttpGet("fields/{tableName}")]
    [RequireMenuAccess(PermissionAction.Load, "MNT-PARAM-MKR", "MNT-PARAM-APR")]
    public async Task<IActionResult> GetFields(string tableName)
    {
        try
        {
            var fields = await _parameterService.GetFieldsAsync(tableName);
            return Ok(ApiResponseFactory.Success(fields, "Parameter fields retrieved successfully"));
        }
        catch (InvalidOperationException ex)
        {
            await _errorLog.LogAsync("Parameter", "ERROR", ex);
            return BadRequest(ApiResponseFactory.Error<List<ParameterFieldDto>>(ex.Message));
        }
    }

    [HttpGet("{tableName}/options")]
    [RequireMenuAccess(PermissionAction.Load, "MNT-PARAM-MKR", "MNT-PARAM-APR")]
    public async Task<IActionResult> GetOptions(string tableName)
    {
        try
        {
            var options = await _parameterService.GetOptionsAsync(tableName);
            return Ok(ApiResponseFactory.Success(options, "Parameter options retrieved successfully"));
        }
        catch (InvalidOperationException ex)
        {
            await _errorLog.LogAsync("Parameter", "ERROR", ex);
            return BadRequest(ApiResponseFactory.Error<Dictionary<string, List<DropdownItemDto>>>(ex.Message));
        }
    }

    [HttpPost("approval-list/{tableName}")]
    [RequireMenuAccess(PermissionAction.Load, "MNT-PARAM-APR")]
    public async Task<IActionResult> GetApprovalRows(string tableName, [FromBody] DataTableRequestDto request)
    {
        try
        {
            var result = await _parameterService.GetApprovalRowsAsync(tableName, request);
            return Ok(ApiResponseFactory.Page(result.Data, request, result.TotalCount, "Parameter approvals retrieved successfully"));
        }
        catch (InvalidOperationException ex)
        {
            await _errorLog.LogAsync("Parameter", "ERROR", ex);
            return BadRequest(ApiResponseFactory.Error<PaginatedResponse<ParameterApprovalRowDto>>(ex.Message));
        }
    }

    [HttpPost("{tableName}/list")]
    [RequireMenuAccess(PermissionAction.Load, "MNT-PARAM-MKR", "MNT-PARAM-APR")]
    public async Task<IActionResult> GetList(string tableName, [FromBody] DataTableRequestDto request)
    {
        try
        {
            var result = await _parameterService.GetListAsync(tableName, request);
            return Ok(new ParameterListResponseDto
            {
                Success = true,
                Message = "Parameters retrieved successfully",
                Code = Const.SUCCESS_CODE,
                Data = result.Rows,
                Fields = result.Fields,
                KeyField = result.KeyField,
                Pagination = new PaginationInfo
                {
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    TotalCount = result.TotalCount,
                },
            });
        }
        catch (InvalidOperationException ex)
        {
            await _errorLog.LogAsync("Parameter", "ERROR", ex);
            return BadRequest(ApiResponseFactory.Error<ParameterListResponseDto>(ex.Message));
        }
    }

    [HttpGet("{tableName}/detail")]
    [RequireMenuAccess(PermissionAction.ViewDetail, "MNT-PARAM-MKR", "MNT-PARAM-APR")]
    public async Task<IActionResult> GetDetail(string tableName, [FromQuery] string? key)
    {
        try
        {
            var row = await _parameterService.GetAsync(tableName, key);
            return row == null
                ? NotFound(ApiResponseFactory.NotFound<Dictionary<string, string>>("Parameter not found."))
                : Ok(ApiResponseFactory.Success(row, "Parameter retrieved successfully"));
        }
        catch (InvalidOperationException ex)
        {
            await _errorLog.LogAsync("Parameter", "ERROR", ex);
            return BadRequest(ApiResponseFactory.Error<Dictionary<string, string>>(ex.Message));
        }
    }

    [HttpPost("save")]
    [RequireMenuAccess(PermissionAction.AddEdit, "MNT-PARAM-MKR")]
    public async Task<IActionResult> Save([FromBody] ParameterSaveRequestDto request)
        => ToActionResult(await _parameterService.SaveAsync(request));

    [HttpDelete("{tableName}")]
    [RequireMenuAccess(PermissionAction.Delete, "MNT-PARAM-MKR")]
    public async Task<IActionResult> Delete(string tableName, [FromQuery] string? key)
        => ToActionResult(await _parameterService.DeleteAsync(tableName, key));

    [HttpPost("pending/list")]
    [RequireMenuAccess(PermissionAction.Load, "MNT-PARAM-APR")]
    public async Task<IActionResult> GetPending([FromBody] ParameterPendingRequestDto request)
    {
        var result = await _parameterService.GetPendingAsync(request);
        var pageRequest = new DataTableRequestDto
        {
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            SearchTerm = request.SearchTerm,
            SortColumn = request.SortColumn,
            SortDirection = request.SortDirection,
        };
        return Ok(ApiResponseFactory.Page(result.Data, pageRequest, result.TotalCount, "Pending parameters retrieved successfully"));
    }

    [HttpPost("pending/{id:guid}/approve")]
    [RequireMenuAccess(PermissionAction.Approve, "MNT-PARAM-APR")]
    public async Task<IActionResult> Approve(Guid id)
        => ToActionResult(await _parameterService.ApproveAsync(id));

    [HttpPost("pending/{id:guid}/reject")]
    [RequireMenuAccess(PermissionAction.Reject, "MNT-PARAM-APR")]
    public async Task<IActionResult> Reject(Guid id)
        => ToActionResult(await _parameterService.RejectAsync(id));

    private IActionResult ToActionResult(BaseGeneralResponse result)
    {
        var response = ApiResponseFactory.FromResult(result);
        return result.Success ? Ok(response) : BadRequest(response);
    }
}
