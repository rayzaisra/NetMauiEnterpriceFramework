using MudBlazor;

namespace __PROJECT__.Shared.Pages.Documentation;

public static class FrameworkFlowCatalog
{
    public static IReadOnlyList<Flow> Flows { get; } = new[]
    {
        new Flow
        {
            Key = "login",
            Title = "Login",
            Tab = "Login",
            Icon = Icons.Material.Filled.Lock,
            Summary = "Sign in with an active user that already has an approved role.",
            Audience = "All users",
            Approval = "No approval",
            Prerequisites = new[]
            {
                "The user exists in um_user_app and is active.",
                "The user has one active row in um_user_group.",
                "The account is not locked.",
            },
            Steps = new[]
            {
                new Step
                {
                    Title = "Open the login page",
                    Detail = "Navigate to / and enter the User ID and Password. Remember Me keeps the refresh token in local storage.",
                },
                new Step
                {
                    Title = "Validate the account",
                    Detail = "The API checks the user, role, lock flag, and password before issuing a session.",
                },
                new Step
                {
                    Title = "Complete first-login password change",
                    Detail = "If is_first_login is true, the API returns no tokens. Complete the inline password change form before continuing.",
                },
                new Step
                {
                    Title = "Receive the session",
                    Detail = "A normal login issues an access token and refresh token, records the current session, and revokes previous refresh tokens.",
                },
                new Step
                {
                    Title = "Open the dashboard",
                    Detail = "The client stores the tokens and opens /dashboard.",
                },
            },
            Warnings = new[]
            {
                "A newly approved user cannot log in until Role Maintenance is also approved.",
                "Five failed attempts lock non-admin accounts. Group 00 is exempt from the automatic lock.",
                "A new login replaces the previous session for the same user.",
            },
            Technical = new[]
            {
                "POST /api/auth/login",
                "First login: POST /api/auth/first-login/change-password",
                "Tables: um_user_app, um_user_group, um_user_app_flag, um_user_app_session, um_user_refresh_token",
            },
        },
        new Flow
        {
            Key = "user",
            Title = "Create User",
            Tab = "User",
            Icon = Icons.Material.Filled.EmojiPeople,
            Summary = "Create the user identity. Role assignment is a separate approval flow.",
            Audience = "Administrator (group 00)",
            Approval = "User Approval",
            Prerequisites = new[]
            {
                "A group exists for the later role assignment.",
                "No existing user or pending request uses the same User ID.",
            },
            Steps = new[]
            {
                new Step
                {
                    Title = "Open User Maintenance",
                    Detail = "Go to Setting > User Maintenance > List.",
                },
                new Step
                {
                    Title = "Add the user",
                    Detail = "Select Add User and fill in User ID, Full Name, Gender, Email, Phone Number, Password, and Active.",
                },
                new Step
                {
                    Title = "Submit the request",
                    Detail = "Save stages the user in temp_user_app_pending with approval type A.",
                },
                new Step
                {
                    Title = "Approve the user",
                    Detail = "Open User Maintenance > Approval, select Approve, and Submit.",
                },
                new Step
                {
                    Title = "Confirm the result",
                    Detail = "Approval creates um_user_app and um_user_app_flag with is_first_login set to true.",
                },
            },
            Warnings = new[]
            {
                "Add User has no group picker. The user cannot log in until a role is approved in Role Maintenance.",
                "A User ID cannot be reused after a soft delete.",
                "The same administrator can submit and approve the request.",
            },
            Technical = new[]
            {
                "POST /api/user",
                "POST /api/approval/users/{id}/approve",
                "temp_user_app_pending -> um_user_app, um_user_app_flag",
                "Reject removes the pending row only.",
            },
        },
        new Flow
        {
            Key = "group",
            Title = "Create Group",
            Tab = "Group",
            Icon = Icons.Material.Filled.Groups,
            Summary = "Create the group master that is used as a role.",
            Audience = "Administrator (group 00)",
            Approval = "Group Approval",
            Prerequisites = new[]
            {
                "The Group ID is unique.",
                "No pending group request uses the same Group ID.",
            },
            Steps = new[]
            {
                new Step
                {
                    Title = "Open Group Maintenance",
                    Detail = "Go to Setting > Group Maintenance > List.",
                },
                new Step
                {
                    Title = "Add the group",
                    Detail = "Select Add Group and fill in Group ID, Group Name, and Active.",
                },
                new Step
                {
                    Title = "Submit the request",
                    Detail = "Save stages the group in temp_group_app_pending with approval type A.",
                },
                new Step
                {
                    Title = "Approve the group",
                    Detail = "Open Group Maintenance > Approval, select Approve, and Submit.",
                },
                new Step
                {
                    Title = "Confirm the result",
                    Detail = "Approval creates or updates um_group_app and marks it approved.",
                },
            },
            Warnings = new[]
            {
                "Group 00 is the administrator group and cannot be deleted.",
                "A group that is still assigned to users cannot be deleted.",
                "A Group ID cannot be reused after a soft delete.",
            },
            Technical = new[]
            {
                "POST /api/group",
                "POST /api/approval/groups/{id}/approve",
                "temp_group_app_pending -> um_group_app",
                "Reject removes the pending row only.",
            },
        },
        new Flow
        {
            Key = "role",
            Title = "Assign Role",
            Tab = "Role",
            Icon = Icons.Material.Filled.AdminPanelSettings,
            Summary = "Connect one approved user to one approved group and set the permission flags.",
            Audience = "Administrator (group 00)",
            Approval = "Role Approval",
            Prerequisites = new[]
            {
                "The user is already approved and active.",
                "The group is already approved and active.",
                "The user has no active role and no pending role request.",
            },
            Steps = new[]
            {
                new Step
                {
                    Title = "Open Role Maintenance",
                    Detail = "Go to Setting > Role Maintenance > List.",
                },
                new Step
                {
                    Title = "Add the role",
                    Detail = "Select Add Role, choose the User and Group, then set Can Add/Edit, Can Delete, Read Only, and Active.",
                },
                new Step
                {
                    Title = "Submit the request",
                    Detail = "Save stages the role in tb_parameter_sytem_temporary with table_name um_user_group.",
                },
                new Step
                {
                    Title = "Approve the role",
                    Detail = "Open Role Maintenance > Approval, select Approve, and Submit.",
                },
                new Step
                {
                    Title = "Refresh the session",
                    Detail = "The user must sign in again or refresh the token to receive the new role claim.",
                },
            },
            Warnings = new[]
            {
                "Role Maintenance rejects a user that still exists only in the pending user table.",
                "One user has one role. um_user_group.user_id is the primary key.",
                "Can Add/Edit, Can Delete, and Read Only are displayed but are not enforced by the current server authorization.",
            },
            Technical = new[]
            {
                "POST /api/usergroup",
                "POST /api/approval/roles/{id}/approve",
                "Tables: um_user_group, um_group_app, tb_parameter_sytem_temporary, tb_parameter_sytem_temporary_detail",
            },
        },
        new Flow
        {
            Key = "menus",
            Title = "Menus and Mapping",
            Tab = "Menus",
            Icon = Icons.Material.Filled.MenuBook,
            Summary = "Create menus with approval, then map approved menus to groups immediately.",
            Audience = "Administrator (group 00)",
            Approval = "Menu Approval; mapping is immediate",
            Prerequisites = new[]
            {
                "The parent menu exists when a parent is selected.",
                "The Menu Unique value is not used by another active menu.",
                "The target group exists for mapping.",
            },
            Steps = new[]
            {
                new Step
                {
                    Title = "Open Menu Maintenance",
                    Detail = "Go to Setting > Menu Maintenance > List and select Add Menu.",
                },
                new Step
                {
                    Title = "Fill the menu form",
                    Detail = "Enter Menu Unique, Name, URL, Component Type, Parent Level, Parent Menu, Icon, Order, and Active.",
                },
                new Step
                {
                    Title = "Approve the menu",
                    Detail = "Save stages the menu request. Approve it in Menu Maintenance > Approval.",
                },
                new Step
                {
                    Title = "Map the menu",
                    Detail = "Open Menu Maintenance > Mapping Menu, select the group, check the menu and page, then Save.",
                },
                new Step
                {
                    Title = "Confirm access",
                    Detail = "Mapping is applied immediately. Parent menus and all level-A roots are added automatically.",
                },
            },
            Warnings = new[]
            {
                "A menu without a group mapping is invisible in the sidebar.",
                "Component Type controls Web rendering. A menu without one is disabled.",
                "Mapping has no approval and replaces the complete mapping for that group.",
                "A menu with child menus cannot be deleted.",
                "Parent levels: A is a root, B is a group, D is a page.",
            },
            Technical = new[]
            {
                "POST /api/menu",
                "POST /api/approval/menus/{id}/approve",
                "POST /api/menu/save-map",
                "Tables: um_menu, um_group_menu, tb_parameter_sytem_temporary, tb_parameter_sytem_temporary_detail",
                "Component examples: assembly-qualified page type, DynamicScreen, DynamicReport, DynamicUrlView",
            },
        },
        new Flow
        {
            Key = "parameter",
            Title = "Add Parameter",
            Tab = "Parameter",
            Icon = Icons.Material.Filled.Tune,
            Summary = "Register a new parameter table and its metadata. This is separate from adding parameter data.",
            Audience = "Administrator (group 00) and developer or DBA",
            Approval = "Parameter Approval",
            Prerequisites = new[]
            {
                "The physical SQL table exists.",
                "The table name is added to ParameterEngineService.AllowedTables.",
                "The card and field metadata will be created through the Parameter System cards.",
            },
            Steps = new[]
            {
                new Step
                {
                    Title = "Create and register the table",
                    Detail = "The developer creates the SQL table and adds its name to AllowedTables. A card alone is not enough.",
                },
                new Step
                {
                    Title = "Add the parameter card",
                    Detail = "Open Parameter Maintenance > Maker > Parameter System, then add the table name, description, maker URL, approval URL, flags, and icon.",
                },
                new Step
                {
                    Title = "Approve the card",
                    Detail = "Approve the request from Parameter Maintenance > Approval.",
                },
                new Step
                {
                    Title = "Add field metadata",
                    Detail = "Use the Parameter System Detail card to define field name, order, description, key, type, reference, mandatory, and display flags.",
                },
                new Step
                {
                    Title = "Approve the metadata",
                    Detail = "Approve the field metadata before using the Maker form.",
                },
            },
            Warnings = new[]
            {
                "A database card alone does not make a table usable. The name must be in AllowedTables.",
                "Card and field metadata changes use the same maker and approval engine.",
                "order_seq is part of the metadata key. Changing it can make an update look like an insert.",
                "Dynamic screens can write parameter tables with direct SQL and bypass this flow.",
            },
            Technical = new[]
            {
                "Tables: tb_parameter_system, tb_parameter_system_detail",
                "Whitelist: ParameterEngineService.AllowedTables",
                "POST /api/parameter/save",
                "POST /api/parameter/pending/{id}/approve",
            },
        },
        new Flow
        {
            Key = "maker",
            Title = "Parameter Maker and Approval",
            Tab = "Maker",
            Icon = Icons.Material.Filled.Checklist,
            Summary = "Add, edit, or delete parameter data through staging and approval.",
            Audience = "Administrator (group 00)",
            Approval = "Parameter Approval",
            Prerequisites = new[]
            {
                "The parameter table is registered.",
                "The card and field metadata are approved.",
            },
            Steps = new[]
            {
                new Step
                {
                    Title = "Open the Maker",
                    Detail = "Go to Parameter Maintenance > Maker and select the parameter card.",
                },
                new Step
                {
                    Title = "Add or edit data",
                    Detail = "The form is generated from field metadata. Use Add, Edit, or Delete on the row.",
                },
                new Step
                {
                    Title = "Submit to staging",
                    Detail = "Save validates mandatory fields and duplicate pending keys, then writes the request to staging.",
                },
                new Step
                {
                    Title = "Review in Approval",
                    Detail = "Open Parameter Maintenance > Approval and select the same card.",
                },
                new Step
                {
                    Title = "Approve or reject",
                    Detail = "Select Approve or Reject per row, then Submit. Approval writes to the target table; rejection removes staging only.",
                },
            },
            Warnings = new[]
            {
                "Only one pending request is allowed for the same key.",
                "Approve All and Reject All affect the loaded page only. Submit calls the API per row.",
                "There is no batch endpoint and no all-or-nothing transaction across rows.",
                "Rejection does not store status 5. It writes an audit entry and removes the staging row.",
                "Sensitive columns are hidden from the approval list.",
            },
            Technical = new[]
            {
                "POST /api/parameter/save",
                "POST /api/parameter/pending/{id}/approve",
                "POST /api/parameter/pending/{id}/reject",
                "Tables: tb_parameter_sytem_temporary, tb_parameter_sytem_temporary_detail, target table, tb_log",
            },
        },
        new Flow
        {
            Key = "password",
            Title = "Change Password",
            Tab = "Password",
            Icon = Icons.Material.Filled.Lock,
            Summary = "Change a password during first login or from the account screens.",
            Audience = "All users",
            Approval = "No approval",
            Prerequisites = new[]
            {
                "The current password is known.",
                "The new password meets the password policy.",
            },
            Steps = new[]
            {
                new Step
                {
                    Title = "First-login change",
                    Detail = "Use the inline form on the login page. No token is issued until the change succeeds.",
                },
                new Step
                {
                    Title = "Normal change",
                    Detail = "Open /change-password or Profile > Change Password.",
                },
                new Step
                {
                    Title = "Enter the passwords",
                    Detail = "Provide the current password, new password, and confirmation.",
                },
                new Step
                {
                    Title = "Validate and save",
                    Detail = "The server validates the password policy and updates the stored hash.",
                },
                new Step
                {
                    Title = "Sign in again",
                    Detail = "Use the new password on the next login.",
                },
            },
            Warnings = new[]
            {
                "Policy: at least 8 characters with uppercase, lowercase, number, and special character.",
                "No approval is required.",
                "The authenticated change flow does not revoke other sessions or refresh tokens.",
                "An administrator reset sets is_first_login to true, so the user must change the password at the next login.",
            },
            Technical = new[]
            {
                "POST /api/auth/change-password",
                "POST /api/auth/first-login/change-password",
                "Tables: um_user_app, um_user_app_flag",
            },
        },
    };

    public sealed class Flow
    {
        public string Key { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string Tab { get; init; } = string.Empty;
        public string Icon { get; init; } = Icons.Material.Filled.Circle;
        public string Summary { get; init; } = string.Empty;
        public string Audience { get; init; } = string.Empty;
        public string Approval { get; init; } = string.Empty;
        public IReadOnlyList<string> Prerequisites { get; init; } = Array.Empty<string>();
        public IReadOnlyList<Step> Steps { get; init; } = Array.Empty<Step>();
        public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> Technical { get; init; } = Array.Empty<string>();
    }

    public sealed class Step
    {
        public string Title { get; init; } = string.Empty;
        public string Detail { get; init; } = string.Empty;
    }
}
