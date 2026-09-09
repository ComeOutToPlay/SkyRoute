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

---

## Phase 2 — Domain models and rules

**Status:** ✅ Complete · **Commit:** `8f08365` — "Phase 2: Domain models and rules (Airport,
CabinClass, FlightSearchCriteria.IsInternational, FlightOffer, Booking, Passenger,
DocumentValidator, IFlightProvider) + Phase 1 implementation log"

### What was implemented

All 9 files listed in `docs/03-execution-plan.md` Phase 2 were created in
`backend/SkyRoute.Domain/`, exactly as specified, with no additions and no omissions:

- `ValueObjects/Airport.cs` — `sealed record Airport(string Code, string City, string Country,
  string CountryCode)`.
- `Enums/CabinClass.cs` — `Economy, Business, First`.
- `Enums/BookingStatus.cs` — `Confirmed` only, kept as an enum for future extensibility
  (e.g. `Cancelled`) per the plan's own note.
- `Models/FlightSearchCriteria.cs` — `Origin`/`Destination` as full `Airport` value objects,
  `DepartureDate` (`DateOnly`), `PassengerCount`, `CabinClass`, and the derived
  `IsInternational => Origin.CountryCode != Destination.CountryCode` property, copied verbatim
  from the plan's code sample.
- `Models/FlightOffer.cs` — provider, flight number, origin/destination as plain airport code
  strings (matching the `02-revision.md` JSON contract, where offers carry codes, not full
  `Airport` objects), `DepartureTime`/`ArrivalTime` as unspecified-kind `DateTime` (no UTC
  offset — airport-local), `DurationMinutes`, `CabinClass`, `PricePerPassenger` (`decimal`).
  **Confirmed: no `IsInternational` property here**, per the plan's explicit instruction.
- `Entities/Passenger.cs` — `FullName`, `Email`, `DocumentNumber`.
- `Entities/Booking.cs` — `Reference`, `FlightOffer` (embedded snapshot, not a live reference),
  `Passengers`, `TotalPrice`, `Currency`, `Status`, `CreatedAtUtc`.
- `Rules/DocumentValidator.cs` — copied verbatim from the plan's code sample: `IsValidPassport`
  (`^[A-Z]{1,2}[0-9]{6,7}$`), `IsValidNationalId` (`^[0-9]{9}$`), `IsValid(documentNumber,
  isInternational)` dispatcher, `Normalize` (trim + uppercase) helper. Placed in
  `Domain/Rules/`, **not** `Application/Services/`, per the confirmed architectural decision.
- `Interfaces/IFlightProvider.cs` — `ProviderName` + `SearchAsync(FlightSearchCriteria,
  CancellationToken) : Task<IReadOnlyList<FlightOffer>>`, matching the architecture baseline
  section of the execution plan exactly.

No files were created beyond this list. `SkyRoute.Domain.csproj` was not modified — it still
has zero `ProjectReference` entries, confirming Domain remains dependency-free.

### Decisions made during implementation

None beyond what the plan already specified — every file's shape, field names, and the
`DocumentValidator`/`FlightSearchCriteria` code bodies were taken directly from
`docs/03-execution-plan.md`. No ambiguity or conflict with `challenge.md` or
`docs/02-revision.md` was encountered.

One forward-looking observation, **not acted on** in this phase: Phase 8 will need to locate a
booking's `flightId` inside a cached search's offers, which implies some kind of `Id` on the
wire-level `FlightOfferDto`. The execution plan does not give `FlightOffer` (the Domain model)
an `Id` field, which is consistent with the same reasoning already applied to
`IsInternational` — identity/wire-format concerns belong to the Application DTO layer (Phase 3),
not to the Domain model. No `Id` field was added to `FlightOffer` in this phase; this is noted
here so it is not mistaken for an oversight when Phase 3/8 introduce `FlightOfferDto.Id`.

### Deviations from the plan

None. All 9 files match the plan's specification exactly.

### Files added

```
backend/SkyRoute.Domain/ValueObjects/Airport.cs
backend/SkyRoute.Domain/Enums/CabinClass.cs
backend/SkyRoute.Domain/Enums/BookingStatus.cs
backend/SkyRoute.Domain/Models/FlightSearchCriteria.cs
backend/SkyRoute.Domain/Models/FlightOffer.cs
backend/SkyRoute.Domain/Entities/Passenger.cs
backend/SkyRoute.Domain/Entities/Booking.cs
backend/SkyRoute.Domain/Rules/DocumentValidator.cs
backend/SkyRoute.Domain/Interfaces/IFlightProvider.cs
backend/SkyRoute.Tests/Domain/DocumentValidatorTests.cs        (new — see Validation below)
backend/SkyRoute.Tests/Domain/FlightSearchCriteriaTests.cs      (new — see Validation below)
backend/SkyRoute.Tests/UnitTest1.cs                             (deleted — template placeholder,
                                                                  superseded by real tests above)
```

No files outside `backend/SkyRoute.Domain/` and `backend/SkyRoute.Tests/` were modified.

### Validation performed

The plan lists two test areas for this phase ("written now or in Phase 9 — logic is simple
enough to defer without risk"). They were written now, immediately alongside the code, rather
than deferred, since the milestone-testing instruction for this implementation session calls
for running relevant tests after each major milestone:

- `DocumentValidatorTests.cs` — 6 `[Theory]`/`[Fact]` methods, 13 total cases: passport accept
  (`X1234567`, `AB123456`, lower-case input normalized), passport reject (national-id-shaped,
  empty, null), national ID accept (`123456789`, with surrounding whitespace trimmed),
  national ID reject (passport-shaped, empty, null), plus two `IsValid(...)` dispatch tests
  confirming it routes to the correct rule based on `isInternational`. This exceeds the plan's
  stated minimum of 4 cases.
- `FlightSearchCriteriaTests.cs` — 2 `[Fact]` methods: same country code (JFK↔LAX, both `US`)
  → `IsInternational == false`; different country codes (JFK↔LHR, `US` vs `GB`) →
  `IsInternational == true`.

| Check | Command | Result |
|---|---|---|
| Backend build | `dotnet build backend/SkyRoute.slnx` | **Build succeeded**, 0 errors, 2 warnings (pre-existing NU1510, unrelated to this phase) ✅ |
| Domain isolation | Inspected `SkyRoute.Domain.csproj` | 0 `ProjectReference` entries — confirmed dependency-free ✅ |
| Unit tests | `dotnet test backend/SkyRoute.slnx` | **Passed! 15/15**, 0 failed, 117 ms ✅ |

All Definition of Done criteria from `docs/03-execution-plan.md` Phase 2 are met:
`SkyRoute.Domain` compiles with zero dependencies on any other project in the solution;
`DocumentValidator` and `IsInternational` are callable and correct in isolation, verified by a
real xUnit test run rather than a scratch test.

### Time

Plan estimate: 20 min. Actual: on par with the estimate — no environment issues were
encountered in this phase (Phase 1 had already resolved the NuGet/npm friction), and every file
was a direct transcription of the plan's specification.

---

## Phase 3 — Application layer and DTOs

**Status:** ✅ Complete · **Commit:** `b5ae0d0` — "Phase 3: Application layer and DTOs (7 DTOs,
IAirportCatalog, ISearchOfferCache, IBookingStore, FlightSearchService/BookingService stubs,
3 exception types)"

### What was implemented

All 15 files listed in `docs/03-execution-plan.md` Phase 3 were created in
`backend/SkyRoute.Application/`:

- **7 DTOs** in `Dtos/`: `AirportDto`, `FlightSearchRequestDto`, `FlightOfferDto` (id, provider,
  flightNumber, origin, destination, departureTime, arrivalTime, durationMinutes, cabinClass,
  pricePerPassenger, totalPrice), `SearchResponseDto` (searchId, passengerCount, currency,
  **isInternational**, flights), `PassengerDto`, `BookingRequestDto` (searchId, flightId,
  passengers — no price, no route data, no isInternational flag), `BookingResponseDto`
  (bookingReference, status, flightSummary, pricePerPassenger, passengerCount, totalPrice,
  currency). Field names and shapes match the JSON contracts in `docs/02-revision.md`
  §"Endpoints" exactly.
- **3 abstractions** in `Abstractions/`: `IAirportCatalog` (`FindByCode`, `GetAll`),
  `ISearchOfferCache` (`Store`, `Get`, plus the `CachedSearch` record bundling criteria +
  offers), `IBookingStore` (`Save`, `FindByReference`) — signatures copied from the plan.
- **2 service stubs** in `Services/`: `FlightSearchService.SearchAsync` and
  `BookingService.BookAsync`, both throwing `NotImplementedException` with a message pointing
  to the phase where each is completed (5 and 8 respectively), per the plan's explicit
  instruction to stub these now and implement them later.
- **3 exception types** in `Exceptions/`: `OfferExpiredException` (carries `SearchId`),
  `FlightNotFoundException` (carries `FlightId`), `ValidationException` (carries a
  field→errors dictionary, plus a convenience single-field constructor) — covering the three
  400/404/409 failure modes the plan names.

`SkyRoute.Application.csproj` was not modified beyond what Phase 1 already set up — it still
has a single `ProjectReference` to `SkyRoute.Domain` only.

### Decisions made during implementation

Two interpretation points, neither of which required inventing a new architectural decision —
both are documented inline in the source and reasoned from what was already approved:

1. **`ValidationException` is the project's own type, not FluentValidation.** The plan's file
   list allows `Exceptions/ValidationException.cs (or reuse FluentValidation's if adopted)`.
   Before writing it, checked whether FluentValidation had been adopted anywhere: it is not
   referenced in any `.csproj` in the solution, and it is not part of the approved architecture
   baseline in `docs/02-revision.md` or `docs/03-execution-plan.md` (it only appears in the
   superseded `docs/01-implementation-plan.md`). Since it was never adopted, the plan's own
   fallback applies: a project-owned `ValidationException` was written instead, carrying a
   `field → string[]` error dictionary so the WebApi layer (Phase 5) can map it to a
   ProblemDetails body with per-field errors.
2. **`BookingResponseDto.FlightSummary` reuses `FlightOfferDto` rather than a new type.** The
   plan's Phase 3 file list does not include a separate "flight summary" DTO, and
   `FlightOfferDto` already carries exactly what `challenge.md` §3.3 asks the booking screen to
   show (route, provider, times, cabin class) plus the pricing fields. Introducing a second,
   near-identical DTO would have been an unapproved addition; reusing the existing one keeps
   the DTO surface exactly as small as the plan specifies.

No other decisions were made. No ambiguity or conflict with `challenge.md` or
`docs/02-revision.md` blocked this phase.

### Deviations from the plan

None. All 15 files match the plan's specification; the two interpretation points above are
applications of the plan's own stated fallbacks, not deviations from it.

### Files added

```
backend/SkyRoute.Application/Dtos/AirportDto.cs
backend/SkyRoute.Application/Dtos/FlightSearchRequestDto.cs
backend/SkyRoute.Application/Dtos/FlightOfferDto.cs
backend/SkyRoute.Application/Dtos/SearchResponseDto.cs
backend/SkyRoute.Application/Dtos/PassengerDto.cs
backend/SkyRoute.Application/Dtos/BookingRequestDto.cs
backend/SkyRoute.Application/Dtos/BookingResponseDto.cs
backend/SkyRoute.Application/Abstractions/IAirportCatalog.cs
backend/SkyRoute.Application/Abstractions/ISearchOfferCache.cs
backend/SkyRoute.Application/Abstractions/IBookingStore.cs
backend/SkyRoute.Application/Services/FlightSearchService.cs   (stub)
backend/SkyRoute.Application/Services/BookingService.cs         (stub)
backend/SkyRoute.Application/Exceptions/OfferExpiredException.cs
backend/SkyRoute.Application/Exceptions/FlightNotFoundException.cs
backend/SkyRoute.Application/Exceptions/ValidationException.cs
backend/SkyRoute.Application/Class1.cs                          (deleted — template placeholder,
                                                                   superseded by real files above)
```

No files outside `backend/SkyRoute.Application/` were modified.

### Validation performed

The plan states "Relevant tests: none new in this phase (DTOs are data holders); orchestration
logic is tested once implemented, in Phases 5 and 8." No new tests were written, matching that
instruction. The existing Phase 2 test suite was re-run to confirm nothing was broken:

| Check | Command | Result |
|---|---|---|
| Backend build | `dotnet build backend/SkyRoute.slnx` | **Build succeeded**, 0 errors, 2 warnings (pre-existing NU1510, unrelated to this phase) ✅ |
| Application dependency check | Inspected `SkyRoute.Application.csproj` | Single `ProjectReference` → `SkyRoute.Domain` only — confirmed ✅ |
| Regression check | `dotnet test backend/SkyRoute.slnx` | **Passed! 15/15**, 0 failed, 93 ms — unchanged from Phase 2 ✅ |

All Definition of Done criteria from `docs/03-execution-plan.md` Phase 3 are met:
`SkyRoute.Application` compiles, referencing only `SkyRoute.Domain`; every DTO field matches
the JSON contracts in `docs/02-revision.md`; `isInternational` is present on
`SearchResponseDto` and absent from `FlightOfferDto`.

### Time

Plan estimate: 20 min. Actual: on par with the estimate — no environment issues in this phase;
the only time spent beyond direct transcription was verifying the FluentValidation-adoption
question before writing `ValidationException`.

---

## Phase 4 — Flight providers and pricing

**Status:** ✅ Complete · **Commit:** `59b5697` — "Phase 4: Flight providers and pricing
(AirportCatalog, RouteFareTable, GlobalAirProvider, BudgetWingsProvider, MemoryOfferCache,
Infrastructure DI)"

### What was implemented

All 6 files listed in `docs/03-execution-plan.md` Phase 4 were created in
`backend/SkyRoute.Infrastructure/`:

- `Data/AirportCatalog.cs` — implements `IAirportCatalog` with the exact 6-airport, 4-country
  hardcoded list from `docs/02-revision.md` (JFK, LAX, ORD — US; LHR — GB; CDG — FR; FCO — IT).
- `Providers/RouteFareTable.cs` — internal, shared base-fare/duration/coverage lookup keyed by
  airport-code pair (order-independent). Encodes: BudgetWings never covers First Class or
  long-haul routes; `ORD↔FCO` is covered by neither provider (the deliberately uncovered pair
  that makes the empty state reachable). Also exposes `CabinMultiplier` (Economy 1.0 / Business
  2.5 / First 4.0) and `RoundAwayFromZero` (see Decisions below).
- `Providers/GlobalAirProvider.cs` — implements `IFlightProvider`. `base = routeFare ×
  cabinMultiplier`; `pricePerPassenger = RouteFareTable.RoundAwayFromZero(base × 1.15m)`;
  `Task.Delay(400, ct)` simulated latency; 2 offers per search with deterministic flight
  numbers/times seeded from `HashCode.Combine(origin, destination, date, cabinClass,
  providerName)`.
- `Providers/BudgetWingsProvider.cs` — implements `IFlightProvider`. Same base computation;
  `pricePerPassenger = Max(RoundAwayFromZero(base × 0.90m), 29.99m)`; returns `[]` for First
  Class or long-haul routes before even computing a price; same latency and determinism
  approach as GlobalAir.
- `Caching/MemoryOfferCache.cs` — implements `ISearchOfferCache` over `IMemoryCache`, 10-minute
  TTL, keyed by `"search-offers:{searchId}"`.
- `DependencyInjection.cs` — `AddInfrastructure(this IServiceCollection)` registers
  `IAirportCatalog`, `ISearchOfferCache`, and both `IFlightProvider` implementations as
  singletons, plus `AddMemoryCache()`. **`IBookingStore` is intentionally not registered yet**
  (see Decisions below).

Two NuGet packages were added to `SkyRoute.Infrastructure.csproj`:
`Microsoft.Extensions.Caching.Memory` and `Microsoft.Extensions.DependencyInjection.Abstractions`
— required because Infrastructure is a plain class library (`Microsoft.NET.Sdk`), not a Web SDK
project, so it does not inherit `IMemoryCache`/`IServiceCollection` from a shared framework the
way `SkyRoute.WebApi` does. Mechanical requirement of already-approved tasks, not a new
architectural decision.

### Decisions made during implementation

Three points required a stop-and-ask rather than a silent judgment call, per this session's
explicit instruction to report ambiguity instead of inventing architecture:

1. **`RouteFareTable` base-fare and duration values are invented mock data.** No document
   (`challenge.md`, `docs/02-revision.md`, `docs/03-execution-plan.md`) specifies concrete
   dollar amounts or durations per airport pair — only the pricing *formulas* and the
   requirement that mocks be "realistic" (`challenge.md` §2). Proposed values (180–420 USD by
   distance, with `JFK↔ORD = 30.00` deliberately low so BudgetWings' $29.99 floor actually
   engages) were presented to the user before writing any code and approved as-is. To be
   documented as a "mock data assumption" in the README (Phase 10).
2. **A synthetic away-from-zero rounding test, not an end-to-end midpoint case.** The plan
   requires testing "an away-from-zero midpoint case (e.g. a base fare that rounds differently
   under banker's vs away-from-zero rounding)." Mathematically verified that none of the 14
   approved `RouteFareTable` values, combined with the three cabin multipliers (1.0/2.5/4.0),
   produce a true midpoint (a value ending in exactly `...X5` with an even preceding digit) —
   any integer-dollar base fare times those multipliers times 1.15 never lands on one. Two
   alternatives (adding a 7th airport just for this test; changing an already-approved fare)
   were proposed and rejected by the user in favor of a third: extract the rounding call into
   an `internal static RouteFareTable.RoundAwayFromZero(decimal)` method, unit-tested directly
   with synthetic values (`2.345m → 2.35m`, `1.005m → 1.01m`) independent of the real route
   data. This required adding `<InternalsVisibleTo Include="SkyRoute.Tests" />` to
   `SkyRoute.Infrastructure.csproj` — the standard .NET mechanism for testing `internal` types,
   not a new architectural layer. A test with a misleading name
   (`SearchAsync_RoundsAwayFromZero_NotBankersRounding`, which did not actually exercise a
   midpoint) was caught and renamed to `SearchAsync_RoundsToTwoDecimals_ForTheApprovedRoute
   FareTable` before being left in the suite.
3. **`IBookingStore` is not registered in `DependencyInjection.cs` yet.** Phase 4's task 6 says
   `AddInfrastructure` should register "`IBookingStore` (stub for now, filled in Phase 8)", but
   Phase 4's file list does not include any `IBookingStore` implementation file, and Phase 8
   explicitly owns `Infrastructure/Data/InMemoryBookingStore.cs`. Registering it now would have
   required inventing an unlisted stub class. Left unregistered, with an inline comment
   explaining why, to be added in Phase 8 alongside the real implementation.

No other decisions were made. No conflict with `challenge.md` blocked this phase.

### Deviations from the plan

None in the final state. The one file not explicitly listed in Phase 4 —
`RouteFareTableRoundingTests.cs` — is a test file (Phase 4's own "Relevant tests" section
requires the midpoint-rounding case; the file is the mechanism to satisfy that requirement
after the approved alternative in Decision #2 above), not a production/architecture file, and
was added after asking the user how to resolve the missing-midpoint-case problem.

### Files added

```
backend/SkyRoute.Infrastructure/Data/AirportCatalog.cs
backend/SkyRoute.Infrastructure/Providers/RouteFareTable.cs
backend/SkyRoute.Infrastructure/Providers/GlobalAirProvider.cs
backend/SkyRoute.Infrastructure/Providers/BudgetWingsProvider.cs
backend/SkyRoute.Infrastructure/Caching/MemoryOfferCache.cs
backend/SkyRoute.Infrastructure/DependencyInjection.cs
backend/SkyRoute.Infrastructure/Class1.cs                        (deleted — template placeholder,
                                                                    superseded by real files above)
backend/SkyRoute.Infrastructure/SkyRoute.Infrastructure.csproj    (modified — added
  Microsoft.Extensions.Caching.Memory, Microsoft.Extensions.DependencyInjection.Abstractions,
  InternalsVisibleTo for SkyRoute.Tests)
backend/SkyRoute.Tests/Infrastructure/GlobalAirProviderTests.cs
backend/SkyRoute.Tests/Infrastructure/BudgetWingsProviderTests.cs
backend/SkyRoute.Tests/Infrastructure/RouteFareTableRoundingTests.cs
```

No files outside `backend/SkyRoute.Infrastructure/` and `backend/SkyRoute.Tests/` were
modified.

### Validation performed

All four test areas named in `docs/03-execution-plan.md` Phase 4 are covered:

- **GlobalAir pricing** (`GlobalAirProviderTests.cs`, 4 tests): +15% surcharge, 2-decimal
  rounding, cabin multiplier applied before the provider rule (Economy vs Business price
  differs correctly), uncovered route (`ORD↔FCO`) returns an empty list.
- **BudgetWings pricing** (`BudgetWingsProviderTests.cs`, 7 tests): -10% discount, the $29.99
  floor engaging on the cheap route (`JFK↔ORD`), discount computed from the base fare only
  (explicitly asserting the double-discount value `145.80` is NOT produced), cabin multiplier
  applied before the provider rule, empty list for First Class, empty list for long-haul
  (`JFK↔LHR`), empty list for the uncovered route.
- **Away-from-zero midpoint rounding** (`RouteFareTableRoundingTests.cs`, 5 tests): synthetic
  values proving `RoundAwayFromZero` differs from banker's rounding on a true midpoint
  (`2.345m`), plus two additional midpoint cases and a non-midpoint sanity check.

| Check | Command | Result |
|---|---|---|
| Backend build | `dotnet build backend/SkyRoute.slnx` | **Build succeeded**, 0 errors, 2 warnings (pre-existing NU1510, unrelated to this phase) ✅ |
| Unit tests | `dotnet test backend/SkyRoute.slnx` | **Passed! 31/31**, 0 failed (24 from Phases 2–3 + 7 new BudgetWings tests; GlobalAir's 4 and the rounding suite's 5 were added and verified incrementally before BudgetWings) ✅ |

All Definition of Done criteria from `docs/03-execution-plan.md` Phase 4 are met: both
providers return decimal-correct, rounded prices for every covered combination of the 6
airports × 3 cabin classes; the `ORD↔FCO` route returns nothing from either provider;
`dotnet test` passes for all pricing tests written so far.

### Time

Plan estimate: 35 min. Actual: longer than estimated, primarily due to the two stop-and-ask
points (RouteFareTable mock values; the missing-midpoint-case problem and its two rejected
alternatives before landing on the approved `RouteFareTable.RoundAwayFromZero` extraction).
Neither delay was caused by the plan being wrong — both were genuine gaps the plan left open
(concrete mock data; whether the approved route set happens to produce a midpoint case) that
warranted a real decision rather than a silent assumption.

