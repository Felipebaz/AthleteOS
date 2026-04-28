# AthleteOS

> Intelligent coaching SaaS platform for endurance sports with an AI assistant for coaches.
> Work in progress. Working name, subject to change.

## What this is

A B2B platform that helps running, cycling, triathlon and swimming coaches manage athletes remotely. The coach's assistant is the differentiating feature: a chat interface scoped to each athlete's data that helps the coach analyze training, draft plan adjustments, and answer questions like *"how is Juan doing this week?"*. The coach is always in the loop — the AI suggests, the coach approves.

**Primary user (paying):** the coach.
**Consumer user (free):** the athlete.

**Status:** Phase 0 — pre-development. Stack was just changed from .NET to TypeScript (see [`docs/adr/0003-stack-change-to-typescript.md`](./docs/adr/0003-stack-change-to-typescript.md)).

## Documentation

All technical documentation lives in the repo:

| File | Contents |
| --- | --- |
| [`CLAUDE.md`](./CLAUDE.md) | Operational briefing for AI agents (Claude Code). |
| [`docs/ARCHITECTURE.md`](./docs/ARCHITECTURE.md) | Buildable architecture (TypeScript edition). 7 sections. |
| [`docs/VISION.md`](./docs/VISION.md) | Long-term north-star architecture. Reference, not plan. |
| [`docs/adr/`](./docs/adr) | Architecture Decision Records — formal decisions. |
| [`docs/MIGRATION_NOTES.md`](./docs/MIGRATION_NOTES.md) | Notes on the .NET → TypeScript migration. |
| [`specs/`](./specs) | Feature specs (Spec-Driven Development). |

**Where to start:**

- If you're new to the project: read this README, then `docs/ARCHITECTURE.md` sections 1-3.
- If you're an AI agent: read `CLAUDE.md`. It redirects you to what you need.
- If you're contributing: read `CONTRIBUTING.md`.

## Tech stack (summary)

**Backend:** Node.js 20 + TypeScript + Fastify + Prisma + PostgreSQL + Redis + BullMQ.
**Frontend:** React + Vite + TypeScript + TanStack Router + TanStack Query + Tailwind + shadcn/ui.
**AI:** Anthropic Claude (direct SDK, no LangChain).
**APIs:** REST with OpenAPI auto-generated from Zod schemas.
**Infra:** Docker + GitHub Actions + Railway/Fly.io (MVP) → AWS (scale).
**Monorepo:** pnpm workspaces + Turborepo.

Full detail in [`docs/ARCHITECTURE.md`](./docs/ARCHITECTURE.md) section 4.

## Local dev requirements

- **Node.js 20 LTS** — recommended via [Volta](https://volta.sh/) or [fnm](https://github.com/Schniz/fnm)
- **pnpm 9+** — `npm install -g pnpm` or via Volta
- **Docker Desktop** (or Docker Engine + Compose plugin on Linux)
- **Git**
- Editor: VS Code with the recommended extensions (`.vscode/extensions.json`)

## Local setup

### 1. Clone and install

```bash
git clone <repo-url>
cd athleteos
pnpm install
```

### 2. Environment variables

```bash
cp .env.example .env
```

Defaults work for local development. Edit only if you need to override.

### 3. Start infrastructure

```bash
docker compose up -d
```

This starts:

- PostgreSQL 16 on `localhost:5432`
- Redis 7 on `localhost:6379`
- Mailhog (local SMTP catcher) on `localhost:8025` (web UI)

Verify:

```bash
docker compose ps
```

### 4. Apply database migrations

```bash
# Will be available once the api app exists (Phase 1).
pnpm --filter api db:migrate
```

### 5. Run everything in dev mode

```bash
pnpm dev
```

This starts (in parallel, via Turborepo):

- API at `http://localhost:3000`
- Coach dashboard at `http://localhost:5173`
- Athlete PWA at `http://localhost:5174`
- Swagger UI at `http://localhost:3000/docs`

Or run them individually:

```bash
pnpm --filter api dev
pnpm --filter web-coach dev
pnpm --filter web-athlete dev
```

## Repo structure

```
athleteos/
├── README.md                    This file
├── CLAUDE.md                    AI agent briefing
├── CONTRIBUTING.md              Contribution guide
├── .env.example
├── .nvmrc                       Node version pin
├── .editorconfig
├── docker-compose.yml           Local infra (Postgres, Redis, Mailhog)
├── package.json                 Root workspace
├── pnpm-workspace.yaml
├── turbo.json
├── tsconfig.base.json           Shared TS config
│
├── apps/
│   ├── api/                     Fastify backend
│   │   └── src/
│   │       ├── main.ts          Composition root
│   │       ├── modules/         iam, training-data, coaching
│   │       └── shared/          Cross-cutting (logging, errors, db client)
│   ├── web-coach/               Coach dashboard (React)
│   └── web-athlete/             Athlete PWA (React)
│
├── packages/
│   ├── shared-types/            Types shared across apps
│   ├── api-client/              Generated from OpenAPI spec
│   └── eslint-config/           Shared lint rules
│
├── docs/
│   ├── ARCHITECTURE.md          Buildable plan
│   ├── VISION.md                Long-term north star
│   ├── MIGRATION_NOTES.md       .NET → TypeScript migration
│   └── adr/                     Architecture Decision Records
│
├── specs/                       Feature specs (SDD)
│
└── .github/
    └── workflows/               CI/CD pipelines
```

## Useful commands

| Command | What it does |
| --- | --- |
| `pnpm install` | Install all workspace dependencies |
| `pnpm dev` | Start all apps in dev mode (API + frontends) |
| `pnpm build` | Build everything via Turborepo |
| `pnpm lint` | Lint all packages |
| `pnpm test` | Run all tests |
| `pnpm test:e2e` | Run Playwright end-to-end tests |
| `pnpm format` | Format with Prettier |
| `pnpm typecheck` | Run `tsc --noEmit` across the monorepo |
| `docker compose up -d` | Start local infra |
| `docker compose down` | Stop local infra |
| `docker compose logs -f postgres` | Postgres logs |
| `pnpm --filter api db:migrate` | Apply Prisma migrations |
| `pnpm --filter api db:studio` | Open Prisma Studio |

## Conventions

- **Commits:** [Conventional Commits](https://www.conventionalcommits.org/) in English. Examples: `feat(coaching): add session matching`, `fix(api): correct token refresh path`, `docs(adr): add ADR-0006`.
- **Branches:** Adapted Gitflow. `main` = prod. `develop` = integration. Features on `feature/<short-name>`.
- **Code:** English everywhere — variable names, comments, commit messages, docs.
- **Tests:** Mandatory for new code in `domain/` and `application/` layers.

## Rules

1. **Never commit secrets.** Use `.env.local` (in `.gitignore`) locally; cloud secrets manager in deployed environments.
2. **Never push directly to `main` or `develop`.** Always via PR.
3. **New features require a spec** in `specs/` before code (see [ADR-0002](./docs/adr/0002-sdd-sobre-ddd.md)).
4. **Architectural changes require an ADR** in `docs/adr/`.
5. **No LangChain. No Hugging Face in MVP.** See [ADR-0005](./docs/adr/0005-direct-anthropic-sdk-no-langchain.md).
6. **REST only. No GraphQL, no tRPC.** See [ADR-0004](./docs/adr/0004-rest-over-graphql-trpc.md).

## Maintainer

Felipe — early-stage solo project. For bugs, issues, or proposals: open a GitHub issue.

## License

To be defined. Until a formal decision, the code is proprietary; redistribution not permitted.
