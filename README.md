# Iris

A real-time visual map of all public transport in Sweden. Every bus, tram, metro, train and ferry appears as a moving dot on a live map. Hover or tap for vehicle details.

---

## What It Does

- Streams live vehicle positions from Trafiklab's GTFS-RT feed (50+ Swedish operators)
- Renders thousands of GPS-accurate, color-coded, smoothly animated dots on a dark map
- Shows vehicle info on hover: line, operator, destination, delay
- Filters by vehicle type (bus, train, metro, tram, ferry)
- Fully responsive — works on desktop and mobile

---

## Tech Stack

| Layer | Technology |
|---|---|
| Backend | C# / ASP.NET Core 10 |
| Realtime transport | SignalR |
| GTFS-RT parsing | Google.Protobuf |
| Frontend | TypeScript / React 18 / Vite |
| Map renderer | MapLibre GL JS + deck.gl |
| Map tiles | CartoDB Dark Matter (free) |
| State | Zustand |
| Styling | Tailwind CSS |
| Backend tests | xUnit |
| Frontend tests | Vitest + React Testing Library |
| Smoke tests | Playwright |

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/)
- A Trafiklab API key — register free at [trafiklab.se](https://www.trafiklab.se)

### Secrets

The Trafiklab API key is stored using [.NET User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) — outside the repo, never committed.

Create `backend/Iris.Api/appsettings.Local.json` (gitignored):

```json
{
  "TRAFIKLAB_REALTIME_API_KEY": "your_trafiklab_realtime_key",
  "GTFS_SWEDEN_3_REALTIME_API_KEY": "your_gtfs_sweden_3_key"
}
```

### Visual Studio (recommended — single F5 launch)

1. Set your API key (see Secrets above, one-time setup)

2. Install npm dependencies once:
   ```bash
   cd frontend && npm install
   ```

3. Open `Iris.slnx` in Visual Studio 2022

4. Press **F5** — the backend starts and automatically launches the Vite dev server in the background. The browser opens to `http://localhost:5173` once both are ready.

> Requires Visual Studio 2022 17.5+ with the **ASP.NET and web development** workload.

### Command Line

```bash
# Install npm deps (first time only)
cd frontend && npm install

# Start backend (also auto-starts frontend via SpaProxy)
cd backend/Iris.Api && dotnet run
```

Or separately:
```bash
# Terminal 1
cd backend/Iris.Api && dotnet run

# Terminal 2
cd frontend && npm run dev
```

Open http://localhost:5173

### Run Tests

```bash
# Backend unit + integration tests
dotnet test

# Frontend unit tests
cd frontend && npm test

# Smoke tests (requires both servers running)
cd frontend && npm run test:e2e
```

---

## Project Structure

```
Iris.slnx              — Visual Studio solution (open this)

/backend
  /Iris.Api            — Web API, SignalR hub, background polling services
  /Iris.Core           — Domain models, GTFS-RT parsing, interfaces
  /Iris.Tests          — xUnit tests

/frontend
  frontend.esproj      — VS JavaScript project (optional, needs Node.js workload)
  /src
    /components        — Map, VehicleLayer, InfoPanel, FilterBar
    /hooks             — useSignalR, map state
    /stores            — Zustand state
    /types             — TypeScript interfaces
```

---

## Data Sources

**Primary:** [GTFS Sweden 3](https://www.trafiklab.se/api/gtfs-datasets/gtfs-sweden/) via Trafiklab
- VehiclePositions feed — updated every 2 seconds
- TripUpdates feed — delays and ETAs, every 15 seconds
- 50+ operators covering most Swedish public transport

**Known limitation:** SJ (national rail) does not publish live GPS positions through Trafiklab. SJ trains show schedule/delay data only, no moving dot.

---

## Vehicle Colors

| Type | Color |
|---|---|
| Bus | Blue `#4A9EFF` |
| Train | Orange `#FF8C42` |
| Metro | Purple `#B06AFF` |
| Tram | Green `#3EFFB0` |
| Ferry | Cyan `#00D4FF` |

---

## Deployment

Currently local only. Azure deployment planned:
- Backend → Azure App Service
- Frontend → Azure Static Web Apps
- SignalR scaling → Azure SignalR Service
- Secrets → Azure Key Vault

---

## License

MIT
