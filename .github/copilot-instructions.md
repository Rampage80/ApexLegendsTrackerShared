# ApexLegendsTrackerShared guidance

## Scope
- This repository owns the versioned C# contracts consumed by the ApexLegendsTracker Web and Service repositories.
- Keep changes focused on public DTOs, interfaces, package metadata, and packaging workflow. Do not change runtime behavior here because this project has no host application.
- Read `docs/api-contract.md` before changing a public type or serialization name.

## Contract discipline
- Treat `PlayerLookupResult` and `IPlayerLookupContract` as cross-repository contracts.
- Prefer additive, nullable-safe properties. Do not remove, rename, or retype public members without updating both consumers and the package version.
- Preserve the existing PascalCase JSON names and `System.Text.Json` compatibility unless a coordinated contract change requires otherwise.
- Never add secrets, live payloads, credentials, or environment-specific URLs to this repository.

## Validation
- Run `dotnet build ApexLegendsTrackerShared.slnx -c Release` after contract or package changes.
- Confirm the generated package version and output under `LocalFeed/` when packaging changes.
- When a public contract changes, inspect and validate the Web and Service repositories if they are available; record unavailable follow-up in `cross-repo-context.md`.
- Ignore generated `bin/`, `obj/`, and `LocalFeed/` contents when reasoning about source changes.
- For package validation, run the explicit project build/pack commands in `docs/api-contract.md`; solution build alone may not emit a `.nupkg`.

## Working style
- Keep public APIs and existing formatting stable.
- Avoid speculative abstractions and unrelated refactors.
- Update `docs/api-contract.md` and the shared coordination notes when the contract or package workflow changes.
