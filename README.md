# Merkatto

Plataforma web de **gestión operativa para bodegas y minimarkets peruanos**.
Diseñada para la realidad de pequeños comercios: cuadre al final del día, compra al por mayor
y venta por unidad, inventario aproximado y pagos en efectivo / Yape / Plin / POS.

> Filosofía: **"Controla tu negocio sin cambiar la forma en que trabajas."**

## Modelo de despliegue

**Single-tenant por negocio.** Cada comercio tiene su propio frontend, backend y base de datos
PostgreSQL. La escalabilidad se logra replicando deployments, no con multi-tenancy lógica.
Todas las instalaciones usan el **mismo código**; cada cliente se diferencia solo por su `.env`.

## Stack

| Capa      | Tecnología                                            |
|-----------|-------------------------------------------------------|
| Frontend  | Angular 21 (standalone, signals) + TailwindCSS        |
| Backend   | ASP.NET Core Web API (.NET 10) — modular monolith     |
| Base de datos | PostgreSQL + EF Core                              |
| Auth      | JWT corto (en memoria) + Refresh token (cookie httpOnly) |
| Infra     | Docker / docker-compose + reverse proxy con HTTPS     |

## Estructura del repositorio

```
/backend    Solución .NET (Domain, Application, Infrastructure, Api) + tests
/frontend   Aplicación Angular 21
/docker     Dockerfiles, docker-compose, configs de despliegue
/docs       Arquitectura, runbooks (instalación, backup/restore), ADRs
```

## Desarrollo local

Requisitos: .NET 10 SDK, Node 20+, Docker.

```bash
# Backend
cd backend && dotnet restore && dotnet build

# Frontend
cd frontend && npm install && npm start

# Stack completo
cd docker && docker compose up
```

Ver el plan de arquitectura completo en `docs/`.
