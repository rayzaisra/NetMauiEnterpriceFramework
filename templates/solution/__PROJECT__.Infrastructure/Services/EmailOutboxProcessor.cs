using System.Net;
using System.Net.Http.Json;
using System.Net.Mail;
using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Configuration;

using __PROJECT__.Application.Interfaces;
using __PROJECT__.Domain.Entities;

namespace __PROJECT__.Infrastructure.Services;

public class EmailOutboxProcessor
{
    private static readonly HttpClient HttpClient = new();
    private readonly IUnitOfWork _unitOfWork;
    private readonly IErrorLogService _errorLog;
    private readonly IConfiguration _configuration;

    public EmailOutboxProcessor(IUnitOfWork unitOfWork, IErrorLogService errorLog, IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _errorLog = errorLog;
        _configuration = configuration;
    }

    public async Task ProcessPendingAsync()
    {
        var now = DateTime.UtcNow;
        var messages = await _unitOfWork.EmailOutbox.GetPendingAsync(now, 20);

        if (messages.Count == 0)
        {
            return;
        }

        var providers = await _unitOfWork.EmailProviders.GetActiveProvidersAsync();

        foreach (var message in messages)
        {
            var provider = providers.FirstOrDefault(p =>
                                string.Equals(p.provider_type, message.provider_type, StringComparison.OrdinalIgnoreCase))
                            ?? providers.FirstOrDefault();

            if (provider == null)
            {
                message.status = "AdminFallback";
                message.last_error = "No active email provider.";
                continue;
            }

            try
            {
                if (string.Equals(provider.provider_type, "SMTP", StringComparison.OrdinalIgnoreCase))
                {
                    await SendSmtpAsync(provider, message, GetPassPhrase());
                }
                else if (string.Equals(provider.provider_type, "GMAIL", StringComparison.OrdinalIgnoreCase))
                {
                    await SendGmailAsync(provider, message, GetPassPhrase());
                }
                else
                {
                    throw new InvalidOperationException("Unsupported email provider.");
                }

                message.status = "Sent";
                message.sent_at = DateTime.UtcNow;
                message.last_error = null;
            }
            catch (Exception ex)
            {
                message.attempt_count++;
                message.status = message.attempt_count >= 3 ? "AdminFallback" : "Failed";
                message.next_attempt_at = DateTime.UtcNow.AddMinutes(Math.Pow(5, message.attempt_count));
                message.last_error = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
                await _errorLog.LogAsync("EmailOutbox", "ERROR", ex);
            }
        }

        await _unitOfWork.SaveChangesAsync();
    }

    internal static async Task SendSmtpAsync(rf_email_provider provider, tb_email_outbox message, string? passPhrase = null)
    {
        using var config = JsonDocument.Parse(provider.configuration_json ?? "{}");
        var root = config.RootElement;
        var host = GetString(root, "host");
        var port = GetInt(root, "port", 587);
        var username = GetString(root, "username");
        var password = SecretProtector.Unprotect(GetString(root, "password"), passPhrase);
        var fromEmail = provider.from_email ?? GetString(root, "fromEmail");
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(fromEmail))
        {
            throw new InvalidOperationException("SMTP host/from email is not configured.");
        }

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = GetBool(root, "enableSsl", true),
            Credentials = string.IsNullOrWhiteSpace(username)
                ? CredentialCache.DefaultNetworkCredentials
                : new NetworkCredential(username, password),
        };

        using var mail = new MailMessage
        {
            From = new MailAddress(fromEmail, provider.from_name ?? "Enterprise Portal"),
            Subject = message.subject,
            Body = message.body_html ?? message.body_text ?? string.Empty,
            IsBodyHtml = !string.IsNullOrWhiteSpace(message.body_html),
        };
        mail.To.Add(message.recipient);
        await client.SendMailAsync(mail);
    }

    internal static async Task SendGmailAsync(rf_email_provider provider, tb_email_outbox message, string? passPhrase = null)
    {
        using var config = JsonDocument.Parse(provider.configuration_json ?? "{}");
        var root = config.RootElement;
        var clientId = GetString(root, "clientId");
        var clientSecret = SecretProtector.Unprotect(GetString(root, "clientSecret"), passPhrase);
        var refreshToken = SecretProtector.Unprotect(GetString(root, "refreshToken"), passPhrase);
        var fromEmail = provider.from_email ?? GetString(root, "fromEmail");
        if (string.IsNullOrWhiteSpace(clientId)
            || string.IsNullOrWhiteSpace(clientSecret)
            || string.IsNullOrWhiteSpace(refreshToken)
            || string.IsNullOrWhiteSpace(fromEmail))
        {
            throw new InvalidOperationException("Gmail OAuth configuration is incomplete.");
        }

        using var tokenResponse = await HttpClient.PostAsync(
            "https://oauth2.googleapis.com/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["refresh_token"] = refreshToken,
                ["grant_type"] = "refresh_token",
            }));
        var tokenBody = await tokenResponse.Content.ReadAsStringAsync();
        if (!tokenResponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Gmail token request failed: {tokenBody}");
        }

        using var tokenJson = JsonDocument.Parse(tokenBody);
        var accessToken = GetString(tokenJson.RootElement, "access_token");
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new InvalidOperationException("Gmail access token is empty.");
        }

        var rawMessage = $"From: {fromEmail}\r\n"
                         + $"To: {message.recipient}\r\n"
                         + $"Subject: {message.subject}\r\n"
                         + "MIME-Version: 1.0\r\n"
                         + "Content-Type: text/html; charset=UTF-8\r\n\r\n"
                         + (message.body_html ?? message.body_text ?? string.Empty);
        var raw = Convert.ToBase64String(Encoding.UTF8.GetBytes(rawMessage))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://gmail.googleapis.com/gmail/v1/users/me/messages/send");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = JsonContent.Create(new { raw });
        using var response = await HttpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Gmail send failed: {await response.Content.ReadAsStringAsync()}");
        }
    }

    private string? GetPassPhrase() => _configuration["Encryption:PassPhrase"];

    private static string? GetString(JsonElement element, string name)
        => element.TryGetProperty(name, out var value) ? value.GetString() : null;

    private static int GetInt(JsonElement element, string name, int fallback)
    {
        if (!element.TryGetProperty(name, out var value))
        {
            return fallback;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
        {
            return number;
        }

        return value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out number)
            ? number
            : fallback;
    }

    private static bool GetBool(JsonElement element, string name, bool fallback)
    {
        if (!element.TryGetProperty(name, out var value))
        {
            return fallback;
        }

        if (value.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            return value.GetBoolean();
        }

        return value.ValueKind == JsonValueKind.String && bool.TryParse(value.GetString(), out var result)
            ? result
            : fallback;
    }
}
