> Code paths: Assets/Scripts/SS3D/Systems/IngameConsoleSystem/
> Entry points: CommandsController
> Status: partial

# In-game console

## Overview

Dev/admin in-game console commands routed through `CommandsController`. Commands are discovered by reflection from `Command` subclasses. Server commands check `PermissionSubSystem` role before executing.

**Condemned UI:** console panel uGUI — do not extend; move to UITK debug layer when UiShell lands ([agent-first composition](../2026-07_agent-first-composition.md)). Command dispatch is **not** condemned.

## Start here

- `Assets/Scripts/SS3D/Systems/IngameConsoleSystem/CommandsController.cs` — command dispatch (offline, server, client)
- `Assets/Scripts/SS3D/Systems/IngameConsoleSystem/Commands/Command.cs` — base class; subclasses auto-register
- `Assets/Scripts/SS3D/Systems/IngameConsoleSystem/Commands/IdAccessCommands/` — `accesscheck`, `accessgrant`, `accessrevoke`, `accesspreset` dev helpers
- `Assets/Scripts/SS3D/Systems/IngameConsoleSystem/Commands/ScreenEffectCommand.cs` — client `screeneffect` intensity setter
- `Assets/Scripts/SS3D/Systems/IngameConsoleSystem/Commands/ScreenEffectHitFlashCommand.cs` — client hit-flash trigger

## Extension points

**New server command:** subclass `Command`, set `Type = Server`, `AccessLevel`, implement `Perform` and `CheckArgs`; name class with `Command` suffix.

**ID access dev commands:** use `IdAccessCommandUtilities` for target resolution and level/preset parsing.

**Screen-effect debug:** client commands call [screen-effects](screen-effects.md); F2 menu is an alternate path on the same subsystem.

## Depends on / Used by

- **Depends on:** [permissions](permissions.md), [id-access](id-access.md) (access dev commands), [entities](entities.md), [inventory](inventory.md), [screen-effects](screen-effects.md)

## Related docs

- [id-access](id-access.md)
- [screen-effects](screen-effects.md)
- [2026-07_agent-first-composition](../2026-07_agent-first-composition.md)
- [ui-shell](ui-shell.md)
- [2026-07_multiplayer-test-harness](../2026-07_multiplayer-test-harness.md) — `console <command line>` in the harness's automation scripts routes through `CommandsController.ClientProcessCommand`
- [INDEX.md](../INDEX.md)
