> Code paths: Assets/Scripts/SS3D/Systems/IngameConsoleSystem/
> Entry points: CommandsController, CommandRegistry
> Status: partial
> Verified: 24e023c7b — 2026-11-10

# In-game console

## Overview

The F12 dev/admin console. `CommandRegistry` (plain C#) discovers every `Command` subclass in every loaded assembly (not only `SS3D.Systems`, so UI assemblies can host commands that would otherwise create an asmdef cycle). It tokenizes lines, binds arguments against each command's declared parameters, and writes help, usage lines and "did you mean" suggestions from those same declarations. `CommandsController` is the FishNet shell: it routes a line to where its command runs, checks the caller's `PermissionSubSystem` role on the server, and writes one `AdminAction` log line for every successful Administrator-or-higher command ([admin-tools.md](../../design/admin-tools.md) §1).

Commands are grouped by `CommandCategory` (Admin, Debug, Session, Test). The category only affects how `help` groups and hides commands; `AccessLevel` decides who may run them. `help` lists what your role can run, `help test` and `help all` widen it, and `help (command)` shows signatures, value lists and an example. Targets are `me`, a ckey (case-sensitive) or `nearest` (closest body with health within 5 m, so combat dummies are reachable). The rework that produced this shape: [2026-11_console-commands](../../archive/architecture/2026-11_console-commands.md).

**Gaps against [admin-tools.md](../../design/admin-tools.md):** `round hold` holds the end screen (`RoundAdminAction.HoldEnd`), not the countdown that §6 describes; nothing pauses the countdown yet. The `AdminAction` line goes to the existing Serilog sinks only; §1's inspectable admin log belongs to the later admin-tools effort.

**Condemned UI:** console panel uGUI — do not extend; move to UITK debug layer when UiShell lands ([agent-first composition](../2026-07_agent-first-composition.md)). Command dispatch is **not** condemned.

**Log viewer:** the third-party `IngameDebugConsole` prefab in `Boot.unity` is separate from this command console. It shows Unity logs and toggles on `` ` ``; its corner counter is off unless Settings > Display > Show Log Indicator is on ([player settings](../2026-10_player-settings.md)). `IngameDebugConsole.Runtime` references `SS3D.Systems`, so code that touches `DebugLogManager` must live in a UI assembly, not Systems.

## Start here

- `IngameConsoleSystem/CommandRegistry.cs` — discovery, tokenizer, binding, `Execute` (never throws), help, suggestions, `Validate` (declaration rules)
- `IngameConsoleSystem/CommandsController.cs` — `ClientProcessCommand` (returns the result for help, unknown and Local commands; null when the server will answer), role check, admin log, answers
- `IngameConsoleSystem/CommandResolver.cs` — live `ICommandResolver`: `me` / ckey / `nearest` targets and item names (`TryResolveItemAsset`, no `Assets.Get` missing-asset error)
- `IngameConsoleSystem/Commands/Command.cs`, `CommandParameter.cs` (`P.Target/Int/Float/Enum/Choice/Item/Text/Rest`), `Subcommand.cs`, `CommandArgs.cs`, `CommandResult.cs`, `CommandContext.cs` — the declaration API
- `IngameConsoleSystem/ConsolePanelView.cs` — condemned uGUI panel; F12 open/close, Console input context while the field is focused
- Admin: `KillCommand`, `HealCommand` (`heal <target>` is a full heal), `HurtCommand`, `SeverCommand`, `DefibCommand`, `RagdollCommand`, `ItemCommands/SpawnCommand` (`spawn <item> [feet|hand]`; `hand` routes two-handed rifles like a pickup), `SpawnDummyCommand`, `DiskCommand` (`disk [target]`, T3 wedge tool), `RoundCommand` (`round start|force|extend|cancel|end|hold|resume` → `RoundSubSystem.ServerRoundAdminAction`), `IdAccessCommands/` (`accesscheck`, `accessgrant`, `accessrevoke`, `accesspreset`), `ChangePermsCommand` (ServerOwner), `PlayerListCommand`, `GhostJumpCommand`
- Debug: `BlastCommand`, `HurtStructureCommand`, `AtmosCommand` (`atmos heat|gas|wake`, caller's tile), `AtmosDamageCommand`, `DoorCommand` / `LightsCommand` (nearest airlock, caller's area), `AreaAmbienceCommand`, client-side `ScreenEffectCommand`, `EnergyVfxCommand`, `RangedMarkersCommand`, and `UI/MainHud/Commands/AlertStackCommand.cs` (MainHud asm)
- Test / Session: `ObserveSyncCommand` (Local; `observesync baseline|held|hurt|door-opened|lights-off|atmos [assert]`), `QuitCommand`, `ReconnectCommand`

**Awaiting answers:** `CommandsController.AnswerShown` fires for every answer the local console shows, local or from the server. `ScenarioConsole` (PlayMode tests) awaits it.

## Extension points

**New command:** subclass `Command` with a public parameterless constructor. Declare `Name` (lowercase, unique), `Category`, a one-sentence `Description` (no usage text), `AccessLevel`, `Type`, `Parameters` (a static array of `P.*`) or `Subcommands` (cached via `??=`), and an `Example` (arguments after the name). Implement `Perform(CommandContext, CommandArgs)` returning `CommandResult.Success` / `Error`. Use `context.CallerEntity` for "the caller's body" and a `P.Target()` first parameter for "acts on someone". Rules (checked by `CommandRegistry.Validate` in EditMode): an optional parameter can't be followed by a required one, and an optional `Target` or a `Rest` must be last.

**Commands that call UI assemblies (MainHud, etc.):** put the `Command` subclass in that UI assembly (MainHud → Systems is fine; Systems → MainHud cycles).

**ID access dev commands:** use `IdAccessCommandUtilities` for inventory lookup, level/preset parsing and `UpdateRecordAccess`.

**Screen-effect debug:** client commands call [screen-effects](screen-effects.md); F2 menu is an alternate path on the same subsystem.

**Alert-stack debug:** client `alertstack` lives in [inventory](inventory.md) MainHud; F4 menu is the alternate path (F3 is local-speech debug).

## Pitfalls

- **T/B open chat or the map editor while typing in the console:** Open and Close both sit on F12, so one press could run them in either order and leave the gameplay context live. `ConsolePanelView` ties the Console input context to the text field's focus (`InputTextEntryScope`) and treats Open as a toggle; keep context ownership on focus, not on Open/Close.
- **A harness assertion that throws instead of returning `Error` fails the script outright:** `wait_console` retries on a returned `!Ok` result; `Execute` turns an exception into an error result too, but write "not yet" as `CommandResult.Error`, not a throw.
- **A command class that isn't public and top-level is invisible:** `DiscoverCommands` skips nested and non-public types, so the EditMode tests' private fakes stay out of the roster. Declare real commands as `public class`.
- **Arity is checked before parsing:** extra tokens fail as "too many arguments" before any target resolves. Before that, `disk one two` reported `no player with ckey 'one'`.
- **Ckeys are case-sensitive targets** (`PlayerSubSystem.GetPlayer` matches exactly); `me` avoids the problem in scenarios.

## Tests

- EditMode: `Tests/EditMode/CommandRegistryTests.cs` — tokenizer, binding, defaults, subcommands, help, suggestions, declaration rules, and every real command's declaration and example.
- PlayMode scenarios drive commands through `ScenarioConsole` (`Tests/PlayMode/Framework/Scenarios/`), which awaits `AnswerShown`; `ConsoleToolScenarios` covers disk, ghostjump, `nearest` on a dummy, error and usage answers, role refusal and `round` mode checks.
- Multiplayer smoke: `Testing/multiplayer/scenarios/observer-sync*.txt` use `atmos`, `door`, `lights`, `hurt` and `observesync`; `late-join*` use `playerlist`.

## Depends on / Used by

- **Depends on:** [permissions](permissions.md), [id-access](id-access.md) (access dev commands), [entities](entities.md), [inventory](inventory.md) (incl. MainHud-hosted `alertstack`), [screen-effects](screen-effects.md), [rounds-lobby](rounds-lobby.md) (`round`), [inputs](inputs.md) (Console context)
- **Used by:** [Multiplayer testing](networking-session-testing.md) (`console` / `wait_console` route through `CommandsController.ClientProcessCommand`), PlayMode scenarios

## Related docs

- Effort: [2026-11_console-commands](../../archive/architecture/2026-11_console-commands.md) — shipped
- Known design deltas: [REVIEW_2026-10 — Known delta](../../design/REVIEW_2026-10.md#known-delta-revised-design-vs-code) — rows: admin-tools (action logging, two-way Admin help)
- Design (read-only): [admin-tools.md](../../design/admin-tools.md) — permission tiers, ahelp, stealth observation, world intervention, moderation this system will eventually need to expose
- [id-access](id-access.md)
- [screen-effects](screen-effects.md)
- [2026-07_agent-first-composition](../2026-07_agent-first-composition.md)
- [ui-shell](ui-shell.md)
- [INDEX.md](../INDEX.md)
