using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using __PROJECT__.Application.DTOs.General;
using __PROJECT__.Application.Interfaces;

namespace __PROJECT__.Api.Controllers;

[ApiController]
[Route("api/client-error")]
[AllowAnonymous]
public class ClientErrorController : ControllerBase
{
    private readonly IErrorLogService _errorLog;

    public ClientErrorController(IErrorLogService errorLog)
    {
        _errorLog = errorLog;
    }

    [HttpPost]
    [RequestSizeLimit(16_384)]
    public async Task<IActionResult> Log([FromBody] ClientErrorLogDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { message = "Message is required." });
        }

        var source = string.IsNullOrWhiteSpace(request.Source)
            ? "Client"
            : request.Source.Trim()[..Math.Min(request.Source.Trim().Length, 100)];
        var message = request.Message.Trim();
        var detail = message.Length > 3500 ? message[..3500] : message;
        var stack = string.IsNullOrWhiteSpace(request.StackTrace)
            ? string.Empty
            : request.StackTrace.Trim();

        var exception = new Exception(
            $"{detail}{Environment.NewLine}Platform: {request.Platform ?? "unknown"}{Environment.NewLine}{stack}");

        await _errorLog.LogAsync(source, "ERROR", exception, request.UserId);
        return Ok(new { success = true });
    }
}
