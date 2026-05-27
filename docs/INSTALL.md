# Instalación de Merkatto para un nuevo negocio

Cada negocio = una instalación independiente (frontend + backend + PostgreSQL propios).
Todas las instalaciones usan **las mismas imágenes**; solo cambia el archivo `.env`.

## Requisitos del servidor
- Docker + Docker Compose.
- Un dominio público apuntando al servidor (para HTTPS automático con Caddy).
- Puertos 80 y 443 abiertos.

## Pasos

1. Clonar el repositorio en el servidor del cliente.
2. Configurar el entorno:
   ```bash
   cd docker
   cp .env.example .env
   ```
3. Editar `.env` con los valores del cliente:
   - `DOMAIN` y `PUBLIC_URL` → dominio del negocio (p.ej. `martita.tudominio.pe`).
   - `POSTGRES_PASSWORD` → contraseña fuerte.
   - `JWT_SIGNING_KEY` → `openssl rand -base64 48`.
   - `ADMIN_EMAIL` / `ADMIN_PASSWORD` → credenciales del primer administrador.
4. Levantar el stack:
   ```bash
   docker compose up -d --build
   ```
   En el primer arranque la API aplica las migraciones y crea el administrador (`DbInitializer`).
5. Entrar a `https://<DOMAIN>` e iniciar sesión con el admin sembrado. Cambiar la contraseña.

## Actualizaciones
```bash
git pull
docker compose up -d --build   # las nuevas migraciones se aplican al iniciar la API
```

## Notas de seguridad
- El `.env` real **nunca** se versiona (está en `.gitignore`).
- HTTPS es obligatorio en producción; Caddy gestiona los certificados automáticamente.
- El refresh token vive en cookie `httpOnly/Secure/SameSite=Strict`; el access token solo en memoria del navegador.
