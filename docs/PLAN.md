# Plan técnico — Merkatto

> **Estado de implementación:** Fase 0 (Scaffolding) **completa y verificada E2E** — monorepo,
> solución .NET, Angular 21 + Tailwind, `DbContext` + migración inicial (18 tablas), auth
> (login/refresh rotatorio/logout/me), auditoría, soft-delete y stack Docker. Las entidades de
> todo el MVP ya existen en `Domain`; lo pendiente de Fase 1 son los servicios de Application,
> los controllers y las features Angular por módulo. Ver `docs/ARCHITECTURE.md` (resumen) y
> `CLAUDE.md` (guía de desarrollo).

---

# Plan: Plataforma "Merkatto" — Gestión operativa para bodegas y minimarkets (single-tenant)

## Context

Plataforma web profesional para gestión operativa de bodegas/minimarkets peruanos.
Primer piloto: **Bodega Martita**.

**Modelo de despliegue: single-tenant por negocio.** Cada cliente tiene su propio frontend,
backend y base de datos PostgreSQL; el sistema se instala de forma independiente por comercio.
La escalabilidad se logra **replicando deployments**, no con multi-tenancy lógica: no existe
`TenantId`. Esto mantiene la arquitectura simple, fácil de desplegar, de mantener y de aislar
por cliente.

Filosofía de producto: *"Controla tu negocio sin cambiar la forma en que trabajas."* No se
registra cada venta individual; el sistema modela la realidad peruana (efectivo/Yape/Plin/POS,
cuadre al final del día, compra al por mayor, venta por unidad, inventario aproximado).

**Decisiones de alcance:**
- MVP = Operativo esencial **+ Fiados Simples** desde el día 1.
- **Monorepo** (`/backend`, `/frontend`, `/docker`, `/docs`).
- Una sola imagen Docker por servicio, **diferenciada por cliente solo vía `.env`** (branding,
  secrets, dominio). Mismo código para todas las instalaciones.

## Stack (obligatorio)
- **Frontend:** Angular 21 (standalone components, signals, nuevo control flow) + TailwindCSS.
- **Backend:** ASP.NET Core Web API (.NET) — modular monolith, Clean Architecture pragmática.
- **DB:** PostgreSQL + EF Core (migrations).
- **Infra:** Docker / docker-compose, reverse proxy con HTTPS.
- **Auth:** JWT corto (en memoria) + Refresh Token rotatorio (cookie httpOnly).

---

## 1. Arquitectura general

Cada negocio = 1 stack aislado: `web (Angular+Nginx)` → `api (ASP.NET)` → `db (PostgreSQL)`,
orquestado por un `docker-compose` por servidor de cliente, detrás de un reverse proxy
(Caddy o Traefik) que resuelve TLS automáticamente. No hay comunicación entre instalaciones.

```
[Caddy/Traefik :443] ──► [web: Angular build + Nginx]
                     └──► [api: ASP.NET Core]  ──► [db: PostgreSQL] (volumen persistente)
                                                └─► [backup: cron pg_dump] (volumen backups)
```

### Backend — estructura del repositorio (monorepo)
```
/backend
  /src
    Merkatto.Api/            # Host Web API: controllers, middleware, DI, config, auth, security headers
    Merkatto.Application/    # Casos de uso / servicios de aplicación, DTOs, validators (FluentValidation), interfaces
    Merkatto.Domain/         # Entidades, enums, value objects, reglas de dominio (conversión de unidades, márgenes)
    Merkatto.Infrastructure/ # EF Core DbContext, configuraciones, migrations, repos, interceptores (audit/auditable), servicios externos (PDF/Excel)
  /tests
    Merkatto.UnitTests/
    Merkatto.IntegrationTests/   # Testcontainers/Postgres
/frontend                    # Angular 21
/docker                      # Dockerfiles, docker-compose.yml, docker-compose.prod.yml, .env.example, configs Nginx/Caddy
/docs                        # Arquitectura, runbook de instalación, backup/restore, ADRs
```
Dentro de `Application`/`Domain`/`Infrastructure` la organización es **feature-first** (módulos):
`Auth`, `Products`, `Purchases`, `Inventory`, `DailyClosing`, `Expenses`, `Credit`,
`Dashboard`, `Reports`, `Alerts`, `Audit`. Un solo deployable (modular monolith), sin
microservicios.

> Nota: se evita CQRS/MediatR/Event Sourcing/DDD pesado por la directiva explícita de
> "no sobreingeniería". Servicios de aplicación simples + repositorios/EF directo.

### Frontend — estructura
```
/frontend/src/app
  core/      # auth (signals), http interceptors, guards, error handling, config
  shared/    # componentes UI reutilizables (botones, tablas, modales, inputs), pipes, directivas
  features/  # products, purchases, inventory, daily-closing, expenses, credit, dashboard, settings
  layout/    # shell desktop-first (sidebar, topbar), responsive (tablet/móvil complementario)
```
Estado con **signals + servicios** (sin NgRx). Desktop-first, responsive con breakpoints Tailwind.
Prioridad UX: 1) Laptop/PC, 2) Tablet, 3) Celular. Estética inspirada en Stripe/Notion/Linear
(limpio, minimalista, rápido).

---

## 2. Modelo de datos (entidades principales)

Base común: `BaseEntity { Id (bigint/identity), CreatedAt, CreatedBy, UpdatedAt, UpdatedBy }`
+ interfaces `IAuditable` y `ISoftDelete { IsDeleted, DeletedAt }`. **Global query filter para
soft-delete** (oculta registros borrados). Convención de DB **snake_case** vía
`EFCore.NamingConventions`.

**Auth**
- `User` (Id, Email/Username, PasswordHash, Role[enum: Administrator|Collaborator], IsActive, …)
- `RefreshToken` (Id, UserId, TokenHash, ExpiresAt, RevokedAt, ReplacedByTokenHash, CreatedByIp)
  — rotación + detección de reuso.

**Productos / Conversión de unidades** (núcleo del cálculo)
- `Category` (Id, Name), `Brand` (Id, Name) opcional.
- `Product`: Name, CategoryId, BrandId?, InternalCode?,
  - Compra: `PurchaseUnit` (paquete/caja/fardo/maple/unidad), `LastPurchaseCost` (costo de 1 unidad de compra), `UnitsPerPurchaseUnit` (factor de conversión, p.ej. 6).
  - Venta: `SaleUnit`, `SalePrice` (precio unitario de venta).
  - Derivados (calculados en dominio): `UnitCost = LastPurchaseCost / UnitsPerPurchaseUnit`, `Margin = SalePrice - UnitCost`, `MarginRate`.
  - Inventario: `WarehouseStock`, `CounterStock`, `MinStock` (en unidad base de venta).
  - `IsActive`.
  - *Ejemplo galleta soda:* 10 paquetes × S/6 (6 u/paq) → UnitCost S/1, SalePrice S/1.50, Margin S/0.50.

**Compras**
- `Supplier` (Id, Name, Phone?, Notes?).
- `Purchase` (Id, SupplierId, Date, TotalCost, Notes).
- `PurchaseItem` (Id, PurchaseId, ProductId, Quantity, PurchaseUnit, UnitCostSnapshot, ConversionFactorSnapshot, Subtotal) → al registrar, **convierte a unidad base y suma a `WarehouseStock`**; opcionalmente actualiza `LastPurchaseCost`.

**Inventario híbrido**
- `StockMovement` (ledger append-only): (Id, ProductId, MovementType[Purchase|Transfer|Adjustment|EstimatedSale|InternalUse], QuantityBaseUnits ±, Location[Warehouse|Counter], SourceType, SourceId, Date) — trazabilidad y base de estimaciones.
- `InventoryAdjustment` (Id, ProductId, Type[Loss|Expired|InternalUse|Correction], Quantity, Reason, Date, UserId).
- Transferencia almacén→mostrador = 2 movimientos. Estimación de rotación/consumo se calcula como servicio sobre deltas de stock + cierres (referencial, no exacto).

**Cierre diario**
- `DailyClosing` (Id, BusinessDate[unique], CashAmount, YapeAmount, PlinAmount, PosAmount, PosCommissionRate, PosCommissionAmount, TotalExpenses, QuickPurchases, GrossIncome, NetFlow, EstimatedProfit, Notes, ClosedByUserId, ClosedAt).
  - `GrossIncome = Cash+Yape+Plin+Pos`; `NetFlow = GrossIncome − Expenses − QuickPurchases − PosCommission`; `EstimatedProfit` referencial vía margen estimado.

**Gastos**
- `Expense` (Id, Date, Type[Luz|Agua|Movilidad|Reposicion|Mantenimiento|CompraRapida|Otros], Amount, Description, DailyClosingId?, UserId).

**Fiados Simples** (MVP — debe ser rápido, sin formularios largos)
- `CreditCustomer` (Id, Name, Phone?, Notes?).
- `CreditSale` (Id, CustomerId, Date, TotalAmount, Notes) + `CreditSaleItem` (Description libre p.ej. "Pan x4", Quantity, LineTotal) — items opcionales.
- `CreditPayment` (Id, CustomerId, Amount, Date, Notes).
- `Balance` por cliente = Σ CreditSale − Σ CreditPayment (calculado).

**Auditoría / Alertas / NRUS**
- `AuditLog` (Id, UserId, Action, EntityName, EntityId, OldValues json, NewValues json, Timestamp, IpAddress) — vía interceptor de `SaveChanges`. Alimenta el **Timeline Operacional**.
- `Alert` (Id, Type, Severity, Message, EntityRef, IsRead, CreatedAt) — generadas por checks (stock bajo, gasto alto, etc.).
- `NrusEstimate` (Id, Year, Month, EstimatedIncome, EstimatedCategory, EstimatedQuota, GeneratedAt) — snapshot mensual referencial.

---

## 3. API REST (diseño)
Versionado `/api/v1`, JSON, sustantivos en plural, paginación + filtros en listados, códigos
HTTP correctos, `ProblemDetails` para errores.
- `POST /auth/login`, `POST /auth/refresh`, `POST /auth/logout`, `GET /auth/me`
- `GET/POST/PUT/DELETE /products`, `PATCH /products/{id}/stock`, `GET /categories`, `GET /brands`
- `GET/POST /purchases`, `GET /suppliers`
- `GET /inventory`, `POST /inventory/adjustments`, `POST /inventory/transfers`, `GET /inventory/movements`
- `GET/POST /daily-closings`, `GET /daily-closings/{date}`
- `GET/POST /expenses`
- `GET/POST /credit/customers`, `POST /credit/sales`, `POST /credit/payments`, `GET /credit/customers/{id}/balance`
- `GET /dashboard/summary`, `GET /alerts`, `GET /timeline`
- (Fase 2) `GET /nrus/estimate`, `GET /reports/{type}?format=pdf|excel`

---

## 4. Autenticación y seguridad
- **Access token** JWT ~15 min, devuelto en el body y guardado **en memoria** (signal) en Angular.
- **Refresh token** en cookie **httpOnly + Secure + SameSite=Strict**, rotatorio con detección de reuso; endpoint `/auth/refresh`; interceptor que renueva ante 401.
- **Password hashing**: Argon2id (`Isopoh.Cryptography.Argon2`); BCrypt como alternativa aceptable.
- **Rate limiting** (middleware nativo de .NET) en endpoints de auth.
- **Validación** backend estricta con FluentValidation; sanitización de inputs.
- **SQLi**: EF Core parametrizado por defecto. **XSS**: Angular sanitiza por defecto; API solo JSON.
- **Security headers** (CSP, HSTS, X-Content-Type-Options, Referrer-Policy) vía middleware; **CORS** restringido al origen del frontend.
- **Secrets** solo por variables de entorno/`.env` (nunca en repo); **HTTPS obligatorio** en el reverse proxy.
- **Roles**: `Administrator` (gestión total) / `Collaborator` (operación diaria) vía políticas `[Authorize]`.
- **Primer arranque**: seed de usuario admin desde variables de entorno (instalación por cliente).

## 5. Auditoría, soft-delete y backups
- Interceptor EF llena `CreatedBy/UpdatedBy/At` y escribe `AuditLog` (quién, qué, cuándo) → Timeline.
- **Soft delete** global (recuperabilidad); borrado físico solo administrativo.
- **Backups**: contenedor/cron con `pg_dump` nocturno, rotación de retención, runbook de restore en `/docs`; recomendado copiar dumps a almacenamiento externo. Volumen dedicado para backups.

---

## 6. Infraestructura Docker
- Dockerfiles **multi-stage** (api: SDK→runtime; web: node build→Nginx).
- `docker-compose.yml` (dev) y `docker-compose.prod.yml` (prod) con servicios `web`, `api`, `db`, `proxy`, `backup`.
- **Una sola imagen por servicio para todos los clientes**; cada instalación se diferencia solo por `.env` (dominio, branding, JWT secret, credenciales DB, admin seed). `.env.example` documentado.
- Volúmenes persistentes para `db` y `backups`. Healthchecks y `depends_on`.

---

## 7. Roadmap por fases

**Fase 0 — Scaffolding** (base técnica)
Monorepo, solución .NET (4 proyectos + tests), Angular 21 + Tailwind, `BaseEntity`/auditable/soft-delete, `DbContext` + 1ª migration, docker-compose dev, auth skeleton (login/refresh/roles), seed admin, health endpoints.

**Fase 1 — MVP (Esencial + Fiados)** ← objetivo de lanzamiento en Bodega Martita
Auth+Roles completo · Productos + conversión de unidades · Compras (suma stock) · Inventario híbrido (almacén/mostrador, ajustes, transferencias, ledger) · Cierre Diario (con comisión POS) · Gastos · **Fiados Simples** · Dashboard básico · Auditoría + Timeline · Backups operativos · Hardening de seguridad.

**Fase 2 — Inteligencia y reportes**
NRUS (estimación + alertas + disclaimer) · Reportes PDF (QuestPDF) y Excel (ClosedXML) · Alertas inteligentes (stock bajo, gasto alto, ventas bajas, consumo, críticos) · Dashboard enriquecido (más vendidos/rentables) · estimaciones de rotación.

**Fase 3 — Pulido y replicación**
Optimización UX/perf, tooling de instalación/replicación por cliente, documentación de onboarding de nuevos comercios, mejoras de estimación.

---

## 8. Convenciones
- **C#**: PascalCase tipos/métodos, `_camelCase` campos privados, sufijo `Async`. **DB**: snake_case.
- **API**: `/api/v1/...`, plural, kebab donde aplique. **Angular**: archivos kebab-case, standalone, signals, feature folders.
- **Git**: monorepo, Conventional Commits, ramas por feature. **ADRs** en `/docs` para decisiones clave.

## Verificación (end-to-end, al implementar)
1. `docker compose up` levanta `web`, `api`, `db`, `proxy`; API responde en `/health`.
2. EF migrations aplican y seed crea admin; `POST /auth/login` devuelve access token + setea cookie refresh; `/auth/refresh` rota token.
3. Crear producto galleta soda (10 paq × S/6, 6 u/paq) → verifica `UnitCost=1`, `Margin=0.50`; registrar compra incrementa `WarehouseStock`.
4. Transferencia almacén→mostrador y ajuste por merma reflejan el ledger `StockMovement`.
5. Registrar Cierre Diario con POS → `GrossIncome`/`NetFlow`/comisión correctos; Gasto enlazado.
6. Fiado: crear cliente, registrar fiado "Pan x4 / Leche x2", registrar pago, verificar `Balance`.
7. `AuditLog` registra cambios (quién/qué/cuándo) y aparecen en Timeline; backup `pg_dump` genera dump restaurable.
8. Tests: unit (cálculos de conversión/cierre) + integración con Postgres (Testcontainers).
