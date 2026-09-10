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

---

## Phase 5 — Search API

**Status:** ✅ Complete · **Commit:** `5fa8b47` — "Phase 5: Search API (FlightSearchService
complete, Program.cs composition, AirportsController, FlightsController,
ProblemDetailsExceptionHandler)"

### What was implemented

All 5 files/changes listed in `docs/03-execution-plan.md` Phase 5 were completed:

- **`FlightSearchService.SearchAsync` (completed)**: resolves `Origin`/`Destination` via
  `IAirportCatalog.FindByCode`, throwing `ValidationException` (400) for an unknown code or for
  `origin == destination`; builds `FlightSearchCriteria`; fans out to every registered
  `IFlightProvider` in parallel via `Task.WhenAll`, wrapping each call in a try/catch so one
  provider throwing contributes zero offers instead of failing the whole search; generates a
  new `Guid` `searchId` and stores `(criteria, offers)` in `ISearchOfferCache`; maps to
  `SearchResponseDto` with `Currency = "USD"` and `IsInternational = criteria.IsInternational`.
- **`Program.cs`**: CORS policy `AllowLocalAngular` restricted to `http://localhost:4200`,
  applied before `MapControllers()`; `AddControllers().AddJsonOptions(...
  JsonStringEnumConverter())`; `AddProblemDetails()` + `AddExceptionHandler
  <ProblemDetailsExceptionHandler>()`; `AddInfrastructure()` (Phase 4's extension method);
  `FlightSearchService`/`BookingService` registered as concrete `AddScoped` classes (no
  interface — confirmed decision from `docs/02-revision.md`); HTTPS redirection skipped in
  Development to avoid dev-certificate friction.
- **`Controllers/AirportsController.cs`** — `GET /api/airports` → `IAirportCatalog.GetAll()`
  mapped to `AirportDto[]`.
- **`Controllers/FlightsController.cs`** — `POST /api/flights/search`, delegating to
  `FlightSearchService`; passenger-count validation (1–9, `challenge.md` §3.1) declared on
  `FlightSearchRequestDto` via `[Range(1, 9)]` and enforced automatically by `[ApiController]`,
  not duplicated in the controller body.
- **`Middleware/ProblemDetailsExceptionHandler.cs`** — implements `IExceptionHandler`; maps
  `ValidationException` → 400 (with the field→errors dictionary as a ProblemDetails
  extension), `FlightNotFoundException` → 404, `OfferExpiredException` → 409, anything else →
  500 with a generic title (no stack trace leaked to the client).

The two template placeholder files left over from Phase 1 (`WeatherForecastController.cs`,
`WeatherForecast.cs`) were deleted, now that real controllers exist in their place.

### Decisions made during implementation

No architectural decisions were required — every task in this phase had a concrete, unambiguous
specification. One **implementation bug** was found and fixed during the manual verification
step (task 6), documented here rather than under "Decisions" because it was a code defect, not
an interpretation choice:

- **`[property: Range(1, 9)]` on a record primary-constructor parameter throws at request-
  validation time on ASP.NET Core 10.** Manually testing `POST /api/flights/search` with a
  valid JFK→LHR payload returned an unexpected `500`. Temporarily surfacing the exception
  message (reverted immediately after diagnosis) revealed:
  `InvalidOperationException: Record type 'FlightSearchRequestDto' has validation metadata
  defined on property 'Passengers' that will be ignored. 'Passengers' is a parameter in the
  record primary constructor and validation metadata must be associated with the constructor
  parameter.` MVC's validator does not support `[property: ...]`-targeted attributes on a
  record's primary constructor parameters — the attribute must target the parameter directly.
  Fixed by changing `[property: Range(1, 9)] int Passengers` to `[Range(1, 9)] int Passengers`
  in `FlightSearchRequestDto`. Re-verified all four manual test scenarios afterward; all
  passed. This is a framework behaviour quirk, not a plan ambiguity — no report-and-stop was
  needed because the fix is unambiguous and does not touch architecture, contracts, or any
  approved decision.

No conflict with `challenge.md` or `docs/02-revision.md` blocked this phase.

### Deviations from the plan

None. All 5 files/changes match the plan's specification; the `[Range]` attribute placement
was an implementation detail (bug fix) within the already-approved validation requirement, not
a deviation from what the plan asked for.

### Files added/changed

```
backend/SkyRoute.Application/Services/FlightSearchService.cs   (completed)
backend/SkyRoute.Application/Dtos/FlightSearchRequestDto.cs    (added [Required]/[Range]
                                                                  validation attributes)
backend/SkyRoute.WebApi/Program.cs                              (modified — full composition)
backend/SkyRoute.WebApi/Controllers/AirportsController.cs       (new)
backend/SkyRoute.WebApi/Controllers/FlightsController.cs        (new)
backend/SkyRoute.WebApi/Middleware/ProblemDetailsExceptionHandler.cs   (new)
backend/SkyRoute.WebApi/Controllers/WeatherForecastController.cs   (deleted — template
                                                                      placeholder)
backend/SkyRoute.WebApi/WeatherForecast.cs                      (deleted — template placeholder)
backend/SkyRoute.Tests/Application/FlightSearchServiceTests.cs  (new — see Validation below)
```

No files outside `backend/SkyRoute.Application/`, `backend/SkyRoute.WebApi/`, and
`backend/SkyRoute.Tests/` were modified.

### Validation performed

All four test areas named in `docs/03-execution-plan.md` Phase 5 are covered in
`FlightSearchServiceTests.cs` (5 tests, using fakes for `IAirportCatalog`, `IFlightProvider`,
`ISearchOfferCache`):

- Aggregation: `totalPrice = pricePerPassenger × passengerCount`, verified with a 3-passenger
  search.
- One provider throwing (`ThrowingProvider`) does not fail the whole search — the other
  provider's offer is still returned.
- Zero-result search (no providers registered) returns an empty `Flights` list, not an error.
- Unknown airport code throws `ValidationException` (mapped to 400 by the middleware).
- `IsInternational` is `true` for JFK→LHR and `false` for JFK→LAX in the response, in a single
  test asserting both directions.

Beyond the automated tests, task 6's manual curl verification was performed against a running
instance of the API (`dotnet run`, port 5215):

| Scenario | Result |
|---|---|
| `POST /api/flights/search` JFK→LHR (international) | 200 OK; `isInternational: true`; only GlobalAir offers (BudgetWings correctly absent — long-haul route); `totalPrice = pricePerPassenger × 2` verified numerically |
| `POST /api/flights/search` JFK→LAX (domestic) | 200 OK; `isInternational: false`; 2 GlobalAir offers (207.00) + 2 BudgetWings offers (162.00), matching Phase 4's unit-tested values exactly |
| `POST /api/flights/search` ORD→FCO (uncovered route) | 200 OK; `flights: []` — not an error |
| `POST /api/flights/search` JFK→ZZZ (unknown airport) | 400 ProblemDetails with a field-specific error (`"Destination": ["Unknown airport code: 'ZZZ'."]`) |
| `GET /api/airports` | 200 OK, 6 airports |
| CORS preflight (`OPTIONS`, `Origin: http://localhost:4200`) | 204 No Content; `Access-Control-Allow-Origin: http://localhost:4200` present |

| Check | Command | Result |
|---|---|---|
| Backend build | `dotnet build backend/SkyRoute.slnx` | **Build succeeded**, 0 errors, 2 warnings (pre-existing NU1510, unrelated to this phase) ✅ |
| Unit tests | `dotnet test backend/SkyRoute.slnx` | **Passed! 36/36**, 0 failed (31 from Phases 2–4 + 5 new) ✅ |
| Manual verification | curl against a running `dotnet run` instance | All 6 scenarios above returned the expected status code and body ✅ |

All Definition of Done criteria from `docs/03-execution-plan.md` Phase 5 are met:
`POST /api/flights/search` returns a correct, schema-matching JSON body for both a domestic and
an international route, and an empty array for the uncovered route; `GET /api/airports` returns
all 6 airports; all Phase 5 tests pass; CORS allows a request from `http://localhost:4200`
(verified directly with a preflight request, ahead of Phase 6 as the plan anticipated).

### Time

Plan estimate: 30 min. Actual: longer than estimated, primarily due to diagnosing the
`[property: Range]`/record-primary-constructor validation bug during manual verification
(temporary debug logging added and removed, server restarted several times to isolate the
cause). The delay was a genuine framework defect surfaced by testing, not a plan or
architecture issue.

---

## Phase 6 — Angular search UI and results

**Status:** ✅ Complete · **Commit:** not committed yet (implemented and validated in working tree)

### What was implemented

All Phase 6 scope items from `docs/03-execution-plan.md` were implemented in
`frontend/skyroute-app/src/app/` and wired to the existing Phase 5 backend endpoints only
(`GET /api/airports`, `POST /api/flights/search`):

- **Environment config**: `environments/environment.ts` created with
  `apiUrl: 'http://localhost:5169/api'`, matching `SkyRoute.WebApi` launch settings.
- **Models**: `Airport`, `SearchRequest`, `FlightOfferView`, `SearchResponse` created and
  aligned to backend DTO shape, including `isInternational` at the search-response root.
- **Airport service** (`core/services/airport.service.ts`): calls `GET /airports` via
  `HttpClient`, caches the list in a signal, and reuses a shared in-flight observable to avoid
  duplicate concurrent calls.
- **Flight service** (`core/services/flight.service.ts`): calls `POST /flights/search` and
  returns typed `SearchResponse`.
- **Search state** (`core/state/search-state.ts`): signal-based state holder with
  `airports`, `criteria`, `searchId`, `results`, `isInternational`, `sortMode`,
  `selectedOfferId`, `loading`, `error`; includes `computed()` `sortedResults` and a pure
  `sortFlightOffers(...)` function supporting all four required sort modes.
- **Search form feature** (`features/search/search-form.ts` + `.html` + `.scss`): reactive
  form with origin/destination selects, departure date, passengers, cabin class; validators for
  required fields, passenger range 1-9, and a form-level `origin !== destination` rule;
  submits to `FlightService.search()`, updates `SearchState`, and navigates to `/results`.
- **Results empty state** (`features/results/empty-state.ts`): renders when there are no
  sorted results.
- **Sort toolbar** (`features/results/sort-toolbar.ts` + `.html` + `.scss`): client-side
  control for Price ↑, Price ↓, Duration, Departure time; updates only `SearchState.sortMode`
  (no HTTP call, no navigation side-effects).
- **Results list** (`features/results/results-list.ts` + `.html` + `.scss`): table bound to
  `sortedResults()`, showing provider, flight number, departure, arrival, duration, cabin class,
  and the required pricing presentation (total as primary, per-person as secondary muted text);
  shows loading status from state; row selection sets `selectedOfferId` and navigates to
  `/booking/:flightId`.
- **Duration pipe** (`shared/pipes/duration.ts`): formats duration minutes to `Xh Ym`.
- **Routing update** (`app.routes.ts`): `/search`, `/results`, `/booking/:flightId`
  (placeholder component for booking until Phase 7), plus default redirect `/ -> /search`.

To support the Phase 6 routed flow, the root shell was simplified to router-outlet rendering and
HttpClient provision was added:

- `app.html` replaced with `<router-outlet />`.
- `app.ts` simplified (removed template title signal used only by Angular scaffold screen).
- `app.config.ts` updated with `provideHttpClient()`.
- `app.spec.ts` updated to remove the scaffold-title assertion that no longer applies.

### Decisions made during implementation

1. **Plan path ambiguity handled literally.** The Phase 6 section says files are under
   `src/app/` and includes `environments/environment.ts`. To avoid introducing a new
   interpretation, the environment file was placed at `src/app/environments/environment.ts`
   (not `src/environments/`).
2. **Sort UI implemented as `<select>`.** The plan allows "buttons/select"; select was used as
   the minimal MVP control while preserving the required four client-side sort modes.
3. **Results route entered immediately on submit with loading state.** The form navigates to
   `/results` right after submit and keeps `loading=true` until the HTTP response resolves,
   ensuring a visible loading indicator while search is in progress.

No architecture changes were introduced, and no additional infrastructure/services were added.

### Deviations from the plan

None in functional scope. All required Phase 6 capabilities were implemented.

Minor implementation delta (non-scope): two supporting app-shell updates (`app.config.ts` for
`provideHttpClient`, and root template simplification) were required so the new Phase 6 routes
and services can run in the scaffolded Angular app.

### Files added/changed

```
frontend/skyroute-app/src/app/environments/environment.ts                               (new)
frontend/skyroute-app/src/app/core/models/airport.ts                                    (new)
frontend/skyroute-app/src/app/core/models/search-request.ts                             (new)
frontend/skyroute-app/src/app/core/models/search-response.ts                            (new)
frontend/skyroute-app/src/app/core/services/airport.service.ts                          (new)
frontend/skyroute-app/src/app/core/services/flight.service.ts                           (new)
frontend/skyroute-app/src/app/core/state/search-state.ts                                (new)
frontend/skyroute-app/src/app/features/search/search-form.ts                            (new)
frontend/skyroute-app/src/app/features/search/search-form.html                          (new)
frontend/skyroute-app/src/app/features/search/search-form.scss                          (new)
frontend/skyroute-app/src/app/features/results/empty-state.ts                           (new)
frontend/skyroute-app/src/app/features/results/sort-toolbar.ts                          (new)
frontend/skyroute-app/src/app/features/results/sort-toolbar.html                        (new)
frontend/skyroute-app/src/app/features/results/sort-toolbar.scss                        (new)
frontend/skyroute-app/src/app/features/results/results-list.ts                          (new)
frontend/skyroute-app/src/app/features/results/results-list.html                        (new)
frontend/skyroute-app/src/app/features/results/results-list.scss                        (new)
frontend/skyroute-app/src/app/shared/pipes/duration.ts                                  (new)
frontend/skyroute-app/src/app/features/results/sort.spec.ts                             (new)
frontend/skyroute-app/src/app/features/search/search-form.spec.ts                       (new)
frontend/skyroute-app/src/app/app.routes.ts                                             (modified)
frontend/skyroute-app/src/app/app.config.ts                                             (modified)
frontend/skyroute-app/src/app/app.ts                                                    (modified)
frontend/skyroute-app/src/app/app.html                                                  (modified)
frontend/skyroute-app/src/app/app.spec.ts                                               (modified)
```

No files outside `frontend/skyroute-app/src/app/` were modified for this phase.

### Validation performed

Relevant tests requested in Phase 6 were added and run:

- `features/results/sort.spec.ts` (Vitest): verifies ordering for all four sort modes and
  asserts purity (source array is not mutated).
- `features/search/search-form.spec.ts` (Vitest): verifies
  `origin === destination` is rejected and passenger counts outside 1-9 are rejected.

One strict-typing issue surfaced during the first test-target build pass and was fixed:

- `search-form.ts` attempted `setSelectedOfferId(null)` while the state API accepted `string`.
  Resolved by adding `clearSelectedOfferId()` in `SearchState` and using that method in submit.

| Check | Command | Result |
|---|---|---|
| Frontend tests (first run) | Angular test target | Failed with TS2345 in `search-form.ts` (null argument), fixed immediately ✅ |
| Frontend tests (rerun) | Angular test target | **Passed! 3 test files, 8 tests, 0 failed** ✅ |
| Frontend build | Angular build target | **Build succeeded**, bundle emitted to `dist/skyroute-app` ✅ |

Definition-of-done checks covered by implemented behavior and validation:

- Search form fields and validators present and enforced.
- Loading state displayed during in-flight search.
- Results table includes required columns and total-vs-per-person price distinction.
- Client-side sorting implemented for all four modes without service calls.
- Empty-state component implemented for zero-result searches.

### Time

Plan estimate: 40 min. Actual: slightly above estimate due to one compile-time strict typing fix
found during milestone validation (`TS2345`) and immediate retest/build verification.

---

## Phase 7 — Booking flow (frontend)

**Status:** ✅ Complete · **Commit:** not committed yet (implemented and validated in working tree)

### What was implemented

All Phase 7 frontend scope items from `docs/03-execution-plan.md` were implemented under
`frontend/skyroute-app/src/app/` without adding architectural layers:

- **Booking models** (`core/models/booking.ts`):
  `PassengerFormValue`, `BookingRequest`, `BookingResponse`, mirroring the agreed backend
  contract shape (`searchId`, `flightId`, `passengers[]`; no client-sent price or
  `isInternational`).
- **Booking API service** (`core/services/booking.service.ts`):
  `HttpClient.post<BookingResponse>` to `POST /api/bookings` with typed error mapping via
  `BookingApiError`, including `isOfferExpired` when HTTP status is `409`.
- **Document validator factory** (`shared/validators/document-number.ts`):
  client-side UX validator using the exact backend regexes:
  passport `^[A-Z]{1,2}[0-9]{6,7}$`, national ID `^[0-9]{9}$`, with trim/uppercase normalization.
- **Route guard** (`core/guards/has-selected-offer.guard.ts`):
  blocks `/booking/:flightId` when neither `selectedOfferId` nor `searchId` exists in
  `SearchState`, redirecting to `/search`.
- **Booking summary component** (`features/booking/booking-summary.ts` + `.html` + `.scss`):
  displays route, provider, flight number, departure, arrival, duration, and cabin class from
  selected offer and current search criteria.
- **Passenger form component** (`features/booking/passenger-form.ts` + `.html` + `.scss`):
  - creates a `FormArray` with one row per passenger based on `SearchState.criteria().passengers`
  - fields per passenger: full name, email, document number
  - dynamic label switches between **Passport Number** and **National ID** from
    `SearchState.isInternational()`
  - document validator switches rule by route type via `documentNumberValidator(...)`
  - price breakdown uses state-held values (per-person, count, total) without recomputing from
    user input
  - confirm button posts `{ searchId, flightId, passengers }` to backend
  - on `409`, shows "fares have changed" banner with button back to `/search`
- **Confirmation component** (`features/booking/confirmation.ts` + `.html` + `.scss`):
  renders `bookingReference`, flight summary and total price using the successful booking
  response passed in navigation state.
- **Routing update** (`app.routes.ts`):
  removed Phase 6 booking placeholder and wired:
  - `/booking/:flightId` -> `PassengerFormComponent` with `canActivate: [hasSelectedOfferGuard]`
  - `/booking/confirmation/:bookingReference` -> `ConfirmationComponent`

To satisfy the Phase 7 UX requirement "back to search keeps previous criteria pre-filled", the
search form now restores criteria from `SearchState` when the component is created:

- `features/search/search-form.ts` reads `searchState.criteria()` and `patchValue(...)` on init.

### Decisions made during implementation

1. **Typed booking error instead of untyped status checks in components.**
   `BookingService` maps `HttpErrorResponse` to `BookingApiError` so the component can reliably
   distinguish `409` (offer expired) from generic failures without spreading transport details
   across UI code.
2. **Guard condition accepts `selectedOfferId` or `searchId`.**
   The guard follows the Phase 7 wording ("selectedOfferId (or searchId)") to avoid blank
   booking screens while still protecting direct access with no search context.
3. **Confirmation uses router navigation state for MVP.**
   This keeps Phase 7 frontend within scope while remaining compatible with Phase 8's optional
   `GET /api/bookings/{reference}` enhancement for hard-refresh resilience.

No additional infrastructure, global stores, or backend changes were introduced in this phase.

### Deviations from the plan

None in scope or architecture.

One implementation fix was required during validation (not a scope change):

- `PassengerFormComponent` initially declared `searchState` as `private`, which broke template
  access at build time (`TS2341`). It was changed to `protected`.

### Files added/changed

```
frontend/skyroute-app/src/app/core/models/booking.ts                              (new)
frontend/skyroute-app/src/app/core/services/booking.service.ts                    (new)
frontend/skyroute-app/src/app/shared/validators/document-number.ts                (new)
frontend/skyroute-app/src/app/core/guards/has-selected-offer.guard.ts             (new)
frontend/skyroute-app/src/app/core/guards/has-selected-offer.guard.spec.ts        (new)
frontend/skyroute-app/src/app/shared/validators/document-number.spec.ts           (new)
frontend/skyroute-app/src/app/features/booking/booking-summary.ts                 (new)
frontend/skyroute-app/src/app/features/booking/booking-summary.html               (new)
frontend/skyroute-app/src/app/features/booking/booking-summary.scss               (new)
frontend/skyroute-app/src/app/features/booking/passenger-form.ts                  (new)
frontend/skyroute-app/src/app/features/booking/passenger-form.html                (new)
frontend/skyroute-app/src/app/features/booking/passenger-form.scss                (new)
frontend/skyroute-app/src/app/features/booking/confirmation.ts                    (new)
frontend/skyroute-app/src/app/features/booking/confirmation.html                  (new)
frontend/skyroute-app/src/app/features/booking/confirmation.scss                  (new)
frontend/skyroute-app/src/app/app.routes.ts                                       (modified)
frontend/skyroute-app/src/app/features/search/search-form.ts                      (modified)
```

No files outside `frontend/skyroute-app/src/app/` were modified for this phase.

### Validation performed

Relevant Phase 7 tests were added and executed:

- `shared/validators/document-number.spec.ts`:
  passport accept/reject, national ID accept/reject, and rule switching by `isInternational`.
- `core/guards/has-selected-offer.guard.spec.ts`:
  redirects to `/search` when no selected offer/search context exists.

Incremental milestone runs:

| Check | Command | Result |
|---|---|---|
| Frontend tests after Phase 7 core (service/validator/guard) | Angular test target | **Passed! 5 files, 12 tests, 0 failed** ✅ |
| Frontend tests after booking UI + routes | Angular test target | **Passed! 5 files, 12 tests, 0 failed** ✅ |
| Frontend build (first run after UI wiring) | Angular build target | Failed with `TS2341` (`searchState` visibility in template), fixed immediately ✅ |
| Frontend build (rerun) | Angular build target | **Build succeeded**, bundle emitted to `dist/skyroute-app` ✅ |

Definition-of-done coverage validated on frontend side:

- Selecting a flight from results routes to booking UI.
- Dynamic document label and validator switch by `isInternational`.
- Confirm action posts expected booking payload.
- `409` path shows "fares changed" banner and returns to pre-filled search.
- Guard prevents blank direct access by redirecting to `/search`.

Backend note: end-to-end booking confirmation depends on Phase 8 backend implementation
(`POST /api/bookings` controller/service/store).

### Time

Plan estimate: 35 min. Actual: slightly above estimate due to one compile-time visibility fix
(`TS2341`) discovered during build validation and immediate retest/rebuild.

