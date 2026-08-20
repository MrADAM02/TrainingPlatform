# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Training Platform: web-based training & document management platform for Jabartah (~500 users). See `Training_Platform_PRD.pdf` for full requirements.

- `backend/` — ASP.NET Core API (.NET 10), Clean Architecture (Domain / Application / Infrastructure / Api)
- `frontend/` — Nuxt 4 (SSR), Pinia, `@nuxtjs/i18n` (Arabic RTL default / English)

The two are deployed independently (backend as a container, frontend as a static build) and have no build-time dependency on each other.

## Commands

### Backend (run from `backend/`)

```
docker compose up -d                        # start local Postgres
dotnet run --project src/TrainingPlatform.Api  # run API on http://localhost:5080
dotnet build
dotnet test
dotnet test --filter FullyQualifiedName~ClassName   # run a single test class/method
```

In Development, EF Core migrations apply automatically on startup and a default Administrator account is seeded from `appsettings.Development.json` (`Seed:AdminEmail` / `Seed:AdminPassword`). There is no public self-registration — accounts are created by an Administrator via `POST /api/v1/users`.

Adding a migration (from `backend/`):
```
dotnet ef migrations add <Name> --project src/TrainingPlatform.Infrastructure --startup-project src/TrainingPlatform.Api
```

### Frontend (run from `frontend/`)

```
pnpm install
pnpm dev         # http://localhost:3010
pnpm build
pnpm lint
pnpm typecheck
```

### CI

GitHub Actions (`.github/workflows/backend-ci.yml`, `frontend-ci.yml`) build/test each project on push/PR to `main`, scoped by path so backend and frontend changes don't trigger each other's pipeline. Backend CI: restore → build (Release) → test. Frontend CI: install (frozen lockfile) → lint → typecheck → build.

## Backend architecture

**Clean Architecture, feature-sliced.** Each Application-layer feature area (`Users`, `Content`, `Enrollments`, `Activity`, `Dashboard`, `Search`, `Auth`) is a folder containing one subfolder per use case (e.g. `Content/Courses/CreateCourse/CreateCourseCommand.cs`), each holding the command/query record, its `FluentValidation` validator, and its handler in a single file. `Content/Contracts/` and `Enrollments/Contracts/` hold read-model DTOs shared across queries in that area.

**CQRS via a hand-rolled mediator, not MediatR** (see `Application/Abstractions/Messaging` and `Application/Common/Messaging/Sender.cs`) — MediatR requires a paid license for production use as of v13+, so don't reintroduce it even though the package still appears in `Directory.Packages.props`. Handlers implement `ICommandHandler<TCommand>` / `ICommandHandler<TCommand, TResponse>` / `IQueryHandler<TQuery, TResponse>` and are auto-registered by assembly scan (Scrutor) in `Application/DependencyInjection.cs` — a new handler needs no manual DI wiring, just the right interface. Endpoints depend only on `ISender`.

**Result pattern, no exceptions for domain failures.** Handlers return `Result` / `Result<T>` (`Domain/Common/Result.cs`, `Error.cs`, `ValidationError.cs`). Endpoints translate failures via `Endpoints/CustomResults.cs`, which maps `ErrorType` (Validation/NotFound/Conflict/Unauthorized/Forbidden) to the matching HTTP status and formats validation errors as an RFC 7807 `ValidationProblem`.

**Minimal APIs, one `*Endpoints.cs` per feature** under `Api/Endpoints/<Feature>/`, each a `MapXEndpoints(this IEndpointRouteBuilder app)` extension registered in `Program.cs`. Endpoints are thin: build the command/query, `sender.Send`, translate the `Result`. Authorization is applied per-route-group via policy (`RequireAuthorization("RequireAdministrator")`, `"RequireTrainerOrAdministrator"`), defined in `Infrastructure/DependencyInjection.cs`. **All authorization is enforced server-side on every endpoint** — frontend route guards are UX-only, never trust the client.

**Auth:** JWT bearer tokens with rotating, hashed refresh tokens (`Infrastructure/Identity/TokenService.cs`, `RefreshTokenEntity.cs`); reuse of an already-rotated refresh token revokes the whole session (theft mitigation). `JwtBearerOptions.MapInboundClaims = false` is required — otherwise ASP.NET Core silently remaps the `role`/`sub` claim names the token service issues, breaking role checks. Auth endpoints (`login`, `token-refresh`) are behind dedicated rate limiters (`Program.cs`).

**Persistence:** EF Core with Npgsql + snake_case naming convention (`EFCore.NamingConventions`). `ApplicationDbContext` lives in `Infrastructure/Database/`; migrations are in `Infrastructure/Database/Migrations/`. ASP.NET Core Identity (`ApplicationUser`, `IdentityRole<Guid>`) backs user accounts; roles are the fixed set in `Domain/Users/Roles.cs` (`Administrator`, `Trainer`, ...) — validate against `Roles.All` rather than hardcoding role strings.

**File storage:** S3-compatible (`Infrastructure/Storage/S3FileStorageService.cs`), configured for MinIO locally — `UseHttp` is derived from the configured `ServiceUrl` scheme since `AmazonS3Config` otherwise always signs URLs as https.

**Cross-cutting:** `IActivityLogService` (`Infrastructure/Activity/`) records an audit trail for mutating actions (see `CreateUserCommandHandler` for the pattern: perform the mutation, then log on success). `IUserContext` / `CurrentUserContext` expose the authenticated caller's identity to handlers without touching `HttpContext` directly.

## Frontend architecture

Nuxt 4 SSR app under `frontend/app/` (pages, components, layouts, middleware, composables, stores, types) with `pnpm` workspaces (`pnpm-workspace.yaml`).

- **Auth state** lives in the Pinia `stores/auth.ts`; `composables/useApi.ts` wraps `$fetch` to attach the bearer token and transparently retry once on a 401 via `authStore.refreshAccessToken()`.
- **Route protection is UX-only**: `middleware/auth.global.ts` redirects unauthenticated/under-privileged users before render, but the backend is the actual authorization boundary — never rely on this middleware alone when adding a feature.
- **i18n:** Arabic (`ar`, RTL) is the default locale, English (`en`) is secondary; `strategy: 'no_prefix'`, locale persisted via the `training-platform-locale` cookie. Translation files are `frontend/i18n/locales/{ar,en}.json` — add keys to both when adding user-facing strings.
- **UI:** `@nuxt/ui` (Tailwind v4-based) component library.
