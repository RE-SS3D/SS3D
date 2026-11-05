> Code paths: Assets/Scripts/SS3D/Logging/
> Entry points: Log, Logs, LogManager, LogSettings
> Status: shipped
> Verified: e2f86cf20 — 2026-11-05

# Logging

## Overview

Serilog-based structured logging with a mandatory sender and a `Logs` category, namespace-level filtering via the `LogSettings` ScriptableObject, Unity console and file sinks, and client-ID enrichment for multiplayer.

## Start here

- `Assets/Scripts/SS3D/Logging/Log.cs` — primary API: `Verbose` / `Debug` / `Information` / `Warning` / `Error` / `Fatal`
- `Assets/Scripts/SS3D/Logging/Logs.cs` — category enum (`Generic`, `Important`, `ServerOnly`, `ClientOnly`, `Physics`, `UI`, `Testing`, …)
- `Assets/Scripts/SS3D/Logging/LogManager.cs` — Serilog pipeline setup, file sink format choice
- `Assets/Scripts/SS3D/Logging/LogSettings/LogSettings.cs` — namespace filters, `UseCompactJsonFormatter`
- `Assets/Scripts/SS3D/Logging/ClientIdEnricher.cs` — multiplayer client ID enrichment
- `Assets/Scripts/SS3D/Logging/SS3DUnityTextFormatter.cs` + `LogColors.cs` — colored Unity console output

## Extension points

- Call `Log.Information(this, "Template {Value}", Logs.Generic, value)`: sender first, a message template with named holes, an optional `Logs` category, then the values. Use this instead of `Debug.Log`.
- New category: add a value to `Logs`. `LogType.cs` is a legacy duplicate of the first five values and has no users.

## Pitfalls

- **Harness waits forever on `LogServer.json`:** the multiplayer harness tails compact-JSON logs. Player builds and any `-testscript=` run always write JSON (`LogManager`). In Editor Play Mode the format follows `LogSettings.UseCompactJsonFormatter`, whose C# default is `true` because Unity re-serializes the asset on open and a `false` default silently reverted it.

## Tests

- EditMode `LogTests.cs` (Unity console formatting of dictionaries, structs, lists); the multiplayer harness depends on the JSON sink.

## Depends on / Used by

- **Used by:** all systems; `Testing/multiplayer/` reads the JSON logs.

## Related docs

- [FORK_STATUS.md](../../FORK_STATUS.md) § Structured logging
- [networking-session](networking-session.md) — multiplayer harness
