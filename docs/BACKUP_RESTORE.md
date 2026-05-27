# Backups y restauración (PostgreSQL)

## Estrategia
- El servicio `backup` del `docker-compose` ejecuta `pg_dump` comprimido **al iniciar y cada día ~02:00**.
- Los dumps se guardan en el volumen `backups` (`/backups` dentro del contenedor) con nombre
  `<db>_AAAAMMDD_HHMMSS.sql.gz`.
- Retención configurable con `BACKUP_RETENTION_DAYS` (por defecto 14 días); los más antiguos se eliminan.
- Recomendado: copiar periódicamente los dumps a almacenamiento externo (otro servidor u object storage).
- Complemento a nivel de datos: **soft delete** (los registros borrados son recuperables en la BD).

## Listar / copiar backups
```bash
cd docker
docker compose exec backup ls -lh /backups
# copiar un dump al host:
docker compose cp backup:/backups/<archivo>.sql.gz ./<archivo>.sql.gz
```

## Restaurar
> Restaurar sobrescribe datos. Hazlo con la app detenida y validando el archivo de origen.

```bash
cd docker
# 1) detener la API para que nadie escriba durante la restauración
docker compose stop api

# 2) restaurar el dump elegido sobre la base
gunzip -c /ruta/al/<archivo>.sql.gz | \
  docker compose exec -T db psql -U "$POSTGRES_USER" -d "$POSTGRES_DB"

# 3) reiniciar la API
docker compose start api
```

## Backup manual puntual
```bash
docker compose exec backup sh -c 'pg_dump --no-owner --no-privileges "$PGDATABASE" | gzip > /backups/manual_$(date +%Y%m%d_%H%M%S).sql.gz'
```
