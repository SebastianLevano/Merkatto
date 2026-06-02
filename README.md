# Merkatto

Plataforma de **gestión operativa para bodegas y minimarkets peruanos**.
Cuadre diario, compra al por mayor, inventario por almacén/mostrador, fiados, reportes y NRUS.

> **"Controla tu negocio sin cambiar la forma en que trabajás."**

---

## Modelo del sistema

Merkatto sigue el modelo **un negocio = una instalación aislada**. No hay multi-tenancy:
cada bodega tiene su propia base de datos, sus propios datos y su propio instalable.

### Roles

| Rol | Quién | Qué puede hacer |
|---|---|---|
| **Administrador** | El dueño/operador del sistema | Solo gestión de usuarios — crear, resetear contraseña, desactivar |
| **Encargado** | El bodeguero o empleado de confianza | Operación completa: inventario, compras, cierres, fiados, reportes |

El Administrador **no** ve el dashboard ni la operación de ninguna bodega. Cada bodega tiene un solo Encargado.

---

## Tres modos de uso

### 1. Escritorio standalone (sin internet)
La bodega corre completamente offline. El usuario admin inicial se crea en el **wizard de primer arranque**. Sin configuración adicional.

### 2. Escritorio con identidad central
El Encargado se autentica contra un servidor central la primera vez (requiere internet) y luego puede trabajar sin conexión usando el **cache de credenciales local**. Los datos operativos siempre son locales (SQLite). Este modelo permite al Administrador gestionar todas sus bodegas desde un solo lugar.

### 3. Panel de administración
El Administrador usa la app en **modo admin** (`client.json` con `"mode":"admin"`): abre una ventana directamente al servidor central donde solo ve la gestión de usuarios. No levanta base de datos ni backend local.

---

## Funcionalidades (MVP completo)

- **Inventario** — stock por almacén y mostrador, transferencias, ajustes, ledger de movimientos
- **Productos** — conversión paquete→unidad, costo desde compras, margen calculado
- **Compras** — por proveedor (texto libre con autocomplete), editar/eliminar con reversión de stock
- **Cierre diario** — efectivo / Yape / Plin / POS, comisión POS, gastos del día, flujo neto
- **Gastos** — CRUD, editar y eliminar (admin)
- **Fiados** — cliente → ventas + pagos → saldo en tiempo real
- **Dashboard** — último cierre, stock bajo, saldo de fiados, productos más rentables
- **Alertas** — stock bajo, cierres pendientes
- **NRUS** — estimación de categoría y cuota mensual
- **Reportes** — PDF (cierres) y Excel (gastos, compras) — se abren en la app nativa del sistema
- **Gestión de usuarios** — crear, editar, resetear contraseña, desactivar (solo Administrador)
- **Configuración** — nombre del negocio, cambio de contraseña
- **Primer arranque** — wizard cuando la BD está vacía (solo modo standalone)
- **Backups escritorio** — automático al arrancar + manual desde UI; rotación 7 copias
- **Auto-update** — Velopack: el instalable se actualiza solo cuando hay una nueva versión publicada

---

## Stack

| Capa | Tecnología |
|---|---|
| Frontend | Angular 21 (standalone, signals) + TailwindCSS v4 |
| Backend | ASP.NET Core (.NET 10) — modular monolith, Clean Architecture pragmática |
| DB escritorio | SQLite (EF Core, schema via `EnsureCreated`) |
| DB nube | PostgreSQL + EF Core (migrations) |
| Auth | JWT corto en memoria + refresh token rotatorio (cookie httpOnly) |
| Ventana desktop | Photino.NET (WKWebView / WebView2 nativo) |
| Auto-update | Velopack |
| Infra nube | Docker / docker-compose + Caddy (HTTPS automático) |

---

## Estructura del repositorio

```
/backend
  src/
    Merkatto.Domain          Entidades, enums, reglas de dominio
    Merkatto.Application     Casos de uso, DTOs, validators, abstracciones
    Merkatto.Infrastructure  EF Core, interceptors (audit/soft-delete), seguridad, broker central
    Merkatto.Api             Controllers, middleware, Program.cs (sabor nube / central)
    Merkatto.Desktop         Host Photino — sabor escritorio (SQLite + Velopack)
  tests/
    Merkatto.UnitTests       Tests de dominio (sin DB)
    Merkatto.IntegrationTests  46 tests: Postgres (Testcontainers) + SQLite in-memory

/frontend                    Angular 21 (features lazy-loaded, señales, Tailwind)
/docker                      Dockerfiles, docker-compose, configs Nginx/Caddy
/desktop                     Scripts de build y ejemplos de client.json
/docs                        PLAN.md, ARCHITECTURE.md, DESKTOP.md, RELEASE.md, RUNBOOK.md
```

---

## Desarrollo local

**Requisitos:** .NET 10 SDK · Node 20+

### Modo escritorio standalone

```bash
# 1. Buildear el SPA
cd frontend && npm install && npm run build
cp -r dist/merkatto-web/browser ../backend/src/Merkatto.Desktop/wwwroot

# 2. Correr
cd ../backend
dotnet run --project src/Merkatto.Desktop
# Credenciales dev: admin@sistema.pe / Admin123$  (appsettings.Development.json)
# BD vacía → aparece wizard de primer arranque
```

Datos en `~/Library/Application Support/Merkatto/` (macOS) o `C:\ProgramData\Merkatto\` (Windows).

### Modo escritorio con central

Crear `backend/src/Merkatto.Desktop/client.json` (gitignoreado):
```json
{ "centralBaseUrl": "http://localhost:5080" }
```
El login valida contra el central y cachea credenciales para uso offline. Sin `client.json` el comportamiento es standalone puro.

### Modo panel admin

```json
{ "mode": "admin", "centralBaseUrl": "http://localhost:5080" }
```
El Desktop abre una ventana al central sin levantar backend local.

### Sabor nube (Docker)

```bash
cd docker
cp .env.example .env   # completar todos los valores
docker compose up --build
```

### Solo API + frontend dev

```bash
cd backend && dotnet run --project src/Merkatto.Api   # API en :5080
cd frontend && npm start                               # dev server en :4200
```

---

## Tests

```bash
cd backend
dotnet test tests/Merkatto.UnitTests         # sin DB, rápidos
dotnet test tests/Merkatto.IntegrationTests  # Postgres (Testcontainers) + SQLite in-memory
```

46 tests de integración corren en ambos proveedores para garantizar paridad. Incluyen tests
del broker de identidad central (online, offline, rechazo, aislamiento por instalación).

---

## Publicar el instalable

```bash
# macOS (desde la raíz del repo)
./desktop/publish-mac.sh 1.0.0
# → desktop/releases-mac/Merkatto.app

# Windows (en PowerShell)
.\desktop\publish-win.ps1 1.0.0
# → desktop/releases-win/
```

El binario es el mismo para todas las bodegas. La configuración por cliente va en `client.json`
junto al ejecutable. Ver plantillas en `desktop/client-bodega.example.json` y
`desktop/client-admin.example.json`. Ver `docs/RELEASE.md` para el proceso completo.

---

## Deploy del servidor central

El servidor central es el sabor nube (`Merkatto.Api` + Postgres + Docker). Es la fuente de
verdad de usuarios para todas las bodegas con identidad centralizada.
Ver `docs/RUNBOOK.md` para la instalación en VPS (pendiente hasta el primer cliente real).

---

## Convenciones de código

- **C#** — PascalCase, `_camelCase` campos privados, sufijo `Async`
- **DB** — snake_case (convención automática EF)
- **API** — `/api/v1/...`, sustantivos en plural
- **Angular** — archivos kebab-case, standalone, signals, feature folders
- **Entidades nuevas** — heredar `BaseEntity`, agregar `DbSet` en `IAppDbContext` y
  `AppDbContext`, crear `IEntityTypeConfiguration`, generar migración

---

## Documentación

| Doc | Contenido |
|---|---|
| `docs/PLAN.md` | Roadmap por fases y decisiones de alcance |
| `docs/ARCHITECTURE.md` | Arquitectura y decisiones técnicas |
| `docs/DESKTOP.md` | Diseño del instalable (offline, central, modos) |
| `docs/RELEASE.md` | Publicar nuevas versiones del instalable |
| `docs/RUNBOOK.md` | Instalación del central en VPS, backups, restore |
| `CLAUDE.md` | Guía para Claude Code (convenciones, comandos, arquitectura) |
