using System.Text.Json;

using Microsoft.EntityFrameworkCore;

using __PROJECT__.Application.DTOs.Email;
using __PROJECT__.Application.DTOs.General;
using __PROJECT__.Application.Interfaces.Queries;
using __PROJECT__.Domain.Entities;
using __PROJECT__.Infrastructure.Data;

namespace __PROJECT__.Infrastructure.Queries;

public class EmailProviderQueryService : IEmailProviderQueryService
{
    private readonly __PROJECT__DbContext _context;

    public EmailProviderQueryService(__PROJECT__DbContext context) => _context = context;

    public async Task<(List<EmailProviderDto> Data, int TotalCount)> GetListAsync(
        DataTableRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var query = _context.rf_email_provider
            .Where(e => e.isDeleted != true);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.Trim();
            query = query.Where(e =>
                (e.provider_name != null && e.provider_name.Contains(search))
                || e.provider_type.Contains(search)
                || (e.from_email != null && e.from_email.Contains(search)));
        }

        var desc = string.Equals(request.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        query = (request.SortColumn ?? string.Empty).ToLowerInvariant() switch
        {
            "providertype" => desc ? query.OrderByDescending(e => e.provider_type) : query.OrderBy(e => e.provider_type),
            "providername" => desc ? query.OrderByDescending(e => e.provider_name) : query.OrderBy(e => e.provider_name),
            "fromemail" => desc ? query.OrderByDescending(e => e.from_email) : query.OrderBy(e => e.from_email),
            "priority" => desc ? query.OrderByDescending(e => e.priority ?? 100) : query.OrderBy(e => e.priority ?? 100),
            "isactive" => desc ? query.OrderByDescending(e => e.isActive) : query.OrderBy(e => e.isActive),
            "lasttestedat" => desc ? query.OrderByDescending(e => e.last_tested_at) : query.OrderBy(e => e.last_tested_at),
            _ => query.OrderBy(e => e.priority ?? 100).ThenBy(e => e.id),
        };

        var total = await query.CountAsync(cancellationToken);
        var entities = await query
            .Skip((Math.Max(request.PageNumber, 1) - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return (entities.Select(ToDto).ToList(), total);
    }

    private static EmailProviderDto ToDto(rf_email_provider entity)
    {
        var config = ReadConfig(entity.configuration_json);
        var dto = new EmailProviderDto
        {
            Id = entity.id,
            ProviderType = entity.provider_type,
            ProviderName = entity.provider_name,
            FromEmail = entity.from_email,
            FromName = entity.from_name,
            Priority = entity.priority ?? 100,
            IsActive = entity.isActive != false,
            LastTestedAt = entity.last_tested_at,
            LastTestStatus = entity.last_test_status,
            LastTestMessage = entity.last_test_message,
        };

        if (string.Equals(entity.provider_type, "SMTP", StringComparison.OrdinalIgnoreCase))
        {
            dto.Host = GetConfig(config, "host");
            dto.Port = int.TryParse(GetConfig(config, "port"), out var port) ? port : 587;
            dto.EnableSsl = !bool.TryParse(GetConfig(config, "enableSsl"), out var ssl) || ssl;
            dto.Username = GetConfig(config, "username");
            dto.HasPassword = !string.IsNullOrWhiteSpace(GetConfig(config, "password"));
        }
        else if (string.Equals(entity.provider_type, "GMAIL", StringComparison.OrdinalIgnoreCase))
        {
            dto.ClientId = GetConfig(config, "clientId");
            dto.HasClientSecret = !string.IsNullOrWhiteSpace(GetConfig(config, "clientSecret"));
            dto.HasRefreshToken = !string.IsNullOrWhiteSpace(GetConfig(config, "refreshToken"));
        }

        return dto;
    }

    private static Dictionary<string, string> ReadConfig(string? json)
    {
        var config = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(json))
        {
            return config;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            foreach (var property in document.RootElement.EnumerateObject())
            {
                config[property.Name] = property.Value.ValueKind == JsonValueKind.String
                    ? property.Value.GetString() ?? string.Empty
                    : property.Value.GetRawText();
            }
        }
        catch
        {
            return config;
        }

        return config;
    }

    private static string? GetConfig(Dictionary<string, string> config, string key)
        => config.TryGetValue(key, out var value) ? value : null;
}
