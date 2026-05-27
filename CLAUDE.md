# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

Merkatto — operational-management web platform for Peruvian bodegas/minimarkets.
**Single-tenant per business**: each client gets its own frontend, backend and PostgreSQL DB.
Scaling is by *replicating deployments*, NOT multi-tenancy. There is no `TenantId` anywhere by
design. All installs run the same images; per-client differences live only in `docker/.env`.

Domain reality it models: no per-sale registration; end-of-day cash-up (efectivo/Yape/Plin/POS),
wholesale purchase + per-unit sale (unit conversion), approximate inventory, simple credit ("fiados").

## Commands

Backend (.NET 10, from `backend/`):
```bash
dotnet build                                   # build solution
dotnet test                                    # all tests
dotnet test tests/Merkatto.UnitTests           # domain math tests (no DB)
dotnet run --project src/Merkatto.Api          # run API (needs Postgres + config)
# EF migrations (startup project is the API):
dotnet dotnet-ef migrations add <Name> --project src/Merkatto.Infrastructure --startup-project src/Merkatto.Api --output-dir Persistence/Migrations
dotnet dotnet-ef database update --project src/Merkatto.Infrastructure --startup-project src/Merkatto.Api
```
The API also applies migrations + seeds the admin automatically on startup (`DbInitializer`).

Frontend (Angular 21, from `frontend/`):
```bash
npm start            # dev server on :4200 (proxies to API on :5080 in dev)
npm run build        # production build -> dist/merkatto-web/browser
npm test             # vitest
```

Full stack (from `docker/`): `cp .env.example .env` then `docker compose up --build`.

## Local dev configuration

- API reads `ConnectionStrings__Default`, `Auth__SigningKey`, `Seed__*` from config/env.
  `appsettings.Development.json` carries a dev-only signing key + seed admin
  (`admin@martita.pe` / `Admin123$`). Never use those in production.
- Frontend dev points at `http://localhost:5080/api/v1` (`src/environments/environment.ts`);
  production uses same-origin `/api/v1` via the reverse proxy (`environment.production.ts`,
  swapped by `fileReplacements` in `angular.json`).

## Architecture

Backend is a **modular monolith / pragmatic Clean Architecture** (no microservices, no
CQRS/MediatR/DDD-heavy patterns — intentionally kept simple). Project dependency direction:
`Api → Application → Domain`, `Infrastructure → Application/Domain`.

- `Merkatto.Domain` — entities + business math. Money/derived values are **computed properties**
  on entities (e.g. `Product.UnitCost = LastPurchaseCost / UnitsPerPurchaseUnit`, `Product.Margin`,
  `DailyClosing.NetFlow`/`PosCommissionAmount`) and are `Ignore()`d in EF configs. When you add a
  computed property, remember to ignore it in the matching `*Configuration`.
- `Merkatto.Application` — use-case services (e.g. `AuthService`), DTOs, FluentValidation
  validators, and abstractions: `IAppDbContext`, `ICurrentUser`, `IPasswordHasher`,
  `IDateTimeProvider`, `ITokenService`. Application depends on EF Core only for `IAppDbContext`.
- `Merkatto.Infrastructure` — `AppDbContext` (implements `IAppDbContext`), EF configurations,
  `AuditingInterceptor`, Argon2 hasher, JWT token service, `DbInitializer`. Postgres + snake_case
  naming convention.
- `Merkatto.Api` — controllers, `Program.cs` wiring, `HttpCurrentUser`, security headers
  middleware, global exception handler (→ ProblemDetails), validation filter, rate limiting.

Key cross-cutting behavior (all in Infrastructure/Persistence):
- **Soft delete**: entities implement `ISoftDelete`; a global query filter hides deleted rows;
  the `AuditingInterceptor` converts hard deletes into soft deletes.
- **Auditing**: `AuditingInterceptor` stamps `IAuditable` fields and writes an `AuditLog` row
  (who/what/when) per change. Secrets (`PasswordHash`, `TokenHash`) are never serialized into logs.
  `RefreshToken` and `AuditLog` are excluded from audit logging.

Auth flow: short-lived JWT access token returned in the body (kept in memory by the SPA) +
rotating refresh token in an **httpOnly, Secure, SameSite=Strict** cookie scoped to
`/api/v1/auth`. Refresh tokens rotate on use with **reuse detection** (a reused revoked token
revokes the whole chain). See `AuthService` + `AuthController`. Frontend mirrors this in
`core/auth/` (token in a signal, `authInterceptor` retries once on 401 via `/auth/refresh`).

Frontend structure: `core/` (auth service/guard/interceptor), `features/` (login, dashboard, +
Phase 1 modules), `layout/` (desktop-first shell). Standalone components + signals, lazy routes,
TailwindCSS v4 (`@import "tailwindcss"` in `styles.css`, `@tailwindcss/postcss` in `.postcssrc.json`).

## Roadmap context

Phase 0 (scaffold + auth + DB + Docker) is done. **Products module is done** (Application
`ProductService`/`CategoryBrandService`, `ProductsController`/`CategoriesController`/
`BrandsController`, Angular `features/products`). Remaining Phase 1 (MVP): Purchases,
Inventory (warehouse/counter + `StockMovement` ledger), Daily Closing, Expenses, **Fiados**,
basic Dashboard. Entities for all of these already exist in `Domain`; what's missing is the
Application services + API controllers + Angular features. Products is the reference pattern
to copy. Full plan: `docs/PLAN.md`.

## Conventions

C#: PascalCase, `_camelCase` private fields, `Async` suffix. DB: snake_case (auto via naming
convention — don't hand-name columns). API: `/api/v1/...`, plural nouns. Angular: kebab-case
files, standalone, signals, feature folders. New entities: derive from `BaseEntity`, add a
`DbSet` to both `AppDbContext` and `IAppDbContext`, add an `IEntityTypeConfiguration`, then
create a migration.
