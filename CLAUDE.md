# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Architecture

Two-process app: an ASP.NET Core Minimal API (`server/Inviter.Api`, .NET 10, SQLite via EF Core) and a React+Vite+TS SPA (`client`, Tailwind v4). In development Vite proxies `/api/*` to the backend on `:5080` — there is no separate "dev gateway"; the proxy in `client/vite.config.ts` is the only thing tying them together.

### Token-based access (no auth yet)
Every `Event` has two opaque base64url tokens generated in `Tokens/TokenGenerator.cs`:
- **`InviteToken`** (~12 chars, shareable) — gates the public read-only view (`GET /api/invite/{t}`) and RSVP submission. Returns *no* guest list.
- **`AdminToken`** (~43 chars, secret) — gates the management endpoints (`GET/PUT /api/manage/{t}`, `DELETE …/rsvp/{id}`). Returns the full event + RSVP list.

Both tokens have unique indexes. **Unknown tokens always return 404** (intentional — don't change to 401/403; we don't want to leak which tokens exist). The frontend persists admin tokens to `localStorage` under `inviter.myEvents` (see `client/src/lib/myEvents.ts`) so users can return to events they created — this is the substitute for accounts until auth lands.

The schema is designed so that `Event.OwnerId : Guid?` can be added later without a destructive migration; admin tokens stay valid alongside auth.

### Backend layout (`server/Inviter.Api`)
- `Domain/` — POCOs (`Event`, `Rsvp`, `RsvpStatus`).
- `Data/AppDbContext.cs` — Fluent config: max-lengths, unique indexes on tokens, `RsvpStatus` stored as `int`, cascade delete from Event to Rsvps.
- `Endpoints/EventEndpoints.cs` — All routes registered as Minimal API handlers under `/api`. Validation is inline via `Results.ValidationProblem`; `DateTime` values are explicitly `SpecifyKind(..., Utc)` to avoid SQLite kind drift.
- `Tokens/TokenGenerator.cs` — `RandomNumberGenerator` + base64url. Don't replace with GUIDs — invite tokens need to be short enough to share.
- `Program.cs` — Configures SQLite, JSON (camelCase + `JsonStringEnumConverter` so the wire format is `"Yes"`/`"No"`/`"Maybe"`, not numbers), CORS for `http://localhost:5173` (Development only), then calls `db.Database.Migrate()` at startup so migrations are applied automatically.

### Frontend layout (`client/src`)
- `api/` — `client.ts` is a thin typed fetch wrapper; throws `ApiError` with status. `types.ts` mirrors the backend DTOs — keep them in sync by hand.
- `lib/myEvents.ts` — localStorage helpers for the "my events" list (the only client-side persistence).
- `lib/format.ts` — date helpers; `da-DK` locale, plus `<input type="datetime-local">` ↔ ISO round-trip.
- `pages/` — One file per route. `ManagePage` also re-calls `rememberEvent()` on load so opening an admin URL on a new browser registers it locally.
- `components/ui.tsx`, `StatusBadge.tsx`, `Layout.tsx` — Shared primitives. UI strings are in Danish.

## Commands

```powershell
# Backend (port 5080)
cd server/Inviter.Api ; dotnet run

# Frontend (port 5173, proxies /api → 5080)
cd client ; npm install ; npm run dev

# Production build (typecheck included via `tsc -b`)
cd client ; npm run build

# EF Core migrations
cd server/Inviter.Api
dotnet ef migrations add <Name>
dotnet ef database update     # usually unnecessary — Program.cs runs Migrate() at startup
```

The SQLite file `server/Inviter.Api/inviter.db` is gitignored and recreated on next run if deleted — convenient for resetting local state.

## Gotchas

- **.NET 10 solution is `.slnx` (XML), not `.sln`.** `dotnet sln Inviter.slnx add ...` works; the older `.sln` path will error.
- **Tailwind v4** is wired via the `@tailwindcss/vite` plugin in `vite.config.ts` and a single `@import "tailwindcss";` in `src/index.css`. There is no `tailwind.config.js` and no PostCSS config — don't add them.
- **TypeScript 6 has `erasableSyntaxOnly` on by default.** Constructor parameter properties (`constructor(public foo: T)`) fail to compile — declare fields explicitly. See `client/src/api/client.ts` for the pattern.
- **`DateTime.Kind` matters** for SQLite. New events set `DateTimeKind.Utc` explicitly in `EventEndpoints.cs` — if you add date fields, do the same or comparisons will silently behave wrong.
- **Curl from PowerShell mangles non-ASCII bodies** (UTF-8 → cp1252). Use a `.http` file, REST client, or the UI to test endpoints with Danish characters.
