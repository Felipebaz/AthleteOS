# ADR-001: Modular Monolith over Microservices

- **Date:** 2026-04-22
- **Status:** Accepted
- **Deciders:** Felipe
- **Related technical context:** `docs/ARCHITECTURE.md` Section 3 (Modules and boundaries).
- **Note:** The decision body below references .NET projects and EF Core. The **architectural decision** (modular monolith, module isolation, communication via contracts/events) remains valid. The implementation language changed to TypeScript per ADR-0003. References to `.csproj`, `SaveChanges`, or `NetArchTest` are superseded; the equivalent enforcement mechanisms in TypeScript are `eslint-plugin-boundaries` and Vitest architecture tests.

## Context and problem

The system models a domain with multiple clearly differentiated bounded contexts (Identity, Athlete Profile, Training Data, Coaching, Intelligence, Communication, eventually Billing). The existence of natural boundaries makes it tempting to implement each context as an independent microservice from the start, following popular modern architecture patterns.

However, real constraints make this decision non-trivial:

1. **Single-person team** in the initial phase (the founder). Operating multiple services, coordinating deploys, managing distributed failures and debugging cross-service problems far exceeds the operational capacity of a single dev.
2. **Product validation phase:** the domain will evolve significantly in the first 6-12 months. The boundaries between contexts that seem clear today could change as we better understand the real problem. Microservices prematurely crystallize boundaries.
3. **Limited budget:** microservices multiply infrastructure costs (multiple instances, networking, distributed observability, inter-service messaging).
4. **Operational complexity:** distributed transactions, sagas, eventual consistency between services, distributed tracing, coordinated deploys, internal API versioning.
5. **Consistency requirements:** several critical operations (activity-session matching, load calculation, suggestion application) strongly benefit from local transactions within a single process.

At the same time, we need the architecture to **not close the door** to extracting services in the future if a specific context needs to scale independently, be rewritten in another language (e.g. Intelligence in Python/ML-heavy), or be operated by a dedicated team when we grow.

## Forces in tension

- **Operational simplicity today** vs. **future scaling flexibility**.
- **Iteration speed** (evolutionary domain) vs. **boundary discipline** (preventing coupling).
- **Low operational costs** vs. **infrastructural separation of concerns**.
- **Strong transactional consistency** (within a context) vs. **independent deploy** (between contexts).
- **Easy onboarding of future devs with familiar stack** vs. **technological freedom per service**.

## Alternatives considered

### Alternative 1: Microservices from day one

Each bounded context as an independent service, with its own DB, API, deploy and potentially different stack. Communication via HTTP/gRPC + asynchronous messaging.

**Pros:**
- Independent scaling per context.
- Isolated failures.
- Technological freedom per service.
- Physical boundaries enforce discipline.

**Cons:**
- Operational complexity disproportionate for 1 dev.
- Multiplied infra cost (instances, load balancers, service mesh, distributed observability).
- Distributed transactions require sagas / complex orchestration.
- Refactoring the domain is much more expensive (implies changing APIs, coordinated deploys, migrations in multiple DBs).
- Debugging cross-service flows is notably harder.
- High risk of *distributed monolith*: coupled microservices without the benefits.

**Why not:** the operational and development cost far exceeds the benefits at this stage. It's a classic premature optimization that killed many early-stage projects.

### Alternative 2: Traditional monolith (without strong modularization)

A single solution with technical layers (Controllers, Services, Repositories) but without explicit separation by bounded context. Everything lives in the same code, all classes can reference all others.

**Pros:**
- Maximum initial speed.
- Extreme simplicity.
- Zero operational complexity.
- Everything in one transaction.

**Cons:**
- Uncontrolled coupling as the code grows.
- Domain boundaries erode; ubiquitous language gets contaminated.
- Impossible to extract a service in the future without deep rewrite.
- Painful refactors because everything touches everything.
- Doesn't leverage the DDD clarity we've already identified.

**Why not:** wastes the advantage of having done domain analysis. As the project grows (and the goal is that it grows), coupling becomes ungovernable. And without internal boundaries, the cost of eventually extracting services is enormous.

### Alternative 3: Modular monolith

A single deployable solution, but with strict separation by bounded context: each module has its own project, its own DB schema, its own use cases, and only communicates with other modules via (a) asynchronous integration events or (b) explicit public interfaces (contracts).

**Pros:**
- Monolith operational simplicity.
- Explicit internal boundaries, verifiable by code review and eventually by static analysis.
- Architectural discipline without distributed cost.
- Cheap domain refactoring while everything lives in the same process.
- Local transactions within a module (which is where we need them).
- Clear evolution path: extract module to microservice when there's justification.
- Aligns perfectly with strategic DDD.

**Cons:**
- Requires discipline to respect boundaries (there's no physical separation to enforce them).
- Scaling requires scaling the entire monolith (all modules go up and down together).
- A bug in one module can bring down the entire process.
- A shared database (though with separate schemas) has operational coupling (migrations, backups, connection limits).

### Alternative 4: Selective microservices (hybrid)

Main monolith + some services extracted from the start (e.g. AI worker as a separate Python service).

**Pros:**
- Technological flexibility where it matters (ML/AI in Python).
- Fewer services than pure microservices.

**Cons:**
- Operational complexity is still significant.
- Deciding what to extract today implies guessing what will need to scale in the future.
- For a single dev, it's still two different things to operate.

**Why not now:** defensible as future evolution (extract AI worker to Python when own models justify it), but not as a starting point.

## Decision

**Build the system as a modular monolith, with one .NET project per layer per bounded context, separate database schemas per context, and inter-module communication exclusively via asynchronous integration events or public contracts.**

The folder structure and the rule "modules don't reference each other in code" are documented in `docs/ARCHITECTURE.md` level 10 and in `CLAUDE.md`.

Async workers (SyncWorker, AnalysisWorker, AIWorker, etc.) are separate processes but share the same codebase and module libraries. They are "facets" of the same monolith with different entry points.

This decision is made recognizing that the trade-offs are correct for the current phase (1 dev, product validation, evolutionary domain), not as an ideological position. If conditions change significantly, this decision is revisited.

## Consequences

### Positive

- **Viable operability for a single dev.** One main process + pool of workers is manageable. A production incident is debuggable in one terminal.
- **Low initial infrastructure costs.** Less than 100 USD/month supports the full MVP.
- **Refactorable domain.** Moving code between modules is an IDE operation, not a migration project.
- **Local transactions where needed.** Applying an AI suggestion to the plan is atomic within the Coaching module.
- **Explicit internal boundaries.** The discipline of "modules don't reference each other directly" forces clean design and prepares the ground for extracting services when needed.
- **Simple onboarding.** A new dev clones a repo, starts Docker Compose, and has everything running in minutes.
- **Strong technical story for the portfolio.** "Modular monolith with DDD, outbox pattern, integrated events, ready for microservices" is a mature narrative that differentiates the project.

### Negative / Accepted trade-offs

- **Discipline dependent on the dev.** Module boundaries between modules are not enforced by network or process. An improper `using` between modules compiles. Mitigation: custom linting + code review + architecture tests (using NetArchTest or similar).
- **Uniform scaling.** If Intelligence (CPU-intensive due to LLMs) needs more resources, the entire application scales with it. Mitigation: workers are separate from the API, so AnalysisWorker can be scaled without touching the API.
- **Shared failure.** A memory bug in Coaching also brings down Identity. Mitigation: robust error handling, circuit breakers on external calls, supervision.
- **Shared database.** Though with separate schemas, a badly written migration or a heavy query affects other contexts. Mitigation: connection limits per module, query timeouts, isolation with RLS.
- **Temptation for shortcuts.** It's easy to "just this once" break the rule and do a cross-schema query or a cross-module `using`. Mitigation: clear rules in CLAUDE.md + architecture tests + review.

### Neutral

- The folder structure is more elaborate than a traditional monolith, but simpler than microservices.
- The outbox pattern and the event bus are necessary even in a modular monolith (for robust inter-module communication). This is overhead that a traditional monolith wouldn't have, but it's infrastructure paid once.

## When to revisit this decision

This decision is reviewed if any of these conditions are met:

1. **Team scale:** more than 5-8 developers actively working on the code. Coordination in a monolith becomes friction.
2. **Load scale:** a specific context (probably Intelligence) requires fundamentally different technology or scaling than the rest (e.g. we need Python + GPUs for own models).
3. **Stable boundaries:** the domain stabilizes and boundaries don't change for 6+ months. The "evolutionary domain" argument disappears.
4. **Availability problems:** a bug in one module frequently brings down the entire system, and the cost of this exceeds the operational cost of separating.
5. **Compliance requirements:** some enterprise client requires physical isolation of their data or processing.
6. **Operational maturity:** we have SRE, advanced distributed observability, and experience operating distributed systems.

The first candidate for extraction, when the time comes, will probably be **Intelligence** (for ML stack and LLM scaling reasons). The second reasonable candidate is **TrainingData** (for ingestion volume and need for dedicated throughput).

## References

- *Building Microservices* — Sam Newman (chapters on when NOT to do microservices).
- *Monolith to Microservices* — Sam Newman (the evolution path from monolith).
- *Implementing Domain-Driven Design* — Vaughn Vernon (bounded contexts as service boundaries).
- Modular Monolith: A Primer — Kamil Grzybek (reference article series).
- *.NET Microservices: Architecture for Containerized .NET Applications* — Microsoft (free book, chapter on modular monolith).
- `docs/ARCHITECTURE.md` level 4 (Architectural principles, particularly P8: "Designed for one dev today, extensible to a team of 10 tomorrow").
