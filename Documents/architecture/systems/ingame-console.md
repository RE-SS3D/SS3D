> Code paths: Assets/Scripts/SS3D/Systems/IngameConsoleSystem/
> Entry points: CommandsController
> Status: partial

# In-game console

## Overview

Dev/admin in-game console commands routed through `CommandsController`. Commands are discovered by reflection from `Command` subclasses. Server commands check `PermissionSubSystem` role before executing.

## Start here

- `Assets/Scripts/SS3D/Systems/IngameConsoleSystem/CommandsController.cs` — command dispatch (offline, server, client)
- `Assets/Scripts/SS3D/Systems/IngameConsoleSystem/Commands/Command.cs` — base class; subclasses auto-register
- `Assets/Scripts/SS3D/Systems/IngameConsoleSystem/Commands/IdAccessCommands/` — `accesscheck`, `accessgrant`, `accessrevoke`, `accesspreset` dev helpers

## Extension points

**New server command:** subclass `Command`, set `Type = Server`, `AccessLevel`, implement `Perform` and `CheckArgs`; name class with `Command` suffix.

**ID access dev commands:** use `IdAccessCommandUtilities` for target resolution and level/preset parsing.

## Depends on / Used by

- **Depends on:** [permissions](permissions.md), [id-access](id-access.md) (access dev commands), [entities](entities.md), [inventory](inventory.md)

## Related docs

- [id-access](id-access.md)
- [INDEX.md](../INDEX.md)
