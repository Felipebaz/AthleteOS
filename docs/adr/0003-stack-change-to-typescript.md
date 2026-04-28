# ADR-0003 — Stack change from .NET to TypeScript

- **Status:** Accepted
- **Date:** 2026-04
- **Supersedes:** implicit decision in initial repo setup (.NET 8/10)

## Context

The repository was initialized with C# / .NET as the backend stack. The choice was driven by:

- Felipe's current coursework at ORT (Diseño de Aplicaciones 1) using C# and Clean Architecture
- Strong DDD ergonomics in C# (records, value types, EF Core configurations)
- Personal familiarity with the language

After eight commits of foundation work (BuildingBlocks, solution structure, Docker Compose, ADRs), no product feature has been built yet. The decision is being revisited before Phase 1 work begins.

The reframing came from honest review of three constraints:

1. **Single developer, ~20 hours/week.** Mental context-switching between TypeScript (frontend) and C# (backend) is a real cognitive cost.
2. **AI is a core product capability.** The TypeScript ecosystem for LLM applications (Anthropic SDK, Vercel AI SDK, structured outputs with Zod) is more mature in 2026 than .NET's equivalent.
3. **Job market positioning.** TypeScript + Node is dominant in remote-USD-paying startups; .NET dominates enterprise but is rare in early-stage SaaS.

## Options considered

### Option A — Stay on .NET 8

**Pros:**
- No throwaway work; existing BuildingBlocks setup is preserved.
- Strong DDD ergonomics (records, sealed types, EF Core).
- Continuity with current ORT coursework (WorldCupPlanner uses .NET).
- C# gives stricter compile-time guarantees than TypeScript.

**Cons:**
- Two languages to maintain (TypeScript frontend + C# backend).
- Anthropic SDK and AI tooling more mature in JavaScript/TypeScript.
- Slower iteration: more ceremony around solutions, projects, configurations.
- Heavier deploys: .NET cold-start and image sizes are larger.
- Smaller surface area in remote startup job market.

### Option B — TypeScript end-to-end (chosen)

**Pros:**
- Single language across backend, frontend, scripts, tests.
- Shared types between backend and frontend via monorepo packages.
- Mature AI tooling (Anthropic SDK, Zod for structured outputs).
- Faster iteration: lighter setup, faster builds, faster deploys.
- Better fit for managed PaaS hosting (Railway, Fly.io) for cost and simplicity.
- Larger remote-USD job market.
- Frontend skills (already in TypeScript) directly transfer.

**Cons:**
- TypeScript's type system is less strict than C# at runtime (compile-time only).
- DDD patterns require more boilerplate (no built-in records with structural equality).
- Throws away existing .NET scaffolding (~1-2 days of setup work).
- Felipe's current C# coursework no longer reinforces project work.

### Option C — Hybrid (.NET backend + TypeScript-only frontend, sharing nothing)

**Pros:**
- Preserves both stacks' strengths.

**Cons:**
- All cons of Option A plus the integration overhead of OpenAPI/codegen for type sharing.
- Highest cognitive load.

## Decision

**Option B — TypeScript end-to-end.**

Concretely:

- Backend: Node.js 20 LTS + TypeScript 5.4+ + Fastify + Prisma + PostgreSQL + BullMQ
- Frontend: React + Vite + TypeScript + TanStack stack + Tailwind + shadcn/ui
- Monorepo: pnpm workspaces + Turborepo
- Shared packages for types between apps

Clean Architecture is preserved per module: `domain/`, `application/`, `infrastructure/`, `api/`. DDD tactical patterns (aggregates, value objects, domain events, repositories as ports) are kept; the C# idioms (records, EF Core configurations) are translated to TypeScript equivalents (readonly classes, Prisma schema + mapper functions).

## Consequences

### Positive

- One mental model. Less context-switching, faster iteration.
- AI features built with native, well-documented tooling.
- Repo simpler: one `package.json`, one runtime, one build system (Turborepo).
- Frontend and backend types stay in sync via OpenAPI codegen.
- Reduced infrastructure complexity (no .NET runtime, smaller images).

### Negative

- All existing `.NET` scaffolding is removed: `BuildingBlocks/*`, `*.csproj`, `*.slnx`, `Directory.Build.props`, `Directory.Packages.props`, `docker-compose.yml` (the .NET-specific service is removed; Postgres + Redis kept).
- ADR-0001 ("Modular monolith over microservices") is preserved in spirit but the implementation language is now different.
- Felipe's coursework continues in C# (independent project at ORT), but AthleteOS is no longer tied to it.

### Migration

A `MIGRATION_NOTES.md` document at the repo root details what is removed, kept, and added. Migration is done in a single commit on `develop`, before Phase 0 of the new roadmap begins.

### What does NOT change

- Clean Architecture as architectural style.
- DDD as design approach (aggregates, value objects, ubiquitous language).
- Modular monolith (ADR-0001).
- Spec-driven development on top of DDD (ADR-0002).
- Conventional Commits and Gitflow.
- ADR-driven decision history.
- The product vision and bounded contexts (just the language they're expressed in).

## Notes for AI agents (Claude Code)

When working in this repo after this ADR is accepted:

- Ignore any `.cs`, `.csproj`, `.slnx`, `Directory.*.props` files. They are scheduled for removal.
- Refer to `docs/ARCHITECTURE.md` (TypeScript edition) as the source of truth.
- The previous architecture document is preserved as `docs/VISION.md` for reference but is not the active plan.
