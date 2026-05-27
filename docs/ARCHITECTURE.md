# Arquitectura de Merkatto

Resumen ejecutivo. El plan completo (modelo de datos, API, roadmap por fases) está en
`~/.claude/plans/quiero-planear-y-desarrollar-wobbly-glade.md`; la guía operativa para
desarrollar está en `CLAUDE.md` (raíz del repo).

## Decisión central: single-tenant por negocio
Cada comercio tiene su propio frontend, backend y base de datos. No hay multi-tenancy lógica
(sin `TenantId`). Escalar = replicar deployments. Mismas imágenes para todos; lo único que
cambia por cliente es `docker/.env`.

## Backend — modular monolith / Clean Architecture pragmática
```
Api  ─►  Application  ─►  Domain
            ▲
Infrastructure ─────────────┘  (implementa abstracciones de Application; EF Core + PostgreSQL)
```
- **Domain**: entidades + matemática de negocio como propiedades calculadas (conversión de
  unidades, márgenes, flujos del cierre diario).
- **Application**: servicios de caso de uso, DTOs, validadores y abstracciones
  (`IAppDbContext`, `ICurrentUser`, `IPasswordHasher`, `IDateTimeProvider`, `ITokenService`).
- **Infrastructure**: `AppDbContext`, configuraciones EF, `AuditingInterceptor` (auditoría +
  soft delete), hasher Argon2id, token service JWT, `DbInitializer` (migraciones + seed).
- **Api**: controllers, headers de seguridad, manejo global de errores (ProblemDetails),
  filtro de validación, rate limiting, CORS.

## Seguridad
JWT corto en memoria + refresh token rotatorio (cookie httpOnly/Secure/SameSite=Strict) con
detección de reuso · Argon2id · rate limiting en auth · validación estricta (FluentValidation)
· headers de seguridad · CORS restringido al origen del SPA · secrets por entorno · HTTPS en el
proxy · auditoría (quién/qué/cuándo) · soft delete · backups `pg_dump`.

## Frontend — Angular 21
Standalone components + signals, rutas lazy, TailwindCSS v4. `core/` (auth: servicio con
signals, guard, interceptor con refresh-on-401), `features/`, `layout/` (shell desktop-first).
Prioridad UX: laptop → tablet → móvil.

## Estado
Fase 0 (scaffold, auth, BD+migración, Docker) completa y verificada E2E. Fase 1 (MVP: Productos,
Compras, Inventario, Cierre, Gastos, Fiados, Dashboard) pendiente — las entidades ya existen en
Domain; falta servicios de Application + controllers + features Angular.
