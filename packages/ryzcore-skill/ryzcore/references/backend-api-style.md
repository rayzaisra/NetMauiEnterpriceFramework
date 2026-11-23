# Backend API coding style

Applies to `<Project>.Domain`, `<Project>.Application`,
`<Project>.Infrastructure`, and `<Project>.Api`.

## Layer and file placement

```text
Domain/Entities/rf_<name>.cs                 one entity per file, lowercase name
Application/DTOs/<Feature>/<Name>Dto.cs      request/response DTOs
Application/Interfaces/Repositories/IXRepository.cs
Application/Interfaces/Queries/IXQueryService.cs
Application/Interfaces/IXService.cs          use-case service contract
Infrastructure/Repositories/<Area>/XRepository.cs
Infrastructure/Queries/XQueryService.cs
Infrastructure/Services/XService.cs
Infrastructure/UnitOfWork.cs                 IUnitOfWork implementation
Api/Controllers/XController.cs
Api/Helpers/ApiResponseFactory.cs            shared response factory
Api/Program.cs                               DI registration
```

Rules:

- One public interface per file, file name equals the interface name
  (`IUserQueryService.cs`, not `UserQueries.cs`).
- One implementation class per file, file name equals the class name. Never
  combined files such as `AuthRepositories.cs` / `ManagementRepositories.cs`.
- Repository interfaces live in `Application/Interfaces/Repositories`; query
  interfaces in `Application/Interfaces/Queries`; use-case service interfaces
  directly in `Application/Interfaces`. Implementations live in the matching
  `Infrastructure` folder.
- Every new interface/implementation must be registered in `Api/Program.cs` as
  `AddScoped<IInterface, Implementation>()`.
- Repository interfaces are specific, not a public generic `IRepository<T>` as
  the primary contract. Entity keys vary (`string`, `int`, `long`, `Guid`).
- `IUnitOfWork` exposes every repository plus `SaveChangesAsync` and
  `BeginTransactionAsync`; a new repository is added there and constructed
  lazily over the shared DbContext.

## Responsibility split

| Component | Owns | Never does |
|---|---|---|
| Controller | routing, model binding, authorization, mapping to the response envelope | business rules, `DbContext`, `SaveChanges` |
| Use-case service | business rules, validation, orchestration, approval staging, `SaveChangesAsync`, transactions | raw `_context` queries, `DbContext` injection |
| Query service | read projections, joins, search, sort, paging, `GeneralListResponseDto<T>` | writes, `SaveChanges` |
| Repository | tracked entity reads, existence checks, add/update/remove, DB-specific operations | business validation, `SaveChanges`, audit rows, HTTP logic |
| DbContext | entity mapping, centralized audit on `SaveChanges` | — |

A repository returns entities; a query service returns DTOs; a service returns
`BaseGeneralResponse` (writes) or the query result (reads). A controller never
returns an EF entity.

## Repository style

```csharp
public interface IUmUserAppRepository
{
    Task<um_user_app?> GetByIdAsync(string userId);
    Task<um_user_app?> GetActiveByIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAnyAsync(string userId, CancellationToken cancellationToken = default);
    Task AddAsync(um_user_app entity);
    Task UpdateAsync(um_user_app entity);
}
```

```csharp
public class RfDefaultValueRepository : IRfDefaultValueRepository
{
    private readonly __PROJECT__DbContext _context;

    public RfDefaultValueRepository(__PROJECT__DbContext context) => _context = context;

    public Task<rf_default_value?> GetByCodeAsync(
        string code,
        CancellationToken cancellationToken = default)
        => _context.rf_default_value.FirstOrDefaultAsync(
            e => e.def_code == code && e.isDeleted != true,
            cancellationToken);
}
```

- Active-row filter is `isDeleted != true` unless the existing query says
  otherwise.
- Use async EF APIs with `CancellationToken`.
- No `SaveChanges`, no `try/catch` just to log.

## Query service style (paging)

```csharp
public async Task<GeneralListResponseDto<UserListItemDto>> GetListAsync(
    DataTableRequestDto request,
    CancellationToken cancellationToken = default)
{
    var page = Math.Max(1, request.PageNumber);
    var pageSize = request.PageSize <= 0 ? 10 : Math.Min(request.PageSize, 200);

    var query = from u in _context.um_user_app
                where u.isDeleted != true
                select new UserListItemDto { /* projection */ };

    if (!string.IsNullOrWhiteSpace(request.SearchTerm))
    {
        var search = request.SearchTerm.Trim();
        query = query.Where(x => x.UserId.Contains(search));
    }

    var desc = string.Equals(request.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);
    query = (request.SortColumn ?? string.Empty).ToLowerInvariant() switch
    {
        "userid" => desc ? query.OrderByDescending(x => x.UserId) : query.OrderBy(x => x.UserId),
        _ => desc ? query.OrderByDescending(x => x.UserId) : query.OrderBy(x => x.UserId),
    };

    var totalCount = await query.CountAsync(cancellationToken);
    var data = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

    return new GeneralListResponseDto<UserListItemDto> { Data = data, TotalCount = totalCount };
}
```

Hard rules:

- Every list is server-side paged. Never `GetAllAsync()` for a list page and
  never do client-side paging.
- Clamp `PageNumber >= 1` and `PageSize` between 1 and a safe maximum (`200`).
- `SearchTerm` is trimmed and applied server-side; `SortColumn` is matched
  case-insensitively and always has a default branch (sort by key).
- `CountAsync` runs before `Skip/Take` so the total matches the filter.
- Project directly into the list DTO inside the query.

## Use-case service style

```csharp
public async Task<BaseGeneralResponse> CreateAsync(UserEditDto dto)
{
    try
    {
        if (await _unitOfWork.UmUserApp.ExistsAnyAsync(dto.UserId))
        {
            return Fail("User ID already exists.");
        }

        await _unitOfWork.TempUserAppPending.AddAsync(new temp_user_app_pending { /* ... */ });
        await _unitOfWork.SaveChangesAsync();
        return Success("User creation request sent for approval.");
    }
    catch (Exception ex)
    {
        await _errorLog.LogAsync("UserManagement", "ERROR", ex);
        return Fail(ex.Message);
    }
}

private static BaseGeneralResponse Success(string message) => new()
{
    Success = true,
    Code = Const.SUCCESS_CODE,
    Message = message,
};

private static BaseGeneralResponse Fail(string message) => new()
{
    Success = false,
    Code = Const.ERROR_CODE,
    Message = message,
};
```

- Depend on `IUnitOfWork` and query interfaces, never on `DbContext`.
- Commit through `_unitOfWork.SaveChangesAsync()`; use
  `BeginTransactionAsync` only for explicit multi-step transactions.
- A caught exception that is handled is logged via `IErrorLogService`; if it
  cannot be handled, rethrow and let `ApiExceptionMiddleware` log it once.
- Maker-checker writes go to `temp_*_pending` staging, never straight to the
  live table.

## Controller style

List endpoint (paged, `POST` + `[FromBody]`):

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
[RequireMenuAccess(PermissionAction.Load, "MNT-USER-LST")]
public class UserController : ControllerBase
{
    private readonly IUserManagementService _userService;

    public UserController(IUserManagementService userService) => _userService = userService;

    [HttpPost("list")]
    public async Task<IActionResult> GetList([FromBody] DataTableRequestDto request)
    {
        var result = await _userService.GetListAsync(request);
        return Ok(ApiResponseFactory.Page(
            result.Data, request, result.TotalCount, "Users retrieved successfully"));
    }

    [HttpPost]
    [RequireMenuAccess(PermissionAction.AddEdit, "MNT-USER-LST")]
    public async Task<IActionResult> Create([FromBody] UserEditDto dto)
        => ToActionResult(await _userService.CreateAsync(dto));

    [HttpDelete("{userId}")]
    [RequireMenuAccess(PermissionAction.Delete, "MNT-USER-LST")]
    public async Task<IActionResult> Delete(string userId)
        => ToActionResult(await _userService.DeleteAsync(userId));
}
```

Transaction endpoint (create/update/delete):

```csharp
[HttpPost]
public async Task<IActionResult> Create([FromBody] UserEditDto dto)
    => ToActionResult(await _userService.CreateAsync(dto));

private IActionResult ToActionResult(BaseGeneralResponse result)
{
    var response = ApiResponseFactory.FromResult(result);
    return result.Success ? Ok(response) : BadRequest(response);
}
```

- `[Route("api/[controller]")]`; list actions are `[HttpPost("list")]`.
- Authorization is per endpoint when one controller serves mixed audiences.
  Use `[Authorize]` plus `[RequireMenuAccess(action, menuUnique)]`; reserve
  `[Authorize(Roles = "00")]` for genuinely super-admin-only endpoints.
- Group `00` bypasses permission filters. For non-`00` groups, `can_read_only`
  blocks all business actions, `can_delete` grants delete, and every other
  business action requires `can_add_edit`.
- `[AllowAnonymous]` only for login/refresh/forgot/reset/health.
- Single-item reads return `NotFound(ApiResponseFactory.NotFound<T>(...))`.
- Never `return new { message = ... }` and never return a bare DTO.

## Response envelope (default JSON shape)

```jsonc
// success (transaction)
{ "success": true, "message": "...", "code": "00", "data": null, "errors": null, "timestamp": "..." }

// failure
{ "success": false, "message": "...", "code": "99", "data": null,
  "errors": [{ "field": "", "message": "..." }], "timestamp": "..." }
```

```jsonc
// list / paged
{
  "pagination": { "pageNumber": 1, "pageSize": 10, "totalCount": 42, "totalPages": 5 },
  "success": true,
  "message": "Users retrieved successfully",
  "code": "00",
  "data": [ /* items */ ],
  "errors": null,
  "timestamp": "..."
}
```

Types:

```text
ApiResponse<T>            Success, Message, Code, Data, Errors, Timestamp
PaginatedResponse<T>      : ApiResponse<List<T>> + Pagination (PageNumber, PageSize, TotalCount, TotalPages)
PaginationInfo            PageNumber, PageSize, TotalCount, TotalPages (computed)
ApiError                  Field, Message
BaseGeneralResponse       Success, Code, Message   (service-level result for writes)
GeneralListResponseDto<T> Data, TotalCount         (query-service result, internal to the API)
DataTableRequestDto       PageNumber=1, PageSize=10, SearchTerm, SortColumn, SortDirection="asc"
```

Codes come from `Domain.Enums.Const`: `SUCCESS_CODE = "00"`,
`DATA_NOT_FOUND_CODE = "98"`, `ERROR_CODE = "99"`.

Always build responses through `ApiResponseFactory`
(`Success`, `Page`, `NotFound`, `Error`, `FromResult`). Do not instantiate the
envelopes ad hoc, do not rename the properties, and keep the JSON casing
unchanged (default System.Text.Json serialization; do not add a custom naming
policy).

## Endpoint checklist

1. Service interface in `Application/Interfaces`, implementation in
   `Infrastructure/Services`, registered in `Program.cs`.
2. Repository/query interface + implementation in separate files; repository
   added to `IUnitOfWork`.
3. Controller action with correct `[Authorize]` and envelope.
4. List endpoints accept `DataTableRequestDto` and return
   `ApiResponseFactory.Page(...)`.
5. Write endpoints return `ApiResponseFactory.FromResult(...)` with 200/400.
6. Build API and Web; the Web client deserializes `ApiResponse<T>` /
   `PaginatedResponse<T>`, so a shape change breaks it.
