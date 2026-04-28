# ADR-0005 — Direct Anthropic SDK, no LangChain or AI orchestration framework

- **Status:** Accepted
- **Date:** 2026-04
- **Related:** ADR-0003 (TypeScript stack), ADR-0004 (REST APIs)

## Context

AthleteOS has AI as a core product capability:

- A conversational assistant scoped to an athlete's data (chat with the coach).
- Automatic suggestion generation (Phase 2): scheduled analysis that surfaces athletes needing attention.

A decision is needed on how to integrate LLM capabilities. The choice influences code complexity, debugging, performance, vendor lock-in, and the engineer's depth of understanding of the AI primitives.

## Options considered

### Option A — Direct Anthropic SDK + Zod for structured outputs (chosen)

Use `@anthropic-ai/sdk` directly. Define a domain port `ICoachAssistant` and implement it with the SDK. Use Anthropic's native function calling (tools) for the LLM to retrieve additional data on demand. Validate structured outputs with Zod schemas.

**Pros:**
- Minimal abstraction. Code maps directly to Anthropic's documented primitives.
- Easy to debug: every LLM call is a function call we wrote.
- Full control over prompts, context, retries, error handling.
- No framework to learn beyond the SDK itself.
- Performance: no wrapper overhead.
- Vendor swap is local: replace one file (`AnthropicCoachAssistant`) with another implementation of the same interface.
- The skills learned (prompt engineering, function calling, structured outputs) transfer to OpenAI, Google, Mistral — they all converged on the same patterns.

**Cons:**
- We write a few utilities ourselves (retry on validation failure, conversation history management, tool call dispatch). Each is small.
- No "agent framework" magic — but that's also a pro.

### Option B — LangChain (rejected)

A framework abstracting prompts, chains, agents, memory, retrieval, and tools.

**Pros:**
- Pre-built primitives for many patterns.
- Active ecosystem.

**Cons:**
- Industry sentiment in 2024-2026 has shifted strongly against LangChain for production. Public post-mortems from companies that removed it (e.g. Octomind) cite leaky abstractions, debugging difficulty, and performance overhead.
- Wrapping the SDK obscures what the LLM actually receives, making prompt iteration slower.
- The framework's "easy mode" is fine for demos and breaks in production.
- Updating LangChain often breaks our code (it has had multiple major rewrites: LangChain → LangChain Expression Language → LangGraph).
- Anthropic's own engineering posts and SDK examples do not use LangChain.
- A junior developer learning LangChain learns LangChain, not how LLMs work. We want the second.

### Option C — Vercel AI SDK as primary abstraction (partially adopted)

The `ai` package by Vercel offers streaming utilities, structured outputs, and a unified provider interface.

**Pros:**
- Lightweight (much more so than LangChain).
- Excellent for streaming chat UI to React.
- Provider-agnostic (works with Anthropic, OpenAI, etc.).

**Cons:**
- Adds an abstraction layer on top of the official SDK.
- The provider-agnostic angle is a benefit only if we plan to switch — and our port pattern already gives us that.

**Verdict:** the Vercel AI SDK may be adopted **on the frontend only** for streaming chat UI ergonomics. The backend still uses the Anthropic SDK directly. It's not a framework decision; it's a UI utility.

### Option D — Hugging Face Inference API or self-hosted models (rejected for MVP)

Use open-source models via Hugging Face's Inference API, or self-host them.

**Pros:**
- Lower per-call cost at scale.
- No vendor dependency for inference.
- Access to specialized models (embeddings, classifiers, etc.).

**Cons:**
- Open-source LLMs at MVP scale are weaker than Claude for complex reasoning over training data.
- Self-hosting is a significant operational burden for a single developer.
- The cost difference is irrelevant at <100 coaches.
- Embeddings and specialized models are not needed in MVP (athlete data fits in context window).

**Verdict:** revisit when we have evidence (cost, latency, or capability) that justifies the move. Not now.

## Decision

**Option A — Direct Anthropic SDK with Zod for structured outputs, optional Vercel AI SDK on the frontend for streaming.**

Concretely:

- Backend `coaching` module defines `ICoachAssistant` interface in `domain/ports/`.
- `AnthropicCoachAssistant` in `infrastructure/ai/` implements it using `@anthropic-ai/sdk`.
- Tools (function calling) are defined per use case and dispatched to use case methods.
- All structured LLM outputs are validated against Zod schemas; on failure, retry up to 2x with the validation error fed back into the prompt.
- Every LLM call is logged with token counts, latency, model, and a hard cost cap per coach per day.

## Consequences

### Positive

- Code is readable by anyone who has read the Anthropic SDK docs.
- AI capabilities are first-class but not coupled to a specific provider.
- Skills developed (prompt engineering, function calling, structured outputs, evaluation) are universally applicable.
- Performance is as good as it gets — no wrapper overhead.

### Negative

- We write small utilities ourselves (retry, conversation pruning, tool dispatch). Probably ~200 lines total. Acceptable.
- We don't get "agents" or "graph" abstractions for free. Acceptable — we don't need autonomous agents; we have a coach in the loop.

### What this decision rules out

- No LangChain anywhere in the codebase.
- No Hugging Face client in MVP.
- No self-hosted LLM in MVP.
- No proprietary AI framework whose internals we can't inspect.

### Future-friendly

When/if we add:

- **Embeddings** for semantic search → use Anthropic embeddings or OpenAI embeddings directly into pgvector. No framework needed.
- **Multi-provider failover** → handled at the port layer (a `MultiProviderCoachAssistant` that wraps two providers).
- **Vector retrieval (RAG)** → handled in the use case, fetching from pgvector and including snippets in the prompt. No framework needed.
- **Evals** → write our own using Vitest + a small dataset of (input, expected) pairs. No framework needed.

If a real, concrete need for agent orchestration, multi-step reasoning graphs, or complex retrieval pipelines emerges, we will revisit and possibly adopt a focused tool (LangGraph, LlamaIndex, Mastra) at that time. We will not preemptively pay the cost.

## References

- Anthropic SDK (TypeScript): https://github.com/anthropics/anthropic-sdk-typescript
- Anthropic tool use (function calling): https://docs.claude.com/en/docs/build-with-claude/tool-use
- Zod: https://zod.dev
- Vercel AI SDK: https://sdk.vercel.ai
