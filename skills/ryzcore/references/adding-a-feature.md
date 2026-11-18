# Adding a feature

Before running this checklist, complete the review gate in `change-workflow.md`:
analyze the request, map the end-to-end flow, inspect the affected code and
database objects, present the impact analysis and proposed changes, and wait
for user approval.

## Expected order

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

## Build and verification

Generated application:

```powershell
dotnet build <Project>.sln
dotnet run --project <Project>.Api
dotnet run --project <Project>.Web
```

MAUI Windows:

```powershell
dotnet build <Project>.App -f net10.0-windows10.0.19041.0
```

MAUI Android debug deployment uses the active Android emulator/device. Do not
use generated `bin/obj`, `AppPackages`, or test artifacts as source.

Standalone Debug APK:

```powershell
dotnet publish <Project>.App.csproj `
  -f net10.0-android `
  -c Debug `
  -p:AndroidPackageFormats=apk `
  -p:EmbedAssembliesIntoApk=true `
  -p:AndroidEnableProfiledAot=false
```

## Verification expectations

- Run the same validation the CI would run: build the affected projects and
  fix type/lint errors before finishing.
- For schema changes, apply the new migration in a disposable database and
  confirm the main and audit databases behave separately.
- For UI changes, verify both Web and MAUI build paths.
- Do not claim a build, login, migration, or test succeeded without running it.
  If blocked by a device, database, or process lock, say so explicitly.
