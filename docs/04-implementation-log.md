# SkyRoute — Implementation Log

This log records what was **actually done** for each phase, as executed, versus what was
merely planned. It is a factual record (commands run, files created, decisions made under
real conditions, validation performed) — not a restatement of `docs/03-execution-plan.md`.

---

## Phase 1 — Solution & project scaffolding

**Status:** ✅ Complete · **Commit:** `8e5008b` — "Phase 1: solution and project scaffolding
(backend 5 projects, Angular workspace)"

### What was implemented

**Environment**
- Node was upgraded via `nvm` from the active `22.16.0` to **`22.22.3`** (installed fresh, then
  activated) to satisfy Angular 22's `engines.node` requirement. `node -v` / `npm -v` confirmed
  `v22.22.3` / `10.9.8` after activation.
- Git repository initialised at the repo root (`git init`); this repo had no `.git` before this
  phase.

**Backend — `backend/`**
- `SkyRoute.slnx` (solution file) created and all five projects added to it.
- `SkyRoute.Domain` — class library, `net10.0`, no project references. Empty folders created:
  `Entities/`, `ValueObjects/`, `Enums/`, `Models/`, `Rules/`, `Interfaces/`.
- `SkyRoute.Application` — class library, `net10.0`, references `SkyRoute.Domain`. Empty
  folders created: `Dtos/`, `Services/`, `Abstractions/`.
- `SkyRoute.Infrastructure` — class library, `net10.0`, references `SkyRoute.Domain` and
  `SkyRoute.Application`. Empty folders created: `Providers/`, `Caching/`, `Data/`.
- `SkyRoute.WebApi` — ASP.NET Core Web API, `net10.0`, generated with `-controllers` (controller-
  based, not Minimal APIs), references `SkyRoute.Application` and `SkyRoute.Infrastructure`.
  Empty folders created: `Controllers/`, `Middleware/`. Package added:
  `Microsoft.Extensions.Caching.Memory` (10.0.12) — see "Deviations" below.
- `SkyRoute.Tests` — xUnit test project, `net10.0`, references `SkyRoute.Domain`,
  `SkyRoute.Application`, `SkyRoute.Infrastructure`.
- Template-generated placeholder files were left as-is where the plan did not call for their
  removal (`WeatherForecastController.cs`, `WeatherForecast.cs` in WebApi; `Class1.cs` in
  Application and Infrastructure; `UnitTest1.cs` in Tests) — **except** `SkyRoute.Domain`'s
  `Class1.cs`, which the plan explicitly said to delete, and which was deleted. These
  placeholders carry no logic and do not affect any acceptance criterion; they will be removed
  organically as Phase 2+ adds real files to those projects.

**Frontend — `frontend/skyroute-app/`**
- Scaffolded with `npx @angular/cli@22 new skyroute-app --style=scss --ssr=false --skip-git
  --routing --skip-install --ai-config=none`.
- Dependencies installed separately via `npm install --legacy-peer-deps` (see "Deviations").
- Result: standalone components, zoneless change detection, Vitest as the test runner, routing
  enabled, SCSS styles, no SSR — all Angular 22 defaults, matching `docs/02-revision.md` and
  `docs/03-execution-plan.md`.
- No `features/`, `core/`, or `shared/` folders were created yet — Phase 1 only scaffolds the
  default CLI workspace; the app-specific folder structure is introduced in Phases 2–8 as
  described in the execution plan.

**Root**
- `.gitignore` created, covering Visual Studio (`bin/`, `obj/`, `*.user`, `.vs/`) and Node
  (`node_modules/`, `dist/`, `.angular/`, npm logs), plus IDE/OS noise.
- Initial commit made: 46 files, 10,510 insertions. Verified no `node_modules/`, `bin/`,
  `obj/`, or `dist/` paths were captured by `git status --short` after `.gitignore` was in
  place.

### Decisions made during implementation

1. **Solution file is `SkyRoute.slnx`, not `SkyRoute.sln`.** The .NET 10 SDK (10.0.400)
   generates the new XML solution format by default when running `dotnet new sln`. This is a
   tooling behaviour, not an architectural choice — `dotnet sln`, `dotnet build`, and IDE tooling
   all operate on `.slnx` identically to `.sln`. All subsequent commands and any README
   instructions should reference `backend/SkyRoute.slnx`.
2. **Template placeholder files were kept, not proactively deleted**, except where the plan
   explicitly named a file for removal (`SkyRoute.Domain/Class1.cs`). Deleting
   `WeatherForecastController.cs`/`WeatherForecast.cs`/`UnitTest1.cs`/the other `Class1.cs` files
   now would be pure churn since Phase 2–5 will add real files to those same projects and the
   placeholders will very likely be replaced or removed naturally at that point. This does not
   change any contract, DTO, or business rule from `docs/03-execution-plan.md`.

### Deviations from the plan (environment-level, not architectural)

These three issues were caused by the local machine's network/tooling configuration, not by
anything in `challenge.md`, `docs/02-revision.md`, or `docs/03-execution-plan.md`. Each was
resolved without altering any contract, DTO, folder structure, or business rule defined in the
approved plan.

| # | Issue | Root cause | Resolution |
|---|---|---|---|
| 1 | `dotnet new webapi` failed on first attempt with `NU1301: Unable to load the service index ... proget.dev.rph.int` | A private corporate NuGet feed (`nuget-priv`) registered on this machine is unreachable from this environment (DNS failure) | `dotnet nuget disable source nuget-priv` (reversible; `nuget.org` remained enabled and was the actual source used for every package restored) |
| 2 | The retried `dotnet new webapi` then failed with "Creating this template will make changes to existing files" | The first (failed) invocation had already written partial output before the restore step failed | Re-ran with `--force` to overwrite the partial scaffold cleanly |
| 3 | `npm install` failed with `TypeError: Cannot read properties of null (reading 'edgesOut')` inside npm's `arborist` dependency resolver, reproducible even after `npm cache clean --force` | A known npm 10.9.8 resolver bug triggered by Vitest 4's optional peer dependency graph (`@vitest/browser-playwright`, `canvas`, `jsdom`) | `npm install --legacy-peer-deps` — installed all 377 packages, 0 vulnerabilities. Does not change any dependency version in `package.json`; only changes the resolution algorithm used for this install |

A fourth, non-blocking item: `dotnet add ... package Microsoft.Extensions.Caching.Memory`
succeeded but emitted `warning NU1510: PackageReference ... will not be pruned. This package is
automatically available and does not need to be referenced explicitly.` on every subsequent
build. This is because .NET 10's shared framework already includes this package transitively.
The explicit reference was kept because `docs/03-execution-plan.md` Phase 1, task 11, calls for
it explicitly — removing it is a one-line change to consider in a later cleanup pass, but doing
so now would deviate from the approved task list without being asked. Flagged here for
visibility, not treated as a defect.

### Files added

```
.gitignore
backend/SkyRoute.slnx
backend/SkyRoute.Domain/SkyRoute.Domain.csproj
backend/SkyRoute.Domain/{Entities,ValueObjects,Enums,Models,Rules,Interfaces}/   (empty)
backend/SkyRoute.Application/SkyRoute.Application.csproj
backend/SkyRoute.Application/Class1.cs                                          (template placeholder)
backend/SkyRoute.Application/{Dtos,Services,Abstractions}/                      (empty)
backend/SkyRoute.Infrastructure/SkyRoute.Infrastructure.csproj
backend/SkyRoute.Infrastructure/Class1.cs                                       (template placeholder)
backend/SkyRoute.Infrastructure/{Providers,Caching,Data}/                       (empty)
backend/SkyRoute.WebApi/SkyRoute.WebApi.csproj
backend/SkyRoute.WebApi/Program.cs                                              (template default)
backend/SkyRoute.WebApi/Controllers/WeatherForecastController.cs                (template placeholder)
backend/SkyRoute.WebApi/WeatherForecast.cs                                      (template placeholder)
backend/SkyRoute.WebApi/{Controllers,Middleware}/                               (Middleware empty)
backend/SkyRoute.WebApi/Properties/launchSettings.json
backend/SkyRoute.WebApi/appsettings.json, appsettings.Development.json
backend/SkyRoute.WebApi/SkyRoute.WebApi.http
backend/SkyRoute.Tests/SkyRoute.Tests.csproj
backend/SkyRoute.Tests/UnitTest1.cs                                             (template placeholder)
frontend/skyroute-app/                                                          (full Angular 22 CLI scaffold:
  angular.json, package.json, package-lock.json, tsconfig*.json, .editorconfig,
  .prettierrc, .gitignore, .vscode/, public/favicon.ico,
  src/main.ts, src/index.html, src/styles.scss,
  src/app/app.ts, app.html, app.scss, app.spec.ts, app.config.ts, app.routes.ts)
```

No files outside `backend/`, `frontend/`, and the root `.gitignore` were modified.

### Validation performed

| Check | Command | Result |
|---|---|---|
| Node version | `node -v` | `v22.22.3` ✅ |
| npm version | `npm -v` | `10.9.8` ✅ |
| Backend build | `dotnet build backend/SkyRoute.slnx` | **Build succeeded**, 0 errors, 2 warnings (NU1510, informational, see above) ✅ |
| Frontend build | `npx ng build` (from `frontend/skyroute-app`) | Application bundle generation complete, `dist/skyroute-app` produced ✅ |
| Frontend serve | `npx ng serve --port 4200`, then `Invoke-WebRequest http://localhost:4200` | **HTTP 200** ✅, process stopped cleanly afterward |
| Git state | `git status --short` after `git add -A` | Only source files staged; no `node_modules/`, `bin/`, `obj/`, or `dist/` paths present ✅ |
| Git history | `git log --oneline` | Single commit `8e5008b`, 46 files, 10,510 insertions ✅ |

All Definition of Done criteria from `docs/03-execution-plan.md` Phase 1 are met:
`dotnet build` succeeds across all 5 projects with correct references; `ng serve` renders the
default Angular page at `localhost:4200`; the git repo is initialised with an initial commit.

### Time

Plan estimate: 15 min. Actual: longer than estimated, primarily due to the three environment
issues above (unreachable private NuGet feed, template overwrite conflict, npm resolver bug)
and the interactive-prompt hang on the first `ng new` attempt (Angular's AI-tools selection
prompt, resolved by adding `--ai-config=none` on retry). None of the overrun was caused by the
plan itself being wrong; all of it was local-machine tooling friction, resolved without
architectural changes.

