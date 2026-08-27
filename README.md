# Training Platform

Web-based training & document management platform for Jabartah (~500 users): course/document
management, role-based access, enrollment & progress tracking, quizzes, certificates, reports,
and a bilingual (Arabic RTL / English) UI. See `Training_Platform_PRD.pdf` for full requirements
and `CLAUDE.md` for detailed architecture notes.

## Features

- **Auth & RBAC** — JWT login with rotating refresh tokens; Administrator / Trainer / Trainee
  roles, enforced server-side on every endpoint.
- **Courses & modules** — Trainer/Admin authoring, publish/unpublish, an open catalog (any
  published course is browsable; enrollment gates downloading).
- **Lesson types** — video (inline player), PDF (quick preview + download), image (zoomable
  viewer), and text (trainer-authored reading lessons) — each with a dedicated lesson page,
  progress tracking, and a "next lesson" flow.
- **Document versioning** — replacing a file archives the previous version; full history with
  per-version download.
- **Enrollment & progress** — bulk enroll/unenroll, auto-completion once every document is
  consumed and every required quiz is passed.
- **Quizzes** — single-choice quizzes, unlimited retakes, optional "required for completion."
- **Certificates** — auto-issued on course completion, printable certificate page.
- **Bookmarking & learning streak** — "My Library" saved courses, a streak widget computed
  honestly from real activity (no fabricated watch-time).
- **Search** — keyword + course + content-type filters, scoped to what the caller can access.
- **Reports** — org summary, course completion rates, per-trainee progress drill-down, CSV export.
- **Admin panel** — user management (create/edit/deactivate/delete/reset password/bulk CSV
  import), activity log with CSV export.
- **i18n** — Arabic (RTL, default) and English, switchable per user.

## Structure

```
backend/    ASP.NET Core API (.NET 10) — Clean Architecture: Domain / Application / Infrastructure / Api
frontend/   Nuxt 4 (SSR) — Pinia, @nuxtjs/i18n (Arabic RTL default / English), @nuxt/ui
```

The two are deployed independently (backend as a Docker container, frontend as an SSR app on
Netlify) and have no build-time dependency on each other.

## Prerequisites

- .NET 10 SDK
- Node 24+, pnpm
- Docker (for local Postgres + MinIO)

## Running locally

**1. Start Postgres and MinIO (local object storage):**

```
cd backend
docker compose up -d
```

MinIO's console is at `http://localhost:9001` (login: `minio_dev_only` / `minio_dev_only_secret`).
Create a bucket named `training-platform` there — uploads/downloads won't work without it. (The
same setup can be scripted with `backend/scripts/setup-object-storage.sh`, which also enables
versioning; useful for staging/prod parity.)

**2. Run the backend API:**

```
cd backend/src/TrainingPlatform.Api
dotnet run
```

Runs on `http://localhost:5080`. On first run in Development, migrations apply automatically and
a default Administrator account is seeded from `appsettings.Development.json`
(`Seed:AdminEmail` / `Seed:AdminPassword` — dev-only placeholder credentials, not for production).

There is no public self-registration — accounts are created by an Administrator via
`POST /api/v1/users` (or the `/admin/users` page once the frontend is running).

**3. Run the frontend:**

```
cd frontend
pnpm install
pnpm dev
```

Runs on `http://localhost:3010`. Points at the backend via `NUXT_PUBLIC_API_BASE`
(defaults to `http://localhost:5080/api/v1`).

## Backend architecture notes

- CQRS via hand-rolled `ICommand`/`IQuery`/`ISender` (see `Application/Abstractions/Messaging`) —
  not MediatR, which requires a paid license for production use as of v13+.
- JWT auth with rotating, hashed refresh tokens; reuse of an already-rotated token revokes the
  whole session (theft mitigation).
- All authorization is enforced server-side via policy-based auth on every endpoint; frontend
  route guards are UX-only.
- File storage targets the plain S3 API (`Infrastructure/Storage/S3FileStorageService.cs`),
  pointed at MinIO locally — switching to real Hetzner Object Storage is a config change only.

## Frontend architecture notes

- Auth state lives in the Pinia `stores/auth.ts`; `composables/useApi.ts` wraps `$fetch` to
  attach the bearer token and transparently retries once on a 401 via token refresh.
- Route protection (`middleware/auth.global.ts`) is UX-only — the backend is the real
  authorization boundary.
- Arabic (`ar`, RTL) is the default locale; translation files are
  `frontend/i18n/locales/{ar,en}.json` — add keys to both when adding user-facing strings.
- UI components come from `@nuxt/ui` (Tailwind v4-based); shared building blocks like
  `CourseCard`, `StatCard`, and `CourseCoverPlaceholder` live in `frontend/app/components/`.

## Scripts

`backend/scripts/` — `setup-object-storage.sh` (bucket + versioning provisioning),
`backup-database.sh` / `restore-database.sh` (pg_dump-based backup/restore against object
storage), `load-test.js` (k6 load test against core read endpoints).

## CI

GitHub Actions (`.github/workflows/`) builds and lints both projects on push/PR to `main`, scoped
by path so backend and frontend changes don't trigger each other's pipeline.
`deploy-backend.yml` additionally builds/publishes/deploys the API on every push to `main` that
touches `backend/**` — see below.

## Deploying to a server

Every push to `main` that touches `backend/**` builds the API image, publishes it to GitHub
Container Registry (`ghcr.io/mradam02/trainingplatform-api`), then SSHes into the server and
redeploys — see `.github/workflows/deploy-backend.yml`. The server runs in `Production` mode: no
Swagger — migrations and admin seeding still apply automatically on every boot (same
`DbSeeder`/`MigrateAsync` call as local dev, just driven by real env vars instead of
`appsettings.Development.json`).

The frontend is **not** part of this pipeline — it's deployed separately on Netlify.

**One-time server setup** (assumes the VM, Docker, and nginx already exist):

```bash
mkdir -p /opt/trainingplatform && cd /opt/trainingplatform
# copy docker-compose.prod.yml and .env.production.example here (scp, or a shallow git clone)
cp .env.production.example .env
# edit .env: POSTGRES_PASSWORD, JWT_KEY (openssl rand -base64 48), CORS_ORIGIN (the Netlify
# URL), SEED_ADMIN_EMAIL/SEED_ADMIN_PASSWORD, and the real Hetzner Object Storage credentials
```

Create the Hetzner Object Storage bucket once, from anywhere with the credentials (not necessarily
the server):

```bash
STORAGE_SERVICE_URL=https://<region>.your-objectstorage.com \
STORAGE_ACCESS_KEY=... STORAGE_SECRET_KEY=... STORAGE_BUCKET=training-platform \
  backend/scripts/setup-object-storage.sh
```

First deploy — either push to `main`, or smoke-test manually on the server before trusting CI:

```bash
docker compose -f docker-compose.prod.yml up -d
curl http://127.0.0.1:5000/health   # should return 200
```

**One-time GitHub setup** — add these as repo secrets (Settings → Secrets and variables →
Actions): `DEPLOY_HOST`, `DEPLOY_USER`, `DEPLOY_SSH_KEY`. After the first successful `publish`
run, flip the `trainingplatform-api` GHCR package to Public (repo → Packages → package settings)
so the server can pull it without authenticating.

**Reverse proxy / TLS** — nginx already runs on the server; `backend/deploy/nginx/lms-api.jab-eri.org.conf`
is the site config for `lms-api.jab-eri.org` (points at the API container's `127.0.0.1:5000`
binding). Copy it into `/etc/nginx/sites-available/`, symlink into `sites-enabled/`, then:

```bash
sudo certbot --nginx -d lms-api.jab-eri.org
sudo nginx -t && sudo systemctl reload nginx
```

Once the Netlify app has a real URL, update `CORS_ORIGIN` in the server's `.env` (redeploy to pick
it up) and point Netlify's `NUXT_PUBLIC_API_BASE` at `https://lms-api.jab-eri.org/api/v1`.
