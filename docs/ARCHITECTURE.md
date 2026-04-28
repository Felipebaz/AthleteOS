# Architecture — AthleteOS

> **Project:** Coaching SaaS platform for endurance sports with AI assistant
> **Status:** Phase 0 — pre-development. Stack switched from .NET to TypeScript (see ADR-0003).
> **Author:** Felipe
> **Version:** 1.1 — TypeScript edition with refined wearable strategy
> **Last updated:** 2026-04
> **Time budget:** ~20 hours/week
> **Goal at 3 months:** 1 friendly coach using a working but unpolished version with 2-3 athletes.

> **Changelog**
> - **v1.1** — Wearable integration strategy clarified after market research: Strava OAuth + manual FIT file upload as MVP ingestion paths. Garmin Connect Developer Program moved to Phase 4+ (enterprise-only, requires legal entity). Polar to Phase 3-4 (evaluated against demand). COROS not committed (no robust public API).
> - **v1.0** — Initial TypeScript edition.

---

## How to read this document

This is the **buildable plan**, not the visionary one. Earlier drafts had 15 sections describing a 2-year architecture. That version became `docs/VISION.md` (a north-star reference, not a plan).

This document has 7 sections, in order from "what" to "how":

1. **Product scope (current and explicitly excluded)**
2. **Architectural principles** — the rules I follow
3. **Modules and boundaries** — what exists in MVP
4. **Stack** — exact technologies
5. **APIs and contracts** — how things talk to each other
6. **AI assistant** — what it does, how it's wired
7. **Roadmap** — 4 phases to a real user

Appendix A is the glossary. Appendix B lists ADRs.

---

# Section 1 — Product scope

## 1.1 What the product does

A coach manages 5-25 endurance athletes (running, cycling, triathlon) remotely. AthleteOS is the coach's workspace:

- The athlete connects Strava. AthleteOS pulls their activities automatically.
- The coach writes a weekly training plan for each athlete.
- AthleteOS matches each executed activity with the planned session and shows compliance.
- The coach has an AI assistant (chat) that knows the athlete's data and helps draft plan adjustments and answer "how is X doing this week".
- The system periodically generates suggestions ("Juan's last 3 sessions show high fatigue — consider an easy week"). The coach approves, edits or rejects each suggestion.

**One sentence:** the coach saves hours per week and spots problems they would have missed.

## 1.2 Who pays

The **coach**. Not the athlete. The athlete uses a free, lightweight web view of their plan.

## 1.3 What's IN the MVP (mes 3)

The MVP is whatever lets one real coach with 2-3 athletes use the product daily. Concretely:

- Coach signs up, logs in, invites athletes.
- Athlete creates account, connects Strava (OAuth).
- Strava activities are automatically ingested and stored.
- Athlete can also manually upload `.fit` files (universal fallback for any device — Garmin, COROS, Polar, Suunto, Wahoo).
- Coach sees a dashboard with their athletes and last activity per athlete.
- Coach creates a weekly training plan for each athlete (manual, no AI yet).
- Activities auto-match to planned sessions; compliance shown.
- Coach chats with an AI assistant scoped to a specific athlete ("how is Juan?", "draft easier week for Juan").
- Athlete sees their own week and recent activities in a simple page.

That's it. No notifications. No payments. No Garmin. No mobile app. No multi-coach. No fancy charts.

## 1.4 What's OUT of MVP (deferred or rejected)

| Out | When it comes back | Why deferred |
|-----|-------------------|--------------|
| Garmin Connect Developer Program (official OAuth) | Phase 4+ (after first paying coach + legal entity constituted) | Garmin's program is enterprise-only; requires a registered company, public privacy policy, and a commercial use case. Realistic application + approval window: 4-12 weeks. Until then, Garmin users are covered indirectly via Strava sync and directly via manual FIT upload. |
| Polar AccessLink API | Phase 3-4, evaluated against demand | API is approachable (OAuth2, ~2-week approval), but only exposes new data — no historical backfill — which limits onboarding value. Polar has low share in LatAm endurance market. Strava sync covers most Polar users. |
| COROS official API | Not committed | COROS does not offer a public, robust developer API. Existing third-party tools rely on reverse-engineered endpoints that break without notice. Not building on those. COROS users covered via Strava sync and manual FIT upload. |
| Other providers (Suunto, Wahoo, Apple Health) | On demand | Strava sync + manual FIT upload covers all of them indirectly. Native integration only when a paying coach demands it. |
| Automatic suggestion generation (nightly job) | Phase 2 | The chat assistant covers 80% of value first; automated suggestions need usage data to tune |
| Push notifications / email digests | Phase 2 | Email-only is fine in MVP; web push adds infra complexity |
| Billing / subscriptions | Phase 3 (mes 6+) | Free for first coaches builds goodwill and gives feedback |
| Multi-tenant with RLS | Phase 3 | `coachId` filters in every query is enough for <50 users |
| TimescaleDB | When activity volume hurts | Plain Postgres handles millions of activities fine for MVP |
| pgvector / embeddings / RAG | Phase 2-3 | LLM context window is enough for 2-3 athletes' last month of data |
| Native mobile app | Year 2+ | Athletes use Strava already; PWA is enough |
| Coach library / templates | Phase 2 | Coaches will copy plans manually first; pattern emerges from usage |
| Athlete subjective feedback (RPE, mood, etc) | Phase 2 | Adds friction in MVP; activity data alone is enough to start |

### Why Strava + manual FIT covers nearly everything in MVP

Almost every endurance athlete with a serious watch (Garmin, COROS, Polar, Suunto, Wahoo) syncs their device to Strava as a hub. By integrating only Strava in MVP, we indirectly capture data from every major device on the market. The data we get this way is slightly less rich than a direct provider API (no detailed HRV, no sleep stage breakdown), but it covers the core endurance metrics — pace, heart rate, distance, duration, GPS, cadence — which is enough for compliance tracking and load analysis.

For the minority of athletes who don't sync to Strava, or for activities that didn't sync correctly, manual `.fit` file upload is the universal fallback. FIT is the industry-standard binary format used by all major device manufacturers. A parsed FIT file gives us full per-second time series of HR, pace, power, cadence, GPS, and altitude — strictly more detailed than Strava's JSON response.

This two-pronged ingestion strategy (Strava OAuth + manual FIT upload) covers >95% of practical use cases without depending on any wearable manufacturer's approval process.

## 1.5 Success criteria

The MVP is successful if:

- 1 real coach uses it weekly for 4 consecutive weeks
- That coach reports they would miss it if it disappeared
- They can articulate what they'd pay for it (even if they're not paying yet)

If those three don't happen, no amount of polish saves it.

---

# Section 2 — Architectural principles

These are the rules. When in doubt, return to these.

**P1 — One language, one stack.** TypeScript end-to-end. Backend, frontend, scripts, tests, IaC. Mental context-switching costs more than any technical advantage of polyglot.

**P2 — Clean Architecture per module.** Each module has the four standard layers: `domain`, `application`, `infrastructure`, `api`. Dependencies point inward. The domain knows nothing about HTTP, databases, or LLMs.

**P3 — Modular monolith, not microservices.** Single deployable. Modules are folders with strict boundaries enforced by import linting (ESLint), not by network calls.

**P4 — REST APIs with OpenAPI.** No GraphQL, no tRPC. REST is the industry standard, what every external integration uses (Strava, Anthropic), and what the job market pays for. See ADR-0004.

**P5 — Async by default for slow operations.** Anything taking >1s (Strava sync, AI calls) goes through a queue (BullMQ). The HTTP request returns immediately.

**P6 — AI is a port, not a dependency.** The domain defines `ICoachAssistant`. Infrastructure implements it with the Anthropic SDK. Swapping providers is changing one file. See ADR-0005.

**P7 — Coach is always in the loop.** The AI never modifies plans directly. It drafts, suggests, explains. The coach clicks "apply" or edits.

**P8 — Tests as documentation.** Domain rules live in tests. If you read a domain test, you understand the business rule. Coverage targets: domain 90%+, application 70%+.

**P9 — Boring technology where it doesn't matter.** PostgreSQL for everything storage. Redis for queues and cache. No NoSQL, no exotic databases, no service mesh. Saved complexity goes into product features.

**P10 — Ship the ugly version first, polish what hurts.** No premature abstraction. No premature scaling. No premature multi-tenancy. Add complexity when a real user creates real pain.

**P11 — Observability from day one.** Every API request has a correlation ID. Every error goes to Sentry. Structured logs only. This is non-negotiable, not "later".

**P12 — Conventional Commits, Gitflow, ADRs.** Process discipline survives stack changes. Every architectural decision is an ADR.

---

# Section 3 — Modules and boundaries

## 3.1 Modules in MVP

Three modules. That's it.

```
apps/api/src/modules/
├── iam/              Identity & Access (auth, users, coach/athlete relationship)
├── training-data/    Strava integration, activities, normalized data
└── coaching/         Training plans, sessions, execution, AI assistant
```

Each module has the same internal structure (Clean Architecture):

```
modules/<name>/
├── domain/           Entities, value objects, domain events, repository interfaces
├── application/      Use cases (one per public operation), DTOs, port interfaces
├── infrastructure/   Prisma repos, external clients, queue workers, AI implementations
└── api/              Fastify routes, request/response schemas (Zod)
```

## 3.2 Why these three (and not seven)

The original doc had: Identity, Athlete Profile, Training Data, Coaching, Intelligence, Communication, Billing.

Collapsed to three because:

- **Athlete Profile → into Coaching.** The athlete's training zones, goals, and injury history are read together with the plan. No real boundary.
- **Intelligence → into Coaching.** The AI assistant operates on plans and activities. Splitting it adds events and indirection. When it grows, extract it.
- **Communication → out of MVP.** Notifications are an email send call. No module needed.
- **Billing → out of MVP.** No payments yet.

## 3.3 Inter-module communication

Modules don't import each other. They communicate two ways:

1. **Public contracts** — each module exports an interface (`IIamPublic`, `ITrainingDataPublic`) consumed by other modules. Implementation is wired in the composition root (`apps/api/src/main.ts`).

2. **Domain events in-process** — when something happens in one module (e.g. `ActivityIngested`), it emits a domain event via a simple event bus. Other modules subscribe. In MVP this is synchronous and in-process. When it hurts, move to a real queue.

No outbox pattern in MVP. No integration events table. If a subscriber fails, log it and move on. Eventual consistency by retry.

## 3.4 Module: `iam`

**Responsibility:** auth, user entities, coach-athlete relationship.

**Aggregates:**
- `User` (email, hashed password, role: coach or athlete)
- `CoachAthleteLink` (relationship between a coach and an athlete, with status: invited, active, ended)

**Use cases:**
- `RegisterCoach`
- `LoginUser`
- `RefreshToken`
- `InviteAthlete` (coach invites by email)
- `AcceptInvitation` (athlete accepts and creates account)

**Public contract:** `IIamPublic.getCoachIdForAthlete(athleteId)`, `getAthletesForCoach(coachId)`.

## 3.5 Module: `training-data`

**Responsibility:** OAuth with Strava, manual FIT file upload, ingest activities from any source, store them in canonical form, expose them.

**Aggregates:**
- `StravaConnection` (athlete's OAuth tokens, encrypted at app level)
- `Activity` (canonical form: type, date, duration, distance, avgHr, etc., regardless of source)

**Use cases:**
- `ConnectStrava` (returns OAuth URL)
- `HandleStravaCallback` (exchanges code, stores tokens)
- `SyncAthleteActivities` (pulls recent activities from Strava; queued)
- `HandleStravaWebhook` (incremental sync trigger)
- `UploadFitFile` (athlete uploads a `.fit` file; parsed server-side and stored as `Activity`)
- `GetActivities` (query, filterable by athlete/date/source)

**Anticorruption layer:** two adapters in MVP, both producing the same canonical `Activity`:
- `StravaActivityMapper` translates Strava's JSON into our model.
- `FitFileMapper` parses a `.fit` binary file (using `fit-file-parser` or equivalent) into our model.

The domain never sees Strava's shape or FIT's binary structure. Adding Garmin/Polar later means adding a third adapter, not changing the domain.

**Domain events emitted:** `ActivityIngested` (regardless of whether the source was Strava or FIT upload).

## 3.6 Module: `coaching`

**Responsibility:** training plans, session execution, AI assistant.

**Aggregates:**
- `TrainingPlan` (per athlete, contains weeks → sessions)
- `SessionExecution` (links a planned session with one or more ingested activities, computes compliance)

**Use cases:**
- `CreateTrainingPlan`
- `UpdatePlannedSession`
- `GetAthleteWeekView` (query for dashboard)
- `GetCoachDashboard` (query: all athletes + last activity + alerts)
- `AskCoachAssistant` (sends a coach question + athlete context to LLM, returns answer)
- `ApplyAssistantSuggestion` (when the assistant proposes a plan change and coach accepts)

**Domain events consumed:** `ActivityIngested` (triggers auto-match with planned session).

**AI integration:** uses `ICoachAssistant` port. See Section 6.

---

# Section 4 — Stack

## 4.1 Backend

| Concern | Choice | Why |
|---------|--------|-----|
| Runtime | Node.js 20 LTS | Stable, current LTS, broad ecosystem |
| Language | TypeScript 5.4+ | Strict mode, mandatory |
| HTTP framework | Fastify 4 | 2-3x faster than Express, built-in JSON Schema validation, OpenAPI plugin, mature plugin ecosystem |
| Validation | Zod | Industry standard for TS validation, integrates with Fastify, generates types and OpenAPI |
| ORM | Prisma | Best DX in Node, type-safe, schema-first, mature migrations, broad adoption |
| Database | PostgreSQL 16 | Boring, complete, can do JSON, full-text search, vectors when needed |
| Queue | BullMQ over Redis | Standard for Node job queues, dashboard included, retries, scheduled jobs |
| Cache | Redis 7 | Same instance as queue, simple |
| Auth | JWT + bcrypt | Standard, no auth provider for MVP |
| AI SDK | `@anthropic-ai/sdk` | Official, no LangChain (see ADR-0005) |
| FIT file parsing | `fit-file-parser` (or `@garmin/fitsdk` if licensing fits) | Industry standard binary format used by all major sport device manufacturers |
| Logging | Pino | Fastify's recommended, structured JSON, fast |
| Errors | Sentry | Free tier covers MVP |
| Testing | Vitest + Supertest | Vitest for unit, Supertest for HTTP integration tests |
| DB testing | Testcontainers | Real Postgres in tests |

## 4.2 Frontend

| Concern | Choice | Why |
|---------|--------|-----|
| Framework | React 18 | Industry standard |
| Build | Vite | Fast, modern |
| Language | TypeScript 5.4+ | Same as backend |
| Routing | TanStack Router | Type-safe routes, modern |
| Server state | TanStack Query | Caching, mutations, paired well with REST |
| Client state | Zustand | Simple, no boilerplate |
| Forms | React Hook Form + Zod | Same validation library as backend |
| UI primitives | shadcn/ui | Copy-paste components, no lock-in, Tailwind-based |
| Styling | Tailwind CSS | Industry standard, productive |
| API client | `openapi-fetch` + types from OpenAPI | Auto-generated types from backend spec |
| Charts | Recharts | Mature, React-native |
| Testing | Vitest + Testing Library + Playwright | Standard React testing stack |

## 4.3 Tooling and ops

| Concern | Choice |
|---------|--------|
| Monorepo | pnpm workspaces + Turborepo |
| Linter | ESLint with `@typescript-eslint` + `eslint-plugin-boundaries` (enforces module boundaries) |
| Formatter | Prettier |
| Pre-commit | Husky + lint-staged |
| CI | GitHub Actions |
| Containers | Docker + Docker Compose for local |
| Hosting (MVP) | Railway or Fly.io — managed Postgres + Redis, deploy from git |
| Domain | Cloudflare (DNS + free CDN + free WAF) |

## 4.4 Repository layout

```
athleteos/
├── apps/
│   ├── api/                  Fastify backend
│   │   └── src/
│   │       ├── modules/      iam, training-data, coaching
│   │       ├── shared/       cross-cutting (logging, errors, db client)
│   │       └── main.ts       composition root, module wiring
│   ├── web-coach/            React dashboard for coach
│   └── web-athlete/          React PWA for athlete
├── packages/
│   ├── shared-types/         types shared between apps (event payloads, etc)
│   ├── api-client/           generated from OpenAPI, used by both web apps
│   └── eslint-config/        shared ESLint rules
├── docs/
│   ├── ARCHITECTURE.md       this file
│   ├── VISION.md             north-star (v0 of architecture)
│   └── adr/                  architecture decision records
├── docker-compose.yml        Postgres + Redis for local dev
├── turbo.json
├── pnpm-workspace.yaml
└── package.json
```

---

# Section 5 — APIs and contracts

## 5.1 Style: REST with OpenAPI

All HTTP traffic is REST. Endpoints are nouns. Verbs are HTTP methods.

OpenAPI spec is generated automatically from Zod schemas via `fastify-zod` (or equivalent). The spec is served at `/docs` (Swagger UI / Scalar). The frontend consumes a TypeScript client generated from this spec.

This means:

- Schema is defined once (Zod)
- Backend validates requests against it
- OpenAPI spec is always in sync
- Frontend types are always in sync
- API documentation is always in sync

## 5.2 Versioning

All endpoints under `/api/v1/`. When breaking changes are needed, `/api/v2/` lives alongside until clients migrate.

## 5.3 Endpoint inventory (MVP)

```
# Auth
POST   /api/v1/auth/register-coach
POST   /api/v1/auth/login
POST   /api/v1/auth/refresh
POST   /api/v1/auth/logout

# Athletes (from coach perspective)
GET    /api/v1/athletes                              # list coach's athletes
POST   /api/v1/athletes/invite                       # send invitation by email
POST   /api/v1/athletes/accept-invitation            # athlete accepts
GET    /api/v1/athletes/:athleteId
DELETE /api/v1/athletes/:athleteId                   # end relationship

# Training data (Strava)
POST   /api/v1/integrations/strava/authorize         # returns OAuth URL
GET    /api/v1/integrations/strava/callback          # OAuth callback (called by Strava)
DELETE /api/v1/integrations/strava                   # disconnect
POST   /api/v1/integrations/strava/sync              # manual resync trigger

# Training data (manual FIT upload — universal fallback)
POST   /api/v1/activities/upload                     # multipart/form-data, accepts .fit files

GET    /api/v1/athletes/:athleteId/activities        # paginated, filterable by date and source
GET    /api/v1/activities/:activityId

# Training plans
GET    /api/v1/athletes/:athleteId/training-plan     # active plan
POST   /api/v1/athletes/:athleteId/training-plans    # create new
PATCH  /api/v1/training-plans/:planId
PATCH  /api/v1/training-plans/:planId/sessions/:sessionId

# AI assistant
POST   /api/v1/coach-assistant/conversations         # start conversation (scoped to athlete)
POST   /api/v1/coach-assistant/conversations/:id/messages
GET    /api/v1/coach-assistant/conversations/:id

# Dashboard (aggregated reads)
GET    /api/v1/dashboard/coach                       # coach's main view
GET    /api/v1/dashboard/athlete                     # athlete's own view

# Health
GET    /health                                        # liveness
GET    /health/ready                                  # readiness (db, redis check)
```

## 5.4 Conventions

- **IDs:** UUID v7 everywhere. Strongly-typed in code (`AthleteId`, `PlanId`).
- **Timestamps:** UTC ISO 8601 (`2026-04-15T10:30:00Z`).
- **Errors:** RFC 7807 Problem Details. Every error has a `type`, `title`, `status`, `detail`, optional `errors[]`.
- **Pagination:** cursor-based (`?cursor=...&limit=50`).
- **Auth:** `Authorization: Bearer <jwt>` header. Access token TTL 15 min. Refresh token rotation.
- **Rate limiting:** Fastify rate-limit plugin. 100 req/min per user, 10 req/min per anonymous IP.
- **Correlation:** every request gets an `X-Request-Id` header (generated if absent) propagated through logs.

## 5.5 Communication with external APIs

| External API | Purpose | Style | Auth | When |
|--------------|---------|-------|------|------|
| Strava | Activity ingestion (primary) | REST | OAuth 2.0 | MVP |
| Anthropic | AI assistant | REST (SDK wraps it) | API key | MVP (Phase 2) |
| Resend / SendGrid | Transactional email | REST | API key | Phase 1 (invites) |
| Garmin Connect Developer Program | Direct activity + health ingestion | REST | OAuth 2.0 | Phase 4+, after legal entity + paying coach |
| Polar AccessLink | Direct activity ingestion | REST | OAuth 2.0 | Phase 3-4, evaluated against demand |

All external calls go through dedicated client classes in `infrastructure/`. No external SDK leaks into application or domain code. FIT file parsing is internal (no external API call); it's a binary parser run server-side after upload.

---

# Section 6 — AI assistant

This is the differentiating feature. Two capabilities, both built on the same foundation:

## 6.1 Capability A — Conversational assistant (chat)

The coach opens an athlete's view. There's a chat panel on the side. They ask:

- *"How is Juan doing this week?"*
- *"Compare Juan's last 4 weeks of training load."*
- *"Draft an easier week for Juan, his HR has been elevated."*
- *"What sessions did he miss in March?"*

The assistant has access to:

- The athlete's profile (zones, goals, injuries)
- The active training plan
- Recent activities (last 4-8 weeks)
- The conversation history

The assistant **never modifies anything directly**. When it proposes a change ("here's a suggested easier week"), the proposal appears in the UI as an actionable card with "Apply" / "Edit" / "Reject" buttons.

## 6.2 Capability B — Automatic suggestions (Phase 2)

A scheduled job runs weekly. For each active athlete, it builds context, asks Anthropic to identify situations needing the coach's attention, and persists `Suggestion` entities. The coach sees a prioritized list on Monday morning.

This is **out of MVP**. Built once chat assistant has 4+ weeks of usage data to inform what good suggestions look like.

## 6.3 Architecture

The domain defines a port:

```typescript
// modules/coaching/domain/ports/coach-assistant.ts
export interface ICoachAssistant {
  ask(input: CoachAssistantInput): Promise<CoachAssistantResponse>;
}

export type CoachAssistantInput = {
  conversationId: string;
  message: string;
  athleteContext: AthleteContext;
  conversationHistory: Message[];
};

export type CoachAssistantResponse = {
  message: string;
  proposedActions: ProposedAction[];   // structured: e.g. "modify session X with Y"
  citations: Citation[];                // which data points were used
};
```

The infrastructure implements it:

```typescript
// modules/coaching/infrastructure/ai/anthropic-coach-assistant.ts
export class AnthropicCoachAssistant implements ICoachAssistant {
  constructor(private client: Anthropic) {}

  async ask(input: CoachAssistantInput): Promise<CoachAssistantResponse> {
    // 1. Build system prompt + tools spec
    // 2. Call client.messages.create with tools
    // 3. Handle tool_use blocks (function calling)
    // 4. Parse final response into structured output (Zod-validated)
    // 5. Return
  }
}
```

The use case orchestrates:

```typescript
// modules/coaching/application/use-cases/ask-coach-assistant.ts
export class AskCoachAssistantUseCase {
  constructor(
    private assistant: ICoachAssistant,
    private athleteRepo: IAthleteRepository,
    private planRepo: ITrainingPlanRepository,
    private activitiesPort: ITrainingDataPublic,
    private conversationRepo: IConversationRepository,
  ) {}

  async execute(command: AskCommand): Promise<AskResult> {
    // 1. Authorize coach for athlete
    // 2. Load context (athlete, plan, activities)
    // 3. Load or create conversation
    // 4. Call assistant
    // 5. Persist messages and proposed actions
    // 6. Return response
  }
}
```

**The domain has no idea Anthropic exists.** Swapping to OpenAI, Mistral, or self-hosted is one file change.

## 6.4 Function calling (tools)

The LLM doesn't read the entire database. It receives a small initial context and can request more via tools:

- `get_activity_details(activityId)` — full data of a specific activity
- `get_session_history(athleteId, sessionType, weeks)` — historical sessions of a given type
- `compare_weeks(athleteId, weekA, weekB)` — comparison of metrics across two weeks

The use case implements these tools and exposes them to the LLM. The LLM decides when to call them. Each tool call is logged.

This pattern (function calling) is **how serious AI applications are built in 2026**. It's stable, well-documented, and supported natively by Anthropic and OpenAI.

## 6.5 Structured outputs

When the assistant proposes plan changes, the response is validated against a Zod schema:

```typescript
const ProposedActionSchema = z.discriminatedUnion('type', [
  z.object({
    type: z.literal('modify_session'),
    sessionId: z.string().uuid(),
    changes: z.object({
      durationMinutes: z.number().optional(),
      intensity: z.enum(['easy', 'moderate', 'hard', 'recovery']).optional(),
      description: z.string().optional(),
    }),
    reasoning: z.string(),
  }),
  z.object({
    type: z.literal('add_note'),
    sessionId: z.string().uuid(),
    note: z.string(),
  }),
  // ...
]);
```

The LLM is instructed to return JSON matching this schema. If validation fails, the use case retries with the validation error in the prompt. Retried at most twice.

## 6.6 Cost and observability

Every LLM call is logged with: input tokens, output tokens, model used, latency, conversationId, athleteId. A daily aggregate dashboard shows cost per coach. Hard cap: if a coach exceeds $5 USD/day in LLM costs, the assistant stops responding and alerts the founder.

## 6.7 What's NOT in the AI layer

- **No LangChain.** Direct SDK only. See ADR-0005.
- **No Hugging Face.** Anthropic only. If we ever need embeddings, we'll evaluate then.
- **No agents in the autonomous sense.** The assistant has tools but always returns to the coach.
- **No fine-tuning.** Prompting + context is enough for MVP.
- **No RAG / vector search in MVP.** Athlete data fits in context window.

---

# Section 7 — Roadmap

Four phases. Each ends with a concrete deliverable.

## Phase 0 — Project setup (week 1)

Create the monorepo. Postgres + Redis via Docker Compose. Empty Fastify app with `/health`. Empty React apps. CI passing. Sentry wired.

**Done when:** `pnpm dev` starts everything locally and there's a green CI run on `main`.

**~15 hours.**

## Phase 1 — Identity and activity ingestion (weeks 2-5)

`iam` module (register, login, invite, accept). `training-data` module with two ingestion paths: real Strava OAuth + activity sync, and manual `.fit` file upload. Coach dashboard shows their athletes and recent activities. Athlete page shows their own activities and lets them upload FIT files for sessions that didn't sync automatically.

No plans yet, no AI yet.

**Done when:** I (Felipe) connect my own Strava and see my activities in the coach dashboard logged in as a coach with myself as athlete. I can also drop a `.fit` file from my watch and see it appear correctly parsed.

**~70 hours.**

## Phase 2 — Coaching and AI assistant (weeks 6-10)

`coaching` module: training plan CRUD, session execution matching. AI chat assistant with athlete context.

**Done when:** I write a plan for myself, my Strava activities auto-match to it, and I can chat with the assistant about my own training.

**~80 hours.**

## Phase 3 — First real user (weeks 11-13)

One real friendly coach onboarded. Bug fixing. UX polish on what they hit. Email notifications for invites. Basic admin tools (reset sync, view a coach's data for support).

**Done when:** that coach has been using the product for 4 consecutive weeks with at least 2 of their athletes.

**~50 hours.**

---

After Phase 3, decisions reset based on real usage:

- If the coach loves it → start phase 4 (Garmin, automatic suggestions, second coach).
- If the coach is meh → don't build more, talk to more coaches, find the real problem.
- If the coach hates it → kill or pivot.

---

# Appendix A — Glossary

| Term | Definition |
|------|------------|
| Coach | Primary user. Pays (eventually). Manages 5-25 athletes remotely. |
| Athlete | Consumer user. Free. Connects Strava, sees their plan. |
| Training plan | A coach's prescription for an athlete over a date range, organized in weeks and sessions. |
| Planned session | A session the coach prescribed (e.g. "Tuesday: 60min easy run"). |
| Activity | A workout ingested from Strava (or another source eventually). |
| FIT file | Binary file format (`.fit`) created by Garmin and adopted as industry standard. Contains per-second time series of HR, pace, GPS, power, cadence, etc. Generated by virtually every modern sport watch (Garmin, COROS, Polar, Suunto, Wahoo). Used as universal fallback ingestion path. |
| Session execution | The link between a planned session and the activities that fulfilled it, with compliance score. |
| Compliance | How well the executed activity matched the planned session (volume, intensity, type). |
| Coach assistant | The AI chat feature. Always coach-in-the-loop. |
| Suggestion | An AI-generated proposal for the coach to evaluate. |
| Module | A bounded chunk of code with strict boundaries. Three in MVP: `iam`, `training-data`, `coaching`. |
| ADR | Architecture Decision Record. Markdown file capturing a decision and its tradeoffs. |

---

# Appendix B — Architecture Decision Records

ADRs live in `docs/adr/`. Each captures a decision: context, options considered, decision, consequences.

| # | Title | Status |
|---|-------|--------|
| 0001 | Modular monolith over microservices | Accepted |
| 0002 | Spec-driven development on top of DDD | Accepted |
| 0003 | Stack change from .NET to TypeScript | Accepted |
| 0004 | REST + OpenAPI over GraphQL/tRPC | Accepted |
| 0005 | Direct Anthropic SDK, no LangChain | Accepted |

When a new architectural decision is made, a new ADR is added. ADRs are not edited once accepted — superseded ones are marked "Superseded by ADR-XXXX".

---

**End of document.**

*This is a living plan. The companion document `docs/VISION.md` describes the 2-year north star. When VISION conflicts with this document, this document wins for what's currently being built.*
