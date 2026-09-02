# Shared Contract Reference

This repository owns the versioned NuGet package consumed by the ApexLegendsTracker Web and Service repositories.

## Package

- Package ID: `ApexLegendsTrackerShared`
- Current version: `1.1.0`
- Target framework: `net10.0`
- Local package output: `LocalFeed/`
- Published package source: GitHub Packages for the `Rampage80` owner

Use the explicit project commands below when validating package output:

```powershell
dotnet build .\ApexLegendsTrackerShared\ApexLegendsTrackerShared.csproj -c Release
dotnet pack .\ApexLegendsTrackerShared\ApexLegendsTrackerShared.csproj -c Release --no-build -o .\artifacts
```

## Public API

- `IPlayerLookupContract.QueryByNameAsync(string playerName, string platform, CancellationToken cancellationToken = default)` is the framework-neutral lookup abstraction.
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
