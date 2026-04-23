# Specs

This directory contains the **specs** (specifications) of each system feature. They are the primary artifact in the development flow.

If you haven't already, start with:
- **ADR-0002** (`docs/adr/0002-sdd-sobre-ddd.md`) — methodological decision to combine SDD with DDD.
- **`docs/ARCHITECTURE.md`** — the domain model that these specs respect.
- **`CLAUDE.md`** — operational rules for the agent.

---

## What is a spec?

A spec describes a feature in terms of **domain behavior**, not technical implementation. It answers:

- What does this feature do in business language?
- Which bounded context does it belong to?
- Which aggregates does it touch?
- Which invariants does it preserve?
- Which scenarios verify it?
- Which events does it emit?

A spec does NOT describe:

- Specific files, classes, or methods (that goes in the implementation plan).
- Broad architectural decisions (that goes in ADRs).
- UI / wireframes (that goes in separate design docs).

---

## Why do specs exist?

Three concrete reasons:

1. **Context for the AI agent.** Claude Code generates coherent code when it has explicit context. The spec is that context, structured and reviewable.
2. **Source of truth about behavior.** When something doesn't work as expected, review the spec. If the spec said so, the code is wrong. If the spec didn't say it, the spec was incomplete.
3. **Tests derived automatically.** The Given/When/Then scenarios in the spec translate directly to acceptance tests. The declared invariants become unit tests of the aggregate.

---

## When to write a spec

### Write a spec

- New features that affect the domain.
- New use cases (commands or queries).
- New events (domain or integration).
- Significant changes to an existing aggregate.
- Any feature that will be delegated to the AI agent for implementation.

### Don't write a spec

- Trivial bugfixes (typo, off-by-one, bad parsing).
- Pure refactors without behavior change (those may require an ADR if architectural).
- Configuration, dependency, or infrastructure changes.
- UI/UX adjustments that don't affect the backend.
- Performance improvements that don't change the contract.

**Practical rule:** if you're going to ask the agent to "implement X" and X has domain logic, write the spec first.

---

## How to write a spec

### Step 1: Determine the bounded context

Before writing anything, ask yourself: **which bounded context does this feature belong to?**. If it touches several, decide which is the primary "owner" and what parts are resolved by events to the others.

If you can't answer clearly, the strategic model has a gap. Update `ARCHITECTURE.md` before continuing.

### Step 2: Copy the template

```
cp specs/_template.md specs/<context>/<number>-<kebab-name>.md
```

Numbering: sequential within the bounded context, with zero-padding (`001`, `002`, ..., `025`).

### Step 3: Fill in sections in order

Follow the template from top to bottom. Order matters because each section informs the next:

1. **Metadata** (initial status: `Draft`).
2. **Domain context** — bounded context, aggregate, business capabilities.
3. **User story** — in domain terms, not UI terms.
4. **Preserved invariants** — which ones from the aggregate, mark new ones if any.
5. **Acceptance scenarios** — minimum 3: happy path, error, edge case.
6. **Use cases involved** — commands and queries.
7. **Events emitted and consumed**.
8. **Authorization and multi-tenancy**.
9. **Non-functional considerations** only if applicable.
10. **Out of scope** — explicit.
11. **Dependencies**.
12. **Open questions** if any.
13. **Definition of Done**.
14. **Implementation notes** (optional).

### Step 4: Review the spec before implementing

Review checklist:

- [ ] Does it respect the bounded context boundaries?
- [ ] Does it use ubiquitous language (business names, not generic ones)?
- [ ] Are the invariants clear and verifiable?
- [ ] Are the scenarios complete (happy path + errors + edge cases)?
- [ ] Are the scenarios testable (well-defined Given/When/Then)?
- [ ] Is it clear which events are emitted and who consumes them?
- [ ] Does the out-of-scope prevent scope creep?
- [ ] Do open questions have an owner?

If any check fails, iterate before moving to implementation.

### Step 5: Change status to `Accepted` and delegate to the agent

Once reviewed, the spec moves to `Accepted`. Now you can:

1. Write the implementation plan (technical structure).
2. Delegate to the AI agent with: spec + plan + `CLAUDE.md` + `ARCHITECTURE.md`.
3. Review the generated code against the spec.

### Step 6: Update status to `Implemented` when the PR is merged

And if during implementation something was discovered that refines the model, update `ARCHITECTURE.md` or add an ADR **in the same PR or the next one**.

---

## Directory organization

```
specs/
├── README.md                                        ← this file
├── _template.md                                     ← template to copy
├── identity/
│   ├── 001-coach-registration.md
│   └── 002-athlete-invitation.md
├── athlete-profile/
│   ├── 001-create-athlete-profile.md
│   └── 002-update-training-zones.md
├── training-data/
│   ├── 001-connect-strava.md
│   ├── 002-ingest-activity.md
│   └── 003-disconnect-provider.md
├── coaching/
│   ├── 001-create-training-plan.md
│   ├── 002-adjust-training-week.md
│   └── 003-record-session-feedback.md
├── intelligence/
│   ├── 001-calculate-readiness.md
│   └── 002-generate-weekly-suggestions.md
└── communication/
    └── 001-send-plan-adjustment-notification.md
```

File names: `NNN-kebab-case-description.md`.

One subdirectory per bounded context, aligned with the backend modules.

---

## Spec statuses

| Status | Meaning |
|--------|---------|
| `Draft` | Being written. Don't implement yet. |
| `Review` | Complete but waiting for review (mine or another dev's). |
| `Accepted` | Reviewed and ready to implement. |
| `Implemented` | Feature merged to main. |
| `Deprecated` | Feature no longer applies; kept for history. |

---

## Relationship with other artifacts

| Artifact | What it captures | Rate of change |
|----------|-----------------|----------------|
| `docs/ARCHITECTURE.md` | Strategic model, structure, principles | Low (months) |
| `docs/adr/*.md` | Individual architectural decisions | Low, one per decision |
| `specs/**/*.md` | Behavior of concrete features | High (per feature) |
| Code in `src/` | Implementation that respects everything above | Very high |

**Influence flow:** ARCHITECTURE → ADRs → specs → code.

**Learning flow (feedback):** code → discovery → refined spec → sometimes ARCHITECTURE updated or new ADR.

---

## How to work with Claude Code using specs

The typical flow is:

```
1. Write spec in specs/<context>/NNN-name.md, status Draft.
2. Self-review. Move to Review.
3. If there's another reviewer, wait. Move to Accepted when approved.
4. Write implementation plan (can be in the same PR as the spec).
5. Open session with Claude Code:
   - Reference: @specs/<context>/NNN-name.md
   - Reference: @CLAUDE.md
   - Instruction: "Implement the spec following the plan. Start with aggregate tests. Show diffs before applying large changes."
6. Review generated code:
   - Does it fulfill the scenarios?
   - Does it respect invariants?
   - Does it follow the rules in CLAUDE.md?
7. Tests pass, internal code review → merge.
8. Update spec status to Implemented.
```

**Anti-pattern to avoid:** opening Claude Code and asking "implement the plan adjustment feature" without a spec. The agent will invent structure, and in each session it will invent a different one.

---

## Spec granularity

One spec ≠ a small user story, but also not a huge epic.

**Too large:** "Training planning system". This is a module, not a spec. Decompose it into multiple specs.

**Too small:** "Validate that the plan title is not empty". That's an invariant, part of a larger spec.

**Right size:** "Adjust training week". A cohesive feature, with one or two commands, 3-5 acceptance scenarios, a clear user story.

Rule: if the spec fits in ~2-4 hours of total implementation, it's probably the right size.

---

## Directory maintenance

- Implemented specs **are not deleted**. History matters.
- If a feature changes in a breaking way, create a new spec (e.g. `002-adjust-training-week-v2.md`) and mark the previous one as `Deprecated` with a link to the new one.
- Every quarter, review `Draft` specs that never advanced and decide: resume, promote, or discard.

---

## Process for proposing changes to the template

If during use you detect that the template is missing something or has something unnecessary:

1. Open an issue describing the friction.
2. Discuss in a PR with a proposed change to `_template.md`.
3. Once accepted, apply retroactively to future specs (don't rewrite existing ones unless necessary).

---

*The template is a starting point, not a cage. If a specific spec needs additional or different sections, that's valid as long as the fundamentals (domain context, invariants, scenarios) are present.*
