I've read `challenge.md` fully and confirmed the environment (empty `backend/` and `frontend/` folders, .NET SDKs 8/9/10 available, Node 22, npm available — Angular CLI not globally installed but can be used via `npx` or installed as dev dependency). Below is my proposed implementation plan.

---

# Proposed Architecture

## Repo Layout
```
skyroute/
  backend/
    SkyRoute.Api/                 # ASP.NET Core Web API (single project, .NET 8 LTS)
      Controllers/
        FlightsController.cs      # POST /api/flights/search
        BookingsController.cs     # POST /api/bookings, GET /api/bookings/{ref}
      Providers/
        IFlightProvider.cs        # provider abstraction
        GlobalAirProvider.cs      # mock + pricing rule
        BudgetWingsProvider.cs    # mock + pricing rule
      Aggregation/
        FlightSearchService.cs    # fan-out to providers, aggregate, map to DTOs
      Domain/
        Airport.cs, CabinClass.cs, Flight.cs, Money.cs, Booking.cs
      Validation/
        DocumentValidator.cs      # passport vs national ID rules, per "route type"
      Dtos/
        SearchRequest/Response, BookingRequest/Response DTOs + FluentValidation validators
      Data/
        AirportsCatalog.cs        # static hardcoded list (>=6 airports, >=2 countries)
        InMemoryBookingStore.cs   # simple in-memory store (ConcurrentDictionary)
    SkyRoute.Api.Tests/           # xUnit test project
      Providers/*PricingTests.cs
      Validation/DocumentValidatorTests.cs
      Services/FlightSearchServiceTests.cs
      Controllers/*ControllerTests.cs (WebApplicationFactory, optional if time allows)
  frontend/
    skyroute-app/                 # Angular (standalone components, Angular 17/18 style)
      src/app/
        core/
          models/ (flight, search-request, booking, airport)
          services/ (flight.service.ts, booking.service.ts)
        features/
          search/ (search-form component)
          results/ (results-list component, sort toolbar, empty/loading states)
          booking/ (booking-summary, passenger-form, confirmation)
        shared/
          pipes (duration, currency helpers)
          validators (document-number validator factory: passport vs national id)
      environments/ (apiUrl)
  README.md                       # setup, architecture rationale, trade-offs
  challenge.md
```

## Backend Design

### Provider Abstraction (extensible for future airlines)
```csharp
public interface IFlightProvider
{
    string ProviderName { get; } // "GlobalAir", "BudgetWings"
    Task<IReadOnlyList<ProviderFlightOffer>> SearchAsync(FlightSearchCriteria criteria, CancellationToken ct);
}
```
- Each provider mock generates a small set of deterministic-but-varied flight offers (based on origin/destination/date) with a **base fare**, flight number, times, duration.
- Each provider implementation applies **its own pricing rule internally** (or via a small `IPricingStrategy` injected per provider) so the rule lives next to the provider that owns it — this satisfies "each provider applies its own rules" and keeps it trivial to add a 3rd provider with a different rule, just by implementing `IFlightProvider`.
- `FlightSearchService` calls all registered providers in parallel (`Task.WhenAll`), catches/logs per-provider failures without failing the whole search (resilience for future real integrations), normalizes results into a common `FlightOfferDto`, and returns to the controller. Providers are registered via DI (`IEnumerable<IFlightProvider>`), so adding a provider = register one more class, no other code changes.

### Pricing Rules (unit-tested precisely)
- **GlobalAir**: `finalPricePerPassenger = round(baseFare * 1.15, 2)`
- **BudgetWings**: `discounted = baseFare * 0.90`; `finalPricePerPassenger = max(discounted, 29.99)` — discount applies to base fare only, per spec.
- **Total price** = `finalPricePerPassenger * passengerCount` (computed once in the aggregation layer so the "total vs per-person" distinction is explicit and consistent for all providers — avoids duplicating passenger multiplication inside each provider).
- I will encapsulate rounding/money math using `decimal` (never float/double) to avoid precision bugs — important business-rule correctness point.

### API Contracts (draft)

**POST `/api/flights/search`**
Request:
```json
{
  "origin": "JFK",
  "destination": "LHR",
  "departureDate": "2025-12-01",
  "passengers": 2,
  "cabinClass": "Economy"
}
```
Response:
```json
{
  "flights": [
    {
      "id": "globalair-GA123-2025-12-01",
      "provider": "GlobalAir",
      "flightNumber": "GA123",
      "origin": "JFK",
      "destination": "LHR",
      "departureTime": "2025-12-01T18:30:00Z",
      "arrivalTime": "2025-12-02T06:45:00Z",
      "durationMinutes": 495,
      "cabinClass": "Economy",
      "pricePerPassenger": 320.50,
      "totalPrice": 641.00,
      "currency": "USD",
      "passengers": 2
    }
  ]
}
```
(No sorting applied server-side — the array order is arbitrary/stable; sorting is 100% client-side per spec.)

**POST `/api/bookings`**
Request:
```json
{
  "flightId": "globalair-GA123-2025-12-01",
  "searchSnapshot": { "origin": "JFK", "destination": "LHR", "passengers": 2, "cabinClass": "Economy", "departureDate": "2025-12-01" },
  "passengers": [
    { "fullName": "Jane Doe", "email": "jane@example.com", "documentNumber": "X1234567" }
  ]
}
```
- Backend re-validates document number format server-side based on whether origin/destination countries differ (never trust client-only validation), re-derives price server-side (never trust client-submitted price) by re-invoking the same provider/pricing logic, and returns:
```json
{
  "bookingReference": "SR-8F3K2A",
  "status": "Confirmed",
  "totalPrice": 641.00,
  "currency": "USD"
}
```
- Booking stored in-memory (`ConcurrentDictionary<string, Booking>`) — no DB needed for this scope; I'll note this as a limitation/future improvement (persistence) in the README.

⚠ Key design decision: since flight offers are generated fresh per search (mocked, not persisted), the booking request needs enough info to **re-derive** the exact flight/price server side. I'll include the minimal `searchSnapshot` + `flightId` (deterministically encodes provider+flightNumber+date+cabin) so the backend can regenerate the same offer deterministically and validate the price wasn't tampered with. This is a nice correctness/security touch worth calling out in the walkthrough.

### Passport vs National ID Validation
- Determine "international" by comparing `Airport.CountryCode` for origin vs destination (airports catalog holds country codes).
- Validation rules (documented assumption, since spec doesn't specify exact formats):
  - **National ID**: alphanumeric, 5–15 chars (adjustable per "country" if we want realism, but I'll keep one general rule for simplicity, and note this as a simplification).
  - **Passport Number**: typically 6–9 alphanumeric characters (common real-world pattern) — I'll implement a reasonably standard rule (e.g., `^[A-Z0-9]{6,9}$`, case-insensitive) and document it as an assumption.
- Validator will be a shared piece of logic (`DocumentValidator`) used both in a FluentValidation validator (backend) — this is the authoritative check — and mirrored in an Angular custom validator factory (frontend, same regexes) for immediate UX feedback. I'll centralize the regex constants so they're easy to keep in sync (e.g., a small shared constants file used to generate both, or just clearly documented duplication since there's no shared package between Angular/.NET in this scope).

## Frontend Design (Angular, standalone components, signals or RxJS — I'll use standalone components + Angular signals for state where simple, RxJS for the service/API layer)

### Flow / Pages
1. **Search page** (`/search`): reactive form (origin, destination select, date picker, passenger count 1–9, cabin class select). Origin/destination use the same hardcoded airport list; validation prevents origin === destination.
2. **Results page/section**: 
   - Loading spinner while `POST /flights/search` in flight.
   - Empty state component if `flights.length === 0`.
   - Results table/list with columns: provider, flight number, departure, arrival, duration, cabin, **total price (primary)** + **per-person price (secondary, smaller/muted text)**.
   - Sort toolbar (buttons or a select): Price ↑/↓, Duration (shortest first), Departure time — implemented via a pure client-side computed/sorted array (Angular `computed()` signal or a pipe), no re-fetch.
3. **Booking page**: on flight selection, navigate to `/booking/:flightId` (with state passed via a small in-memory "selected flight" service/store, or router state) showing:
   - Flight summary card (route, provider, times, cabin).
   - Price breakdown (per-passenger price × passenger count = total).
   - Dynamic passenger form (`FormArray`, one group per passenger count) with full name, email, document number — label + validator switch based on `isInternational` flag computed from the search's origin/destination countries.
   - Confirm Booking button → `POST /api/bookings` → on success show confirmation screen with booking reference; on failure show inline error.

### State Management
- Given the scope (single flow, no complex global state), I'll avoid NgRx and use:
  - A small `SearchStateService` (Angular injectable, signal-based) holding: last search criteria, last search results, sort selection — enough to support "search → results → booking → confirmation" navigation without re-fetching, and to survive component destruction/recreation between routes.
  - HttpClient-based `FlightService` / `BookingService` wrapping the two endpoints.
- This is a deliberate "no unnecessary abstractions" choice per your scope-discipline rule — NgRx would be overkill here.

## Testing Strategy
- **Backend (xUnit)** — prioritized since pricing/validation are the core business rules:
  - `GlobalAirPricingTests`: base fare → +15%, rounding to 2 decimals (including edge cases like values needing round-half-up).
  - `BudgetWingsPricingTests`: −10% discount, $29.99 floor enforcement, discount applied to base fare only (not to some already-discounted value).
  - `DocumentValidatorTests`: international vs domestic route detection, passport regex accept/reject cases, national ID regex accept/reject cases.
  - `FlightSearchServiceTests`: aggregates offers from multiple fake `IFlightProvider`s, computes total = per-passenger × passengers correctly, handles a provider throwing without failing entire search.
  - Optionally 1–2 controller-level tests via `WebApplicationFactory` for the happy path of search + booking if time permits.
- **Frontend (Jasmine/Karma or Jest, whichever Angular CLI defaults to)** — lighter touch given time budget:
  - Sorting logic unit tests (price asc/desc, duration, departure time) as a pure function/service — easy to test, high value.
  - Document-type validator unit tests (passport vs national ID switch logic).
  - Maybe 1 component test for the booking form label/validator switching if time allows.
- I will explicitly note in the README any testing gaps left due to the 3–4 hour time-box (e.g., no e2e tests, limited controller/component test coverage) as an acknowledged trade-off, per your scope-discipline rule.

## Hardcoded Airports (≥6, ≥2 countries) — draft list
| Code | City | Country |
|---|---|---|
| JFK | New York | US |
| LAX | Los Angeles | US |
| ORD | Chicago | US |
| LHR | London | GB |
| CDG | Paris | FR |
| FCO | Rome | IT |

International = origin.Country != destination.Country. Domestic pairs available (JFK↔LAX↔ORD), international pairs available (JFK↔LHR, etc.) so both booking-flow paths are exercisable.

## Documentation (README)
- Setup/run instructions for backend (`dotnet run`) and frontend (`npm start` / `ng serve`), plus how to run tests for both.
- Architecture decision summary (provider abstraction, no persistence/in-memory store, no auth, client-side sort, price re-derivation on booking).
- Trade-offs/limitations section: no DB, no auth, simplified document regexes (documented as assumption), no e2e tests, no live airport API, minimal error-handling/retry policies for provider calls, no pagination.
- "Future improvements" section per your scope-discipline note: persistence (EF Core + real DB), auth, real provider integrations w/ retries & circuit breakers, i18n, richer airport data/autocomplete.

## Assumptions I'm making (will confirm or just document if no objection)
1. **.NET 8** LTS for the backend (available on this machine alongside .NET 9/10) — stable choice for a "senior" submission; will use `dotnet new webapi` minimal-hosting style but with Controllers (not minimal APIs) for clearer separation of concerns and easier to demo in interview. Open to using .NET 9 instead if you prefer newest.
2. Angular latest CLI (I'll scaffold via `npx @angular/cli@latest new` — will pick whatever current stable Angular version npx resolves, standalone components, no Angular Universal/SSR).
3. In-memory storage only (no database) — matches scope-discipline guidance to avoid cloud/deployment overhead; I'll call this out as a limitation.
4. Currency fixed to USD for all providers (spec doesn't require multi-currency).
5. Document number regex rules are a reasonable invented standard since the spec doesn't give exact formats — documented as an assumption in README.
6. No authentication/authorization (explicitly out of scope per your rules).

---

If this plan looks good, let me know if you'd like any adjustments (e.g., .NET version, NgRx vs signal-based state, minimal APIs vs controllers, or specific document validation rules), then toggle to Act mode and I'll implement it end-to-end.