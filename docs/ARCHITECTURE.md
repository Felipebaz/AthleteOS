# Architecture Vision Document

> **Project:** Intelligent coaching SaaS platform for endurance sports
> **Working name (placeholder):** `AthleteOS` / `CoachLens` — to be defined after validation
> **Author:** Felipe
> **Version:** 0.1 — Initial draft
> **Last updated:** 2026-04
> **Audience:** the founder (myself), future technical collaborators, AI agents assisting development (Claude Code)

---

## How to read this document

This document goes from most abstract to most concrete, in 15 levels. Each level answers a different question:

1. **Product vision** — What problem do we solve and for whom?
2. **Functional requirements** — What does the system do?
3. **Non-functional requirements** — How does it behave?
4. **Architectural principles** — What rules guide every decision?
5. **Bounded contexts** — What are the parts of the business?
6. **Logical deployment view** — How is it packaged and run?
7. **Data view** — What types of data do we handle?
8. **Security view** — How do we protect everything?
9. **Technical risks** — What can go wrong?
10. **Backend modules** — How is the server code structured?
11. **Domain model per context** — What aggregates, events and use cases exist?
12. **Critical end-to-end flows** — What happens when an athlete uploads an activity?
13. **Internal Clean Architecture and technologies** — What stack, what patterns?
14. **Infrastructure, CI/CD and environments** — How is it deployed and operated?
15. **Roadmap and construction phases** — In what order is this built?

The first 9 levels are **product invariants**: they don't depend on the language or the stack. Levels 10 onwards drill down to technical detail.

---

# Level 1 — Product vision

## 1.1 Problem

Endurance sports coaches (running, cycling, triathlon, open water swimming) who train athletes remotely face three systemic problems:

1. **Manual analysis overload.** A coach with 15-25 active athletes spends between 6 and 17 hours per week just looking at Garmin/Strava data, comparing it with what was planned, detecting deviations, and adjusting plans.

2. **Late detection of critical signals.** Overtraining, imminent injuries, subclinical illness, and performance plateaus first appear in the data (HRV, sleep quality, HR variability, adherence). The coach sees them days or weeks later because they review data inconsistently.

3. **Fragmented and outdated tools.** TrainingPeaks is the standard but has 2012 UX and zero predictive intelligence. Many coaches (especially Spanish-speaking ones) end up using Excel + WhatsApp + Strava, with all the friction that implies.

## 1.2 Value proposition

A system that:

1. **Automatically ingests** training and health data for all of a coach's athletes from their wearables (Garmin, Strava, Polar, Suunto, COROS).
2. **Continuously analyzes** calculating load, readiness, trends, and detecting anomalies.
3. **Presents the coach with a prioritized dashboard** with "these 3 athletes need your attention this week and why", instead of forcing them to review 25 athletes one by one.
4. **Suggests concrete plan adjustments** that the coach approves, modifies or rejects with a click, never applied without human review.
5. **Learns from the coach** with each acceptance/rejection, adapting to their style and philosophy.

The proposition in one sentence: **the coach recovers weekly hours and increases their capacity to serve more athletes without lowering coaching quality.**

## 1.3 Stakeholders

| Stakeholder | Role | Who decides the purchase | Value received |
|-------------|------|--------------------------|----------------|
| **Independent coach** | Primary paying user | Yes | Time recovered, business scalability, better athlete retention |
| **Coach's athlete** | Consumer user | No (but influences) | More adaptive plan, faster feedback, better experience |
| **Club / Organization** | Institutional buyer (phase 2) | Yes | Squad monitoring, team-level injury prevention |
| **Administrator** | Platform operator | N/A | Support and monitoring tools |

## 1.4 North star metrics

Without these metrics nailed down, every architecture decision is opinion:

| Metric | MVP goal (6 months) | Year 1 goal |
|--------|---------------------|-------------|
| Hours/week saved per coach | 3+ | 6+ |
| AI suggestion acceptance rate | 50%+ | 70%+ |
| Active paying coaches | 10-20 | 100+ |
| Athletes per coach average | 10-15 | 18-25 |
| Monthly coach retention | 85%+ | 92%+ |
| Coach NPS | 30+ | 50+ |
| Overtraining/injury incidents detected vs. manual baseline | +30% | +60% |

## 1.5 What the product is NOT (explicitly)

It's as important to define this as to define what it is, to resist scope creep:

- **Not an app for the end athlete** (we don't compete with Strava or Garmin Connect).
- **Not a classes or booking platform** (we don't compete with Trainerize or MindBody).
- **Not a sports social network** (we don't compete with Strava as a feed).
- **Not a product for gym / pure strength coaches** (in MVP; that's future expansion).
- **Doesn't generate plans without human supervision.** The coach always approves.
- **Doesn't give medical advice or diagnose.** Detects patterns, suggests, refers.

---

# Level 2 — Functional requirements

Requirements are grouped by **business capabilities**, not UI features. A capability is something the system allows, independent of how it's implemented.

## 2.1 Capability: Identity and access management

- RF-IAM-01: Coach registers with email + password (or OAuth Google/Apple).
- RF-IAM-02: Coach invites athletes by email with a unique, expiring link.
- RF-IAM-03: Athlete accepts invitation by creating an account or signing in.
- RF-IAM-04: Coach can manage roles within their organization (future).
- RF-IAM-05: Each coach is an isolated tenant; one coach never sees another's athletes.
- RF-IAM-06: System admin can impersonate users for support (with auditing).
- RF-IAM-07: Password recovery with single-use token.
- RF-IAM-08: Optional MFA for coaches, mandatory for admins.
- RF-IAM-09: Athlete can end their relationship with a coach and migrate to another coach preserving history.

## 2.2 Capability: Athlete sports profile

- RF-ATH-01: Register basic biometric data (age, weight, gender, height).
- RF-ATH-02: Register and update training zones (HR, pace, power) manually or by test.
- RF-ATH-03: Register injury history with dates and current status.
- RF-ATH-04: Define one or more active goals (event, date, target).
- RF-ATH-05: Configure weekly availability (trainable days/hours).
- RF-ATH-06: Keep updated baselines (HRV, resting HR, average sleep).
- RF-ATH-07: Register relevant equipment (shoe km, bike km — phase 2).

## 2.3 Capability: External data ingestion

- RF-ING-01: Athlete connects Strava via OAuth 2.0.
- RF-ING-02: Athlete connects Garmin Connect via OAuth.
- RF-ING-03: Automatic ingestion of new activities without manual intervention.
- RF-ING-04: Ingestion of daily health metrics (sleep, HRV, stress, steps).
- RF-ING-05: Normalization of heterogeneous data to an internal canonical model.
- RF-ING-06: Detection and discarding of duplicates across providers.
- RF-ING-07: Manual upload of .fit/.gpx/.tcx files as fallback.
- RF-ING-08: Historical reprocessing on demand (last week, last month, all).
- RF-ING-09: Detection and notification of disconnections (expired or revoked token).

## 2.4 Capability: Training planning

- RF-PLAN-01: Coach creates a plan for an athlete with a date range and goal.
- RF-PLAN-02: Plan is structured in weeks; each week contains daily sessions.
- RF-PLAN-03: Each session has type, target duration, target intensity, description.
- RF-PLAN-04: Coach edits plan; each edit creates a new version (immutable history).
- RF-PLAN-05: Coach saves reusable blocks ("library" of sessions/weeks/mesocycles).
- RF-PLAN-06: AI generates plan draft from goal + history; coach approves/edits.
- RF-PLAN-07: Plan respects hard physiological rules (load progression ≤10% weekly without explicit flag).
- RF-PLAN-08: Athlete only sees their active plan; doesn't access old versions by default.

## 2.5 Capability: Execution tracking

- RF-EXEC-01: Automatic matching of ingested activity with planned session by date + type.
- RF-EXEC-02: Manual matching when automatic fails (coach or athlete).
- RF-EXEC-03: Deviation calculation (volume, intensity, compliance) planned vs. executed.
- RF-EXEC-04: Athlete reports daily subjective wellness (RPE, sleep, mood, muscle soreness).
- RF-EXEC-05: Athlete adds text notes to executed sessions.
- RF-EXEC-06: Daily calculation of derived metrics (TSS, CTL, ATL, TSB or equivalents).
- RF-EXEC-07: Session marking: completed, partial, skipped, replaced.

## 2.6 Capability: Intelligent analysis

- RF-AI-01: Nightly calculation of "readiness score" per athlete (0-100).
- RF-AI-02: Detection of anomalies in HRV, resting HR, sleep, load.
- RF-AI-03: Identification of plateaus (no improvement in key metric for N weeks).
- RF-AI-04: Generation of prioritized suggestions for the coach, with textual reasoning + data.
- RF-AI-05: Suggestion categories: session adjustment, block adjustment, professional referral, no action.
- RF-AI-06: Coach feedback recording (accepted, modified, rejected + optional reason).
- RF-AI-07: Incremental learning of coach style from their feedback.
- RF-AI-08: Confidence score on each suggestion (low confidence → shown but with disclaimer).
- RF-AI-09: Mandatory explainability: every suggestion shows what data supports it.

## 2.7 Capability: Coach-athlete communication

- RF-COM-01: Asynchronous messaging in thread per athlete.
- RF-COM-02: Messages can reference specific sessions or metrics.
- RF-COM-03: Push notifications (web push + email as fallback).
- RF-COM-04: Notification preferences by channel and type.
- RF-COM-05: Coach answers questions with context (AI suggests response; coach approves/edits).

## 2.8 Capability: Billing and subscriptions (phase 2)

- RF-BILL-01: Plans segmented by number of managed athletes.
- RF-BILL-02: Monthly and annual subscriptions with annual discount.
- RF-BILL-03: Recurring payments via Stripe (+ MercadoPago for LatAm).
- RF-BILL-04: Self-service upgrade, downgrade, cancellation.
- RF-BILL-05: Tax invoice generation (Uruguay, Argentina, Spain as priority countries).
- RF-BILL-06: 7-day grace period on billing failure.
- RF-BILL-07: Account reactivation after recovered payment preserves all history.

## 2.9 Capability: Reports and exports

- RF-REP-01: Automatic weekly report for athlete (summary + insights).
- RF-REP-02: Monthly report for coach (KPIs of their operation).
- RF-REP-03: Complete athlete data export (legal portability).
- RF-REP-04: Plan export in standard format (PDF + JSON).

## 2.10 Capability: Platform administration

- RF-ADM-01: Internal admin panel with platform metrics.
- RF-ADM-02: Audit logs accessible to admins.
- RF-ADM-03: Feature flag management.
- RF-ADM-04: Support tools (view a tenant's state, reset synchronizations).

---

# Level 3 — Non-functional requirements

This is the section that separates junior projects from serious projects. NFRs must be **measurable and verifiable**, not vague statements.

## 3.1 Availability

| Aspect | MVP goal | Mature product goal |
|--------|----------|---------------------|
| Monthly uptime | 99.0% (7h downtime/month) | 99.9% (43min/month) |
| RTO (Recovery Time Objective) | 4 hours | 30 minutes |
| RPO (Recovery Point Objective) | 24 hours | 1 hour |
| Planned maintenance | Announced 72h in advance | Same + low-demand window |

**Critical window:** 05:00-10:00 UTC-3 (Hispanic morning) is when coaches review nightly data; no maintenance there.

## 3.2 Performance

| Operation | p50 | p95 | p99 | Timeout |
|-----------|-----|-----|-----|---------|
| Login | 200ms | 500ms | 1s | 5s |
| Coach dashboard (main view) | 500ms | 1.5s | 3s | 10s |
| Athlete detail view | 400ms | 1s | 2s | 8s |
| Save plan adjustment | 150ms | 400ms | 800ms | 5s |
| Activity sync (background) | N/A | 5min from registration | 15min | 1h |
| AI suggestion generation | 3s | 10s | 20s | 60s (async with feedback) |
| Nightly readiness calculation | N/A | 30s/athlete | 2min | 10min |

## 3.3 Scalability

| Metric | MVP (month 6) | Year 1 | Aspirational year 3 |
|--------|---------------|--------|---------------------|
| Active coaches | 20 | 500 | 5,000 |
| Active athletes | 300 | 10,000 | 100,000 |
| Activities ingested/month | 10,000 | 300,000 | 3,000,000 |
| Peak requests/second | 5 | 100 | 1,000 |
| Time-series data (GB) | 5 | 150 | 1,500 |
| Monthly infrastructure cost | <100 USD | <2,000 USD | <20,000 USD |

**Principle:** the architecture doesn't have to *support* 5000 coaches today, it has to *be able to get there* without a deep rewrite.

## 3.4 Security

- Encryption in transit: TLS 1.3 minimum; HSTS enabled.
- Encryption at rest: AES-256 in DB and object storage.
- Application-level encryption for extra-sensitive fields (OAuth tokens, medical data).
- Secrets in managed vault (never in committed .env files, never in code).
- Automatic rotation of critical secrets every 90 days.
- Optional MFA for coaches, mandatory for admins.
- Rate limiting: 100 req/min per authenticated user; 10 req/min per unauthenticated IP.
- OWASP Top 10 protection verified (injection, XSS, CSRF, SSRF, etc.).
- Dependency scanning in CI (Snyk, Dependabot, or equivalent).
- Secret scanning in CI (gitleaks or similar).
- Pentesting before commercial GA.

## 3.5 Privacy and compliance

- **Applicable regulatory frameworks:**
  - Law 18.331 (Uruguay) — Personal data protection.
  - GDPR — if we serve European users (applies extraterritorially).
  - LGPD (Brazil) — if we expand there.
  - Law 19.628 (Chile), Habeas Data (Argentina).
- **Health data** treated as special category (GDPR Art. 9, LGPD Art. 11). Explicit consent required.
- Versioned and auditable consents.
- Explicit retention policy:
  - Athlete activities and data: while user exists + 30 days post-deletion.
  - System logs: 30-90 days.
  - Audit logs: 5 years.
  - Backups: 90-day rotation.
- Right to be forgotten: effective deletion (hard delete after security window).
- Portability: complete JSON + CSV export in less than 30 days.
- DPA signed with all providers (Anthropic, hosting, email, etc.).

## 3.6 Observability

- Structured logs (JSON) centralized, 30-day minimum retention.
- Distributed traceability with correlation ID per request.
- Business metrics (not just technical): suggestions/day, acceptance rate, synchronized activities, coach MAU.
- Standard technical metrics: latency, throughput, error rate, saturation (RED + USE).
- Proactive alerts for error rate > 1%, degraded p95 latency, queues growing without draining.
- Separate dashboards by audience: technical (on-call) and product (founder).
- Error tracking with context (Sentry or equivalent).

## 3.7 Maintainability

- Test coverage: 90%+ in Domain, 70%+ in Application, 60%+ global.
- Local dev setup in <30 min for a new dev.
- Living architectural documentation (this document + ADRs).
- Linter + formatter mandatory in CI.
- Zero-downtime deploy (rolling / blue-green).
- Automated rollback in <5 min on degradation.
- DB migrations always reversible (documented exceptions).

## 3.8 Usability

- Accessibility WCAG 2.1 AA as minimum target.
- Responsive in coach dashboard (tablet usable, mobile in read mode).
- Athlete PWA: mobile-first, installable, offline for today's plan.
- Internationalization prepared (i18n); launched in English only initially.
- First meaningful interaction on 4G mobile: <3 seconds.
- Dark mode supported.

## 3.9 Portability and vendor lock-in

- Business domain doesn't depend on any specific cloud.
- Domain core doesn't depend on a specific LLM (`IInsightGenerator` abstraction).
- Migration between clouds feasible in <1 month on serious provider incident.
- Data always recoverable: portable backups (no proprietary format).

## 3.10 Operational costs

| Component | Goal per coach/month | Justification |
|-----------|---------------------|----------------|
| Infra (compute + DB + storage) | <0.50 USD | Healthy margin against 20-40 USD subscription |
| LLM (inferences) | <0.30 USD | Caching + right model for each task |
| Email + push | <0.05 USD | Low volume |
| **Total infra/coach/month** | **<1 USD** | Gross margin >90% |

---

# Level 4 — Architectural principles

The rules that guide every technical decision. When in doubt, go to these principles.

## P1. Domain-Driven Design as north star

The code reflects the business. Code names are the names the coach uses. Modules correspond to business contexts, not technical layers. If a coach can't understand it when looking at a context diagram, something's wrong.

**Practical consequence:** there are no classes `UserManager`, `DataProcessor`, `Helper`. There are `TrainingPlan`, `Athlete`, `ReadinessSnapshot`, `CoachSuggestion`.

## P2. Strict Clean Architecture

Dependencies point **inward**, toward the domain. The domain doesn't know about infrastructure or the web. Infrastructure implements ports (interfaces) defined by the domain or the application.

**Dependency rule:**
```
Api ──► Application ──► Domain
 │            │
 └──► Infrastructure ──► Application
                   └──► Domain
```

Domain depends on no one. Application depends only on Domain. Infrastructure depends on Application and Domain. Api depends on all (composes).

## P3. Event-driven between contexts, transactional within

- **Within a bounded context:** transactional operations with ACID guarantees.
- **Between bounded contexts:** asynchronous events with eventual consistency.
- **Never distributed transactions.**

## P4. Explicit consistency boundaries

What must be immediately consistent lives in the same aggregate. What can be eventually consistent lives in separate contexts. A use case modifies **one single aggregate**.

## P5. Async by default for slow operations

Ingestion, analysis, AI generation, notifications: all via queues. The user gets an immediate response ("processing..."), doesn't wait.

## P6. Security and privacy as requirements, not features

Every design passes through the filter: what sensitive data does it touch? how is it protected? who accesses it? how is it audited? Not added later.

## P7. Observability as requirement

A module is not "done" if it doesn't expose metrics, structured logs, and traces. It's part of the Definition of Done.

## P8. Designed for one dev today, extensible to a team of 10 tomorrow

Modular monolith for now. Boundaries so clear that splitting into microservices is mechanical, not a redesign.

## P9. Vendor-agnostic in the core, pragmatic in the periphery

The domain doesn't depend on Anthropic or AWS. Adapters can be. Changing LLM or email provider doesn't touch the domain.

## P10. Automation over discipline

What can be verified with a test, CI check, or linter, is automated. Don't depend on the dev remembering.

## P11. Tests as executable documentation

Domain tests describe business rules better than any comment. They read as specifications.

## P12. Fail loud, fail early

Better an explicit error at deploy than weird behavior in production. Strict validations at the boundaries, hard invariants in the domain.

## P13. Data is the most valuable asset

When in doubt, preserve data. Backups, audit trail, soft delete when applicable, partial event sourcing for critical decisions.

## P14. Evolvability > premature scalability

Don't optimize for 1M users when you have 10. But do leave doors open for when you get there.

## P15. The best code is code that doesn't exist

Before building something, ask if there's a managed solution that solves it. Auth, payments, emails, observability: rarely worth building from scratch.

---

# Level 5 — Bounded contexts

## 5.1 Context types (Core / Supporting / Generic)

Following DDD, we classify each context by how much competitive value it generates:

| Context | Type | Build or buy |
|---------|------|-------------|
| Coaching | **Core** | Build, it's the heart |
| Intelligence | **Core** | Build, it's the advantage |
| Athlete Profile | Supporting | Build simple |
| Training Data Ingestion | Supporting | Build, with emphasis on anticorruption layer |
| Communication | Supporting | Build simple, outsource delivery |
| Identity & Access | Generic | Consider buying (Clerk, Auth0) or building standard |
| Billing | Generic | Buy (Stripe) |
| Notification Delivery | Generic | Buy (Expo Push, SendGrid) |

## 5.2 Description of each context

### 5.2.1 Identity & Access Context

**Responsibility:** authentication, authorization, user management, tenancy, invitations.

**Ubiquitous language:** User, Coach, Athlete, Tenant, Role, Permission, Session, Invitation, Refresh token.

**Boundaries:**
- **Does:** signup, login, logout, token management, role management, invitations, password recovery, MFA.
- **Doesn't:** sports profile (that's Athlete Profile), notification preferences (Communication), coach-athlete relationship as a business concept (Coaching).

**Upstream of:** all other contexts.

---

### 5.2.2 Athlete Profile Context

**Responsibility:** athlete's sports profile. Distinct from the user. An athlete can exist as a profile before they have an active account.

**Ubiquitous language:** Athlete, Training zone, Goal, Injury, Availability, Physiological test, Baseline.

**Boundaries:**
- **Does:** sports profile CRUD, zone management, injury registration, baseline calculation from historical data.
- **Doesn't:** authenticate the athlete (Identity), store activities (Training Data), plan training (Coaching).

**Collaborates with:** Coaching (provides info for planning), Intelligence (provides context for analysis).

---

### 5.2.3 Training Data Context

**Responsibility:** ingestion, normalization, and storage of wearable and external source data. It's the boundary with the outside world.

**Ubiquitous language:** Activity, Data stream, Health metric, Provider, Synchronization, External token, Webhook.

**Boundaries:**
- **Does:** connect external providers, ingest activities, normalize to canonical model, detect duplicates, store time-series streams, offer queries to other contexts.
- **Doesn't:** interpret the data (Intelligence), relate it to the plan (Coaching), show it to the user (API + frontend).

**Key architectural pattern:** **Anticorruption Layer** with one adapter per external provider. Translates heterogeneous data to the internal canonical model (`CanonicalActivity`).

---

### 5.2.4 Coaching Context

**Responsibility:** heart of the product. Training plans, their evolution, execution, and the coach-athlete relationship.

**Ubiquitous language:** Plan, Periodization, Mesocycle, Training week, Planned session, Executed session, Deviation, Adjustment, Applied suggestion, Template, Coach library.

**Boundaries:**
- **Does:** create/edit plans, version changes, match execution with planning, calculate compliance, manage coach feedback on suggestions.
- **Doesn't:** store raw activity data (Training Data), run predictive analysis (Intelligence), communicate with the athlete (Communication).

**Main aggregates:** `TrainingPlan`, `SessionExecution`, `CoachLibrary`.

---

### 5.2.5 Intelligence Context

**Responsibility:** analysis, predictions, suggestions, learning the coach's style. Where AI lives.

**Ubiquitous language:** Readiness, Trend, Anomaly, Prediction, Model confidence, Suggestion, Reasoning, Coach feedback loop.

**Boundaries:**
- **Does:** calculate daily readiness, detect anomalies, generate contextualized suggestions, learn from feedback, offer insights for dashboards.
- **Doesn't:** modify plans (only suggests, Coaching applies), make autonomous decisions (always coach-in-the-loop), store raw data.

**Main aggregates:** `AthleteReadinessSnapshot`, `CoachSuggestion`, `CoachStyleProfile`.

---

### 5.2.6 Communication Context

**Responsibility:** coach-athlete messaging, cross-channel notifications, preferences.

**Ubiquitous language:** Thread, Message, Notification, Channel (push, email, in-app), Preference, Delivery.

**Boundaries:**
- **Does:** manage coach-athlete threads, orchestrate delivery to channels, manage preferences, record deliveries.
- **Doesn't:** physically deliver (Notification Delivery outsourced), decide which events generate notifications (each emitting context decides).

---

### 5.2.7 Billing Context (phase 2)

**Responsibility:** subscriptions, payments, billing.

**Ubiquitous language:** Subscription, Price plan, Period, Charge, Invoice, Coupon, Payment method.

**Boundaries:**
- The real engine is Stripe/MercadoPago. Billing Context is the internal record system that reflects and exposes the billing state to the rest of the business.

## 5.3 Context Map

```
                    ┌────────────────────┐
                    │  Identity & Access │
                    │       (U/S)        │
                    └─────────┬──────────┘
                              │ upstream
                              ▼
       ┌──────────────────────┼──────────────────────┐
       │                      │                      │
       ▼                      ▼                      ▼
┌─────────────┐       ┌─────────────┐        ┌─────────────┐
│   Athlete   │       │   Training  │        │  Coaching   │
│   Profile   │◄─────►│     Data    │        │             │
│             │  U/S  │             │        │   (Core)    │
└──────┬──────┘       └──────┬──────┘        └──────┬──────┘
       │                     │                      │
       │                     │ Published Language    │
       │                     │ (canonical events)    │
       │                     ▼                      │
       │              ┌─────────────┐               │
       └─────────────►│Intelligence │◄──────────────┘
                      │             │  Partnership
                      │   (Core)    │
                      └──────┬──────┘
                             │
                             ▼
                      ┌─────────────┐
                      │Communication│
                      │   (OHS)     │
                      └─────────────┘

              ┌────────────────────────────┐
              │  External Providers        │
              │  (Garmin, Strava, Polar)   │
              └─────────────┬──────────────┘
                            │
                            │ ACL (Anticorruption Layer)
                            ▼
                    [Training Data]

Legend:
U/S = Upstream/Downstream (Customer-Supplier)
OHS = Open Host Service
ACL = Anticorruption Layer
```

**Applied patterns:**
- **Customer-Supplier (U/S)** between Identity and the rest: Identity dictates, others consume.
- **Published Language** between Training Data and Intelligence/Coaching: stable canonical events like `ActivityIngested`.
- **Partnership** between Coaching and Intelligence: they evolve together, strong collaboration.
- **Open Host Service** with Communication: anyone can talk to it via a stable API.
- **Anticorruption Layer** against external providers: domain protection.
- **Conformist** with Stripe (in phase 2): we accept their model, don't fight it.

---

# Level 6 — Logical deployment view

## 6.1 Runtime components

```
┌───────────────────────────────────────────────────────────────────┐
│                           EDGE / GATEWAY                           │
│  TLS • WAF • Rate limiting • CDN for assets • Reverse proxy       │
└──────────────────────────────┬────────────────────────────────────┘
                               │
         ┌─────────────────────┼─────────────────────┐
         │                     │                     │
         ▼                     ▼                     ▼
┌──────────────────┐   ┌──────────────────┐  ┌──────────────────┐
│  Coach Dashboard │   │   Athlete PWA    │  │  Admin Panel     │
│  (React + Vite)  │   │  (React + Vite,  │  │   (React)        │
│   desktop-first  │   │    PWA install)  │  │                  │
└──────────────────┘   └──────────────────┘  └──────────────────┘
         │                     │                     │
         └─────────────────────┼─────────────────────┘
                               │ HTTPS + JWT
                               ▼
┌───────────────────────────────────────────────────────────────────┐
│                  MAIN API (.NET 8 stateless)                        │
│                                                                     │
│  ┌────────────┐ ┌────────────┐ ┌────────────┐ ┌────────────┐     │
│  │  BFF Web   │ │ BFF Mobile │ │ Core API   │ │ Admin API  │     │
│  │  (Coach)   │ │  (Athlete) │ │            │ │            │     │
│  └────────────┘ └────────────┘ └────────────┘ └────────────┘     │
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────┐  │
│  │             DOMAIN MODULES (bounded contexts)                 │  │
│  │                                                               │  │
│  │  Identity • AthleteProfile • TrainingData • Coaching •        │  │
│  │  Intelligence • Communication • Billing(phase 2)              │  │
│  └─────────────────────────────────────────────────────────────┘  │
└───────────────────────────────────────────────────────────────────┘
                               │
                               ▼
┌───────────────────────────────────────────────────────────────────┐
│                    EVENT BUS / QUEUES                               │
│              (Redis Streams MVP → RabbitMQ at scale)               │
└───────────────────────────────────────────────────────────────────┘
                               │
         ┌─────────────┬───────┼───────┬─────────────┐
         │             │       │       │             │
         ▼             ▼       ▼       ▼             ▼
┌──────────────┐ ┌──────────┐ ┌────────────┐ ┌──────────────┐
│ Sync Workers │ │ Analysis │ │ AI Worker  │ │ Notification │
│ (Garmin,     │ │ Worker   │ │ (LLM calls)│ │ Dispatcher   │
│  Strava...)  │ │          │ │            │ │              │
└──────────────┘ └──────────┘ └────────────┘ └──────────────┘
         │             │             │             │
         └─────────────┴─────────────┴─────────────┘
                               │
                               ▼
┌───────────────────────────────────────────────────────────────────┐
│                         STORAGE                                     │
│                                                                     │
│  ┌────────────┐  ┌────────────┐  ┌────────────┐  ┌────────────┐  │
│  │ PostgreSQL │  │TimescaleDB │  │   Redis    │  │   Object   │  │
│  │(OLTP core) │  │(time-series│  │  (cache)   │  │  Storage   │  │
│  │            │  │ activities)│  │            │  │ (FIT, etc) │  │
│  └────────────┘  └────────────┘  └────────────┘  └────────────┘  │
└───────────────────────────────────────────────────────────────────┘

┌───────────────────────────────────────────────────────────────────┐
│                     EXTERNAL SERVICES                               │
│                                                                     │
│  Anthropic (Claude) • Garmin Connect API • Strava API • Polar     │
│  SendGrid/Resend (email) • Sentry (errors) • OTel backend (obs)   │
│  Stripe (phase 2) • Expo Push (if native app, phase 2)            │
└───────────────────────────────────────────────────────────────────┘
```

## 6.2 Topologies by phase

| Phase | Compute | Data | Observation |
|-------|---------|------|-------------|
| **MVP** | 1 API + 2 workers, PaaS (Railway/Fly) | Managed PostgreSQL + Redis | Single region |
| **Early traction (year 1)** | Auto-scaled API + worker pool | DB with read replica + Redis cluster | Single region, global CDN |
| **Scale (year 3+)** | Multi-service / extracted microservices | DB sharded by tenant + read replicas | Multi-region if latency demands it |

## 6.3 Deploy principles

- **Stateless API:** any instance can serve any request. All state goes to DB or cache.
- **Idempotent workers:** the same message can be processed twice without consequences.
- **12-factor app:** config via env vars, logs to stdout, explicit dependencies.
- **Immutable deployments:** one Docker image per version, promoted between environments.

---

# Level 7 — Data view

## 7.1 Data categories

| Category | Volume | Access pattern | Technology |
|----------|--------|----------------|------------|
| Transactional business | Medium | Balanced read/write | PostgreSQL |
| Activity time series | High | Batch write, aggregated read | TimescaleDB (PostgreSQL extension) |
| Derived / analytical data | Medium | Heavy read, nightly write | PostgreSQL + materialized views + Redis cache |
| Embeddings / vector | Low-medium | Semantic search | pgvector (PostgreSQL extension) |
| Binary files | High (bytes) | Rare write, rare read | Object storage (S3, R2) |
| Logs and audit | High | Append-only, occasional search | PostgreSQL partitioned + archiving |

## 7.2 Multi-tenancy decision

**Chosen model:** **Shared database, shared schema with `tenant_id` on every table.**

**Reasons:**
- Operational simplicity (one DB, one set of migrations).
- Low cost (not one DB per tenant).
- Facilitates cross-tenant analytics (with care).

**Accepted cons:**
- Risk of data leak between tenants if a filter is forgotten (mitigated with RLS or automatic filters in repo).
- Query noise if one tenant has high volume.

**Implementation:**
- Each business table has `tenant_id` NOT NULL column.
- PostgreSQL Row Level Security (RLS) enabled by default.
- Session variable `app.current_tenant` set on each request.
- Repositories in code automatically filter by current tenant.

**When to revisit:** if an enterprise client requires physical isolation → schema-per-tenant or DB-per-tenant.

## 7.3 Data policies

| Type | Retention | Backup | Archiving |
|------|-----------|--------|-----------|
| Athlete activities | User lifetime + 30d post-deletion | Daily, 90d retained | Cold at >2 years |
| Detailed streams | Same | Same | Compression + cold at >1 year |
| Coach-athlete messages | User lifetime + 30d | Daily | No archiving |
| System logs | 30-90 days | No backup (ephemeral) | N/A |
| Audit logs | 5 years | Monthly | Glacial >1 year |
| Backups | 90-day rotation | Off-site weekly | N/A |

## 7.4 Data modeling principles

1. **UUID v7 as identifiers** (not auto-incremental or pure v4): time-orderable, without volume revelation, concurrently insertable.
2. **Strongly-typed IDs in code**, never generic `Guid`: `AthleteId`, `TrainingPlanId`.
3. **Timestamps always in UTC**. Timezone conversion only in the UI.
4. **Soft delete with `deleted_at` flag** where legally or business-required; hard delete where required (GDPR/DPA).
5. **Mandatory audit fields:** `created_at`, `updated_at`, `created_by`, `updated_by`.
6. **Outbox table in each context schema** for outgoing events.
7. **Migrations always reversible** except documented exceptions.

---

# Level 8 — Security view

## 8.1 Threat model (STRIDE summary)

| Threat | Main vector | Mitigation |
|--------|-------------|------------|
| **S**poofing (impersonation) | Token theft, phishing | Short JWT + rotating refresh, MFA, HSTS |
| **T**ampering (manipulation) | MITM, injection | TLS 1.3, prepared statements, strict validation |
| **R**epudiation (denial) | Operation without traceability | Audit log of sensitive actions with user+timestamp+IP |
| **I**nformation disclosure | SQL injection, cross-tenant leak | Parameterized ORM, RLS, least privilege principle |
| **D**enial of service | Request flood, heavy queries | Rate limiting, timeouts, circuit breakers, bounded queue |
| **E**levation of privilege | Authorization bug | Policy-based authz, authz tests in CI |

## 8.2 Defense in layers

### Layer 1 — Perimeter
- TLS 1.3 everywhere, HSTS, optional HPKP.
- Managed WAF (CloudFlare/Vercel/cloud provider).
- Rate limiting per IP and per user.
- Cloud provider DDoS protection.

### Layer 2 — Authentication
- Password hashing with Argon2id (preferred) or bcrypt cost 12+.
- Access JWT with short TTL (15 min).
- Rotating refresh token with reuse detection.
- Optional MFA via TOTP or passkeys.
- Lockout after N failed attempts with backoff.

### Layer 3 — Authorization
- Policy-based: each endpoint declares requirements (`CanManageAthlete`, `CanViewPlan`).
- Multi-tenant enforcement by RLS in DB + code filter (double check).
- Least privilege principle on DB roles.

### Layer 4 — Data protection
- Encryption at rest (AES-256) in DB and object storage.
- Application-level encryption for OAuth tokens and sensitive medical fields.
- Vault for secrets (AWS Secrets Manager, Doppler, or similar).
- Automatic secret rotation every 90d for integration secrets.

### Layer 5 — Auditing
- Log of accesses to sensitive data (coach reading athlete info).
- Admin operation log.
- Tamper-evident: append-only, optional hash chain.
- 5-year retention.

### Layer 6 — Incident response
- Documented runbook.
- Defined on-call responsibilities (rotation when team exists).
- User communication plan (72h for GDPR notification).
- Blameless post-mortems.

## 8.3 OAuth with external providers (specific)

- Garmin/Strava tokens **encrypted at application level** before persisting.
- Minimum required scopes.
- Revocation detection and re-authorization request.
- Never log tokens.
- Automatic refresh token renewal.

---

# Level 9 — Technical risks

## 9.1 Risk matrix

| # | Risk | Probability | Impact | Exposure | Mitigation |
|---|------|-------------|--------|----------|------------|
| R1 | Garmin/Strava changes API and breaks ingestion | Medium | High | **High** | ACL, multiple providers, manual upload as fallback |
| R2 | LLM costs scale disproportionately | High | Medium | **High** | Caching, structured outputs, model per task, cost monitoring/tenant |
| R3 | LLM hallucinates a dangerous suggestion | Medium | High | **High** | Coach-in-the-loop always, structured validation, hard domain rules |
| R4 | Incorrect physiological modeling (TSS, CTL, etc.) | Medium | High | **High** | Consult 2-3 expert coaches, validate with literature, test with real data |
| R5 | Health data leak | Low | Catastrophic | **High** | Security by design, pentesting, double encryption, minimum data |
| R6 | Vendor lock-in with specific LLM | Medium | Medium | Medium | `IInsightGenerator` abstraction, periodic alternative evaluation |
| R7 | Complexity exceeds team capacity (1 dev) | High | High | **High** | Managed services, core focus, ruthless feature cutting |
| R8 | Premature over-engineering | High | Medium | **High** | Modular monolith, no microservices until real pain |
| R9 | Low adoption by coaches (unvalidated product) | Medium | Catastrophic | **High** | Validate with 10+ coaches before code, early paid beta |
| R10 | Churn due to lack of sustained value | Medium | High | High | Engagement metrics from day 1, client check-ins |
| R11 | Legal issues from health data | Low | Catastrophic | Medium | Local legal advisor, DPAs, clear terms, granular consents |
| R12 | Catastrophic DB failure without usable backup | Low | Catastrophic | Medium | Daily backups tested monthly (real restore test) |

**Rule:** risks marked with **High** exposure must have a concrete backlog item each sprint.

## 9.2 Non-technical risks to keep in mind

- **Focus drift:** adding features for a second segment (nutritionists, gym) before dominating the first.
- **Founder burnout:** no revenue for 6+ months building alone is hard; have clear deadlines and breaks.
- **Lack of real feedback:** building in a vacuum without real coaches testing.

---

# Level 10 — Backend modules

Now we land the bounded contexts into real .NET code structure.

## 10.1 Solution organization

**Pattern:** modular monolith with one project per layer per bounded context, plus shared projects.

```
src/
├── BuildingBlocks/                           # shared code, no domain
│   ├── BuildingBlocks.Domain/                # base classes: Entity, AggregateRoot, ValueObject, DomainEvent
│   ├── BuildingBlocks.Application/           # MediatR behaviors, generic abstractions
│   ├── BuildingBlocks.Infrastructure/        # EF interceptors, outbox base, event bus abstraction
│   └── BuildingBlocks.Api/                   # filters, middlewares, problem details
│
├── Modules/
│   ├── Identity/
│   │   ├── Identity.Domain/
│   │   ├── Identity.Application/
│   │   ├── Identity.Infrastructure/
│   │   └── Identity.Api/                     # controllers + endpoints
│   │
│   ├── AthleteProfile/
│   │   ├── AthleteProfile.Domain/
│   │   ├── AthleteProfile.Application/
│   │   ├── AthleteProfile.Infrastructure/
│   │   └── AthleteProfile.Api/
│   │
│   ├── TrainingData/
│   │   ├── TrainingData.Domain/
│   │   ├── TrainingData.Application/
│   │   ├── TrainingData.Infrastructure/
│   │   │   └── Integrations/                 # one adapter per provider
│   │   │       ├── Strava/
│   │   │       ├── Garmin/
│   │   │       └── Polar/
│   │   └── TrainingData.Api/
│   │
│   ├── Coaching/
│   │   ├── Coaching.Domain/
│   │   ├── Coaching.Application/
│   │   ├── Coaching.Infrastructure/
│   │   └── Coaching.Api/
│   │
│   ├── Intelligence/
│   │   ├── Intelligence.Domain/
│   │   ├── Intelligence.Application/
│   │   ├── Intelligence.Infrastructure/
│   │   │   └── Llm/                          # abstractions + Anthropic impl
│   │   └── Intelligence.Api/
│   │
│   └── Communication/
│       ├── Communication.Domain/
│       ├── Communication.Application/
│       ├── Communication.Infrastructure/
│       └── Communication.Api/
│
├── Bootstrap/
│   └── ApiHost/                              # entry point, composes all modules
│
└── Workers/
    ├── SyncWorker/                           # processes sync queues
    ├── AnalysisWorker/                       # readiness, anomalies
    ├── AiWorker/                             # LLM calls
    ├── NotificationWorker/                   # notification dispatch
    └── OutboxPublisher/                      # publishes outbox events to bus

tests/
├── Modules/
│   ├── Identity.UnitTests/
│   ├── Identity.IntegrationTests/
│   ├── Coaching.UnitTests/
│   └── ...
└── E2E/
    └── CriticalFlows/
```

## 10.2 Inter-module communication rules

1. **Modules don't reference each other in code.** The Coaching module doesn't do `using Intelligence.Domain`. This prevents hidden dependencies.

2. **Inter-module communication only via two channels:**
   - **Integration Events** (asynchronous, decoupled, via bus).
   - **Module Public API**, exposed as an interface in a `ModuleX.Contracts` project consumable by other modules if synchronous reading is needed.

3. **Each module owns its DB schema.** Schemas are named the same as the module: `identity`, `coaching`, `intelligence`, etc.

4. **No cross-schema queries.** If Coaching needs info from Athlete Profile, it requests it via public API or receives it via event.

## 10.3 Host composition

The `ApiHost` project is the only one that references all module `*.Api` projects and composes them:

```
Program.cs (conceptual pseudocode)
    ↓
builder.Services
    .AddBuildingBlocks()
    .AddIdentityModule(configuration)
    .AddAthleteProfileModule(configuration)
    .AddTrainingDataModule(configuration)
    .AddCoachingModule(configuration)
    .AddIntelligenceModule(configuration)
    .AddCommunicationModule(configuration);
    ↓
app.MapIdentityEndpoints();
app.MapAthleteProfileEndpoints();
// ... etc
```

Each module exposes an extension method `AddXxxModule` and `MapXxxEndpoints` in its `.Api` project. This makes adding a new module a single line.

---

# Level 11 — Domain model per context

Here we describe the main aggregates, events, and use cases of the core contexts. Generic/supporting contexts are summarized.

## 11.1 Coaching Context — detail

### Aggregates

#### `TrainingPlan` (Aggregate Root)

**Invariants:**
- Always has at least one associated goal.
- Weeks don't overlap or leave gaps in the plan period.
- Weekly load increase ≤ 10% without explicit coach flag.
- No 2 consecutive high-intensity sessions without ≥24h recovery.
- A completed session is not modified; a new plan version is created.

**Internal entities:**
- `TrainingWeek`
- `PlannedSession`

**Value objects:**
- `Intensity` (low, medium, threshold, VO2max, neuromuscular)
- `SessionType` (easy run, intervals, tempo, long run, strength, rest, cross-training)
- `Duration`
- `LoadProgression`

**Key operations:**
- `CreateFromTemplate(templateId, athleteId, startDate)`
- `CreateFromAiDraft(aiDraftId, coachAdjustments)`
- `AdjustWeek(weekNumber, changes, reason)`
- `ApplyCoachSuggestion(suggestionId, overrides?)`
- `Archive()`

**Events emitted:**
- `TrainingPlanCreated`
- `TrainingPlanAdjusted`
- `TrainingPlanArchived`
- `PlannedSessionUpdated`

#### `SessionExecution` (Aggregate Root)

**Responsibility:** how a session was executed, matched with what was planned.

**Invariants:**
- Has reference to `PlannedSessionId` and one or more `ActivityId` (from Training Data).
- Status is automatically derived from the match (`Completed`, `Partial`, `Substituted`, `Skipped`).
- Subjective athlete feedback can only be written by the athlete.

**Value objects:**
- `ComplianceScore` (0-100, derived from volume and intensity)
- `SubjectiveFeedback` (RPE, mood, sleep, muscle soreness)

**Key operations:**
- `LinkActivity(activityId)`
- `RecordSubjectiveFeedback(feedback)`
- `MarkAsSkipped(reason)`
- `RecalculateCompliance()`

**Events emitted:**
- `SessionExecutionUpdated`
- `SessionDeviationDetected` (when compliance < threshold)

#### `CoachLibrary` (Aggregate Root)

Coach's library with reusable session, week, and mesocycle templates. Simple but worth modeling explicitly.

### Events consumed (from other contexts)

- `ActivityIngested` → tries to match with `PlannedSession`, creates/updates `SessionExecution`.
- `HealthMetricsUpdated` → trigger for recomputation of derived metrics.
- `CoachSuggestionGenerated` → save reference so the coach can apply it.

### Main use cases (Application layer)

- `CreateTrainingPlanCommand`
- `AdjustTrainingWeekCommand`
- `ApplyCoachSuggestionCommand`
- `RecordSessionFeedbackCommand` (athlete)
- `GetAthleteWeekViewQuery`
- `GetCoachDashboardQuery`

---

## 11.2 Intelligence Context — detail

### Aggregates

#### `AthleteReadinessSnapshot` (Aggregate Root)

Daily athlete readiness state. Computed each night (or on demand).

**Invariants:**
- Unique per athlete per day.
- Global score (0-100) derived from components.
- Has a `confidenceScore` based on how much real data is available.

**Calculated components:**
- `HrvTrend` (% vs 28-day baseline).
- `SleepQuality` (hours + wearable score).
- `TrainingLoadBalance` (TSB / CTL ratio).
- `SubjectiveWellness` (from athlete feedback).

**Possible flags:** `PossibleOvertraining`, `PossibleIllness`, `HighFatigue`, `LowAdherence`, `NormalState`.

**Events emitted:**
- `ReadinessSnapshotCalculated`
- `AnomalyDetected` (if any critical flag)

#### `CoachSuggestion` (Aggregate Root)

An AI-generated suggestion for the coach about an athlete.

**Invariants:**
- Has textual reasoning + structured data supporting it.
- Has a category (`SessionAdjust`, `BlockAdjust`, `Referral`, `Informational`).
- Has `confidenceScore`.
- Status: `Pending`, `Accepted`, `ModifiedAndAccepted`, `Rejected`, `Expired`.
- Coach feedback is immutable once given.

**Key operations:**
- `Accept(coachId)`
- `AcceptWithModifications(coachId, modifications, reason?)`
- `Reject(coachId, reason?)`
- `Expire()` (after certain time without response)

**Events emitted:**
- `CoachSuggestionGenerated`
- `CoachSuggestionAccepted`
- `CoachSuggestionRejected`

#### `CoachStyleProfile` (Aggregate Root)

Coach style model, built from their acceptances/rejections. Used to personalize future suggestions.

**Content:**
- Learned weights by suggestion type.
- Explicit preferences (preferred training zones, periodization styles).
- Statistics (historical acceptance rate, average response time).

### Main use cases

- `CalculateReadinessCommand` (triggered by nightly scheduler)
- `GenerateWeeklyReviewSuggestionsCommand` (plan adjustment suggestions)
- `RecordSuggestionFeedbackCommand`
- `GetPendingSuggestionsQuery`
- `GetAthleteReadinessQuery`

---

## 11.3 Training Data Context — detail

### Aggregates

#### `AthleteDataSource` (Aggregate Root)

An athlete's connection to an external provider.

**Contains:** OAuth tokens (encrypted), scopes, status, last sync, recent errors.

**Operations:** `Connect`, `Disconnect`, `RefreshToken`, `TriggerSync`, `MarkAsFailed`.

#### `Activity` (Aggregate Root)

A sports activity ingested into the system, already normalized.

**Invariants:**
- Unique per `(source, externalId)`.
- `startTimestamp < endTimestamp`.
- TSS is calculated internally, not received from the provider.

**Value objects:** `ActivityType`, `MetricsSummary`, `GeoRoute`.

**Operations:** `LinkToPlannedSession`, `RecalculateMetrics`, `MarkAsManual`.

#### `HealthMetricSnapshot` (Aggregate Root)

Daily health metrics (sleep, HRV, stress, steps, weight) for an athlete.

### Integration with external providers (ACL)

Pattern per provider:

```
Infrastructure/Integrations/Strava/
├── StravaOAuthClient.cs
├── StravaActivityFetcher.cs
├── StravaWebhookReceiver.cs
├── StravaActivityMapper.cs     # translates from Strava model to CanonicalActivity
└── StravaAdapter.cs             # implements IWearableProvider
```

Each adapter implements the `IWearableProvider` interface:

```
IWearableProvider
├── ConnectAsync(authCode, athleteId)
├── FetchActivitiesAsync(since, until)
├── FetchHealthMetricsAsync(date)
├── HandleWebhookAsync(payload)
└── DisconnectAsync()
```

---

## 11.4 Athlete Profile Context — summary

### Main aggregate

#### `Athlete` (Aggregate Root)

**Contains:**
- Basic biometric data.
- `TrainingZones` (HR, pace, power).
- Injury history (`InjuryRecord[]`).
- Active goals (`Goal[]`).
- Weekly availability (`WeeklyAvailability`).
- Baselines (`HrvBaseline`, `RestingHrBaseline`).

**Invariants:**
- Zones never overlap incorrectly.
- No 2 active goals with the same event on the same date.
- Baselines are updated only with validated data.

**Operations:** `RecalibrateZones`, `RegisterInjury`, `UpdateBaseline`, `SetAvailability`.

---

## 11.5 Identity Context — summary

### Aggregates

- `User` — basic identity and credentials.
- `Coach` — User extension with coach data (tenant owner).
- `AthleteAccount` — User extension for athletes (can be with multiple coaches over time).
- `Invitation` — invitation token with expiration.
- `Session` / `RefreshToken` — session management.

If outsourced to Clerk/Auth0, this context simplifies to an adapter + the business concept of "Coach" and "Athlete" extended on the external user.

---

## 11.6 Communication Context — summary

### Aggregates

- `ConversationThread` — coach-athlete thread.
- `Message` — message within a thread.
- `Notification` — notification generated by events from other contexts, with preferences applied.

### Events consumed from other contexts

Communication subscribes to many events and decides what to notify:
- `TrainingPlanAdjusted` → notifies the athlete.
- `CoachSuggestionGenerated` (if high confidence) → notifies the coach.
- `SessionDeviationDetected` → notifies the coach.
- `ActivityIngested` (large ones) → notifies the coach.

---

## 11.7 Integration events (the "published language" between contexts)

List of stable cross-context events. These are contracts that don't change lightly.

| Event | Emitter | Typical consumers |
|-------|---------|-------------------|
| `UserRegistered` | Identity | AthleteProfile, Communication |
| `AthleteInvitedToCoach` | Identity | AthleteProfile, Coaching |
| `AthleteProfileUpdated` | AthleteProfile | Coaching, Intelligence |
| `DataSourceConnected` | TrainingData | Communication |
| `ActivityIngested` | TrainingData | Coaching, Intelligence |
| `HealthMetricsUpdated` | TrainingData | Intelligence |
| `TrainingPlanCreated` | Coaching | Communication, Intelligence |
| `TrainingPlanAdjusted` | Coaching | Communication, Intelligence |
| `SessionExecutionUpdated` | Coaching | Intelligence |
| `ReadinessSnapshotCalculated` | Intelligence | Coaching (for dashboard) |
| `CoachSuggestionGenerated` | Intelligence | Coaching, Communication |
| `CoachSuggestionAccepted` | Coaching | Intelligence (learning loop) |
| `CoachSuggestionRejected` | Coaching | Intelligence (learning loop) |

**Versioning:** each event has a version in the schema. Breaking changes → new version, backward compatibility with previous version during transition period.

---

# Level 12 — Critical end-to-end flows

This section is gold for understanding the system. We write flows as sequences of steps, naming the participating components.

## 12.1 Flow: Athlete connects Strava and their first activity appears on the coach's dashboard

**Participants:** Athlete PWA, Identity API, TrainingData API, Strava API, SyncWorker, Outbox Publisher, Event Bus, Coaching handler, Intelligence handler, Coach Dashboard.

**Sequence:**

1. Athlete in the PWA taps "Connect Strava".
2. Frontend redirects to `/oauth/strava/authorize` (TrainingData API), which generates state and redirects to Strava.
3. Athlete authorizes on Strava.
4. Strava redirects to callback `/oauth/strava/callback` with authorization code.
5. TrainingData API:
    - Validates state.
    - Exchanges code for tokens via `StravaOAuthClient`.
    - Creates `AthleteDataSource` aggregate with encrypted tokens.
    - Triggers `InitialSyncCommand` use case.
    - Persists aggregate + event in outbox in same transaction.
6. `OutboxPublisher` reads outbox, publishes `DataSourceConnected` to bus.
7. `SyncWorker` consumes `DataSourceConnected` and starts historical sync (last 30 days).
8. For each activity fetched from Strava:
    - Map to `CanonicalActivity` via `StravaActivityMapper`.
    - Persist `Activity` aggregate.
    - `ActivityIngested` event to outbox.
9. `OutboxPublisher` publishes `ActivityIngested`.
10. **Coaching handler** consumes `ActivityIngested`:
    - Finds matchable `PlannedSession` by date + type.
    - Creates or updates `SessionExecution`.
    - Emits `SessionExecutionUpdated`.
11. **Intelligence handler** consumes `ActivityIngested`:
    - Updates athlete's aggregated metrics (TSS, CTL, ATL).
    - If anomalous pattern detected, generates suggestion.
12. In parallel, athlete sees "Strava connected ✅" immediately (step 5 already completed).
13. Coach, when refreshing dashboard, sees new activities and any generated suggestions.

**Guarantees:**
- If sync fails mid-way, it's resumed per non-duplicated activity.
- If the API goes down after step 5, the athlete sees successful connection but the sync completes in the background.
- If mapping fails for one activity, the others continue.

## 12.2 Flow: Generation and application of a weekly suggestion

**Participants:** Scheduler, AI Worker, Intelligence Application, LLM (Anthropic), Coaching API, Coach Dashboard.

**Sequence:**

1. Every Monday at 06:00 UTC-3, a scheduled job enqueues `GenerateWeeklyReviewCommand` for each active coach.
2. `AI Worker` consumes the commands one by one.
3. For each coach, the worker:
    - Gets the list of active athletes.
    - For each athlete: builds context (current plan, last 2-4 weeks of execution, readiness snapshots, goals, injury history).
    - Calls `IInsightGenerator` (Anthropic implementation) with structured prompt + context.
    - LLM returns structured JSON with suggestions (or "no action needed").
    - JSON validation against schema; if fails, retry with feedback.
    - Creates `CoachSuggestion` aggregates, one per recommendation.
    - Persists + outbox `CoachSuggestionGenerated`.
4. Communication handler consumes `CoachSuggestionGenerated` and sends notification to coach.
5. Monday 9:00 AM, coach opens dashboard.
6. Dashboard calls `GetCoachDashboardQuery` which returns prioritized athletes + pending suggestions.
7. Coach taps "Accept" on a suggestion.
8. Frontend calls `ApplyCoachSuggestionCommand` on Coaching API.
9. Coaching API:
    - Loads `CoachSuggestion` via Intelligence public API.
    - Loads athlete's `TrainingPlan`.
    - Applies suggested changes to plan (new version).
    - Persists + outbox `CoachSuggestionAccepted` + `TrainingPlanAdjusted`.
10. Intelligence handler consumes `CoachSuggestionAccepted` and updates `CoachStyleProfile` (learns).
11. Communication handler consumes `TrainingPlanAdjusted` and notifies athlete.

## 12.3 Flow: Athlete records subjective session feedback

1. Athlete completes a run (Garmin uploads it to Strava → we already have it in the system via flow 12.1).
2. Athlete in the PWA sees "How was the session?" with quick controls (RPE 1-10, mood, last night's sleep).
3. Athlete fills it in, frontend calls `RecordSessionFeedbackCommand` on Coaching API.
4. Coaching API loads `SessionExecution`, adds feedback, persists + emits `SessionExecutionUpdated`.
5. Intelligence handler consumes and adjusts day's readiness if applicable.
6. If very high RPE + large deviation → Intelligence generates an immediate suggestion (doesn't wait until Monday).

## 12.4 Flow: Revoked OAuth token recovery

1. `SyncWorker` attempts sync, Strava responds `401 Unauthorized`.
2. Worker checks if it's an expired token → attempts refresh. If successful, continues.
3. If the refresh also fails (revoked), worker marks `AthleteDataSource` as `Disconnected` with reason `ExternalRevocation`.
4. Emits `DataSourceDisconnected`.
5. Communication notifies the athlete: "Your Strava connection was lost. Reconnect by tapping here."

---

# Level 13 — Internal Clean Architecture and technologies

## 13.1 Internal structure of a module (example: Coaching)

```
Coaching.Domain/
├── Aggregates/
│   ├── TrainingPlanAggregate/
│   │   ├── TrainingPlan.cs                # AggregateRoot
│   │   ├── TrainingPlanId.cs              # strongly-typed ID
│   │   ├── TrainingWeek.cs                # Entity
│   │   ├── PlannedSession.cs              # Entity
│   │   ├── SessionType.cs                 # Value Object
│   │   ├── Intensity.cs                   # Value Object
│   │   └── Events/
│   │       ├── TrainingPlanCreated.cs
│   │       └── TrainingPlanAdjusted.cs
│   └── SessionExecutionAggregate/
│       └── ...
├── DomainServices/
│   └── LoadProgressionPolicy.cs           # logic that doesn't belong to an aggregate
├── Repositories/
│   ├── ITrainingPlanRepository.cs         # interfaces only
│   └── ISessionExecutionRepository.cs
├── Exceptions/
│   ├── InvalidPlanAdjustmentException.cs
│   └── LoadProgressionViolatedException.cs
└── SeedWork/                              # module base classes if needed
    └── (usually empty if BuildingBlocks.Domain is used)

Coaching.Application/
├── UseCases/
│   ├── CreateTrainingPlan/
│   │   ├── CreateTrainingPlanCommand.cs
│   │   ├── CreateTrainingPlanHandler.cs
│   │   ├── CreateTrainingPlanValidator.cs
│   │   └── CreateTrainingPlanResult.cs
│   ├── AdjustTrainingWeek/
│   ├── ApplyCoachSuggestion/
│   ├── GetAthleteWeekView/                # query
│   └── GetCoachDashboard/                 # query
├── IntegrationEventHandlers/
│   ├── OnActivityIngestedHandler.cs       # from TrainingData
│   ├── OnAthleteProfileUpdatedHandler.cs  # from AthleteProfile
│   └── OnCoachSuggestionGeneratedHandler.cs
├── Abstractions/                          # ports toward infra
│   ├── IIntegrationEventBus.cs
│   ├── IAthleteProfileApi.cs              # public API of another module
│   └── IIntelligenceApi.cs
├── Dtos/                                  # internal, not the API ones
│   ├── AthleteWeekView.cs
│   └── CoachDashboardView.cs
└── Behaviors/                             # MediatR pipeline (optional if not in BuildingBlocks)

Coaching.Infrastructure/
├── Persistence/
│   ├── CoachingDbContext.cs
│   ├── Configurations/                    # EF Core entity configs
│   │   ├── TrainingPlanConfiguration.cs
│   │   └── SessionExecutionConfiguration.cs
│   ├── Repositories/
│   │   ├── TrainingPlanRepository.cs
│   │   └── SessionExecutionRepository.cs
│   ├── Outbox/
│   │   └── CoachingOutboxProcessor.cs
│   └── Migrations/
├── IntegrationEvents/
│   └── CoachingIntegrationEventPublisher.cs
└── Startup/
    └── CoachingModuleExtensions.cs        # AddCoachingModule + MapCoachingEndpoints

Coaching.Api/
├── Endpoints/
│   ├── CoachDashboard/
│   │   └── GetCoachDashboardEndpoint.cs
│   ├── TrainingPlans/
│   │   ├── CreateTrainingPlanEndpoint.cs
│   │   └── AdjustTrainingWeekEndpoint.cs
│   └── SessionExecutions/
│       └── RecordFeedbackEndpoint.cs
├── Contracts/                             # API request/response, separate from Application DTOs
│   ├── Requests/
│   └── Responses/
├── Mapping/
│   └── CoachingApiMappingProfile.cs
└── Authorization/
    └── CoachAccessRequirement.cs
```

## 13.2 Consolidated tech stack

### Backend
| Layer | Technology | Why |
|-------|------------|-----|
| Language | C# 12 / .NET 8 | Mature for DDD, top-tier performance, job market |
| Web framework | ASP.NET Core Minimal APIs | Modern, low overhead |
| Mediator | MediatR | De facto standard for CQRS-light in .NET |
| Validation | FluentValidation | Composable, testable |
| ORM | EF Core 8 | Mature, supports DDD well with configurations |
| Migrations | EF Core Migrations | Versioned, reversible |
| Background jobs | Hangfire | Built-in UI, retries, scheduled jobs |
| Mapping | Mapperly (source-gen) or manual | No reflection, performant |
| Logging | Serilog | Structured, rich sinks |
| Observability | OpenTelemetry (.NET SDK) | Open standard |
| Errors | Sentry.NET | Contextual error tracking |
| Testing | MSTest (course continuity) + FluentAssertions + Testcontainers + Bogus | Already in use |
| HTTP client | Refit or HttpClient + DelegatingHandlers | Typed, testable |

### Data
| Component | Technology |
|-----------|------------|
| Transactional DB | PostgreSQL 16 |
| Time-series | TimescaleDB (extension) |
| Vector | pgvector (extension) |
| Cache / event bus MVP | Redis 7 |
| Object storage | Cloudflare R2 (S3-compatible, cheap) |

### External integrations
| Purpose | Service |
|---------|---------|
| LLM | Anthropic (Claude Sonnet / Haiku per task) |
| Wearables | Garmin Connect, Strava, Polar, Suunto, COROS (gradual) |
| Transactional email | Resend or SendGrid |
| Auth (if outsourced) | Clerk (option A) or ASP.NET Core Identity (option B) |
| Payments (phase 2) | Stripe + MercadoPago |
| Monitoring | Grafana Cloud free tier or Better Stack |

### Frontend
| Component | Technology |
|-----------|------------|
| Framework | React 18 + TypeScript + Vite |
| Routing | TanStack Router |
| Server state | TanStack Query |
| Client state | Zustand |
| Forms | React Hook Form + Zod |
| Styling | Tailwind + shadcn/ui |
| Charts | Recharts or Visx |
| PWA | Vite PWA plugin + Workbox |
| Testing | Vitest + React Testing Library + Playwright (E2E) |

### Monorepo (frontends)
| Tool | Use |
|------|-----|
| pnpm workspaces | Package management |
| Turborepo | Build orchestration + cache |

### DevOps
| Component | Technology |
|-----------|------------|
| Containers | Docker |
| CI/CD | GitHub Actions |
| Hosting MVP | Railway or Fly.io |
| IaC (when applicable) | Terraform or Pulumi |

## 13.3 Key technical patterns

1. **Outbox pattern:** integration events in `outbox` table within the same transaction as the aggregate. Worker publishes to bus.
2. **Inbox pattern:** consumed events recorded in `inbox` table for idempotency.
3. **CQRS light:** commands go through aggregates; queries read directly to projections/DTOs.
4. **Anticorruption Layer:** one implementation per external provider, translation to canonical model.
5. **Specification pattern:** for reusable complex queries.
6. **Domain events in-process:** dispatched by MediatR within the same unit of work.
7. **Strongly-typed IDs:** records in Domain, converted by EF to DB columns.
8. **Result pattern:** expected errors as values, not exceptions; exceptions only for truly exceptional cases.
9. **Pipeline behaviors (MediatR):** logging, validation, transaction, authorization applied cross-cutting.
10. **Feature flags:** granular control of new features in production.

## 13.4 Testing strategy

| Level | Tools | Target coverage | What it tests |
|-------|-------|-----------------|---------------|
| Unit — Domain | MSTest + FluentAssertions | 90%+ | Aggregate invariants, value objects, domain services |
| Unit — Application | MSTest + NSubstitute | 70%+ | Use cases with mocks |
| Integration | MSTest + Testcontainers (real PostgreSQL) | 60%+ | Repos, event handlers, intra-context flows |
| Contract | MSTest | 100% of external integrations | External provider adapters |
| E2E | Playwright | 5-10 critical flows | User-facing flows |
| Performance | k6 or NBomber | Critical endpoints | Latency under load |
| Security | OWASP ZAP + Snyk | Automated in CI | Common vulnerabilities |

---

# Level 14 — Infrastructure, CI/CD and environments

## 14.1 Environments

| Environment | Purpose | Data | Costs |
|-------------|---------|------|-------|
| `local` | Dev on dev machine | Docker Compose with fixtures | 0 |
| `dev` | `develop` branch deployed, branch previews | Synthetic | ~20 USD/month |
| `staging` | Pre-production, QA, integration tests | Anonymized or rich synthetic | ~40 USD/month |
| `prod` | Real production | Real | Variable |

## 14.2 Infrastructure per environment (MVP)

**MVP - Railway or Fly.io:**
- 1 API service
- 1-2 Worker services
- Managed PostgreSQL (with TimescaleDB)
- Managed Redis
- Cloudflare R2 for object storage
- Cloudflare as free CDN + WAF

**Post-traction - AWS:**
- ECS Fargate for API + workers
- RDS PostgreSQL Multi-AZ with TimescaleDB
- ElastiCache Redis
- S3 + CloudFront
- Secrets Manager
- Route 53 + ACM

## 14.3 CI/CD Pipeline (GitHub Actions)

### PR pipeline (on each push to branch)
1. Checkout.
2. Setup .NET 8.
3. Restore dependencies.
4. Build in Release mode.
5. Lint (dotnet format --verify).
6. Run unit tests + integration tests with Testcontainers.
7. Test coverage published.
8. Dependency scan (Snyk or equivalent).
9. Secret scan (gitleaks).
10. Build Docker image to verify it builds.
11. Block merge if anything fails.

### `develop` pipeline (after merge)
1. Everything above.
2. Build + push Docker image tagged with commit SHA.
3. Automatic deploy to `dev` environment.
4. Post-deploy smoke tests.

### `main` pipeline (after merge with approval)
1. Everything from develop.
2. Automatic deploy to `staging`.
3. Manual approval gate.
4. Deploy to `prod` with rolling strategy.
5. Post-deploy monitoring (5 min) with automatic rollback on error rate spike.

### Nightly pipeline
1. Backup restore test in ephemeral environment.
2. Dependency update check.
3. Performance smoke test.

## 14.4 Branch strategy (adapted Gitflow)

```
main       ────────────────────────●───●──────  (prod)
                                  /   /
release    ───────●───────●─────/───/─────────  (staging fixed)
                 /       /     /   /
develop    ──●──●───●───●─────●───●──────────   (dev)
             \  \    \   \
feature      ●──●    ●    ●   (feature branches)
```

- `main` = prod.
- `develop` = dev environment, continuous integration.
- `feature/*` = feature branches.
- `release/*` = pre-prod stabilization.
- `hotfix/*` = urgent patches from main.

Commits in Conventional Commits in English.

## 14.5 Observability

### Stack
- **Logs:** Serilog → Grafana Loki or Better Stack Logs.
- **Metrics:** OpenTelemetry → Prometheus + Grafana.
- **Traces:** OpenTelemetry → Tempo or Honeycomb.
- **Errors:** Sentry.
- **Uptime:** Better Uptime or UptimeRobot.

### Mandatory dashboards
1. **Technical (for on-call):** latency p50/p95/p99, error rate, saturation, pending queues, DB connections.
2. **Integrations:** success rate per provider, expired tokens, pending activities.
3. **AI:** LLM latency, tokens consumed, cost per day, parsing error rate.
4. **Product (for founder):** MAU/WAU, suggestions generated, acceptance rate, activities ingested, in-app NPS.

### Alerts (example)
| Condition | Severity | Channel |
|-----------|----------|---------|
| Error rate > 2% in 5 min | High | Push + email |
| p95 latency > 3s in 10 min | Medium | Email |
| Pending queue > 1000 msgs | High | Push |
| LLM day cost > threshold | Medium | Daily email |
| Nightly backup failed | Critical | Immediate push |

## 14.6 Secrets management

- **Never in code or in versioned env vars.**
- **Local dev:** `.env.local` in `.gitignore`, example in `.env.example`.
- **Cloud:** Secrets Manager (AWS/Doppler/Railway secrets).
- **Automatic rotation** quarterly for critical credentials.
- **Least privilege access** + audit log.

## 14.7 Backup strategy

- **DB:** daily snapshots, 30-day retention; weekly snapshots retained 90 days.
- **Monthly restore test** in ephemeral environment (if you've never restored, you don't have a backup).
- **Object storage:** versioning enabled + lifecycle to cold storage after 30 days.
- **Cross-region backups** for DR (when the business justifies it).

---

# Level 15 — Roadmap and construction phases

## 15.1 Guiding principle

**"Vertical slices before horizontal layers."**

Don't build "all the infrastructure layer", then "all the domain layer". Build **the thinnest possible flow that works end-to-end**, then iterate widening it.

## 15.2 Proposed phases

### Phase 0 — Validation (weeks 1-2)

**No code.** Code in this phase is negative.

- Interviews with 8-10 real endurance coaches (Uruguay, Argentina, Spain).
- Questions centered on pain, not on solution.
- Document on paper: refined wedge, value proposition, willingness-to-pay.
- Informed go/no-go decision based on collected data.
- Recruitment of 2-3 coaches for private beta.

**Deliverable:** validation document with quotes, insights, tentative pricing.

---

### Phase 1 — Technical foundations (weeks 3-5)

**Plumbing only, no product feature.**

- Monorepo setup (backend + frontends).
- .NET solution with BuildingBlocks.
- Skeleton of 2 modules: Identity + TrainingData (the minimum for something to work end-to-end).
- CI/CD working: PR checks, automatic deploy to dev.
- Docker Compose for local development.
- Base observability (Serilog, Sentry, healthchecks).
- Coach dashboard empty but authenticated.
- Athlete PWA empty but authenticated.
- Database with initial migrations.
- Unit and integration tests with real coverage.

**Deliverable:** an athlete can log in, connect Strava (mock for testing), and see an empty dashboard. Deployed in dev.

**Portfolio story:** "Architected foundation: Clean Architecture, CI/CD, observability, auth — all working before writing a single business feature."

---

### Phase 2 — Core ingestion and visualization (weeks 6-9)

**Minimum viable vertical flow.**

- Real Strava integration (OAuth + activity sync).
- Anticorruption layer for Strava with contract tests.
- Outbox pattern working.
- Event bus between modules.
- Coach dashboard shows list of athletes with last activity.
- Athlete view on dashboard with activity timeline.
- Athlete PWA shows their activities.

**Deliverable:** a coach invites an athlete, the athlete connects Strava, their activities appear on the coach's dashboard. Functional end-to-end.

**Private beta 1 starts:** 1-2 trusted coaches test.

---

### Phase 3 — Planning (weeks 10-13)

- Coaching module: create plan, view plan, edit plan.
- Automatic activity ↔ planned session matching.
- SessionExecution with subjective athlete feedback in the PWA.
- Plan versioning working.
- Coach library (basic templates).
- Basic notifications (web push + email fallback).

**Deliverable:** complete coaching flow without AI yet. Already usable as "premium Excel".

**Private beta 2:** 3-5 coaches, 20-30 real athletes.

---

### Phase 4 — Intelligence (weeks 14-17)

- Intelligence module with nightly readiness calculation (no AI, physiological formulas).
- Integration with Anthropic Claude.
- Weekly suggestion generation for the coach.
- Prioritized dashboard ("these 3 athletes need attention").
- Suggestion application to plan (with coach review).
- Learning loop: tracking coach feedback.

**Deliverable:** the product's "magic moment" works. Coach opens dashboard on Monday and receives actionable suggestions.

**Early-access public beta:** 10-20 coaches paying early-bird (reduced price).

---

### Phase 5 — Polish and integration expansion (weeks 18-22)

- Integration with Garmin Connect (official OAuth or library with migration plan).
- Integration with Polar (if demand).
- UX improvements based on feedback.
- Performance tuning: indexes, caches, heavy queries.
- Mature production observability.
- Security hardening (light pentesting).
- Legal compliance: terms, privacy policy, DPAs.

**Deliverable:** stable product for commercial GA.

---

### Phase 6 — Monetization (weeks 23-26)

- Billing module with Stripe.
- Plans: starter (up to 10 athletes), pro (up to 30), enterprise (unlimited + features).
- 14-day trials.
- Stripe webhooks.
- Self-service upgrade/downgrade/cancel.
- Tax billing Uruguay (initial) + Argentina/Spain.

**Deliverable:** first real charges. Real business metrics.

---

### Phase 7+ — Scale and expansion (month 7+)

- More providers (COROS, Suunto, Wahoo).
- Multi-language (Brazilian Portuguese, Spanish).
- Enriched communication module (async chat, audio notes).
- Own ML model for injury prediction.
- Features for clubs/teams.
- Native iOS/Android app if demand.
- Coach marketplace (coaches find athletes).

## 15.3 Definition of Done per phase

A phase is done when:
- All tests pass in CI.
- Coverage within targets.
- Deploy to staging successful.
- Basic business metrics visible in dashboard.
- Documentation updated (README + this doc + ADRs).
- At least one real user tested the new features.

## 15.4 Signals for not moving to the next phase

- Feature from current phase not used by beta testers.
- Critical bugs unresolved.
- Tech debt growing faster than added value.
- Coaches churning without understood reason.

## 15.5 Explicit non-goals in year 1

To maintain focus, these things are **not** built:

- Native iOS/Android app.
- Video analysis / computer vision.
- Nutrition / meal planning.
- Social feed / community.
- Marketplace.
- Multi-sport support outside endurance (strength, team sports).
- Mass self-service onboarding (everything is invite-only in year 1).

---

## Appendix A — Glossary

| Term | Definition |
|------|-----------|
| **Coach** | Primary paying user. Professional trainer managing athletes remotely. |
| **Athlete** | Consumer user. Person who executes the coach's plans. |
| **Tenant** | Isolation scope. One coach (or organization) = one tenant. |
| **Training plan** | Temporal structure of sessions assigned to an athlete. |
| **Planned session** | Session the coach prescribed. |
| **Executed session** | What the athlete actually did, derived from an activity. |
| **Activity** | Record of a workout from a wearable. |
| **Readiness** | Daily athlete preparedness score for training. |
| **TSS / CTL / ATL / TSB** | Training Stress Score / Chronic / Acute Training Load / Training Stress Balance. Standard load metrics. |
| **HRV** | Heart Rate Variability. Heart rate variability, recovery indicator. |
| **Suggestion** | AI-generated recommendation for the coach to evaluate. Never applied alone. |
| **Bounded context** | Subdomain with its own language and model (DDD). |
| **Aggregate** | Cluster of entities with a root that guarantees invariants (DDD). |
| **Outbox pattern** | Pattern for consistency between DB and published events. |
| **ACL** | Anticorruption Layer. Layer that translates external models to internal model. |

## Appendix B — Recommended technical references

**DDD and architecture:**
- "Implementing Domain-Driven Design" — Vaughn Vernon.
- "Domain-Driven Design Distilled" — Vaughn Vernon.
- "Learning Domain-Driven Design" — Vlad Khononov.
- Eventstorming.com — Alberto Brandolini.

**Clean Architecture:**
- "Clean Architecture" — Robert C. Martin.
- ".NET Microservices: Architecture for Containerized .NET Applications" — Microsoft (free book).

**Sports physiology (critical for the domain):**
- "The Science of Running" — Steve Magness.
- "Training and Racing with a Power Meter" — Hunter Allen, Andrew Coggan.
- "Faster Road Racing" — Pete Pfitzinger.

**LLMs in production:**
- "Designing Machine Learning Systems" — Chip Huyen.
- Anthropic documentation on prompt engineering and structured outputs.

## Appendix C — Immediate next steps after reading this document

1. Create empty repository with this document as `/docs/ARCHITECTURE.md`.
2. Open `/docs/adr/` and start registering ADRs (first ADR: "Why modular monolith").
3. Start Phase 0: schedule 3 interviews with coaches for this week.
4. In parallel: setup minimum monorepo (step 1 of Phase 1) to warm up.
5. Choose placeholder name and buy cheap domain (~15 USD/year) to avoid blocking on branding.

---

**End of document.**

*This is a living document. Every important decision is reflected here or in an associated ADR. If something changes in the product or the domain, this is updated first.*
