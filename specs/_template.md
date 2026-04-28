# SPEC NNN: Feature title

> Spec template following the SDD flow on top of DDD (see ADR-0002).
> A spec describes **behavior** in the domain's ubiquitous language, not technical structure.

---

## Metadata

- **ID:** SPEC-{context}-{number}
- **Status:** Draft | Review | Accepted | Implemented | Deprecated
- **Author:** Felipe
- **Created:** YYYY-MM-DD
- **Last updated:** YYYY-MM-DD
- **Related issues/PRs:** #NN

---

## Domain context

- **Bounded context:** (iam | training-data | coaching)
- **Affected aggregate(s):** name of the aggregate root that is created or modified
- **Business capabilities covered:** references to `docs/ARCHITECTURE.md` level 2 (e.g. RF-PLAN-04)
- **Related end-to-end flow:** if applicable, reference to ARCHITECTURE level 12

---

## User story

Classic format but in domain terms:

> As a **[role: coach / athlete / admin]**, I want to **[capability in business language]**, so that **[concrete benefit]**.

Example:
> As a coach, I want to adjust the sessions of a week in an athlete's plan, to respond to how they felt that week and adapt the load for the coming weeks.

---

## Preserved invariants

List the invariants of the aggregate(s) that this feature **must respect**. Reference invariants already documented in `ARCHITECTURE.md` level 11 and/or declare new invariants if this feature introduces them.

Example:
- Weekly load increase ≤ 10% without an explicit coach flag (standard physiological rule).
- No 2 consecutive high-intensity sessions without ≥24h recovery.
- Already-completed sessions are not modified; any change creates a new version of the plan.

If this feature introduces new invariants to the domain, mark them clearly with **[NEW]** and make sure to update `ARCHITECTURE.md` in the same PR.

---

## Acceptance scenarios

Given/When/Then format. These scenarios translate directly to acceptance tests. **Each spec must have at least 3 scenarios:** the happy path, an expected error case, and an edge case.

### E1: [short scenario name, happy path]

**Given** [relevant initial system state]
**And** [additional condition if applicable]
**When** [action that triggers the use case]
**Then** [expected result]
**And** [secondary result if applicable]

### E2: [error/invariant violation scenario]

...

### E3: [edge case or boundary case]

...

### E4: [optional, more scenarios if the feature warrants it]

---

## Use cases / operations involved

List the commands and queries that implement this feature. For each one:

### Command: CommandName

- **Input:**
  - `Field1` (type): description.
  - `Field2` (type): description.
- **Output:** `Result<ReturnType>`
- **Preconditions:** what must be true before execution.
- **Postconditions:** what is true after successful execution.
- **Expected errors:** what errors can be returned (as `Result.Failure`).

### Query: QueryName

- **Input:** parameters.
- **Output:** response DTO.
- **Considerations:** authorization, caching, etc.

---

## Events emitted

### Domain events (in-process, within the bounded context)

- `EventName`: when it's emitted, what data it carries, who consumes it internally.

### Integration events (cross-context, via outbox)

- `IntegrationEventName`: when it's emitted, what data it carries, which contexts consume it.

---

## Events consumed

If this feature reacts to events from other contexts, list them here:

- `EventName` emitted by `OtherContext`: how this feature reacts when it receives it.

---

## Authorization and multi-tenancy

- **Who can execute:** allowed role(s).
- **Scope:** what data can be seen/modified. Tenant isolation respect.
- **Special rules:** mentions of `CanManageAthlete`, `CanViewPlan`, etc.

---

## Non-functional considerations

Only if relevant to this feature. Don't copy everything from `ARCHITECTURE.md`.

- **Performance:** specific SLA if it differs from the default.
- **Security:** sensitive data involved, special handling.
- **Privacy:** consents, retention.
- **Observability:** specific metrics or logs that this feature must expose.

---

## Out of scope

Explicit list of what this feature **does not** do. Prevents scope creep and makes the scope clear.

Example:
- AI suggestions for the adjustment (that's a separate feature from the Intelligence context).
- Notification to the athlete of the change (handled by Communication listening to the event).
- Dashboard UI (separate frontend feature).

---

## Dependencies

- **On other features/specs:** if this depends on others being implemented first.
- **On architectural decisions (ADRs):** if any specific ADR applies.
- **On external services:** if it requires a new integration or consumes an external API.

---

## Open questions

If there are unresolved aspects of the spec, declare them here so they're not forgotten. Before marking the spec as `Accepted`, these questions must be answered.

- [ ] What happens in case X that isn't covered above?
- [ ] Is a data migration of existing data required?

---

## Definition of Done

- [ ] Unit tests of the aggregate(s) cover declared invariants.
- [ ] Acceptance tests cover all scenarios E1..EN.
- [ ] Integration tests validate persistence and events.
- [ ] API endpoints exposed if applicable, with correct authorization.
- [ ] Observability instrumented (structured logs, metrics if applicable).
- [ ] API documentation updated (OpenAPI).
- [ ] `ARCHITECTURE.md` updated if the model was refined.
- [ ] ADR created if there was a new architectural decision.
- [ ] Code review approved.

---

## Implementation notes

Free space for considerations that the implementer (human or agent) should keep in mind, without being part of the spec's contract.

Example:
- "Consider using the specification pattern for load progression validation because it's reused in other features."
- "Library X has a known bug in method Y, avoid using it."

---

## Change history

| Date | Change | Reason |
|------|--------|--------|
| YYYY-MM-DD | Creation | Initial draft |
