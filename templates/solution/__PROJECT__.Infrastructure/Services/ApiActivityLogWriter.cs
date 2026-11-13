using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using __PROJECT__.Application.DTOs.General;
using __PROJECT__.Domain.Entities;
using __PROJECT__.Infrastructure.Data;
using __PROJECT__.Infrastructure.Options;

namespace __PROJECT__.Infrastructure.Services;

public sealed class ApiActivityLogWriter : BackgroundService
{
    private readonly ApiActivityLogQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<ApiActivityLogOptions> _options;
    private readonly ILogger<ApiActivityLogWriter> _logger;

    public ApiActivityLogWriter(
        ApiActivityLogQueue queue,
        IServiceScopeFactory scopeFactory,
        IOptions<ApiActivityLogOptions> options,
        ILogger<ApiActivityLogWriter> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = _options.Value;
        if (!options.Enabled)
        {
            return;
        }

        var batchSize = Math.Clamp(options.BatchSize, 1, 1000);
        var flushInterval = TimeSpan.FromSeconds(Math.Clamp(options.FlushIntervalSeconds, 1, 60));
        var batch = new List<ApiActivityLogEntry>(batchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                batch.Add(await _queue.Reader.ReadAsync(stoppingToken));

                using var interval = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                interval.CancelAfter(flushInterval);

                while (batch.Count < batchSize)
                {
                    if (!await _queue.Reader.WaitToReadAsync(interval.Token))
                    {
                        break;
                    }

                    while (batch.Count < batchSize && _queue.Reader.TryRead(out var entry))
                    {
                        batch.Add(entry);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (OperationCanceledException)
            {
                // The interval elapsed; persist the partial batch and keep listening.
            }

            await FlushAsync(batch, stoppingToken);
            batch.Clear();
        }

        if (batch.Count > 0)
        {
            await FlushAsync(batch, CancellationToken.None);
        }
    }

    private async Task FlushAsync(List<ApiActivityLogEntry> entries, CancellationToken cancellationToken)
    {
        if (entries.Count == 0)
        {
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var audit = scope.ServiceProvider.GetRequiredService<AuditLogDbContext>();

            audit.tb_log_activity.AddRange(entries.Select(entry => new tb_log_activity
            {
                user_id = entry.UserId,
                ip_address = entry.IpAddress,
                http_method = entry.HttpMethod,
                path = entry.Path,
                route_template = entry.RouteTemplate,
                status_code = entry.StatusCode,
                success = entry.Success,
                duration_ms = entry.DurationMs,
                correlation_id = entry.CorrelationId,
                user_agent = entry.UserAgent,
                client_type = entry.ClientType,
                error_code = entry.ErrorCode,
                created_date = entry.CreatedDate,
            }));

            await audit.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not persist {Count} API activity records.", entries.Count);
        }
    }
}
