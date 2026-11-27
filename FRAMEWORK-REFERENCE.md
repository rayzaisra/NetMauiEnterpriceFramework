# RyzCore Framework Reference

> **Read this before changing the framework or a generated application.**
>
> This document describes the actual architecture and conventions of
> `RyzCore`. Do not infer behavior from generic .NET
> templates; follow the patterns and constraints below.

This guidance is also packaged as a **separate** Agent Skill,
`ryzcore-skill`, under `packages/ryzcore-skill/ryzcore/` (`SKILL.md` +
`references/`). It is not part of the generated solution or the npm generator
package. When a rule changes in this document, mirror it in the skill
references.

## 1. Repository and generator

The repository has three distinct layers:

```text
src/                 Node.js scaffolder and SQL Server setup
bin/                 CLI entry point
templates/solution/  .NET 10 solution template copied by the generator
packages/ryzcore-skill/  separate agent-skill npm package (not shipped by the generator)
```

The generated application is produced by replacing tokens in the template:

| Token | Purpose |
|---|---|
| `__PROJECT__` | Project/root namespace name |
| `__ROOT_NAMESPACE__` | Root namespace |
| `__DB_NAME__` | Main database name |
| `__AUDIT_DB_NAME__` | Audit database name |
| `__SQL_SERVER__`, `__SQL_USER__`, `__SQL_PASSWORD__` | SQL Server connection |
| `__JWT_SECRET__` | Generated JWT signing secret |
| `__ENCRYPTION_PASSPHRASE__` | Generated encryption passphrase |
| `__API_URL__` | API public URL, normally `https://localhost:7143` |
| `__WEB_URL__` | Web public URL, normally `https://localhost:44353` |
| `__APP_ID__` | Android application ID |
| `__ADMIN_*__` | Initial administrator seed |
| `__BRAND_NAME__` | Display name |

Rules for template edits:

- Keep tokens intact unless the change is intentionally generated-specific.
- Never hardcode a generated project name, database name, secret, or port.
- Never edit `templates/**/bin` or `templates/**/obj`.
- The template is copied into a generated solution; changes there affect every
  future generated application.

## 2. Generated project map

Typical generated solution:

```text
<Project>.Domain
<Project>.Application
<Project>.Infrastructure
<Project>.Api
<Project>.Shared
<Project>.Web
<Project>.App
```

Dependency direction:

```text
Domain            <- no project dependency
Application       -> Domain
Infrastructure    -> Application (and transitively Domain)
Api               -> Application + Infrastructure
Shared            -> Application + Domain
Web               -> Shared
App               -> Shared
```

Do not make Shared depend on Web/App. Do not put DbContext or EF Core in
`Domain` or `Shared`. Host-specific behavior belongs in the host project or is
selected through host abstractions/DI.

## 3. Non-negotiable coding rules

- Preserve public behavior unless the task explicitly asks to change it:
  routes, component types, DTO names/properties, JSON shape, error messages,
  authorization, database schema, and transaction boundaries.
- Read the existing implementation before editing. Do not guess an endpoint,
  table, column, DTO, or interface.
- Keep business logic out of controllers and Razor components. Controllers
  validate/auth/return; use-case services implement business rules.
- Repository = database access. Service = business/use-case logic. Query service
  = read-only projections, filtering, sorting, joining, and paging.
- A repository must not call `SaveChanges` or `SaveChangesAsync`.
- Writes are committed through `IUnitOfWork.SaveChangesAsync`.
- Use a transaction only when a use case updates more than one aggregate or
  performs an explicit multi-step business transaction.
- Do not introduce a public generic repository as the primary abstraction.
  Entity keys vary (`string`, `int`, `long`, `Guid`, composite), and not every
  entity inherits `BaseEntity`/`BaseAuditEntity`; follow the actual table role.
- Keep audit capture centralized in `__PROJECT__DbContext`. Do not manually
  create `tb_audit_trail` rows for normal CRUD.
- Never swallow an exception silently. If a service catches an exception and
  handles it, it must call `IErrorLogService.LogAsync`. If it cannot handle it,
  let it bubble to `ApiExceptionMiddleware`; do not log the same exception at
  both layers.
- Keep sensitive values out of audit: password, token, refresh, secret,
  configuration, email body, and long text are intentionally filtered.
- Entity property names intentionally use lower snake_case to match SQL
  columns. Do not "fix" them to PascalCase.
- Use async EF APIs and pass `CancellationToken` to database/API methods.
- Never add `bin/`, `obj/`, generated APKs, database files, logs, or captured
  bug reports to commits.
- Generated applications use an admin group code of `"00"`. Do not replace it
  with `"Admin"` or a display name.

## 4. C# and Razor conventions

- Nullable is enabled; implicit usings are enabled.
- Use file-scoped namespaces.
- Types, methods, properties, and DTOs use PascalCase.
- Parameters/local variables use camelCase; private fields use `_camelCase`.
- Interface names start with `I`.
- DTO names end with `Dto`; list/page DTOs use the existing naming style.
- Keep one public entity class per file, named after the SQL table in lowercase
  (for example `um_user_app.cs`, `tb_email_outbox.cs`).
- Keep one repository/query interface per file. Do not combine unrelated
  interfaces into a "repositories" file.
- Keep one repository **implementation class** per file, with the file named
  after the class (for example `UmUserAppRepository.cs`). Do not create
  `AuthRepositories.cs`, `ManagementRepositories.cs`, or any other combined
  repository file. The same one-public-class-per-file rule applies to query
  services and use-case services.
- Do not add a new repository class to an existing combined repository file.
  Split the combined file into one file per repository class instead.
- Use `Task`/`Task<T>` and the `Async` suffix for asynchronous operations.
- Use `record` only where the existing code uses it; do not introduce records
  for EF entities.
- Razor components should call injected application/API services, not EF
  contexts or SQL.
- Follow the existing MudBlazor and CSS class conventions. Do not add a new
  UI framework or duplicate a page just to change styling.
- The framework targets .NET 10, MudBlazor 9.x, EF Core 10, SQL Server, Blazor
  WebAssembly, and MAUI Blazor Hybrid.

## 5. Database architecture

There are two SQL Server databases per generated application:

1. **Main database** — business/application data.
2. **Audit/log database** — `tb_audit_trail`, `tb_audit_trail_detail`, `tb_log`,
   `tb_log_activity`.

Two EF Core `DbContext` instances are used:

```text
__PROJECT__DbContext  -> main database
AuditLogDbContext     -> audit/log database
```

The audit database connection is `ConnectionStrings:AuditLogConnection`.
The main connection is `ConnectionStrings:DefaultConnection`.

At API startup in Development (or with `--seed-only`):

- both databases are migrated;
- synonyms in the main database point to audit tables in the audit database;
- seed data is applied when `Seed:Enabled` is true.

Key main-table groups:

```text
enum_*                      reference enums
um_user_app                 users
um_user_group               user/group membership
um_group_app                groups/roles
um_user_app_flag            login/lock flags
um_user_app_session         active sessions
um_user_refresh_token       refresh tokens
um_menu, um_group_menu      dynamic menu and group mapping
tb_screen_menu*             dynamic screen configuration
tb_report_set               report configuration
tb_parameter_*              parameter engine and approval staging
temp_*_pending              maker-checker staging
tb_notification             notification transactions (BaseEntity)
rf_notification_type        notification type/template parameter (BaseEntity, varchar id)
rf_email_provider           email provider parameter (main DB)
rf_email_template           email template parameter (main DB)
tb_email_outbox             outbound email queue (main DB)
tb_password_reset_token     password reset
rf_*                        reference data
proc_seq                    sequence helper
tb_log                      operational/error log (audit DB)
tb_log_activity             automatic HTTP API activity log (audit DB)
```

`BaseAuditEntity` fields include `isActive`, `isDeleted`, `createdBy`,
`createdDate`, `modifiedBy`, `modifiedDate`, `deletedBy`, and `deletedDate`.
`BaseEntity` adds approval/lock/default fields. Nullable audit fields are
intentional; use `isDeleted != true` for active records unless the existing
query explicitly does otherwise.

`um_user_app` intentionally has no `kode_cabang`/`branch_code` field. Do not
add it back to the entity, migrations, DTOs, seeders, installer prompts, or
configuration. The generator must not ask for a branch code during installation.

### 5.1 Base entity contract

Both base classes live in one file:
`templates/solution/__PROJECT__.Domain/Entities/BaseEntity.cs`; in a generated
application this is `<Project>.Domain/Entities/BaseEntity.cs`. There is no
separate `BaseAuditEntity.cs`. Do not create a duplicate class or copy these
properties into individual entities.

```csharp
public abstract class BaseAuditEntity
{
    [Column("is_active")]
    public bool? isActive { get; set; }

    [Column("is_deleted")]
    public bool? isDeleted { get; set; }

    [Column("created_by")]
    [StringLength(100)]
    public string? createdBy { get; set; }

    [Column("created_date")]
    public DateTime? createdDate { get; set; }

    [Column("modified_by")]
    [StringLength(100)]
    public string? modifiedBy { get; set; }

    [Column("modified_date")]
    public DateTime? modifiedDate { get; set; }

    [Column("deleted_by")]
    [StringLength(100)]
    public string? deletedBy { get; set; }

    [Column("deleted_date")]
    public DateTime? deletedDate { get; set; }
}
```

`BaseEntity` extends it with the approval and locking fields:

```csharp
public abstract class BaseEntity : BaseAuditEntity
{
    [Column("status_approval")]
    public int? statusApproval { get; set; }

    [Column("is_locked")]
    public bool? isLocked { get; set; }

    [Column("is_default")]
    public bool? isDefault { get; set; }

    [Column("approved_by")]
    [StringLength(100)]
    public string? approvedBy { get; set; }

    [Column("approved_date")]
    public DateTime? approvedDate { get; set; }

    [Column("approval_type_id")]
    [StringLength(2)]
    public string? approvalTypeId { get; set; }
}
```

Rules for a new table/entity:

- A new business/CRUD table that needs standard audit columns must inherit
  `BaseAuditEntity`.
- A new business table that also needs maker-checker/lock/default approval fields
  must inherit `BaseEntity`. Do not inherit both; `BaseEntity` already includes
  `BaseAuditEntity`.
- Do not repeat `isActive`, `isDeleted`, `createdBy`, `createdDate`,
  `modifiedBy`, `modifiedDate`, `deletedBy`, or `deletedDate` in the derived
  entity.
- Audit/log tables (`tb_audit_trail`, `tb_audit_trail_detail`, `tb_log`, and
  `tb_log_activity`) intentionally do not inherit these classes because their
  schemas are managed separately.
- Custom staging/detail/join/projection tables follow the existing entity
  pattern; do not force `BaseEntity` inheritance when the table does not use
  those columns.
- The base classes do not define the primary key. Each entity defines its own
  key and table mapping.
- The base columns are nullable by design. Do not change their nullability
  without an explicit schema requirement.
- When a new derived entity is mapped, EF creates the inherited columns in the
  new table. For an existing table, add a new migration instead of editing an
  applied migration.

Schema changes must be made through a new EF migration. Do not edit an applied
migration. Main and audit migrations live under:

```text
Infrastructure/Migrations/Main
Infrastructure/Migrations/Audit
```

## 6. Audit and logging flow

`__PROJECT__DbContext.SaveChanges` / `SaveChangesAsync`:

1. captures Added/Modified/Deleted entries;
2. saves the main transaction;
3. writes audit headers/details to `AuditLogDbContext`;
4. does not fail the business save if audit writing fails.

Sensitive audit columns are skipped by name matching. Do not weaken this.

Operational errors use `IErrorLogService.LogAsync` and are written to
`tb_log` (`entity`, `log_type`, `log_msg`, `created_by`). API exceptions are
captured by `ApiExceptionMiddleware`. Mobile client errors can be sent to
`POST /api/client-error`, which writes the technical detail to `tb_log`.
The UI should show a user-friendly message; never show raw HTML/stack traces.

For a network outage, the audit API may also be unreachable. In that case the
client shows the generic error and the technical log cannot be persisted until
the server is reachable. Do not claim an audit row was written when the API
request failed.

### 6.1 Error logging contract (`tb_log`)

`tb_log` is the operational-error log. It is separate from
`tb_audit_trail`, which records data changes.

The required error path is:

```text
repository/query fails
  -> service may catch only if it can handle the error
     -> if handled, call IErrorLogService.LogAsync(...)
     -> if not handled, rethrow
  -> ApiExceptionMiddleware catches the unhandled exception
     -> IErrorLogService.LogAsync(entity, "ERROR", exception, userId)
     -> row is written to tb_log in the audit database
```

Rules:

- Repositories generally do **not** catch exceptions. Let them bubble so the
  API boundary logs them once. This avoids duplicate `tb_log` rows.
- A service that catches an exception and returns a controlled failure must
  log the technical detail through `IErrorLogService` before returning.
- Background jobs must log their own caught exceptions through
  `IErrorLogService`, because there is no HTTP middleware for them.
- Mobile/client errors may be sent to `POST /api/client-error`; the endpoint
  writes the technical detail to `tb_log`.
- The UI must show a general message for connectivity/server failures. Never
  display raw HTML, stack traces, SQL errors, or internal exception messages.
- Error logging is best-effort: if the audit database or API is unreachable,
  the original request still fails/handles normally and the log cannot be
  persisted at that moment.

Do not add a `try/catch` to every repository just to write `tb_log`. That
creates duplicate logging and hides the original call stack. Log at the layer
that actually handles the exception.

### 6.2 Automatic data audit (`tb_audit_trail`)

The following is automatic only for tracked entities saved through the
main `__PROJECT__DbContext.SaveChanges` or `SaveChangesAsync`:

- `EntityState.Added` -> audit header type `insert`;
- `EntityState.Modified` -> audit header type `update`, changed properties only;
- `EntityState.Deleted` -> audit header type `delete`;
- audit header and details are written to the audit database after the main
  save succeeds;
- password, token, refresh, secret, configuration, email body, and long-text
  columns are excluded.

Important limits:

- `ExecuteSqlRaw`, `ExecuteSqlInterpolated`, raw ADO.NET, stored procedures,
  and dynamic SQL are **not** automatically audited. If a raw-SQL write must be
  audited, implement it explicitly in the owning engine/service.
- `AuditLogDbContext` is intentionally not self-audited; writing an audit row
  must not create another audit row recursively.
- The audit write is non-blocking for the business transaction: if the audit
  database write fails, the main business save is not rolled back.
- Audit write failures are swallowed by design. Do not report to a user that
  an audit row definitely exists when the audit DB is unavailable.
- Do not manually add `tb_audit_trail` rows for normal entity CRUD; this would
  duplicate the centralized audit.

### 6.3 Automatic API activity log (`tb_log_activity`)

`tb_log_activity` records HTTP API calls in the audit database. It is written
automatically by `ApiActivityMiddleware`; do **not** add logging code to each
controller. A newly added controller is covered automatically when it is in the
same API host and passes through the normal middleware pipeline.

Pipeline placement:

```text
UseRouting
  -> UseCors
  -> UseAuthentication
  -> ApiActivityMiddleware
  -> rate limiting
  -> ActiveSessionMiddleware
  -> UseAuthorization
  -> controller / endpoint
```

Recorded fields:

```text
id, user_id, ip_address, http_method, path, route_template,
status_code, success, duration_ms, correlation_id, user_agent,
client_type, error_code, created_date
```

Behavior:

- `user_id` comes from `ClaimTypes.NameIdentifier` after authentication.
- `success` is true for HTTP 2xx/3xx.
- `path` is stored without the query string. Request/response bodies,
  authorization headers, passwords, tokens, and secrets are never logged.
- `client_type` is optional and comes from the `X-Client-Type` request header.
- `/health`, `/swagger`, `/hangfire`, and `/favicon.ico` are excluded by default
  through `AuditActivity:ExcludedPaths`.
- Writes are queued in memory and persisted in batches by
  `ApiActivityLogWriter`; a database write never delays or fails the business
  request. Activity rows can be lost during an abrupt process shutdown or if
  the queue/database is unavailable.
- Unknown/non-existent API paths may also produce a 404 activity row unless
  excluded. SignalR requests use the same middleware unless their path is
  excluded.
- For an authenticated user to be recorded, the middleware must remain after
  `UseAuthentication`.
- Behind a reverse proxy, configure forwarded headers so `ip_address` is the
  client address rather than the proxy address.
- The feature is configured by `AuditActivity:Enabled`, `QueueCapacity`,
  `BatchSize`, `FlushIntervalSeconds`, and `ExcludedPaths`.

Do not add manual `tb_log_activity` writes in controllers. If a request must be
excluded, add a narrow path exclusion rather than disabling logging globally.

## 7. Repository, Unit of Work, and query services

Interfaces live in `Application/Interfaces/Repositories` and
`Application/Interfaces/Queries`. Implementations live in
`Infrastructure/Repositories` and `Infrastructure/Queries`.

Interface file naming is part of the contract:

- one public interface per file;
- the file name must match the interface name exactly;
- query interface names use the `I` prefix and `QueryService` suffix;
- use `IUserQueryService.cs`, not `UserQueries.cs`;
- use `IUserQueryService`, not `UserQueries` or `IUserQueries`.

Repository responsibilities:

- fetch tracked entities for updates;
- existence checks;
- add/update/remove operations;
- execute database-specific operations for that use case.

Repository restrictions:

- no `SaveChanges`;
- no business validation;
- no audit rows;
- no HTTP/UI logic.

Query-service responsibilities:

- read-only projections;
- joins;
- search/filter/sort/paging;
- `AsNoTracking` where the result is not mutated;
- return existing DTOs, usually `GeneralListResponseDto<T>`;
- use `DataTableRequestDto` and honor `PageNumber`, `PageSize` (with a safe
  maximum), `SearchTerm`, `SortColumn`, and `SortDirection`.

Use-case service responsibilities:

- orchestrate repositories/query services;
- enforce business rules;
- call `IUnitOfWork.SaveChangesAsync`;
- use `BeginTransactionAsync` only for explicit multi-step transactions;
- preserve existing audit/transaction ordering.

`IUnitOfWork` exposes the specific repositories and `SaveChangesAsync`.
`UnitOfWork` lazily constructs repositories over the shared DbContext.

Do not replace this with `IRepository<TEntity>` everywhere. A small internal
generic base in Infrastructure may be acceptable, but the public contract must
remain specific and intentional.

## 8. API conventions

- Controllers use `[ApiController]` and `[Route("api/[controller]")]`.
- Authenticated controllers use `[Authorize]`.
- Do **not** use `[Authorize(Roles = "00")]` for maintenance or business
  endpoints that a non-`00` group may access. Roles are static claims and cannot
  represent database menu mappings or the three `um_user_group` flags.
- Use `[RequireMenuAccess(PermissionAction.Action, "MENU-UNIQUE")]` for
  menu-scoped endpoints. Group `00` bypasses the filter; every other group needs
  an active `um_group_menu` row and the appropriate flag.
- Keep `[Authorize(Roles = "00")]` only on endpoints that must remain
  super-admin-only regardless of database configuration.
- `[AllowAnonymous]` is limited to authentication/health/bootstrap endpoints.
- Do not accidentally expose dropdowns or lookups that contain admin data.
- Preserve the existing response envelope (`ApiResponse<T>` /
  `BaseGeneralResponse` / `GeneralListResponseDto<T>`).
- Do not return EF entities directly.
- Log unexpected exceptions through `IErrorLogService`; return the established
  error envelope.
- The API owns JWT validation, refresh/session validation, CORS, rate limiting,
  Hangfire, SignalR, and Swagger.
- Development CORS allows localhost/127.0.0.1; production uses
  `Cors:AllowedOrigins`.
- The API default development profile is `https` on port `7143`;
  Web uses `https://localhost:7143`; Android emulator uses
  `https://10.0.2.2:7143`.
- Visual Studio IIS Express can expose the same API on `44333`. The Web client
  probes `ApiFallbackUrls` in Development, which includes both `7143` and
  `44333`; keep that fallback when changing launch profiles and rebuild Web so
  `appsettings.Development.json` is regenerated.
- MAUI also probes `44333`, `7143`, and `44325` during startup. Windows MAUI can
  use IIS Express on `https://localhost:44333`. Android emulator should use the
  Kestrel profile on `0.0.0.0:7143` through `https://10.0.2.2:7143`; IIS Express
  bound only to localhost is not reliably reachable from the emulator.

Typical controller structure:

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ExampleController : ControllerBase
{
    private readonly IExampleService _service;

    public ExampleController(IExampleService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] DataTableRequestDto request,
        CancellationToken cancellationToken)
        => Ok(ApiResponseFactory.Success(
            await _service.GetListAsync(request, cancellationToken)));
}
```

### 8.1 Menu-scoped action permissions

Authorization has two layers:

1. **Menu mapping** (`um_group_menu`) decides which pages/modules a group can
   open.
2. **Group flags** (`um_user_group`) decide which actions are allowed inside
   those pages.

Group `00` is the super administrator and bypasses both layers. Other groups are
evaluated by `IPermissionService`/`PermissionRules`:

| Condition | Result |
|---|---|
| `can_read_only = true` | All business actions denied, including Export and View Detail. Load/search/paging remain allowed. |
| `can_add_edit = true` | Add, Edit, Import, Upload, Export, View Detail, Approve, Reject, Submit, Save, Process, Reset Password, and Unlock are allowed. |
| `can_delete = true` | Delete and bulk delete are allowed. |
| `can_add_edit = false`, `can_delete = true` | Delete-only UI: only delete actions appear. |
| `PermissionAction.Load` | Allowed whenever the menu is mapped (subject to `can_read_only`, which does not block reads). |

Endpoint pattern:

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
[RequireMenuAccess(PermissionAction.Load, "MNT-USER-LST")]
public class UserController : ControllerBase
{
    [HttpPost("list")]
    public async Task<IActionResult> GetList([FromBody] DataTableRequestDto request) { ... }

    [HttpPost]
    [RequireMenuAccess(PermissionAction.AddEdit, "MNT-USER-LST")]
    public async Task<IActionResult> Create([FromBody] UserEditDto dto) { ... }

    [HttpDelete("{userId}")]
    [RequireMenuAccess(PermissionAction.Delete, "MNT-USER-LST")]
    public async Task<IActionResult> Delete(string userId) { ... }
}
```

Rules:

- Pass multiple menu uniques when one endpoint serves more than one page, for
  example parameter maker/approval endpoints:
  `RequireMenuAccess(PermissionAction.Load, "MNT-PARAM-MKR", "MNT-PARAM-APR")`.
- Dynamic screens use `IPermissionService.HasScreenAccessAsync(screenId, action)`
  because their menu identity is `um_menu.menu_url`, not `menu_unique`.
- Permission changes are read from the database per request. The API responds
  immediately; the client UI reflects the change after refresh/navigation.
  This is not token revocation or forced logout.
- Do not add per-page permission matrices. Use the three flags plus menu
  mapping; if a future action is truly different, add a `PermissionAction` and
  document the mapping in `PermissionRules`.

Client pattern:

```razor
<PermissionGate Action="PermissionAction.AddEdit">
    <MudButton ...>Add</MudButton>
</PermissionGate>

<PermissionGate AnyOf="PermissionSets.EditDelete">
    <MudTh>Actions</MudTh>
</PermissionGate>
```

- Gate page action groups, row-action columns/cells, bulk actions, import/export,
  approval controls, and dialog save buttons.
- Keep search, load, sort, paging, refresh, cancel/close, navigation, profile,
  theme, and logout ungated.
- The sidebar only exposes menus returned by `GET api/menu`; do not hardcode
  maintenance menu items for non-`00` groups.
- New business pages must use the same `PermissionGate` pattern. Blazor cannot
  transparently intercept every raw `MudButton`, so the gate is mandatory at the
  action site.

## 9. Dynamic menus, screens, and parameters

Menus are database-driven, not hardcoded in the layout:

- `um_menu` stores menu identity, URL, parent, icon, order, component type,
  and level.
- `um_menu.menu_icon`, `tb_parameter_system.icon`, and
  `enum_menu_icon.menu_icon_code` store exact MudBlazor
  `Icons.Material.Filled` field names such as `Settings`. Do not store Font
  Awesome classes or SVG markup there. `menu_icon_name` is the display
  description; `menu_icon_name_alternate` must stay `NULL`. `MenuIconResolver`
  resolves the stored name to the MudBlazor SVG value at render time.
- `um_group_menu` maps menus to groups.
- Parent levels use `A`/`B`/`D` constants in `Domain.Enums.Const.ParentLevel`.
- Menu hierarchy is returned by query services and rendered by
  `RecursiveMenuItem`/the authenticated layout.
- The sidebar can therefore grow dynamically. Do not hardcode menu items into
  Web or MAUI layouts.
- The framework flow guide is the custom page
  `/dashboard/documentation/framework-flows`. It is readable by every
  authenticated user. The `DOC` root is rendered as the fixed top-bar `Docs`
  menu, not in the sidebar; its `DOC-FLOW` child is the `Framework Flows`
  dropdown item. `MenuSeeder.EnsureDocumentationMenuAsync` creates both rows for
  existing databases and maps them to every active group; it is idempotent and
  must remain safe to run on every seed.

`DynamicComponentResolver` maps component names such as `DynamicScreen`,
`DynamicReport`, and `DynamicUrlView` to shared Razor pages.
Dynamic screen/report/parameter data is driven by SQL metadata.

`DynamicEngineService` and `ParameterEngineService` intentionally use raw SQL,
stored procedures, dynamic SQL, and explicit transaction logic. They are
engine boundaries, not ordinary CRUD services. Do not refactor them into
generic repositories without a separate approved design.

### 9.1 Data-page UI standard

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

**Current normalization note**

Some existing pages still differ from this target, for example screen-menu and
approval pages use a different action position/toolbar, and some dialogs mix
field/button variants. Treat those as legacy deviations: do not copy them into
new pages; normalize them when the page is next modified.

### 9.2 Email provider/template parameters

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

### 9.3 Notification types and email gate

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

### 9.4 Mobile dashboard pages

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

## 10. Authentication and authorization

- JWT access + refresh tokens; active sessions are checked by middleware.
- Idle timeout is server-enforced through `Jwt:IdleTimeoutMinutes` (default
  `15`). Authenticated API requests refresh `um_user_app_session.last_activity`
  (throttled writes); when idle time exceeds the configured limit, the session
  and refresh tokens are revoked and the next authenticated request or refresh
  returns `401`. Client-side timers/dialogs are UX only.
- `GET /api/auth/session-time` returns idle remaining seconds
  (`remainingSeconds` for compatibility, plus `idleRemainingSeconds` and
  `idleTimeoutMinutes`). It is not treated as user activity for idle reset.
- Login, refresh, forgot-password, reset-password validation/reset, and
  first-login password change are anonymous endpoints.
- All other API endpoints require authentication unless explicitly reviewed.
- Administrator role code is `"00"`.
- `AuthController` handles login and token lifecycle; do not duplicate auth
  logic in UI services.
- Password reset uses `tb_password_reset_token` and `tb_email_outbox`; tokens
  are stored hashed and are single-use/expiring.
- Sensitive values must never be logged or audited.

## 11. Web and MAUI hosts

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

## 12. Configuration and secrets

Primary files:

```text
Api/appsettings.json
Api/appsettings.Development.json
Web/wwwroot/appsettings.json
Web/wwwroot/appsettings.Development.json
App/Resources/Raw/appsettings.json
```

Important keys:

```text
ConnectionStrings:DefaultConnection
ConnectionStrings:AuditLogConnection
Jwt:*
Cors:AllowedOrigins
PasswordReset:PublicBaseUrl
Encryption:PassPhrase
IpRateLimiting:*
```

Use generated tokens/configuration; never commit production secrets.
Development settings may contain local SQL credentials in a generated test
instance, but not in the framework source.

`Jwt:Secret` must be at least 64 bytes because signing uses HMAC-SHA512. The
generator supplies a random 64-byte Base64 secret. Do not leave
`__JWT_SECRET__` or another short placeholder in a running app; it causes
`IDX10653` during token generation. Generate a replacement with 64 random bytes
and keep it only in environment/user secrets or local development settings.

## 13. Adding a feature: expected order

1. Inspect the existing entity, DTO, repository/query/service, controller, and
   UI pattern for the closest feature.
2. Add the entity/table mapping and a new migration; never edit an applied
   migration.
3. Add request/response DTOs without changing existing JSON shapes.
4. Add a specific repository interface and implementation for writes. Put the
   interface and implementation in separate files with matching class names.
5. Add a query interface/implementation for list/search/sort/join.
6. Add the use-case service and register it in API DI.
7. Add or update the controller with correct authorization and response
   envelope.
8. Add the Shared API service and Razor page/component.
9. Add menus through the existing seeder/database process if the feature needs
   navigation. Do not hardcode it in the layout.
10. Update tests or add a focused test where the project supports it.
11. Build API, Web, and MAUI. Run migrations/seed in a disposable database.
12. Never commit generated test databases, build output, secrets, or APK files.

## 14. Build and verification

Framework checks:

```powershell
dotnet build templates/solution/<Project>.Web/<Project>.Web.csproj
dotnet build templates/solution/<Project>.App/<Project>.App.csproj -f net10.0-windows10.0.19041.0
npm run check
```

Generated application:

```powershell
dotnet build <Project>.sln
dotnet run --project <Project>.Api
dotnet run --project <Project>.Web
```

MAUI Android debug deployment uses the active Android emulator/device.
Do not use generated `bin/obj`, `AppPackages`, or test artifacts as source.

## 15. AI guardrails

Before editing:

- Search for the existing implementation and mirror its conventions.
- Identify whether the task is Domain, Application, Infrastructure, API,
  Shared, Web, or MAUI; do not move logic across boundaries casually.
- Check authorization and multi-database implications.
- Check whether the table is soft-deleted and whether approval fields apply.
- Check whether the operation must use a transaction.

Never:

- invent tables, columns, endpoints, DTO fields, or service methods;
- change response JSON or public routes without an explicit requirement;
- call `SaveChanges` from a repository;
- use `DbContext` directly in Razor;
- add audit writes manually for normal CRUD;
- swallow an exception without calling `IErrorLogService` or rethrowing it;
- add `try/catch` logging to every repository instead of letting a single
  boundary handle the error;
- rename lower-snake-case entity fields;
- replace specific repositories with a generic repository;
- combine multiple repository interfaces or implementation classes into one
  file;
- create a one-off CRUD table/dialog/approval layout instead of the canonical
  User/Group/Role data-page shell;
- move the `Actions` column to the right or add checkboxes without real bulk
  actions;
- build inline CRUD forms or page-specific modal styles when the standard dialog
  pattern applies;
- put a textarea or `MudSwitch` in a half-width dialog column;
- place a `MudSwitch` between text/select fields instead of below all
  non-boolean inputs;
- use a custom layout for a standard data page (Profile and Menu Mapping are
  allowed custom pages);
- edit applied migrations;
- hardcode menus or generated application names;
- use `localhost` from an Android device; use `10.0.2.2` for an emulator;
- rebuild the API while an API process is locking its DLL/EXE;
- claim a build, login, migration, or test succeeded without running it.

When a task changes UI, verify both Web and MAUI build paths and preserve
host-specific layouts. When a task changes persistence, verify main DB and
audit DB behavior separately.
