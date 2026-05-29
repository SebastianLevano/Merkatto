# Diseño — Merkatto instalable de escritorio (offline) + nube

> **Estado:** diseño aprobado para discusión, **sin implementar todavía**.
> Decisiones tomadas (2026-05-29):
> 1. **Dos sabores desde un solo código base**: escritorio offline (PC del cliente, SQLite)
>    y nube (VPS, PostgreSQL — modelo actual del `RUNBOOK.md`).
> 2. **Instalaciones independientes** — sin consola central. Cada bodega es un sistema
>    aislado; vos provisionás su admin. Coincide con la filosofía single-tenant del proyecto.
> 3. **Cambio de contraseña obligatorio** al primer ingreso, en ambos sabores.

---

## 1. Objetivo

Que puedas **vender Merkatto como un programa que el cliente instala en su PC** (un `.exe`,
sin Docker, sin conocimientos técnicos), que **funcione sin internet**, y que **se
actualice solo** cuando vos publicás una versión nueva — sin perder el camino actual de
hospedarlo en la nube para clientes que lo prefieran.

| | **Escritorio (nuevo)** | **Nube (actual)** |
|---|---|---|
| Dónde corre | PC del cliente | VPS que vos hospedás |
| Base de datos | SQLite (archivo) | PostgreSQL |
| Internet | No requiere | Requiere |
| Entrega | Instalador Windows `.exe` | URL + login |
| Actualización | Auto-update (cliente baja de tu feed) | `git pull` + redeploy (vos) |
| Costo recurrente | Ninguno | VPS por cliente |
| Backups | Copia local del `.db` + export | Contenedor `backup` (pg_dump) |

El principio "una sola imagen para todos los clientes, diferenciada solo por configuración"
se mantiene: **el instalador es el mismo para todas las bodegas**; lo específico de cada una
(nombre del negocio, admin) se define en el **primer arranque**, no se hornea en el `.exe`.

---

## 2. Arquitectura: un código, dos sabores

La regla de dependencias actual no cambia (`Api → Application → Domain`,
`Infrastructure → Application/Domain`). Lo único que se vuelve "intercambiable" es:

1. **El proveedor de base de datos** (Npgsql ↔ SQLite), elegido por configuración.
2. **Cómo se sirve el frontend** (Nginx/Caddy en nube ↔ el propio backend en escritorio).
3. **El anfitrión del proceso** (contenedor Docker ↔ ejecutable de escritorio con ventana).

Se agrega **un solo proyecto nuevo**: `src/Merkatto.Desktop` — un lanzador que arranca el
host web de `Merkatto.Api` en proceso, sobre un puerto local, y abre una ventana de
aplicación apuntando a ese `localhost`. Todo lo demás (Domain/Application/Infrastructure/Api)
se reutiliza tal cual.

```
NUBE                                ESCRITORIO
[Caddy] → [web: Nginx+Angular]      [Merkatto.Desktop .exe]
        → [api: .NET] → [Postgres]    ├─ Kestrel (Api) en http://localhost:PUERTO
                                       │     └─ sirve Angular desde wwwroot
                                       ├─ ventana webview → localhost:PUERTO
                                       └─ SQLite en %PROGRAMDATA%\Merkatto\merkatto.db
```

---

## 3. Capa de datos: PostgreSQL + SQLite  ⚠️ (lo más delicado)

### 3.1 Selección de proveedor por configuración

`AppDbContext`/`AddInfrastructure` lee `Database:Provider` (`Postgres` | `Sqlite`):

- `Postgres` → `UseNpgsql(...)` (como hoy).
- `Sqlite`  → `UseSqlite("Data Source=<ruta>/merkatto.db")`.

La convención **snake_case** (`EFCore.NamingConventions`) funciona igual en ambos.

### 3.2 Migraciones separadas por proveedor

Las migraciones de EF Core son **específicas del proveedor** (tipos y SQL distintos). No se
comparte una sola carpeta. Solución estándar:

```
Infrastructure/Persistence/Migrations/Postgres/   ← las actuales
Infrastructure/Persistence/Migrations/Sqlite/     ← nuevas, generadas con el provider Sqlite
```

Se selecciona el `MigrationsAssembly`/carpeta según el proveedor activo. `DbInitializer`
sigue aplicando migraciones al arrancar; solo cambia cuál set usa.

### 3.3 EL RIESGO PRINCIPAL: dinero en SQLite

SQLite **no tiene tipo decimal nativo**. EF Core guarda `decimal` como TEXTO para no perder
precisión, pero entonces las operaciones **traducidas a SQL** (`Sum`, `Average`, `OrderBy`,
`Max`/`Min` sobre decimales) **dan resultados incorrectos o lanzan excepción**. Esto es
crítico en un sistema de plata.

En el código actual hay agregaciones de dinero que **se ejecutan en el servidor (SQL)** y por
lo tanto romperían en SQLite:

| Archivo | Qué suma | Server-side |
|---|---|---|
| `CreditService.cs:28` | saldo: `Sales.Sum(TotalAmount) − Payments.Sum(Amount)` | sí (en `Select`) |
| `AlertService.cs:77-78` | saldo de fiados | sí |
| `DashboardService.cs:29-30` | ventas/pagos de crédito | sí |
| `ExpenseService.cs:30,33` | gastos por tipo (`GroupBy`) | sí |
| `ProductService.cs:37`, `InventoryService.cs:33` | `Sum(Quantity)` de movimientos de stock | sí (decimal) |

> Las sumas que ya operan sobre listas materializadas (`ReportService`, `PurchaseService`,
> `CreditService:44`) **no** corren riesgo: son LINQ-to-Objects en C#.

**Dos caminos para resolverlo** (a elegir cuando implementemos):

- **A — Dinero como enteros (céntimos) con value converter global. (Recomendado)**
  Mapear cada propiedad monetaria `decimal` a `long` (céntimos) vía un converter aplicado por
  convención, en **ambos** proveedores. Las sumas pasan a ser sobre enteros → exactas y
  soportadas en SQLite y Postgres por igual; mismos resultados en los dos lados. Costo: tocar
  el mapeo de las columnas de dinero y regenerar migraciones; las filas crudas en la BD quedan
  en céntimos (menos legibles en `psql`, irrelevante para el usuario).

- **B — Forzar agregación en cliente solo en SQLite.**
  Materializar (`ToListAsync`) antes de sumar en los ~6 puntos de arriba. El volumen de una
  bodega es minúsculo (cientos de filas/mes), así que el costo de performance es nulo. Costo:
  el código queda "consciente del proveedor" en varios lugares (más frágil de mantener).

Recomiendo **A** por correctitud y uniformidad. Además: usar `DateOnly` (ya en uso) que EF
mapea a TEXTO ISO en SQLite y ordena bien; revisar `DateTimeOffset` (auditoría) y `jsonb`
(`AuditLog.OldValues/NewValues`) → en SQLite van como TEXTO (config por proveedor).

### 3.4 Verificación obligatoria

Los **tests de integración** deben correrse **también contra SQLite** (hoy usan
Testcontainers/Postgres). Un test que cree cierres/fiados y verifique que los totales del
dashboard y los saldos coinciden **al céntimo** entre ambos proveedores es la red de
seguridad de esta migración.

---

## 4. Servir el SPA desde el backend + ventana de escritorio

En nube, Nginx sirve Angular y Caddy hace TLS. En escritorio **no hay proxy**: el propio
`Merkatto.Api` sirve el SPA.

- `UseStaticFiles()` + `MapFallbackToFile("index.html")`; el build de Angular (`npm run build`)
  se copia a `wwwroot`.
- El frontend de producción ya usa **same-origin** `/api/v1` → funciona sin cambios cuando el
  backend sirve el SPA en el mismo origen.
- **Ventana de la app** (para que se sienta un programa, no un navegador):
  - **Photino.NET** *(recomendado)* — ventana nativa con el webview del sistema, liviano y
    100% .NET, multiplataforma. El proceso arranca Kestrel y abre la ventana a `localhost`.
  - **WebView2** — alternativa nativa de Windows (motor Edge), también liviana, solo Windows
    (aceptable: el target son bodegas con Windows).
  - *(Descarte)* Electron: funciona pero pesa ~150 MB de Chromium; innecesario teniendo backend
    .NET.

### 4.1 Cookie de refresh en `localhost`  ⚠️ verificar

El refresh token va en cookie **`Secure` + `SameSite=Strict`**. `Secure` exige HTTPS. En
escritorio el origen es `http://localhost:PUERTO`. `localhost` es "contexto seguro" según los
navegadores, pero el comportamiento exacto de cookies `Secure` sobre `http://localhost` hay que
**verificarlo en el webview elegido**. Plan seguro: que Kestrel sirva **HTTPS local con un
certificado de desarrollo autofirmado** generado en la instalación, de modo que el origen sea
`https://localhost` y la config de cookies sea idéntica a la de nube.

---

## 5. Auto-actualización (Velopack)

Como la PC del cliente está detrás de su router (sin IP pública), **no se puede "empujar"**.
El patrón es **pull**: la app consulta tu **feed de releases** al arrancar (y/o periódicamente),
baja el delta y aplica la actualización en el próximo reinicio. Para vos el efecto es el mismo
que empujar: **publicás una versión → todos los clientes se actualizan solos**.

- Herramienta: **Velopack** (.NET): genera instalador, releases y **deltas**; soporta rollback;
  el feed puede ser un bucket estático (S3/Backblaze/B2) o GitHub Releases.
- Flujo tuyo para publicar: `vpk pack` → subir al feed. Sin tocar la PC del cliente.
- **Migraciones + datos en cada update**: al actualizar, la versión nueva corre sus migraciones
  EF sobre el `merkatto.db`. **Antes de migrar, copiar el `.db`** (backup pre-migración) para
  que una migración fallida no corrompa datos.
- **Ubicación de datos crítica:** la base de datos y los backups **deben vivir FUERA de la
  carpeta de la app** (que Velopack reemplaza en cada update). Van en
  `%PROGRAMDATA%\Merkatto\` (no en el directorio versionado de instalación).

---

## 6. Primer arranque / provisión por bodega

Mismo instalador para todas las bodegas. Lo específico se define al instalar:

- **Asistente de primer arranque** (cuando la BD está vacía): nombre del negocio + creación del
  usuario admin (email + contraseña temporal). Lo completás vos en la entrega.
- Opcional: dejar junto al `.exe` un `client.json` por cliente (nombre del negocio, email del
  admin) para pre-rellenar el asistente y estandarizar la entrega.
- El **cambio de contraseña obligatorio** (sección 7) garantiza que, tras la entrega, solo el
  cliente conozca su clave.
- Licencia: opcional y **offline** (clave validada localmente) si querés legitimar instalaciones.
  Una consola central / phone-home queda **descartada** por la decisión de instalaciones
  independientes.

---

## 7. Cambio de contraseña obligatorio (ambos sabores)

- **Dominio:** agregar `MustChangePassword: bool` a `User`. El admin sembrado y **todo usuario
  creado por un admin** arrancan en `true`. Migración en ambos proveedores.
- **Backend:** el login sigue devolviendo token, pero la respuesta (y `/auth/me`) incluye
  `mustChangePassword`. **Defensa en profundidad:** un filtro rechaza todos los endpoints salvo
  `auth/*`, `me` y `change-password` mientras la bandera sea `true` (no solo control en el
  cliente). `change-password` la pone en `false`.
- **Frontend:** si `mustChangePassword`, el guard redirige a una pantalla de cambio obligatorio y
  bloquea la navegación al dashboard hasta completarlo.

---

## 8. Gestión de usuarios in-app (instalaciones independientes)

Para crear admins/colaboradores **dentro de cada instalación** (el rol `Collaborator` ya existe
en el dominio, falta exponerlo):

- **Endpoints** (solo `Administrator`): `GET /users`, `POST /users` (email, rol, contraseña
  temporal, `MustChangePassword=true`), `PUT /users/{id}` (rol, activo), `POST
  /users/{id}/reset-password`, baja lógica con `DELETE`.
- **Protección anti-bloqueo:** no permitir desactivar/eliminar al propio usuario ni al **último
  admin activo**.
- **Frontend:** feature `users/` + entrada en el sidebar visible solo para admin.

---

## 9. Backups en escritorio

La nube tiene el contenedor `backup` (pg_dump nocturno). En escritorio no hay cron de contenedor:

- **Automático:** al arrancar y/o una vez al día, copiar `merkatto.db` a
  `%PROGRAMDATA%\Merkatto\backups\merkatto_AAAAMMDD_HHMMSS.db`, con rotación (N copias).
- **Manual:** botón "Exportar respaldo" en Configuración → guardar el `.db` donde el cliente
  elija (USB, Drive).
- **Restauración:** opción para reemplazar la BD desde un respaldo (app cerrada/reiniciada).

---

## 10. Estructura de proyectos y build/publish

```
backend/src/
  Merkatto.Domain | Application | Infrastructure | Api   ← sin cambios estructurales
  Merkatto.Desktop/                                       ← NUEVO: lanzador (Photino/WebView2)
```

- **Frontend:** un único `npm run build`. Nube → al contenedor Nginx (como hoy). Escritorio →
  copiado a `wwwroot` del host .NET por un paso de build.
- **Publicar nube:** Docker, sin cambios.
- **Publicar escritorio:**
  `dotnet publish src/Merkatto.Desktop -c Release -r win-x64 --self-contained` (incluye runtime
  .NET y SQLite) → `vpk pack` produce instalador + entrada de release → subir al feed.

---

## 11. Riesgos y puntos de verificación

| Riesgo | Mitigación |
|---|---|
| **Decimales en SQLite** (sección 3.3) | Céntimos enteros (camino A) + tests cruzados Postgres/SQLite al céntimo |
| Cookie `Secure` en `http://localhost` | Kestrel HTTPS local con cert de desarrollo |
| DB dentro de la carpeta que el updater reemplaza | Datos en `%PROGRAMDATA%`, fuera del install dir |
| Migración fallida corrompe `.db` | Backup pre-migración automático |
| Lockout de admin | Bloquear baja del último admin/propio usuario |
| Divergencia de comportamiento entre sabores | Tests de integración corriendo en **ambos** proveedores |

---

## 12. Secuencia de implementación sugerida (cuando se apruebe codear)

1. **Abstracción de proveedor + migraciones SQLite** y **dinero en céntimos** (fundacional);
   tests de integración cruzados Postgres/SQLite al céntimo. *(Mayor esfuerzo y riesgo: primero.)*
2. **Cambio de contraseña obligatorio** + **gestión de usuarios in-app** (independiente del
   empaquetado, bajo riesgo, sirve ya en nube).
3. **Servir SPA desde el backend** + **ventana de escritorio** (Photino/WebView2) + HTTPS local.
4. **Primer arranque / asistente** + **backups de escritorio**.
5. **Auto-update (Velopack)** + **instalador** + **feed de releases**; backup pre-migración.

Cada fase es entregable y verificable por separado; la 2 podría adelantarse e ir a producción
en el sabor nube sin esperar al resto.
