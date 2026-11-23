# API, authentication, and configuration

## API conventions

- Controllers use `[ApiController]` and `[Route("api/[controller]")]`.
- Authenticated controllers use `[Authorize]`.
- Do not use `[Authorize(Roles = "00")]` for maintenance/business endpoints that
  a non-`00` group may access. Use
  `[RequireMenuAccess(PermissionAction.Action, "MENU-UNIQUE")]`; group `00`
  bypasses it, while other groups need an active `um_group_menu` row and the
  appropriate `um_user_group` flag.
- Reserve `[Authorize(Roles = "00")]` for endpoints that must stay
  super-admin-only regardless of database configuration.
- `[AllowAnonymous]` is limited to authentication/health/bootstrap endpoints.
- Do not accidentally expose dropdowns or lookups that contain admin data.
- Preserve the existing response envelope (`ApiResponse<T>` /
  `BaseGeneralResponse` / `PaginatedResponse<T>` /
  `GeneralListResponseDto<T>`).
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

Permission matrix: `can_read_only` blocks every business action including
Export/View Detail; `can_delete` grants delete; all other business actions
require `can_add_edit`; `Load` is allowed once the menu is mapped. Group `00`
bypasses menu mapping and all flags. Permission changes apply on the next API
request; the UI updates after refresh/navigation.

Typical controller structure (list endpoints are `POST` + `[FromBody]`, see
`backend-api-style.md`):

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ExampleController : ControllerBase
{
    private readonly IExampleService _service;

    public ExampleController(IExampleService service) => _service = service;

    [HttpPost("list")]
    public async Task<IActionResult> GetList([FromBody] DataTableRequestDto request)
    {
        var result = await _service.GetListAsync(request);
        return Ok(ApiResponseFactory.Page(
            result.Data, request, result.TotalCount, "Items retrieved successfully"));
    }
}
```

## Authentication and authorization

- JWT access + refresh tokens; active sessions are checked by middleware.
- Login, refresh, forgot-password, reset-password validation/reset, and
  first-login password change are anonymous endpoints.
- All other API endpoints require authentication unless explicitly reviewed.
- Administrator role code is `"00"`.
- `AuthController` handles login and token lifecycle; do not duplicate auth
  logic in UI services.
- Password reset uses `tb_password_reset_token` and `tb_email_outbox`; tokens
  are stored hashed and are single-use/expiring.
- Sensitive values must never be logged or audited.

## Configuration and secrets

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
generator supplies a random 64-byte Base64 secret. Do not leave the generated
placeholder or another short secret in a running app; it causes `IDX10653`
during token generation. Generate a replacement with 64 random bytes and keep
it only in environment/user secrets or local development settings.
