# SPEC 001: Create training plan

---

## Metadata

- **ID:** SPEC-COACHING-001
- **Status:** Accepted
- **Author:** Felipe
- **Created:** 2026-04-22
- **Last updated:** 2026-04-22
- **Related issues/PRs:** —

---

## Domain context

- **Bounded context:** Coaching
- **Affected aggregate(s):** `TrainingPlan` (creation)
- **Business capabilities covered:** RF-PLAN-01, RF-PLAN-02, RF-PLAN-03 (see `docs/ARCHITECTURE.md` level 2.4)
- **Related end-to-end flow:** precondition for the flows in ARCHITECTURE level 12

---

## User story

As a coach, I want to create a training plan for one of my athletes, with a date range, a goal, and a weekly session structure, so that the athlete knows what to train each day and I can track their progress.

---

## Preserved invariants

All these `TrainingPlan` aggregate invariants are validated when creating the plan:

1. **Valid date range:** `startDate < endDate` and `endDate > today - 7 days` (plans entirely in the past are not created).
2. **Minimum duration of 1 week and maximum of 52 weeks.** Longer plans are divided into separate mesocycles.
3. **Complete weekly coverage:** `TrainingWeek`s cover the entire `[startDate, endDate]` range without gaps or overlaps.
4. **Each `TrainingWeek` starts on Monday and ends on Sunday** (domain convention; see implementation note).
5. **At least one associated goal (`Goal`)** when created (reference to the athlete's active goal).
6. **The creating coach is the plan owner:** `CoachId` of the plan == authenticated coach executing the command.
7. **The athlete belongs to the coach:** verified against the athlete's `AthleteProfile`, which must have the coach as the active coach.
8. **No two simultaneous active plans for the same athlete** in the same period (the previous one can be archived).
9. **Load progression ≤ 10% weekly** (physiological invariant; applies when the plan has sessions with calculated load). If initialized with empty/placeholder sessions, this invariant applies when they're populated, not at creation.
10. **Each `PlannedSession` has a valid type** (running, cycling, swimming, strength, rest, cross-training) and duration > 0 (except rest/off).

Invariants 1-8 are verified at creation. Invariants 9-10 are verified as sessions are added (can be in the same command or subsequent commands).

---

## Acceptance scenarios

### E1: Successful creation of plan with minimum structure

**Given** an authenticated coach with ID `coach-uuid-A`
**And** an athlete `athlete-uuid-X` whose active coach is `coach-uuid-A`
**And** the athlete has an active goal `goal-uuid-1` (half marathon in 12 weeks)
**When** the coach executes `CreateTrainingPlanCommand` with:
  - `athleteId = athlete-uuid-X`
  - `goalId = goal-uuid-1`
  - `startDate = 2026-05-04` (Monday)
  - `endDate = 2026-07-26` (Sunday, 12 weeks later)
  - `title = "Half marathon — spring 2026"`
  - No initial sessions (to be filled in later)
**Then** a `TrainingPlan` is created with a new ID
**And** the plan has 12 empty `TrainingWeek`s covering the range
**And** the plan is in `Active` state
**And** it's persisted in the DB
**And** `TrainingPlanCreatedDomainEvent` is emitted (within Coaching)
**And** `TrainingPlanCreatedIntegrationEvent` is emitted (for Communication and Intelligence)
**And** the command returns `Result<TrainingPlanId>.Success` with the created plan ID

### E2: Rejection because athlete doesn't belong to coach

**Given** an authenticated coach `coach-uuid-A`
**And** an athlete `athlete-uuid-Z` whose active coach is `coach-uuid-B` (another coach)
**When** coach `coach-uuid-A` executes `CreateTrainingPlanCommand` for `athlete-uuid-Z`
**Then** the command returns `Result.Failure(CoachingErrors.AthleteNotOwnedByCoach)`
**And** no plan is created
**And** no events are emitted
**And** the attempt is recorded in audit log (possible cross-tenant access attempt)

### E3: Rejection due to invalid dates

**Given** an authenticated coach with a valid athlete
**When** the coach tries to create a plan with `startDate = 2026-05-04` and `endDate = 2026-05-03` (endDate before startDate)
**Then** the command returns `Result.Failure(CoachingErrors.InvalidDateRange)`
**And** no plan is created

### E4: Rejection due to duration out of range

**Given** an authenticated coach with a valid athlete
**When** the coach tries to create a plan with `endDate - startDate = 380 days` (more than 52 weeks)
**Then** the command returns `Result.Failure(CoachingErrors.PlanDurationExceedsMaximum)`
**And** no plan is created

**Symmetric scenario:** duration less than 7 days → `Result.Failure(CoachingErrors.PlanDurationBelowMinimum)`.

### E5: Rejection due to overlap with existing active plan

**Given** a coach `coach-uuid-A` with athlete `athlete-uuid-X`
**And** an active plan `plan-uuid-1` exists for `athlete-uuid-X` from `2026-05-01` to `2026-07-31`
**When** the coach tries to create another plan for the same athlete with `startDate = 2026-06-01`
**Then** the command returns `Result.Failure(CoachingErrors.ActivePlanAlreadyExists)`
**And** the second plan is not created
**And** the message includes the `TrainingPlanId` of the existing active plan (to facilitate UX of "archive the previous one")

### E6: Edge case — dates automatically adjusted to week boundaries

**Given** an authenticated coach with a valid athlete
**When** the coach creates a plan with `startDate = 2026-05-06` (Wednesday) and `endDate = 2026-06-02` (Tuesday)
**Then** the command automatically adjusts the dates to week boundaries
**And** the plan ends up with `startDate = 2026-05-04` (previous Monday) and `endDate = 2026-06-07` (next Sunday)
**And** the warning "Dates adjusted to week boundaries" is returned in `Result.Success` (not an error, it's information)

**Note:** the auto-adjustment behavior is a UX decision. Alternative: reject with error and ask the user to correct. See "Open questions".

---

## Use cases / operations involved

### Command: CreateTrainingPlanCommand

- **Input:**
  - `CoachId` (implicit, from authenticated user).
  - `AthleteId` (Guid): athlete for whom the plan is created.
  - `GoalId` (Guid): athlete's goal that the plan targets.
  - `StartDate` (DateOnly): start date.
  - `EndDate` (DateOnly): end date.
  - `Title` (string, 3-100 chars): plan title.
  - `Description` (string?, max 500 chars): optional description.
  - `AutoAdjustWeekBoundaries` (bool, default true): whether to adjust dates to week boundaries.
- **Output:** `Result<CreateTrainingPlanResult>` where `CreateTrainingPlanResult` contains:
  - `PlanId` (TrainingPlanId).
  - `AdjustedStartDate`, `AdjustedEndDate` (if adjusted).
  - `WeeksCreated` (int).
  - `Warnings` (list<string>): non-blocking notices.
- **Preconditions:** authenticated coach, athlete exists and belongs to the coach, goal exists and belongs to the athlete.
- **Postconditions:** plan created in `Active` state, empty weeks created, events emitted.
- **Expected errors:**
  - `CoachingErrors.AthleteNotOwnedByCoach`
  - `CoachingErrors.AthleteNotFound`
  - `CoachingErrors.GoalNotFound` / `GoalNotAssociatedWithAthlete`
  - `CoachingErrors.InvalidDateRange`
  - `CoachingErrors.PlanDurationExceedsMaximum`
  - `CoachingErrors.PlanDurationBelowMinimum`
  - `CoachingErrors.ActivePlanAlreadyExists`

### Query: GetTrainingPlanByIdQuery

**(Related but separate spec — not part of this feature, only mentioned for completeness.)**

---

## Events emitted

### Domain events (in-process, within Coaching)

- **`TrainingPlanCreatedDomainEvent`**
  - Emitted: when the plan is created.
  - Data: `TrainingPlanId`, `AthleteId`, `CoachId`, `StartDate`, `EndDate`.
  - Internal consumers: none currently. Extension point for future logic within Coaching (e.g. initialization of coach-specific library).

### Integration events (cross-context, via outbox)

- **`TrainingPlanCreatedIntegrationEvent`**
  - Emitted: when the creation transaction completes.
  - Data: `TrainingPlanId`, `AthleteId`, `CoachId`, `TenantId`, `StartDate`, `EndDate`, `Title`, `CreatedAt`.
  - Consumers:
    - **Communication:** to generate notification to athlete ("your coach created a new plan for you").
    - **Intelligence:** to initialize `CoachStyleProfile` if it's the coach's first plan and/or pre-load analytical context.

---

## Events consumed

This feature doesn't consume events; it's the entry point of the flow.

---

## Authorization and multi-tenancy

- **Who can execute:** users with `Coach` role, authenticated.
- **Scope:** the coach can only create plans for athletes that belong to them (verified against `AthleteProfile.CoachId`).
- **Policy:** `CanManageAthleteTrainingPlan` with requirement that validates `CoachId == authenticatedUser.CoachId && athlete.CoachId == authenticatedUser.CoachId`.
- **Tenant isolation:** the plan is created with the coach's `TenantId`. PostgreSQL RLS blocks any attempt to read/write outside the tenant.
- **Audit log:** creation is recorded with `CoachId`, `AthleteId`, `PlanId`, timestamp, request IP.

---

## Non-functional considerations

- **Performance:** synchronous creation with SLA of p95 < 400ms. The 12-52 empty weeks are fast inserts (< 100ms even for a long plan).
- **Transactionality:** plan creation + week creation + outbox write → a single transaction. If anything fails, full rollback.
- **Observability:** structured log at creation with `{ event: "TrainingPlanCreated", coachId, athleteId, planId, duration }`. Metric `training_plans_created_total` incremented.
- **Idempotency:** not required at the command level (it's a deliberate coach action). But if the client retries due to timeout, the "active plan exists" check acts as protection.

---

## Out of scope

This spec does **not** cover:

- Populating the plan with sessions (that's a separate spec, probably `002-populate-plan-with-sessions.md`).
- AI plan generation (`generate-training-plan-draft.md`, Intelligence context).
- Plan editing post-creation (`003-adjust-training-week.md` and similar).
- Plan archiving or deletion.
- Dashboard UI for creation.
- Actual notification to the athlete (only the event is emitted; Communication decides how to notify).
- Coach reusable templates (future feature, will use this spec as a base).

---

## Dependencies

- **Prior specs:** none. This is a foundational Phase 1 spec.
- **Applicable ADRs:**
  - ADR-0001 (Modular monolith) — module structure.
  - ADR-0002 (SDD on top of DDD) — methodology.
- **External services:** none. Only local DB and outbox.
- **Affected backend modules:** `Coaching` (primary), `AthleteProfile` (via public API to verify ownership), `Identity` (for authentication).

---

## Open questions

- [x] Is the auto-adjustment of dates to week boundaries silent or does it ask for confirmation? → **Resolved:** silent with warning in the Result. The UI can decide whether to show the warning or not.
- [ ] What happens if the athlete's goal is further out than the plan (e.g. goal in 6 months, 4-week plan)? Is creating the plan allowed? → **Current hypothesis:** yes, it's valid to have a short plan within a long horizon. Confirm with coaches in validation.
- [ ] Is creating a plan in the past allowed? E.g. to record an already-executed cycle. → **Current hypothesis:** no in MVP, "import historical plan" feature is separate.

---

## Definition of Done

- [ ] Unit tests of the `TrainingPlan` aggregate cover all invariants 1-8.
- [ ] Unit test of the factory method `TrainingPlan.Create(...)` with all scenarios.
- [ ] Unit tests of the handler `CreateTrainingPlanCommandHandler` with mocks.
- [ ] Integration tests with Testcontainers: creates plan, verifies persistence + outbox + events.
- [ ] Acceptance tests E1-E6 implemented as integration tests.
- [ ] Endpoint `POST /api/v1/training-plans` exposed with authentication and authorization.
- [ ] Request/response DTOs with FluentValidation validation.
- [ ] DB migration applied (table `coaching.training_plans`, `coaching.training_weeks`, `coaching.outbox_messages`).
- [ ] OpenAPI updated.
- [ ] Structured logs implemented.
- [ ] Metric `training_plans_created_total` exposed.
- [ ] Audit log records creation.
- [ ] Code review approved.
- [ ] This spec status updated to `Implemented`.

---

## Implementation notes

- **Monday-Sunday week convention:** standard in the European/Latin American sports domain. Users with Sunday-Saturday calendar (US-centric) don't apply to the initial target market. If expanding to US, reassess.
- **`TrainingPlan.Create(...)` as static factory method in the aggregate**, not a public constructor. Allows encapsulating creation logic including generating empty weeks.
- **`TrainingWeek` as entity within the aggregate, not a separate aggregate.** Its lifecycle depends on the plan.
- **"Active plan exists" validation requires a repository query before creating.** Pattern: in handler, not in the aggregate constructor (the invariant depends on external context).
- **Integration event emission via outbox:** within the same DbContext transaction that persists the aggregate. See ADR-0005 (to be written) on outbox pattern.
- **Reference to `GoalId` from AthleteProfile context:** stored as Guid, not as a DB FK (cross-schema). Existence verification via `IAthleteProfileApi.GoalExistsAsync(goalId, athleteId)` in the handler.
- **Performance considered:** creating 52 empty weeks = 52 inserts. Use EF `AddRange` for batch insert, not a loop with SaveChanges.

---

## Change history

| Date | Change | Reason |
|------|--------|--------|
| 2026-04-22 | Creation | Initial draft (Felipe) |
| 2026-04-22 | Resolved open question on auto-adjustment | UX decision made |
| 2026-04-22 | Status → Accepted | Review completed |
