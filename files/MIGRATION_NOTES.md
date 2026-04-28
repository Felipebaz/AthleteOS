# Migration Notes — .NET to TypeScript

This document describes the one-time migration from the original .NET 8 setup to the new TypeScript stack. After migration is complete, this document remains as a historical record.

> See `docs/adr/0003-stack-change-to-typescript.md` for the rationale.

## What gets removed

These files and directories no longer have a place in the repo and are deleted in a single migration commit:

```
src/BuildingBlocks/                          (entire folder)
AthleteOS.slnx
Directory.Build.props
Directory.Packages.props
.editorconfig                                (replaced; see "What gets added")
docker/postgres/init/                        (kept only if init scripts apply; otherwise removed)
```

The `.NET`-specific service in `docker-compose.yml` is removed. PostgreSQL and Redis services are kept (re-tagged as needed).

If `tests/` contains only .NET test projects, the directory is cleared and recreated empty (TypeScript tests live alongside source under `__tests__/` or `*.test.ts` files, not in a separate top-level folder).

## What gets kept

These files and directories are preserved:

```
README.md                       (updated; see new version)
CONTRIBUTING.md                 (review and update commands; concepts unchanged)
CLAUDE.md                       (update tech stack section)
.gitignore                      (add Node-specific entries)
.env.example                    (rewritten for new stack; old values irrelevant)
docs/ARCHITECTURE.md            (replaced with new TypeScript edition)
docs/adr/0001-*.md              (modular monolith — still valid)
docs/adr/0002-*.md              (SDD on top of DDD — still valid)
docs/VISION.md                  (NEW: the previous ARCHITECTURE.md becomes the vision document)
specs/                          (kept; specs are language-agnostic)
```

## What gets added

New files for the TypeScript stack:

```
package.json                    (root, workspace declaration)
pnpm-workspace.yaml
turbo.json
tsconfig.base.json
.npmrc
.nvmrc                          (Node version pin)

apps/api/
  package.json
  tsconfig.json
  src/main.ts                   (placeholder Fastify app with /health)
  src/modules/                  (empty for now; modules created in Phase 1)
  src/shared/

apps/web-coach/
  package.json
  tsconfig.json
  vite.config.ts
  src/main.tsx                  (placeholder)

apps/web-athlete/
  package.json
  tsconfig.json
  vite.config.ts
  src/main.tsx                  (placeholder)

packages/shared-types/
  package.json
  tsconfig.json

packages/api-client/
  package.json
  (filled in once OpenAPI spec exists)

packages/eslint-config/
  package.json
  index.js

docs/adr/0003-stack-change-to-typescript.md
docs/adr/0004-rest-over-graphql-trpc.md
docs/adr/0005-direct-anthropic-sdk-no-langchain.md

docker-compose.yml              (rewritten: just Postgres + Redis + Mailhog)

docs/MIGRATION_NOTES.md         (this file)
```

## Suggested migration commit sequence

Do this on a fresh `feature/migration-to-typescript` branch off `develop`:

1. **Commit 1 — `chore: archive .NET stack`**
   - Move existing `docs/ARCHITECTURE.md` to `docs/VISION.md`.
   - Delete .NET-specific files listed above.
   - Update `.gitignore` to remove .NET patterns and add Node patterns.

2. **Commit 2 — `docs: add ADRs for stack change, REST, no LangChain`**
   - Add `docs/adr/0003-*.md`, `0004-*.md`, `0005-*.md`.
   - Add `docs/MIGRATION_NOTES.md`.

3. **Commit 3 — `docs: replace ARCHITECTURE with TypeScript edition`**
   - Add new `docs/ARCHITECTURE.md`.

4. **Commit 4 — `chore(repo): set up pnpm + Turborepo monorepo skeleton`**
   - Add root `package.json`, `pnpm-workspace.yaml`, `turbo.json`, `tsconfig.base.json`, `.nvmrc`, `.editorconfig` (TypeScript-friendly).
   - Add empty `apps/` and `packages/` workspaces.

5. **Commit 5 — `feat(api): bootstrap Fastify app with /health endpoint`**
   - Minimal Fastify app, Pino logger, env loading.
   - Dockerfile for the API.

6. **Commit 6 — `chore(infra): rewrite docker-compose.yml for Postgres + Redis + Mailhog`**

7. **Commit 7 — `feat(web): scaffold web-coach and web-athlete with Vite + React`**

8. **Commit 8 — `chore(ci): replace .NET workflow with Node CI pipeline`**
   - GitHub Actions: install pnpm, run `turbo lint test build`.

9. **PR to `develop` → review → merge.**

After merge, Phase 0 of the new roadmap is complete.

## Things to update outside the codebase

- **`CLAUDE.md`** — update the "Tech stack" section to reflect TypeScript. Update commands.
- **GitHub repo description** — should mention TypeScript, not .NET.
- **README badges** (if any) — replace .NET badge with Node + TypeScript.
- **Local dev environment** — install Node 20 LTS via Volta or fnm; install pnpm 9+. .NET SDK no longer required.
- **IDE setup** — for consistency, recommend VS Code with the standard TypeScript + ESLint + Prettier extensions. JetBrains Rider is no longer the recommended editor; WebStorm is fine if preferred.

## What stays the same

- Conventional Commits in English.
- Adapted Gitflow (`main` is prod, `develop` is integration, features on `feature/*`).
- ADR discipline.
- Spec-driven development for new features (`specs/` folder).
- Clean Architecture layers per module.
- Modular monolith.
- DDD tactical patterns.
- The product itself.

## Rollback plan

If, within the first week of the new stack, a hard blocker is discovered (extremely unlikely but worth stating), the migration commit is reverted on `develop` and the .NET setup resumes. The ADRs introduced are then marked `Status: Superseded` and a new ADR explains the rollback.

Realistically, this rollback is not expected.
