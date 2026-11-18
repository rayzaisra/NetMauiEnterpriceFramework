# Change workflow: analyze, propose, then implement

This gate applies to **every request that means to implement or change
something** (feature, page, endpoint, table, menu, fix, refactor). It does not
apply to pure questions or read-only exploration, which may be answered
directly.

Never treat a request as permission to start editing. Do not touch code until
the user has reviewed the proposal and approved it.

## Step 1 - Understand the request

- Restate the goal, scope, and acceptance criteria in one short block.
- Identify the touchpoint: domain/data, application contract, infrastructure,
  API, Shared UI, Web host, MAUI host, database, menu/metadata.
- If the requirement is ambiguous or would change public behavior/schema,
  stop and ask before designing.

## Step 2 - Map the flow end to end

Describe the flow from the user interface to the database, naming concrete
files and routes you expect to touch:

```text
user action
  -> Razor page/component (Shared/Pages/...)
  -> Shared API service (Shared/Services/...)
  -> API controller (Api/Controllers/...)
  -> use-case service (Application/Infrastructure)
  -> repository / query service
  -> EF DbContext / table / stored procedure
  -> response envelope -> UI update
```

For a data page, also name the list query, dialog component, menu row, and
approval path. For a master table, name the parameter maker/approval card and
stored procedures.

## Step 3 - Inspect the existing implementation

Read, do not guess:

- entity/table and its base class (`BaseEntity`/`BaseAuditEntity`);
- DTOs and existing JSON contracts;
- repository, query service, and use-case service for the closest feature;
- controller route, authorization, and response envelope;
- Razor page/dialog and the canonical User/Group/Role pattern;
- menu rows (`um_menu`, `um_group_menu`) and dynamic screen metadata
  (`tb_screen_menu*`, `tb_parameter_system*`);
- migrations and stored procedures;
- configuration keys and seed data.

## Step 4 - Impact analysis

List explicitly:

- **Files**: files to add and files to modify, one line each with the reason.
- **Database**: new/changed tables and columns, key types, indexes, and the new
  EF migration; never edit an applied migration.
- **Stored procedures**: which ones change or are added (paging query,
  parameter save/approval, screen/report).
- **Menus and permissions**: `um_menu`/`um_group_menu` rows, parameter cards,
  screen metadata; who can access what (admin role `"00"` vs login-only).
- **Audit/log**: whether the change flows through `__PROJECT__DbContext`
  (automatic audit) or raw SQL/stored procedures (not automatic), and where
  errors will be logged (`IErrorLogService` / `tb_log`).
- **Configuration/secrets**: new keys, tokens, or environment values.
- **UI**: Web and MAUI impact, data-page/dialog conventions, design system
  (default/neo), destroyed behavior or routes.
- **Risks and open decisions**: anything that could change existing behavior,
  performance (large tables, paging), approval semantics, or data migration.

## Step 5 - Present the proposal and wait

Present, in the user's language:

1. the goal and scope;
2. the end-to-end flow;
3. the add/change list (file by file);
4. schema/SP/menu/audit impact;
5. risks and open questions.

For UI work, include the recommended page layout/component structure and wait
for approval before editing. Do not implement the design in parallel.

Then wait for explicit approval. Approval may cover the whole proposal or a
subset; implement only what was approved. If the user changes scope, rerun the
impact analysis for the new scope.

## Step 6 - Implement and verify

- Implement only the approved scope, following the canonical patterns.
- Build the affected projects; run the relevant verification.
- Report exactly what changed, what was verified, and what could not be
  verified (build, migration, login, device, process lock).
- Never claim a build, migration, or test succeeded without running it.
