using System.Net.Http.Json;

using __PROJECT__.Application.DTOs.General;
using __PROJECT__.Application.DTOs.Notification;

namespace __PROJECT__.Shared.Services;

public class NotificationApiService
{
    private readonly ApiService _api;

    public NotificationApiService(ApiService api) => _api = api;

    public async Task<List<NotificationDto>> GetRecentAsync(int top = 20)
        => await _api.GetFromJsonAsync<List<NotificationDto>>($"api/notification/recent?top={top}", unwrap: true)
           ?? new List<NotificationDto>();

    public async Task<int> GetUnreadCountAsync()
        => await _api.GetFromJsonAsync<int>("api/notification/unread-count", unwrap: true);

    public async Task<BaseGeneralResponse> MarkSeenAsync(long id)
        => await ReadAsync(await _api.PostAsync($"api/notification/{id}/seen", new { }));

    public async Task<BaseGeneralResponse> MarkAllSeenAsync()
        => await ReadAsync(await _api.PostAsync("api/notification/seen-all", new { }));

    private static async Task<BaseGeneralResponse> ReadAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<BaseGeneralResponse>()
                   ?? new BaseGeneralResponse { Success = true };
        }

        return new BaseGeneralResponse { Success = false, Message = $"Request failed ({(int)response.StatusCode})." };
    }
}
