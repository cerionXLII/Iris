# Iris — Sweden Public Transport Live Map

A real-time visual map of all public transport in Sweden. Every bus, tram, metro, train and ferry is a moving dot on the map. Click or hover for vehicle info.

Inspiration: https://sl-map.gunnar.se/ — build something better.

---

## Core Requirements

- Moving dots on a map, one per vehicle, updated in real time
- Mouseover/tap shows: vehicle type, line, operator, destination, delay
- Works on both web and mobile (responsive)
- Modern, stunning design — something you want to just watch
- No SQL database (in-memory state, file-based static data where needed)
- Local dev only for now; Azure deployment later
- Tests after every feature

---

## Tech Stack

### Backend — C# / ASP.NET Core 10+
> Always use the latest stable .NET release. Upgrade when new versions ship.
- **ASP.NET Core Minimal API** — lightweight, fast HTTP + WebSocket host
- **SignalR** — real-time push of vehicle positions to frontend clients
- **Hosted Services** — background workers polling GTFS-RT feeds
- **Google.Protobuf** — parse GTFS-RT binary feeds from Trafiklab
- **In-memory state** — `ConcurrentDictionary` holding latest vehicle positions; no DB
- **HttpClient + IHttpClientFactory** — managed HTTP connections to Trafiklab

### Frontend — TypeScript / React
- **React 18 + Vite** — fast dev server, optimized builds
- **MapLibre GL JS** — open-source WebGL map renderer (free, no API key)
- **react-map-gl** — React wrapper for MapLibre with hooks API
- **deck.gl** — GPU-accelerated layer for rendering 1000+ animated vehicle dots
- **@microsoft/signalr** — SignalR client, receives position updates from backend
- **Zustand** — minimal global state (vehicle positions, filters, selected vehicle)
- **Tailwind CSS** — utility-first styling, responsive by default

### Map Tiles
- **CartoDB Dark Matter** — free, stunning dark basemap built on OpenStreetMap
- No API key required, CC-BY licensed
- URL: `https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png`

---

## Project Structure

```
/Iris
  /backend
    /Iris.Api          — ASP.NET Core Web API, SignalR hubs, background services
    /Iris.Core         — Domain models, interfaces, GTFS-RT parsing logic
    /Iris.Tests        — xUnit unit + integration tests
  /frontend
    /src
      /components      — Map, VehicleLayer, InfoPanel, FilterBar, etc.
      /hooks           — useVehicles, useSignalR, useMapState
      /stores          — Zustand stores
      /types           — TypeScript interfaces (Vehicle, TripUpdate, etc.)
    index.html
    vite.config.ts
    package.json
  README.md
  CLAUDE.md
```

---

## Data Architecture

### Vehicle Position Flow
```
Trafiklab GTFS-RT (every 2s)
  → C# BackgroundService polls VehiclePositions.pb
  → Protobuf parsed into VehiclePosition domain objects
  → Stored in-memory (keyed by vehicleId)
  → SignalR hub broadcasts delta updates to all connected clients
  → Frontend deck.gl ScatterplotLayer re-renders with new positions
```

### Mouse-over Info Flow
```
User hovers vehicle dot
  → deck.gl picking returns vehicleId
  → Frontend looks up cached TripUpdate (delay, destination, line)
  → InfoPanel renders: line, operator, destination, delay, speed
```

### Static Data (GTFS)
- On startup, download `sweden.zip` from Trafiklab static feed once
- Parse routes, stops, trips into in-memory lookup tables
- Used to resolve route names, vehicle types, operator names from IDs
- No DB — kept as dictionaries in memory

---

## Testing Strategy

### Backend — xUnit
- **Unit tests**: GTFS-RT protobuf parsing, domain logic, position delta calculation
- **Integration tests**: Full pipeline from raw `.pb` bytes to SignalR message shape
- Use sample `.pb` files (downloaded once, checked into `/testdata`) for deterministic tests
- Run: `dotnet test`

### Frontend — Vitest + React Testing Library
- **Unit tests**: Zustand store logic, data transformation hooks, utility functions
- **Component tests**: InfoPanel renders correct vehicle data, FilterBar toggles work
- No map rendering tests (WebGL can't run in jsdom) — mock the map layer
- Run: `npm test`

### Smoke Tests — Playwright
- Start backend + frontend locally
- Assert map loads, tiles visible, at least one vehicle dot appears within 10s
- Assert hover shows info panel
- Assert mobile viewport renders correctly
- Run: `npm run test:e2e`

### Rules
- Tests run after every new feature before moving on
- Failing tests block the next task
- Test files live next to what they test (`*.test.ts`, `*.Tests.csproj`)

---

## Look & Feel

### Visual Language
- **Dark map** — CartoDB Dark Matter base, near-black background
- **Vehicle dots** — small, glowing circles, color-coded by type:
  - Bus → `#4A9EFF` (blue)
  - Train → `#FF8C42` (orange)
  - Metro → `#B06AFF` (purple)
  - Tram → `#3EFFB0` (green)
  - Ferry → `#00D4FF` (cyan)
- **Dot size** — scales slightly with zoom level
- **Motion** — positions interpolated smoothly between 2s updates (no teleporting)
- **Selected vehicle** — pulsing ring highlight, slightly larger dot

### UI Components
- **Info panel** — glassmorphism card (backdrop-blur, semi-transparent), slides in from bottom on mobile / appears at cursor on desktop
- **Filter bar** — minimal icon-based toggle strip to show/hide vehicle types
- **Stats overlay** — small live counter (total vehicles visible, vehicles by type)
- **No clutter** — no sidebars, no menus. The map is everything.

### Responsiveness
- Desktop: full-screen map, info panel floats near cursor
- Mobile: full-screen map, info panel slides up from bottom as sheet
- Touch-friendly tap targets (min 44px)

---

## Security

- **API key never reaches the frontend** — all Trafiklab calls made server-side only
- API keys loaded from `appsettings.Local.json` (gitignored), never hardcoded:
  - `TRAFIKLAB_REALTIME_API_KEY` — Trafiklab Realtime API
  - `GTFS_SWEDEN_3_REALTIME_API_KEY` — GTFS Sweden 3 Realtime
- **CORS** — backend only allows requests from the local frontend origin (dev) or production domain
- **Rate limiting** — backend enforces per-IP limits on REST endpoints
- **HTTPS** — ASP.NET Core dev cert locally; enforce HTTPS in production
- **No user input reaches external APIs** — frontend only receives push data, sends no queries to Trafiklab
- **SignalR** — no authentication needed locally; add JWT when deploying to Azure

---

## Development Workflow

1. **Feature branch** per feature (e.g., `feature/vehicle-layer`, `feature/info-panel`)
2. Implement backend first, then frontend, then connect
3. Write tests alongside the feature
4. Run full test suite before merging
5. **Refactor pass** — after 2–3 features, look for dead code, duplication, or better abstractions. Remove aggressively.
6. Keep bundle size in check — run `vite build --report` regularly

### Running Locally
```bash
# Backend
cd backend/Iris.Api
dotnet run

# Frontend
cd frontend
npm run dev

# Tests
dotnet test                  # backend
npm test                     # frontend unit
npm run test:e2e             # playwright smoke tests
```

---

## Azure Deployment (Future)

When ready to deploy:
- **Backend** → Azure App Service (Linux, .NET 10)
- **Frontend** → Azure Static Web Apps
- **SignalR at scale** → Azure SignalR Service (backend switches to Azure backplane)
- **Secrets** → Azure Key Vault for API key
- **CI/CD** → GitHub Actions: build → test → deploy

No changes to local architecture needed; keep Azure-specific config in deployment pipeline only.

---

## API Research Findings (2026-03-22)

### Primary Data Source: GTFS Sweden 3

The existing Trafiklab key covers **GTFS Sweden 3**, which is the right choice for this project.

- **50+ operators** nationwide: SL, Västtrafik, Skånetrafiken, regional buses, ferries, trams, metro
- **VehiclePositions** — GPS coordinates, updated every **2 seconds** → the moving dots
- **TripUpdates** — delays and ETAs, updated every 15s → mouseover info
- **ServiceAlerts** — disruptions, updated every 15s
- Format: GTFS-RT protobuf (binary, efficient, good C# library support via `Google.Protobuf`)

**API endpoints:**
- VehiclePositions: `https://opendata.samtrafiken.se/gtfs-rt/sweden/VehiclePositions.pb?key=API_KEY`
- TripUpdates: `https://opendata.samtrafiken.se/gtfs-rt/sweden/TripUpdates.pb?key=API_KEY`
- ServiceAlerts: `https://opendata.samtrafiken.se/gtfs-rt/sweden/ServiceAlerts.pb?key=API_KEY`
- Static timetables: `https://opendata.samtrafiken.se/gtfs/sweden/sweden.zip?key=API_KEY`

### Known Gap: SJ Trains

**SJ (Swedish national rail) does not share GPS positions through Trafiklab.** SJ provides TripUpdates (schedule/delay data) only — no live GPS coordinates. SJ intercity trains cannot be shown as moving dots. Position could be estimated by interpolating between stations using delay data, but it would be approximate. Oxyfi also does not cover SJ.

### Optional Add-on: Oxyfi

Oxyfi is a Swedish company that puts GPS hardware on regional trains and streams positions via WebSocket. Separate API key required.

- **1-second updates**, <100ms latency
- Covers only **5 regional operators**, ~72–100 vehicles: Norrtåg, Tåg i Bergslagen, Värmlandstrafik, Kalmars länstrafik, Blekingetrafiken
- Format: NMEA GPRMC sentences
- **One connection per API key** — backend must fan out to clients
- **Verdict:** Low priority. These trains already appear via GTFS-RT. Add later for smoother animation.

### What We Don't Need
- **ResRobot** — journey planning only, no vehicle positions
- **SL APIs** — Stockholm only, fully redundant with GTFS Sweden 3
- **GTFS Sverige 2** — static schedules only, no realtime
- **NeTEx Regional** — more complex XML format, same operators; only if GTFS-RT proves insufficient

---

## Local Keys (not to be leaked)

Trafiklab Realtime APIs: `91f495e4efb24b5da36be12385f8e194`

GTFS Sweden 3 Realtime
Key: f98019ff1a6d4ac1a909be17e4d1c784 
