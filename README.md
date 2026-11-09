# Supplier Feed Throttle & Dedup Service

ASP.NET Core (.NET 8), SQLite and Dapper service that ingests supplier reservation feeds. It throttles each supplier to 100 requests per rolling 60 seconds and deduplicates payloads, so retries and out-of-order deliveries are safe.

| Endpoint | Purpose |
|---|---|
| `POST /api/reservations/ingest` | Ingest a reservation. `supplierId` is in the JSON body. Returns `201` (new), `200` (`Duplicate`, `Updated`, `StaleIgnored`), `409` (`Conflict`), `400` (invalid), `429` (throttled, with `Retry-After`). |
| `GET /api/reservations/stats/{supplierId}` | Returns `{ supplierId, ingestedCount, throttledCount }`. |

## Quick Start

Prerequisite: [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```
dotnet run --project src/SupplierFeed.Api --urls http://localhost:5000
dotnet test
```

The SQLite file (`supplier-feed.db`, WAL mode) is created on first start. Tests use a temporary database and a `FakeTimeProvider` per test. Import `postman_collection.json` for ready-made requests; its `baseUrl` defaults to `http://localhost:5000`.

## Assumptions Made

- **Stale updates:** a changed payload with an older `updatedAtUtc` returns `200` with `status: StaleIgnored` and leaves the stored record untouched. A changed payload with an identical `updatedAtUtc` cannot be ordered, so it returns `409 Conflict`.
- **Hash scope:** the SHA-256 covers only the business fields (`roomId`, `checkIn`, `checkOut`, `price`). If these match the stored record the request is a `Duplicate`, whatever `updatedAtUtc` says.
- **Quota:** every admitted request consumes quota, including duplicates and stale updates. Throttled (`429`) and invalid (`400`) requests do not.
- **Unknown suppliers:** the stats endpoint returns `200` with zero counts instead of `404`.
- **Shared state:** the rate limiter keeps its sliding window in SQLite (`supplier_request_admissions`), so every instance pointed at the same database file enforces one shared limit and the window survives restarts.

## Technical Challenges & Tradeoffs Directed by the Engineer

- **Brief alignment:** the first design used a supplier-in-route endpoint and a different payload. It was redirected to match the brief exactly: `supplierId` in the body, the specified field set, and the stats route.
- **Deterministic hashing:** strings are trimmed, ``price` is formatted with `ToString(CultureInfo.InvariantCulture)` after trailing zeros are stripped (`450.00` and `450` match, while `450.001` and `450.002` differ), and dates use ISO-8601 UTC before SHA-256. Prices are stored as `TEXT` in SQLite to avoid binary floating point.
- **Atomic writes:** each ingest runs in `BEGIN IMMEDIATE`, so concurrent writes to one reservation queue instead of racing. The update also carries a SQL-level guard (`WHERE updated_at_utc < @UpdatedAtUtc`), so an out-of-order webhook can never overwrite newer data.
- **Database-backed rate limiting:** `SqliteRateLimiter` runs one short `BEGIN IMMEDIATE` transaction per request: delete admissions at or before `now - 60s`, count the rest, reject with `Retry-After = oldest + 60s - now` when the count is 100 or more, otherwise insert an admission. `BEGIN IMMEDIATE` takes SQLite's write lock up front, so concurrent requests, even from different instances, serialize and cannot both take the last slot. Waiting writers respect `busy_timeout` (5000 ms). The `(supplier_id, admitted_at_utc)` index keeps each step an index range operation, and timestamps are fixed-width ISO-8601 UTC text so they compare correctly as strings. Throttled requests insert nothing, so they never consume quota.
- **Trade-offs:** every ingest now costs an extra write transaction, and because SQLite has a single writer, all rate-limit checks and ingests serialize on one lock. That caps throughput and adds latency under contention, and instances must share a filesystem; SQLite over a network share is unreliable. The ingest service records throttled requests in the stats table.

## What Would Be Done Differently in Production

- **Distributed rate limiting:** move the window to Redis (sorted sets and a Lua script for an atomic check-and-add) or a server database such as PostgreSQL, so limit checks stop contending with ingest writes on a single SQLite lock.
- **Transactional outbox:** write ingest events to an outbox table in the same transaction as the reservation, then publish them to downstream consumers reliably.
- **Observability:** OpenTelemetry traces and metrics (throttle rate, outcome counts, write latency) and structured logging with correlation IDs.
- **Housekeeping:** purge admission rows of idle suppliers (rows are only deleted when that supplier next calls), and use a server database such as PostgreSQL if write volume outgrows SQLite's single writer.
