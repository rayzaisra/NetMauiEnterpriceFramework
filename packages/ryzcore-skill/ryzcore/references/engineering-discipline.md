# Engineering discipline: four principles

These four principles shape **how** you work in RyzCore, not just what
conventions you copy. They exist because the most common failure mode is not
"wrong framework knowledge" but **premature action**: assuming scope, adding
speculative abstractions, and touching code beyond the request.

Apply them together with `change-workflow.md`.

---

## 1. Think before coding

Surface assumptions instead of silently choosing one interpretation.

### Example: "Add a feature to export user data"

**Wrong - assumes everything**

```csharp
[HttpGet("export")]
[Authorize(Roles = "00")]
public async Task<IActionResult> Export()
{
    var users = await _context.um_user_app.ToListAsync();   // all rows, tracked
    var csv = BuildCsv(users, "user_id,full_name,email,password");
    return File(Encoding.UTF8.GetBytes(csv), "text/csv", "users.csv");
}
```

Problems: exports every row (no paging, memory risk), includes `password`,
bypasses the response envelope, invents a file location, and never asks whether
this is a download, a Hangfire job, or a paged API.

**Right - clarify first**

```text
Before implementing, I need to clarify:

1. Scope: all users or a filtered subset? `um_user_app` has soft-deleted rows
   and sensitive columns (`password`, `created_by`).
2. Delivery: browser download, background job via Hangfire + email, or a
   paginated API endpoint?
3. Fields: which columns may be exported? Sensitive values are excluded from
   audit for a reason.
4. Volume: how many rows typically? This decides streaming vs paging.

The framework already has Excel/PDF export through `ExportController` and
Hangfire jobs. The smallest correct change is likely a new export job reusing
that path - confirm before I add anything.
```

### Example: "Make the search faster"

**Wrong - pick silently**

```csharp
// Adds caching + indexes + async batching in one pass, unrequested
public async Task<GeneralListResponseDto<ItemDto>> GetListAsync(DataTableRequestDto request)
{
    return await _cache.GetOrCreateAsync($"items:{request.SearchTerm}", async entry =>
    {
        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
        // ... rewrites the whole query service
    });
}
```

**Right - name the interpretations**

```text
"Faster" could mean:
1. Lower latency for one search   -> index + narrower projection, keep paging
2. Higher throughput              -> AsNoTracking, smaller PageSize max
3. Faster perceived speed         -> progressive UI state, no query change

Current principle in this framework: list queries are paged server-side
(PageSize clamped to 200) and projected directly into the DTO. Which of the
three matters here, and what is the current measured latency?
```

**Rule:** when a request has more than one reasonable reading, list the
readings and ask. Do not implement the most interesting one.

---

## 2. Simplicity first

Write the minimum that satisfies the request. In RyzCore this is a hard rule,
not a preference: no speculative abstractions, no invented layers.

### Example: "Add a function to calculate discount"

**Wrong - over-abstracted**

```csharp
public interface IDiscountStrategy { decimal Calculate(decimal amount); }
public sealed class PercentageDiscountStrategy : IDiscountStrategy { /* ... */ }
public sealed class FixedDiscountStrategy : IDiscountStrategy { /* ... */ }
public sealed class DiscountCalculator
{
    private readonly IDiscountStrategy _strategy;
    private readonly DiscountOptions _options;
    public DiscountCalculator(IDiscountStrategy strategy, IOptions<DiscountOptions> options) { }
    public decimal Apply(decimal amount) { /* 40 lines */ }
}
// plus DI registration, options binding, and a factory
```

**Right - one method**

```csharp
public static decimal CalculateDiscount(decimal amount, decimal percent)
    => amount * (percent / 100m);
```

Add the strategy pattern only when a second discount type actually exists.
If that requirement arrives later, refactor then.

### Example: "Save user preferences to database"

**Wrong - speculative features**

```csharp
public async Task<bool> SaveAsync(
    string userId, Dictionary<string, string> prefs,
    bool merge = true, bool validate = true, bool notify = false,
    CancellationToken cancellationToken = default)
{
    // caching layer, validator pipeline, change notification,
    // none of which were requested
}
```

**Right - just what was asked**

```csharp
public async Task<BaseGeneralResponse> SaveAsync(string userId, string preferencesJson)
{
    var user = await _unitOfWork.UmUserApp.GetActiveByIdAsync(userId);
    if (user is null) return Fail("User not found.");

    user.preferences = preferencesJson;
    user.modifiedBy = _currentUser.UserId;
    user.modifiedDate = DateTime.Now;

    await _unitOfWork.SaveChangesAsync();
    return Success("Preferences saved.");
}
```

Add caching when performance demands it, validation when bad data appears, and
notification when a requirement exists.

### RyzCore-specific simplicity rules

- Do not add a generic repository as the primary contract.
- Do not create a service layer for a one-line controller action.
- Do not add a new `rf_*` table for a value that `rf_default_value` already holds.
- Do not build a page-specific overlay, toolbar, or dialog when the canonical
  one exists.
- Do not add configuration keys nobody reads.

---

## 3. Surgical changes

Touch only the lines the request requires. Never reformat, rename, or
"improve" adjacent code.

### Example: "Fix the bug where empty emails crash the validator"

**Wrong - drive-by refactor**

```diff
- public BaseGeneralResponse Create(UserEditDto dto)
+ public BaseGeneralResponse Create(UserEditDto dto)
  {
+     ArgumentNullException.ThrowIfNull(dto);
      dto.UserId = dto.UserId.Trim();
-     if (!IsValidEmail(dto.Email))
+     if (!IsValidEmail(dto.Email?.Trim()))
          return Fail("Invalid email.");
+
+     if (dto.FullName is { Length: < 3 })
+         return Fail("Full name too short.");
+
+     if (dto.PhoneNumber?.Any(c => !char.IsDigit(c)) == true)
+         return Fail("Phone number must be numeric.");
+
      // ...
  }
```

Problems: adds null guards, name-length rules, and phone validation nobody
asked for. All of it can break existing callers and tests.

**Right - surgical**

```diff
-     if (!IsValidEmail(dto.Email))
+     if (!IsValidEmail(dto.Email?.Trim()))
          return Fail("Invalid email.");
```

Only the reported defect changed.

### Example: "Add logging to the upload function"

**Wrong - reformat everything**

```diff
- private bool Upload(string path, string destination)
+ private bool Upload(string path, string destination)
  {
+     _logger.LogInformation("Starting upload: {Path}", path);
      try
      {
-         using var stream = File.OpenRead(path);
-         var response = _client.Post(destination, stream);
-         return response.IsSuccessStatusCode;
+         using var stream = File.OpenRead(path);
+         using var response = _client.Post(destination, stream);
+         var success = response.IsSuccessStatusCode;
+
+         if (success)
+             _logger.LogInformation("Upload successful: {Path}", path);
+         else
+             _logger.LogError("Upload failed: {Path}, status={Status}",
+                 path, (int)response.StatusCode);
+
+         return success;
      }
-     catch (Exception ex)
+     catch (Exception ex)
      {
-         Console.WriteLine($"Error: {ex.Message}");
+         _logger.LogError(ex, "Upload error: {Path}", path);
          return false;
      }
  }
```

Changing `using var` to `using var` is fine, but do not also rename parameters,
switch to primary constructors, restructure the class, or rewrite the boolean
flow.

### Surgical rules for RyzCore

- Never rename lower_snake_case entity properties "for consistency".
- Never split or merge repository files while fixing a bug.
- Never change the JSON envelope while adding a field.
- Never rewrite a page's layout while fixing one cell's `DataLabel`.
- If a change requires reformatting to be readable, stop and ask.
- `is_active` ordering, `Cancel`/`Save` labels, and `Actions` position are part
  of the contract - do not "polish" them incidentally.

---

## 4. Goal-driven execution

Every task needs a verifiable success criterion **before** code is written.
"I'll review and improve" is not a plan.

### Example: "Fix the authentication system"

**Wrong - vague**

```text
I'll fix authentication by:
1. Reviewing the code
2. Identifying issues
3. Making improvements
4. Testing
```

**Right - verifiable**

```text
"Fix authentication" needs a specific symptom. If the issue is
"users stay logged in after a password change":

1. Reproduce: write a test or manual step that logs in, changes the password,
   and confirms the old refresh token still works.
   Verify: the old token is accepted (bug reproduced).

2. Implement: revoke sessions/refresh tokens on password change through
   the existing `um_user_refresh_token` / `um_user_app_session` path.
   Verify: the old token is rejected, new login works.

3. Edge cases: multiple active sessions, concurrent change, first-login flow.

4. Regression: existing auth tests and Web/MAUI login still pass.

Which symptom are you seeing?
```

### Example: "Add rate limiting to the API"

**Right - incremental and verifiable**

```text
1. Confirm the existing pipeline: `IpRateLimiting:*` is already configured
   and `ApiActivityMiddleware` sits before rate limiting.
   Verify: current config and middleware order read from the code.

2. Add the policy for the one endpoint that needs it.
   Verify: 11 rapid calls -> first 10 succeed, the 11th returns 429.

3. Extend to the remaining endpoints of that controller.
   Verify: existing endpoint tests still pass.

4. Update configuration only if a new key is genuinely required.
   Verify: app starts with the default configuration.

Each step is independently verifiable. Start with step 1?
```

### Example: "Sorting breaks when there are duplicate scores"

**Wrong - fix without reproducing**

```csharp
query = query.OrderByDescending(x => x.Score);
```

**Right - reproduce, then fix**

```csharp
// 1. Reproduce: the current sort is unstable for ties
var page = await _query.GetListAsync(new DataTableRequestDto
{
    SortColumn = "score",
    SortDirection = "desc",
});
// Observe: rows with equal score change order between calls.

// 2. Fix with a deterministic tiebreaker that matches server-side sorting
query = desc
    ? query.OrderByDescending(x => x.Score).ThenBy(x => x.Id)
    : query.OrderBy(x => x.Score).ThenBy(x => x.Id);

// 3. Verify: repeated calls return the same order; paging is stable.
```

### Verification contract

- State the success criterion before coding.
- Prefer a test or a repeatable manual step over "looks right".
- Run the build for the affected projects (`Api`, `Web`, `App`).
- Report exactly what was verified and what was not. Never claim a build,
  migration, login, or test passed without running it.

---

## Anti-patterns summary

| Principle | Anti-pattern | Fix |
|---|---|---|
| Think before coding | Silently assumes scope, fields, delivery format, or which DB table | List assumptions, offer the smallest option, ask |
| Simplicity first | Strategy pattern + DI + options for one calculation | One method until a second case exists |
| Surgical changes | Reformatting, renaming, adding validation while fixing one bug | Change only the lines that fix the issue |
| Goal-driven | "I'll review and improve the code" | "Reproduce bug X -> make it pass -> verify no regression" |

## Why timing matters

The over-engineered examples are not obviously wrong - they follow respectable
design patterns. The defect is **timing**: complexity added before it is needed

- makes the code harder to read and review,
- introduces more bugs than it prevents,
- takes longer to implement and to test,
- and hides the actual change inside noise.

Simple code is easier to review, faster to ship, and can be refactored later
when the second case actually arrives.

**Good code solves today's problem simply, not tomorrow's problem prematurely.**
