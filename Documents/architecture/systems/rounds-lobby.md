> Code paths: Assets/Scripts/SS3D/Systems/Rounds/, Assets/Scripts/SS3D/Systems/Lobby/
> Entry points: RoundSubSystem, ReadyPlayersSubSystem, RoundSubSystemBase
> Status: shipped

# Rounds / lobby

## Overview

Round lifecycle state machine with single-flight `CancellationTokenSource` (prevents double start/stop and embark-during-ending races). States: `Stopped → Preparing → WarmingUp → Ongoing → Ending → Ended`. Pre-round lobby UI shows ready players and round state. Join ordering hardened across entity, player, and gamemode subsystems. Round end appends a JSONL entry via [persistence](persistence.md) (`gamemode`, map id, player count, duration).

## Start here

- `Assets/Scripts/SS3D/Systems/Rounds/RoundSubSystem.cs` — concrete round subsystem
- `Assets/Scripts/SS3D/Systems/Rounds/RoundSubSystemBase.cs` — state machine base with generation-tracked CTS
- `Assets/Scripts/SS3D/Systems/Rounds/RoundState.cs` — round state enum
- `Assets/Scripts/SS3D/Systems/Rounds/ReadyPlayersSubSystem.cs` — player ready tracking
- `Assets/Scripts/SS3D/Systems/Lobby/UI/LobbyView.cs` — lobby UI root
- `Assets/Scripts/SS3D/Systems/Lobby/UI/LobbyReadyView.cs` — ready button / player list

## Extension points

- Round transitions: extend `RoundSubSystemBase` state handlers and messages in `Messages/`.
- Round-end history: `RoundSubSystem.AppendRoundHistory` → `PersistenceSubSystem.AppendRoundHistory`.
- Spawn flow: `SpawnReadyPlayersEvent` and [entities](entities.md) / [gamemodes-roles-traits](gamemodes-roles-traits.md).
- Tests: `Assets/Scripts/Tests/KnownIssueReproduction/RoundLifecycle_*`.

## Depends on / Used by

- **Depends on:** [entities](entities.md), [player-control](player-control.md), [gamemodes-roles-traits](gamemodes-roles-traits.md), [persistence](persistence.md) (round-end history append)
- **Used by:** All in-round gameplay (gates when simulation is active); [persistence](persistence.md) (round history on end)

## Related docs

- Plan: [persistence_architecture_design_2fe61864.plan.md](../../plans/persistence_architecture_design_2fe61864.plan.md)
