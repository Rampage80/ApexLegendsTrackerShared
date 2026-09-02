---
name: Shared C# contract guidance
description: Apply when editing C# source in the shared Apex Legends Tracker contract package.
applyTo: "**/*.cs"
---

- Preserve public member names, types, defaults, and JSON serialization behavior unless the change is explicitly coordinated.
- Prefer additive nullable-safe DTO properties and immutable-style `init` members, matching the existing models.
- Keep `IPlayerLookupContract` framework-neutral; do not add Web or Service dependencies.
- Do not place endpoint URLs, credentials, or runtime configuration in this package.
- After edits, run the focused Release build and check the package version/output when applicable.
