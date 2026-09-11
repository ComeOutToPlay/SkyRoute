# SkyRoute — Flight Search & Booking Platform

A full-stack flight aggregation and booking system built with **.NET 10** (backend) and **Angular 22** (frontend), featuring search across multiple airline providers, dynamic passenger document validation, and server-side booking price integrity.

**Completion Status:** Phase 10 — Final integration, testing, and documentation.

---

## Quick Start

### Prerequisites

- **Node.js 22.22.3+** (use `nvm install 22.22.3 && nvm use 22.22.3`)
- **.NET SDK 10.0.400+**
- **Git**

### Backend Setup & Run

```powershell
cd backend
dotnet build
dotnet run --project SkyRoute.WebApi
```

Expected: Server listens on `http://localhost:5169`

### Frontend Setup & Run

```powershell
cd frontend/skyroute-app
npm install
npm start
```

Expected: Angular dev server on `http://localhost:4200`

Open http://localhost:4200 in a browser and search for flights.

---

## Test Suites

### Backend Tests

```powershell
cd backend
dotnet test
```

Runs **xUnit** test suite covering:
- Document validation rules (passport vs national ID)
- Pricing formulas (GlobalAir +15%, BudgetWings −10% with $29.99 floor)
- Cabin class multipliers
- Flight search aggregation and error isolation
- Booking service validation logic (document format, passenger count, cache expiry)

### Frontend Tests

```powershell
cd frontend/skyroute-app
npm test -- --watch=false
```

Runs **Vitest** test suite covering:
- Client-side sorting logic (no API refetch)
- Document number validator factory (rule switching by route type)
- Route guard (preventing direct `/booking` access without a selected offer)

---

## Architecture Decisions

### Clean Architecture (Backend)

The backend is structured in **four layers**, enforcing dependency direction from outer to inner:

```
WebApi (controllers, middleware, composition root)
  ↓
Application (services, DTOs, orchestration logic)
  ↓
Domain (entities, value objects, enums, business rules)
  ↓
Infrastructure (providers, caching, persistence)
```

**Rationale:**
- **Domain** is independent — it contains pure business logic (`DocumentValidator`, `FlightOffer`, `Booking`) with zero external dependencies.
- **Application** orchestrates domain logic and calls abstractions (`IAirportCatalog`, `ISearchOfferCache`, `IBookingStore`, `IFlightProvider`); it does not depend on any concrete implementation.
- **Infrastructure** provides the concrete implementations (mock airline providers, in-memory catalogue, cache, booking store).
- **WebApi** is the composition root and HTTP boundary layer.

**No unnecessary abstractions:** `FlightSearchService` and `BookingService` are concrete classes with a single implementation each, registered as scoped dependencies. No `IFlightSearchService` interface — it would add boilerplate with zero benefit.

### Booking Price Integrity via `searchId` + `IMemoryCache`

The client **never sends a price or route data** when booking — only `searchId`, `flightId`, and passenger details.

**Flow:**
1. `POST /api/flights/search` generates offers from the mock providers, stores `(criteria, offers)` in a 10-minute cache under a unique `searchId`, and returns that ID to the client.
2. `POST /api/bookings` references only the `searchId` and looks up the cached offer server-side to validate and price the booking.

**Security consequence:** A client cannot:
- Book a First Class offer while claiming Economy to get charged less.
- Fake the route to bypass passport validation (route type is derived from the cached criteria).
- Send a modified price (there is no price to send).

If the `searchId` is not found or has expired (10-minute TTL), the server returns **409 Conflict** (`OfferExpiredException`), which is what real travel aggregators do when a fare lapses.

### Provider Abstraction via `IFlightProvider`

```csharp
public interface IFlightProvider
{
    string ProviderName { get; }
    Task<IReadOnlyList<FlightOffer>> SearchAsync(FlightSearchCriteria criteria, CancellationToken ct);
}
```

**Consumed as:** `IEnumerable<IFlightProvider>` in `FlightSearchService`, which fans out with `Task.WhenAll`.

**Consequence:** Onboarding a third airline requires:
- One new class implementing `IFlightProvider`
- One DI registration line in `Program.cs`

No other code changes.

**Error isolation:** If one provider throws, it is logged and excluded from the results; the search does not fail entirely.

### `IsInternational` is Derived on `FlightSearchCriteria`

```csharp
public bool IsInternational => Origin.CountryCode != Destination.CountryCode;
```

This is a **computed property**, not a stored field or a client-supplied flag. It ensures:
- The route type (and thus document validation rule) cannot be faked.
- The value is consistent across all offers in a single search (derived once from criteria, not recomputed per offer).
- The frontend has one source of truth for the label/validator switch (sent in the search response as `isInternational`).

### `DocumentValidator` in `Domain/Rules/`

The document validation logic (regex patterns, normalization) lives in `Domain/Rules/DocumentValidator` — pure business logic with **zero dependencies**. This allows:
- Straightforward unit testing in isolation.
- Safe reuse from both the Application layer (server-side booking validation) and, mirrored, from the Angular validator factory (client-side UX feedback only; server remains authoritative).

### In-Memory Storage (No Database)

- **Airport Catalogue** — hardcoded in `AirportCatalog` (6 airports, 4 countries).
- **Offer Cache** — `IMemoryCache` with 10-minute TTL, backing `ISearchOfferCache`.
- **Booking Store** — `ConcurrentDictionary<string, Booking>`, persisting only for the lifetime of the process.

**Implication:** Bookings do not survive process restart. This is documented as a trade-off.

### Client-Side Sorting (No API Refetch)

Sorting (by price, duration, departure time) is a **pure `computed()`** in Angular's reactive state, bound directly to the results array. Changing the sort order triggers **zero HTTP requests** — verified in the Network tab of DevTools.

### Reactive State via Angular Signals (Frontend)

The frontend uses **Angular 22's signals** for all reactive state, managed in a centralized `SearchState` service:

- **Mutable state** — `signal<T>()` for airports, search criteria, results, sort mode, selected offer ID, loading/error flags
- **Derived state** — `computed()` for sorted results, dynamic document label (Passport vs National ID), selected offer details
- **Signal reactivity** — components read state via signal accessor calls (`searchState.results()`), triggering automatic re-renders when state changes
- **Lifecycle management** — `takeUntilDestroyed()` ensures clean unsubscription; `DestroyRef` handles component cleanup

This pattern is a modern alternative to NgRx or other external state libraries, providing:
- Type safety (signals are strongly typed)
- Developer ergonomics (no boilerplate)
- Performance (fine-grained reactivity, zero unnecessary re-renders)
- Testability (pure functions for sorting, validation, derived state)

### No Authentication

Booking requires only a `searchId` and basic passenger details. No login, no user accounts, no payment gateway.

---

## Assumptions

These are deliberate simplifications made during implementation to stay within the time box while meeting all stated requirements.

### Document Format Regexes (Invented)

The brief does not specify exact formats for passport or national ID numbers. The implementation assumes:

- **Passport Number (international):** `^[A-Z]{1,2}[0-9]{6,7}$`  
  Examples: `X1234567` (1 letter + 7 digits), `AB123456` (2 letters + 6 digits).
  
- **National ID (domestic):** `^[0-9]{9}$`  
  Examples: `123456789` (exactly 9 digits).

These are plausible formats, but not derived from any real-world ID standard. They are disjoint — no valid ID satisfies both rules — so the label/validator change is visibly meaningful during testing.

### Airport Times are Local (No Timezone Database)

Flight times (`departureTime`, `arrivalTime`) are serialized as **local to the airport** (e.g., `2025-12-01T18:30:00`, no `Z` suffix). Angular's `date` pipe renders them as-is. This avoids the complexity of a timezone database or offset computation while remaining realistic for a user navigating within their local timezone.

**Overnight arrivals** are flagged with a `+1` badge in the UI (e.g., "Arrives Dec 2, 06:45") so the date is unambiguous.

### Pricing is in USD Only

All prices are hardcoded as USD. No multi-currency conversion or exchange rates.

### Simulated Provider Latency

Providers include a `Task.Delay(400, ms)` to simulate network latency. This makes:
- The **loading indicator observable** (real mocks return in <1ms, so the spinner would flash invisibly).
- The **parallel fan-out meaningful** (total time ≈ slowest provider, not the sum).

### Deliberately Uncovered Route (ORD ↔ FCO)

The route pair ORD (Chicago) ↔ FCO (Rome) is not covered by either mock provider. This is intentional:
- It makes the **empty state reachable** and demonstrable in the UI.
- If every search returned flights, the empty state could never be tested.

### Cabin Class Multipliers (Invented)

Cabin pricing multipliers are assumed at:
- **Economy:** 1.0× (base fare)
- **Business:** 2.5× (base fare)
- **First:** 4.0× (base fare)

These are plausible but invented for the challenge.

### Offer Determinism (Seeded RNG)

Offers are generated from an RNG seeded by the search criteria (origin + destination + date + cabin). The same search within a session yields the same results. This is a defense-in-depth measure to prevent silent errors, but correctness does not depend on it.

---

## Trade-offs & Known Limitations

### No Database

Bookings are stored in a `ConcurrentDictionary` in process memory. They are **lost on process restart**. A production system would use EF Core + SQL Server (or similar) and provide booking lookups by reference.

**Workaround for Phase 10:** The confirmation page survives a page refresh because it uses in-memory router state (passing the booking object via navigation extras) and an optional `GET /api/bookings/{reference}` endpoint. Restart the backend, and the booking is gone — this is by design.

### No Authentication / Authorization

There is no login, no user identity, and no multi-user isolation. Bookings are looked up by reference number alone (anyone with the reference can view it). This is acceptable for a demo but not for production.

### Limited Automated Test Coverage

While the test suite covers the highest-value scenarios (pricing, document validation, booking logic), full end-to-end Selenium-style browser testing is out of scope. The manual Phase 10 walkthrough is the primary quality gate.

### No Error Recovery Policies

If a provider fails or times out:
- The request is logged and excluded from results (graceful degradation).
- There are no retry policies, circuit breakers, or exponential backoff.

A production system would implement resilience patterns (e.g., Polly policies).

### No Pagination

Search results for a single query are returned in full. There is no result paging or lazy loading. For a demo this is fine; a production system would paginate.

### No E2E Tests

There are no Selenium/Playwright tests. The Phase 10 manual walkthrough (with REST Client and DevTools Network verification) is the acceptance gate.

### Frontend Does Not Handle Slow Networks

The `loading` signal is shown while a request is in flight, but there is no timeout or retry logic on the client. A slow network that never responds will hang the UI indefinitely.

### Price Recalculation on Client Has Limits

When a user changes passenger count in the search form, the UI recalculates prices locally based on the rules. However, this calculation is not authoritative — the server always recomputes the total at booking time from the cached offer. Any discrepancy between client-side and server-side calculation would be caught at booking validation.

---

## Future Improvements

### 1. Database Persistence

Replace `ConcurrentDictionary` booking store with **Entity Framework Core** and SQL Server (or PostgreSQL). Bookings would survive restarts and be queryable by users or admins.

### 2. Authentication & Authorization

Add user accounts (ASP.NET Identity or external provider like Auth0), tie bookings to users, and prevent unauthorized access to other users' bookings.

### 3. Real Airline Provider Integrations

Replace mock providers with real APIs (Amadeus, Sabre, GDS) behind adapter interfaces. Implement retry policies and circuit breakers to handle provider failures gracefully.

### 4. Pagination & Infinite Scroll

Implement result paging on the backend (`take`/`skip` parameters) and lazy-load results on the frontend as the user scrolls.

### 5. Internationalization (i18n)

Localize all UI labels, validation messages, and error text. Support at least 2–3 languages (English, Spanish, French).

### 6. Payment Integration

Wire up a payment processor (Stripe, PayPal) so users actually pay for bookings. Store payment IDs and implement refund/cancellation workflows.

### 7. Airport Search Autocomplete

Replace hardcoded airport dropdowns with a searchable autocomplete (AJAX-backed) that filters by city name or airport code.

### 8. Email Confirmations

Send booking confirmation emails with reference number and itinerary to the email address provided at booking.

### 9. Offer Refresh / "Fares Changed" Recovery

When a booking fails with 409 `OfferExpired`, offer the user a one-click re-search that fills in the same criteria and shows updated fares, rather than forcing a manual re-entry.

### 10. Performance Optimization

- Implement client-side result caching (in a service) so switching back to a previous search doesn't re-fetch.
- Add HTTP caching headers (`Cache-Control`, `ETag`) to the `/api/airports` endpoint.
- Consider server-side result caching for common search routes.

### 11. Analytics

Log search volume, booking success rates, and provider performance metrics. Identify slow providers or routes with low conversion.

### 12. Mobile Responsive Design

Enhance the mobile experience with proper touch targets, viewport configuration, and potentially a native mobile app.

---

## Architecture Source Documents

For detailed rationale, decision history, and phase-by-phase implementation guidance, see:

- [`docs/02-revision.md`](docs/02-revision.md) — Approved architectural decisions, pricing formulas, API contracts, error handling.
- [`docs/03-execution-plan.md`](docs/03-execution-plan.md) — Phase-by-phase breakdown, time estimates, test priorities, MVP fallback options.
- [`challenge.md`](challenge.md) — Original business requirements from the hiring team.

---

## Verification Checklist

The following 22 acceptance criteria from the challenge brief have been verified:

| # | Requirement | Status |
|---|---|---|
| 1 | Search form: origin, destination, departure date, passengers 1–9, cabin class | ✅ |
| 2 | ≥6 airports, ≥2 countries, hardcoded | ✅ 6 airports, 4 countries |
| 3 | Results show provider, flight #, departure, arrival, duration, cabin, price | ✅ |
| 4 | Total price primary; per-passenger secondary | ✅ |
| 5 | Sortable by price ↑/↓, duration, departure time | ✅ |
| 6 | Sorting triggers no additional API call | ✅ Verified in Network tab |
| 7 | Loading indicator while search in progress | ✅ |
| 8 | Clear empty state when no flights match | ✅ ORD↔FCO tested |
| 9 | Booking screen: flight summary (route, provider, times, cabin) | ✅ |
| 10 | Price breakdown: per-passenger, count, total | ✅ |
| 11 | Passenger form: full name, email, document number | ✅ |
| 12 | Confirm Booking → backend → booking reference | ✅ |
| 13 | International → "Passport Number" label + validation | ✅ JFK↔LHR tested |
| 14 | Domestic → "National ID" label + validation | ✅ JFK↔LAX tested |
| 15 | Backend API supports search + booking | ✅ |
| 16 | GlobalAir: base + 15%, rounded to 2 decimals | ✅ Unit tested |
| 17 | BudgetWings: base − 10%, $29.99 floor | ✅ Unit tested |
| 18 | Architecture supports onboarding additional providers | ✅ `IEnumerable<IFlightProvider>` |
| 19 | Working app, runs locally, frontend + backend | ✅ |
| 20 | Source in a Git repository | ✅ |
| 21 | README: setup/run, architecture, trade-offs | ✅ This file |
| 22 | Incomplete parts documented in README | ✅ See "Trade-offs" above |

---

## Support & Questions

For questions about the architecture, see the source documents listed above. For bugs or missing features, refer to the "Future Improvements" section.

**Happy flying!** ✈️
