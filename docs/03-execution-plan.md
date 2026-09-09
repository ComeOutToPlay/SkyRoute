# SkyRoute — Execution Plan

**Status:** ready to implement · **Source of truth for architecture:** this document
**Time box:** ~4 hours · **Stack:** .NET 10 · Angular 22 · Node 22.22.3+ (nvm)

## Note on `docs/02-revision.md`

`02-revision.md` still describes a single flat project (`backend/SkyRoute.Api` with
`Controllers/Providers/Services/Models/Data`). During the follow-up review we approved a
**Clean Architecture, multi-project** backend instead (`Domain / Application / Infrastructure /
WebApi`), plus three concrete decisions that were never written back to that file because they
were approved while edits were blocked:

- `FlightSearchCriteria.IsInternational` is a derived property (`Origin.CountryCode !=
  Destination.CountryCode`), not a stored flag.
- `FlightOffer` does **not** carry `IsInternational` — it is a criteria-level concept, computed
  once per search, not once per offer.
- `SearchResponseDto` exposes `isInternational` once, at the response root.
- `DocumentValidator` lives in `Domain/Rules/`, not `Application/Services/`.

This plan is the authoritative version of the architecture. All contracts, pricing formulas,
airport list, and testing priorities from `02-revision.md` remain valid and are carried over
unchanged; only the backend project layout and the four points above are corrected here.

---

## Architecture baseline (recap)

```
backend/
  SkyRoute.sln
  SkyRoute.Domain/            no dependencies
  SkyRoute.Application/       -> Domain
  SkyRoute.Infrastructure/     -> Domain, Application
  SkyRoute.WebApi/             -> Application, Infrastructure (composition root)
  SkyRoute.Tests/              -> all of the above

frontend/
  skyroute-app/                Angular 22, standalone, zoneless, signals, Vitest
```

**Why `IAirportCatalog` and `IBookingStore` are added to `Application/Abstractions/`:**
`FlightSearchService` needs to resolve airport codes; `BookingService` needs to persist
bookings. Both concrete implementations live in Infrastructure (hardcoded catalogue,
`ConcurrentDictionary` store — both already approved). Without an interface, Application would
either depend on Infrastructure (wrong direction) or duplicate the airport list (drift risk on
the most scrutinised requirement). These two interfaces are not new architectural layers — they
are the minimum seam the already-approved Infrastructure components require. `ISearchOfferCache`
already established this exact pattern for the offer cache.

**Provider abstraction (unchanged, the one interface that earns its place on merit):**

```csharp
public interface IFlightProvider
{
    string ProviderName { get; }
    Task<IReadOnlyList<FlightOffer>> SearchAsync(FlightSearchCriteria criteria, CancellationToken ct);
}
```

Two implementations today, a third promised by the brief (§2), consumed polymorphically via
`IEnumerable<IFlightProvider>`. Onboarding provider #3 = one class + one DI registration line.

**Explicitly not introduced anywhere in this plan:** EF Core/DbContext, `Money` value object,
`IPricingStrategy`, NgRx, `IFlightSearchService`/`IBookingService` interfaces (single
implementation each, no benefit), server-side sorting, a client-supplied price or
`isInternational` flag on the booking request.

---

## Phase 1 — Solution & project scaffolding

**Objective:** stand up the five backend projects wired together, plus the Angular workspace,
with nothing in them yet except empty folders and project references. Nothing here is business
logic; the goal is a solution that builds and a frontend that serves, before any feature code
exists.

**Files/projects created:**
```
backend/SkyRoute.sln
backend/SkyRoute.Domain/SkyRoute.Domain.csproj
backend/SkyRoute.Application/SkyRoute.Application.csproj
backend/SkyRoute.Infrastructure/SkyRoute.Infrastructure.csproj
backend/SkyRoute.WebApi/SkyRoute.WebApi.csproj
backend/SkyRoute.Tests/SkyRoute.Tests.csproj
frontend/skyroute-app/  (full Angular CLI scaffold)
.gitignore
```

**Tasks in order:**
1. `nvm use 22.22.3` (install first if missing) — verify with `node -v`.
2. `git init` at the repo root; add a `.gitignore` covering Visual Studio (`bin/`, `obj/`) and
   Node (`node_modules/`, `dist/`, `.angular/`).
3. `dotnet new sln -n SkyRoute -o backend`
4. `dotnet new classlib -n SkyRoute.Domain -o backend/SkyRoute.Domain -f net10.0` (delete the
   generated `Class1.cs`)
5. `dotnet new classlib -n SkyRoute.Application -o backend/SkyRoute.Application -f net10.0`
6. `dotnet new classlib -n SkyRoute.Infrastructure -o backend/SkyRoute.Infrastructure -f net10.0`
7. `dotnet new webapi -controllers -f net10.0 -n SkyRoute.WebApi -o backend/SkyRoute.WebApi`
8. `dotnet new xunit -n SkyRoute.Tests -o backend/SkyRoute.Tests -f net10.0`
9. Add all five projects to the solution.
10. Wire references: Application→Domain; Infrastructure→Domain+Application;
    WebApi→Application+Infrastructure; Tests→Domain+Application+Infrastructure.
11. `dotnet add SkyRoute.WebApi package Microsoft.Extensions.Caching.Memory`
12. Create empty folder skeleton — Domain: `Entities/ ValueObjects/ Enums/ Models/ Rules/
    Interfaces/`; Application: `Dtos/ Services/ Abstractions/`; Infrastructure: `Providers/
    Caching/ Data/`; WebApi: `Controllers/ Middleware/`.
13. `npx @angular/cli@22 new skyroute-app --style=scss --ssr=false --skip-git --routing` inside
    `frontend/`.
14. Verify `dotnet build backend/SkyRoute.sln` succeeds and `npm start` (from
    `frontend/skyroute-app`) serves the default template.

**Dependencies on previous phases:** none — this is the starting point.

**Relevant tests:** none yet; this phase only proves the build/serve pipeline works.

**Definition of done:** `dotnet build` succeeds across all 5 projects with correct references;
`ng serve` renders the default Angular page at `localhost:4200`; git repo initialised with an
initial commit.

**Estimated time:** 15 min

---

## Phase 2 — Domain models and rules

**Objective:** implement every piece of pure business logic that has zero external
dependencies: entities, value objects, enums, the search criteria model with its derived
`IsInternational`, the flight offer model, the document validator, and the provider interface.
By the end of this phase, the core business rules of the whole challenge (document rule,
international/domestic detection, cabin enum) exist and are unit-testable in isolation.

**Files created (all in `SkyRoute.Domain/`):**
```
ValueObjects/Airport.cs
Enums/CabinClass.cs
Enums/BookingStatus.cs
Models/FlightOffer.cs
Models/FlightSearchCriteria.cs
Entities/Booking.cs
Entities/Passenger.cs
Rules/DocumentValidator.cs
Interfaces/IFlightProvider.cs
```

**Tasks in order:**
1. `ValueObjects/Airport.cs` — `public sealed record Airport(string Code, string City, string
   Country, string CountryCode);`
2. `Enums/CabinClass.cs` — `Economy, Business, First`.
3. `Enums/BookingStatus.cs` — `Confirmed` (only value needed now; kept as an enum so it is
   extensible later, e.g. `Cancelled`).
4. `Models/FlightSearchCriteria.cs`:
   ```csharp
   public sealed class FlightSearchCriteria
   {
       public required Airport Origin { get; init; }
       public required Airport Destination { get; init; }
       public required DateOnly DepartureDate { get; init; }
       public required int PassengerCount { get; init; }
       public required CabinClass CabinClass { get; init; }
       public bool IsInternational => Origin.CountryCode != Destination.CountryCode;
   }
   ```
5. `Models/FlightOffer.cs` — provider, flight number, origin/destination codes, departure/arrival
   `DateTime` (unspecified kind, local-to-airport, no offset), `DurationMinutes`, `CabinClass`,
   `PricePerPassenger` (`decimal`). **No `IsInternational` here** — confirmed architectural
   decision; it is a criteria-level fact, not a per-offer fact, and putting it here would compute
   the same boolean redundantly for every offer in a search.
6. `Entities/Passenger.cs` — `FullName`, `Email`, `DocumentNumber`.
7. `Entities/Booking.cs` — `Reference`, `FlightOffer` (snapshot, not a live reference),
   `Passengers`, `TotalPrice`, `Currency`, `Status`, `CreatedAtUtc`.
8. `Rules/DocumentValidator.cs`:
   ```csharp
   public static class DocumentValidator
   {
       private const string PassportPattern = @"^[A-Z]{1,2}[0-9]{6,7}$";
       private const string NationalIdPattern = @"^[0-9]{9}$";

       public static bool IsValidPassport(string? documentNumber) =>
           Regex.IsMatch(Normalize(documentNumber), PassportPattern);

       public static bool IsValidNationalId(string? documentNumber) =>
           Regex.IsMatch(Normalize(documentNumber), NationalIdPattern);

       public static bool IsValid(string? documentNumber, bool isInternational) =>
           isInternational ? IsValidPassport(documentNumber) : IsValidNationalId(documentNumber);

       private static string Normalize(string? value) =>
           value?.Trim().ToUpperInvariant() ?? string.Empty;
   }
   ```
   Lives in `Domain/Rules/`, **not** `Application/Services/` — confirmed decision. No DI, no
   dependencies, pure regex — trivially unit-testable and safe to call from both Application
   (booking validation) and, mirrored, from the Angular validator factory.
9. `Interfaces/IFlightProvider.cs` (signature shown in the architecture baseline above).

**Dependencies on previous phases:** Phase 1 (projects must exist).

**Relevant tests (written now or in Phase 9 — logic is simple enough to defer without risk):**
- `DocumentValidator`: passport accept (`X1234567`) / reject (`123456789`); national ID accept
  (`123456789`) / reject (`X1234567`). 4 cases minimum.
- `FlightSearchCriteria.IsInternational`: same country → `false`; different countries → `true`.

**Definition of done:** `SkyRoute.Domain` compiles with zero dependencies on any other project
in the solution; `DocumentValidator` and `IsInternational` are callable and correct in isolation
(quick scratch test or immediately-written xUnit test).

**Estimated time:** 20 min

---

## Phase 3 — Application layer and DTOs

**Objective:** define the wire contracts (DTOs) and the two orchestration services
(`FlightSearchService`, `BookingService`) as concrete classes, plus the abstractions Application
needs from Infrastructure. No implementation of providers/catalogue/cache/store yet — those are
Infrastructure (Phase 4/5) — only the interfaces and the orchestration logic that will call them.

**Files created (all in `SkyRoute.Application/`):**
```
Dtos/AirportDto.cs
Dtos/FlightSearchRequestDto.cs
Dtos/FlightOfferDto.cs
Dtos/SearchResponseDto.cs
Dtos/PassengerDto.cs
Dtos/BookingRequestDto.cs
Dtos/BookingResponseDto.cs
Abstractions/IAirportCatalog.cs
Abstractions/ISearchOfferCache.cs
Abstractions/IBookingStore.cs
Services/FlightSearchService.cs
Services/BookingService.cs
Exceptions/OfferExpiredException.cs
Exceptions/FlightNotFoundException.cs
Exceptions/ValidationException.cs   (or reuse FluentValidation's if adopted)
```

**Tasks in order:**
1. DTOs, matching the contracts fixed in `02-revision.md` §"Endpoints" exactly, plus the one
   confirmed addition — `isInternational` on `SearchResponseDto`:
   - `AirportDto(string Code, string City, string Country, string CountryCode)`
   - `FlightSearchRequestDto(string Origin, string Destination, DateOnly DepartureDate, int
     Passengers, CabinClass CabinClass)`
   - `FlightOfferDto` — id, provider, flightNumber, origin, destination, departureTime,
     arrivalTime, durationMinutes, cabinClass, pricePerPassenger, totalPrice
   - `SearchResponseDto` — `searchId`, `passengerCount`, `currency`, **`isInternational`**,
     `flights: FlightOfferDto[]`
   - `PassengerDto(string FullName, string Email, string DocumentNumber)`
   - `BookingRequestDto(Guid SearchId, string FlightId, PassengerDto[] Passengers)`
   - `BookingResponseDto` — bookingReference, status, flightSummary, pricePerPassenger,
     passengerCount, totalPrice, currency
2. `Abstractions/IAirportCatalog.cs` — `Airport? FindByCode(string code); IReadOnlyList<Airport>
   GetAll();`
3. `Abstractions/ISearchOfferCache.cs` — `void Store(Guid searchId, FlightSearchCriteria
   criteria, IReadOnlyList<FlightOffer> offers); CachedSearch? Get(Guid searchId);` (a small
   internal `CachedSearch` record bundling criteria + offers).
4. `Abstractions/IBookingStore.cs` — `void Save(Booking booking); Booking? FindByReference(string
   reference);`
5. `Services/FlightSearchService.cs` — orchestration only, implemented fully in Phase 5 once
   providers exist; stub the method signature now:
   `Task<SearchResponseDto> SearchAsync(FlightSearchRequestDto request, CancellationToken ct)`.
6. `Services/BookingService.cs` — stub `Task<BookingResponseDto> BookAsync(BookingRequestDto
   request)`; full logic in Phase 8.
7. Exception types for the three failure modes the controllers must map to HTTP status codes
   (400 unknown airport / mismatched passenger count / invalid document, 404 flight not found,
   409 offer expired).

**Dependencies on previous phases:** Phase 2 (`FlightSearchCriteria`, `FlightOffer`, `Booking`,
`Airport`, `DocumentValidator` must exist).

**Relevant tests:** none new in this phase (DTOs are data holders); orchestration logic is
tested once implemented, in Phases 5 and 8.

**Definition of done:** `SkyRoute.Application` compiles, referencing only `SkyRoute.Domain`;
every DTO field matches the JSON contracts in `02-revision.md` exactly (field names, casing via
`JsonStringEnumConverter` handled in Phase 5's `Program.cs` setup); `isInternational` is present
on `SearchResponseDto` and absent from `FlightOfferDto`.

**Estimated time:** 20 min

---

## Phase 4 — Flight providers and pricing

**Objective:** implement the two mock providers with their pricing rules, the airport catalogue,
and the offer cache — the three Infrastructure pieces `FlightSearchService` depends on. This is
the phase where the challenge's core business rule (§2 pricing table) becomes real, tested code.

**Files created (all in `SkyRoute.Infrastructure/`):**
```
Data/AirportCatalog.cs
Providers/GlobalAirProvider.cs
Providers/BudgetWingsProvider.cs
Providers/RouteFareTable.cs
Caching/MemoryOfferCache.cs
DependencyInjection.cs
```

**Tasks in order:**
1. `Data/AirportCatalog.cs` implementing `IAirportCatalog` — hardcoded list of 6 airports / 4
   countries from `02-revision.md` (JFK, LAX, ORD — US; LHR — GB; CDG — FR; FCO — IT).
2. `Providers/RouteFareTable.cs` — a small deterministic base-fare lookup keyed by
   `(origin, destination)`, shared by both providers so their prices differ only by their own
   rule, not by inventing unrelated base fares. Also encodes **route/cabin coverage**: BudgetWings
   returns no offers for First Class or long-haul; ORD↔FCO is returned by neither provider
   (the deliberately uncovered pair that makes the empty state reachable).
3. `Providers/GlobalAirProvider.cs` implementing `IFlightProvider`:
   - `base = routeFare(origin, destination) × cabinMultiplier` (Economy 1.0 / Business 2.5 /
     First 4.0)
   - `pricePerPassenger = Math.Round(base * 1.15m, 2, MidpointRounding.AwayFromZero)`
   - `await Task.Delay(400, ct)` simulated latency
   - deterministic flight numbers/times seeded from the search criteria (date + route + cabin)
4. `Providers/BudgetWingsProvider.cs` implementing `IFlightProvider`:
   - same `base` computation
   - `pricePerPassenger = Math.Max(Math.Round(base * 0.90m, 2, MidpointRounding.AwayFromZero),
     29.99m)` — discount applied to the base fare only, floor is per-passenger
   - ensure at least one route/cabin combination (short domestic hop, Economy) produces a base
     fare low enough that the $29.99 floor actually triggers — otherwise the rule is untestable
     in the UI
   - same simulated latency and coverage restrictions as above
5. `Caching/MemoryOfferCache.cs` implementing `ISearchOfferCache` over `IMemoryCache`, 10-minute
   sliding/absolute TTL.
6. `DependencyInjection.cs` — `AddInfrastructure(this IServiceCollection services)` registering
   `IAirportCatalog`, both `IFlightProvider` implementations, `ISearchOfferCache`, `IBookingStore`
   (stub for now, filled in Phase 8).

**Dependencies on previous phases:** Phase 2 (`IFlightProvider`, `FlightOffer`,
`FlightSearchCriteria`), Phase 3 (`IAirportCatalog`, `ISearchOfferCache` interfaces).

**Relevant tests:**
- GlobalAir pricing: +15%, 2-decimal rounding, away-from-zero midpoint case (e.g. a base fare
  that rounds differently under banker's vs away-from-zero rounding).
- BudgetWings pricing: −10%, the $29.99 floor engages on the cheap route, discount is computed
  from the base fare only (not from an already-discounted value).
- Cabin multiplier changes the price and is applied before the provider rule.
- A route/cabin combination with zero coverage returns an empty list from both providers.

**Definition of done:** both providers return decimal-correct, rounded prices for every
combination of the 6 airports × 3 cabin classes that they cover; the ORD↔FCO route (or
equivalent uncovered pair) returns nothing from either provider; `dotnet test` passes for the
pricing tests written so far.

**Estimated time:** 35 min

---

## Phase 5 — Search API

**Objective:** wire everything into a running, callable `POST /api/flights/search` and
`GET /api/airports`, with full `Program.cs` composition (CORS, JSON enum converter,
ProblemDetails, DI registrations). At the end of this phase, the search half of the challenge
is fully functional and testable with curl/Postman, independent of any frontend work.

**Files created/modified:**
```
SkyRoute.WebApi/Program.cs                 (modified)
SkyRoute.WebApi/Controllers/AirportsController.cs
SkyRoute.WebApi/Controllers/FlightsController.cs
SkyRoute.WebApi/Middleware/ProblemDetailsExceptionHandler.cs
SkyRoute.Application/Services/FlightSearchService.cs   (completed)
```

**Tasks in order:**
1. Complete `FlightSearchService.SearchAsync`:
   - resolve `Origin`/`Destination` via `IAirportCatalog.FindByCode` → throw a "not found"
     application exception (mapped to 400) if either code is unknown or if origin == destination
   - build `FlightSearchCriteria`
   - fan out to all registered `IFlightProvider`s with `Task.WhenAll`, wrapping each call so one
     provider throwing is caught/logged and excluded, not fatal to the whole search
   - flatten results into `FlightOffer[]`, generate a new `searchId` (`Guid.NewGuid()`), store
     `(criteria, offers)` in `ISearchOfferCache`
   - map to `SearchResponseDto` (`searchId`, `passengerCount`, `currency = "USD"`,
     `isInternational = criteria.IsInternational`, `flights` mapped from `FlightOffer[]`)
2. `Program.cs`:
   - `builder.Services.AddInfrastructure()` (Phase 4's extension method) and
     `AddApplication()` (registers `FlightSearchService`, `BookingService` as concrete classes)
   - CORS policy `AllowLocalAngular` → origin `http://localhost:4200`, applied before
     `MapControllers()`
   - `AddControllers().AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new
     JsonStringEnumConverter()))`
   - `AddProblemDetails()` + a global exception-to-ProblemDetails mapping middleware
   - `AddMemoryCache()`
3. `Controllers/AirportsController.cs` — `GET /api/airports` → `IAirportCatalog.GetAll()`
   mapped to `AirportDto[]`.
4. `Controllers/FlightsController.cs` — `POST /api/flights/search` → validates the request
   shape (passengers 1–9, valid cabin enum value) and delegates to `FlightSearchService`.
5. `Middleware/ProblemDetailsExceptionHandler.cs` — maps the Application exception types from
   Phase 3 to status codes: unknown airport / same origin-destination / invalid passenger count
   → 400; anything unexpected → 500 with a generic ProblemDetails body (no stack trace leaked).
6. Manual verification with curl/Postman: search JFK→LHR (international, GlobalAir + BudgetWings
   both respond), search JFK→LAX (domestic), search ORD→FCO (empty array, 200 OK — not an
   error).

**Dependencies on previous phases:** Phase 3 (DTOs, service stub), Phase 4 (providers,
catalogue, cache).

**Relevant tests:**
- Aggregation: total = per-passenger × passengerCount; one provider throwing does not fail the
  search (mock a throwing `IFlightProvider` in the test).
- Zero-result search on the uncovered route returns `flights: []`, HTTP 200.
- Unknown airport code → 400 ProblemDetails.
- `isInternational` is `true` for JFK→LHR and `false` for JFK→LAX in the response root.

**Definition of done:** `POST /api/flights/search` returns a correct, schema-matching JSON body
for both a domestic and an international route, and an empty array for the uncovered route;
`GET /api/airports` returns all 6 airports; all Phase 5 tests pass; CORS allows a request from
`http://localhost:4200` (verified once Phase 6 exists, but the policy must be in place now).

**Estimated time:** 30 min

---

## Phase 6 — Angular search UI and results

**Objective:** the search form, results list with the required columns, client-side sorting,
loading/empty states, and the total-vs-per-person price distinction — everything in §3.1 and
§3.2 of the brief, using only `GET /api/airports` and `POST /api/flights/search`.

**Files created (all under `frontend/skyroute-app/src/app/`):**
```
core/models/airport.ts
core/models/search-request.ts
core/models/search-response.ts
core/services/airport.service.ts
core/services/flight.service.ts
core/state/search-state.ts
features/search/search-form.ts (+html/scss)
features/results/results-list.ts (+html/scss)
features/results/sort-toolbar.ts (+html/scss)
features/results/empty-state.ts
shared/pipes/duration.ts
app.routes.ts   (modified)
environments/environment.ts
```

**Tasks in order:**
1. `environments/environment.ts` — `apiUrl: 'http://localhost:5xxx/api'` (match the WebApi's
   launch profile port).
2. Models: `Airport`, `SearchRequest`, `FlightOfferView`, `SearchResponse` (mirroring the backend
   DTOs field-for-field, including `isInternational` on the response).
3. `core/services/airport.service.ts` — `HttpClient.get<Airport[]>` to `GET /api/airports`,
   exposed once and cached in a signal (loaded on app/search-page init).
4. `core/services/flight.service.ts` — `HttpClient.post<SearchResponse>` to
   `POST /api/flights/search`.
5. `core/state/search-state.ts` — signal-based service holding: `airports`, `criteria`,
   `searchId`, `results` (`FlightOfferView[]`), `isInternational`, `sortMode`, `selectedOfferId`,
   `loading`, `error`. Exposes a `computed()` `sortedResults` over `results` + `sortMode`.
6. `features/search/search-form.ts` — reactive form: origin/destination `<select>` (from
   `airports()`), departure date, passengers (1–9, numeric input or select), cabin class
   `<select>`. Validators: required fields, `origin !== destination` as a form-level validator,
   passenger range. On submit: set `loading = true`, call `FlightService.search()`, populate
   `SearchState`, navigate to `/results`.
7. `features/results/empty-state.ts` — shown when `sortedResults().length === 0`.
8. `features/results/sort-toolbar.ts` — buttons/select for: Price ↑, Price ↓, Duration
   (shortest first), Departure time. Writes to `SearchState.sortMode` signal only — no service
   call, no navigation.
9. `features/results/results-list.ts` — table/list bound to `sortedResults()`. Each row: provider,
   flight number, departure, arrival, duration (via `shared/pipes/duration.ts`), cabin class,
   **total price as the primary figure**, `USD {{ pricePerPassenger }} per person` as secondary
   muted text below it. Loading spinner bound to `SearchState.loading`. Row click sets
   `selectedOfferId` and navigates to `/booking/:flightId`.
10. `app.routes.ts` — `/search` → search-form, `/results` → results-list, `/booking/:flightId` →
    placeholder until Phase 7.

**Dependencies on previous phases:** Phase 5 (both endpoints must be live and CORS-enabled).

**Relevant tests (Vitest):**
- Sort function/computed: correct ordering for all four modes, pure — asserted directly on a
  fixed array of offers, no `HttpClient` involved.
- Search form validator: rejects `origin === destination`, rejects passenger count outside 1–9.

**Definition of done:** a user can pick origin/destination/date/passengers/cabin, submit, see a
loading indicator, then a results table with total price primary + per-person secondary; sorting
the results by any of the four modes does not trigger a new HTTP call (verified in dev tools
Network tab); searching ORD→FCO shows the empty state.

**Estimated time:** 40 min

---

## Phase 7 — Booking flow (frontend)

**Objective:** the booking screen — flight summary, price breakdown, dynamic passenger form
with the label/validator that switches on route type, and the Confirm Booking action — covering
§3.3 of the brief on the client side. The actual server-side validation is Phase 8; this phase
assumes `POST /api/bookings` exists per the agreed contract and builds the UI against it.

**Files created:**
```
core/models/booking.ts
core/services/booking.service.ts
shared/validators/document-number.ts
features/booking/booking-summary.ts (+html/scss)
features/booking/passenger-form.ts (+html/scss)
features/booking/confirmation.ts (+html/scss)
core/guards/has-selected-offer.guard.ts
app.routes.ts   (modified)
```

**Tasks in order:**
1. `shared/validators/document-number.ts` — a validator factory
   `documentNumberValidator(isInternational: boolean): ValidatorFn` mirroring the exact backend
   regexes (`^[A-Z]{1,2}[0-9]{6,7}$` passport / `^[0-9]{9}$` national ID) — for immediate UX
   feedback only; the backend remains authoritative (Phase 8).
2. `core/guards/has-selected-offer.guard.ts` — a `CanActivate` guard on `/booking/:flightId`
   that redirects to `/search` when `SearchState.selectedOfferId` (or `searchId`) is empty —
   prevents a blank screen if the booking route is hit directly or refreshed.
3. `core/models/booking.ts` — `PassengerFormValue`, `BookingRequest`, `BookingResponse`
   mirroring the backend DTOs (`searchId`, `flightId`, `passengers[]` — no price, no route data,
   no `isInternational` sent by the client, per the approved price-integrity design).
4. `core/services/booking.service.ts` — `HttpClient.post<BookingResponse>` to
   `POST /api/bookings`; surfaces a typed error so the component can distinguish `409` from
   other failures.
5. `features/booking/booking-summary.ts` — reads the selected offer + criteria from
   `SearchState`: route, provider, times, cabin class.
6. `features/booking/passenger-form.ts` — `FormArray` with one `FormGroup` per passenger
   (`passengerCount` from `SearchState`), fields: full name, email, document number. Label reads
   **"Passport Number"** or **"National ID"** from `SearchState.isInternational()` (the value the
   backend already computed and returned in the search response — not recalculated client-side
   from raw country codes, avoiding a second source of truth). The document number field's
   validator is built via `documentNumberValidator(isInternational)`.
7. Price breakdown block: per-passenger price × passenger count = total, reusing the same
   values already held in `SearchState` (no recomputation on the client).
8. Confirm Booking button → calls `BookingService.book()` with `{ searchId, flightId,
   passengers }`. On success, navigate to the confirmation view with the booking reference. On
   `409`, show a "fares have changed, please search again" banner with a button back to
   `/search` that keeps the previous search criteria pre-filled.
9. `features/booking/confirmation.ts` — displays `bookingReference`, flight summary, total
   price.

**Dependencies on previous phases:** Phase 6 (`SearchState`, results navigation must exist so
there is a selected offer to book). Can be built in parallel with Phase 8 once the
`BookingRequestDto`/`BookingResponseDto` contract from Phase 3 is frozen (it already is).

**Relevant tests (Vitest):**
- `document-number.ts` validator factory: passport rule accepts/rejects correctly; national ID
  rule accepts/rejects correctly; switching `isInternational` swaps which rule is active.
- Guard: redirects to `/search` when no offer is selected.

**Definition of done:** selecting a flight from results navigates to a booking screen showing
the correct label ("Passport Number" vs "National ID") for the route just searched; submitting
a valid form calls the backend and shows a booking reference on success; refreshing
`/booking/:flightId` directly (no prior search) redirects to `/search` instead of breaking.

**Estimated time:** 35 min

---

## Phase 8 — Backend booking validation and confirmation

**Objective:** implement the authoritative side of the booking flow: `POST /api/bookings` fully
validating and pricing server-side, with the exact sequence agreed in `02-revision.md` §1/§2.
This is the phase where booking price integrity and document-rule enforcement — the two most
scrutinised business rules in the brief — become real, tested server code.

**Files created/modified:**
```
SkyRoute.Infrastructure/Data/InMemoryBookingStore.cs
SkyRoute.Application/Services/BookingService.cs      (completed)
SkyRoute.WebApi/Controllers/BookingsController.cs
```

**Tasks in order:**
1. `Infrastructure/Data/InMemoryBookingStore.cs` implementing `IBookingStore` over a
   `ConcurrentDictionary<string, Booking>`; reference generation `SR-XXXXXX` (6 random
   uppercase alphanumeric chars), retrying on collision (astronomically unlikely, but cheap to
   guard).
2. Complete `BookingService.BookAsync(BookingRequestDto request)`, in this exact order (per
   `02-revision.md` §"POST /api/bookings — Server sequence"):
   1. `ISearchOfferCache.Get(request.SearchId)` → `null` ⇒ throw `OfferExpiredException` (409).
   2. Find `request.FlightId` inside the cached offers → not found ⇒ throw
      `FlightNotFoundException` (404).
   3. `request.Passengers.Count == cachedSearch.Criteria.PassengerCount` → mismatch ⇒ throw a
      validation exception (400).
   4. For each passenger, validate `DocumentNumber` via
      `DocumentValidator.IsValid(passenger.DocumentNumber, cachedSearch.Criteria.IsInternational)`
      → any failure ⇒ 400 with per-passenger field errors (which passenger index, which field).
   5. `totalPrice = offer.PricePerPassenger * cachedSearch.Criteria.PassengerCount`.
   6. Build the `Booking` entity (snapshot of the offer + passengers + total), generate the
      reference, persist via `IBookingStore.Save`.
   7. Map to `BookingResponseDto` and return.
3. `Controllers/BookingsController.cs` — `POST /api/bookings` delegating to `BookingService`;
   exception middleware (Phase 5) already maps the thrown exceptions to 400/404/409.
4. Optional: `GET /api/bookings/{reference}` → `IBookingStore.FindByReference`, 404 if missing.
   Explicitly beyond the brief's scope; included only because it is ~8 lines and lets the
   confirmation page survive a refresh.
5. Manual verification: book a valid JFK→LHR itinerary with a correct passport number → 200 +
   reference; retry with a national-ID-shaped document on the same international route → 400;
   book using a `searchId` that has expired/never existed → 409; book with a mismatched
   passenger count → 400.

**Dependencies on previous phases:** Phase 4 (`ISearchOfferCache` populated by real searches),
Phase 5 (`FlightsController` must have run at least one search to populate the cache before a
booking can be tested end-to-end), Phase 3 (`BookingRequestDto`/`BookingResponseDto`), Phase 2
(`DocumentValidator`, `Booking` entity).

**Relevant tests:**
- Valid international booking with a correctly-formatted passport succeeds.
- Valid domestic booking with a correctly-formatted national ID succeeds.
- International booking with a national-ID-shaped document fails with 400.
- Domestic booking with a passport-shaped document fails with 400.
- Booking against an unknown/expired `searchId` returns 409.
- Booking with `passengers.Count != passengerCount` returns 400.
- `totalPrice` in the response equals `pricePerPassenger × passengerCount` from the cached
  offer, never from anything the client sent.

**Definition of done:** the full search → book round trip works end-to-end via curl/Postman
for both a domestic and an international route; all 7 tests above pass; no code path accepts a
price or an `isInternational` flag from the request body.

**Estimated time:** 30 min

---

## Phase 9 — Tests

**Objective:** consolidate and fill any gap in the test suites that Phases 2–8 already
introduced tests for incrementally. This phase is a checkpoint, not a from-scratch writing
exercise — most tests should already exist by now; this is where missing ones are added and the
full suite is run once, green, before moving to Phase 10.

**Files (in `SkyRoute.Tests/`, mirroring source namespaces):**
```
Domain/DocumentValidatorTests.cs
Domain/FlightSearchCriteriaTests.cs
Infrastructure/GlobalAirProviderTests.cs
Infrastructure/BudgetWingsProviderTests.cs
Application/FlightSearchServiceTests.cs
Application/BookingServiceTests.cs
```
Frontend, under `frontend/skyroute-app/src/app/`:
```
features/results/sort.spec.ts (or wherever the sort computed/function lives)
shared/validators/document-number.spec.ts
```

**Tasks in order (priority order — if time runs out, stop after the number reached):**
1. GlobalAir pricing: +15%, 2-decimal rounding, an away-from-zero midpoint case.
2. BudgetWings pricing: −10%, $29.99 floor, discount applied to base fare only.
3. Cabin multiplier applied before the provider rule, for both providers.
4. `DocumentValidator`: passport accept/reject, national ID accept/reject (4 cases).
5. `FlightSearchService` aggregation: total = per-passenger × count; one provider throwing does
   not fail the whole search.
6. Zero-result search (uncovered route) returns an empty list, HTTP 200, not an error.
7. `BookingService`: 409 on expired/unknown `searchId`; 400 on passenger-count mismatch; 400 on
   wrong document format for the route type; success path returns the server-computed total.
8. Frontend: sort function pure and correct for all four modes.
9. Frontend: document-number validator factory switches rule by `isInternational`.
10. *(Optional, only if time remains)* one `WebApplicationFactory` integration test covering a
    full search → book happy path end-to-end through real HTTP, in-process.

**Dependencies on previous phases:** all of Phases 2–8 (this phase tests what they built).

**Definition of done:** `dotnet test` is green for items 1–7 at minimum; `npm test` (Vitest) is
green for items 8–9; any test skipped due to time is explicitly listed in the README's
"trade-offs" section, not silently dropped.

**Estimated time:** 20 min (assuming most tests were written alongside their phase, not
deferred — this phase is a top-up, not the first time tests are written)

---

## Phase 10 — Final integration, README and acceptance review

**Objective:** one clean end-to-end run of the whole application, a complete README, and a
pass against the acceptance checklist below. No new features — this phase only fixes anything
broken by integration and documents the result.

**Files created/modified:**
```
README.md
.gitignore   (verified complete)
```

**Tasks in order:**
1. Full manual walkthrough with both apps running from a clean `git clone`-equivalent state:
   search JFK→LHR (international) → book with a valid passport → confirmation; search JFK→LAX
   (domestic) → book with a valid national ID → confirmation; search ORD→FCO → empty state;
   sort results by all four modes with the Network tab open (assert zero extra requests);
   attempt a booking after manually clearing/expiring the cache entry (or waiting past TTL) →
   409 banner.
2. Fix any integration issues surfaced by step 1 (port mismatches, CORS, enum casing,
   date/time display bugs) — no new features.
3. Write `README.md`:
   - Setup/run instructions for backend (`dotnet run --project backend/SkyRoute.WebApi`) and
     frontend (`nvm use 22.22.3`, `npm install`, `npm start`), plus how to run each test suite
     (`dotnet test`, `npm test`).
   - Architecture decisions: Clean Architecture layering (Domain/Application/Infrastructure/
     WebApi), provider abstraction (`IEnumerable<IFlightProvider>`), booking price integrity via
     `searchId` + `IMemoryCache`, `IsInternational` derived on `FlightSearchCriteria`,
     `DocumentValidator` in `Domain/Rules/`, in-memory storage, no auth, client-side sorting.
   - Assumptions: invented document regexes, USD only, airport-local times without offset,
     simulated provider latency, the deliberately uncovered ORD↔FCO route, cabin price
     multipliers.
   - Trade-offs / known limitations: no database, no auth, limited automated test coverage if
     Phase 9 was cut short, no e2e tests, no retry/circuit-breaker policies on providers, no
     pagination.
   - Future improvements: EF Core persistence, authentication, real provider integrations with
     resilience policies, pagination, i18n, e2e coverage, a proper airport search/autocomplete.
4. Final `git add`/`git commit`; confirm the repository is clean and buildable from a fresh
   checkout.

**Dependencies on previous phases:** all of them.

**Relevant tests:** none new; this phase re-runs the full suite once (`dotnet test`, `npm test`)
as a final gate before calling the challenge done.

**Definition of done:** the acceptance checklist below is fully checked; the app runs end-to-end
from a clean checkout following only the README's instructions; both test suites pass.

**Estimated time:** 20 min

---

## Recommended implementation order

Strictly sequential for Phases 1–5 (each depends on the previous one existing and compiling).
Phases 6 and 7 can interleave with 8 once the DTO contracts are frozen (they are, from Phase 3),
but the safer path in a time-boxed session is still linear:

1 → 2 → 3 → 4 → 5 → 6 → 7 → 8 → 9 → 10

Rationale: Phase 5 gives a fully working, curl-testable backend before any frontend code exists,
which de-risks the rest of the session — frontend bugs become visibly frontend-only, backend
bugs are already caught. Tests are written alongside each phase (as listed in that phase's
"Relevant tests"), with Phase 9 only as a top-up/checkpoint, not a bolt-on at the end.

---

## Time budget (~4 hours)

| Phase | Time | Running total |
|---|---|---|
| 1 — Scaffolding | 15 min | 0:15 |
| 2 — Domain | 20 min | 0:35 |
| 3 — Application/DTOs | 20 min | 0:55 |
| 4 — Providers/pricing | 35 min | 1:30 |
| 5 — Search API | 30 min | 2:00 |
| 6 — Angular search/results | 40 min | 2:40 |
| 7 — Booking flow (frontend) | 35 min | 3:15 |
| 8 — Booking validation (backend) | 30 min | 3:45 |
| 9 — Tests (top-up) | 20 min | 4:05 |
| 10 — Integration/README | 20 min | 4:25 |

Totals to ~4h25m including the test top-up and final polish — a deliberate ~10% buffer over the
brief's "3–4 hours" framing, matching `.clinerules/scope.md`'s priority order (correctness >
coverage > architecture > tests > UX > docs). If Phase 9's top-up finds nothing missing (most
tests were written inline per phase), the real total lands closer to 4h.

---

## MVP fallback if time runs short

If time pressure hits partway through, cut in this order — each cut preserves a demoable,
correct core over a polished edge:

1. **First cut — Phase 9 optional item 10** (integration test): drop the
   `WebApplicationFactory` smoke test. Zero impact on functionality.
2. **Second cut — Phase 10 polish**: skip the manual 409-expiry walkthrough demo and the
   `GET /api/bookings/{reference}` endpoint (already marked optional in Phase 8). Confirmation
   page keeps working via in-memory router state; it just won't survive a hard refresh.
3. **Third cut — Phase 7 secondary UX**: drop the `has-selected-offer` guard's redirect polish
   (leave a simple "no flight selected" message instead of a full guard+redirect) and the sort
   toolbar's dedicated styling — keep the four sort operations functional via a plain
   `<select>`, just less polished visually.
4. **Fourth cut — Phase 9 tests 8–9 (frontend)**: if backend tests (1–7) are done but time is
   critical, frontend Vitest tests can be dropped — sorting and the validator factory are simple
   enough that manual verification substitutes, and this is explicitly the lowest-priority item
   per `02-revision.md`'s test ordering.
5. **Never cut, even under severe time pressure** (these are the graded core):
   - GlobalAir/BudgetWings pricing correctness (tests 1–3) — the challenge's central business
     rule.
   - `DocumentValidator` + server-side enforcement in `BookingService` — the ⚠-flagged
     requirement in §3.3.
   - The `searchId` + cache price-integrity mechanism — removing it would mean trusting client
     data for price, which is the one thing explicitly designed against.
   - Client-side-only sorting (no refetch) — an explicit, easily-checked acceptance criterion.
   - The empty state and loading indicator — both explicitly required and easy to lose track of
     under time pressure since they are "the search worked, why bother" corner cases.

If Phase 6/7 (frontend) has to be cut short entirely, the fallback is a minimal single-page
Angular app (no routing between `/search`/`/results`/`/booking`) with three sections shown/hidden
by a signal instead of by router — still meets every functional requirement in the brief, just
with a less polished navigation model. This should be a last resort, documented explicitly as a
time-boxed simplification in the README, not a silent shortcut.

---

## Final acceptance checklist (mapped to `challenge.md`)

| # | Requirement | Brief § | Verified by |
|---|---|---|---|
| 1 | Search form: origin, destination, departure date, passengers 1–9, cabin class | §3.1 | Phase 6 |
| 2 | ≥6 airports, ≥2 countries, hardcoded | §3.1 | Phase 2/4 — 6 airports, 4 countries |
| 3 | Results show provider, flight number, departure, arrival, duration, cabin, price | §3.1 | Phase 6 |
| 4 | Total price is primary; per-passenger price visible as secondary | §3.1 ⚠ | Phase 6 |
| 5 | Sortable by price ↑/↓, duration, departure time | §3.2 | Phase 6 |
| 6 | Sorting triggers no additional API call | §3.2 | Phase 6, verified in Phase 10 |
| 7 | Loading indicator while search is in progress | §3.2 | Phase 4 (latency) + Phase 6 |
| 8 | Clear empty state when no flights match | §3.2 | Phase 4 (uncovered route) + Phase 6 |
| 9 | Booking screen: flight summary (route, provider, times, cabin) | §3.3 | Phase 7 |
| 10 | Price breakdown: per-passenger, passenger count, total | §3.3 | Phase 7 |
| 11 | Passenger form: full name, email, document number | §3.3 | Phase 7 |
| 12 | Confirm Booking → backend → booking reference | §3.3 | Phase 7/8 |
| 13 | International → "Passport Number" label + validation | §3.3 ⚠ | Phase 2 + 7 + 8 |
| 14 | Domestic → "National ID" label + validation | §3.3 ⚠ | Phase 2 + 7 + 8 |
| 15 | Backend API supports search + booking with defined contracts | §3.4 | Phases 3, 5, 8 |
| 16 | GlobalAir: base + 15%, rounded to 2 decimals | §2 | Phase 4 |
| 17 | BudgetWings: base − 10%, $29.99 floor, discount on base fare only | §2 | Phase 4 |
| 18 | Architecture supports onboarding additional providers | §2 ⚠ | `IEnumerable<IFlightProvider>`, Phase 4 |
| 19 | Working app, runs locally, frontend + backend | §5.1 | Phase 10 |
| 20 | Source in a Git repository | §5.2 | Phase 1 + Phase 10 |
| 21 | README: setup/run, architecture decisions, trade-offs | §5.3 | Phase 10 |
| 22 | Any incomplete part documented in README | §5 note | Phase 10 |

Every row above must be checked before calling the challenge complete. Rows 4, 6, 8, 13, 14, 16,
17 and 18 are the ones most likely to be silently missed under time pressure and are exactly the
"never cut" list above.
