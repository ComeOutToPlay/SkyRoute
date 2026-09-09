# SkyRoute — Revised Implementation Plan

**Status:** approved, ready to implement
**Stack (locked):** .NET 10 (SDK 10.0.400) · Angular 22.1.7 · Node 22.22.3+ via nvm

This document supersedes `docs/implementation-plan.md`, which is kept only as history.
It folds in the corrections from the plan review plus the two stack decisions made afterwards
(.NET 10 / Angular 22) and the booking-integrity decision (`searchId` + `IMemoryCache`).

---

## 0. Prerequisites

```powershell
nvm install 22.22.3
nvm use 22.22.3
```

Angular 22 declares `engines.node = ^22.22.3 || ^24.15.0 || >=26.0.0`. Any of those works;
`nvm` handles it in one command, so this is a setup step, not a constraint on the design.

Also, at the start of implementation:

```powershell
git init          # the repo is not initialised yet (deliverable #2 requires a Git repo)
```

with a `.gitignore` covering VisualStudio + Node.

---

## 1. Key decision — booking price integrity

The client **never sends a price, nor any route data**, when booking.

- `POST /api/flights/search` generates the offers, stores them in `IMemoryCache` under a
  `searchId` (10-minute TTL), and returns that id alongside the results.
- `POST /api/bookings` references only `searchId` + `flightId`.
- Origin, destination, cabin class and passenger count are read back from **server-side state**.

Consequences:

- A client cannot book a First Class offer while claiming Economy to be charged less.
- A client cannot fake the route to bypass passport validation, because `isInternational` is
  derived from the cached criteria, not from a client flag.
- A cache miss returns **409 `OfferExpired`** — which is what real aggregators do when a fare
  lapses, and is a good talking point for the interview walkthrough.

`expectedTotalPrice` / `409 PriceMismatch` was considered and **rejected**: with no client-supplied
price there is no tampering surface left to validate, so it would be a belt over already-working
braces and extra branches to test.

The offer generator is still fully deterministic (RNG seeded from the search criteria) as
defence in depth, but correctness no longer depends on that determinism.

---

## 2. Backend — `backend/SkyRoute.Api`

Scaffold:

```powershell
dotnet new webapi -controllers -f net10.0 -n SkyRoute.Api -o backend/SkyRoute.Api
```

`-controllers` is required: the template defaults to Minimal APIs. Note that since .NET 9 the
template wires `Microsoft.AspNetCore.OpenApi` but **no Swagger UI**, so add `Scalar.AspNetCore`
(or Swashbuckle) if a browsable API surface is wanted for the demo.

### Folder layout

`Controllers/` · `Providers/` · `Services/` · `Models/` · `Data/`

Deliberately dropped from the earlier plan: the `Money` value object (`decimal` plus a
`const string Currency = "USD"` covers 100% of the need) and `IPricingStrategy` (a strategy
interface with exactly one implementation per provider is pure indirection — pricing lives
inside the provider, which is literally what the brief describes).

### `Program.cs`

- Dev CORS policy for `http://localhost:4200` — without it every frontend call fails.
- `JsonStringEnumConverter` + case-insensitive binding; wire values `Economy | Business | First`,
  with the UI mapping `First` to the "First Class" label. Unknown value ⇒ 400, not 500.
- ProblemDetails (RFC 7807) as the single error contract.
- `IMemoryCache` registration.
- HTTP only in dev, to avoid dev-certificate friction.

### Provider abstraction

```csharp
public interface IFlightProvider
{
    string ProviderName { get; }
    Task<IReadOnlyList<ProviderFlightOffer>> SearchAsync(
        FlightSearchCriteria criteria, CancellationToken ct);
}
```

Registered via DI and consumed as `IEnumerable<IFlightProvider>`, so onboarding a third airline
is one new class and one registration line — nothing else changes. `FlightSearchService` fans out
with `Task.WhenAll`, isolates and logs per-provider failures (one provider throwing must not fail
the whole search), and normalises everything into a single DTO shape.

### Endpoints

**`GET /api/airports`** → `[{ code, city, country, countryCode }]`

Exists so the airport catalogue (and especially `countryCode`) is defined once, server-side.
The frontend needs country data to choose Passport vs National ID; duplicating it in the Angular
app would let the two drift apart on the single most closely-reviewed requirement.
"Hardcode ≥6 airports" is still satisfied — they are hardcoded, in the backend.

**`POST /api/flights/search`**

Request:

```json
{ "origin": "JFK", "destination": "LHR", "departureDate": "2025-12-01",
  "passengers": 2, "cabinClass": "Economy" }
```

Response:

```json
{
  "searchId": "b3f1c2a4-...",
  "passengerCount": 2,
  "currency": "USD",
  "flights": [
    {
      "id": "globalair-GA123-20251201-Economy",
      "provider": "GlobalAir",
      "flightNumber": "GA123",
      "origin": "JFK",
      "destination": "LHR",
      "departureTime": "2025-12-01T18:30:00",
      "arrivalTime": "2025-12-02T06:45:00",
      "durationMinutes": 495,
      "cabinClass": "Economy",
      "pricePerPassenger": 320.50,
      "totalPrice": 641.00
    }
  ]
}
```

`passengerCount`, `currency` and `searchId` live at the response root rather than being repeated
on every row. No server-side ordering — sorting is 100% client-side per the brief. Note the
flight `id` encodes cabin class, so it can never be confused with the same flight in another cabin.

**`POST /api/bookings`**

```json
{
  "searchId": "b3f1c2a4-...",
  "flightId": "globalair-GA123-20251201-Economy",
  "passengers": [
    { "fullName": "Jane Doe", "email": "jane@example.com", "documentNumber": "X1234567" }
  ]
}
```

Server sequence:

1. Resolve `searchId` in cache → miss ⇒ **409 `OfferExpired`**.
2. Find `flightId` within that search → missing ⇒ **404**.
3. Assert `passengers.Count == passengerCount` **from the cached search** ⇒ 400 otherwise.
4. Derive `isInternational` by comparing the cached origin/destination `countryCode`s; apply the
   passport or national-ID rule accordingly ⇒ 400 with field-level errors otherwise.
5. Recompute `totalPrice = pricePerPassenger × passengerCount` from the cached offer.
6. Return `{ bookingReference, status, flightSummary, pricePerPassenger, passengerCount,
   totalPrice, currency }`.

Bookings stored in a `ConcurrentDictionary<string, Booking>`. Reference format `SR-XXXXXX`.

**`GET /api/bookings/{reference}`** — not requested by the brief. Kept anyway because it is ~8
lines and makes the confirmation page survive a refresh. Explicitly flagged as beyond scope.

### Pricing rules

```
base        = routeFare × cabinMultiplier          // Economy 1.0 · Business 2.5 · First 4.0
GlobalAir   = Math.Round(base × 1.15m, 2, MidpointRounding.AwayFromZero)
BudgetWings = Math.Max(Math.Round(base × 0.90m, 2, MidpointRounding.AwayFromZero), 29.99m)
total       = pricePerPassenger × passengerCount   // no second rounding
```

- `decimal` throughout, never `float`/`double`.
- `MidpointRounding.AwayFromZero` is explicit: .NET's default `Math.Round` is banker's rounding
  (`2.345 → 2.34`), which is not what the brief's "round to 2 decimal places" means.
- Round **per passenger first**, then multiply. Rounding the total separately would make
  `total ≠ displayed per-person × count`, and the brief calls out that these are two different
  numbers the UI must distinguish clearly.
- The cabin multiplier is applied **before** the provider rule, which also keeps BudgetWings'
  "discount applies to the base fare only" unambiguous.
- The $29.99 floor is **per passenger**. At least one BudgetWings offer (short domestic hop,
  Economy) must land below it so the rule is demonstrable in the UI, not only in a unit test.

### Provider mocks

- **Route/cabin coverage map** per provider, so results genuinely vary: BudgetWings serves no
  First Class and no long-haul; one airport pair (ORD↔FCO) is served by nobody. This is what makes
  the **empty state reachable** — if every search always returns flights, the required empty state
  can never be demonstrated.
- `await Task.Delay(400, ct)` of simulated network latency, so the **loading indicator is
  actually observable** (mocks otherwise return in under a millisecond and the spinner flashes for
  a single frame). It also makes the parallel fan-out meaningful: total time ≈ slowest provider,
  not the sum.
- Offers are generated from an RNG seeded by the search criteria, so the same search yields the
  same results within a session.

### Times

Flight times are treated as **local to the airport** and serialised **without an offset**
(`2025-12-01T18:30:00`), then rendered as-is by Angular's `date` pipe. Emitting `...Z` would make
a 18:30 Dec-1 departure display as 13:30 Nov-30 in a US browser — a silent, embarrassing bug.
`arrivalTime` is computed as `departureTime + durationMinutes` so the two can never disagree, and
a `+1` badge is shown for overnight arrivals. The "no real timezone database" simplification is
documented in the README.

### Document validation

Regexes are deliberately **disjoint**, so the rule change is *visible*, not just the label:

| Route | Label | Rule | Valid | Invalid |
|---|---|---|---|---|
| International | Passport Number | `^[A-Z]{1,2}[0-9]{6,7}$` | `X1234567` | `123456789` |
| Domestic | National ID | `^[0-9]{9}$` | `123456789` | `X1234567` |

An overlapping pair of rules (as originally planned) would let a reviewer toggle JFK→LHR vs
JFK→LAX, see the label change, and never see validation behave differently — which reads as a
half-implemented requirement. Input is normalised (trim + uppercase) before matching. The server
is authoritative and derives the route type itself; the Angular validator mirrors the same
constants purely for immediate UX feedback. Both regexes are documented as invented-but-plausible
assumptions in the README, since the brief gives no formats.

### Airport catalogue

| Code | City | Country |
|---|---|---|
| JFK | New York | US |
| LAX | Los Angeles | US |
| ORD | Chicago | US |
| LHR | London | GB |
| CDG | Paris | FR |
| FCO | Rome | IT |

6 airports across 4 countries. Domestic pairs (JFK↔LAX↔ORD) and international pairs (JFK↔LHR,
LHR↔CDG…) are both reachable, so both booking-flow document paths can be demonstrated.

---

## 3. Frontend — `frontend/skyroute-app` (Angular 22)

Scaffold:

```powershell
npx @angular/cli@22 new skyroute-app --style=scss --ssr=false --skip-git --routing
```

`--skip-git` matters: `ng new` initialises a repo and commits by default, which would nest a
second repo inside the root one.

### Angular 22 defaults that shape the code

Verified against `@schematics/angular@22.1.7`:

| Option | v22 default | Impact |
|---|---|---|
| `testRunner` | **`vitest`** | Tests are **not** Jasmine/Karma. Use `vi.fn()`, not `jasmine.createSpy()`. README must say Vitest. |
| `zoneless` | **`true`** | No `zone.js`. Signals are mandatory for change detection, not optional — this validates the signal-based state decision. |
| `fileNameStyleGuide` | **`2025`** | Files are `search-form.ts`, not `search-form.component.ts`. |
| `standalone` | `true` | No NgModules. |
| `ssr` | `false` | Explicitly disabled above. |

### Structure

```
src/app/
  core/
    models/     flight, search-request, search-response, booking, airport
    services/   airport.service.ts, flight.service.ts, booking.service.ts
    state/      search-state.ts        (signal-based)
  features/
    search/     search-form
    results/    results-list, sort-toolbar, empty-state
    booking/    booking-summary, passenger-form, confirmation
  shared/
    validators/ document-number.ts     (factory: passport vs national id)
    pipes/      duration.ts
```

### Flow

1. **`/search`** — reactive form: origin/destination (from `GET /api/airports`), departure date,
   passengers 1–9, cabin class. Cross-field validation prevents origin === destination.
2. **`/results`** — loading spinner while the request is in flight; empty state when
   `flights().length === 0`; error state on failure. Each row shows provider, flight number,
   departure, arrival, duration, cabin, **total price as the primary figure** with
   `USD x.xx per person` as muted secondary text — the brief is explicit that these are two
   different numbers and the UI must make the distinction clear.
   Sort toolbar: price ↑/↓, duration (shortest first), departure time — implemented as a
   `computed()` over the stored results, sorting on `totalPrice`. **No refetch on sort change.**
3. **`/booking/:flightId`** — flight summary card, price breakdown
   (per-passenger × passenger count = total), and a `FormArray` with one group per passenger
   (full name, email, document number). Label **and** validator switch on route type.
   A **route guard** redirects to `/search` when no offer is selected in state, so refreshing the
   booking page cannot produce a blank screen.
4. **Confirmation** — booking reference on success. On `409 OfferExpired`, show a
   "fares have changed, please search again" banner with a button back to `/search` that keeps
   the previous criteria.

### State

A signal-based `SearchState` service holding criteria, `searchId`, results, selected offer and
sort choice. This survives navigation between routes without refetching. **No NgRx** — a single
linear flow does not justify it, and the scope rules explicitly warn against unnecessary
abstractions. `HttpClient` services wrap the three endpoints.

---

## 4. Tests

Ordered by value, so that if time runs out the cut falls on the least important item.

**Backend — xUnit (`backend/SkyRoute.Api.Tests`)**

1. GlobalAir pricing: +15%, 2-decimal rounding, away-from-zero midpoint cases.
2. BudgetWings pricing: −10%, the $29.99 floor, discount applied to the base fare only.
3. Cabin multiplier applied before the provider rule.
4. Document validator: passport accept/reject, national ID accept/reject (4 cases).
5. Search aggregation: total = per-passenger × count; one provider throwing does not fail the search.
6. Zero-result search (uncovered route) returns an empty list, not an error.
7. Booking with an expired/unknown `searchId` returns 409.

**Frontend — Vitest**

8. Sort function is pure and correct for all four modes (no `HttpClient` involvement).
9. Document validator factory switches rule by route type.

One `WebApplicationFactory` smoke test only if everything above is green. Writing five polished
test classes while features remain half-built would be the wrong trade in a 3–4 hour box.

---

## 5. README

- Setup and run instructions for both apps, including `nvm use 22.22.3`, and how to run each test suite.
- Architecture decisions: provider abstraction, offer cache and price integrity, in-memory storage,
  no auth, client-side sorting.
- Assumptions: invented document regexes, USD only, airport-local times, simulated latency, the
  deliberately uncovered route.
- Trade-offs / known limitations: no database, no auth, no e2e tests, no retry/circuit-breaker
  policies, no pagination.
- Future improvements: EF Core persistence, authentication, real provider integrations with
  resilience policies, pagination, i18n, e2e coverage.

---

## 6. Budget

| Phase | Time |
|---|---|
| Backend | 90 min |
| Frontend | 90 min |
| Tests | 40 min |
| README + polish | 20 min |

---

## 7. Requirement traceability

| Brief | Where it is satisfied |
|---|---|
| §3.1 form fields | `/search` reactive form |
| §3.1 ≥6 airports, ≥2 countries | `AirportsCatalog` — 6 airports, 4 countries |
| §3.1 result columns | results-list |
| §3.1 total vs per-person | total primary, per-person secondary; rounded per passenger then multiplied |
| §3.2 four sort modes | `computed()` sort toolbar |
| §3.2 no API call on sort | sorting runs purely over stored signal state |
| §3.2 loading indicator | spinner + 400 ms simulated provider latency |
| §3.2 empty state | reachable via the uncovered ORD↔FCO route |
| §3.3 flight summary | booking-summary component |
| §3.3 price breakdown | per-passenger × count = total |
| §3.3 passenger form | `FormArray`, one group per passenger |
| §3.3 booking reference | `POST /api/bookings` → `SR-XXXXXX` |
| §3.3 passport vs national ID | disjoint regexes; label + validator switch; enforced server-side |
| §3.4 backend API | 4 endpoints, ProblemDetails, documented contracts |
| §2 pricing rules | inside each provider, unit-tested |
| §2 future providers | `IEnumerable<IFlightProvider>` via DI — new provider = 1 class + 1 registration |
| §5 deliverables | local run, git repo, README |





