using MudBlazor;
using __PROJECT__.Shared.Services;

namespace __PROJECT__.Shared.Pages.Documentation;

public static class FrameworkFlowCatalog
{
    public static IReadOnlyList<Flow> GetFlows(LanguageService L) => new[]
    {
        new Flow
        {
            Key = "login",
            Title = L["FfLogin"],
            Tab = L["FfLogin"],
            Icon = Icons.Material.Filled.Lock,
            Summary = L["FfSignInWithAnActiveUser"],
            Audience = L["FfAllUsers"],
            Approval = L["FfNoApproval"],
            Prerequisites = new[]
            {
                L["FfTheUserExistsInUmuserappAnd"],
                L["FfTheUserHasOneActiveRow"],
                L["FfTheAccountIsNotLocked"],
            },
            Steps = new[]
            {
                new Step
                {
                    Title = L["FfOpenTheLoginPage"],
                    Detail = L["FfNavigateToAndEnterTheUser"],
                },
                new Step
                {
                    Title = L["FfValidateTheAccount"],
                    Detail = L["FfTheApiChecksTheUserRole"],
                },
                new Step
                {
                    Title = L["FfCompleteFirstloginPasswordChange"],
                    Detail = L["FfIfIsfirstloginIsTrueTheApi"],
                },
                new Step
                {
                    Title = L["FfReceiveTheSession"],
                    Detail = L["FfANormalLoginIssuesAnAccess"],
                },
                new Step
                {
                    Title = L["FfOpenTheDashboard"],
                    Detail = L["FfTheClientStoresTheTokensAnd"],
                },
            },
            Warnings = new[]
            {
                L["FfANewlyApprovedUserCannotLog"],
                L["FfFiveFailedAttemptsLockNonadminAccounts"],
                L["FfANewLoginReplacesThePrevious"],
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
            Title = L["FfCreateUser"],
            Tab = L["FfUser"],
            Icon = Icons.Material.Filled.EmojiPeople,
            Summary = L["FfCreateTheUserIdentityRoleAssignment"],
            Audience = L["FfAdministratorGroup00"],
            Approval = L["FfUserApproval"],
            Prerequisites = new[]
            {
                L["FfAGroupExistsForTheLater"],
                L["FfNoExistingUserOrPendingRequest"],
            },
            Steps = new[]
            {
                new Step
                {
                    Title = L["FfOpenUserMaintenance"],
                    Detail = L["FfGoToSettingUserMaintenanceList"],
                },
                new Step
                {
                    Title = L["FfAddTheUser"],
                    Detail = L["FfSelectAddUserAndFillIn"],
                },
                new Step
                {
                    Title = L["FfSubmitTheRequest"],
                    Detail = L["FfSaveStagesTheUserInTempuserapppending"],
                },
                new Step
                {
                    Title = L["FfApproveTheUser"],
                    Detail = L["FfOpenUserMaintenanceApprovalSelectApprove"],
                },
                new Step
                {
                    Title = L["FfConfirmTheResult"],
                    Detail = L["FfApprovalCreatesUmuserappAndUmuserappflagWith"],
                },
            },
            Warnings = new[]
            {
                L["FfAddUserHasNoGroupPicker"],
                L["FfAUserIdCannotBeReused"],
                L["FfTheSameAdministratorCanSubmitAnd"],
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
            Title = L["FfCreateGroup"],
            Tab = L["FfGroup"],
            Icon = Icons.Material.Filled.Groups,
            Summary = L["FfCreateTheGroupMasterThatIs"],
            Audience = L["FfAdministratorGroup00"],
            Approval = L["FfGroupApproval"],
            Prerequisites = new[]
            {
                L["FfTheGroupIdIsUnique"],
                L["FfNoPendingGroupRequestUsesThe"],
            },
            Steps = new[]
            {
                new Step
                {
                    Title = L["FfOpenGroupMaintenance"],
                    Detail = L["FfGoToSettingGroupMaintenanceList"],
                },
                new Step
                {
                    Title = L["FfAddTheGroup"],
                    Detail = L["FfSelectAddGroupAndFillIn"],
                },
                new Step
                {
                    Title = L["FfSubmitTheRequest"],
                    Detail = L["FfSaveStagesTheGroupInTempgroupapppending"],
                },
                new Step
                {
                    Title = L["FfApproveTheGroup"],
                    Detail = L["FfOpenGroupMaintenanceApprovalSelectApprove"],
                },
                new Step
                {
                    Title = L["FfConfirmTheResult"],
                    Detail = L["FfApprovalCreatesOrUpdatesUmgroupappAnd"],
                },
            },
            Warnings = new[]
            {
                L["FfGroup00IsTheAdministratorGroup"],
                L["FfAGroupThatIsStillAssigned"],
                L["FfAGroupIdCannotBeReused"],
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
            Title = L["FfAssignRole"],
            Tab = L["FfRole"],
            Icon = Icons.Material.Filled.AdminPanelSettings,
            Summary = L["FfConnectOneApprovedUserToOne"],
            Audience = L["FfAdministratorGroup00"],
            Approval = L["FfRoleApproval"],
            Prerequisites = new[]
            {
                L["FfTheUserIsAlreadyApprovedAnd"],
                L["FfTheGroupIsAlreadyApprovedAnd"],
                L["FfTheUserHasNoActiveRole"],
            },
            Steps = new[]
            {
                new Step
                {
                    Title = L["FfOpenRoleMaintenance"],
                    Detail = L["FfGoToSettingRoleMaintenanceList"],
                },
                new Step
                {
                    Title = L["FfAddTheRole"],
                    Detail = L["FfSelectAddRoleChooseTheUser"],
                },
                new Step
                {
                    Title = L["FfSubmitTheRequest"],
                    Detail = L["FfSaveStagesTheRoleInTbparametersytemtemporary"],
                },
                new Step
                {
                    Title = L["FfApproveTheRole"],
                    Detail = L["FfOpenRoleMaintenanceApprovalSelectApprove"],
                },
                new Step
                {
                    Title = L["FfRefreshTheSession"],
                    Detail = L["FfTheUserMustSignInAgain"],
                },
            },
            Warnings = new[]
            {
                L["FfRoleMaintenanceRejectsAUserThat"],
                L["FfOneUserHasOneRoleUmusergroupuserid"],
                L["FfCanAddeditCanDeleteAndRead"],
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
            Title = L["FfMenusAndMapping"],
            Tab = L["FfMenus"],
            Icon = Icons.Material.Filled.MenuBook,
            Summary = L["FfCreateMenusWithApprovalThenMap"],
            Audience = L["FfAdministratorGroup00"],
            Approval = L["FfMenuApprovalMappingIsImmediate"],
            Prerequisites = new[]
            {
                L["FfTheParentMenuExistsWhenA"],
                L["FfTheMenuUniqueValueIsNot"],
                L["FfTheTargetGroupExistsForMapping"],
            },
            Steps = new[]
            {
                new Step
                {
                    Title = L["FfOpenMenuMaintenance"],
                    Detail = L["FfGoToSettingMenuMaintenanceList"],
                },
                new Step
                {
                    Title = L["FfFillTheMenuForm"],
                    Detail = L["FfEnterMenuUniqueNameUrlComponent"],
                },
                new Step
                {
                    Title = L["FfApproveTheMenu"],
                    Detail = L["FfSaveStagesTheMenuRequestApprove"],
                },
                new Step
                {
                    Title = L["FfMapTheMenu"],
                    Detail = L["FfOpenMenuMaintenanceMappingMenuSelect"],
                },
                new Step
                {
                    Title = L["FfConfirmAccess"],
                    Detail = L["FfMappingIsAppliedImmediatelyParentMenus"],
                },
            },
            Warnings = new[]
            {
                L["FfAMenuWithoutAGroupMapping"],
                L["FfComponentTypeControlsWebRenderingA"],
                L["FfMappingHasNoApprovalAndReplaces"],
                L["FfAMenuWithChildMenusCannot"],
                L["FfParentLevelsAIsARoot"],
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
            Title = L["FfAddParameter"],
            Tab = L["FfParameter"],
            Icon = Icons.Material.Filled.Tune,
            Summary = L["FfRegisterANewParameterTableAnd"],
            Audience = L["FfAdministratorGroup00AndDeveloperOr"],
            Approval = L["FfParameterApproval"],
            Prerequisites = new[]
            {
                L["FfThePhysicalSqlTableExists"],
                L["FfTheTableNameIsAddedTo"],
                L["FfTheCardAndFieldMetadataWill"],
            },
            Steps = new[]
            {
                new Step
                {
                    Title = L["FfCreateAndRegisterTheTable"],
                    Detail = L["FfTheDeveloperCreatesTheSqlTable"],
                },
                new Step
                {
                    Title = L["FfAddTheParameterCard"],
                    Detail = L["FfOpenParameterMaintenanceMakerParameterSystem"],
                },
                new Step
                {
                    Title = L["FfApproveTheCard"],
                    Detail = L["FfApproveTheRequestFromParameterMaintenance"],
                },
                new Step
                {
                    Title = L["FfAddFieldMetadata"],
                    Detail = L["FfUseTheParameterSystemDetailCard"],
                },
                new Step
                {
                    Title = L["FfApproveTheMetadata"],
                    Detail = L["FfApproveTheFieldMetadataBeforeUsing"],
                },
            },
            Warnings = new[]
            {
                L["FfADatabaseCardAloneDoesNot"],
                L["FfCardAndFieldMetadataChangesUse"],
                L["FfOrderseqIsPartOfTheMetadata"],
                L["FfDynamicScreensCanWriteParameterTables"],
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
            Title = L["FfParameterMakerAndApproval"],
            Tab = L["FfMaker"],
            Icon = Icons.Material.Filled.Checklist,
            Summary = L["FfAddEditOrDeleteParameterData"],
            Audience = L["FfAdministratorGroup00"],
            Approval = L["FfParameterApproval"],
            Prerequisites = new[]
            {
                L["FfTheParameterTableIsRegistered"],
                L["FfTheCardAndFieldMetadataAre"],
            },
            Steps = new[]
            {
                new Step
                {
                    Title = L["FfOpenTheMaker"],
                    Detail = L["FfGoToParameterMaintenanceMakerAnd"],
                },
                new Step
                {
                    Title = L["FfAddOrEditData"],
                    Detail = L["FfTheFormIsGeneratedFromField"],
                },
                new Step
                {
                    Title = L["FfSubmitToStaging"],
                    Detail = L["FfSaveValidatesMandatoryFieldsAndDuplicate"],
                },
                new Step
                {
                    Title = L["FfReviewInApproval"],
                    Detail = L["FfOpenParameterMaintenanceApprovalAndSelect"],
                },
                new Step
                {
                    Title = L["FfApproveOrReject"],
                    Detail = L["FfSelectApproveOrRejectPerRow"],
                },
            },
            Warnings = new[]
            {
                L["FfOnlyOnePendingRequestIsAllowed"],
                L["FfApproveAllAndRejectAllAffect"],
                L["FfThereIsNoBatchEndpointAnd"],
                L["FfRejectionDoesNotStoreStatus5"],
                L["FfSensitiveColumnsAreHiddenFromThe"],
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
            Title = L["FfChangePassword"],
            Tab = L["FfPassword"],
            Icon = Icons.Material.Filled.Lock,
            Summary = L["FfChangeAPasswordDuringFirstLogin"],
            Audience = L["FfAllUsers"],
            Approval = L["FfNoApproval"],
            Prerequisites = new[]
            {
                L["FfTheCurrentPasswordIsKnown"],
                L["FfTheNewPasswordMeetsThePassword"],
            },
            Steps = new[]
            {
                new Step
                {
                    Title = L["FfFirstloginChange"],
                    Detail = L["FfUseTheInlineFormOnThe"],
                },
                new Step
                {
                    Title = L["FfNormalChange"],
                    Detail = L["FfOpenChangepasswordOrProfileChangePassword"],
                },
                new Step
                {
                    Title = L["FfEnterThePasswords"],
                    Detail = L["FfProvideTheCurrentPasswordNewPassword"],
                },
                new Step
                {
                    Title = L["FfValidateAndSave"],
                    Detail = L["FfTheServerValidatesThePasswordPolicy"],
                },
                new Step
                {
                    Title = L["FfSignInAgain"],
                    Detail = L["FfUseTheNewPasswordOnThe"],
                },
            },
            Warnings = new[]
            {
                L["FfPolicyAtLeast8CharactersWith"],
                L["FfNoApprovalIsRequired"],
                L["FfTheAuthenticatedChangeFlowDoesNot"],
                L["FfAnAdministratorResetSetsIsfirstloginTo"],
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
