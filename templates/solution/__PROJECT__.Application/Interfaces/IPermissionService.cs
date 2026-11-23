using __PROJECT__.Application.Common;

namespace __PROJECT__.Application.Interfaces;

public interface IPermissionService
{
    Task<bool> HasAccessAsync(PermissionAction action, params string[] menuUniques);
    Task<bool> HasScreenAccessAsync(string screenId, PermissionAction action);
}
