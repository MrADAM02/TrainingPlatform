#!/usr/bin/env bash
# Nightly PostgreSQL backup to S3-compatible object storage (PRD §8.3).
#
# Intended to run via cron on the host VM, e.g.:
#   0 2 * * * POSTGRES_HOST=... POSTGRES_PASSWORD=... ... /opt/training-platform/scripts/backup-database.sh >> /var/log/training-platform-backup.log 2>&1
#
# Requires: pg_dump (postgresql-client package), mc (MinIO client).
set -euo pipefail

: "${POSTGRES_HOST:?POSTGRES_HOST is required}"
: "${POSTGRES_PORT:=5432}"
: "${POSTGRES_DB:?POSTGRES_DB is required}"
: "${POSTGRES_USER:?POSTGRES_USER is required}"
: "${POSTGRES_PASSWORD:?POSTGRES_PASSWORD is required}"
: "${STORAGE_SERVICE_URL:?STORAGE_SERVICE_URL is required}"
: "${STORAGE_ACCESS_KEY:?STORAGE_ACCESS_KEY is required}"
: "${STORAGE_SECRET_KEY:?STORAGE_SECRET_KEY is required}"
: "${STORAGE_BUCKET:?STORAGE_BUCKET is required}"
: "${BACKUP_RETENTION_DAYS:=30}"

TIMESTAMP=$(date -u +%Y%m%dT%H%M%SZ)
DUMP_FILE="/tmp/${POSTGRES_DB}-${TIMESTAMP}.sql.gz"
REMOTE_KEY="backups/postgres/${POSTGRES_DB}-${TIMESTAMP}.sql.gz"

echo "[backup] Dumping ${POSTGRES_DB}@${POSTGRES_HOST}:${POSTGRES_PORT}..."
PGPASSWORD="$POSTGRES_PASSWORD" pg_dump \
  -h "$POSTGRES_HOST" -p "$POSTGRES_PORT" -U "$POSTGRES_USER" -d "$POSTGRES_DB" \
  --format=plain --no-owner --no-privileges \
  | gzip > "$DUMP_FILE"

echo "[backup] Dump size: $(du -h "$DUMP_FILE" | cut -f1)"

echo "[backup] Uploading to ${STORAGE_BUCKET}/${REMOTE_KEY}..."
mc alias set backup-target "$STORAGE_SERVICE_URL" "$STORAGE_ACCESS_KEY" "$STORAGE_SECRET_KEY" >/dev/null
mc cp "$DUMP_FILE" "backup-target/${STORAGE_BUCKET}/${REMOTE_KEY}"

rm -f "$DUMP_FILE"

echo "[backup] Pruning backups older than ${BACKUP_RETENTION_DAYS} days..."
mc rm --older-than "${BACKUP_RETENTION_DAYS}d" --recursive --force \
  "backup-target/${STORAGE_BUCKET}/backups/postgres/" \
  || echo "[backup] Nothing to prune."

echo "[backup] Done: ${REMOTE_KEY}"
