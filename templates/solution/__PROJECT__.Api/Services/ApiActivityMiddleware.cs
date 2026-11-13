using System.Diagnostics;
using System.Security.Claims;

using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

using __PROJECT__.Application.DTOs.General;
using __PROJECT__.Application.Interfaces;
using __PROJECT__.Infrastructure.Options;

namespace __PROJECT__.Api.Services;

public sealed class ApiActivityMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IOptions<ApiActivityLogOptions> _options;
    private readonly ILogger<ApiActivityMiddleware> _logger;

    public ApiActivityMiddleware(
        RequestDelegate next,
        IOptions<ApiActivityLogOptions> options,
        ILogger<ApiActivityMiddleware> logger)
    {
        _next = next;
        _options = options;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IApiActivityLogQueue queue)
    {
        var options = _options.Value;
        if (!options.Enabled || ShouldSkip(context.Request.Path, options))
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        Exception? failure = null;

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            failure = ex;
            throw;
        }
        finally
        {
            stopwatch.Stop();
            var statusCode = failure == null ? context.Response.StatusCode : StatusCodes.Status500InternalServerError;
            var endpoint = context.GetEndpoint() as RouteEndpoint;

            var entry = new ApiActivityLogEntry
            {
                UserId = Truncate(context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, 100),
                IpAddress = NormalizeIp(context.Connection.RemoteIpAddress),
                HttpMethod = context.Request.Method,
                Path = Truncate(context.Request.Path.Value, 500),
                RouteTemplate = Truncate(endpoint?.RoutePattern.RawText, 300),
                StatusCode = statusCode,
                Success = statusCode is >= 200 and < 400,
                DurationMs = (int)Math.Min(stopwatch.ElapsedMilliseconds, int.MaxValue),
                CorrelationId = Truncate(context.TraceIdentifier, 64),
                UserAgent = Truncate(context.Request.Headers.UserAgent.ToString(), 500),
                ClientType = Truncate(context.Request.Headers["X-Client-Type"].ToString(), 20),
                ErrorCode = failure == null ? null : Truncate(failure.GetType().Name, 20),
                CreatedDate = DateTime.Now,
            };

            if (!queue.TryEnqueue(entry))
            {
                _logger.LogWarning("API activity queue is full; activity for {Method} {Path} was dropped.",
                    entry.HttpMethod, entry.Path);
            }
        }
    }

    private static bool ShouldSkip(PathString path, ApiActivityLogOptions options)
    {
        var value = path.Value ?? string.Empty;
        return (options.ExcludedPaths ?? []).Any(prefix =>
        {
            if (string.IsNullOrWhiteSpace(prefix))
            {
                return false;
            }

            if (value.Equals(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var segmentPrefix = prefix.EndsWith('/') ? prefix : prefix + "/";
            return value.StartsWith(segmentPrefix, StringComparison.OrdinalIgnoreCase);
        });
    }

    private static string? NormalizeIp(System.Net.IPAddress? address)
    {
        if (address == null)
        {
            return null;
        }

        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        return address.ToString();
    }

    private static string? Truncate(string? value, int maxLength)
        => string.IsNullOrWhiteSpace(value)
            ? null
            : value.Length <= maxLength ? value : value[..maxLength];
}
