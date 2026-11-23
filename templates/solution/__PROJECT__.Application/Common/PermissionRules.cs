namespace __PROJECT__.Application.Common;

public static class PermissionRules
{
    public static bool Can(
        bool isSuperAdmin,
        bool canAddEdit,
        bool canDelete,
        bool canReadOnly,
        PermissionAction action)
    {
        if (isSuperAdmin)
        {
            return true;
        }

        if (action == PermissionAction.Load)
        {
            return true;
        }

        if (canReadOnly)
        {
            return false;
        }

        return action == PermissionAction.Delete ? canDelete : canAddEdit;
    }
}

public static class PermissionSets
{
    public static readonly IReadOnlyList<PermissionAction> AddEdit = new[]
    {
        PermissionAction.Add,
        PermissionAction.Edit,
    };

    public static readonly IReadOnlyList<PermissionAction> EditDelete = new[]
    {
        PermissionAction.Edit,
        PermissionAction.Delete,
    };

    public static readonly IReadOnlyList<PermissionAction> ApproveReject = new[]
    {
        PermissionAction.Approve,
        PermissionAction.Reject,
    };

    public static readonly IReadOnlyList<PermissionAction> ActionRow = new[]
    {
        PermissionAction.ViewDetail,
        PermissionAction.Edit,
        PermissionAction.Delete,
    };
}
