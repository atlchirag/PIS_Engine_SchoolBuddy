# One-Shot ETA Change — Ola Maps API Cost Reduction

**Date:** 2026-09-16 (planned) / 2026-09-18 (actually applied to source)
**Files changed:** `PISEngine/ETAPredictor.cs`, `PISEngine/Stops.cs`
**Build status:** Passing (`PISEngine/bin/Debug/PISEngine.exe`, built with VS 2022+ MSBuild at `Microsoft Visual Studio/18/Insiders/MSBuild/Current/Bin/MSBuild.exe` - the old `Framework64/v4.0.30319/MSBuild.exe` cannot build this project, it rejects every `$"..."` interpolated string)

> **Note (2026-09-18):** this document was written before the code was edited. On 2026-09-18 the
> source was checked and `ETAPredictor.cs` / `Stops.cs` were still the original every-2-minute
> version - none of the changes below were present. They have now been applied and the solution
> builds. Nothing in section 6 (prediction drift) was changed: one API call per trip is the
> intended behaviour.
**Backup of original:** `ETAPredictor.cs.bak` in the session scratchpad directory

---

## 1. The Problem

The engine was calling the Ola Maps `distanceMatrix` API **every 2 minutes for every pending stop**, for the entire 4-hour route window. Ola bills this endpoint **per element** (`origins × destinations`), so a single route trip was consuming thousands of billable elements.

Two bugs made it worse than intended:

### Bug A — the skip filter used `&&` instead of `||`

`ETAPredictor.cs` (old line 805):

```csharp
if (this.stopArray[i].MsgSend == true && this.stopArray[i].IsReached == true)
    continue;
```

A stop was only excluded from the API call when it had **both** been notified **and** been marked reached. Sending the notification alone was not enough — the stop kept getting re-priced on every subsequent cycle.

### Bug B — `IsReached` was almost never set to `true`

`IsReached = true` was assigned in exactly one place (old line 455), inside a `hasVias` guard:

```csharp
if (hasVias && lastViahit >= 0 && lastViahit < viaTable.Rows.Count)
{
    if (this.stopArray[i].linkNo > 0 && this.stopArray[i].linkNo < via_order ...)
        this.stopArray[i].IsReached = true;
}
```

Consequences:

- Routes with **no vias** (the DIRECT fallback flow) never set `IsReached` for any stop, so combined with Bug A **no stop was ever skipped** — every stop was billed on every cycle for the full 4 hours.
- Stops with `link_no = 0` (newer auto-created stops) never set `IsReached` either.
- `reachDis = 140` was passed into `Newthread` but never used; the distance-based "reached" block was commented out, so `stopArray[i].Distance` was computed and discarded.

### Cost impact (30-stop route, one trip)

| | Before |
|---|---|
| Cycles per trip | ~120 (every 2 min × 4 h) |
| API calls | ~120 |
| Billed elements | ~3,600 |

---

## 2. The New Approach

`startEngine()` already blocks the route until the trip has genuinely begun - GPS fix newer than
3 minutes, ignition `i2 = 1`, and the bus more than **200 m** away from the school. Only after
that gate passes does the 2-minute loop (and therefore `Newthread`) run at all, so the single API
call lands at the real start of the trip.

Call the API **once per trip**, convert each returned duration into an **absolute arrival clock time**, persist it, and from then on decide every notification by comparing that stored time against `DateTime.Now`. No further API calls.

---

## 3. Changes in `PISEngine/Stops.cs`

Two fields added to the `Stop` class:

```csharp
// Absolute clock time the bus is predicted to reach this stop, fixed by the one and only
// distance-matrix call of the trip. Every later cycle compares this against DateTime.Now
// instead of asking the API again.
public DateTime PredictedArrival { get; set; }
public bool EtaFetched { get; set; }
```

---

## 4. Changes in `PISEngine/ETAPredictor.cs`

### 4.1 New fields controlling the API budget

```csharp
// One-shot ETA policy.
// The Ola distance matrix is billed per element (origins x destinations), so re-pricing
// every pending stop every 2 minutes for the whole trip is what runs the bill up. We ask
// the API once per trip, turn each duration into an absolute arrival clock time, store it,
// and from then on every cycle decides purely on the stored time. Raise _maxEtaApiHits if
// the frozen prediction drifts too far on long routes - each extra hit costs another
// (1 x pending stops) elements.
private const int _maxEtaApiHits = 1;
private int _etaApiHits = 0;
```

`_etaApiHits` is an **instance** field. `Program.cs` creates a fresh `ETAPredictor` per route start, so the budget resets naturally for each trip and each route has its own independent budget.

### 4.2 `Newthread()` rewritten

The method was split into two clearly separated phases.

**Phase 1 — price the route once:**

```csharp
if (_etaApiHits < _maxEtaApiHits)
{
    if (!FetchAndStoreArrivalTimes(source, j, retreivalCounter, route_name, user_id))
    {
        // API unreachable or unusable. _etaApiHits is deliberately not incremented
        // so the next cycle retries - without a successful fetch no stop ever gets
        // an arrival time and no parent ever gets a message.
        return;
    }
}
```

**Phase 2 — pure clock arithmetic, no API:**

```csharp
for (int i = 0; i < j; i++)
{
    Stop stop = this.stopArray[i];

    if (stop.MsgSend || !stop.EtaFetched)
        continue;

    // Same gate as before: on a VIA route a stop with no link_no cannot be
    // trusted to be ordered correctly, so it is not alerted.
    if (hasVias && stop.linkNo == 0)
        continue;

    // Existing behaviour was "eta <= eta_msg AND eta <= 10", i.e. the lower of
    // the two. Kept as is so schools that configured a smaller window keep it.
    int configured = stop.eta_msg > 0 ? stop.eta_msg : 10;
    int threshold = Math.Min(configured, 10);

    double minutesLeft = (stop.PredictedArrival - DateTime.Now).TotalMinutes;
    if (minutesLeft > threshold)
        continue;

    // ... update bs_stop_master.status = 1, send notification, set MsgSend = true
}
```

The whole body is wrapped in `try / finally` so `this.startThread = true` always runs, including on the early `return` when the API fails. Previously that flag was reset on several separate exit paths.

### 4.3 New method `FetchAndStoreArrivalTimes()`

This is the single billed API call of the trip.

```csharp
private bool FetchAndStoreArrivalTimes(string source, int j, int retreivalCounter,
                                       string route_name, int user_id)
```

Behaviour:

1. Builds the pending-stop list, now skipping on `MsgSend || IsReached` (fixes Bug A).
2. If nothing is pending, sets `_etaApiHits = _maxEtaApiHits` and returns `true` — the route is finished.
3. Calls `GetEta_here(source, destinations, retreivalCounter)` — unchanged, still logs the request URI into `bs_Api_test`.
4. Returns `false` (without incrementing `_etaApiHits`) when the API is unreachable or the element count does not match the request, so the next cycle retries without burning the one-shot budget.
5. On success, anchors every arrival time to a single instant so the stops stay consistent with each other:

```csharp
DateTime predictedFrom = DateTime.Now;

for (int k = 0; k < pendingStops.Count; k++)
{
    Stop stop = this.stopArray[pendingStops[k]];
    stop.ETA_Default   = etaList[k];
    stop.PredictedArrival = predictedFrom.AddMinutes(etaList[k]);
    stop.EtaFetched    = true;

    // UPDATE bs_stop_master SET eta='HH:mm:ss' WHERE Id=<stop.Id>
}

_etaApiHits++;
```

The `bs_stop_master.eta` column now holds a **stable absolute arrival time** written once, instead of being overwritten with a fresh value every 2 minutes.

### 4.4 Notification message text

The old message hardcoded `DateTime.Now.AddMinutes(10)` as the arrival time regardless of the real ETA. It now uses the stored prediction and the actual threshold:

```csharp
string message = $"Dear parent, the bus for route {route_name} shall reach your stop " +
                 $"within {threshold} min at {stop.PredictedArrival:HH:mm:ss}";
```

### 4.5 Route-complete reset block

`ETAPredictor.cs` (around line 493) — `EtaFetched` added alongside the existing resets:

```csharp
stop.MsgSend    = false;
stop.IsReached  = false;
stop.EtaFetched = false;
```

### 4.6 Logging

Two new log lines make the change verifiable in production:

- On fetch: `"Arrival times stored for N stops from a single API call. API hits used this trip: 1/1."`
- On send: `"Message sent to stop <name> from stored ETA HH:mm:ss (VIA|DIRECT flow, no API call)."`

---

## 5. Result

| | Before | After |
|---|---|---|
| API calls per trip | ~120 | **1** |
| Billed elements (30-stop route) | ~3,600 | **30** |
| Reduction | — | **~99%** |

`bs_Api_test` will now contain a single row per route trip. That table is the easiest way to verify the drop after deployment — compare row counts per `route_id` per day before and after.

---

## 6. Known Trade-off — Prediction Drift

The prediction is frozen at the moment the bus leaves the school geofence. Two sources of error follow from that:

1. **Dwell time is not included.** `distanceMatrix` returns driving time only, not the 1–2 minutes the bus spends at each stop while children board. Across 20 stops this accumulates to roughly 20–40 minutes, so later stops will be notified **earlier than the bus actually arrives**.
2. **No correction for changing traffic.** The previous 2-minute re-query absorbed traffic changes automatically; a single up-front prediction cannot.

The first few stops on a route are affected negligibly. Later stops will show visible drift.

### Mitigations available

- **Increase `_maxEtaApiHits`** (`ETAPredictor.cs`, the constant near the top of the `Newthread` region). Setting it to 2 or 3 spreads corrections across the trip and still keeps the bill at 2–3% of the old figure.
- **Add a per-stop dwell buffer** — e.g. add `stop_order × 1 minute` to `PredictedArrival` — which improves accuracy substantially while keeping exactly one API call. Not implemented; needs a decision on the per-stop constant.

---

## 7. Separate Issues Noted, Not Changed

These were found while tracing the cost problem. None are fixed by this change.

1. **Hardcoded API key.** `GetEta_here()` contains the Ola key in plain source (`vPUpQe6nr6OJm6j2JWjHz67JybeiTZ7d1Nd7ilDU`). If the source or compiled binary has been shared, part of the bill may be external usage. Consider moving it to `App.config` and rotating the key.
2. **Routes with `link_no = 0` stops never end early.** In the VIA flow those stops can never get `MsgSend = true`, so `stops.Any(x => x.MsgSend == false)` stays true and the route thread runs the full 4 hours. No longer an API cost after this change, but it does hold a thread and keep `running = 1` longer than necessary.
3. **`reachDis = 140` is still dead.** It is passed into `Newthread` and never read. The distance-based `IsReached` logic remains commented out.
