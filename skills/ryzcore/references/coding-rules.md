# Coding rules

Non-negotiable rules and C#/Razor conventions for this solution.

## Non-negotiable rules

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
- Never commit `bin/`, `obj/`, generated APKs, database files, logs, or
  captured bug reports.
- This application uses an admin group code of `"00"`. Do not replace it with
  `"Admin"` or a display name.

## C# and Razor conventions

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
- Put repository interfaces in `Application/Interfaces/Repositories`, query
  interfaces in `Application/Interfaces/Queries`, and use-case service
  interfaces in `Application/Interfaces`. Implementations go to
  `Infrastructure/Repositories`, `Infrastructure/Queries`, and
  `Infrastructure/Services`. Register each pair in `Api/Program.cs`.
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
