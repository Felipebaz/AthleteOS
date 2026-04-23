# CLAUDE.md

> Briefing for AI agents (Claude Code) working in this repository.
> Humans read the [README](./README.md). You read this.

---

## Golden rule

**Before writing any code, read `docs/ARCHITECTURE.md`.** It contains the 15 architecture levels of the project: problem, requirements, bounded contexts, aggregates, flows, stack, roadmap. Everything you need to work with sound judgment is there. This file is the map to that document and the operational rules that complement it.

If you're asked to do something that contradicts `docs/ARCHITECTURE.md`, **ask before acting**. The architecture document is the source of truth; this file operates within its rules.

---

## What this project is in 3 lines

B2B SaaS platform for endurance sports coaches (running, cycling, triathlon, swimming) who manage athletes remotely. Ingests data from wearables (Strava, Garmin, Polar), analyzes it with AI, and presents the coach with a prioritized dashboard with actionable suggestions that the coach approves. The paying user is the coach, not the athlete.

**Current name:** placeholder (`AthleteOS` / `CoachLens`). Not final. Don't spend time on it.

**Current phase:** `[UPDATE: Phase 0 - Validation | Phase 1 - Foundations | Phase 2 - Ingestion | etc.]`

---

## Stack at a glance

```
Backend:     C# / .NET 8 + ASP.NET Core Minimal APIs + EF Core 8 + MediatR
             FluentValidation + Serilog + OpenTelemetry + Hangfire + MSTest
Data:        PostgreSQL 16 + TimescaleDB + pgvector + Redis
Frontend:    React 18 + TypeScript + Vite + TanStack (Query/Router) + Zustand
             Tailwind + shadcn/ui + Vitest + Playwright
AI:          Anthropic Claude via API (structured outputs)
Monorepo:    pnpm workspaces + Turborepo (frontends). Backend in separate repo or folder.
Infra MVP:   Railway or Fly.io + Cloudflare R2 (storage) + Cloudflare CDN
CI/CD:       GitHub Actions + Docker
Testing:     MSTest + FluentAssertions + Testcontainers + Bogus + NSubstitute
```

Full detail and justifications in `docs/ARCHITECTURE.md` level 13.

---

## Bounded contexts (what parts exist)

The system is a **modular monolith** organized by bounded contexts (DDD). Each has its own domain, its own DB schema, and isolated endpoints.

| Context | Type | What it does |
|---------|------|--------------|
| `Identity` | Generic | Auth, users, tenants, invitations |
| `AthleteProfile` | Supporting | Sports profile, zones, goals, injuries |
| `TrainingData` | Supporting | External data ingestion and normalization |
| `Coaching` | **Core** | Plans, execution, coach-athlete relationship |
| `Intelligence` | **Core** | Readiness, suggestions, learning |
| `Communication` | Supporting | Messaging, notifications |
| `Billing` | Generic | Subscriptions, payments (phase 2) |

**Fundamental rule:** modules do NOT reference each other by code. They communicate via:
1. **Integration events** (asynchronous, via outbox + bus).
2. **Module public API** (interfaces exposed in `ModuleX.Contracts`).

If you're tempted to do `using Coaching.Domain` from `Intelligence.Application`, **stop and ask for guidance**. You probably need an event or a contract.

---

## Non-negotiable architecture rules

These are the rules that keep the project healthy. Violating them causes future technical pain and will be rejected in code review.

### 1. Strict Clean Architecture

Dependencies point toward the domain:

```
Api ──► Application ──► Domain
 │           │
 └──► Infrastructure ──► Application ──► Domain
```

- `Domain` depends on nothing (not even `Microsoft.*` beyond `System.*`).
- `Application` depends only on `Domain` (and `BuildingBlocks.Application`).
- `Infrastructure` implements interfaces declared in `Domain` or `Application`.
- `Api` composes everything.

**Check:** if you're writing an `EntityFrameworkRepository` class inside `Domain`, something's wrong. If `Domain` has a `using Microsoft.EntityFrameworkCore`, something's wrong.

### 2. One use case = one aggregate

A command handler modifies **one single aggregate**. If it seems like you need to modify two, reconsider:
- Are they really the same aggregate?
- Should this be eventual consistency with an event?
- Is a domain service missing?

Never do transactions that touch 2 distinct aggregates with the same `SaveChanges`.

### 3. Outbox pattern for integration events

Cross-context events **always** go through outbox:
1. Command handler modifies aggregate.
2. In the same transaction, event is inserted into `outbox` table.
3. `OutboxPublisher` worker reads outbox and publishes to bus.
4. Consumers register in `inbox` table for idempotency.

Never publish to the bus directly from a handler. Never.

### 4. Strongly-typed IDs

Never use raw `Guid` as an identifier in the domain. Always use typed wrappers:

```csharp
public readonly record struct AthleteId(Guid Value);
public readonly record struct TrainingPlanId(Guid Value);
```

EF Core converts automatically with `ValueConverter` configured in `BuildingBlocks.Infrastructure`.

### 5. Tenant isolation

Every business table has `tenant_id` NOT NULL. Never omit it in:
- Entity creation.
- Queries (always filter).
- Migrations.

PostgreSQL Row Level Security (RLS) is enabled. If your query bypasses RLS, the test will fail.

### 6. No LLM in the domain

Anthropic calls live **only in `Intelligence.Infrastructure.Llm`**. The Intelligence domain doesn't know what provider is underneath. It depends on `IInsightGenerator`.

Same rule for any external service: adapter in Infrastructure, interface in Application or Domain.

### 7. Domain events != integration events

- **Domain events:** in-process, within the same bounded context, dispatched by MediatR within the unit of work. Suffix: `DomainEvent`.
- **Integration events:** cross-context, asynchronous, via outbox. Suffix: `IntegrationEvent`.

Don't confuse them. A `TrainingPlanCreatedDomainEvent` can trigger internal logic of the Coaching module. A `TrainingPlanCreatedIntegrationEvent` can be consumed by Communication to notify the athlete.

### 8. Result pattern, not exceptions for flow

Expected errors (validation, resource not found, business rule violated) are returned as `Result<T>` or `Result`. Exceptions only for truly exceptional errors (bug, infrastructure down).

```csharp
// Good
return Result.Failure<AthleteDto>(AthleteErrors.NotFound);

// Bad (for expected errors)
throw new NotFoundException("Athlete not found");
```

Violated invariants in aggregates do throw domain exceptions (`InvalidPlanAdjustmentException`), because they represent a caller bug.

### 9. Tests are part of the Definition of Done

- Minimum coverage: Domain 90%+, Application 70%+, global 60%+.
- Every new use case needs a test.
- Every new aggregate needs invariant tests.
- Integration tests with Testcontainers (real PostgreSQL, not in-memory).
- Don't merge if CI is red.

### 10. Observability from the start

New code must have:
- Structured logs with Serilog (appropriate level, no `Console.WriteLine`).
- OpenTelemetry metrics if it's a relevant operation.
- Error handling that preserves context (Sentry with useful tags).

---

## Backend folder structure

```
src/
├── BuildingBlocks/
│   ├── BuildingBlocks.Domain/            # Entity, AggregateRoot, ValueObject, DomainEvent base
│   ├── BuildingBlocks.Application/        # MediatR behaviors, abstractions
│   ├── BuildingBlocks.Infrastructure/     # Outbox, interceptors, event bus
│   └── BuildingBlocks.Api/                # Middlewares, filters
│
├── Modules/
│   ├── {ModuleName}/
│   │   ├── {ModuleName}.Domain/
│   │   ├── {ModuleName}.Application/
│   │   ├── {ModuleName}.Infrastructure/
│   │   ├── {ModuleName}.Api/
│   │   └── {ModuleName}.Contracts/        # public API for other modules (optional)
│   └── ...
│
├── Bootstrap/
│   └── ApiHost/                           # Program.cs, composes modules
│
└── Workers/
    ├── SyncWorker/
    ├── AnalysisWorker/
    ├── AiWorker/
    ├── NotificationWorker/
    └── OutboxPublisher/

tests/
├── Modules/
│   ├── {ModuleName}.UnitTests/
│   └── {ModuleName}.IntegrationTests/
└── E2E/
```

**When creating a new module:** follow the internal structure detailed in `docs/ARCHITECTURE.md` level 13.1.

---

## Code conventions

### Naming

- **Classes, records, structs:** `PascalCase`.
- **Methods, properties:** `PascalCase`.
- **Local variables, parameters:** `camelCase`.
- **Private fields:** `_camelCase` with underscore.
- **Constants:** `PascalCase` (not SCREAMING_SNAKE).
- **Files:** one public type per file, name == type.

### Domain names

- **Yes:** `TrainingPlan`, `Athlete`, `CoachSuggestion`, `ReadinessSnapshot`, `ApplyCoachSuggestionCommand`.
- **No:** `UserManager`, `DataHelper`, `ProcessorService`, `UtilClass`.

Names reflect the **business ubiquitous language**, not technical patterns.

### Language

- **Code (classes, variables, methods):** English. Always.
- **Comments:** English. Always.
- **Commits:** Conventional Commits in English.
- **Docs (README, ADRs, this file):** English.
- **Log messages:** English (easier to search).
- **User-facing strings:** English (i18n prepared for future localization).

### Commits (Conventional Commits in English)

Format: `<type>(<scope>): <short description>`

Types used: `feat`, `fix`, `refactor`, `test`, `docs`, `chore`, `perf`, `build`, `ci`.

Examples:

```
feat(coaching): add week adjustment with versioning
fix(training-data): fix activity matching with planned session
refactor(intelligence): extract readiness calculation to domain service
test(coaching): cover load progression invariants
docs(adr): add ADR on outbox pattern decision
chore(deps): update MediatR to 12.2.0
```

Scope = module or area (`coaching`, `intelligence`, `infra`, `ci`, etc.).

### Branches

Gitflow:
- `main` — production.
- `develop` — continuous integration, `dev` environment.
- `feature/<descriptive-name>` — new features.
- `fix/<name>` — bugfixes.
- `hotfix/<name>` — urgent patches from main.
- `release/<version>` — pre-prod stabilization.

**Never commit directly to `main` or `develop`.** Always via PR.

---

## When you're asked to implement something new

Follow this mental checklist:

1. **Is it in `docs/ARCHITECTURE.md`?** Reference the section. If what's asked contradicts the doc, ask first.
2. **Which bounded context does it belong to?** If it touches several, think about events.
3. **Is it a command or query?** Command = goes through aggregate. Query = direct read with DTO.
4. **Which aggregate does it modify?** Only one.
5. **What events does it emit?** Domain event or integration event?
6. **What invariants does it need to preserve?**
7. **What tests does it need?** Unit for aggregate + unit for handler + integration if there's a new repo.
8. **Does it require a DB change?** Reversible migration. Never touch migrations already applied in prod.
9. **Does it affect the public API?** Update contracts, version if breaking.
10. **Does it touch secrets or sensitive data?** Extra care with encryption, logs, auditing.

When done: **did I update docs if architecture changed? Did I add an ADR if it was an important decision?**

---

## When asked for something unclear

If the request is ambiguous or too broad, **don't implement blindly**. Respond with:

1. Your interpretation of what was requested.
2. 2-3 concrete clarification questions.
3. A proposed approach.

Bad response example: implement 4 files and 200 lines when asked to "add the notifications feature".

Good response example: "I understand you want notifications when the plan is adjusted. Before implementing: (a) web push, email, or both? (b) only the athlete or the coach too? (c) which events trigger notifications?"

---

## What you must NOT do (even if asked)

- **Don't install packages without asking.** New dependencies require evaluation.
- **Don't migrate to another ORM/DB/framework on impulse.** Stack changes are ADRs.
- **Don't delete tests.** If a test fails, fix the code or understand the test. Deleting it is silent regression.
- **Don't commit secrets.** Not even in feature branches. Not even "temporarily".
- **Don't disable CI checks to merge.** If CI fails, fix it.
- **Don't introduce microservices.** Modular monolith until there's a business reason to split. Decision is in ADR-001.
- **Don't write code without tests** in domain modules (core).
- **Don't optimize prematurely.** Measure first. Optimize after.
- **Don't rewrite working code** "because it looks ugly", unless there's a clear reason.
- **Don't generate destructive migrations** without explicit user confirmation.

---

## Sensitive data and security

This product handles **health data** (HRV, sleep, weight, injuries). Special category under GDPR/LGPD/Uruguay Law 18.331.

### Required treatment

- Wearable OAuth tokens (Garmin, Strava): **encrypted at application level** before persisting.
- Medical/injury data: encrypted at rest, never in logs.
- PII (emails, names): never in error logs with details.
- Coach-athlete messages: never in logs, never sent to LLM without redaction if they contain identifiable data.

### Safe logs

```csharp
// Bad
_logger.LogInformation("Sync for athlete {Email} starting", athlete.Email);

// Good
_logger.LogInformation("Sync for athlete {AthleteId} starting", athlete.Id);
```

PII only in dedicated audit log, not in operational logs.

### Access to sensitive data

Any admin/staff read of coach or athlete data is logged in audit log. There's no "peeking" without a record.

---

## Working with AI (Claude in the product)

The system uses Anthropic's Claude for:
- Generating suggestions for the coach.
- Generating plan drafts.
- Narrative explanations of analyses.

### Rules

1. **Never in the domain.** Always behind `IInsightGenerator` or another port.
2. **Always structured outputs.** Never parse free text for business decisions. Request JSON with schema, validate with FluentValidation.
3. **Versioned prompts.** They live in files (`Intelligence/Infrastructure/Llm/Prompts/v1/weekly-review.md`). Version bump when changing.
4. **Full logging.** Each call: prompt sent, response received, prompt version, latency, tokens, estimated cost.
5. **Aggressive caching.** Identical responses to identical contexts are not recalculated (key = hash of prompt + context).
6. **Coach-in-the-loop always.** No suggestion is applied without human approval. None.
7. **Hard domain rules are never violated by AI suggestion.** If the LLM suggests something that violates load progression, the application layer rejects it before reaching the coach.

---

## Useful commands (update as they're created)

```bash
# Backend .NET
dotnet build                            # build entire solution
dotnet test                             # all tests
dotnet test --filter Category=Unit      # unit tests only
dotnet ef migrations add <Name> --project src/Modules/<Module>/<Module>.Infrastructure
dotnet ef database update --project src/Modules/<Module>/<Module>.Infrastructure

# Frontend
pnpm install                            # install monorepo deps
pnpm dev                                # start dev environment
pnpm test                               # tests
pnpm build                              # production build
pnpm lint                               # lint

# Docker local
docker compose up -d                    # start PostgreSQL, Redis, etc.
docker compose down                     # stop everything
docker compose logs -f <service>        # logs
```

---

## Where to find things

| You need... | Go to... |
|-------------|----------|
| Product vision and problem | `docs/ARCHITECTURE.md` level 1 |
| Functional requirements (RF-* IDs) | `docs/ARCHITECTURE.md` level 2 |
| Non-functional requirements with targets | `docs/ARCHITECTURE.md` level 3 |
| Principles that guide decisions | `docs/ARCHITECTURE.md` level 4 |
| What bounded contexts exist | `docs/ARCHITECTURE.md` level 5 |
| Deployment and component view | `docs/ARCHITECTURE.md` level 6 |
| Data model and multi-tenancy | `docs/ARCHITECTURE.md` level 7 |
| Threat model and defense | `docs/ARCHITECTURE.md` level 8 |
| Technical risks and mitigations | `docs/ARCHITECTURE.md` level 9 |
| Backend module structure | `docs/ARCHITECTURE.md` level 10 |
| Aggregates, events, use cases per context | `docs/ARCHITECTURE.md` level 11 |
| End-to-end flows step by step | `docs/ARCHITECTURE.md` level 12 |
| Full tech stack with justification | `docs/ARCHITECTURE.md` level 13 |
| CI/CD, environments, infra | `docs/ARCHITECTURE.md` level 14 |
| Roadmap and phases | `docs/ARCHITECTURE.md` level 15 |
| Individual architectural decisions | `docs/adr/` |
| Domain glossary | `docs/ARCHITECTURE.md` Appendix A |

---

## About the human

I'm **Felipe**. Software engineering student at ORT (Uruguay), with frontend experience (React/TypeScript) expanding to backend (C#/.NET). This is my ambitious portfolio project + potential commercial spin-off.

### Preferences when working with me

- **Direct and technical.** No filler, no "great idea!". To the point.
- **Honesty about bad decisions.** If what I'm asking for is bad, tell me with reasons. I prefer grounded pushback over compliance.
- **TDD when applicable.** Especially in Domain and Application. Red-green-refactor.
- **Conventional Commits in English.**
- **Clean Architecture with Mousqués naming** (ORT course) mapped to standard names: `InterfazUsuario` = Api, `ServiciosAplicacion` = Application, `ReglasNegocio` = Domain, `AccesoDatos` = Infrastructure. In code I use standard English names; in comments/docs either works.
- **English** in docs and communication.

### How to respond well to me

- Before writing complex code, propose the approach.
- If you're choosing between two paths, show me both and recommend one.
- Explain the "why" of non-obvious decisions.
- Explicitly note when you're doing something outside what was asked ("added X because Y").

---

## Updating this document

This file evolves. When it changes:
- Stack → update Stack section.
- Architecture rules → update with reference to ADR.
- Project phase → update above.
- Useful commands → add as they're created.

If you're going to update this file, let me know first. It's a contract.

---

*Last updated: [date]. If you find a contradiction between this file and `docs/ARCHITECTURE.md`, the architecture doc wins. Let me know to resolve the inconsistency.*
