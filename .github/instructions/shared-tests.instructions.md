---
name: Shared contract test guidance
description: Apply when adding or updating tests for the shared Apex Legends Tracker package.
applyTo: "**/*Test*.cs,**/*Tests/**/*.cs"
---

- Test serialized contract shape and default/null behavior, especially for additive changes.
- Use small representative JSON fixtures; do not commit credentials or large upstream payloads.
- Cover compatibility-sensitive names and types, not implementation details.
- Run the narrowest test command first, then the Release solution build if the contract changed.
