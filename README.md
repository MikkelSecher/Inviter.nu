# Inviter

Web-app til at oprette events, modtage RSVP-tilbagemeldinger, og se gæstelister.

## Stack
- **Backend**: ASP.NET Core (Minimal APIs) + EF Core + SQLite — `server/Inviter.Api`
- **Frontend**: React + Vite + TypeScript + Tailwind CSS — `client`

## Forudsætninger
- .NET SDK 8+ (testet på 10)
- Node 20+ og npm

## Kør lokalt

### Backend
```powershell
cd server/Inviter.Api
dotnet run
```
API'et starter på `http://localhost:5080`. SQLite-filen `inviter.db` oprettes ved første kørsel.

### Frontend
I et nyt terminalvindue:
```powershell
cd client
npm install   # første gang
npm run dev
```
Vite åbner på `http://localhost:5173` og proxier `/api`-kald til backend.

## API-overblik
| Method | Path | Beskrivelse |
|---|---|---|
| `POST` | `/api/events` | Opret event. Returnerer invite- og admin-token. |
| `GET`  | `/api/invite/{token}` | Offentligt event-view. |
| `POST` | `/api/invite/{token}/rsvp` | Indsend tilbagemelding. |
| `GET`  | `/api/manage/{adminToken}` | Fuldt event + gæsteliste (arrangør). |
| `PUT`  | `/api/manage/{adminToken}` | Opdatér event. |
| `DELETE` | `/api/manage/{adminToken}/rsvp/{rsvpId}` | Fjern en gæst. |

## Flow uden authentication
Når et event oprettes, returneres både en delbar `inviteToken` og en hemmelig `adminToken`. Browseren gemmer `adminToken` i `localStorage` (`inviter.myEvents`), så arrangøren kan se sine events under `/mine`. Auth tilføjes senere — modellen har plads til `Event.OwnerId` så eksisterende events kan "claimes" af en bruger.

## EF Core migrationer
```powershell
cd server/Inviter.Api
dotnet ef migrations add <Navn>
dotnet ef database update
```
DB migreres også automatisk ved opstart (`db.Database.Migrate()` i `Program.cs`).
