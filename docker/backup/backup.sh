#!/bin/sh
# Nightly pg_dump with retention. Runs as a long-lived container; sleeps until the next 02:00 UTC.
set -eu

RETENTION="${BACKUP_RETENTION_DAYS:-14}"
DIR=/backups

run_backup() {
  ts=$(date -u +%Y%m%d_%H%M%S)
  file="$DIR/${PGDATABASE}_${ts}.sql.gz"
  echo "[backup] dumping to $file"
  pg_dump --no-owner --no-privileges "$PGDATABASE" | gzip > "$file"
  echo "[backup] pruning dumps older than ${RETENTION} days"
  find "$DIR" -name "${PGDATABASE}_*.sql.gz" -mtime "+${RETENTION}" -delete
}

# Seconds until next 02:00 UTC — BusyBox-compatible (no leading-zero octal issues)
secs_until_0200() {
  h=$(date -u +%-H 2>/dev/null || date -u +%H | sed 's/^0*//' | grep . || echo 0)
  m=$(date -u +%-M 2>/dev/null || date -u +%M | sed 's/^0*//' | grep . || echo 0)
  s=$(date -u +%-S 2>/dev/null || date -u +%S | sed 's/^0*//' | grep . || echo 0)
  secs_since_midnight=$(( h * 3600 + m * 60 + s ))
  target=7200
  if [ "$secs_since_midnight" -lt "$target" ]; then
    echo $(( target - secs_since_midnight ))
  else
    echo $(( 86400 - secs_since_midnight + target ))
  fi
}

# Run once on start, then daily at ~02:00 UTC.
run_backup || echo "[backup] initial dump failed"
while true; do
  delay=$(secs_until_0200)
  echo "[backup] sleeping ${delay}s until next run"
  sleep "$delay"
  run_backup || echo "[backup] scheduled dump failed"
done
