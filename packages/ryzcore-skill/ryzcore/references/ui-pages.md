# UI standards, dynamic menus, and hosts

## Dynamic menus, screens, and parameters

Menus are database-driven, not hardcoded in the layout:

- `um_menu` stores menu identity, URL, parent, icon, order, component type,
  and level.
- `um_group_menu` maps menus to groups.
- Parent levels use `A`/`B`/`D` constants in `Domain.Enums.Const.ParentLevel`.
- Menu hierarchy is returned by query services and rendered by
  `RecursiveMenuItem`/the authenticated layout.
- The sidebar can therefore grow dynamically. Do not hardcode menu items into
  Web or MAUI layouts.

`DynamicComponentResolver` maps component names such as `DynamicScreen`,
`DynamicReport`, and `DynamicUrlView` to shared Razor pages.
Dynamic screen/report/parameter data is driven by SQL metadata.

`DynamicEngineService` and `ParameterEngineService` intentionally use raw SQL,
stored procedures, dynamic SQL, and explicit transaction logic. They are
engine boundaries, not ordinary CRUD services. Do not refactor them into
generic repositories without a separate approved design.

## Data-page UI standard

Data pages must share one visual and interaction pattern. The canonical
maintenance pages are `UserList`, `GroupList`, and `RoleList`; use their shell
as the starting point for every new CRUD/data page.

**List page shell**

1. `MudBreadcrumbs`.
2. `page-heading` **outside** the card:
   - `dashboard-kicker`;
   - `h1`;
   - short description;
   - primary Add button;
   - optional Export button;
   - import/upload actions use `MudFileUpload` with a `MudButton` as
     `CustomContent`; never use a `label` plus raw `InputFile` as a button;
   - long-running save, submit, select, export, and import actions must set
     the `is-busy` state on their `MudButton`; the global `ProcessOverlay`
     observes that state. Do not add page-specific progress overlays.
3. `MudPaper Elevation="0" Class="dashboard-card"`.
4. `MudTable` with:
   - `ServerData`;
   - `Hover="true"`;
   - `Dense="false"` unless the page explicitly needs compact rows;
   - `Breakpoint="Breakpoint.Sm"`;
   - `Elevation="0"`;
   - `HorizontalScrollbar="true"` for wide tables.
5. `ToolBarContent`: search field aligned to the right using `MudSpacer`.
6. `HeaderContent` column order:
   - selection checkbox first **only when the page supports bulk actions**;
   - `Actions` next, always left;
   - data columns after Actions;
   - every sortable header uses `MudTableSortLabel` with a backend-sortable
     `SortLabel`.
7. `RowTemplate` must follow the same order as the header. Every `MudTd` must
   have `DataLabel` for mobile breakpoint rendering.
8. Standard action cell:
   - Edit icon (`Color.Primary`);
   - Delete icon (`Color.Error`);
   - any page-specific action after those, using a consistent icon/button.
9. Always provide `NoRecordsContent`, `LoadingContent`, and `TablePager` with
   the same page-size options used by the canonical list pages.
10. Status uses the established `MudChip` colors: success = active/approved,
    error = locked/rejected, default = inactive/pending.

Do not move `Actions` to the right, do not add a checkbox without a real bulk
action, and do not create a one-off table toolbar. If row selection is required,
the checkbox is the first column and `Actions` remains immediately after it.

**Add/Edit dialog standard**

Use this exact structural pattern for every Add/Edit dialog:

```razor
<MudDialog>
    <TitleContent>
        <MudText Typo="Typo.h6">@Title</MudText>
    </TitleContent>

    <DialogContent>
        <MudForm @ref="_form">
            <MudGrid Spacing="2">
                <!-- Text/select/number/date: two fields per row -->
                <MudItem xs="12" sm="6">
                    <MudTextField Variant="Variant.Outlined" ... />
                </MudItem>
                <MudItem xs="12" sm="6">
                    <MudSelect Variant="Variant.Outlined" ... />
                </MudItem>

                <!-- Text area: always one full-width row -->
                <MudItem xs="12">
                    <MudTextField Variant="Variant.Outlined"
                                  Lines="3" ... />
                </MudItem>

                <!-- Boolean: always after all non-boolean fields,
                     one full-width row per switch -->
                <MudItem xs="12">
                    <MudSwitch T="bool" Color="Color.Primary"
                               Label="Active" ... />
                </MudItem>
            </MudGrid>
        </MudForm>
    </DialogContent>

    <DialogActions>
        <MudButton Variant="Variant.Outlined"
                   Color="Color.Default"
                   OnClick="@(_ => MudDialog.Cancel())">Cancel</MudButton>
        <MudButton Variant="Variant.Filled"
                   Color="Color.Primary"
                   Disabled="@_saving"
                   OnClick="@(_ => SubmitAsync())">Save</MudButton>
    </DialogActions>
</MudDialog>
```

Layout rules:

- Use `MudDialog` with `TitleContent`, `DialogContent`, and `DialogActions`.
- Use `MudForm` and `MudGrid Spacing="2"` inside `DialogContent`.
- Every text, select, numeric, or date input uses
  `Variant="Variant.Outlined"` and `MudItem xs="12" sm="6"`:
  two non-boolean fields per row on desktop/tablet, one per row on mobile.
- A multiline text area uses `MudTextField` with `Lines` (or the equivalent
  multiline field) and `MudItem xs="12"`: always one full-width row.
- A boolean uses `MudSwitch`, never `MudCheckBox`, and always has
  `MudItem xs="12"`: one full-width row per switch.
- Put all text/select/textarea fields first. Place all `MudSwitch` controls
  below them, in a separate boolean section. Do not insert a switch between
  text/select fields.
- Do not put two switches in one row and do not put a switch or textarea in a
  `sm="6"` column.
- Primary Save is the rightmost action. Cancel appears before it. Use the same
  labels (`Cancel`, `Save`) across the dialog family; do not mix
  `Close`/`Cancel` or `Save`/`Save Changes`.
- Show saving state on Save and prevent double submission.
- After a successful save: close the dialog with a result and reload the table
  through `ReloadServerData`.
- Validation errors and server errors must be user-friendly; technical details
  go to `tb_log`.
- Do not build an inline form on the list page when the established CRUD pattern
  uses a dialog.

Existing dialogs that use a different grid/order or field variants are legacy
deviations: normalize them when the dialog is next modified.

**Approval page standard**

- Use the same breadcrumb, page-heading, card, table, search, empty state,
  loading state, and pager shell.
- Approval-specific bulk buttons belong in the page-heading action group.
- The row decision control belongs in the left `Actions` area; the final
  Submit/Approve action stays in the same footer position on every approval
  page.
- Approval pages must not introduce a different table card, toolbar, or modal
  style. They may only differ in the row decision control and available bulk
  actions.

**Screen Menu page standard**

`ScreenMenuEditor` must use the same data-page shell as User/Group/Role:
breadcrumb and page-heading outside the card, `dashboard-card`, table toolbar,
Actions on the left, sortable data headers, DataLabel cells, and pager. A
screen-menu table must not put its heading inside the card or move Actions to
the right.

**Custom pages**

Custom layouts are allowed only for pages whose purpose is not a standard data
list, for example:

- Profile;
- Menu Mapping;
- Dashboard hero/landing content;
- an explicitly custom workflow screen.

Custom pages may use a different composition but must reuse the shared palette,
typography, card/button styles, spacing tokens, and authorization patterns.
Do not copy a custom layout into a CRUD list page, and do not create a separate
Web/MAUI implementation of a standard data page merely to change its look.
Visual host differences belong in CSS/theme, not in duplicated CRUD logic.

## Email provider/template parameters

`rf_email_provider` and `rf_email_template` are registered in
`tb_parameter_system` and `tb_parameter_system_detail`. Their cards must remain
available in both parameter maker and parameter approval.

- `ParameterSeeder` must keep the card rows and field metadata for both tables.
- `ParameterEngineService.AllowedTables` and the parameter stored procedures
  must list both table names.
- Provider SMTP/Gmail configuration is edited through the virtual fields
  `smtp_*` and `gmail_*`; `configuration_json` is stored encrypted in the
  temporary parameter payload but must never be exposed in the list, pending
  summary, or approval payload.
- Password/client-secret/refresh-token values are blank on read. Leaving them
  blank during an edit preserves the existing encrypted value; entering a new
  value re-encrypts it.
- Template `body_html` and `body_text` use multi-line fields; do not collapse
  them into a single-line text input.
- `rf_email_provider.id` is an identity key. Parameter approval for a new
  provider must insert without an explicit id.
- The old `MNT-EMAIL` and `MNT-EMAIL-PRV` menus are removed. Email provider
  configuration belongs under Parameter Maintenance; the legacy EmailProvider
  page/API may remain as a technical fallback but must not be re-seeded as a menu.

## Notification types and email gate

`rf_notification_type` is a reference/parameter table. It uses `BaseEntity` and a
`varchar(50)` uppercase string id. `tb_notification` is the transaction table and
also uses `BaseEntity`; its `notification_type_id` is `varchar(50)` and joins to
`rf_notification_type.id`.

- `ParameterSeeder` registers `rf_notification_type`; `ParameterEngineService`
  must keep it in `AllowedTables`.
- Maker/approval fields: `id`, `short_text`, `long_text` (textarea), `href`,
  `is_send_mail`, `is_send_web`, and `is_active` last.
- Notification creation requires a type id from this table. Do not auto-create a
  parameter type at runtime.
- `@[tboId]` in the type text/href is replaced with `tb_notification.reff_id`.
- Web real-time delivery uses `UserHub` and the `ReceiveNotification` event.
- Email delivery is implemented behind `rf_default_value.NOTIF_MAIL`. The seed
  value is `false`; changing it to `true` enables outbox queuing, but the type
  must also have `is_send_mail = true` and an active email provider must exist.
- `NOTIF_MAIL` is edited through the existing `rf_default_value` parameter card.

## Mobile dashboard pages

MAUI dashboard pages use a separate mobile shell and routes; do not put large
`@if (IsMauiApp)` blocks into the Web pages.

```text
/maui/dashboard     -> MauiDashboard + MauiAuthenticatedLayout
/maui/profile       -> MauiProfile + MauiAuthenticatedLayout
/maui/notifications -> MauiNotifications + MauiAuthenticatedLayout
/maui/menu          -> all authorized D menus
/maui/settings      -> MNT subtree
```

The mobile dashboard consumes:

- `GET api/menu` for the authorized menu tree;
- `GET api/dashboard/widgets` for user-scoped dashboard widgets;
- `GET api/notification/recent` for the notification badge;
- `GET api/notification/unread-count` for the exact unread badge;
- `/api/auth/me` and the existing profile route for profile data.

The dashboard menu list includes level-D menus outside the `MNT` subtree, shows
at most seven items, and always provides a "Lihat Semua" entry. The setting icon
is rendered only when the authorized menu tree contains an `MNT` subtree.

`DashboardWidgetService` is currently a demo backend source. Its DTO and HTTP
contract are the replacement point for a future user/group widget table; do not
hardcode widget data in the MAUI page.

## Web and MAUI hosts

Web and MAUI share Razor pages/components in `Shared`, but are separate hosts:

```text
Web -> Blazor WebAssembly, localhost API, desktop shell
App -> MAUI Blazor Hybrid, app shell, mobile behavior
```

Host-specific behavior:

- Web loads Shared CSS plus `Web/wwwroot/css/app.css`.
- MAUI loads Shared CSS plus `App/wwwroot/app.css` and uses MAUI resources.
- The MAUI login page currently selects a host-specific full-screen auth layout;
  the Web login keeps its split-screen layout.
- Authenticated layout behavior is shared and responsive. Do not assume that a
  separate mobile dashboard already exists; check the current layout before
  changing it.
- Do not introduce Web-only APIs into Shared. Use service abstractions or
  host-specific implementations.
- The MAUI API base URL comes from `Resources/Raw/appsettings.json` and
  `MauiApiSettings`; fallback order includes the Android emulator host
  `10.0.2.2`.
- Debug MAUI builds use a certificate-validation bypass; do not copy that
  behavior into Release builds.
- UI style has a shared neo-brutalist theme plus default mode. Style choice is
  persisted in browser/app storage under `enterprise-ui-style` (`default`/`neo`,
  with legacy `classic` mapped to `default`).
- Color palette uses one of `default`, `fintech`, `monochrome`, `creative`, or
  `wellness`, persisted under `enterprise-ui-palette`. Installer defaults are
  generated into `Shared/Theme/ThemeDefaults.cs`; runtime choices override them.
- Theme palette is applied through `ThemeCatalog`/`ThemePreferenceService` and
  CSS variables under `html[data-palette]`; do not add page-specific palette CSS.
- Web and MAUI must both build after Shared UI changes.

### MAUI standalone APK

Fast Deployment is for IDE debugging only. A standalone Debug APK must embed
assemblies:

```powershell
dotnet publish <Project>.App.csproj `
  -f net10.0-android `
  -c Debug `
  -p:AndroidPackageFormats=apk `
  -p:EmbedAssembliesIntoApk=true `
  -p:AndroidEnableProfiledAot=false
```

Without `EmbedAssembliesIntoApk=true`, the APK can fail at startup with:

```text
No assemblies found in .../.__override__/arm64-v8a
```

The signed output is under
`bin/Debug/net10.0-android/<ApplicationId>-Signed.apk`.
