#!/usr/bin/env bash
# Provisions the object storage bucket this app expects (bucket creation + versioning), so
# every environment — local MinIO, staging/prod Hetzner Object Storage — is set up the same way
# and this isn't left as tribal knowledge from a one-off manual command.
#
# Usage: STORAGE_SERVICE_URL=... STORAGE_ACCESS_KEY=... STORAGE_SECRET_KEY=... STORAGE_BUCKET=... ./setup-object-storage.sh
set -euo pipefail

: "${STORAGE_SERVICE_URL:?STORAGE_SERVICE_URL is required}"
: "${STORAGE_ACCESS_KEY:?STORAGE_ACCESS_KEY is required}"
: "${STORAGE_SECRET_KEY:?STORAGE_SECRET_KEY is required}"
: "${STORAGE_BUCKET:?STORAGE_BUCKET is required}"

mc alias set setup-target "$STORAGE_SERVICE_URL" "$STORAGE_ACCESS_KEY" "$STORAGE_SECRET_KEY" >/dev/null

if mc ls "setup-target/${STORAGE_BUCKET}" >/dev/null 2>&1; then
  echo "[setup] Bucket '${STORAGE_BUCKET}' already exists."
else
  echo "[setup] Creating bucket '${STORAGE_BUCKET}'..."
  mc mb "setup-target/${STORAGE_BUCKET}"
fi

echo "[setup] Enabling versioning (PRD §8.3: object storage versioning for uploaded documents/videos)..."
mc version enable "setup-target/${STORAGE_BUCKET}"
mc version info "setup-target/${STORAGE_BUCKET}"

echo "[setup] Done."
