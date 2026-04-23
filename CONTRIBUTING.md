# How to contribute to AthleteOS

Thank you for considering contributing. This document describes the process and conventions. If you're an AI agent (Claude Code), read `CLAUDE.md` first.

## Before you start

1. Read `README.md` to understand the project.
2. Read `docs/ARCHITECTURE.md` (at least levels 1-5) to understand the model.
3. Review `docs/adr/` to see decisions already made.
4. Review `specs/` to see pending or in-progress feature specs.

## Workflow

### For a trivial bugfix

1. Create branch `fix/short-description` from `develop`.
2. Make the fix.
3. Add a test that prevents regression.
4. Commit with Conventional Commits in English.
5. PR to `develop`.

### For a new feature

1. **Write a spec** in `specs/<bounded-context>/NNN-name.md` using the template (`specs/_template.md`).
2. Mark the spec as `Review` and request review.
3. Once accepted (status `Accepted`), write an implementation plan.
4. Create branch `feature/<slug>` from `develop`.
5. Implement following the plan and the rules in `CLAUDE.md`.
6. Tests covering all acceptance scenarios from the spec.
7. PR to `develop` referencing the spec.
8. When merged, update the spec status to `Implemented`.

### For an architectural decision

1. Copy `docs/adr/000-template.md` to `docs/adr/NNNN-title.md`.
2. Fill in: context, alternatives, decision, consequences.
3. PR to `develop` for discussion.
4. When approved, update status to `Accepted` and add to the index in `docs/adr/README.md`.

## Commit conventions

We follow **Conventional Commits in English**. Format:

```
<type>(<scope>): <short description in imperative>

<optional body: what and why>

<optional footer: refs, breaking changes>
```

### Valid types

| Type | When |
|------|------|
| `feat` | New feature |
| `fix` | Bugfix |
| `refactor` | Code change without behavior change |
| `test` | Add or fix tests |
| `docs` | Documentation changes |
| `chore` | Maintenance tasks (deps, config) |
| `perf` | Performance improvement |
| `build` | Build system changes |
| `ci` | CI/CD changes |
| `style` | Formatting, no logic change |

### Scope

Corresponds to the affected module or area: `coaching`, `intelligence`, `training-data`, `identity`, `api`, `web-coach`, `pwa-athlete`, `infra`, `ci`, `adr`, `spec`, `deps`, etc.

### Examples

```
feat(coaching): add AdjustTrainingWeekCommand with invariant validation

Implements RF-PLAN-04 according to spec in specs/coaching/002-adjust-training-week.md.
Respects load progression and recovery invariants between intensities.
Emits TrainingPlanAdjustedIntegrationEvent.

Closes #42
```

```
fix(training-data): fix activity deduplication between Strava and Garmin
```

```
docs(adr): add ADR-0005 on outbox pattern
```

```
chore(deps): update MediatR to 12.2.0
```

## Branches

We follow adapted Gitflow:

- `main` — production. Only accepts merges from `release/*` or `hotfix/*`.
- `develop` — integration. Accepts merges from `feature/*` and `fix/*`.
- `feature/<slug>` — new features.
- `fix/<slug>` — non-urgent bugfixes.
- `hotfix/<slug>` — urgent patches from `main`.
- `release/<version>` — pre-production stabilization.

**Never commit directly to `main` or `develop`.**

## Code review

Every PR needs at least one approval. Criteria:

- Does it solve what it claims to solve?
- Do the tests cover the important cases?
- Does it respect the architecture (see `docs/ARCHITECTURE.md` level 4)?
- Does it follow the conventions (this document + `.editorconfig`)?
- Does it not introduce technical debt without justification?
- Is documentation updated if applicable?

## Tests

- **Unit tests:** mandatory in Domain and Application. Target coverage 80%+.
- **Integration tests:** for repositories, event handlers, external integrations.
- **E2E tests:** for critical user flows.
- **Rule:** if code is added without tests, justify why in the PR.

## Dev environment setup

See `README.md` section "Local setup".

## Questions

If something is unclear:

1. Review the documentation (`docs/`, `CLAUDE.md`, `specs/`).
2. If you can't find an answer, open an issue with the `question` label.

## Code of conduct

This project follows a basic code of conduct: respectful communication, constructive criticism, no personal attacks. Technical discussions with arguments, not opinions. Disagreements are resolved with data or experiments, not with authority.

---

*This document evolves. If you find friction with any process, propose changes via PR.*
