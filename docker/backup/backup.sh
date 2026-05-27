#!/bin/sh
# Nightly pg_dump with retention. Runs as a long-lived container; sleeps until the next 02:00.
set -eu

RETENTION="${BACKUP_RETENTION_DAYS:-14}"
DIR=/backups

run_backup() {
  ts=$(date +%Y%m%d_%H%M%S)
  file="$DIR/${PGDATABASE}_${ts}.sql.gz"
  echo "[backup] dumping to $file"
  pg_dump --no-owner --no-privileges "$PGDATABASE" | gzip > "$file"
  echo "[backup] pruning dumps older than ${RETENTION} days"
  find "$DIR" -name "${PGDATABASE}_*.sql.gz" -mtime "+${RETENTION}" -delete
}

# Run once on start, then daily at ~02:00.
run_backup || echo "[backup] initial dump failed"
while true; do
  now=$(date +%s)
  next=$(date -d "tomorrow 02:00" +%s 2>/dev/null || date -v+1d -v2H -v0M -v0S +%s)
  sleep $(( next - now ))
  run_backup || echo "[backup] scheduled dump failed"
done
