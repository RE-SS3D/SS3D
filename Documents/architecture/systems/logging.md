> Code paths: Assets/Scripts/SS3D/Logging/
> Entry points: Log, Logs, LogManager, LogSettings
> Status: shipped

# Logging

## Overview

Serilog-based structured logging with mandatory sender + context enum, namespace-level filtering via `LogSettings` ScriptableObject, Unity console + file sinks, and client-ID enrichment for multiplayer.

## Start here

- `Assets/Scripts/SS3D/Logging/Log.cs` — primary logging API
- `Assets/Scripts/SS3D/Logging/LogManager.cs` — Serilog pipeline setup
- `Assets/Scripts/SS3D/Logging/LogSettings/LogSettings.cs` — namespace filter configuration
- `Assets/Scripts/SS3D/Logging/ClientIdEnricher.cs` — multiplayer client ID enrichment

## Extension points

- Use `Log.Info(sender, context, message)` with `LogType` context enum — avoid `Debug.Log`.

## Depends on / Used by

- **Used by:** All systems

## Related docs

- [FORK_STATUS.md](../../FORK_STATUS.md) § Structured logging
