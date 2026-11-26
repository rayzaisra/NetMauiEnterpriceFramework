# Guardrails

## Before editing

- Complete the gate in `change-workflow.md`: analyze, map the end-to-end flow,
  inspect the code, present the impact analysis, and wait for user approval.
- Apply the four principles in `engineering-discipline.md`: think before
  coding, simplicity first, surgical changes, goal-driven execution.
- State the verifiable success criterion for the task before writing code.
- Search for the existing implementation and mirror its conventions.
- Identify whether the task is Domain, Application, Infrastructure, API,
  Shared, Web, or MAUI; do not move logic across boundaries casually.
- Check authorization and multi-database implications.
- Check whether the table is soft-deleted and whether approval fields apply.
- Check whether the operation must use a transaction.

## Never

- start editing an implementation request before presenting the flow, impact
  analysis, and proposed changes and receiving user approval;
- pick one interpretation of an ambiguous request without naming the
  alternatives and asking;
- add abstractions, options, caches, or layers for a requirement that does not
  exist yet (no strategy pattern for one calculation, no generic repository,
  no unused configuration keys);
- reformat, rename, or restructure code outside the lines the request requires;
- add validation, guards, or "improvements" nobody asked for while fixing a bug;
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
- create a dedicated CRUD page, controller, or menu for an `rf_*` master table
  instead of registering it in parameter maintenance;
- name a master/reference table without the `rf_` prefix;
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
