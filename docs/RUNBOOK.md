# Runbook de instalación — Merkatto

Guía paso a paso para desplegar Merkatto en un VPS para un nuevo negocio.
Cada instalación es **completamente independiente**: un dominio, un servidor (o directorio),
una base de datos. Las imágenes Docker son las mismas para todos los clientes; solo cambia
el `.env`.

---

## Índice

1. [Requisitos](#1-requisitos)
2. [Preparar el VPS](#2-preparar-el-vps)
3. [Configurar el DNS](#3-configurar-el-dns)
4. [Instalar Docker en el servidor](#4-instalar-docker-en-el-servidor)
5. [Desplegar Merkatto](#5-desplegar-merkatto)
6. [Verificar que todo funciona](#6-verificar-que-todo-funciona)
7. [Operaciones habituales](#7-operaciones-habituales)
8. [Backups y restauración](#8-backups-y-restauración)
9. [Segunda instalación (nuevo cliente)](#9-segunda-instalación-nuevo-cliente)
10. [Troubleshooting](#10-troubleshooting)

---

## 1. Requisitos

### Servidor (VPS)

| Recurso | Mínimo recomendado |
|---------|-------------------|
| CPU     | 1 vCPU            |
| RAM     | 1 GB              |
| Disco   | 20 GB SSD         |
| OS      | Ubuntu 22.04 LTS  |

Proveedores probados: **Hetzner Cloud** (CX11 ~€4/mes), DigitalOcean, Contabo.

### Dominio

Un subdominio o dominio apuntando al IP del servidor, por ejemplo:
- `martita.midominio.pe`
- `bodega-elena.midominio.com`

Caddy obtiene el certificado TLS automáticamente vía Let's Encrypt. **Requiere que el
dominio ya resuelva al IP del servidor antes del primer arranque.**

### Local (para el operador)

- SSH al servidor (clave o contraseña)
- `git` instalado en el servidor

---

## 2. Preparar el VPS

Conectarse como root (o un usuario con sudo):

```bash
ssh root@<IP_DEL_SERVIDOR>
```

### Actualizar el sistema

```bash
apt update && apt upgrade -y
```

### Crear un usuario no-root (recomendado)

```bash
adduser merkatto
usermod -aG sudo merkatto
# Copiar tu clave SSH al nuevo usuario
rsync --archive --chown=merkatto:merkatto ~/.ssh /home/merkatto
```

### Configurar el firewall

```bash
ufw allow ssh
ufw allow 80/tcp
ufw allow 443/tcp
ufw enable
```

---

## 3. Configurar el DNS

En el panel de tu registrador de dominio, agregar un registro A:

| Tipo | Nombre         | Valor          | TTL  |
|------|----------------|----------------|------|
| A    | `martita`      | `<IP_SERVIDOR>`| 300s |

Verificar que el dominio resuelva (puede tardar hasta 5 minutos con TTL bajo):

```bash
# desde cualquier máquina
dig +short martita.midominio.pe
# debe devolver el IP del servidor
```

> **Importante:** Caddy fallará si el dominio no resuelve al momento del primer arranque.
> Confirmar la resolución antes de continuar.

---

## 4. Instalar Docker en el servidor

```bash
# Instalar Docker Engine (script oficial)
curl -fsSL https://get.docker.com | sh

# Agregar el usuario al grupo docker (evita usar sudo en cada comando)
usermod -aG docker merkatto

# Cerrar sesión y volver a entrar para que el grupo tome efecto
exit
ssh merkatto@<IP_DEL_SERVIDOR>

# Verificar
docker --version
docker compose version
```

---

## 5. Desplegar Merkatto

### 5.1 Clonar el repositorio

```bash
git clone <URL_DEL_REPO> /opt/merkatto-martita
cd /opt/merkatto-martita/docker
```

> Convención: un directorio por cliente en `/opt/merkatto-<nombre>`.

### 5.2 Crear el archivo .env

```bash
cp .env.example .env
nano .env   # o vim .env
```

Completar **todos** los valores:

```dotenv
# Dominio del cliente (Caddy gestiona HTTPS automáticamente)
DOMAIN=martita.midominio.pe
PUBLIC_URL=https://martita.midominio.pe

# Base de datos
POSTGRES_DB=merkatto
POSTGRES_USER=merkatto
POSTGRES_PASSWORD=<contraseña-fuerte-aleatoria>

# JWT — generar con: openssl rand -base64 48
JWT_SIGNING_KEY=<clave-de-al-menos-32-chars>
JWT_ISSUER=Merkatto
JWT_AUDIENCE=Merkatto

# Administrador inicial (se crea una sola vez)
ADMIN_EMAIL=admin@martita.pe
ADMIN_NAME=Administrador
ADMIN_PASSWORD=<contraseña-inicial-fuerte>

# Nombre del negocio (también editable desde Configuración en la app)
BUSINESS_NAME=Bodega Martita

# Backups: días de retención
BACKUP_RETENTION_DAYS=14
```

Generar contraseñas seguras:

```bash
# JWT signing key
openssl rand -base64 48

# Contraseñas de base de datos y admin
openssl rand -base64 24
```

### 5.3 Levantar el stack

```bash
cd /opt/merkatto-martita/docker
docker compose up -d --build
```

El primer arranque tarda 2-4 minutos (descarga imágenes base, compila la app).
En el primer inicio la API aplica las migraciones y crea el usuario administrador
automáticamente.

### 5.4 Verificar los logs de inicio

```bash
docker compose logs -f api
# Buscar: "Application started" y "Seeded administrator"
# Ctrl+C para salir
```

---

## 6. Verificar que todo funciona

### 6.1 Health check de la API

```bash
curl https://martita.midominio.pe/health
# Debe responder: Healthy
```

### 6.2 Entrar a la aplicación

1. Abrir `https://martita.midominio.pe` en el navegador.
2. Iniciar sesión con `ADMIN_EMAIL` y `ADMIN_PASSWORD`.
3. Ir a **Configuración** → cambiar la contraseña del administrador.
4. Ir a **Configuración** → verificar que el nombre del negocio es correcto.

### 6.3 Checklist de verificación E2E

- [ ] HTTPS activo (candado en el navegador, certificado de Let's Encrypt)
- [ ] Login funciona y devuelve token
- [ ] Dashboard carga sin errores
- [ ] Crear un producto de prueba (ej: Galleta S/.1.50)
- [ ] Registrar una compra de prueba
- [ ] Hacer un cierre diario de prueba
- [ ] Confirmar que el backup inicial se generó:
  ```bash
  docker compose exec backup ls -lh /backups
  ```

---

## 7. Operaciones habituales

### Ver estado de los contenedores

```bash
cd /opt/merkatto-martita/docker
docker compose ps
```

### Ver logs en tiempo real

```bash
docker compose logs -f          # todos los servicios
docker compose logs -f api      # solo el backend
docker compose logs -f web      # solo el frontend
docker compose logs -f proxy    # Caddy (útil para ver si obtiene el cert)
```

### Reiniciar un servicio

```bash
docker compose restart api
```

### Apagar y levantar todo

```bash
docker compose down        # apaga (los volúmenes de datos no se borran)
docker compose up -d       # vuelve a levantar
```

### Actualizar a una nueva versión del código

```bash
cd /opt/merkatto-martita
git pull
cd docker
docker compose up -d --build
# Las migraciones nuevas se aplican automáticamente al arrancar la API
```

### Ingresar a la base de datos (psql)

```bash
docker compose exec db psql -U merkatto -d merkatto
```

---

## 8. Backups y restauración

### Estrategia automática

El servicio `backup` corre `pg_dump` al iniciar y cada día a las 02:00 UTC.
Los dumps se guardan como `merkatto_AAAAMMDD_HHMMSS.sql.gz` en el volumen `backups`.
Retención: `BACKUP_RETENTION_DAYS` días (por defecto 14).

### Listar backups existentes

```bash
docker compose exec backup ls -lh /backups
```

### Hacer un backup manual ahora

```bash
docker compose exec backup sh -c \
  'pg_dump --no-owner --no-privileges "$PGDATABASE" | gzip > /backups/manual_$(date +%Y%m%d_%H%M%S).sql.gz'
```

### Copiar un backup al servidor local

```bash
# Desde tu máquina local:
scp merkatto@<IP>:/opt/merkatto-martita/docker/$(docker -H ssh://merkatto@<IP> \
  compose -f /opt/merkatto-martita/docker/docker-compose.yml \
  exec backup ls /backups | tail -1) ./backup-local.sql.gz

# Alternativa más simple desde el servidor:
docker compose cp backup:/backups/<archivo>.sql.gz /tmp/
# Luego descargar con scp desde local
scp merkatto@<IP>:/tmp/<archivo>.sql.gz .
```

### Copiar backups a almacenamiento externo (recomendado)

Agregar un cron en el servidor para subir los dumps a un bucket S3 / Backblaze B2:

```bash
# Instalar rclone (https://rclone.org)
curl https://rclone.org/install.sh | bash
rclone config   # configurar el proveedor de storage

# Cron: copiar backups cada día a las 03:00
crontab -e
# Agregar:
0 3 * * * rclone copy /var/lib/docker/volumes/merkatto-martita_backups/_data/ remote:merkatto-martita-backups/
```

### Restaurar un backup

> **Atención:** restaurar sobrescribe todos los datos actuales. Hacerlo con la app detenida.

```bash
cd /opt/merkatto-martita/docker

# 1. Detener la API (para que no haya escrituras mientras se restaura)
docker compose stop api

# 2. Restaurar el dump
gunzip -c /ruta/al/archivo.sql.gz | \
  docker compose exec -T db psql -U merkatto -d merkatto

# 3. Reiniciar la API
docker compose start api
```

---

## 9. Segunda instalación (nuevo cliente)

Cada cliente es un stack completamente aislado. Para agregar un segundo negocio:

```bash
# Clonar el repo en un directorio propio del cliente
git clone <URL_DEL_REPO> /opt/merkatto-bodegaelena
cd /opt/merkatto-bodegaelena/docker

# Crear su propio .env
cp .env.example .env
nano .env  # dominio diferente, contraseñas diferentes, nombre diferente

# Levantar con un project name distinto (evita colisión de nombres de red/volúmenes)
docker compose -p merkatto-elena up -d --build
```

> Los volúmenes de Docker se nombran con el project name como prefijo, así que
> `merkatto-elena_db-data` y `merkatto-martita_db-data` son completamente separados.

Si ambos clientes están en el mismo servidor, el proxy Caddy de cada stack escucha en
puertos 80/443 — **solo uno puede hacerlo**. Dos opciones:

**Opción A (recomendada) — Caddy global único:**
Configurar un único Caddy en el servidor que enrute por dominio a cada stack de API/web,
y que cada stack exponga solo los contenedores internos (sin `ports` en el compose).

**Opción B — Un servidor por cliente:**
La más simple: un VPS por negocio. Evita cualquier riesgo de colisión. Recomendada
mientras el número de clientes sea pequeño.

---

## 10. Troubleshooting

### La API no arranca — "Auth:SigningKey must be set"

El `JWT_SIGNING_KEY` en `.env` contiene "change-me" o está vacío.
Generar uno válido y actualizar el `.env`:

```bash
openssl rand -base64 48
# Editar .env y reiniciar:
docker compose up -d
```

### Caddy no obtiene el certificado TLS

1. Verificar que el dominio resuelve al IP correcto: `dig +short <DOMAIN>`
2. Verificar que los puertos 80 y 443 están abiertos: `ufw status`
3. Ver logs de Caddy: `docker compose logs proxy`

### La API responde 500 al iniciar sesión

Puede ser un problema de migración. Ver logs de la API:

```bash
docker compose logs api | grep -i "error\|migration\|fail"
```

Si hay un error de migración, intentar:

```bash
docker compose restart api
```

### No se generan backups

Verificar que el contenedor `backup` está corriendo y tiene acceso a la BD:

```bash
docker compose ps backup
docker compose logs backup
```

### Reiniciar todo desde cero (en desarrollo/staging, NO en producción con datos)

```bash
docker compose down -v   # borra también los volúmenes — DESTRUYE LOS DATOS
docker compose up -d --build
```

### Ver uso de disco

```bash
docker system df                   # uso de Docker (imágenes, volúmenes)
df -h                              # uso del servidor
docker compose exec backup du -sh /backups   # tamaño de backups
```

---

## Referencia rápida

```bash
# Ubicación del proyecto
cd /opt/merkatto-<nombre>/docker

# Comandos más usados
docker compose ps                    # estado
docker compose logs -f api           # logs del backend
docker compose up -d --build         # actualizar y reiniciar
docker compose restart api           # reiniciar solo la API
docker compose exec backup ls /backups  # listar backups
openssl rand -base64 48              # generar clave segura
curl https://<DOMAIN>/health         # verificar que la API responde
```
