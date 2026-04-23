# AthleteOS

> Intelligent coaching SaaS platform for endurance sports.
> Work in progress. Working name, subject to change.

## What is this

B2B platform that helps running, cycling, triathlon and swimming coaches manage athletes remotely with AI assistance. Ingests data from wearables (Strava, Garmin, Polar), continuously analyzes it, and presents the coach with a prioritized dashboard with actionable suggestions that the coach approves, modifies or rejects.

**Primary user:** the coach. The athlete uses the app as a consumer, not as a paying customer.

**Current status:** Phase 1 — Technical foundations. No product features yet.

## Documentation

All technical documentation lives in the repo:

| File | Contents |
|------|----------|
| [`CLAUDE.md`](./CLAUDE.md) | Operational briefing for AI agents (Claude Code). |
| [`docs/ARCHITECTURE.md`](./docs/ARCHITECTURE.md) | Full architecture document, 15 levels. |
| [`docs/adr/`](./docs/adr/) | Architecture Decision Records — formal decisions. |
| [`specs/`](./specs/) | Feature specs (Spec-Driven Development). |

**Where to start:**

- If you're a human new to the project: read this README, then `docs/ARCHITECTURE.md` levels 1-5.
- If you're an AI agent: read `CLAUDE.md`. It redirects you to what you need.
- If you're coming to contribute: read [CONTRIBUTING.md](./CONTRIBUTING.md).

## Tech stack (summary)

**Backend:** C# / .NET 8 + ASP.NET Core + EF Core + PostgreSQL + TimescaleDB + Redis.
**Frontend:** React + TypeScript + Vite + TanStack + Tailwind.
**AI:** Anthropic Claude.
**Infra:** Docker + GitHub Actions + Railway/Fly.io (MVP) → AWS (scale).

Full detail in `docs/ARCHITECTURE.md` level 13.

## Local dev requirements

- **.NET 8 SDK** — [download](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Node.js 20+ and pnpm 9+** — recommended via [Volta](https://volta.sh/) or [fnm](https://github.com/Schniz/fnm)
- **Docker Desktop** (or Docker Engine + Compose plugin on Linux)
- **Git**
- Code editor: VS Code + C# Dev Kit (free), Rider, or similar

## Local setup

### 1. Clone the repo

```bash
git clone <repo-url>
cd athleteos
```

### 2. Copy the environment variables file

```bash
cp .env.example .env
```

Edit `.env` if you need to customize anything. For local development, defaults should work.

### 3. Start infrastructure services

```bash
docker compose up -d
```

This starts PostgreSQL (with TimescaleDB), Redis and a local MailHog for email testing. Verify with:

```bash
docker compose ps
```

### 4. Apply database migrations

```bash
# (Available when Block 2 exists)
dotnet run --project src/Bootstrap/ApiHost -- migrate
```

### 5. Run the API

```bash
# (Available when Block 2 exists)
dotnet run --project src/Bootstrap/ApiHost
```

The API runs at `http://localhost:5000`. Swagger UI at `http://localhost:5000/swagger`.

### 6. (Optional) Run the frontends

```bash
# (Available when Block 4 exists)
cd frontends
pnpm install
pnpm dev
```

The coach dashboard runs at `http://localhost:5173`, the athlete PWA at `http://localhost:5174`.

## Repo structure

```
athleteos/
├── CLAUDE.md                    AI agent briefing
├── README.md                    This file
├── .env.example                 Environment variables template
├── .gitignore
├── .editorconfig                Editing conventions
├── docker-compose.yml           Local infrastructure services
├── docs/
│   ├── ARCHITECTURE.md          Architecture (15 levels)
│   └── adr/                     Architecture decisions
├── specs/                       Feature specs
├── src/                         Backend code (.NET)
│   ├── BuildingBlocks/
│   ├── Modules/
│   ├── Bootstrap/
│   └── Workers/
├── frontends/                   Frontend monorepo (to be created)
│   ├── apps/
│   └── packages/
├── tests/                       Backend tests
└── .github/
    └── workflows/               CI/CD (GitHub Actions)
```

## How to run tests

```bash
# Full backend
dotnet test

# Unit tests only
dotnet test --filter Category=Unit

# Single module only
dotnet test tests/Modules/Coaching.UnitTests

# Frontend (when it exists)
cd frontends && pnpm test
```

## Daily commands

| Command | What it does |
|---------|-------------|
| `docker compose up -d` | Start local infra |
| `docker compose down` | Stop local infra |
| `docker compose logs -f postgres` | PostgreSQL logs |
| `dotnet build` | Build the solution |
| `dotnet test` | Run all tests |
| `dotnet format` | Format code |
| `pnpm dev` | Start frontends in dev mode |

## Conventions

- **Commits:** Conventional Commits in English. E.g.: `feat(coaching): add week adjustment`.
- **Branches:** Adapted Gitflow. `main` is prod, `develop` is dev, features on `feature/*`.
- **Code:** English always. Comments and docs in English.
- **Tests:** mandatory for new code in Domain and Application.

## Important rules

1. **Never commit secrets.** Use `.env.local` (in `.gitignore`) and vault in cloud.
2. **Never merge directly to `main` or `develop`.** Always via PR.
3. **New features require a spec before code** (see `docs/adr/0002-sdd-sobre-ddd.md`).
4. **Architectural changes require an ADR.**

## Support and contact

Early-stage project, maintained by [Felipe](https://github.com/<your-username>).

For bugs, issues or proposals: open an issue in this repo.

## License

To be defined. Until a formal decision is made, the code is proprietary and redistribution is not permitted.
