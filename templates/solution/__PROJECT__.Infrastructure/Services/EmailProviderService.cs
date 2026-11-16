using System.Text.Json;

using Microsoft.Extensions.Configuration;

using __PROJECT__.Application.Common;
using __PROJECT__.Application.DTOs.Email;
using __PROJECT__.Application.DTOs.General;
using __PROJECT__.Application.Interfaces;
using __PROJECT__.Application.Interfaces.Queries;
using __PROJECT__.Domain.Entities;
using __PROJECT__.Domain.Enums;

namespace __PROJECT__.Infrastructure.Services;

public class EmailProviderService : IEmailProviderService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailProviderQueryService _queryService;
    private readonly IErrorLogService _errorLog;
    private readonly IConfiguration _configuration;

    public EmailProviderService(
        IUnitOfWork unitOfWork,
        IEmailProviderQueryService queryService,
        IErrorLogService errorLog,
        IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _queryService = queryService;
        _errorLog = errorLog;
        _configuration = configuration;
    }

    public Task<(List<EmailProviderDto> Data, int TotalCount)> GetListAsync(DataTableRequestDto request)
        => _queryService.GetListAsync(request);

    public async Task<EmailProviderDto?> GetAsync(int id)
    {
        var entity = await _unitOfWork.EmailProviders.GetByIdAsync(id);
        return entity == null ? null : ToDto(entity);
    }

    public async Task<BaseGeneralResponse> SaveAsync(EmailProviderDto request)
    {
        var providerType = (request.ProviderType ?? string.Empty).Trim().ToUpperInvariant();
        if (providerType is not ("SMTP" or "GMAIL"))
        {
            return Fail("Provider type must be SMTP or GMAIL.");
        }

        if (string.IsNullOrWhiteSpace(request.FromEmail))
        {
            return Fail("From email is required.");
        }

        var entity = request.Id == 0
            ? new rf_email_provider()
            : await _unitOfWork.EmailProviders.GetByIdAsync(request.Id);

        if (entity == null)
        {
            return Fail("Email provider not found.");
        }

        var config = ReadConfig(entity.configuration_json);
        if (!string.Equals(entity.provider_type, providerType, StringComparison.OrdinalIgnoreCase))
        {
            config.Clear();
        }

        if (providerType == "SMTP")
        {
            if (string.IsNullOrWhiteSpace(request.Host))
            {
                return Fail("SMTP host is required.");
            }

            var password = string.IsNullOrWhiteSpace(request.Password)
                ? GetConfig(config, "password")
                : request.Password;
            if (string.IsNullOrWhiteSpace(password))
            {
                return Fail("SMTP password is required.");
            }

            config["host"] = request.Host.Trim();
            config["port"] = request.Port.ToString();
            config["enableSsl"] = request.EnableSsl.ToString().ToLowerInvariant();
            config["username"] = request.Username?.Trim() ?? string.Empty;
            config["password"] = string.IsNullOrWhiteSpace(request.Password)
                ? password
                : SecretProtector.Protect(password, GetPassPhrase());
        }
        else
        {
            var clientSecret = string.IsNullOrWhiteSpace(request.ClientSecret)
                ? GetConfig(config, "clientSecret")
                : request.ClientSecret;
            var refreshToken = string.IsNullOrWhiteSpace(request.RefreshToken)
                ? GetConfig(config, "refreshToken")
                : request.RefreshToken;

            if (string.IsNullOrWhiteSpace(request.ClientId)
                || string.IsNullOrWhiteSpace(clientSecret)
                || string.IsNullOrWhiteSpace(refreshToken))
            {
                return Fail("Gmail Client ID, Client Secret, and Refresh Token are required.");
            }

            config["clientId"] = request.ClientId.Trim();
            config["clientSecret"] = string.IsNullOrWhiteSpace(request.ClientSecret)
                ? clientSecret
                : SecretProtector.Protect(clientSecret, GetPassPhrase());
            config["refreshToken"] = string.IsNullOrWhiteSpace(request.RefreshToken)
                ? refreshToken
                : SecretProtector.Protect(refreshToken, GetPassPhrase());
        }

        var now = DateTime.Now;
        entity.provider_type = providerType;
        entity.provider_name = request.ProviderName?.Trim();
        entity.from_email = request.FromEmail.Trim();
        entity.from_name = request.FromName?.Trim();
        entity.priority = request.Priority;
        entity.isActive = request.IsActive;
        entity.isDeleted = false;
        entity.configuration_json = JsonSerializer.Serialize(config);
        entity.last_tested_at = null;
        entity.last_test_status = null;
        entity.last_test_message = null;

        if (entity.id == 0)
        {
            entity.createdDate = now;
            entity.createdBy = "system";
            await _unitOfWork.EmailProviders.AddAsync(entity);
        }
        else
        {
            entity.modifiedDate = now;
        }

        await _unitOfWork.SaveChangesAsync();
        return Success("Email provider saved successfully.");
    }

    public async Task<BaseGeneralResponse> DeleteAsync(int id)
    {
        var entity = await _unitOfWork.EmailProviders.GetByIdAsync(id);
        if (entity == null)
        {
            return Fail("Email provider not found.");
        }

        entity.isDeleted = true;
        entity.isActive = false;
        entity.deletedDate = DateTime.Now;
        entity.deletedBy = "system";
        await _unitOfWork.SaveChangesAsync();
        return Success("Email provider deleted successfully.");
    }

    public async Task<BaseGeneralResponse> TestAsync(int id, string? recipient)
    {
        var entity = await _unitOfWork.EmailProviders.GetByIdAsync(id);
        if (entity == null)
        {
            return Fail("Email provider not found.");
        }

        var target = string.IsNullOrWhiteSpace(recipient) ? entity.from_email : recipient.Trim();
        if (string.IsNullOrWhiteSpace(target))
        {
            return Fail("A test recipient is required.");
        }

        try
        {
            var testMessage = new tb_email_outbox
            {
                recipient = target,
                subject = "Email provider test",
                body_html = "<p>Your email provider configuration is working.</p>",
                body_text = "Your email provider configuration is working.",
            };

            if (string.Equals(entity.provider_type, "SMTP", StringComparison.OrdinalIgnoreCase))
            {
                await EmailOutboxProcessor.SendSmtpAsync(entity, testMessage, GetPassPhrase());
            }
            else if (string.Equals(entity.provider_type, "GMAIL", StringComparison.OrdinalIgnoreCase))
            {
                await EmailOutboxProcessor.SendGmailAsync(entity, testMessage, GetPassPhrase());
            }
            else
            {
                return Fail("Unsupported email provider.");
            }

            entity.last_tested_at = DateTime.Now;
            entity.last_test_status = "Success";
            entity.last_test_message = $"Test email sent to {target}.";
            await _unitOfWork.SaveChangesAsync();
            return Success(entity.last_test_message);
        }
        catch (Exception ex)
        {
            await _errorLog.LogAsync("EmailProvider", "ERROR", ex);
            var error = string.IsNullOrWhiteSpace(ex.InnerException?.Message) ? ex.Message : ex.InnerException.Message;
            entity.last_tested_at = DateTime.Now;
            entity.last_test_status = "Failed";
            entity.last_test_message = error.Length > 2000 ? error[..2000] : error;
            await _unitOfWork.SaveChangesAsync();
            return Fail($"Test failed: {error}");
        }
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

    private string? GetPassPhrase() => _configuration["Encryption:PassPhrase"];

    private static BaseGeneralResponse Success(string message) => new()
    {
        Success = true,
        Code = Const.SUCCESS_CODE,
        Message = message,
    };

    private static BaseGeneralResponse Fail(string message) => new()
    {
        Success = false,
        Code = Const.ERROR_CODE,
        Message = message,
    };
}
