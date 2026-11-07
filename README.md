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
- **Single instance:** the rate limiter is in memory and resets on restart.

## Technical Challenges & Tradeoffs Directed by the Engineer

- **Brief alignment:** the first design used a supplier-in-route endpoint and a different payload. It was redirected to match the brief exactly: `supplierId` in the body, the specified field set, and the stats route.
- **Deterministic hashing:** strings are trimmed, `price` is formatted as an invariant-culture `"0.00"` string, and dates use ISO-8601 UTC before SHA-256. Prices are stored as `TEXT` in SQLite to avoid binary floating point.
- **Atomic writes:** each ingest runs in `BEGIN IMMEDIATE`, so concurrent writes to one reservation queue instead of racing. The update also carries a SQL-level guard (`WHERE updated_at_utc < @UpdatedAtUtc`), so an out-of-order webhook can never overwrite newer data.
- **Single responsibility:** `SlidingWindowRateLimiter` is a pure in-memory component (per-supplier queue behind a lock, driven by `TimeProvider`). The ingest service records throttled requests in the stats table.

## What Would Be Done Differently in Production

- **Distributed rate limiting:** Redis, using sorted sets and a Lua script for an atomic check-and-add, so the 100 per 60s limit holds across a multi-instance cluster and survives restarts.
- **Transactional outbox:** write ingest events to an outbox table in the same transaction as the reservation, then publish them to downstream consumers reliably.
- **Observability:** OpenTelemetry traces and metrics (throttle rate, outcome counts, write latency) and structured logging with correlation IDs.
- **Housekeeping:** prune idle supplier windows from memory, and use a server database such as PostgreSQL if write volume outgrows SQLite's single writer.
