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

### Event configuration
Each `Event` carries three configuration flags that `SubmitRsvp` enforces at validation time:
- **`AllowMaybe`** (bool) — controls whether RSVPs can use `RsvpStatus.Maybe`. Defaults to `false` for new events, but the EF migration backfills existing rows to `true`. When toggled off later, existing Maybe RSVPs are kept (only new submits are blocked); `ManagePage`'s edit form notes this explicitly when the toggle moves from on→off.
- **`RsvpDeadline`** (`DateTime?` UTC, nullable) — soft close on RSVPs. Server rejects submits after this time with `ValidationProblem`; `InvitePage` also checks client-side and renders a "Tilmelding lukket" card instead of the form. Create/Update validate `rsvpDeadline ≤ startsAt`. The `<input type="datetime-local">` uses `max={startsAt}` for native browser validation as the first line of defense.
- **`ContactRequirement`** (`None | Email | Phone` enum) — when not `None`, `SubmitRsvp` requires the matching contact field and persists it on the `Rsvp`. Only the required field is stored; the other is discarded server-side even if the client sent it. Admin view shows the value as `mailto:`/`tel:` link.

Validation lives in the shared `ValidateEventOptions` helper in `EventEndpoints.cs` (called from both CreateEvent and UpdateByAdminToken). All three enums (`RsvpStatus`, `ContactRequirement`) wire-serialize as strings via the global `JsonStringEnumConverter` registered in `Program.cs`.

### Backend layout (`server/Inviter.Api`)
Vertical-slice structure: each endpoint is its own static class in `Features/<Area>/<UseCase>.cs` with a single `Handle` method. Cross-cutting code lives in `Domain/`, `Data/`, `Infrastructure/`, and `Shared/`.

- `Features/Events/` — `CreateEvent`, `GetEventPublic`, `GetEventAdmin`, `UpdateEvent` slices, plus the shared `EventValidation` helper used by Create+Update, and `EventEndpoints.MapEventEndpoints` that wires the four routes. Request/response records (`CreateEventRequest`, `EventAdminDto`, etc.) live in `EventDtos.cs` alongside the slices that use them.
- `Features/Rsvps/` — `SubmitRsvp` (with private contact-requirement helper) and `DeleteRsvp` slices, plus `RsvpEndpoints.MapRsvpEndpoints`. DTOs in `RsvpDtos.cs`. `RsvpDto` is referenced cross-slice from `Events.GetEventAdmin` (admin view inlines the RSVPs).
- `Features/Invitees/` — `GetInviteePrefill`, `ListInvitees`, `AddInvitees` (holds the `MaxInviteesPerBulkAdd = 200` constant), `DeleteInvitee`, `SendInvitations` slices, plus `InviteeEndpoints.MapInviteeEndpoints`. DTOs in `InviteeDtos.cs`.
- `Domain/` — POCOs (`Event`, `Rsvp`, `Invitee`) and enums (`RsvpStatus`, `ContactRequirement`). Anæmiske med vilje: ingen rig domænelogik i denne app.
- `Data/AppDbContext.cs` — Fluent config: max-lengths, unique indexes on tokens, `RsvpStatus`/`ContactRequirement` stored as `int`, cascade delete from `Event` to `Rsvps`/`Invitees`. Handlers tale direkte med `AppDbContext` — ingen repository-abstraktion (bevidst vertical-slice valg).
- `Infrastructure/Email/` — `IEmailQueue` + `ChannelEmailQueue` (singleton) + `EmailDispatcher` (hosted service med 3-trins retry-backoff) + `MailKitEmailSender`. Templates (`AdminLinkTemplate`, `RsvpConfirmationTemplate`, `RsvpNotificationTemplate`, `InvitationTemplate`) er statiske builders, kaldes direkte fra slice-handlers.
- `Infrastructure/Tokens/TokenGenerator.cs` — `RandomNumberGenerator` + base64url. Don't replace with GUIDs — invite tokens need to be short enough to share.
- `Shared/Validation.cs` — `LooksLikeEmail`, `NormalizeOptional`, `NormalizeOrganizerEmail`. Bruges på tværs af slices.
- `Program.cs` — Configures SQLite, JSON (camelCase + `JsonStringEnumConverter` so the wire format is `"Yes"`/`"No"`/`"Maybe"`, not numbers), CORS for `http://localhost:5173` (Development only), then calls `db.Database.Migrate()` at startup so migrations are applied automatically. Endpoint-wiring sker via tre `Map…Endpoints()`-kald, ét pr. feature.

#### Tests (`server/Inviter.Api.Tests`)
xUnit + `WebApplicationFactory<Program>` (`InviterApiFactory`) der: (1) overskriver `AppDbContext` til en held SQLite-`:memory:`-connection pr. fixture, (2) erstatter `IEmailQueue` med `FakeEmailQueue` der opsamler `QueuedEmail`-instanser uden at drive `EmailDispatcher`. Karakteriserings-tests pr. slice asserter status-koder, response-shape, validation-fejlbeskeder og email-side-effects. Kør med `dotnet test server/Inviter.Api.Tests/Inviter.Api.Tests.csproj`. Tilføj nye tests sammen med nye slices — én test-fil pr. feature (`EventsTests.cs`, `RsvpsTests.cs`, `InviteesTests.cs`).

### Frontend layout (`client/src`)
- `api/` — `client.ts` is a thin typed fetch wrapper; throws `ApiError` with status. `types.ts` mirrors the backend DTOs — keep them in sync by hand.
- `lib/myEvents.ts` — localStorage helpers for the "my events" list (the only client-side persistence).
- `lib/format.ts` — date helpers; `da-DK` locale, plus `<input type="datetime-local">` ↔ ISO round-trip.
- `lib/utils.ts` — shadcn-generated `cn()` (clsx + tailwind-merge). Import from `@/lib/utils`.
- `pages/` — One file per route. `ManagePage` also re-calls `rememberEvent()` on load so opening an admin URL on a new browser registers it locally.
- `components/ui/*.tsx` — shadcn primitives (button, input, textarea, label, card, badge, dialog, alert-dialog, sonner, dropdown-menu, skeleton). Edit them in place; that's the whole point of shadcn.
- `components/{Layout,StatusBadge,Field,ThemeProvider,ThemeToggle}.tsx` — app-level wrappers. UI strings are in Danish.

### Design system
- **shadcn/ui** is the component layer (initialized with the Nova preset, Radix base, Vite template). New components: `npx shadcn@latest add <name>`. The `@/` path alias resolves to `client/src/`.
- **Tokens live in `client/src/index.css`** as CSS custom properties on `:root` (light) and `.dark` (dark), mapped to Tailwind utilities via `@theme inline { --color-foo: var(--foo); }`. **Without `inline`**, the values are baked at build time and dark mode breaks. Cream/peach/burgundy palette in OKLCH plus custom status tokens (`--status-yes`, `--status-maybe`, `--status-no`).
- **Fonts** come from `@fontsource-variable/inter` and `@fontsource-variable/fraunces` (variable, `opsz` axis). Set heading display sizing with `style={{ fontVariationSettings: '"opsz" 144' }}`. The `@layer base` rule auto-applies `font-serif` to `h1–h4`.
- **Dark mode**: `@custom-variant dark (&:where(.dark, .dark *))` keeps `dark:` utilities at specificity 0. `next-themes` (`attribute="class"`, `enableSystem`) drives `.dark` on `<html>`; the anti-FOUC script in `index.html` reads `localStorage.theme` before the bundle parses to avoid the light flash. Don't remove that script.
- **Toaster** (`Sonner`) is mounted once in `Layout.tsx`. Call `toast.success(...)` from anywhere. Adding a second `<Toaster />` will double-fire under StrictMode.
- **Motion**: import via `motion/react` (the rebranded `framer-motion`). `<AnimatePresence>` only animates direct children with stable `key`s; for list exits use `<motion.li key={r.id}>` inside the `AnimatePresence`. Don't try route-level animations — React Router 7's `<Outlet />` doesn't unmount through AnimatePresence without custom wiring.
- **`AlertDialog`** replaces native `confirm()`: trigger via state (`pendingRemoval`), execute the action in `onClick` of `<AlertDialogAction>`. The pattern is in `ManagePage.tsx` and `MyEventsPage.tsx`.

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
- **Tailwind v4** is wired via the `@tailwindcss/vite` plugin in `vite.config.ts` and a single `@import "tailwindcss";` in `src/index.css`. There is no `tailwind.config.js` and no PostCSS config — don't add them. Custom tokens go in `@theme inline { … }` so they re-resolve through `:root`/`.dark` at runtime.
- **TypeScript 6 has `erasableSyntaxOnly` and bans `baseUrl`.** Constructor parameter properties (`constructor(public foo: T)`) fail to compile — declare fields explicitly. The `@/` path alias is configured via `paths` only (no `baseUrl`); adding `baseUrl` re-triggers the TS5101 deprecation error. See `client/src/api/client.ts` for the no-parameter-properties pattern.
- **`DateTime.Kind` matters** for SQLite. New events set `DateTimeKind.Utc` explicitly in `EventEndpoints.cs` — if you add date fields, do the same or comparisons will silently behave wrong.
- **Animations use `motion`, not `framer-motion`.** The package was rebranded; `import { motion, AnimatePresence } from 'motion/react'`. `framer-motion` is deprecated.
- **`tw-animate-css`, not `tailwindcss-animate`** — the v3 plugin doesn't work on v4. shadcn's CLI installs the right one; don't swap it.
- **Curl from PowerShell mangles non-ASCII bodies** (UTF-8 → cp1252). Use a `.http` file, REST client, or the UI to test endpoints with Danish characters.
