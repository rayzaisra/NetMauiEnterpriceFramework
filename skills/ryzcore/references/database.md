# Database, repositories, and logging

## Database architecture

There are two SQL Server databases per generated application:

1. **Main database** - business/application data.
2. **Audit/log database** - `tb_audit_trail`, `tb_audit_trail_detail`, `tb_log`,
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

There is no branch/cabang column. Do not add `kode_cabang`/`branch_code` back
to the entity, migrations, DTOs, or seeders.

## Base entity contract

Both base classes live in one file:
`__PROJECT__.Domain/Entities/BaseEntity.cs`. There is no separate
`BaseAuditEntity.cs`. Do not create a duplicate class or copy these properties
into individual entities.

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

## Audit and logging flow

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

### Error logging contract (`tb_log`)

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

### Automatic data audit (`tb_audit_trail`)

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

### Automatic API activity log (`tb_log_activity`)

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

## Repository, Unit of Work, and query services

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
