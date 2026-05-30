# Merkatto

Plataforma de **gestión operativa para bodegas y minimarkets peruanos**.
Cuadre diario, compra al por mayor, inventario por almacén/mostrador, fiados, reportes y NRUS.

> **"Controla tu negocio sin cambiar la forma en que trabajás."**

---

## Dos sabores, un solo código

| | **Escritorio** | **Nube** |
|---|---|---|
| Dónde corre | PC del cliente (Windows) | VPS que vos hospedás |
| Base de datos | SQLite (archivo local) | PostgreSQL |
| Internet | No requiere | Requiere |
| Entrega | Instalador `.exe` (Velopack) | URL + login |
| Actualización | Auto-update silencioso | `git pull` + redeploy |

El instalador es el mismo para todas las bodegas. Lo específico de cada cliente (nombre del
negocio, credenciales del admin) se configura en el **wizard de primer arranque**.

---

## Stack

| Capa | Tecnología |
|---|---|
| Frontend | Angular 21 (standalone, signals) + TailwindCSS v4 |
| Backend | ASP.NET Core (.NET 10) — modular monolith, Clean Architecture pragmática |
| DB nube | PostgreSQL + EF Core (migrations) |
| DB escritorio | SQLite (EF Core, mismo modelo, schema via `EnsureCreated`) |
| Auth | JWT corto en memoria + refresh token rotatorio (cookie httpOnly) |
| Ventana desktop | Photino.NET (WKWebView / WebView2 nativo) |
| Auto-update | Velopack — feed estático (GitHub Releases) |
| Infra nube | Docker / docker-compose + reverse proxy HTTPS |

---

## Funcionalidades (MVP completo)

- **Inventario** — stock por almacén y mostrador, transferencias, ajustes, ledger de movimientos
- **Productos** — conversión paquete→unidad, costo desde compras, margen calculado
- **Compras** — por proveedor (texto libre con autocomplete), editar/eliminar con reversión de stock
- **Cierre diario** — efectivo / Yape / Plin / POS, comisión POS, gastos del día, flujo neto
- **Gastos** — CRUD, editar (admin), eliminar (soft-delete)
- **Fiados** — cliente → ventas + pagos → saldo en tiempo real
- **Dashboard** — último cierre, stock bajo, saldo de fiados, productos más rentables
- **Alertas** — stock bajo, cierres pendientes
- **NRUS** — estimación de categoría y cuota mensual
- **Reportes** — PDF y Excel: cierres, gastos y compras (QuestPDF / ClosedXML)
- **Configuración** — nombre del negocio, cambio de contraseña, gestión de usuarios (admin)
- **Primer arranque** — wizard cuando la BD está vacía: nombre del negocio + admin inicial
- **Backups escritorio** — automático al arrancar + manual desde UI; rotación a 7 copias

---

## Estructura del repositorio

```
/backend
  src/
    Merkatto.Domain          Entidades, enums, reglas de dominio
    Merkatto.Application     Casos de uso, DTOs, validators, abstracciones
    Merkatto.Infrastructure  EF Core, interceptors (audit/soft-delete), seguridad
    Merkatto.Api             Controllers, middleware, Program.cs (sabor nube)
    Merkatto.Desktop         Host Photino — sabor escritorio (SQLite + Velopack)
  tests/
    Merkatto.UnitTests       Tests de matemática de dominio (sin DB)
    Merkatto.IntegrationTests Tests E2E: 20 × Postgres + 20 × SQLite

/frontend                    Angular 21 (features lazy-loaded, señales, Tailwind)
/docker                      Dockerfiles, docker-compose, configs Nginx/Caddy
/desktop                     Scripts de build: publish-win.ps1, publish-mac.sh
/docs                        PLAN.md, ARCHITECTURE.md, DESKTOP.md, RELEASE.md, RUNBOOK.md
```

---

## Desarrollo local

**Requisitos:** .NET 10 SDK · Node 20+ · Docker (solo para el sabor nube)

### Sabor escritorio (sin Docker, sin Postgres)

```bash
# 1. Buildear el SPA
cd frontend && npm install && npm run build
cp -r dist/merkatto-web/browser ../backend/src/Merkatto.Desktop/wwwroot

# 2. Correr el Desktop
cd ../backend
ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/Merkatto.Desktop

# La ventana abre en http://localhost:{puerto-aleatorio}
# Si la BD está vacía → aparece el wizard de primer arranque
# Credenciales dev (appsettings.Development.json): admin@desktop.local / Admin123$
```

Datos persistidos en `~/Library/Application Support/Merkatto/` (macOS) o
`C:\ProgramData\Merkatto\` (Windows).

### Sabor nube (Docker)

```bash
cd docker
cp .env.example .env       # editar con tus credenciales
docker compose up --build
# Frontend en :443, API en :5080/api/v1
```

### Solo backend (API + Postgres local)

```bash
cd backend
dotnet run --project src/Merkatto.Api
# Requiere Postgres en localhost:5432 o ajustar appsettings.Development.json
```

### Frontend dev (proxy al API)

```bash
cd frontend && npm start    # dev server en :4200, proxy a API en :5080
```

---

## Tests

```bash
cd backend
dotnet test tests/Merkatto.UnitTests          # sin DB, rápidos
dotnet test tests/Merkatto.IntegrationTests   # Postgres (Testcontainers) + SQLite in-memory
```

Los tests de integración corren en ambos proveedores para garantizar paridad entre sabores.

---

## Publicar una versión del instalable (Windows)

```powershell
# Desde la raíz del repo, en Windows
.\desktop\publish-win.ps1 `
    -Version 1.0.1 `
    -FeedUrl "https://github.com/<owner>/merkatto/releases/"

# Subir a GitHub Releases
gh release create v1.0.1 --title "v1.0.1" --notes "Qué cambió" desktop/releases/*
```

Los clientes instalados se actualizan solos la próxima vez que abren la app.
Ver `docs/RELEASE.md` para el runbook completo.

---

## Convenciones de código

- **C#** — PascalCase, `_camelCase` campos privados, sufijo `Async`
- **DB** — snake_case (convención automática EF)
- **API** — `/api/v1/...`, sustantivos en plural
- **Angular** — archivos kebab-case, standalone, signals, feature folders
- **Entidades nuevas** — heredar `BaseEntity`, agregar `DbSet` en `IAppDbContext` y
  `AppDbContext`, crear `IEntityTypeConfiguration`, generar migration

---

## Documentación adicional

| Doc | Contenido |
|---|---|
| `docs/PLAN.md` | Roadmap por fases y decisiones de alcance |
| `docs/ARCHITECTURE.md` | Resumen de arquitectura y decisiones técnicas |
| `docs/DESKTOP.md` | Diseño del instalable de escritorio (offline + nube) |
| `docs/RELEASE.md` | Cómo publicar una versión nueva del instalable |
| `docs/RUNBOOK.md` | Instalación en VPS, backups, restore |
| `CLAUDE.md` | Guía para Claude Code (convenciones, comandos, arquitectura) |
