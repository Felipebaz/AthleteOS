# Architecture Decision Records (ADRs)

This directory contains the important architectural decisions of the project, each documented with its context, alternatives considered, decision made, and accepted consequences.

## What is an ADR?

An **Architecture Decision Record** is a short document (1-3 pages) that captures a relevant technical decision at the time it's made. It serves to:

- Understand *why* the code is the way it is, months or years later.
- Avoid re-debating decisions already made ("why do we use PostgreSQL and not Mongo?").
- Detect when a decision no longer applies because the context changed.
- Fast onboarding of new collaborators.

The value of an ADR is in writing it **when the decision is made**, not retroactively.

## When to write an ADR

Write an ADR when the decision:

- **Is hard to reverse** (changing language, database, architectural pattern, critical provider).
- **Has consequences visible in the code for a long time** (patterns like CQRS, outbox, anticorruption layer).
- **Involves significant trade-offs** where other people might question the choice.
- **Affects multiple modules or teams**.

**Don't write an ADR** for low-impact reversible decisions, code style (that goes in the linter), or product features (that goes in the backlog).

## Format

All ADRs follow the template in `000-template.md`. Structure:

1. Metadata (date, status, deciders).
2. Context and problem.
3. Forces in tension.
4. Alternatives considered.
5. Decision.
6. Consequences (positive, negative, neutral).
7. When to revisit.
8. References.

## Possible statuses

- **Proposed:** under discussion, not yet implemented.
- **Accepted:** decided and implemented (or in implementation).
- **Deprecated:** no longer applies but kept for history.
- **Superseded by ADR-NNNN:** overridden by a later decision (link to the new one).

## Naming conventions

`NNNN-short-descriptive-title-kebab-case.md`

- `NNNN` = sequential number with zero-padding (0001, 0002, ..., 0123).
- Short, descriptive title in English, kebab-case.
- Never reuse numbers, even if an ADR is deprecated.

Examples: `0001-modular-monolith.md`, `0007-postgresql-as-main-db.md`.

## ADR Index

### Accepted

| # | Title | Date | Status |
|---|-------|-------|--------|
| [0001](./0001-monolito-modular.md) | Modular monolith over microservices | 2026-04-22 | Accepted |
| [0002](./0002-sdd-sobre-ddd.md) | Spec-Driven Development on top of DDD | 2026-04-22 | Accepted |

### Proposed (under discussion)

_(none yet)_

### Deprecated

_(none yet)_

## ADRs planned to write

List of important decisions that will be made and documented as the project advances. **Don't write these preventively**: write them when the decision is actually made, with real context.

### Technical foundations phase

- **ADR-0003:** PostgreSQL + TimescaleDB as main data stack.
- **ADR-0004:** Row-based multi-tenancy with Row Level Security.
- **ADR-0005:** Outbox pattern for integration events.
- **ADR-0006:** Strongly-typed IDs in the domain.
- **ADR-0007:** Authentication: ASP.NET Core Identity vs. Clerk vs. Auth0 (to decide).
- **ADR-0008:** Testing strategy (MSTest + Testcontainers + coverage by layer).

### Ingestion and processing phase

- **ADR-0009:** Anticorruption Layer per external provider (Strava, Garmin, etc.).
- **ADR-0010:** OAuth token handling (application-level encryption + rotation).
- **ADR-0011:** TimescaleDB vs. columnar storage for activity streams.
- **ADR-0012:** Retry strategy and dead letter queue for synchronizations.

### Intelligence phase

- **ADR-0013:** `IInsightGenerator` abstraction to decouple from LLM provider.
- **ADR-0014:** Structured outputs and LLM response validation.
- **ADR-0015:** Prompt versioning as a code artifact.
- **ADR-0016:** Coach-in-the-loop as a non-negotiable requirement.
- **ADR-0017:** LLM response caching for cost control.

### Product phase

- **ADR-0018:** PWA over React Native for the athlete app (MVP).
- **ADR-0019:** Monorepo with pnpm + Turborepo for frontends.
- **ADR-0020:** Typed API client generation from OpenAPI.
- **ADR-0021:** BFF pattern to separate web (coach) and mobile (athlete) concerns.

### Infrastructure and operations phase

- **ADR-0022:** Railway/Fly.io as initial PaaS, migration to AWS/Azure as next step.
- **ADR-0023:** GitHub Actions for CI/CD with manual gates to production.
- **ADR-0024:** Gitflow as branching strategy.
- **ADR-0025:** Conventional Commits in English.
- **ADR-0026:** Observability stack (Serilog + OpenTelemetry + Sentry).
- **ADR-0027:** Backup strategy and restore testing.

### Commercial phase

- **ADR-0028:** Stripe + MercadoPago as payment gateway.
- **ADR-0029:** Tier-based pricing model based on managed athletes.
- **ADR-0030:** Data retention policy and compliance with law 18.331 / GDPR.

This list is indicative and will change as the project evolves. Some planned ADRs may never be written because the decision resolves trivially; others will appear because unexpected problems arose.

## Process for writing a new ADR

1. Copy `000-template.md` to `NNNN-title.md` with the next available number.
2. Fill in sections in order: context → alternatives → decision → consequences.
3. Mark initial status as `Proposed` if it needs discussion, `Accepted` if already decided.
4. Add to the index in this README.
5. Commit with message `docs(adr): add ADR-NNNN on <topic>`.
6. If the decision affects `CLAUDE.md` or `docs/ARCHITECTURE.md`, update those files in the same PR.

## When to deprecate an ADR

If a documented decision no longer applies:

1. Change status to `Deprecated` or `Superseded by ADR-NNNN`.
2. Add a section at the end explaining why.
3. If superseded, link to the new ADR (bidirectionally).
4. **Don't delete the old ADR.** History matters.

---

*ADRs are contracts with the future. Take them seriously, but don't let them become bureaucracy. The goal is clarity, not defensive documentation.*
