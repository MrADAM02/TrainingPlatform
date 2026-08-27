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

The two are deployed independently (backend as a container, frontend as a static build) and have
no build-time dependency on each other.

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
