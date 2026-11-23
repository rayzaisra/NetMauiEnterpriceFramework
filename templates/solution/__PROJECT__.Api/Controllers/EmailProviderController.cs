using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using __PROJECT__.Api.Authorization;
using __PROJECT__.Api.Helpers;
using __PROJECT__.Application.Common;
using __PROJECT__.Application.DTOs.Email;
using __PROJECT__.Application.DTOs.General;
using __PROJECT__.Application.Interfaces;

namespace __PROJECT__.Api.Controllers;

[ApiController]
[Route("api/email-provider")]
[Authorize]
[RequireMenuAccess(PermissionAction.Load, "MNT-PARAM-MKR")]
public class EmailProviderController : ControllerBase
{
    private readonly IEmailProviderService _emailProviderService;
    private readonly IErrorLogService _errorLog;

    public EmailProviderController(IEmailProviderService emailProviderService, IErrorLogService errorLog)
    {
        _emailProviderService = emailProviderService;
        _errorLog = errorLog;
    }

    [HttpPost("list")]
    public async Task<IActionResult> GetList([FromBody] DataTableRequestDto request)
    {
        var result = await _emailProviderService.GetListAsync(request);
        return Ok(ApiResponseFactory.Page(result.Data, request, result.TotalCount, "Email providers retrieved successfully"));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var provider = await _emailProviderService.GetAsync(id);
        return provider == null
            ? NotFound(ApiResponseFactory.NotFound<EmailProviderDto>("Email provider not found."))
            : Ok(ApiResponseFactory.Success(provider, "Email provider retrieved successfully"));
    }

    [HttpPost]
    [RequireMenuAccess(PermissionAction.AddEdit, "MNT-PARAM-MKR")]
    public async Task<IActionResult> Save([FromBody] EmailProviderDto request)
    {
        var result = await _emailProviderService.SaveAsync(request);
        var response = ApiResponseFactory.FromResult(result);
        return result.Success ? Ok(response) : BadRequest(response);
    }

    [HttpDelete("{id:int}")]
    [RequireMenuAccess(PermissionAction.Delete, "MNT-PARAM-MKR")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _emailProviderService.DeleteAsync(id);
        var response = ApiResponseFactory.FromResult(result);
        return result.Success ? Ok(response) : BadRequest(response);
    }

    [HttpPost("{id:int}/test")]
    [RequireMenuAccess(PermissionAction.AddEdit, "MNT-PARAM-MKR")]
    public async Task<IActionResult> Test(int id, [FromQuery] string? recipient)
    {
        try
        {
            var result = await _emailProviderService.TestAsync(id, recipient);
            var response = ApiResponseFactory.FromResult(result);
            return result.Success ? Ok(response) : BadRequest(response);
        }
        catch (Exception ex)
        {
            await _errorLog.LogAsync("EmailProvider", "ERROR", ex);
            return BadRequest(ApiResponseFactory.Error<string>(ex.Message));
        }
    }
}
