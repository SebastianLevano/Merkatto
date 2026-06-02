# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

Merkatto — operational-management desktop app for Peruvian bodegas/minimarkets.
**Single-tenant per business**: each install has its own SQLite database and is bound to a single
Encargado. There is no `TenantId` anywhere by design. Isolation is physical (separate installs),
not logical.

Domain reality it models: no per-sale registration; end-of-day cash-up (efectivo/Yape/Plin/POS),
wholesale purchase + per-unit sale (unit conversion), approximate inventory, simple credit ("fiados").

## Roles

- `Administrator` (role=1) — system owner. Manages users only. Never sees operational dashboard.
- `Encargado` (role=2) — bodega operator. Full access to all operational features.

## Commands

Backend (.NET 10, from `backend/`):
```bash
dotnet build                                   # build solution
dotnet test                                    # all tests (10 unit + 46 integration)
dotnet test tests/Merkatto.UnitTests           # domain math tests (no DB)
dotnet run --project src/Merkatto.Api          # run API (needs Postgres + config)
# EF migrations (Postgres provider):
EF_PROVIDER=Postgres dotnet dotnet-ef migrations add <Name> --project src/Merkatto.Infrastructure --startup-project src/Merkatto.Api --output-dir Persistence/Migrations
```
The API applies migrations + seeds the admin automatically on startup (`DbInitializer`).

Frontend (Angular 21, from `frontend/`):
```bash
npm start            # dev server on :4200 (proxies to API on :5080 in dev)
npm run build        # production build -> dist/merkatto-web/browser
npm test             # vitest
```

Desktop (from `backend/`):
```bash
# Build frontend first, then copy to wwwroot
cd ../frontend && npm run build
cp -r dist/merkatto-web/browser ../backend/src/Merkatto.Desktop/wwwroot
# Run
cd ../backend && dotnet run --project src/Merkatto.Desktop
```

Full stack (from `docker/`): `cp .env.example .env` then `docker compose up --build`.

## Local dev configuration

- API dev seed: `admin@sistema.pe` / `Admin123$` (`appsettings.Development.json` in both Api and Desktop).
  Never use in production.
- Desktop reads `client.json` from `AppContext.BaseDirectory` at startup (gitignored — don't commit it).
  Without `client.json` → standalone mode (local auth only).
  With `{"centralBaseUrl":"..."}` → central broker mode (validates against remote, caches offline).
  With `{"mode":"admin","centralBaseUrl":"..."}` → admin panel mode (Photino window → central, no local DB).
- Frontend dev points at `http://localhost:5080/api/v1` (`environment.ts`); production uses
  same-origin `/api/v1` (`environment.production.ts`, swapped by `fileReplacements`).

## Architecture

Backend: **modular monolith / pragmatic Clean Architecture** (no microservices, no CQRS/MediatR).
Dependency direction: `Api → Application → Domain`, `Infrastructure → Application/Domain`.

- `Merkatto.Domain` — entities + business math. Computed properties (e.g. `Product.UnitCost`,
  `DailyClosing.NetFlow`) are `Ignore()`d in EF configs — remember to ignore when adding new ones.
- `Merkatto.Application` — use-case services, DTOs, FluentValidation validators, abstractions:
  `IAppDbContext`, `ICurrentUser`, `IPasswordHasher`, `IDateTimeProvider`, `ITokenService`,
  `ICentralAuthClient` (optional, injected as `IEnumerable<ICentralAuthClient>`).
- `Merkatto.Infrastructure` — `AppDbContext`, EF configurations, `AuditingInterceptor`, Argon2
  hasher, JWT token service, `DbInitializer`, `HttpCentralAuthClient`.
- `Merkatto.Api` — controllers, `Program.cs` wiring, middleware, rate limiting. Also serves the
  Angular SPA from `wwwroot/` when present (for admin panel mode).
- `Merkatto.Desktop` — Photino host. Reads `client.json`, chooses mode, wires optional
  `ICentralAuthClient` if `Central:BaseUrl` is set, opens the Photino window.

Key cross-cutting behavior (Infrastructure/Persistence):
- **Soft delete**: `ISoftDelete` entities → global query filter; `AuditingInterceptor` converts
  hard deletes to soft deletes.
- **Auditing**: `AuditingInterceptor` stamps `IAuditable` + writes `AuditLog`. Secrets
  (`PasswordHash`, `TokenHash`) are never logged. `RefreshToken` and `AuditLog` are excluded.

Auth flow: JWT (15 min, in memory) + rotating refresh token (httpOnly cookie, `/api/v1/auth`).
Reuse detection: reused revoked token revokes the whole chain.

Central identity broker (when `ICentralAuthClient` is registered):
- `ValidateAsync` returns `null` = offline (fall back to local cache), throws
  `CentralRejectedException` = 401 final (do NOT fall back), returns value = cache and issue
  local token.
- First successful online login binds the install to that Encargado (`bound_user_email` AppSetting).
  Different accounts and Administrators are rejected on that install.
- Password changes go to the central first; offline forced-changes are blocked.

Frontend: `core/` (auth service/guard/interceptor, `role.guard.ts`, `desktop.service.ts`),
`features/`, `layout/shell.ts`. Standalone components + signals, lazy routes, TailwindCSS v4.
Role guards: `operatorGuard` (redirects admin → `/configuracion/usuarios`), `adminOnlyGuard`
(redirects non-admin → `/`). Sidebar is computed by role (`nav()` signal in shell).

## State of the project

MVP complete. All features implemented and tested. Project is in active development (no live
clients yet). Pending: VPS deploy of the central server (when first client signs up).

Key files:
- `backend/src/Merkatto.Infrastructure/Auth/HttpCentralAuthClient.cs` — HTTP broker
- `backend/src/Merkatto.Application/Auth/ICentralAuthClient.cs` — broker contract + exceptions
- `backend/src/Merkatto.Desktop/Program.cs` — three-mode startup logic
- `frontend/src/app/core/auth/role.guard.ts` — role-based route guards
- `backend/src/Merkatto.Infrastructure/Persistence/DbInitializer.cs` — startup seeding + SQLite
  schema upgrades (idempotent ALTER TABLE for new columns)

## Conventions

C#: PascalCase, `_camelCase` private fields, `Async` suffix. DB: snake_case (auto via naming
convention — don't hand-name columns). API: `/api/v1/...`, plural nouns. Angular: kebab-case
files, standalone, signals, feature folders. New entities: derive from `BaseEntity`, add a
`DbSet` to both `AppDbContext` and `IAppDbContext`, add an `IEntityTypeConfiguration`, then
create a migration (Postgres) and an idempotent `ALTER TABLE` guard in `DbInitializer` (SQLite).
