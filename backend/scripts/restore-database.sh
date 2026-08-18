#!/usr/bin/env bash
# Restores a backup produced by backup-database.sh.
#
# Usage: ./restore-database.sh <remote-backup-key> <target-db-name>
#
# WARNING: this drops and recreates <target-db-name>. Never point it at a live database unless
# that is genuinely intended — restoring into a fresh/throwaway database name is the safe way
# to verify a backup without touching production data.
set -euo pipefail

REMOTE_KEY="${1:?Usage: restore-database.sh <remote-backup-key> <target-db-name>}"
TARGET_DB="${2:?Usage: restore-database.sh <remote-backup-key> <target-db-name>}"

: "${POSTGRES_HOST:?POSTGRES_HOST is required}"
: "${POSTGRES_PORT:=5432}"
: "${POSTGRES_USER:?POSTGRES_USER is required}"
: "${POSTGRES_PASSWORD:?POSTGRES_PASSWORD is required}"
: "${STORAGE_SERVICE_URL:?STORAGE_SERVICE_URL is required}"
: "${STORAGE_ACCESS_KEY:?STORAGE_ACCESS_KEY is required}"
: "${STORAGE_SECRET_KEY:?STORAGE_SECRET_KEY is required}"
: "${STORAGE_BUCKET:?STORAGE_BUCKET is required}"

DUMP_FILE="/tmp/restore-$(basename "$REMOTE_KEY")"

echo "[restore] Downloading ${REMOTE_KEY}..."
mc alias set backup-target "$STORAGE_SERVICE_URL" "$STORAGE_ACCESS_KEY" "$STORAGE_SECRET_KEY" >/dev/null
mc cp "backup-target/${STORAGE_BUCKET}/${REMOTE_KEY}" "$DUMP_FILE"

echo "[restore] Recreating database '${TARGET_DB}'..."
PGPASSWORD="$POSTGRES_PASSWORD" psql -h "$POSTGRES_HOST" -p "$POSTGRES_PORT" -U "$POSTGRES_USER" -d postgres \
  -c "DROP DATABASE IF EXISTS \"${TARGET_DB}\";" \
  -c "CREATE DATABASE \"${TARGET_DB}\";"

echo "[restore] Restoring dump into '${TARGET_DB}'..."
gunzip -c "$DUMP_FILE" | PGPASSWORD="$POSTGRES_PASSWORD" psql -h "$POSTGRES_HOST" -p "$POSTGRES_PORT" -U "$POSTGRES_USER" -d "$TARGET_DB" -q

rm -f "$DUMP_FILE"
echo "[restore] Done. Restored into '${TARGET_DB}'."
