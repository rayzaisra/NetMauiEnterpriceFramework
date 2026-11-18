---
name: ryzcore
description: Build and change features in a RyzCore-generated .NET solution or in the RyzCore framework template (.NET 10, Blazor WebAssembly + MudBlazor 9.x, MAUI Blazor Hybrid, EF Core 10 + SQL Server, maker-checker approval, database-driven menus). Use when adding or modifying entities, migrations, repositories, query services, use-case services, API controllers, data pages, dialogs, menus, notifications, or dashboards.
---

# RyzCore

RyzCore is an enterprise .NET 10 framework generator (`create-ryzcore`). Its
applications use Blazor WebAssembly, MAUI Blazor Hybrid, MudBlazor, EF Core,
SQL Server, a separate audit database, maker-checker approval, and
database-driven menus. Follow the conventions in this skill instead of generic
.NET patterns - whether you work on a generated application or on the RyzCore
template itself.

## Working method: analyze, propose, then implement

For **every request that means to implement or change something**, do not edit
code first. Run this gate and wait for user approval:

1. **Analyze** - restate the goal, scope, acceptance criteria, and the layers
   involved (Domain, Application, Infrastructure, API, Shared, Web, MAUI,
   database, menu).
2. **Map the flow** - user action -> Razor page/component -> Shared API service
   -> controller -> use-case service -> repository/query service -> EF
   DbContext -> table/stored procedure. Name concrete files and routes.
3. **Inspect** - read the closest existing implementation and search the real
   code: entities, DTOs, services, controllers, pages, menus, migrations, and
   stored procedures. Never guess.
4. **Impact analysis** - list files to add/modify, schema changes and the new
   migration, tables/columns, stored procedures, menu and permission rows,
   audit/log behavior, configuration/secrets, Web/MAUI impact, plus risks and
   open decisions.
5. **Propose for review** - present the flow and the planned add/change list
   (file by file) and wait for explicit approval. Include the recommended UI
   layout for UI work.
6. **Implement only what was approved**, then build and verify. If the user
   changes scope, redo the impact analysis for the new scope.

"Analyze" is not permission to edit. Do not silently implement alternative
designs. Ask first when the request would change public behavior, schema, or
approval semantics.

## Solution map

```text
<Project>.Domain          entities, enums, constants
<Project>.Application     DTOs, interfaces, use-case contracts
<Project>.Infrastructure  EF Core, repositories, query services, jobs
<Project>.Api             controllers, auth, middleware, SignalR, Hangfire
<Project>.Shared          Razor UI, client API services, layouts
<Project>.Web             Blazor WebAssembly host
<Project>.App             MAUI Blazor Hybrid host
database/                 scripts and stored procedures
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

Never make Shared depend on Web/App. No DbContext or EF Core in Domain/Shared.

## Adding a feature: expected order

1. Find the closest existing feature and mirror its entity, DTO,
   repository/query/service, controller, and UI pattern.
2. Add the entity/table mapping and a new EF migration. Never edit an applied
   migration. Main and audit migrations live under
   `Infrastructure/Migrations/Main` and `Infrastructure/Migrations/Audit`.
3. Add request/response DTOs without changing existing JSON shapes.
4. Add one specific repository interface (Application) and one implementation
   (Infrastructure), each in its own file named after the type.
5. Add a query service for list/search/sort/join/paging.
6. Add the use-case service, then register it in API DI.
7. Add or update the controller with explicit authorization and the standard
   response envelope.
8. Add the Shared API client service and Razor page/component.
9. Add menus through the seeder/database process (`um_menu`, `um_group_menu`,
   `tb_screen_menu*`), never in the layout.
10. Add or update a focused test where the project supports it.
11. Build API, Web, and MAUI (`dotnet build <Project>.sln`).
12. Never commit databases, build output, secrets, or APK files.

## Backend layering (non-negotiable)

```text
Controller   routing, model binding, authorization, response envelope only
Service      business logic, validation, orchestration, SaveChangesAsync, approvals
Query svc    read projections, joins, search, sort, paging (returns DTOs)
Repository   database access only, returns entities, never SaveChanges
DbContext    mapping + centralized audit
```

Interface files: `Application/Interfaces/Repositories/IXRepository.cs`,
`Application/Interfaces/Queries/IXQueryService.cs`, and use-case contracts in
`Application/Interfaces/`. Implementations: `Infrastructure/Repositories`,
`Infrastructure/Queries`, `Infrastructure/Services` - one public class per file,
file name equals the type name. Register every new pair in `Api/Program.cs` and
add repositories to `IUnitOfWork`.

API contract:

- Transactions return `ApiResponse<T>`; lists return `PaginatedResponse<T>`
  with `pagination { pageNumber, pageSize, totalCount, totalPages }`.
- Service-level write results use `BaseGeneralResponse`; query services return
  `GeneralListResponseDto<T>` (Data + TotalCount).
- Build responses only through `ApiResponseFactory`; codes are
  `Const.SUCCESS_CODE = "00"`, `DATA_NOT_FOUND_CODE = "98"`,
  `ERROR_CODE = "99"`. Never return EF entities or ad-hoc `{ message }` objects.
- All list endpoints are `POST` with `[FromBody] DataTableRequestDto` and are
  server-side paged (`PageSize` clamped to 200, `SearchTerm`, `SortColumn`,
  `SortDirection` handled server-side). No `GetAllAsync` for list pages.

## Hard rules

- Repository = database access. Service = business logic. Query service =
  read projections, joins, search, sort, paging.
- A repository never calls `SaveChanges`/`SaveChangesAsync`. Commit through
  `IUnitOfWork.SaveChangesAsync` in the use-case service.
- One public class per file, file named after the class, for entities,
  repository interfaces/implementations, query services, and use-case services.
  Never create combined files such as `AuthRepositories.cs`.
- Do not use `DbContext` directly in Razor components; call injected
  application/API services.
- Entity property names stay lower_snake_case to match SQL columns. Never
  rename them to PascalCase.
- Keep audit centralized in `__PROJECT__DbContext.SaveChanges`. Do not write
  `tb_audit_trail` rows manually for normal CRUD. Raw SQL and stored procedures
  are not audited automatically.
- Never swallow exceptions. If a service handles one, log via
  `IErrorLogService.LogAsync`; otherwise rethrow and let
  `ApiExceptionMiddleware` log it once. Do not add try/catch logging to every
  repository.
- API responses use the established envelope: `ApiResponse<T>` and
  `BaseGeneralResponse` for transactions, `PaginatedResponse<T>` /
  `GeneralListResponseDto<T>` for lists. Never return EF entities or ad-hoc
  `{ message }` payloads.
- Authorization is per endpoint: admin endpoints use `[Authorize(Roles = "00")]`,
  general endpoints require login, and `[AllowAnonymous]` only for
  authentication/health/bootstrap endpoints.
- Do not add `tb_log_activity` writes manually; `ApiActivityMiddleware` covers
  every controller automatically.
- Menus are database-driven. Never hardcode menu items into Web/MAUI layouts.
- New schema changes require a new migration; never edit an applied migration.
- Preserve public behavior unless the task explicitly asks to change it:
  routes, DTO names, JSON shape, messages, authorization, database schema, and
  transaction boundaries.
- Do not claim a build, migration, or test succeeded unless it was actually run.

## Data pages

Every CRUD/data page copies the canonical User/Group/Role maintenance shell:
`MudBreadcrumbs` and page-heading outside the card, `MudPaper Class="dashboard-card"`,
server-side `MudTable`, search in `ToolBarContent`, sortable headers with
`MudTableSortLabel`, `Actions` column on the left, selection checkbox first
only for real bulk actions, `DataLabel` on every cell, empty state, loading
state, and pager.

Add/Edit dialogs: `MudDialog` with `TitleContent`/`DialogContent`/`DialogActions`,
`MudForm` + `MudGrid Spacing="2"`, all inputs outlined. Text/select/number/date
are `xs="12" sm="6"` (two per row), a textarea is `xs="12"`, `MudSwitch` is
`xs="12"` and always after all non-boolean inputs. Cancel before Save; Save is
the rightmost primary action; disable Save while saving.

## Menus and parameters

- A **master/reference table** is named `rf_<domain>` and inherits `BaseEntity`.
  Its UI is parameter maintenance: register the card in
  `ParameterSeeder.Cards`, the fields in `ParameterSeeder.SeedFieldsAsync`, and
  the table name in `ParameterEngineService.AllowedTables` plus both parameter
  stored procedures. Do not build a dedicated CRUD page, controller, service,
  or menu for it.
- `um_menu` (identity/url/parent/icon/order/component type/level) and
  `um_group_menu` (group mapping) drive the sidebar through query services.
  Static pages use `menu_url` + full `component_type`; dynamic screens use
  `tb_screen_menu`/`tb_screen_menu_child` with `component_type = "DynamicScreen"`.
- `DynamicComponentResolver` maps `DynamicScreen`, `DynamicReport`,
  `DynamicUrlView` to shared pages. Screen/report/parameter data is SQL metadata.
- `DynamicEngineService` and `ParameterEngineService` intentionally use raw
  SQL/stored procedures. Treat them as engine boundaries; do not refactor them
  into generic repositories.
- Parameter maker writes to `tb_parameter_sytem_temporary` staging; approval
  applies it to the real table. Keep `rf_*` tables registered in
  `tb_parameter_system` metadata and the engine allowlists.

## Build and verify

```powershell
dotnet build <Project>.sln
dotnet run --project <Project>.Api
dotnet run --project <Project>.Web
dotnet build <Project>.App -f net10.0-windows10.0.19041.0
```

Android debug deployment uses the running emulator with
`https://10.0.2.2:7143` as the API base URL (host API listening on
`0.0.0.0:7143`). A standalone Debug APK requires
`-p:EmbedAssembliesIntoApk=true`.

## Reference files

Read only what the task needs:

- `references/change-workflow.md` - mandatory analyze/impact/proposal/approval
  gate before any implementation.
- `references/coding-rules.md` - non-negotiable rules and C#/Razor conventions.
- `references/backend-api-style.md` - interface/implementation placement,
  repository vs service vs query-service code style, controller style, and the
  default JSON envelope for transaction and paged list responses.
- `references/database.md` - entities, base classes, repositories/UoW/query
  services, audit and logging flows.
- `references/api.md` - controllers, envelopes, authorization, JWT,
  configuration, and secrets.
- `references/ui-pages.md` - data-page/dialog/approval/screen-menu standards,
  dynamic menus, Web vs MAUI hosts.
- `references/menus-and-parameters.md` - master-table (`rf_*`) recipe,
  parameter maintenance maker/approval registration, and menu creation.
- `references/adding-a-feature.md` - full feature checklist and verification.
- `references/guardrails.md` - complete never-do list and pre-edit checklist.
