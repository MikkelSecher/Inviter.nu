# Inviter — fra MVP til brugbart produkt

## Context

Inviter er i dag en fungerende prototype: man kan oprette et event, dele et invite-link, modtage RSVPs og administrere dem. Men det er ikke et produkt nogen kan drive deres events på endnu. Den centrale blocker er at admin-adgang udelukkende lever i browserens localStorage — rydder du browseren, mister du adgangen til dit eget event. Dertil mangler de funktioner folk forventer (sletning, aflysning, ændring af RSVP, email-bekræftelser, lokation, gæsteliste-export) og en drift-baseline (rate limiting, secrets via env, health checks, logging, Docker).

Den her plan tager en eksplicit holdning til hvad der **skal** med, hvad der dropbes som **støj**, og hvornår performance-arbejdet sker. Den er opdelt i tre faser, så hver fase kan shippes selvstændigt.

**Antagelser** (kan justeres):
- B2C, private events (fest, bryllup, møde) som primær use case
- Gratis selvbetjening, ingen monetization endnu
- Kun dansk
- Single region, hosted som almindelig web app
- Token-flow til **gæster** beholdes (frictionless RSVP), accounts er for **hosts**

---

## Hvad bygger vi IKKE (støj-listen)

| Feature | Hvorfor ikke |
|---|---|
| Recurring events | Komplekst domæne (RRULE, undtagelser, edit-this-vs-all). Hosts opretter bare et nyt event. |
| Sociale features (kommentar-tråde, likes, gæst-til-gæst) | Trækker produktet mod platform i stedet for værktøj. Modereringsbyrde. |
| Internationalisering | Hvert string-pull koster vedligehold. Engelsk kan komme i ét bigbang senere hvis det viser sig nødvendigt. |
| PWA / offline | RSVP er 30 sekunders engagement. Service worker = bug-overflade uden gevinst. |
| SMS-notifikationer | Pris pr. besked, samtykke-jura, ringe ROI vs email. |
| Waitlist | Implicerer auto-promotion, fairness, edge cases. Capacity + manuel håndtering er nok. |
| Public event discovery | Inviter er privat-by-default. Discovery = andet produkt. |
| Granulære roller (co-host, viewer) | YAGNI. Én owner. Skal nogen hjælpe — del admin-link. |
| Strukturerede dietary/allergy-felter | Free-text comment dækker 95%. Overengineering for fest-kontekst. |
| Embedded kort | Cookie-jura + ekstra bundle. Adresse-tekst + deep-link til Google Maps er nok. |
| Custom CSS/full theming | Lille palette-picker er fint, fuld branding er feature creep. |

---

## Fase 1 — Production-readiness

**Mål:** kan stå offentligt på et domæne uden risiko for data-loss, abuse eller pinlige outages. Admin-adgang er ikke længere en localStorage-lottokupon.

### 1.1 Account-system (host) — L
- ASP.NET Core Identity med cookie-auth (same-origin SPA, JWT er unødvendigt)
- Ny `User`-entitet, `/api/auth/register|login|logout|me`
- `Event.OwnerId : Guid?` migration
- Berørte filer: `server/Inviter.Api/Program.cs`, ny `Auth/`-mappe, `Data/AppDbContext.cs`, ny `Domain/User.cs`, ny migration

### 1.2 Claim-flow for eksisterende events — M
- Logget-ind bruger kan indtaste AdminToken og overtage ejerskab
- Nye events knyttes automatisk til `OwnerId` hvis logget ind
- AdminToken bliver fallback for ikke-claimede / anonyme events
- Berørte filer: nyt `POST /api/events/claim` i `Endpoints/EventEndpoints.cs`, `client/src/pages/MyEventsPage.tsx`

### 1.3 Auth i frontend — M
- Ny `/login`, `/signup`, AuthContext, beskyttede routes
- `MyEventsPage` henter fra server hvis logget ind, ellers localStorage som i dag
- Berørte filer: `client/src/main.tsx`, ny `client/src/contexts/AuthContext.tsx`, nye `pages/LoginPage.tsx`, `pages/SignupPage.tsx`

### 1.4 Rate limiting — S
- Built-in `AddRateLimiter`: fixed window
- POST `/api/invite/{t}/rsvp`: 10/min/IP
- POST `/api/events`: 5/time/IP
- Berørte filer: `Program.cs`

### 1.5 Anti-bot på RSVP — S
- Honeypot-felt (hidden `<input name="website">`) + min-submit-time (1.5s)
- Ikke CAPTCHA — for meget friktion på B2C
- Berørte filer: `Endpoints/EventEndpoints.cs` (SubmitRsvp), `client/src/pages/InvitePage.tsx`

### 1.6 Structured logging + correlation ID — S
- Serilog → console (JSON i prod, pretty i dev) + rolling file
- Request-ID middleware, logget på hver response
- Berørte filer: `Program.cs`

### 1.7 Health checks — S
- `/health/live` (proces) og `/health/ready` (DB ping)
- Berørte filer: `Program.cs`

### 1.8 CORS + secrets ud af appsettings — S
- CORS-origins fra `appsettings.{env}.json` + env-var override
- Connection string + cookie-key via `dotnet user-secrets` i dev, env-vars i prod
- Berørte filer: `Program.cs`, ny `appsettings.Production.json`

### 1.9 HTTPS, HSTS, secure cookies — S
- Verificér default-pipeline; secure + samesite=lax på auth-cookie
- Berørte filer: `Program.cs`

### 1.10 Docker + Compose — M
- Multi-stage `Dockerfile` for API, statisk Nginx for SPA
- `docker-compose.yml` med volume til SQLite + Litestream-sidecar (replikerer til S3/B2 — billig backup indtil Postgres er nødvendigt)
- Berørte filer: ny `Dockerfile`, ny `client/Dockerfile`, ny `docker-compose.yml`

### 1.11 React error boundary + bedre API-fejl — S
- Top-level error boundary + per-route fallback
- `client.ts` får timeout (10s) og 1 retry på network errors (aldrig på 4xx)
- Berørte filer: `client/src/main.tsx`, ny `client/src/components/ErrorBoundary.tsx`, `client/src/api/client.ts`

### Performance i Fase 1
- `AsNoTracking()` på alle GET-endpoints (`Endpoints/EventEndpoints.cs`)
- Composite index på `Rsvps(EventId, Status)` (brugt i admin-listen)
- Output caching på `GET /api/invite/{t}`, vary-by-token, 30s TTL
- `React.lazy` på `CreateEventPage`, `ManagePage`, `MyEventsPage`. **`InvitePage` forbliver eager** — det er den primære landing
- Dynamic import af `motion` så det kun loades på sider der animerer

---

## Fase 2 — Core product features

**Mål:** brugere kan reelt køre et event end-to-end uden at savne basics. Email er produktets rygrad herfra.

### 2.1 Email-service med outbox — L
- `IEmailSender`-abstraktion + SendGrid eller Postmark implementation
- `EmailOutbox`-tabel + `IHostedService`-worker, så POST /rsvp aldrig venter på SMTP
- Templates: RSVP-bekræftelse til gæst, ny-RSVP notifikation til host, deadline-reminder (cron), event-aflysning
- Berørte filer: ny `Email/`-mappe, ny `Domain/EmailOutbox.cs`, ny migration, `Endpoints/EventEndpoints.cs`

### 2.2 Event soft-delete + aflysning — M
- `CancelledAt : DateTime?` og `DeletedAt : DateTime?` på Event
- Cancel: invite-side viser "Aflyst", auto-email til alle med email
- Delete: soft (30 dages grace) → cleanup-job sletter hard
- Berørte filer: `Domain/Event.cs`, migration, `Endpoints/EventEndpoints.cs`, `client/src/pages/ManagePage.tsx`, `client/src/pages/InvitePage.tsx`

### 2.3 Gæst kan ændre/slette egen RSVP — M
- Returnér `rsvpToken` ved submit, gemmes i localStorage på gæstens device
- `PUT /api/invite/{t}/rsvp/{rsvpToken}` og `DELETE` samme path
- "Du har allerede svaret" -view med edit/withdraw knapper
- Berørte filer: `Domain/Rsvp.cs` (+`RsvpToken`), migration, `Endpoints/EventEndpoints.cs`, `client/src/pages/InvitePage.tsx`

### 2.4 Plus-ones — S
- `PartySize : int` på Rsvp (1–N), host slår feature til/fra per event via ny `AllowPlusOnes` flag
- Admin-headcount viser samlet party-size, ikke kun antal RSVPs
- Berørte filer: `Domain/Event.cs`, `Domain/Rsvp.cs`, migration, RSVP-form, ManagePage

### 2.5 Lokation — S
- `Location : string?` (max 500) på Event
- Invite-side viser tekst + deep-link til `https://www.google.com/maps/search/?api=1&query=…`
- Berørte filer: `Domain/Event.cs`, migration, Create-form, InvitePage

### 2.6 CSV-export — S
- `GET /api/manage/{t}/export.csv` returnerer gæsteliste (navn, status, party size, comment, kontakt, submittedAt)
- Hosts vil ALTID have det til catering/seating
- Berørte filer: `Endpoints/EventEndpoints.cs`, "Eksportér"-knap i ManagePage

### 2.7 QR-kode for invite-link — S
- Client-side `qrcode`-lib, render som SVG på ManagePage
- Ingen server-belastning
- Berørte filer: `client/src/pages/ManagePage.tsx`, ny dep

### 2.8 Pagination på RSVP-listen — M
- Cursor-based, default 50 pr. side
- Først nødvendigt ved >100 RSVPs men cheap at indføre nu
- Berørte filer: `Endpoints/EventEndpoints.cs` (GetByAdminToken), `client/src/pages/ManagePage.tsx`, `client/src/api/types.ts`

### 2.9 react-hook-form + zod — M
- Erstatter ad-hoc useState-validation i Create + RSVP
- Shared zod-schemas mellem de to flows
- Real-time field-level validation, færre re-renders
- Berørte filer: `CreateEventPage.tsx`, `InvitePage.tsx`, `ManagePage.tsx` (EditForm), ny `client/src/lib/schemas.ts`, nye deps

### 2.10 Postgres-readiness (conditional) — M
- EF-koden er allerede provider-neutral; ingen SQLite-specifikke functions
- Skift fra `UseSqlite` til `UseNpgsql` bag en config-flag
- **Trigger:** flyt først når aktive events >200 eller hosting kræver det. Litestream + SQLite holder længe.
- Berørte filer: `Program.cs`, ny migration-suite for Postgres-baseline

### Performance i Fase 2
- Pagination er hovedløsningen for skala (ovenfor)
- Email-sending er async via outbox — POST /rsvp må aldrig vente på SMTP
- Composite index på `Rsvps(EventId, SubmittedAt DESC)` til paginering

---

## Fase 3 — Polish & differentiering

**Mål:** produktet føles færdigt, ikke bare funktionelt.

### 3.1 Cover image — L
- Upload til lokal disk først, object storage senere
- Resize til 1200/600/300 server-side via `SixLabors.ImageSharp`
- Berørte filer: ny `POST /api/events/{id}/cover`, `Event.CoverImageUrl`, Create + Invite UI

### 3.2 Capacity — M
- `MaxAttendees : int?` på Event
- Soft cap (vis "fyldt", tillad Maybe) eller hard cap (afvis Yes) — host vælger via flag
- **Ingen waitlist** (se støj-liste)
- Berørte filer: `Domain/Event.cs`, migration, RSVP-validering, InvitePage, ManagePage

### 3.3 A11y-audit — M
- Axe-run, fokus-håndtering i Dialog/AlertDialog, `aria-live` på toasts og form-fejl, kontrast-tjek på StatusBadge, fuld keyboard-nav på DateTimePicker
- Berørte filer: hele UI, særligt `components/ui/*`, `DateTimePicker.tsx`, `StatusBadge.tsx`

### 3.4 Email-templates som MJML — M
- Pænere mails, korrekt mobile-rendering
- Berørte filer: ny `Email/Templates/`

### 3.5 Bundle-optimering — S
- Drop én af de to variable fonts (eller subset Fraunces til kun headings)
- Audit motion-imports
- Target: <150kb gz initial på InvitePage (den vigtigste landing)
- Berørte filer: `client/vite.config.ts`, `client/index.html`, `client/src/main.tsx`

### 3.6 Host-dashboard på `/mine` — M
- Når account findes: liste fra server, ikke localStorage
- Inline counts (going/maybe/no/total party size) pr. event
- Berørte filer: `MyEventsPage.tsx`, nyt `GET /api/me/events`

### 3.7 ICS calendar-download — S
- Quick-win senere: invite-side får "Tilføj til kalender"-knap der genererer .ics
- Berørte filer: nyt `GET /api/invite/{t}/calendar.ics`, `InvitePage.tsx`

### 3.8 Theming per event (palette-picker) — M
- 4-5 forudvalgte paletter, gemt som `Event.ThemeKey`
- Invite-siden læser theme og overrider CSS-variabler
- Berørte filer: `Domain/Event.cs`, migration, Create-form, `client/src/index.css`, InvitePage

---

## Verification per fase

### Fase 1 — production-readiness
- `dotnet test` (når tests tilføjes) og `npm run build` skal være grønne
- Manuel: opret konto, opret event mens logget ind, log ud, log ind igen → eventet er der uden localStorage
- Manuel: claim et anonymt event via AdminToken → optræder under "Mine events"
- `curl` 11 RSVPs på samme minut → den 11. får 429
- Stop API, `curl /health/ready` → fejl; start, → 200
- Kør i Docker Compose lokalt — alt fungerer
- Lighthouse på `/invite/:token` — initial bundle < 200kb gz

### Fase 2 — core features
- Submit RSVP → modtag bekræftelses-mail; host modtager notifikation
- Aflys event som host → alle gæster med email modtager besked; invite-side viser "Aflyst"
- Submit RSVP, åbn invite igen → "Du har allerede svaret" + edit/withdraw virker
- Tilføj plus-ones, eksportér CSV → korrekt party-size sum
- Importér 500 RSVPs via script, åbn ManagePage → pagineret, ingen freeze

### Fase 3 — polish
- Axe-run scorer 0 critical violations på alle sider
- Upload cover image, se på invite-side i mobil + desktop
- Tilføj til kalender på iOS + Android — event vises korrekt
- Bundle-rapport: <150kb gz initial på InvitePage

---

## Kritiske filer på tværs af faser

- `server/Inviter.Api/Program.cs` — auth, CORS, rate limiting, logging, caching, health
- `server/Inviter.Api/Endpoints/EventEndpoints.cs` — alle endpoint-udvidelser
- `server/Inviter.Api/Domain/Event.cs`, `Rsvp.cs` — schema-evolution
- `server/Inviter.Api/Data/AppDbContext.cs` — indexes, fluent config
- `server/Inviter.Api/Tokens/TokenGenerator.cs` — ny rsvpToken-variant
- `client/src/main.tsx` — lazy routes, AuthContext, ErrorBoundary
- `client/src/api/client.ts` — retry, timeout
- `client/src/pages/*.tsx` — alle flows berøres
- `client/src/lib/schemas.ts` (ny) — shared zod-schemas
