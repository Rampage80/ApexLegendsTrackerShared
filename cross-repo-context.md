# ApexLegendsTracker cross-repository context

## Current state
- UI repo present: `ApexLegendsTrackerWeb`.
- Backend repo present: `ApexLegendsTrackerService`.
- The UI is a Blazor WebAssembly frontend using `HttpClient` with `ApiBaseUrl` from configuration.
- The backend exposes `GET /api/v1/players/{platform}/{playerName}`.
- The API contract is now confirmed in the backend implementation, and the UI should remain aligned with it.

## Confirmed API contract
- Route: `GET /api/v1/players/{platform}/{playerName}`
- Route: `GET /api/v1/map-rotation?version=1|2` (version optional)
- Route: `GET /api/v1/predator-thresholds`
- Valid platforms: `PC`, `PS4`, `X1`
- Request validation: `playerName` is required; invalid platform returns `400`
- Success response: `200 OK` with structured `PlayerLookupResult`
- Shape: `PlayerName`, `Platform`, `Global`, `Realtime`, `Legends`
- Error style: `400` for invalid inputs, upstream failures are surfaced as status codes with a trace identifier
- Map rotation uses backend `MapRotationResponse` with `BattleRoyale`, `Ranked`, `Ltm`, and `Wildcard` modes, each containing shared `Current` and `Next` entries; LTM preserves `EventName`. Predator uses `PredatorResponse.RP` with `PC`, `PS4`, and `X1`; `SWITCH` is excluded. The backend validates map-rotation `version` as `1` or `2` and maps invalid/empty upstream payloads to `502`.

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

## Release automation
- `ApexLegendsTrackerShared`'s package version lives only in `ApexLegendsTrackerShared/ApexLegendsTrackerShared.csproj` (`<Version>`).
- `.github/workflows/main.yml` now triggers on pushes to `master` that touch that csproj, builds/packs/publishes to GitHub Packages, then auto-creates and pushes the matching `vX.Y.Z` git tag (skipped if it already exists). Manual `git tag`/`git push` is no longer required.

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

## Shared contracts package
- `ApexLegendsTrackerShared` (solution `ApexLegendsTrackerShared.slnx`, class library `ApexLegendsTrackerShared/ApexLegendsTrackerShared.csproj`, `net10.0`) is packed as NuGet package `ApexLegendsTrackerShared` (currently `1.5.0`), `GeneratePackageOnBuild=true`, output to `ApexLegendsTrackerShared/LocalFeed`.
- Canonical contract: `ApexLegendsTracker.Shared.PlayerLookupResult` (`PlayerName`, `Platform`, `Global`, `Realtime`, `Legends` — the structured shape, no `RawJson`). The package now contains DTOs only (`PlayerLookupResult`, `MapRotationResponse`, `PredatorResponse`) and exposes no interfaces.
- `IPlayerLookupContract` moved out of the shared package (2026-09-07) into the Service repo (`ApexLegendsTracker.Service.Services`), since only the Service implemented/consumed it; the Web never used it directly.
- Both repos consume it via `PackageReference` to `ApexLegendsTracker.Shared`:
  - Service: `ApexLegendsTracker.Application.csproj` references the package; `ApexTrackerService` (in `ApexLegendsTracker.Service`) implements `IPlayerLookupContract` and deserializes the upstream `bridge` response directly into `PlayerLookupResult` (case-insensitive JSON), then overwrites `PlayerName`/`Platform` with the request values. The old local `IApexTrackerService`/`PlayerLookupResult` in `ApexLegendsTracker.Application/Players` were deleted.
  - Web: `ApexLegendsTracker.Web.csproj` references the package; `IApexTrackerApiClient`/`ApexTrackerApiClient`/`PlayerLookupState` and `_Imports.razor` now use `ApexLegendsTracker.Shared` instead of the deleted local `ApexLegendsTracker.Web.Models.PlayerLookupResult`.
- Each repo has a `NuGet.Config` pointing a `ApexLegendsTrackerSharedLocal` source at `../ApexLegendsTrackerShared/LocalFeed` (relative sibling-folder path) for local restore.
- **CI gap (unresolved):** both repos' GitHub Actions workflows only `actions/checkout` their own repo, so the `../ApexLegendsTrackerShared/LocalFeed` path won't exist on hosted runners — CI builds will fail to restore `ApexLegendsTracker.Shared` until a real feed is set up (e.g. GitHub Packages) and each workflow authenticates to it. Local builds/dev are unaffected.

## Resolved: Web vs Service DTO mismatch
- Previously the Service returned `{ PlayerName, Platform, RawJson }` while the Web expected `{ PlayerName, Platform, Global, Realtime, Legends }` — confirmed bug, `Global`/`Realtime`/`Legends` were always defaulted.
- Resolved by making the structured shape (`Global`/`Realtime`/`Legends`, no `RawJson`) canonical in the shared package; the Service now parses the upstream JSON into that shape instead of passing it through as a string.

## Open coordination work
- **Interface relocated out of shared package (2026-09-07):** `IPlayerLookupContract` moved from `ApexLegendsTrackerShared` into the Service repo (`ApexLegendsTracker.Service.Services`); bumped the shared package to `1.5.0` and the Service's `PackageReference` to match. `PlayersController` now has an added `using ApexLegendsTracker.Service.Services;`. The Web repo still references `1.4.0` and is unaffected since it never used the interface (only `PlayerLookupResult`); bumping its reference is optional and not yet done.
- **Status API response schemas (2026-09-07):** Real map rotation and Predator fixtures were added to the Service tests. Backend DTOs now model the captured fields: shared current/next map entries across battle royale, ranked, LTM, and wildcard modes; LTM event names; and PC/PS4/X1 Predator thresholds while excluding SWITCH. The Web can now consume these stable backend response shapes when UI integration is requested.
- **Shared package 1.4.0 (2026-09-07):** Map rotation and Predator DTOs were moved from the Service into `ApexLegendsTracker.Shared`. The Service and Web package references now target `1.4.0`; the Service uses the local sibling feed for development restore.
- **Upstream API client boundary (2026-09-07):** The Service now routes player, map rotation, and Predator calls through `IApexApiClient`/`ApexApiClient`. Authentication, streaming JSON deserialization, upstream status handling, malformed JSON handling, and empty-response handling are centralized there for future logging and observability wrappers.
- **Contract v1.2.0 (2026-09-02):** Removed arena, battlepass, badges, and selected-legend game-info fields. Retained rank imagery, selected-legend icon/banner, `toNextLevelPercent`, and all-character icon/stat data. The Web displays these retained fields; the backend must consume and publish the same shared package version.
- **AI context efficiency (2026-09-02):** The Shared repo now has compact global Copilot guidance, scoped C#/test instruction files, workspace exclusions for generated output, and a concise contract reference. The Web and Service repos were not available in this workspace, so their existing guidance could not be compared directly; no runtime or package contract behavior changed.
- **AI context efficiency (2026-09-02):** The Web repo now has compact global Copilot guidance, scoped C#/Razor/test instruction files, workspace exclusions for generated output, and a concise API contract reference. Keep backend/shared guidance similarly scoped when those repositories are available; avoid duplicating the contract across instruction files.
- **AI context efficiency (2026-09-02):** The Service repo now has compact backend-specific Copilot guidance, scoped C#/test instruction files, workspace exclusions for generated output, and a concise API contract reference. No runtime or cross-repository API behavior changed.
- Stand up a real package feed reachable from CI (e.g. GitHub Packages) for `ApexLegendsTracker.Shared`, update both workflows to authenticate/restore from it, and retire the local-only `NuGet.Config` source once done.
- `ApexLegendsTrackerShared` now has its own dedicated GitHub repo: `https://github.com/Rampage80/ApexLegendsTrackerShared` (extracted from the `DevProjects` monorepo, own `.gitignore` excluding `bin/`, `obj/`, `LocalFeed/`).
- Create Kubernetes manifests and Helm values for the app and dependencies.
- Add telemetry instrumentation, dashboards, and alerting.
- Decide whether the showcase includes AI/haystack search features or remains a pure API resilience and cloud-native observability showcase.
- **Contract v1.1.0 (2026-08-24):** `PlayerLookupResult` was additively expanded (Tag/Uid/Avatar/LevelPrestige/ToNextLevelPercent/Bans/Arena/Battlepass/Badges on `PlayerGlobalStats`; RankImg/RankedSeason on `PlayerRank`; LobbyState/IsInGame/CanJoin/PartyFull on `PlayerRealtimeStats`; GameInfo/ImgAssets on `SelectedLegend`; new `PlayerLegends.All` dictionary) and packed locally as `1.1.0`. The Web repo now restores `1.1.0` from the local `ApexLegendsTrackerSharedLocal` feed and consumes the new fields in `Results.razor`. **The Service repo has not been updated** (not present in this workspace) — it needs its `PackageReference` bumped to `1.1.0` to actually populate these fields for live requests; until then the Web will show empty/default values for the new fields against the real Service. The package also has not been pushed to GitHub Packages yet, so hosted CI still resolves `1.0.0`.
