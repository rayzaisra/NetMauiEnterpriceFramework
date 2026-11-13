using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net;

using __PROJECT__.Application.DTOs.Auth;
using __PROJECT__.Application.DTOs.General;
using __PROJECT__.Shared.Services.Interfaces;

namespace __PROJECT__.Shared.Services;

public class AuthService
{
    private readonly HttpClient _httpClient;
    private readonly ITokenStorage _tokenStorage;

    public AuthService(HttpClient httpClient, ITokenStorage tokenStorage)
    {
        _httpClient = httpClient;
        _tokenStorage = tokenStorage;
    }

    public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/login", request);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        }

        throw new Exception(await ReadErrorAsync(response));
    }

    public async Task<LoginResponseDto?> RefreshTokenAsync(string refreshToken)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/refresh", new { refreshToken });
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<LoginResponseDto>();
    }

    public async Task<UserResponseDto?> GetCurrentUserAsync()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/auth/me");
        await AttachTokenAsync(request);

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var wrapper = await response.Content.ReadFromJsonAsync<ApiResponse<UserResponseDto>>();
        return wrapper?.Data;
    }

    public async Task<UserResponseDto?> UpdateProfileAsync(UpdateProfileDto request)
    {
        using var message = new HttpRequestMessage(HttpMethod.Put, "api/auth/profile")
        {
            Content = JsonContent.Create(request),
        };
        await AttachTokenAsync(message);

        var response = await _httpClient.SendAsync(message);
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(await ReadErrorAsync(response));
        }

        var wrapper = await response.Content.ReadFromJsonAsync<ApiResponse<UserResponseDto>>();
        return wrapper?.Data;
    }

    public async Task RequestPasswordResetAsync(string userIdOrEmail)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/forgot-password", new ForgotPasswordRequestDto
        {
            UserIdOrEmail = userIdOrEmail,
        });
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(await ReadErrorAsync(response));
        }
    }

    public async Task<bool> ValidatePasswordResetTokenAsync(string token)
    {
        var wrapper = await _httpClient.GetFromJsonAsync<ApiResponse<bool>>(
            $"api/auth/reset-password/validate?token={Uri.EscapeDataString(token)}");
        return wrapper?.Data == true;
    }

    public async Task ResetPasswordAsync(ResetPasswordRequestDto request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/reset-password", request);
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(await ReadErrorAsync(response));
        }
    }

    public async Task ChangeFirstLoginPasswordAsync(FirstLoginPasswordDto request)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, "api/auth/first-login/change-password")
        {
            Content = JsonContent.Create(request),
        };

        var response = await _httpClient.SendAsync(message);
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(await ReadErrorAsync(response));
        }
    }

    public async Task ChangePasswordAsync(ChangePasswordDto request)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, "api/auth/change-password")
        {
            Content = JsonContent.Create(request),
        };
        await AttachTokenAsync(message);

        var response = await _httpClient.SendAsync(message);
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(await ReadErrorAsync(response));
        }
    }

    public async Task LogClientErrorAsync(string source, Exception exception, string? userId = null)
    {
        try
        {
            await _httpClient.PostAsJsonAsync("api/client-error", new ClientErrorLogDto
            {
                Source = source,
                Message = exception.Message,
                StackTrace = exception.ToString(),
                UserId = userId,
                Platform = OperatingSystem.IsBrowser() ? "Web" : "MAUI",
            });
        }
        catch
        {
            // The original error must still be shown to the user when logging is unavailable.
        }
    }

    public async Task LogoutAsync(string? refreshToken)
    {
        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Post, "api/auth/logout")
            {
                Content = JsonContent.Create(new { refreshToken }),
            };
            await AttachTokenAsync(message);
            await _httpClient.SendAsync(message);
        }
        catch
        {
            // Local logout still proceeds.
        }
        finally
        {
            await _tokenStorage.RemoveTokensAsync();
        }
    }

    private async Task AttachTokenAsync(HttpRequestMessage request)
    {
        var token = await _tokenStorage.GetAccessTokenAsync();
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response)
    {
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return "Invalid Username / Password.";
        }

        var content = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(content))
        {
            return response.ReasonPhrase ?? "Request failed.";
        }

        if (content.Contains("<html", StringComparison.OrdinalIgnoreCase)
            || content.Contains("<!doctype", StringComparison.OrdinalIgnoreCase)
            || content.Contains("Bad Request", StringComparison.OrdinalIgnoreCase))
        {
            return "Unable to connect to the server. Please try again later.";
        }

        try
        {
            var json = System.Text.Json.JsonDocument.Parse(content);
            if (json.RootElement.TryGetProperty("message", out var message))
            {
                return message.GetString() ?? content;
            }
        }
        catch
        {
            // Keep raw content.
        }

        return content;
    }
}
