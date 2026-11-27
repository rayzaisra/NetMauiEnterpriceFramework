using __PROJECT__.Domain.Entities;

namespace __PROJECT__.Application.Interfaces.Repositories;

public interface IUmUserAppSessionRepository
{
    Task<um_user_app_session?> GetByUserIdAsync(string userId);
    Task UpsertAsync(string userId, string? sessionId);
    Task RemoveAsync(string userId);
}
