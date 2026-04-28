# CLAUDE.md

> Briefing for AI agents (Claude Code) working in this repository.
> Humans read the [README](./README.md). You read this.

---

## Golden rule

**Before writing any code, read `docs/ARCHITECTURE.md`.** It has 7 sections covering product scope, principles, modules, stack, APIs, AI integration, and roadmap. Everything you need to work with sound judgment is there. This file maps to that document and adds the operational rules that complement it.

If you're asked to do something that contradicts `docs/ARCHITECTURE.md`, **ask before acting**. The architecture document is the source of truth; this file operates within its rules.

---

## What this project is in 3 lines

B2B SaaS platform for endurance sports coaches (running, cycling, triathlon) who manage athletes remotely. Ingests activity data from Strava OAuth and manual FIT file uploads, analyzes it with an AI assistant, and presents the coach with a dashboard showing athlete compliance and AI-generated insights. The paying user is the coach, not the athlete.

**Current name:** placeholder (`AthleteOS` / `CoachLens`). Not final. Don't spend time on it.

**Current phase:** Phase 0 — Monorepo setup. Done when: `pnpm dev` starts everything locally and CI is green on `main`.

---

## Stack at a glance

```
Backend:     Node.js 20 LTS + TypeScript 5.4+ + Fastify 4 + Prisma + BullMQ
             Zod + Pino + Sentry + Vitest + Supertest + Testcontainers
Data:        PostgreSQL 16 + Redis 7
Frontend:    React 18 + TypeScript 5.4+ + Vite + TanStack (Query/Router) + Zustand
             Tailwind + shadcn/ui + Recharts + Vitest + Playwright
AI:          @anthropic-ai/sdk direct (no LangChain — see ADR-0005)
             Optional: Vercel AI SDK on frontend for streaming UI only
Monorepo:    pnpm workspaces + Turborepo
Infra MVP:   Railway or Fly.io + Cloudflare (DNS + CDN + WAF)
CI/CD:       GitHub Actions + Docker
```

Full detail and justifications in `docs/ARCHITECTURE.md` Section 4.

---

## Modules (what parts exist in MVP)

Three modules only. This is a **modular monolith** with Clean Architecture per module.

```
apps/api/src/modules/
├── iam/              Identity & Access (auth, users, coach-athlete relationship)
├── training-data/    Strava OAuth, FIT file upload, activity ingestion and normalization
└── coaching/         Training plans, session execution, AI assistant
```

Each module has the same internal structure:

```
modules/<name>/
├── domain/           Entities, value objects, domain events, repository interfaces, ports
├── application/      Use cases (one per public operation), DTOs
├── infrastructure/   Prisma repos, external clients, queue workers, AI implementations
└── api/              Fastify routes, Zod request/response schemas
```

**Modules do NOT import each other.** They communicate via:
1. **Public contracts** — each module exports an interface (`IIamPublic`, `ITrainingDataPublic`) consumed by other modules. Wired at the composition root (`apps/api/src/main.ts`).
2. **In-process domain events** — synchronous event bus in MVP. When it hurts, move to BullMQ.

If you're tempted to `import` from `../coaching` inside `training-data/`, stop. You need a public contract or an event.

---

## Development methodology: SDD on top of DDD (ADR-0002)

**Write a spec before implementing any domain feature.** This is not optional.

```
specs/<module>/NNN-feature-name.md   ← behavior spec (domain language, scenarios, invariants)
  → implementation plan               ← technical structure derived from spec
  → delegate to agent with spec + CLAUDE.md + ARCHITECTURE.md as context
  → review code against spec
  → merge, mark spec Implemented
```

Spec directory: `specs/` (see `specs/README.md` for full process, `specs/_template.md` to copy).

**When NOT to write a spec:** trivial bugfixes, pure refactors, config/dependency changes, UI adjustments with no backend effect.

---

## Coding discipline

> These guidelines bias toward caution over speed. For truly trivial tasks (a typo fix, a one-liner config change), use judgment. For everything else, follow them.

### Think before coding

- **State assumptions explicitly.** Before implementing, write out what you're assuming. If uncertain, ask — don't guess silently.
- **Present multiple interpretations.** If the request has more than one reasonable reading, surface all of them and ask which is intended. Don't pick one silently.
- **Name what's confusing.** If something is unclear, say exactly what's unclear. Don't implement around the confusion.
- **Push back when warranted.** If a simpler approach exists, say so. If what's asked contradicts the architecture, stop and ask before acting.

### Simplicity first

Minimum code that solves the problem. Nothing speculative.

- No features beyond what was asked.
- No abstractions for single-use code ("we might need this later" is not a reason).
- No "flexibility" or "configurability" that wasn't requested.
- No error handling for impossible scenarios.

Self-check: **"Would a senior engineer say this is overcomplicated?"** If yes, simplify before submitting.

### Surgical changes

Touch only what you must. Don't "improve" what you weren't asked to touch.

- Don't reformat, rename, or restructure code adjacent to your change.
- Match existing style, even if you'd do it differently.
- If you notice unrelated dead code or a bad pattern, **mention it** — don't delete or fix it.

**Orphan cleanup rule:** Remove imports, variables, and functions that *your* changes made unused. Don't remove pre-existing dead code unless explicitly asked. Every changed line should trace directly to the user's request.

### Goal-driven execution

Before starting a non-trivial task, state a brief plan with verifiable criteria:

```
1. [Step] → verify: [how you'll confirm it worked]
2. [Step] → verify: [how you'll confirm it worked]
```

Transform vague requests into verifiable goals:
- "Add validation" → "Write tests for invalid inputs, then make them pass"
- "Fix the bug" → "Write a test that reproduces it, then make it pass"

Strong success criteria let you loop independently. Weak criteria ("make it work") require constant clarification.

---

## Non-negotiable architecture rules

Violating these causes future technical pain and will be rejected in code review.

### 1. Clean Architecture: dependencies point inward

```
api ──► application ──► domain
 │            │
 └──► infrastructure ──► application ──► domain
```

- `domain/` depends on nothing (no Prisma, no Fastify, no external SDKs).
- `application/` depends only on `domain/` and defines ports (interfaces).
- `infrastructure/` implements those ports.
- `api/` composes everything.

**Check:** if `domain/` has a Prisma import, something is wrong. If `infrastructure/` has business logic, something is wrong.

### 2. One use case = one aggregate

A use case modifies **one aggregate**. If it seems like you need two, reconsider:
- Are they really the same aggregate?
- Should this be eventual consistency via an event?
- Is a domain service missing?

### 3. No outbox in MVP — in-process events

Cross-module events are dispatched via a simple synchronous event bus in MVP. Log and move on if a subscriber fails. No outbox table, no inbox table. If reliability becomes a real problem, that's when to add BullMQ-backed publishing.

### 4. Strongly-typed IDs

Never use raw `string` or `number` as an identifier in the domain. Use branded types:

```typescript
type AthleteId = string & { readonly __brand: 'AthleteId' };
type TrainingPlanId = string & { readonly __brand: 'TrainingPlanId' };
```

UUID v7 everywhere. IDs are generated at the domain boundary, not by the DB.

### 5. Tenant isolation in MVP: coachId filter in every query

No Row Level Security in MVP. Instead, every query that touches athlete data must filter by `coachId`. Middleware resolves `coachId` from the JWT and attaches it to the request context. If a query loads data without this filter, it's a security bug.

### 6. AI stays behind a port

Anthropic calls live **only in `coaching/infrastructure/ai/`**. The coaching domain defines `ICoachAssistant` and knows nothing about Anthropic, Claude, or any SDK.

Same rule for every external service: adapter in `infrastructure/`, interface in `application/` or `domain/`.

### 7. Domain events vs. cross-module events

- **Domain events:** in-process, within the same module, synchronous. Example: `TrainingPlanCreatedEvent` triggers internal plan initialization logic.
- **Cross-module events:** emitted by one module, subscribed by another, via the in-process event bus. Example: `ActivityIngested` (from `training-data`) is consumed by `coaching` to auto-match with planned sessions.

Keep them separate. Don't use cross-module events for in-module coordination.

### 8. Result pattern, not exceptions for expected errors

```typescript
// Good
return Result.failure(AthleteErrors.notFound(athleteId));

// Bad (for expected errors)
throw new NotFoundException(`Athlete ${athleteId} not found`);
```

Exceptions only for truly unexpected errors (bugs, infra down). Invariant violations in aggregates throw domain exceptions because they represent a caller bug.

### 9. Tests are part of the Definition of Done

- Coverage targets: domain 90%+, application 70%+.
- Every new use case needs a test.
- Every new aggregate needs invariant tests.
- Integration tests with Testcontainers (real PostgreSQL, not SQLite in-memory).
- Don't merge if CI is red.

### 10. Observability from day one

Every new endpoint or background job must have:
- Structured logs with Pino (no `console.log`). Log level appropriate to severity.
- Correlation ID propagated through the request lifecycle.
- Errors sent to Sentry with useful context (userId, coachId, relevant IDs — never PII like emails or names).

### 11. Specs before features (ADR-0002)

Before asking the agent to implement a domain feature, write the spec in `specs/<module>/`. The spec defines behavior, invariants, and acceptance scenarios. The agent uses it as context. Code without a spec gets rejected in review.

---

## Monorepo layout

```
athleteos/
├── apps/
│   ├── api/                  Fastify backend
│   │   └── src/
│   │       ├── modules/      iam/, training-data/, coaching/
│   │       ├── shared/       cross-cutting (logger, errors, db client, event bus)
│   │       └── main.ts       composition root, module wiring
│   ├── web-coach/            React dashboard for coach
│   └── web-athlete/          React PWA for athlete
├── packages/
│   ├── shared-types/         types shared between apps (event payloads, etc.)
│   ├── api-client/           generated from OpenAPI spec, consumed by both web apps
│   └── eslint-config/        shared ESLint rules (includes boundaries plugin)
├── docs/
│   ├── ARCHITECTURE.md       current buildable plan (TypeScript edition)
│   ├── VISION.md             2-year north star (former architecture doc, not the active plan)
│   └── adr/                  architecture decision records
├── specs/                    feature behavior specs (SDD — see ADR-0002)
├── docker-compose.yml        Postgres + Redis + MailHog for local dev
├── turbo.json
├── pnpm-workspace.yaml
└── package.json
```

---

## Code conventions

### Naming

- **Classes, interfaces, types:** `PascalCase`.
- **Functions, methods, variables:** `camelCase`.
- **Private class fields:** `#camelCase` (native private) or `_camelCase` (conventional).
- **Constants:** `SCREAMING_SNAKE` only for module-level primitive constants. `PascalCase` for objects/records.
- **Files:** one primary export per file. File name matches the export name (`coach-assistant.ts` exports `CoachAssistant`). Kebab-case filenames.

### Domain names

- **Yes:** `TrainingPlan`, `Athlete`, `SessionExecution`, `ActivityIngested`, `AskCoachAssistantUseCase`.
- **No:** `UserManager`, `DataHelper`, `ProcessorService`, `UtilClass`, `Handler`.

Names reflect the **business ubiquitous language**, not technical patterns. See Appendix A of `docs/ARCHITECTURE.md` for the glossary.

### Language

- **Code:** English. Always.
- **Comments:** English. Only when the WHY is non-obvious.
- **Commits:** Conventional Commits in English.
- **Docs (README, ADRs, specs, this file):** English.
- **Log messages:** English.

### Commits (Conventional Commits in English)

Format: `<type>(<scope>): <short description>`

Types: `feat`, `fix`, `refactor`, `test`, `docs`, `chore`, `perf`, `build`, `ci`.

Scope = module or area: `iam`, `training-data`, `coaching`, `infra`, `ci`, `deps`, etc.

Examples:

```
feat(coaching): add training plan creation use case
fix(training-data): correct activity duration parsing from FIT file
refactor(iam): extract JWT validation to shared middleware
test(coaching): cover plan invariant for consecutive high-intensity sessions
docs(adr): add ADR-0005 on direct Anthropic SDK
chore(deps): update Fastify to 4.27.0
```

### Branches (Gitflow)

- `main` — production.
- `develop` — continuous integration, `dev` environment.
- `feature/<descriptive-name>` — new features.
- `fix/<name>` — bugfixes.
- `hotfix/<name>` — urgent patches from main.
- `release/<version>` — pre-prod stabilization.

**Never commit directly to `main` or `develop`.** Always via PR.

---

## When you're asked to implement something new

Follow this checklist in order:

1. **Is there a spec?** For domain features: check `specs/<module>/`. If no spec exists, write one first (see `specs/README.md`). If the request is a bugfix or config change, skip.
2. **Is it in `docs/ARCHITECTURE.md`?** Reference the section. If what's asked contradicts the doc, ask before acting.
3. **Which module does it belong to?** `iam`, `training-data`, or `coaching`. If it touches several, think about cross-module events.
4. **Is it a command or query?** Command = modifies an aggregate. Query = reads data, returns a DTO.
5. **Which aggregate does it modify?** Only one. If two, reconsider.
6. **What events does it emit?** Domain event or cross-module event?
7. **What invariants must it preserve?** List them, they become unit tests.
8. **What tests does it need?** Aggregate invariant tests + use case tests + integration test if there's a new repo.
9. **Does it require a DB change?** Reversible Prisma migration. Never edit migrations already applied to prod.
10. **Does it touch secrets or sensitive data?** Encrypt OAuth tokens. Never log PII. Never send athlete PII to the LLM without explicit handling.

When done: **did I update docs if architecture changed? Did I add an ADR if it was an important decision?**

---

## When asked for something unclear

If the request is ambiguous or too broad, don't implement blindly. Respond with:

1. Your interpretation of what was requested.
2. 2-3 concrete clarification questions.
3. A proposed approach.

Bad: implement 4 files and 200 lines when asked to "add the notifications feature".

Good: "I understand you want notifications when the plan is adjusted. Before implementing: (a) email or in-app? (b) only the athlete, or the coach too? (c) which events trigger it?"

---

## What you must NOT do (even if asked)

**Architecture:**
- **Don't install packages without asking.** New dependencies require evaluation.
- **Don't migrate to another ORM/DB/framework on impulse.** Stack changes are ADRs.
- **Don't introduce microservices.** Modular monolith until there's a business reason to split. See ADR-0001.
- **Don't use LangChain or any AI orchestration framework.** Direct Anthropic SDK only. See ADR-0005.
- **Don't add outbox/inbox tables or RLS** — deferred to Phase 3. MVP uses simpler mechanisms.

**Code quality:**
- **Don't delete tests.** If a test fails, fix the code or understand the test. Deleting is silent regression.
- **Don't write domain code without tests.**
- **Don't implement features without a spec** (for domain features). Spec first, code second.
- **Don't optimize prematurely.** Measure first. Optimize after.
- **Don't rewrite working code** without a clear reason beyond "it looks ugly".
- **Don't improve adjacent code** that isn't part of the change. Mention it instead.

**Safety:**
- **Don't commit secrets.** Not even in feature branches. Not even "temporarily".
- **Don't disable CI checks to merge.** If CI fails, fix it.
- **Don't generate destructive migrations** without explicit user confirmation.

---

## Sensitive data and security

This product handles **health data** (activities, heart rate, training load, injuries). Special category under GDPR/LGPD/Uruguay Law 18.331.

### Required treatment

- Strava OAuth tokens: **encrypted at application level** before persisting (AES-256-GCM).
- PII (emails, names): never in error logs or operational logs.
- Athlete activity data: never sent to the LLM raw — send structured context, not raw Prisma rows.

### Safe logs

```typescript
// Bad
logger.info({ email: athlete.email }, 'Sync starting');

// Good
logger.info({ athleteId: athlete.id }, 'Sync starting');
```

PII only in dedicated audit log, never in operational logs.

### Access to sensitive data

Any admin read of coach or athlete data is logged in an audit log. No "peeking" without a record.

---

## Working with AI (Claude in the product)

The system uses Anthropic's Claude for:
- Conversational AI assistant (chat scoped to an athlete's data).
- Structured plan change proposals (Phase 2: automatic weekly suggestions).

### Rules

1. **Never in the domain.** Always behind `ICoachAssistant` (or another port).
2. **Always structured outputs.** Never parse free text for business decisions. Request JSON matching a Zod schema. Retry up to 2x with validation errors fed back to the model.
3. **Full logging.** Each call: input tokens, output tokens, model, latency, conversationId, athleteId, estimated cost.
4. **Cost cap.** Hard cap per coach per day ($5 USD). If exceeded, assistant stops responding and alerts the founder.
5. **Coach-in-the-loop always.** No suggestion is applied without human approval. None.
6. **No LangChain.** See ADR-0005.

---

## Useful commands

```bash
# Monorepo
pnpm install                            # install all workspace deps
pnpm dev                                # start all apps (via Turborepo)
pnpm build                              # build all apps
pnpm test                               # run all tests
pnpm lint                               # lint all packages
pnpm format                             # format with Prettier

# Backend only (from apps/api/)
pnpm dev                                # start Fastify dev server
pnpm test:unit                          # unit tests
pnpm test:integration                   # integration tests (Testcontainers)
npx prisma migrate dev --name <Name>    # create and apply a new migration
npx prisma migrate deploy               # apply pending migrations (CI/prod)
npx prisma studio                       # DB browser

# Docker local
docker compose up -d                    # start Postgres, Redis, MailHog
docker compose down                     # stop (data preserved)
docker compose down -v                  # stop + wipe data (careful)
docker compose logs -f <service>        # tail logs
```

---

## Where to find things

| You need... | Go to... |
|-------------|----------|
| Product scope and what's in/out of MVP | `docs/ARCHITECTURE.md` Section 1 |
| Architectural principles (P1-P12) | `docs/ARCHITECTURE.md` Section 2 |
| Module responsibilities and contracts | `docs/ARCHITECTURE.md` Section 3 |
| Full tech stack with justification | `docs/ARCHITECTURE.md` Section 4 |
| API endpoints, conventions, OpenAPI | `docs/ARCHITECTURE.md` Section 5 |
| AI assistant architecture and rules | `docs/ARCHITECTURE.md` Section 6 |
| Roadmap and phases | `docs/ARCHITECTURE.md` Section 7 |
| Domain glossary | `docs/ARCHITECTURE.md` Appendix A |
| ADR index | `docs/ARCHITECTURE.md` Appendix B and `docs/adr/` |
| Feature behavior specs | `specs/<module>/` |
| Spec process and template | `specs/README.md` and `specs/_template.md` |
| 2-year north star (not the active plan) | `docs/VISION.md` |

---

## About the human

I'm **Felipe**. Software engineering student at ORT (Uruguay), with frontend experience (React/TypeScript) now building the TypeScript backend. This is my ambitious portfolio project + potential commercial spin-off.

### Preferences when working with me

- **Direct and technical.** No filler, no "great idea!". To the point.
- **Honesty about bad decisions.** If what I'm asking for is bad, tell me with reasons. I prefer grounded pushback over compliance.
- **TDD when applicable.** Especially in `domain/` and `application/`. Red-green-refactor.
- **Conventional Commits in English.**
- **English** in code, docs, and communication.

### How to respond well to me

- Before writing complex code, propose the approach.
- If you're choosing between two paths, show me both and recommend one.
- Explain the "why" of non-obvious decisions.
- Explicitly note when you're doing something outside what was asked ("added X because Y").

---

## Updating this document

This file evolves. When it changes:
- Stack → update Stack section and reference the ADR.
- Architecture rules → update with reference to ADR.
- Project phase → update the phase line above.
- Useful commands → add as they're created.

If you're going to update this file, let Felipe know first. It's a contract.

---

*Last updated: 2026-04-28. If you find a contradiction between this file and `docs/ARCHITECTURE.md`, the architecture doc wins. Flag the inconsistency.*
