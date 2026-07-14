> Code paths: Assets/Scripts/SS3D/Systems/IdAccess/
> Entry points: IdAccessSubSystem, AccessCredentialResolver
> Status: partial

# ID / access

## Overview

Server-side crew identity rail and shared access checks per `Documents/design/id-access.md`. A `CrewRecord` holds the authoritative access bitmask; physical `IDCard` tokens bind to that record. Every consumer calls `IdAccessSubSystem.CheckAccess` against the holder's on-person credential. Machine interfaces, doors, and the ID console share this path.

## Start here

- `Assets/Scripts/SS3D/Systems/IdAccess/IdAccessSubSystem.cs` — crew records, card binding, `CheckAccess`, console edits
- `Assets/Scripts/SS3D/Systems/IdAccess/AccessCredentialResolver.cs` — finds bound ID on inventory containers and hands (direct card or PDA-inserted)
- `Assets/Scripts/SS3D/Systems/IdAccess/AccessMask.cs` / `AccessLevel.cs` — flat bitmask model
- `Assets/Scripts/SS3D/Systems/IdAccess/AccessLevelCatalog.cs` — editable level list for ID console UI
- `Assets/Scripts/SS3D/Systems/IdAccess/AuthLogDeviceBehaviour.cs` — per-device auth log adapter
- `Assets/Scripts/SS3D/Systems/Inventory/Items/Identification/IDCard.cs` — physical token; no independent access state
- `Assets/Scripts/SS3D/Systems/Furniture/AirLockAccessGate.cs` — door access from area default or override
- `Assets/Scripts/SS3D/UI/MachineInterface/AccessGatedMachineInterfaceBehaviour.cs` — server ID scan session for machine UIs
- `Assets/Scripts/SS3D/UI/MachineInterface/IdConsoleController.cs` — Change ID gated editing
- `Assets/Scripts/SS3D/Systems/IngameConsoleSystem/Commands/IdAccessCommands/` — dev `accesscheck`, `accessgrant`, `accessrevoke`, `accesspreset`

## Extension points

**New consumer:** call `IdAccessSubSystem.CheckAccess(inventory, requiredMask, authLogDevice)`; attach `AuthLogDeviceBehaviour` when logging is needed.

**Credential resolution:** scans all `HumanInventory.Containers` plus `Hands.HandContainers`; accepts direct `IDCard` or card inside `PDA`.

**Machine UI gate:** subclass `AccessGatedMachineInterfaceBehaviour`, set `ReadIdControlId` and `RequiredAccess`; sync `AccessGranted`, `AccessScanning`, `AccessDenied` in snapshots.

**Dev testing:** use in-game console commands or `IdAccessDevSettings.AllowConsoleAccessEditing` to bypass Change ID on the ID console.

## Depends on / Used by

- **Depends on:** [inventory](inventory.md) (ID cards, PDA, `HumanInventory`), [roles](gamemodes-roles-traits.md) (spawn-time record creation)
- **Used by:** [machine-interface](machine-interface.md), [area](area.md) (door defaults), [furniture](furniture.md) (airlocks)

## Related docs

- Design (read-only): [Documents/design/id-access.md](../../design/id-access.md)
- Tests: `Assets/Scripts/Tests/EditMode/IdAccessTests/`
