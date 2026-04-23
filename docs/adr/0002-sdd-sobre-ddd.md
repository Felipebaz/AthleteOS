# ADR-0002: Spec-Driven Development on top of Domain-Driven Design

- **Date:** 2026-04-22
- **Status:** Accepted
- **Deciders:** Felipe
- **Related technical context:** `docs/ARCHITECTURE.md` level 4 (Architectural principles, P1: DDD as north star), `CLAUDE.md` (operational rules for AI agents), ADR-0001 (Modular monolith).

## Context and problem

The project will be built with intensive AI agent assistance, specifically Claude Code, as a central tool in the development flow. This raises a methodological question that neither DDD nor any traditional agile process resolves: **how do you delegate implementation work to an AI agent while preserving architectural quality and domain coherence?**

The typical options fail in identifiable ways:

1. **Naive agent use** (conversational prompts like "implement the plan adjustment feature"): produces functional but inconsistent code, with a tendency toward transaction scripts, silent violations of architectural boundaries, and inventing structures that already exist under different names.
2. **Pure DDD without a development methodology**: excellent for strategic modeling, but doesn't define how to convert the model into code in a repeatable way. Without a process, the agent has no mechanisms to respect the model.
3. **Traditional agile process** (user stories in backlog, direct implementation): designed for humans with accumulated context, not for agents that need explicit context in each interaction.

An emerging methodology exists, **Spec-Driven Development (SDD)**, formalized in recent years (GitHub Spec Kit, Amazon Kiro, Anthropic initiatives) specifically designed for AI-assisted development. The core principle: **the specification is the primary artifact, the code is a generated expression of the spec**.

The problem: naively applied SDD tends to produce feature-oriented code without a rich domain model. Typical SDD specs describe external behavior without internal constraints, which invites the agent to choose expedient but incorrect structures.

**The question to resolve:** how to combine DDD (for structure and modeling) with SDD (for development flow with AI) so they reinforce each other rather than compete.

## Forces in tension

- **Development speed with AI agent** vs. **architectural and domain discipline**.
- **Flexibility of conversational prompts** vs. **reproducibility and auditability of the process**.
- **Agent autonomy** vs. **human control over modeling decisions**.
- **Specs detailed to the point of prescribing code** vs. **specs at the behavior level that leave room for the agent**.
- **Formally documented process** vs. **agility for a single dev**.

## Alternatives considered

### Alternative 1: Direct conversational development (no-SDD)

Using the agent as a conversational pair programmer. Each feature is discussed in chat, iterated, and implemented without a prior formal spec.

**Pros:**
- Maximum perceived speed.
- Total flexibility.
- Zero methodological overhead.

**Cons:**
- Impossible to audit why the code is the way it is.
- Decisions are lost in chat history.
- Agent generates inconsistent code between sessions.
- Without an explicit contract, the agent invents structures.
- Massive refactors later when discovering inconsistencies.
- High risk of transaction scripts disguised as DDD.

**Why not:** the cost of inconsistency and rework exceeds the perceived speed. Also loses the auditability value needed by a project with commercial aspirations and portfolio purposes.

### Alternative 2: Pure DDD with manual implementation

Complete DDD modeling, using the agent only as an occasional typing assistant. The dev writes all code by hand.

**Pros:**
- Total control over every line.
- Highly consistent code with the model.
- No risk of agent inventions.

**Cons:**
- Wastes the agent's capability.
- Development speed of a single dev without intensive assistance.
- Doesn't scale to the project's ambitions (MVP in 6 months with 1 dev).
- Volume of boilerplate code that could be generated automatically is written by hand.

**Why not:** in 2026, with capable agents, forgoing intensive assistance is self-limitation. We need to learn to collaborate with the agent, not avoid it.

### Alternative 3: Pure SDD without domain foundation

Adopting SDD as-is (feature-oriented specs, implementation plans, generation) without a DDD framework behind it.

**Pros:**
- Clear and repeatable process.
- High speed with agent.
- Spec → code traceability.

**Cons:**
- Without bounded contexts, the agent mixes responsibilities.
- Without aggregates, there are no invariants; the code ends up being procedural CRUD.
- Without ubiquitous language, names drift to generic (UserService, DataManager).
- Product evolution becomes painful because the model doesn't reflect the business.
- Long-term, produces the same coupling as a non-modular monolith.

**Why not:** the endurance coaching domain is complex (physiological invariants, periodization, training load). A weak model produces software that doesn't scale intellectually with the problem.

### Alternative 4: Strategic DDD + Tactical SDD (disciplined combination)

Maintain DDD as the modeling and structure framework (bounded contexts, aggregates, events, ubiquitous language), and use SDD as the development methodology within that framework. Each spec is written in DDD's ubiquitous language, respects the bounded contexts, and operates on already-identified aggregates.

**Pros:**
- Leverages both methodologies at the levels where they shine.
- Agent has explicit context (the DDD model) and a clear process (the SDD flow).
- Specs are auditable and traceable.
- Domain model is refined in a controlled way as learning occurs.
- Scalable to a team: other devs (human or agents) read specs and produce consistent code.
- Clearly separates modeling decisions (slow, deep, made by the dev with judgment) from implementation decisions (fast, procedural, assistable by agent).

**Cons:**
- Requires discipline to write specs before requesting code.
- Initial overhead of writing the spec before implementing.
- Needs infrastructure: `specs/` directory, templates, review.
- Learning curve for the dev on how to write good specs.

### Alternative 5: BDD (Behavior-Driven Development) as equivalent

Using BDD with Gherkin (Given/When/Then) as feature spec.

**Pros:**
- Clear and executable acceptance language.
- Tool maturity (SpecFlow in .NET, Cucumber).

**Cons:**
- BDD focuses on acceptance scenarios, doesn't cover all aspects of a spec (domain invariants, events emitted, use cases, out-of-scope).
- It's complementary, not a substitute.
- BDD doesn't define a collaboration process with AI agents.

**Why not as sole solution:** BDD is valuable but incomplete. Given/When/Then scenarios are incorporated *within* the SDD spec as an acceptance scenarios section, but don't replace the full spec.

## Decision

**Adopt Domain-Driven Design as the strategic framework (modeling, structure, language) and Spec-Driven Development as the tactical methodology (development flow with AI assistance).**

Operationally, this means:

1. **Strategic modeling lives in `docs/ARCHITECTURE.md` and is refined with care.** Bounded contexts, aggregates, events, invariants, ubiquitous language: all deliberately modeled. Changes to the model require reflection and eventual ADR.

2. **Each feature is developed following the adapted SDD flow:**
   - Write feature spec in `specs/<bounded-context>/<number>-<name>.md`.
   - The spec uses DDD ubiquitous language, declares bounded context and aggregates involved, lists preserved invariants, defines Given/When/Then scenarios, enumerates events emitted.
   - Write implementation plan (technical structure derived from the spec).
   - Delegate implementation to the agent with spec + plan + `CLAUDE.md` as context.
   - Review generated code against spec.
   - If something refines the model is discovered, update `ARCHITECTURE.md` or open an ADR.

3. **Specs are versioned and reviewable artifacts.** They are committed as code. They're refined with PRs. They're the source of truth about what each feature does.

4. **The AI agent operates within the framework, not outside it.** It doesn't invent bounded contexts, doesn't redefine aggregates, doesn't change the ubiquitous language without explicit consent. `CLAUDE.md` codifies these restrictions.

5. **Acceptance scenarios (Given/When/Then) are a mandatory part of the spec** and translate directly into integration/acceptance tests.

## Consequences

### Positive

- **High speed with high quality.** The agent is productive because it has rich context; the code is consistent because the framework is clear.
- **Full auditability.** Each feature has spec + plan + code + tests. The reasoning can be reconstructed months later.
- **Clear onboarding.** A new dev (or new agent) reads `ARCHITECTURE.md`, `CLAUDE.md`, and the existing specs, and knows how to contribute.
- **Controlled domain refinement.** Discoveries during implementation are capitalized by refining `ARCHITECTURE.md`, not left in the code.
- **Differentiating portfolio.** "Built with SDD+DDD, rigorous spec → plan → assisted implementation flow" is a mature engineering story.
- **Tests derived from specs.** Given/When/Then scenarios become acceptance tests automatically.
- **Team scalability.** The process doesn't depend on the original dev; it's repeatable by anyone.

### Negative / Accepted trade-offs

- **Overhead for small features.** Writing a spec for trivial changes (typo fix, cosmetic adjustment) is excessive. Mitigation: define which changes warrant a spec and which don't (guide in the `specs/` directory README).
- **Discipline dependent on the dev.** Nothing forces specs to be written before code. Mitigation: explicit cultural rule, code review rejects new feature PRs without an associated spec.
- **Learning curve.** Writing good specs is a skill. The first ones will be imperfect. Mitigation: iterate on the template, review existing specs before writing new ones.
- **Risk of bikeshedding on specs.** Spending too much time perfecting the spec instead of implementing. Mitigation: spec timebox (max 1-2 hours for normal features).
- **Specs can become desynchronized from code.** If code is modified without updating the spec, the spec loses value. Mitigation: define that the spec is the source of truth; important changes are made in the spec first.

### Neutral

- The `specs/` directory grows over time. It's organized by bounded context to maintain navigability.
- Some specs may evolve into multiple versions (v1, v2) if the feature is remade. History is preserved.
- Not all features have specs. Purely technical changes (refactor, performance improvement, dependency update) don't require specs though they may require an ADR if architectural.

## When to revisit this decision

This decision is reviewed if any of these conditions are met:

1. **The overhead of writing specs exceeds the value.** If weeks pass where the dev avoids tasks because "writing the spec is too much work", the process is miscalibrated.
2. **Specs are not respected.** If the generated code systematically diverges from what was specified and no one corrects it, the framework isn't working.
3. **The agent can't work with the level of detail in the specs.** If specs aren't sufficient for the agent to generate correct code, the template needs rethinking.
4. **A clearly superior methodology emerges.** If something appears that better combines DDD and AI-assisted development, consider migrating.
5. **Team grows significantly.** With 10+ devs, the process may require additional formalization (spec review by another dev, for example).

## References

- GitHub Spec Kit — https://github.com/github/spec-kit
- Amazon Kiro (Spec-Driven Development) — AWS public documentation.
- Anthropic Skills — collaboration patterns with Claude in development.
- *Implementing Domain-Driven Design* — Vaughn Vernon.
- *Domain-Driven Design Distilled* — Vaughn Vernon.
- *Specification by Example* — Gojko Adzic (on BDD and executable specs).
- `docs/ARCHITECTURE.md` levels 4 (principles) and 5 (bounded contexts).
- `CLAUDE.md` sections "Non-negotiable architecture rules" and "When asked to implement something new".
- ADR-0001 (Modular monolith) — establishes the operational simplicity context that SDD+DDD adapts to.
