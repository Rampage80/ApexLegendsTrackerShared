# Shared Contract Reference

This repository owns the versioned NuGet package consumed by the ApexLegendsTracker Web and Service repositories.

## Package

- Package ID: `ApexLegendsTrackerShared`
- Version is defined only in `ApexLegendsTrackerShared/ApexLegendsTrackerShared.csproj` (`<Version>`) — do not tag releases manually.
- Target framework: `net10.0`
- Local package output: `LocalFeed/`
- Published package source: GitHub Packages for the `Rampage80` owner

Use the explicit project commands below when validating package output locally:

```powershell
dotnet build .\ApexLegendsTrackerShared\ApexLegendsTrackerShared.csproj -c Release
dotnet pack .\ApexLegendsTrackerShared\ApexLegendsTrackerShared.csproj -c Release --no-build -o .\artifacts
```

## Release process

- Bump `<Version>` in `ApexLegendsTrackerShared/ApexLegendsTrackerShared.csproj` and push to `master`.
- `.github/workflows/main.yml` triggers on that file changing, builds, packs, publishes to GitHub Packages, and creates/pushes the matching `vX.Y.Z` git tag automatically — no manual `git tag`/`git push` step needed.
- If the derived tag already exists, tagging is skipped; `dotnet nuget push --skip-duplicate` makes re-runs safe.

## Public API

- This package contains only DTOs: `PlayerLookupResult`, `MapRotationResponse`, and `PredatorResponse`. It exposes no interfaces.
- `IPlayerLookupContract` (the lookup query signature) now lives in the Service repo (`ApexLegendsTracker.Service.Services`), since only the Service implements and consumes it.
- `PlayerLookupResult` is the canonical response envelope.
- `PlayerLookupResult` contains `PlayerName`, `Platform`, `Global`, `Realtime`, and `Legends`.
- Nested DTOs carry account, rank, presence, selected-legend, per-legend, badge, and image data. Their C# property names are also the expected JSON names.

## Compatibility

- Additive properties are preferred and should have safe defaults or nullable types.
- Removing, renaming, or retyping a public member is a breaking change for both consumers and requires coordinated updates and a package version decision.
- Keep this library independent of ASP.NET, Blazor, HTTP clients, and runtime configuration.
- Do not add credentials, tokens, or live player payloads to source control.

## Cross-repository coordination

When the public API changes, update the Web and Service package references and serialization tests together. The Web and Service repositories may not be present in every workspace; record any unverified follow-up in `cross-repo-context.md`.
