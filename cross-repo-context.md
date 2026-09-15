# ApexLegendsTracker cross-repository context

## Current state
- UI repo present: `ApexLegendsTrackerWeb`.
- GameStats service repo present: `ApexLegendsTrackerService`.
- The UI is a Blazor WebAssembly frontend using `HttpClient` with `ApiBaseUrl` from configuration.
- The backend exposes `GET /api/v1/players/{platform}/{playerName}`.
- The API contract is now confirmed in the backend implementation, and the UI should remain aligned with it.

## Confirmed API contract
- Route: `GET /api/v1/players/{platform}/{playerName}`
- Valid platforms: `PC`, `PS4`, `X1`
- Request validation: `playerName` is required; invalid platform returns `400`
- Success response: `200 OK` with `PlayerLookupResult`
- Shape: `PlayerName`, `Platform`, `RawJson`
- Error style: `400` for invalid inputs, upstream failures are surfaced as status codes with a trace identifier

## Known contract alignment issues
- UI code currently calls `api/v1/players/{encodedPlatform}/{encodedPlayerName}` and expects a `PlayerLookupResult` payload.
- Backend route expects the same shape and supports the same valid platform values.
- Front-end config is straightforward, but the backend app must also allow the web app origin via CORS for local/browser testing.
- The backend currently uses `Cors:AllowedOrigins` and a `WebClientCorsPolicy` in its startup, which needs to be configured for the frontend origin in local and deployed environments.

## Environment-specific API URL (UI)
- UI is a standalone Blazor WebAssembly app (`WebAssemblyHostBuilder`), published as static assets and deployed to the Azure Web App `ApexLegendsTracker` via `.github/workflows/master_apexlegendstracker.yml`.
- `Program.cs` reads `ApiBaseUrl` from configuration; Blazor WASM automatically layers `wwwroot/appsettings.{Environment}.json` over `wwwroot/appsettings.json` based on the hosting environment (Development locally via `dotnet run`, Production when served statically from Azure App Service) — no env vars or code changes needed.
- Added `ApexLegendsTracker.Web/wwwroot/appsettings.Production.json` with `ApiBaseUrl` = `https://apexlegendstrackerservice-fbfjfgbwhpffexfx.centralus-01.azurewebsites.net/` so the deployed UI targets the deployed backend App Service; local `appsettings.json` keeps `http://localhost:5165/`.
- Open item: backend CORS (`Cors:AllowedOrigins`) must include the deployed UI's origin (Azure Web App `ApexLegendsTracker` URL) in production — could not verify because the backend repo is not present in this workspace.

## Modernization direction
- Target architecture: AWS EKS with containerized .NET services, API gateways/load balancers, managed backing services, and autoscaling.
- Observability baseline: OpenTelemetry + Prometheus + Grafana (preferred free stack), with New Relic free-tier as an alternative SaaS option.
- Resilience baseline: retry with exponential backoff, timeouts, circuit breaker, rate limiting, and health checks.
- Quality baseline: automated tests, linting, SAST, IaC validation, and container scanning.

## Modernization direction
- Target architecture: AWS EKS with containerized .NET services, API gateways/load balancers, managed backing services, and autoscaling.
- Observability baseline: OpenTelemetry + Prometheus + Grafana (preferred free stack), with New Relic free-tier as an alternative SaaS option.
- Resilience baseline: retry with exponential backoff, timeouts, circuit breaker, rate limiting, and health checks.
- Quality baseline: automated tests, linting, SAST, IaC validation, and container scanning.

## Open coordination work
- Add the backend API repository or define the contract more formally.
- Create Kubernetes manifests and Helm values for the app and dependencies.
- Add telemetry instrumentation, dashboards, and alerting.
- Decide whether the showcase includes AI/haystack search features or remains a pure API resilience and cloud-native observability showcase.
- Renamed the backend project structure to WebAPI and Service naming for consistency with the contract and repo layout.

## Planned feature: Apex chat agent (not yet implemented)
- Plan drafted for a home-page chat box: player-specific questions route to the GameStats service's existing player-lookup API (no LLM), while generic Apex knowledge questions route to a small/cheap LLM call made server-side by the AIChat service (never from the WASM client).
- Full implementation brief for a coding agent: `ApexLegendsTrackerShared/docs/apex-chat-agent-plan.md`. Covers a new `ICacheProvider` abstraction (in-memory `MemoryCacheProvider` now, swappable to a distributed implementation later without changing call sites — deliberately not using the existing Redis distributed cache for this feature, due to cost), additive `ChatRequest`/`ChatResponse`/`ChatSource` shared DTOs, a new `/api/v1/chat` Service endpoint (intent classifier, player responder, cached knowledge responder, rate limiting), and a new Web `ChatPanel` UI.
- Open decisions still unresolved: which LLM provider/API key to use, and rate-limit thresholds/budget — plan uses placeholders/defaults (stateless single-turn, cheap small-model tier, 24h answer cache, per-client rate limit) pending user confirmation.
- No code has been written for this feature yet in either repo.
- Model and knowledge strategy review: recommend `gpt-4o-mini` (or an equivalent small non-reasoning chat model) over a nano-class model for concise coaching answers; retain deterministic rule-based classification so no model is used for routing. Do not use browsing, a reasoning model, fine-tuning, embeddings, or a vector database in v1.
- Apex-only context plan: a deterministic topic/injection gate locally rejects non-Apex requests without an LLM call; a small versioned JSON knowledge pack is ranked with keyword matching; only the best 2-3 curated documents (under a strict context limit) are supplied to the LLM. Knowledge answers cache by normalized-question hash plus knowledge-pack version, so pack updates invalidate stale answers.
- Scope reduction: v1 has a single path only (cache + small LLM knowledge responses). The player-lookup API path (routing "what's my rank"-style questions to the existing player endpoint) is explicitly deferred; `ChatSource` ships as a single-value enum now so adding `Player` later is additive, not a breaking contract change.

## Chat agent implemented (v1)
- Shared: `ChatRequest`/`ChatResponse`/`ChatSource` added; package bumped `1.6.0` -> `1.7.0`. New telemetry constants `ChatRequested`/`ChatSucceeded`/`ChatFailed`.
- Service: `ICacheProvider`/`MemoryCacheProvider` (in-process, swappable later), `ApexKnowledgeOptions` (`ApexKnowledge:BaseUrl`/`Model`/`MaxOutputTokens`/`ApiKey`), a curated versioned knowledge pack (`Knowledge/apex-knowledge.json`, 8 documents), `ApexTopicGate` (deterministic allow/deny-list, rejects off-topic and prompt-injection input before any LLM call), `ApexKnowledgeRetriever` (word-boundary keyword scoring, top 2-3 documents), `ApexKnowledgeService` (24h cache keyed by normalized-question hash + knowledge-pack version, calls an OpenAI-compatible `chat/completions` endpoint), `ChatController` (`POST /api/v1/chat`, 500-char cap, fixed-window rate limiting at 10 req/min/instance via `Microsoft.AspNetCore.RateLimiting`). `ChatSource` now serializes as a string (`JsonStringEnumConverter` added to controller JSON options).
- Web: `IApexTrackerApiClient.SendChatMessageAsync`, a new `ChatPanel.razor` component (accessible log region, labeled input, loading/error states) added to `Home.razor`, chat telemetry events (never logging raw message text), and a `JsonStringEnumConverter`+case-insensitive options for reading `ChatResponse`.
- Validation: Service solution (`ApexLegendsTracker.Service.Tests` + `ApexLegendsTracker.WebAPI.Tests`) - 33/33 tests pass. Web solution builds; new chat client tests pass (2/2); the pre-existing unrelated `ExampleAPIJsonReturns.json` fixture-missing failure in `UnitTest1` is untouched by this change.
- Both repos' `NuGet.Config` local feed sources were re-enabled to restore the `1.7.0` package for local validation (previously commented out).
- **Outstanding: the user must supply real GPT/OpenAI authentication.** `ApexKnowledge:ApiKey` is unset in both `appsettings.json` and Production config; it must be provided via local user secrets (`dotnet user-secrets set "ApexKnowledge:ApiKey" "..."` in `ApexLegendsTracker.WebAPI`) and Azure App Service configuration for the deployed Service. `ApexKnowledge:BaseUrl` defaults to `https://api.openai.com/v1/` and `Model` to `gpt-4o-mini`; both are overridable via configuration if a different OpenAI-compatible provider is used.
- Added Azure AI Foundry/Azure OpenAI support to `ApexKnowledgeOptions`/`ApexKnowledgeService`: `UseApiKeyHeader` (sends `api-key` header instead of `Authorization: Bearer`) and `ApiVersion` (appends `?api-version=...`) are both opt-in via configuration, no code changes needed to switch providers. For Foundry, `Model` should be the deployment name. Still need from the user: the Foundry resource endpoint (`BaseUrl`), deployment name (`Model`), `ApiVersion`, and the API key.
- Finalized on Azure OpenAI (not Foundry). User provided the resource endpoint (`https://apexlegendstrackeropenai2.openai.azure.com/`) and stores the API key in a custom environment variable `APEXSERVICE_OPENAIKEY` (not the standard `ApexKnowledge__ApiKey` binding). Implemented: Azure requests now route through `openai/deployments/{Model}/chat/completions?api-version=...` and omit `model` from the request body (the deployment already selects it — `ApexKnowledgeOptions.ApiKey` changed from `init` to a settable property to support this). `Program.cs` reads `APEXSERVICE_OPENAIKEY` explicitly (mirroring the existing `APEXSERVICE_APPINSIGHTS_CONNECTION_STRING` pattern) and `PostConfigure`s `ApexKnowledgeOptions.ApiKey` from it. `appsettings.json` now has `ApexKnowledge:BaseUrl`/`UseApiKeyHeader=true`/`ApiVersion="2024-08-01-preview"` set for Azure OpenAI; no key is committed.
- Open item: confirmed the resource endpoint and env var name, but the actual Azure OpenAI **deployment name** was assumed to be `gpt-4o-mini` (the default `ApexKnowledge:Model` value) — the user should confirm/correct this if their actual deployment is named differently, since Azure OpenAI requires the exact deployment name in the URL path.
- User confirmed the deployed model/deployment name is `gpt-4.1-mini` (model and deployment name are the same). Updated `ApexKnowledgeOptions.Model` default and `appsettings.json` from the `gpt-4o-mini` placeholder to `gpt-4.1-mini`. Service solution rebuilt and tested: 34/34 tests pass. Chat feature configuration is now complete pending the `APEXSERVICE_OPENAIKEY` environment variable being set wherever the Service actually runs.
