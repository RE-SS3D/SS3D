> Code paths: Assets/Scripts/SS3D/Systems/Chat/, Assets/Scripts/SS3D/Systems/Audio/, Assets/Scripts/SS3D/Systems/Screens/
> Entry points: ChatSubSystem, AudioSubSystem, PlayerCameraSubSystem
> Status: stub

# Chat / audio / screens

## Overview

In-game chat, audio playback, and camera/screen controllers. (Navigation map not yet fully reviewed.)

**Condemned UI:** always-on chat window — do not extend or port to UITK; replace per [comms.md](../../design/comms.md) ([agent-first composition](../2026-07_agent-first-composition.md)). The in-game `ToggleChatsButton` on `PlayerCanvas` is disabled (obsolete chrome).

## Start here

- `Assets/Scripts/SS3D/Systems/Chat/ChatSubSystem.cs` — chat subsystem
- `Assets/Scripts/SS3D/Systems/Audio/AudioSubSystem.cs` — audio subsystem
- `Assets/Scripts/SS3D/Systems/Screens/PlayerCameraSubSystem.cs` — player camera
- `Assets/Scripts/SS3D/Systems/Screens/CameraSubSystem.cs` — camera subsystem

## Extension points

(stub)

## Depends on / Used by

- **Depends on:** [player-control](player-control.md)

## Related docs

- Design (read-only): [Documents/design/comms.md](../../design/comms.md)
- [2026-07_agent-first-composition](../2026-07_agent-first-composition.md)
