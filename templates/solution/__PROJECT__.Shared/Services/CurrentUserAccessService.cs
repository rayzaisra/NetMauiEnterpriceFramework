using __PROJECT__.Application.Common;
using __PROJECT__.Application.DTOs.Auth;

namespace __PROJECT__.Shared.Services;

public class CurrentUserAccessService
{
    private readonly AuthService _authService;
    private Task<UserResponseDto?>? _loadTask;
    private UserResponseDto? _user;

    public CurrentUserAccessService(AuthService authService) => _authService = authService;

    public bool IsLoaded { get; private set; }

    public async Task EnsureLoadedAsync()
    {
        _loadTask ??= LoadAsync();
        _user = await _loadTask;
        IsLoaded = true;
    }

    public bool Can(PermissionAction action)
    {
        if (!IsLoaded)
        {
            return false;
        }

        var isSuperAdmin = string.Equals(_user?.RoleId, "00", StringComparison.Ordinal);
        return PermissionRules.Can(
            isSuperAdmin,
            _user?.CanAddEdit == true,
            _user?.CanDelete == true,
            _user?.CanReadOnly == true,
            action);
    }

    public bool CanAny(params PermissionAction[] actions)
        => actions.Any(Can);

    private async Task<UserResponseDto?> LoadAsync()
    {
        try
        {
            return await _authService.GetCurrentUserAsync();
        }
        catch
        {
            return null;
        }
    }
}
