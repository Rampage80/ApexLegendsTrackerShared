# Apex Legends Chat Agent — Implementation Plan

This is a standalone implementation brief for an AI coding agent (e.g. Codex) working
across the relevant repositories:

- **GameStats service**: game-stat and player-lookup APIs (ASP.NET Core Web API, .NET 10)
- **AIChat service**: LLM-backed Apex chat APIs (ASP.NET Core Web API, .NET 10)
- **Frontend**: `ApexLegendsTrackerWeb` (Blazor WebAssembly, .NET 10)
- **Shared contract package**: `ApexLegendsTrackerShared` (`ApexLegendsTracker.Shared`
  NuGet package consumed by both, versioned via `<Version>` in its `.csproj`)

Read each repo's `.github/copilot-instructions.md` and `docs/api-contract.md` before
making changes — they define existing conventions (structured logging, telemetry,
test coverage, accessibility, additive-only contract changes) that this feature must
follow.

## Goal

### Service names

- **AIChat service** handles LLM-backed chat, the knowledge pack, caching, topic
  gating, and the chat endpoint.
- **GameStats service** owns game-stat and player-lookup functionality. It is not
  part of the AIChat request path in v1.

Add a chat box to the Web home page where a player can ask Apex Legends questions.
For v1, every question is handled by a single path: a cached, small/cheap LLM
scoped to Apex Legends knowledge (tips, legends, weapons, maps, ranked mechanics).

**Player-specific questions** (e.g. "what's my rank") are **out of scope for v1**.
The existing live player-lookup API is not wired into chat yet — see "Future work:
player-lookup path" below for how to add it later without reworking this design.

The feature must be lightweight and cheap to run: minimize LLM calls, cache
repeated questions, and rate-limit usage.

## Model recommendation

Use **`gpt-4o-mini`** for v1 (or the equivalent small, non-reasoning chat model
available from the selected provider). It is the right quality/cost balance for
short coaching responses: capable of following a constrained prompt and writing
useful, natural advice without the latency or cost of a flagship reasoning model.

Do not use a reasoning model, web-search tool, or a large-context model for this
workflow. They add latency, cost, and unbounded/unstable source material without
improving the core player-coaching experience. A smaller nano-class model can be
considered later for deterministic tasks such as intent classification, but v1's
rule-based classifier makes that cost unnecessary; it is likely too weak for
reliable game-specific coaching answers.

Answers must be short: target 100-180 words, with an enforced maximum of 250
output tokens. This is a coaching aid, not a general-purpose conversational agent.

## Non-goals / explicit constraints

- **No distributed cache for this feature right now** (cost). Build the cache
  behind an abstraction so it can be swapped to a distributed backend (e.g. Redis)
  later **without changing any call sites** — see "Cache abstraction" below.
- **Never call the LLM (or store its API key) from the Blazor WebAssembly client.**
  All LLM calls happen server-side in the Service. The key lives only in Service
  configuration/user-secrets/App Service settings — never committed to source.
- Do not log full chat message text or LLM prompts/responses in telemetry or
  structured logs. Log only metadata: intent category, success/failure, latency,
  cache hit/miss.
- Do not change the existing `/api/v1/players`, `/api/v1/map-rotation`, or
  `/api/v1/predator-thresholds` contracts.
- Multi-turn conversation memory is **out of scope for v1** — treat each chat
  message as a stateless, single-turn request. (Design should not preclude adding
  history later, but don't build it now.)
- The LLM does **not** browse the web or call external search tools. Game facts
  come only from the curated, versioned Apex knowledge pack described below.
- "Apex-only" means both: a deterministic topic gate rejects unrelated requests
  before an LLM call, and the LLM is instructed to answer only from the supplied
  Apex context. Prompt wording alone is not a sufficient boundary.

## 1. Cache abstraction (AIChat service, new, reusable)

Create a small cache abstraction so today's in-memory cache can become distributed
later by swapping one registration, with no call-site changes.

`ApexLegendsTracker.Service/Caching/ICacheProvider.cs`:

```csharp
namespace ApexLegendsTracker.Service.Caching;

public interface ICacheProvider
{
    Task<T> GetOrCreateAsync<T>(
        string key,
        TimeSpan duration,
        Func<CancellationToken, Task<T>> factory,
        CancellationToken cancellationToken = default);
}
```

`ApexLegendsTracker.Service/Caching/MemoryCacheProvider.cs`:

- Wraps `Microsoft.Extensions.Caching.Memory.IMemoryCache`.
- This is the **only** implementation registered for now (`AddSingleton<ICacheProvider, MemoryCacheProvider>()`).

Later (not part of this task), a `DistributedCacheProvider` implementing the same
interface over `IDistributedCache` (JSON-serializing `T`) can be registered instead
— no consumer code changes required. Document this intent with a one-line comment
on the interface, not a multi-paragraph doc comment.

Do **not** touch the existing `IMemoryCache` usage in `ApexApiClient.GetCachedAsync`
or the existing Redis `IDistributedCache` usage for map-rotation/Predator — those
are unrelated to this feature. Only the new chat/knowledge path uses `ICacheProvider`.

## 2. Shared contract (`ApexLegendsTrackerShared` repo)

Additive-only change. Bump `<Version>` in `ApexLegendsTrackerShared.csproj` (next
minor version) after adding:

```csharp
namespace ApexLegendsTracker.Shared;

public enum ChatSource
{
    Knowledge
}

public sealed record ChatRequest(string Message);

public sealed record ChatResponse(string Reply, ChatSource Source);
```

- `ChatSource` is a single-value enum for now so the response shape doesn't need to
  change when the player-lookup path is added later (see "Future work" below) —
  adding `Player` then is additive.
- Keep names/casing consistent with existing shared DTOs (`PlayerLookupResult`,
  `MapRotationResponse`, etc.).
- `dotnet build`/`dotnet pack` the Shared project to regenerate the local NuGet feed,
  then bump the `PackageReference` version in both the Service and Web `.csproj`
  files and restore.

## 3. AIChat service

### 3.1 Options

`ApexLegendsTracker.Service/Options/ApexKnowledgeOptions.cs`:

```csharp
public sealed class ApexKnowledgeOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4o-mini"; // cheap/small model tier
    public int MaxOutputTokens { get; set; } = 300;
}
```

Bind from configuration section `"ApexKnowledge"`. Populate `ApiKey` via user
secrets locally and App Service configuration in deployed environments — follow the
same pattern already used for `ApexApiOptions`/`ApiKey`. Fail fast (throw
`InvalidOperationException`) if `ApiKey` is missing when the knowledge path is
actually invoked, mirroring `ApexApiClient.GetAsync`'s existing check.

### 3.2 Knowledge responder — LLM call, cached

`ApexLegendsTracker.Service/Services/Chat/IApexKnowledgeService.cs` /
`ApexKnowledgeService.cs`:

- Constructor takes `HttpClient`, `IOptions<ApexKnowledgeOptions>`, `ICacheProvider`.
- `Task<string> AskAsync(string message, CancellationToken ct)`:
  1. Normalize the question (trim, lowercase, collapse whitespace) to build a
     cache key: `$"apex-knowledge:{NormalizedQuestionHash}"`.
  2. `ICacheProvider.GetOrCreateAsync` with a **24 hour** duration wrapping the
     actual LLM call — most repeat questions (tips/meta questions) should never
     re-hit the LLM.
  3. On cache miss, call the LLM chat-completion endpoint with:
     - A fixed system prompt scoping answers to Apex Legends gameplay, tips,
       weapons, legends, maps, and ranked mechanics; instructing it to say it
       doesn't know rather than invent facts, and to refuse unrelated topics
       politely.
     - `MaxOutputTokens` cap from options.
     - No conversation history (single message).
  4. Return the model's text reply.
- Wrap upstream HTTP failures the same way `ApexApiClient` does — throw
  `HttpRequestException` with a clear message and appropriate status code; let the
  controller convert that into a `502`-style response with a trace identifier,
  consistent with the existing status endpoints' error convention.

### 3.3 Apex-only knowledge preparation and retrieval

Do not fine-tune a model or introduce a vector database in v1. Both add operating
cost and maintenance without enough content volume to justify them. Instead, build
a small, human-curated, versioned knowledge pack in the AIChat service repository:

```
ApexLegendsTracker.Service/Knowledge/
  apex-knowledge.json
  ApexKnowledgeDocument.cs
  IApexKnowledgeRetriever.cs
  ApexKnowledgeRetriever.cs
  IApexTopicGate.cs
  ApexTopicGate.cs
```

`apex-knowledge.json` is a compact structured document set, reviewed and updated
when the game changes. Each document has `id`, `title`, `keywords`, `content`,
`patchOrSeason`, and `lastReviewedUtc`. Keep content factual and concise. Start
with 20-40 documents covering: core movement, legend roles, ranked fundamentals,
team composition, rotation, positioning, aim practice, inventory/healing, common
weapon classes, and map-mode fundamentals. Do not hardcode volatile weapon damage,
rotation timers, or current meta rankings unless the document carries a patch/season
label and a review date.

`ApexTopicGate` is deterministic and runs before retrieval/LLM use:

1. Normalize the message and match it against an allow-list of Apex terms (for
   example `Apex Legends`, legend names, weapons, ranked, RP, drop, rotation,
   battle royale, and the known maps/modes).
2. For no match, return a fixed local reply: "I can help with Apex Legends gameplay,
   legends, ranked, weapons, and maps." Do not call the LLM.
3. Add a conservative deny-list for prompt-injection phrases requesting ignored
   instructions, system prompts, or unrelated assistance. Return the same local
   reply without calling the LLM.

`ApexKnowledgeRetriever` uses an in-memory keyword score, not embeddings:

1. Tokenize the normalized question.
2. Score documents by keyword overlap; favor exact title/keyword matches.
3. Supply the top 2-3 documents only, with a strict context limit (for example
   1,200 input tokens total) to `ApexKnowledgeService`.
4. If no document clears a minimum score, return a fixed local clarification asking
   the player to rephrase as an Apex gameplay question. Do not fall back to the
   model's latent knowledge in v1.

This makes the initial agent materially Apex-only: it cannot use browsing, receives
only selected Apex reference text, and has a deterministic no-call path for unknown
or unrelated questions. It also keeps every request bounded in time and tokens.

Update `ApexKnowledgeService` accordingly:

- Run `IApexTopicGate` and `IApexKnowledgeRetriever` before the cache/LLM call.
- Cache only successful LLM answers, using a key made from the normalized question
  **and the knowledge-pack version**. Bumping the pack version naturally invalidates
  stale answers after a game update.
- Send a compact system prompt: "Answer only the Apex Legends question using the
  supplied reference. Do not use outside knowledge. If the reference is insufficient,
  say so. Give concise, practical advice; do not state time-sensitive balance facts
  without the reference's patch label." Then send the selected reference text and
  the player message.
- Set `temperature` to a low value (for example `0.2`) and max output to 250 tokens
  (configure `MaxOutputTokens` with default `250`).
- Do not include raw question text in logs, telemetry, or cache keys. Use a SHA-256
  hash of the normalized question as the cache-key suffix.

### 3.4 Controller

`ApexLegendsTracker.WebAPI/Controllers/ChatController.cs`:

- `POST /api/v1/chat` accepting `ChatRequest`, returning `ChatResponse`.
- Calls `IApexKnowledgeService` directly (no orchestrator/classifier needed while
  there is only one path).
- Validate: reject empty/whitespace `Message`; cap message length (e.g. 500 chars)
  and return `400` above that, mirroring the existing platform-validation style in
  `PlayersController`.
- Add basic **rate limiting** (ASP.NET Core's built-in `Microsoft.AspNetCore.RateLimiting`
  middleware is sufficient) on this route specifically — e.g. a fixed-window limiter
  per client (IP or a lightweight session id) capping requests per minute, so a
  single user can't drive up LLM cost. Return `429` when exceeded.
- Register `ICacheProvider`/`MemoryCacheProvider`, `IApexKnowledgeService`, and a
  named/typed `HttpClient` for the LLM in `Program.cs`, following the existing
  registration style for `ApexApiClient`.

### 3.5 Structured logging & telemetry (backend)

- Log at `Information` when a chat request is handled (cache hit/miss, latency),
  `Warning` for rate-limit rejections or topic-gate rejections, `Error` for
  upstream LLM failures. **Never log the raw message text or the LLM reply.**
- If Application Insights custom events are already used elsewhere in the Service,
  add analogous ones here (event names only, no message content).

## 4. Frontend (`ApexLegendsTrackerWeb`)

### 4.1 API client

Add to `IApexTrackerApiClient` / `ApexTrackerApiClient.cs`:

```csharp
Task<ChatResponse> SendChatMessageAsync(ChatRequest request, CancellationToken cancellationToken = default);
```

- POST to `api/v1/chat` using the same `GetAsync<T>`-style helper pattern already
  in `ApexTrackerApiClient` (error handling, empty-body check).
- Fire telemetry events via the existing `apexTelemetry.trackEvent` JS interop
  (`ChatRequested`, `ChatSucceeded`, `ChatFailed`) — pass intent/source and error
  category only, never the message text, matching how `PlayerLookupFailed` already
  avoids logging sensitive payloads.

### 4.2 UI component

New `ApexLegendsTracker.Web/Pages/Home.razor` section or extracted
`Shared/ChatPanel.razor` component:

- Message list rendered as a scrollable log; container has `role="log"` and
  `aria-live="polite"` so new messages are announced to screen readers without
  stealing focus.
- Text `<input>`/`<textarea>` with a proper `<label>` (visually hidden is fine),
  a submit button, and Enter-to-send keyboard support.
- Disable input and show a loading state while awaiting the response (same pattern
  as the existing `_isLoading`/`SearchAsync` on `Home.razor`).
- Surface errors inline (reuse `.search-error` style) instead of throwing.

## 5. Testing requirements

**AIChat service:**
- `ApexKnowledgeService` tests: cache hit avoids a second HTTP call (mock
  `HttpMessageHandler`), cache key normalization (same question, different
  casing/whitespace → same cache entry), pack-version cache invalidation, context
  cap, and upstream failure surfaces as `HttpRequestException`.
- `ApexTopicGate` tests: Apex terms are accepted; unrelated questions and prompt
  injection attempts receive the local Apex-only reply without invoking retriever
  or LLM.
- `ApexKnowledgeRetriever` tests: relevant documents rank first, no relevant
  document does not invoke the LLM, and selected content remains under the context
  limit.
- `ChatController` tests: empty/too-long message → `400`; rate limit → `429`;
  happy path returns a cached/LLM reply.

**Web:**
- Component test for the chat panel: render, type + submit, loading state, and
  error state.
- Playwright end-to-end happy path: type a generic knowledge question, see a
  reply render.
- Accessibility check: input has an accessible name, live region present, and
  keyboard-only submission works (Enter key), consistent with the repo's WCAG
  requirement.

## 6. Rollout checklist

1. Shared: add `ChatRequest`/`ChatResponse`/`ChatSource`, bump version, pack.
2. AIChat service: cache abstraction, options, knowledge pack/topic gate/retriever,
   AIChat responder, controller, DI registrations, rate limiting, tests,
   `docs/api-contract.md` update.
3. Web: package bump, `IApexTrackerApiClient` method, `ChatPanel` UI, telemetry
   events, tests, `docs/api-contract.md` update.
4. Configure the LLM API key in Service local user-secrets and (when deploying)
   App Service configuration — never commit it.
5. Validate: `dotnet build`/`dotnet test` in both solutions; manual smoke test of
   the chat path locally before deploying.

## 7. Future work: player-lookup path

Not part of this task. When ready to add player-specific answers (e.g. "what's my
rank"):

- Add `Player` back to the `ChatSource` enum (additive) and reintroduce a
  deterministic `IChatIntentClassifier` (self-referential phrase matching, e.g.
  "my rank", "my stats") plus a thin orchestrator so the controller picks between
  the player responder and `IApexKnowledgeService`.
- Add an `IPlayerChatResponder` that requires `PlayerName`/`Platform` on
  `ChatRequest`, calls the existing `IApexApiClient`/`PlayersController` lookup
  path directly (no duplicate upstream logic), and builds a short templated reply
  from the structured result — no LLM involved, zero added AI cost.
- Wire the Web's `PlayerLookupState.Result` into `ChatRequest.PlayerName`/`Platform`
  automatically so returning users aren't re-asked for identity.
- This design (single-path v1 with an additive enum) was chosen specifically so
  this addition requires no breaking shared-contract changes.
