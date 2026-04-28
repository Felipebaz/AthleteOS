# ADR-0004 — REST with OpenAPI as primary API style

- **Status:** Accepted
- **Date:** 2026-04
- **Related:** ADR-0003 (TypeScript stack)

## Context

AthleteOS exposes APIs to:

- The coach dashboard (React)
- The athlete PWA (React)
- Strava (incoming webhooks)
- Eventually, third-party integrations (mobile apps, partners)

A primary API style must be chosen. The decision affects developer experience, frontend integration, documentation, security model, and how easy it is for new collaborators (or AI agents) to contribute.

## Options considered

### Option A — REST with OpenAPI (chosen)

Endpoints are nouns under `/api/v1/`. Verbs are HTTP methods. Schemas are defined with Zod, validated server-side, and used to auto-generate an OpenAPI spec served at `/docs`.

**Pros:**
- Industry standard. Every external integration we touch (Strava, Anthropic, Resend) is REST.
- Massive tooling ecosystem: Postman, Insomnia, Bruno, Scalar, Swagger UI.
- Frontend types auto-generated from OpenAPI via `openapi-typescript` or `openapi-fetch`.
- Authentication, rate limiting, caching, CDN integration are all trivial.
- Most common skill in remote-USD job market.
- Works with any client (web, mobile native, third parties, curl).
- Schema-once, validate everywhere: Zod schema produces (a) request validation, (b) response types, (c) OpenAPI doc, (d) frontend types.

**Cons:**
- Possible over-fetching (responses include fields a client may not need).
- Multiple roundtrips for related resources unless explicit aggregation endpoints are built.
- Manual versioning when contracts change.

### Option B — GraphQL

A single endpoint where clients specify exactly the shape they want.

**Pros:**
- Clients fetch exactly what they need.
- Strong typing on both sides.
- Single endpoint simplifies routing.

**Cons:**
- Significant added complexity: schema definition, resolvers, dataloaders, query depth limits, query cost analysis.
- Caching is much harder (HTTP caching doesn't apply; need GraphQL-specific caches).
- Authorization is harder (per-field).
- Authentication, rate limiting need GraphQL-aware tooling.
- N+1 query problem requires explicit dataloaders.
- Not how external APIs we integrate with work — Strava is REST, Anthropic is REST.
- Smaller talent pool, higher onboarding cost.
- Overkill for one frontend client.

### Option C — tRPC

TypeScript-only RPC framework. Backend defines procedures, frontend imports types directly.

**Pros:**
- Best developer experience in pure TypeScript stacks.
- No code generation step.
- Type safety end-to-end "for free".

**Cons:**
- Locks both sides into TypeScript. Mobile native (iOS Swift / Android Kotlin) cannot consume it natively.
- Locks third-party integrations out (no standard contract).
- Smaller ecosystem of tooling (no Postman equivalent).
- Not present in mainstream job listings; learning it doesn't transfer to other employers.
- Webhooks (Strava → us) still need REST anyway, so we end up running both.

### Option D — gRPC

Binary protocol over HTTP/2 with Protocol Buffers.

**Pros:**
- High performance.
- Strong typing via .proto files.

**Cons:**
- Browser support is limited (requires gRPC-Web proxy).
- Massive overkill for a CRUD-heavy SaaS dashboard.
- Tooling complexity (protoc, codegen pipelines, .proto management).

## Decision

**Option A — REST with OpenAPI.**

Concretely:

- Fastify as HTTP framework.
- Zod for all request/response schema definitions.
- `@fastify/swagger` (or equivalent) generates OpenAPI spec from Zod schemas.
- Spec served at `/docs` (Swagger UI) and as JSON at `/openapi.json`.
- Frontend imports types via a generated client package (`packages/api-client`) using `openapi-typescript` and `openapi-fetch`.
- All endpoints under `/api/v1/`.
- Errors follow RFC 7807 Problem Details.
- Auth via JWT in `Authorization: Bearer` header.

Real-time features (if needed later) are added via Server-Sent Events or WebSockets, not by replacing REST.

## Consequences

### Positive

- Skill learned (REST design + OpenAPI) directly transfers to any backend job.
- All external integrations follow the same paradigm.
- Documentation, validation, types, all kept in sync from one source.
- Standard tooling, standard testing patterns.
- Zero lock-in: any client in any language can consume the API.

### Negative

- Some endpoints will return more data than the smallest possible client needs. Acceptable trade-off; payload sizes are still small.
- Aggregation endpoints (e.g. `GET /api/v1/dashboard/coach`) are explicit and hand-written when a simple resource fetch isn't enough. This is a feature, not a bug — it forces explicit thinking about views.

### What this decision rules out

- No GraphQL endpoint. If a future client genuinely needs it, that's a new ADR.
- No tRPC. The TypeScript-shared-types benefit is achieved via OpenAPI codegen instead.
- No gRPC for client-facing APIs. (gRPC could appear later for internal service-to-service if/when we extract microservices, but that's far away.)

## References

- Fastify: https://fastify.dev
- Zod: https://zod.dev
- OpenAPI 3.1: https://spec.openapis.org/oas/v3.1.0
- RFC 7807 Problem Details: https://www.rfc-editor.org/rfc/rfc7807
