# Conversation log

## 2026-08-17
- User requested a cloud-native showcase strategy for the ApexLegendsTracker frontend.
- Confirmed the UI repo is `ApexLegendsTrackerWeb` and the backend repo is `ApexLegendsTrackerService`.
- Verified the backend route contract: `GET /api/v1/players/{platform}/{playerName}` with valid platforms `PC`, `PS4`, `X1`.
- Recorded the exact API payload shape and the CORS requirement needed to allow the web app to call the backend in local and deployed environments.
- Kept the shared state factual and excluded secrets or sensitive values.

## 2026-08-19
- Renamed the backend solution projects from Api/Infrastructure naming to WebAPI/Service naming to match the requested repo structure.
- Updated the solution, VS Code debug config, README instructions, and project references to the new names.
- Verified the solution still builds successfully after the rename, with only existing dependency warnings from a known OpenAPI vulnerability advisory.

## 2026-08-20
- User requested a new solution/project, `ApexLegendsTrackerShared`, to store the interface shared by the Web and Service repos.
- Created `ApexLegendsTrackerShared.slnx` and class library `ApexLegendsTrackerShared/ApexLegendsTrackerShared.csproj` (`net10.0`) in the shared directory, with `IPlayerLookupContract` and `PlayerLookupResult` mirroring the Service's current contract.
- While inspecting both repos to confirm the shape, found a pre-existing mismatch: the Service's real `PlayerLookupResult` is `{ PlayerName, Platform, RawJson }`, but the Web's `PlayerLookupResult` model expects structured `Global`/`Realtime`/`Legends` stats that the Service never sends. Recorded as an open coordination item; did not change either repo's behavior.
- Validation: `dotnet build ApexLegendsTrackerShared.slnx` succeeds. Did not wire the new project into either repo yet, pending a decision on the canonical shape.
- Discussed forward/backward compatibility: additive fields and `RawJson`'s internal contents can change freely without a version bump; removing/renaming/retyping envelope fields is breaking, and C#/`System.Text.Json` won't throw for a missing constructor property on the old record (it silently passes null/default) — a real risk to flag for future envelope changes.
- User decided to drop `RawJson` in favor of the concrete `Global`/`Realtime`/`Legends` shape (matching the Web's pre-existing model) and to physically share the contract via a new NuGet package both repos consume, rather than a manual/process-only shared reference.
- Implemented: `ApexLegendsTrackerShared` now packs `ApexLegendsTracker.Shared` (`GeneratePackageOnBuild`, output to `LocalFeed`) with the structured `PlayerLookupResult` + `IPlayerLookupContract`. Deleted the Service's local `IApexTrackerService`/`PlayerLookupResult` (`ApexLegendsTracker.Application/Players`) and the Web's local `Models/PlayerLookupResult.cs`; both now reference the `ApexLegendsTracker.Shared` package via a local `NuGet.Config` source. `ApexTrackerService` now deserializes the upstream `bridge` JSON directly into the shared `PlayerLookupResult` (case-insensitive) instead of wrapping it as a raw string, then overwrites `PlayerName`/`Platform` from the request.
- Validation: `dotnet build` (Debug and Release) succeeds for both `ApexLegendsTracker.slnx` and `ApexLegendsTracker.Web.slnx` after the change, including their test projects.
- Open/flagged: both repos' CI workflows only check out their own repo, so the local-folder NuGet source won't resolve on hosted runners — a real feed (e.g. GitHub Packages) and workflow auth are still needed before this survives a push to `master`. Not implemented, since it requires creating a remote for `ApexLegendsTrackerShared` and managing CI secrets.
