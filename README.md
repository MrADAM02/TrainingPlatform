# Training Platform

Web-based training & document management platform for Jabartah (~500 users). See `Training_Platform_PRD.pdf` for full requirements.

## Structure

```
backend/    ASP.NET Core API (.NET 10) — Clean Architecture: Domain / Application / Infrastructure / Api
frontend/   Nuxt 4 (SSR) — Pinia, @nuxtjs/i18n (Arabic RTL default / English)
```

The two are deployed independently (backend as a container, frontend as a static build) and have no build-time dependency on each other.

## Prerequisites

- .NET 10 SDK
- Node 24+, pnpm
- Docker (for local Postgres)

## Running locally

**1. Start Postgres:**

```
cd backend
docker compose up -d
```

**2. Run the backend API:**

```
cd backend/src/TrainingPlatform.Api
dotnet run
```

Runs on `http://localhost:5080`. On first run in Development, migrations apply automatically and a default Administrator account is seeded from `appsettings.Development.json` (`Seed:AdminEmail` / `Seed:AdminPassword` — dev-only placeholder credentials, not for production).

There is no public self-registration — accounts are created by an Administrator via `POST /api/v1/users`.

**3. Run the frontend:**

```
cd frontend
pnpm install
pnpm dev
```

Runs on `http://localhost:3010`.

## Backend architecture notes

- CQRS via hand-rolled `ICommand`/`IQuery`/`ISender` (see `Application/Abstractions/Messaging`) — not MediatR, which requires a paid license for production use as of v13+.
- JWT auth with rotating, hashed refresh tokens; reuse of an already-rotated token revokes the whole session (theft mitigation).
- All authorization is enforced server-side via policy-based auth on every endpoint; frontend route guards are UX-only.

## CI

GitHub Actions (`.github/workflows/`) builds and lints both projects on push/PR to `main`, scoped by path so backend and frontend changes don't trigger each other's pipeline.
