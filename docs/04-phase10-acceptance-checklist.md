# SkyRoute — Phase 10 Manual Intervention Checklist

**Purpose:** Phase 10 is the final integration, testing, and documentation phase. This checklist itemizes every manual step required to complete the challenge, specifies where it must be performed, and identifies whether the REST Client extension can assist.

**Status:** Pending execution after all prior phases are complete (all code committed, both test suites green).

---

## Overview

Phase 10 has four main objectives:

1. **Full manual walkthrough** (§1): execute real user journeys and verify end-to-end behavior across international/domestic/empty-state scenarios, plus cache-expiry handling.
2. **Fix integration issues** (§2): triage and repair any bugs found in step 1.
3. **Write README.md** (§3): document setup, architecture, assumptions, and trade-offs.
4. **Final git status** (§4): prepare the repository for submission.

Each step below is classified by the tool/environment required to execute it.

---

## § 1 — Full Manual Walkthrough

This section requires both the REST Client extension and browser UI interaction. Each scenario tests a specific requirement from the acceptance checklist.

### Scenario A: International Flight Search and Booking (JFK → LHR)

**Objective:** Verify a complete international flight search, booking, and confirmation flow with Passport Number validation.

#### Step A1: Start both applications

**Where:** Terminal (PowerShell)

**What to do:**

1. Open two terminal windows (or tabs).
2. In terminal 1, navigate to the backend directory and start the API:
   ```powershell
   cd c:\epam\skyroute
   dotnet run --project backend/SkyRoute.WebApi
   ```
   Wait for the message: `Now listening on: http://localhost:5169` (or similar port).
3. In terminal 2, navigate to the frontend and start Angular dev server:
   ```powershell
   cd c:\epam\skyroute\frontend\skyroute-app
   nvm use 22.22.3
   npm start
   ```
   Wait for: `✔ Compiled successfully` and `Local: http://localhost:4200/`.

**Expected result:** Both servers running; no errors in either console.

**REST Client capability:** N/A — this is a launch step.

---

#### Step A2: Verify `/api/airports` endpoint (quick sanity check)

**Where:** REST Client extension (VS Code or equivalent)

**What to do:**

1. Create or open a `.http` file (e.g., `phase10-walkthrough.http` in the workspace root).
2. Send a GET request:
   ```http
   GET http://localhost:5169/api/airports
   ```
3. Inspect the response.

**Expected result:**
- HTTP 200
- Response body contains 6 airports (JFK, LAX, ORD, LHR, CDG, FCO)
- Each airport has fields: `code`, `city`, `country`, `countryCode`
- Verify that `LHR` has `countryCode = "GB"` and `JFK` has `countryCode = "US"` (used to confirm `isInternational` is derived correctly)

**REST Client capability:** ✅ Yes — full support.

---

#### Step A3: Search for international flights (JFK → LHR)

**Where:** Browser UI (http://localhost:4200)

**What to do:**

1. Open http://localhost:4200 in a web browser.
2. Navigate to the `/search` route (should be the default landing page).
3. Fill in the search form:
   - **Origin:** JFK
   - **Destination:** LHR
   - **Departure Date:** any future date (e.g., 2025-12-01 if the system allows it, or 2026-12-01)
   - **Passengers:** 1
   - **Cabin Class:** Economy
4. Click **Search** (or equivalent submit button).
5. Wait for the results page to load. A loading spinner should briefly appear.

**Expected result:**
- Results page `/results` displayed
- A list/table of flights is shown with at least one row (GlobalAir and/or BudgetWings)
- Each row displays: provider, flight number, departure time, arrival time, duration, cabin class
- **Price is displayed as: `USD XXX.XX total` with `USD XX.XX per person` shown as secondary text below** (the ⚠ requirement from §3.1 of the brief)
- No empty state (LHR is covered by providers)
- No error banner
- Network tab (DevTools F12 → Network) shows exactly one POST request to `/api/flights/search` with a `searchId` in the response

**REST Client capability:** ✅ Partial — REST Client can send the search request and verify the JSON response structure, but cannot verify UI rendering (price label distinction, table layout, loading indicator visibility). See alternative approach below.

**Alternative approach (if verifying via REST Client):**
```http
POST http://localhost:5169/api/flights/search
Content-Type: application/json

{
  "origin": "JFK",
  "destination": "LHR",
  "departureDate": "2025-12-01",
  "passengers": 1,
  "cabinClass": "Economy"
}
```
**Expected response:**
- Status 200
- Response includes `searchId` (a GUID)
- Response includes `isInternational: true` (at response root, not per-flight)
- Response includes `flights` array with ≥1 element
- Each flight has: `id`, `provider`, `flightNumber`, `origin`, `destination`, `departureTime`, `arrivalTime`, `durationMinutes`, `cabinClass`, `pricePerPassenger`, `totalPrice`
- Verify: `totalPrice = pricePerPassenger × passengers` (1 × pricePerPassenger = totalPrice in this case)

**Important:** Note the `searchId` from the response — it is needed for booking in step A6.

---

#### Step A4: Observe sort functionality (no extra HTTP calls)

**Where:** Browser UI (http://localhost:4200) + DevTools Network tab

**What to do:**

1. On the results page from step A3, open browser DevTools (F12).
2. Go to the **Network** tab.
3. Clear the network log (right-click → Clear or icon).
4. Click the **Sort** control and select **Price (Low to High)** (or equivalent).
5. Observe the Network tab while the sort is applied.
6. Repeat for the other three sort modes: **Price (High to Low)**, **Duration**, **Departure Time**.

**Expected result:**
- After clearing the network log in step 2, **no new HTTP requests appear** when changing sort order
- The results table reorders on the page without any XHR/Fetch calls
- This satisfies §3.2 requirement: "Sorting triggers no additional API call"

**REST Client capability:** ❌ No — REST Client cannot observe browser-side network activity or verify that zero requests are made. This requires direct browser inspection.

---

#### Step A5: Book a flight with a valid Passport Number

**Where:** Browser UI (http://localhost:4200)

**What to do:**

1. On the results page, click on any flight row to select it (or there may be a **Book** or **Select** button).
2. Navigate to the booking page (should be `/booking/:flightId` or similar).
3. Observe the **Passenger Form**:
   - The document field label should read **"Passport Number"** (not "National ID") — confirming the route is international
   - The field should accept a passport format
4. Fill in the passenger form:
   - **Full Name:** Jane Doe (or any name)
   - **Email:** jane@example.com
   - **Passport Number:** X1234567 (matches the regex `^[A-Z]{1,2}[0-9]{6,7}$`)
5. Observe the price breakdown on the booking screen:
   - Should show: per-passenger price, passenger count (1), and total price
   - Values should match those from the search results
6. Click **Confirm Booking**.
7. Wait for the backend response.

**Expected result:**
- HTTP 200 response from `POST /api/bookings`
- The page navigates to a **Confirmation** page (or similar)
- Confirmation page displays:
  - Booking reference (format: `SR-XXXXXX` where X is hex digit)
  - Flight summary (route, provider, times, cabin class)
  - Total price confirmed
- No error banner

**REST Client capability:** ✅ Partial — REST Client can send the booking request after you capture the `searchId` and `flightId` from step A3. See below.

**Alternative approach (using REST Client):**

From the search response in step A3, note:
- `searchId` (e.g., `"b3f1c2a4-1234-5678-90ab-cdef12345678"`)
- `flights[0].id` (e.g., `"globalair-GA123-20251201-Economy"`)

Then send:
```http
POST http://localhost:5169/api/bookings
Content-Type: application/json

{
  "searchId": "b3f1c2a4-1234-5678-90ab-cdef12345678",
  "flightId": "globalair-GA123-20251201-Economy",
  "passengers": [
    {
      "fullName": "Jane Doe",
      "email": "jane@example.com",
      "documentNumber": "X1234567"
    }
  ]
}
```

**Expected response:**
- Status 200
- Response includes: `bookingReference` (format: `SR-XXXXXX`), `status` (value: `"Confirmed"`), `pricePerPassenger`, `passengerCount`, `totalPrice`, `currency` (value: `"USD"`)
- Verify: `totalPrice = pricePerPassenger × passengerCount`
- Verify: `totalPrice` matches the value shown in the search results (no client-side tampering)

---

### Scenario B: Domestic Flight Search and Booking (JFK → LAX)

**Objective:** Verify domestic flight search and booking with National ID validation.

#### Step B1: Search for domestic flights (JFK → LAX)

**Where:** Browser UI or REST Client

**What to do (Browser):**

1. On the results page or search page, go back to `/search` (click a back button or navigate directly).
2. Fill in the search form:
   - **Origin:** JFK
   - **Destination:** LAX
   - **Departure Date:** 2025-12-01 (or same as scenario A for consistency)
   - **Passengers:** 1
   - **Cabin Class:** Economy
3. Click **Search**.

**What to do (REST Client):**

```http
POST http://localhost:5169/api/flights/search
Content-Type: application/json

{
  "origin": "JFK",
  "destination": "LAX",
  "departureDate": "2025-12-01",
  "passengers": 1,
  "cabinClass": "Economy"
}
```

**Expected result:**
- Results page appears with flights
- `isInternational: false` (at response root, if checking via REST Client)
- At least 2 providers should appear: GlobalAir + BudgetWings (both serve short-haul domestic routes)
- Prices differ: GlobalAir = base × 1.15, BudgetWings = base × 0.90
- Note the `searchId` for the booking step

**REST Client capability:** ✅ Yes — send the POST request and inspect the response structure.

---

#### Step B2: Book a flight with a valid National ID

**Where:** Browser UI or REST Client

**What to do (Browser):**

1. Click on a flight to navigate to the booking page.
2. Observe the passenger form:
   - The document field label should read **"National ID"** (not "Passport Number") — confirming the route is domestic
3. Fill in the passenger form:
   - **Full Name:** John Roe
   - **Email:** john@example.com
   - **National ID:** 123456789 (matches the regex `^[0-9]{9}$`)
4. Click **Confirm Booking**.

**What to do (REST Client):**

```http
POST http://localhost:5169/api/bookings
Content-Type: application/json

{
  "searchId": "b3f1c2a4-1234-5678-90ab-cdef12345679",
  "flightId": "budgetwings-BW200-20251201-Economy",
  "passengers": [
    {
      "fullName": "John Roe",
      "email": "john@example.com",
      "documentNumber": "123456789"
    }
  ]
}
```

**Expected result:**
- HTTP 200
- Booking reference returned (format: `SR-XXXXXX`)
- Confirmation page displayed (if browser) with booking details

**REST Client capability:** ✅ Yes.

---

### Scenario C: Empty State (ORD → FCO)

**Objective:** Verify that a deliberately uncovered route returns an empty results list (no flights) without an error.

#### Step C1: Search for flights on uncovered route

**Where:** Browser UI or REST Client

**What to do (Browser):**

1. Go back to `/search`.
2. Fill in the search form:
   - **Origin:** ORD
   - **Destination:** FCO
   - **Departure Date:** 2025-12-01
   - **Passengers:** 1
   - **Cabin Class:** Economy
3. Click **Search**.

**What to do (REST Client):**

```http
POST http://localhost:5169/api/flights/search
Content-Type: application/json

{
  "origin": "ORD",
  "destination": "FCO",
  "departureDate": "2025-12-01",
  "passengers": 1,
  "cabinClass": "Economy"
}
```

**Expected result:**
- Results page displays (HTTP 200, no error)
- **Empty state component is shown** with a message like "No flights found" (if browser)
- Response body shows: `flights: []` (empty array, if REST Client)
- Response is **not** an error — it is a valid 200 response with zero flights
- This satisfies §3.2 requirement: "Show a clear empty state if no flights match the search"

**REST Client capability:** ✅ Yes.

---

### Scenario D: Cache Expiry (409 Offer Expired)

**Objective:** Verify that attempting a booking after the search cache entry expires returns a 409 error and the UI shows the "fares changed" banner.

#### Step D1: Search for flights and record searchId

**Where:** Browser UI or REST Client

**What to do:**

1. Execute a search (e.g., JFK → LHR again).
2. Note the `searchId` from the response.
3. Note the current time.

**Expected result:** `searchId` is captured for step D2.

---

#### Step D2: Wait for cache to expire (or simulate expiry)

**Challenge:** The current implementation uses `IMemoryCache` with a **10-minute TTL**. Waiting 10 minutes is not practical in a walkthrough. There are two options:

**Option A: Wait for the 10-minute TTL (not practical for Phase 10 execution)**

Simply wait 10 minutes + 1 second after the search completes, then proceed to step D3.

**Option B: Code-based cache invalidation (not available via REST Client)**

The current implementation **does not expose an API endpoint to manually clear the cache**. To test the 409 path without waiting, you would need to:

- **Temporarily add a debug endpoint** to `SkyRoute.WebApi` (e.g., `DELETE /api/debug/cache/{searchId}`) that clears a specific cache entry.
- Alternatively, **modify the cache TTL temporarily** (e.g., to 5 seconds) during testing, then revert it.
- Or **use the .NET debugger** to pause execution at the booking service, manually expire the cache programmatically, and resume.

**For the purposes of this Phase 10 checklist, Option B is out of scope** (it requires code changes, which are explicitly prohibited). Therefore, **Option A (waiting for the 10-minute TTL) is the only way to verify cache expiry without adding temporary code**.

**Workaround for Phase 10:** If 10 minutes is impractical, you may:
1. Note that the cache-expiry path is **unit-tested** in `BookingServiceTests.cs` (test: `BookAsync_ThrowsOfferExpiredException_ForUnknownSearchId`)
2. Verify the response structure via REST Client by manually creating a booking request with a **fake/non-existent `searchId`** (step D3 below)
3. Document in the README that the full 10-minute TTL walkthrough was deferred to post-demo validation

This does **not** satisfy the full walkthrough requirement, but it provides a practical alternative that verifies the 409 code path via a unit test + a REST Client call with a hardcoded invalid `searchId`.

---

#### Step D3: Attempt a booking with an expired or invalid searchId

**Where:** Browser UI or REST Client

**What to do (REST Client — simulating an expired/unknown searchId):**

```http
POST http://localhost:5169/api/bookings
Content-Type: application/json

{
  "searchId": "00000000-0000-0000-0000-000000000000",
  "flightId": "globalair-GA123-20251201-Economy",
  "passengers": [
    {
      "fullName": "Jane Doe",
      "email": "jane@example.com",
      "documentNumber": "X1234567"
    }
  ]
}
```

**Expected result:**
- HTTP **409 Conflict**
- Response body is a `ProblemDetails` with `title` indicating "Offer Expired" or similar
- If using the browser UI (after waiting 10 minutes), a banner should appear saying "Fares have changed, please search again" with a button to return to `/search`

**REST Client capability:** ✅ Yes — can verify the 409 response structure.

**UI verification:** ❌ No — requires browser and 10-minute wait, or code modification. Document as a known limitation in the README if not fully tested.

---

## § 2 — Fix Integration Issues

**What to do:** Based on any failures or unexpected behavior from § 1, diagnose and repair integration bugs.

**Where:** Terminal, code editor, and browser

**Examples of issues that might surface:**
- Port mismatch (frontend calling wrong backend port)
- CORS blocking requests
- Enum casing mismatch (e.g., JSON sends `"Economy"` but backend expects `"economy"`)
- Date/time display bugs (timezone offsets, formatting)
- Missing `IMemoryCache` registration
- HTTP status code mapping errors

**How to proceed:**

1. Re-run relevant unit tests:
   ```powershell
   dotnet test backend/SkyRoute.slnx
   npm test -- --watch=false  # from frontend/skyroute-app
   ```
2. Inspect browser console (F12 → Console) for JavaScript errors.
3. Inspect backend console for exceptions.
4. Use REST Client to isolate backend API issues from frontend rendering issues.
5. Make minimal code fixes; no new features.
6. Re-run tests after each fix to ensure no regression.

**Expected result:** All tests pass; all § 1 scenarios execute without errors or visual bugs.

---

## § 3 — Write README.md

**Objective:** Create comprehensive setup and architecture documentation.

**Where:** Text editor (e.g., VS Code), workspace root

**What to do:**

1. Create or edit `README.md` in the repository root (`c:\epam\skyroute\README.md`).
2. Include all sections listed in `docs/03-execution-plan.md` Phase 10 task 3:
   - **Setup/run instructions** for backend and frontend
   - **Test suite execution** (`dotnet test`, `npm test`)
   - **Architecture decisions** (Clean Architecture, provider abstraction, price integrity via `searchId` + `IMemoryCache`, `IsInternational` derived on criteria, etc.)
   - **Assumptions** (invented document regexes, USD only, airport-local times without offset, simulated latency, ORD↔FCO deliberately uncovered, cabin multipliers)
   - **Trade-offs / known limitations** (no database, no auth, limited e2e tests, no resilience policies, no pagination, cache-expiry demo requires 10-minute wait or code changes)
   - **Future improvements** (EF Core, authentication, real provider integrations, pagination, i18n, e2e tests, airport autocomplete)

**Expected result:** `README.md` exists, is complete and accurate, and a new user can follow it to run the application from scratch.

**REST Client capability:** N/A.

---

## § 4 — Final Git Status, Commit, and Clean Checkout Verification

**Objective:** Prepare the repository for submission and verify it is buildable from a clean state.

**Where:** Terminal (PowerShell or equivalent)

**What to do:**

1. **Check git status:**
   ```powershell
   cd c:\epam\skyroute
   git status
   ```
   **Expected:** All changes are staged or committed; no untracked files except `.vs/`, `bin/`, `obj/`, `node_modules/`, `dist/`, `.angular/` (which should be in `.gitignore`).

2. **Stage all changes:**
   ```powershell
   git add -A
   ```

3. **Check what will be committed:**
   ```powershell
   git status
   ```
   **Expected:** Only source files (`.cs`, `.ts`, `.json`, `.md`, `.html`, `.scss`) are shown; no build artifacts.

4. **Commit with a descriptive message:**
   ```powershell
   git commit -m "Phase 10: Final integration, README, and acceptance checklist"
   ```

5. **Verify the commit:**
   ```powershell
   git log --oneline -n 3
   ```
   **Expected:** The new commit appears at the top of the log.

6. **Clean checkout verification** (simulates a fresh clone):
   - Open a new terminal (or use a second repo clone for this step).
   - Clone the repository (or in this case, switch to a clean state):
     ```powershell
     # Simulate a fresh clone by removing build artifacts
     cd c:\epam\skyroute
     Remove-Item -Recurse -Force backend/SkyRoute.WebApi/bin, backend/SkyRoute.WebApi/obj, `
       backend/SkyRoute.Application/bin, backend/SkyRoute.Application/obj, `
       backend/SkyRoute.Infrastructure/bin, backend/SkyRoute.Infrastructure/obj, `
       backend/SkyRoute.Domain/bin, backend/SkyRoute.Domain/obj, `
       backend/SkyRoute.Tests/bin, backend/SkyRoute.Tests/obj, `
       frontend/skyroute-app/dist, frontend/skyroute-app/.angular
     ```

7. **Rebuild from clean state:**
   ```powershell
   # Backend
   dotnet build backend/SkyRoute.slnx
   # Expected: Build succeeded, 0 errors

   # Frontend
   cd frontend/skyroute-app
   nvm use 22.22.3
   npm install
   npm run build
   # Expected: Build completed successfully
   ```

8. **Run test suites once more (final gate):**
   ```powershell
   # Backend
   cd c:\epam\skyroute
   dotnet test backend/SkyRoute.slnx
   # Expected: All tests pass

   # Frontend
   cd frontend/skyroute-app
   npm test -- --watch=false
   # Expected: All tests pass
   ```

**Expected result:**
- Git repository is clean; all changes are committed.
- Fresh build from cleaned artifacts succeeds.
- All tests pass after clean build.
- Repository is ready for submission.

**REST Client capability:** N/A.

---

## Summary: All Manual Intervention Steps

Below is a concise checklist of every manual step required to complete Phase 10:

### Must be performed with Browser UI:
- [ ] **A1** — Start backend and frontend servers
- [ ] **A3** — Browse to `/search`, fill form, execute search for JFK→LHR, verify results page
- [ ] **A4** — Verify sorting with DevTools Network tab (zero additional HTTP requests)
- [ ] **A5** — Select a flight, navigate to booking page, observe "Passport Number" label, enter valid passport, confirm booking, verify confirmation page
- [ ] **B1 (Browser variant)** — Go back to `/search`, search for JFK→LAX, verify results with both providers
- [ ] **B2 (Browser variant)** — Book with valid National ID, verify confirmation
- [ ] **C1 (Browser variant)** — Search for ORD→FCO, verify empty state (no error, just empty list)
- [ ] **D3 (UI verification, requires 10-minute wait)** — Wait for cache to expire, attempt booking, observe 409 banner "Fares changed"

### Can be performed with REST Client extension:
- [ ] **A2** — GET /api/airports, verify 6 airports with correct countries
- [ ] **A3 (REST variant)** — POST /api/flights/search JFK→LHR, verify `searchId`, `isInternational: true`, flights array, `totalPrice = pricePerPassenger × passengers`
- [ ] **B1 (REST variant)** — POST /api/flights/search JFK→LAX, verify `isInternational: false`, both providers present
- [ ] **B2 (REST variant)** — POST /api/bookings with valid National ID, verify HTTP 200, booking reference in response
- [ ] **C1 (REST variant)** — POST /api/flights/search ORD→FCO, verify HTTP 200, empty `flights` array
- [ ] **D3 (REST variant)** — POST /api/bookings with fake `searchId`, verify HTTP 409 with `ProblemDetails` body

### Must be performed in Terminal:
- [ ] **A1** — `dotnet run --project backend/SkyRoute.WebApi` and `npm start` in frontend
- [ ] **§ 2** — Re-run `dotnet test backend/SkyRoute.slnx` and `npm test -- --watch=false` to verify no regressions
- [ ] **§ 4** — `git add -A`, `git commit -m "..."`, verify clean state and fresh build

### Must be performed in Text Editor:
- [ ] **§ 3** — Write or update `README.md` with setup, architecture, assumptions, trade-offs, future work

---

## Important Notes

### Cache Expiry Testing (Scenario D)

The ORD→FCO uncovered route is an alternative to verify the empty-state path; **the 10-minute cache-expiry scenario is the hardest to test without code changes**. Two practical options:

1. **Full test (impractical for demo):** Wait 10 minutes after search, then attempt booking.
2. **Partial test (recommended):** Use REST Client to send a booking request with a known-invalid `searchId`, verify HTTP 409. Confirm via unit tests that the cache-miss logic is correct. Document in the README that full TTL verification was validated via unit tests + single hardcoded-invalid-searchId REST call.

### Sorting Network Verification

Verifying "zero additional requests" requires DevTools Network tab. REST Client cannot observe this. The unit tests verify the sort logic is pure and doesn't re-fetch; manual inspection confirms the UI does not call the API when sorting.

### Document Validation Label Switching

The label **must change** from "Passport Number" to "National ID" based on route type. This is a critical UX indicator and **must be verified in the browser**. REST Client cannot validate UI labels.

---

## Final Acceptance Criteria

Phase 10 is complete when:

- [ ] All scenarios § 1.A–D have been executed (or appropriate alternatives, e.g., REST Client for D3 + unit test for cache logic)
- [ ] No integration bugs remain unfixed
- [ ] README.md is written and complete
- [ ] Git repository is clean and all changes committed
- [ ] `dotnet test` and `npm test` both pass
- [ ] Fresh build from clean artifacts succeeds
- [ ] All 22 rows of the acceptance checklist in `docs/03-execution-plan.md` (final section) are verified as satisfied
