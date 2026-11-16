> Implements: Documents/design/admin-tools.md#1-design-philosophy (action logging), Documents/design/admin-tools.md#2-permission-tiers, Documents/design/admin-tools.md#5-world-intervention, Documents/design/admin-tools.md#6-round--objective-override (round controls)
> Touches systems: ingame-console, permissions, rounds-lobby, networking-session (multiplayer harness), id-access, health
> Status: shipped

**Progress:** phases 1–6 shipped and validated in Unity on 2026-11-10 (EditMode, the PlayMode console scenarios, `late-join` and `observer-sync` smoke, owner hand check). Advances [test-server.md](../../milestones/test-server.md) T3 (console path for admin force-end).

# Console commands rework (Nov 2026)

The F12 console ([ingame-console](../../architecture/systems/ingame-console.md)) grew one command at a time: 41 `Command` subclasses with four ways to pick a target, hand-written usage strings in three syntaxes, and help that lists no parameters. This effort cuts the roster to what tests, dev debugging and playtest admins use. Commands declare typed parameters, and parsing, validation, help and error messages are all generated from those declarations. It also fixes the permission levels that contradict [admin-tools.md](../../design/admin-tools.md) §2/§5. Scope was settled in an implementation grill on 2026-11-09.

## Starting state (2026-11-09, `e2f86cf20`)

- **Dispatch** lives in `CommandsController` (a `NetworkActor`). It reflects every `Command` subclass in every loaded assembly and names each one after its class, minus `Command`. Server and Client commands go through `CmdProcessOnlineCommand`, which checks the role. `Offline` commands run locally with no permission check.
- **Help:** `help` prints all commands from an unordered `Dictionary`, regardless of role, with no parameters. Per-command help is `<cmd> help`; `help <cmd>` prints the full list. Padding uses `new string(' ', 20 - name.Length)`, which throws for a name longer than 20.
- **Arguments:** every command hand-rolls `CheckArgs` → `ICalculatedValues` with its own `TryParse` calls and error wording. The command name is split with `Split(' ')`, the arguments with `Split()`.
- **Targets:**
  - by ckey: `hurt`, `heal`, `kill`, `sever`, `defib`, `ragdoll`, `addhand`
  - by `GameObject.Find`: `hitbodypart`, `examinebodypart`, `destroybodypart`, `dumpcontainer`
  - the caller's own tile or nearest object: `blast`, `atmosdebug`, `doordebug`, `lightdebug`, `areaambience`, `hurtstructure`
  - the item in hand: the trait commands

  A `spawndummy` dummy has no ckey, so no damage command can reach it.
- **Permission bugs:**
  - `spawnitem` and `accesscheck`/`accessgrant`/`accessrevoke`/`accesspreset` are `User` level, so any player can spawn items or grant themselves access.
  - `changeperms` is `Administrator` level and can grant `ServerOwner`.
  - `changeperms` lowercases the role names but not the input, so `Administrator` is rejected.
- **Exceptions:** `observesync` signals "not yet" by throwing. The exception escapes `ClientProcessCommand`, and that is what makes the harness's `wait_console` retry. A Server command that throws dies inside the ServerRpc with no answer, so `ScenarioConsole.Run` times out.
- **Logging:** only `disk` writes an admin action line (`Log.Information`).
- **Round controls** exist as `RoundAdminAction` (`Start, ForceStart, ExtendTimer, CancelCountdown, Stop, HoldEnd, ResumeEnd`) via `RoundSubSystem.CmdRoundAdminAction`, driven from the lobby Server Settings view. There is no console path.
- **Callers:**
  - PlayMode scenarios use `kill`, `hurt`, `heal`, `spawninhand`, `disk`, `ghostjump`, `doordebug`, `atmosdebug`, `accessgrant`, `accessrevoke`.
  - Multiplayer harness scripts (`Testing/multiplayer/scenarios/*.txt`) use `playerlist`, `doordebug`, `lightdebug`, `atmosdebug`, `hurt`, `observesync`, `atmosclientstatus`.
  - `ScenarioFixture` resets health with `heal <ckey> all 999 999`.

## Decisions

| # | Decision |
|---|---|
| 1 | Each command declares a **category**: `Admin`, `Debug`, `Test` or `Session`. A category only changes how `help` groups and hides commands; who may run a command stays in `AccessLevel`. No server setting hides a category. |
| 2 | **Delete:** `hitbodypart`, `destroybodypart`, `examinebodypart`, `dumpcontainer`, `addhand`, `addtrait`, `gettraits`, `describeitem`, `echo`, `screeneffecthitflash`. **Merge** `spawnitem` and `spawninhand` into `spawn`. Omniscient examine ([admin-tools.md](../../design/admin-tools.md) §4) is where inspection goes later. |
| 3 | **Rename:** `doordebug`→`door`, `lightdebug`→`lights`, `atmosdebug`→`atmos`, `rangeddebug`→`rangedmarkers`; `atmosclientstatus` becomes the subcommand `observesync atmos`. |
| 4 | **Add** `round start\|force\|extend\|cancel\|end\|hold\|resume`, mapping 1:1 to `RoundAdminAction` with no new round logic. It gives T3's force-end a console and automation path. |
| 5 | A **target** is a parameter type, resolved by one shared resolver that accepts `me`, a ckey (case-sensitive, matching `PlayerSubSystem.GetPlayer`), or `nearest`: the closest entity with a `HumanHealthController` within **5 m**, not the caller, failing beyond that. `GameObject.Find` targeting is removed. Commands that act on a place take no target and use the caller's position. |
| 6 | When a command acts on a subject, the target is the **first** parameter and is **required** (`hurt me Chest 5 0`, `accessgrant me Security`). An optional `Target` is allowed only as the last parameter (`ghostjump [target]`). A registry test enforces this. |
| 7 | **Declared parameters:** each command returns a list built from `Target`, `Int`/`Float` (optional range), `Enum<T>`, `Choice(...)`, `Item` (shares `TryResolveItem`) and `Rest`. Any parameter can be optional with a default. **Subcommands** are built in, each with its own list. `Perform(CommandContext ctx, Args args)` returns `CommandResult`. `CommandContext` carries the caller's connection, ckey, entity and role. `ICalculatedValues`, `CheckArgs` and the `Usage`/`LongDescription` strings go away. |
| 8 | Each command declares its **`Name`, `Category`, one-sentence `Description` and an example**. The class-suffix naming convention is dropped. A registry test checks that names are unique and lowercase and that every example parses. |
| 9 | **`CommandRegistry`** is plain C# and owns discovery, tokenizing, parsing, target-token handling, help and suggestions. The target resolver is injected through an interface, so EditMode tests use a fake one. `CommandsController` keeps only the FishNet RPCs, the role check and logging. |
| 10 | **Tokenizer:** runs of whitespace collapse, `"double quotes"` group words, and command names, enum values and choice values are case-insensitive. |
| 11 | **Where things run:** the server checks the role first. Arguments are then parsed on the side that runs the command: on the server for Server commands, on the owning client for Client commands, locally for `Local`. `CommandType.Offline` is renamed `Local`. |
| 12 | **Help:** `help` prints Admin, Debug and Session commands that the caller's role can run, sorted and grouped, one signature line each. `help test` and `help all` widen the list. `help <cmd>` adds the description, one line per subcommand, the values of each enum or long choice, and the example. `<cmd> help` is removed. Notation: `<required>`, `[optional=default]`, and up to 4 choices inline as `a\|b\|c`. Longer choices and enums show as `<name>` in the list. |
| 13 | **Results:** `CommandResult` has `Ok` and `Message`. The controller catches exceptions on every path and reports `error: <message>` with `Ok = false`, logging the stack trace. A parse error reads `error: <what>` followed by `usage: <signature>`. Success messages keep their current wording, which tests match on. |
| 14 | **Harness contract:** `ClientProcessCommand` returns the `CommandResult` for `Local` commands. `AutomationSubSystem`'s `wait_console` retries on `!Ok` instead of on an exception, and `observesync` returns failures instead of throwing. |
| 15 | **Unknown command:** the reply is `Unknown command '<x>'. Did you mean: …?`, matching by prefix or edit distance ≤2 across commands the caller may run. |
| 16 | **Permissions:** `spawn` and `access*` require `Administrator` ([admin-tools.md](../../design/admin-tools.md) §5). `changeperms` requires `ServerOwner` (§2), and its role argument is an `Enum<ServerRoleTypes>`, which fixes the case bug. `ghostjump` stays `User`. There is no sandbox exception: testers who need admin commands are promoted with `changeperms`. |
| 17 | **Admin action log:** `CommandsController` writes one `Log.Information` line tagged `AdminAction` for every successful command at `Administrator` or above, recording the ckey, the command line and the resolved target. It replaces `disk`'s own line. Permission checks are not logged (§1). |
| 18 | **Signatures that change:** `heal <target> [zone=all] [brute=full] [burn=full]`, so `heal me` is a full heal. `disk [target]` lists where the disk is when given no target and moves it otherwise; the `here` token is gone because `me` covers it. `spawn <item> [at=feet\|hand]`. `ghostjump [target]`. |

## Command roster after the rework

| Category | Command | Signature (sketch) | Role |
|---|---|---|---|
| Admin | `kill` | `<target>` | Administrator |
| Admin | `heal` | `<target> [zone=all] [brute=full] [burn=full]` | Administrator |
| Admin | `hurt` | `<target> <zone> <brute> [burn=0]` | Administrator |
| Admin | `sever` | `<target> <zone> [force]` | Administrator |
| Admin | `defib` | `<target>` | Administrator |
| Admin | `ragdoll` | `<target> [seconds]` | Administrator |
| Admin | `spawn` | `<item> [at=feet\|hand]` | Administrator |
| Admin | `spawndummy` | — | Administrator |
| Admin | `disk` | `[target]` | Administrator |
| Admin | `round` | `start\|force\|extend\|cancel\|end\|hold\|resume` | Administrator |
| Admin | `accesscheck` | `<target> [level]` | Administrator |
| Admin | `accessgrant` / `accessrevoke` | `<target> <level>` | Administrator |
| Admin | `accesspreset` | `<target> <preset>` | Administrator |
| Admin | `changeperms` | `<ckey> <role>` | ServerOwner |
| Admin | `playerlist` | — | User |
| Admin | `ghostjump` | `[target]` | User |
| Debug | `blast` | `[yield=120] [falloff=25]` | Administrator |
| Debug | `hurtstructure` | `[force=40]` | Administrator |
| Debug | `atmos` | `heat <kelvin>` / `gas <gas> <moles>` / `wake <radius>` | Administrator |
| Debug | `atmosdamage` | `[on\|off\|status]` | Administrator |
| Debug | `door` / `lights` | `open\|close` / `on\|off` | Administrator |
| Debug | `areaambience` | `<profile>` / `clear` | Administrator |
| Debug | `screeneffect`, `alertstack`, `energyvfx`, `rangedmarkers` | as today, declared | User (local visuals) |
| Test | `observesync` | `baseline` / `held <item>` / `hurt` / `door-opened` / `lights-off` / `atmos [assert]` | User (Local) |
| Session | `quit`, `reconnect` | — | User (Local) |

Exact parameter names and defaults are settled while converting each command. The rules in Decisions 5–8 are fixed.

## Phases

1. **Framework.**
   - Add `CommandRegistry`, the parameter types, subcommands, `CommandContext`/`Args`/`CommandResult`, the tokenizer, help generation, suggestions and the `ITargetResolver` interface.
   - Add the EditMode tests listed in **Tests**.
   - Rename `Offline` to `Local`.
2. **Deletions.** Delete the ten commands in Decision 2, along with `ItemCommandUtilities` where nothing else uses it.
3. **Conversions,** one category per commit: Admin, Debug, Test/Session. Each conversion:
   - removes `CheckArgs`/`ICalculatedValues`
   - adopts the target resolver
   - applies Decisions 16 and 18
   - folds `atmosclientstatus` into `observesync`
   - removes the old base members once the last command moves over
4. **Controller.**
   - Make `CommandsController` a thin shell over the registry.
   - Catch exceptions on all paths and add the `AdminAction` log line.
   - Return `CommandResult` from `ClientProcessCommand`; switch `wait_console` to `!Ok`.
5. **Call sites.**
   - Update the PlayMode scenarios and `ScenarioFixture` (`me` targets, `heal me`, `spawn … hand`, `door`, `atmos`).
   - Update the harness `.txt` scripts and `AutomationSubSystem`.
   - Update the docs that quote the old names: the system maps for health, structural-destruction, audio and screen-effects, and the T3 slice text.
6. **`round` command.** Wraps `RoundSubSystem.CmdRoundAdminAction`'s server-side handler, with a PlayMode scenario that runs `round end` on a running round.

## Validation (2026-11-10)

Written without the Editor, then validated in Unity: compile, EditMode (`CommandRegistryTests` including the `RealCommands_*` roster checks), the PlayMode console scenarios, `late-join` and `observer-sync` smoke, and an owner hand check of `help`, `help all`, `help hurt`, a typo and `round end`. Fixes the run needed:

- Discovery took the EditMode tests' private fake commands into the real roster. It now takes public top-level `Command` classes only.
- `disk one two` resolved `one` as a ckey before it noticed the extra argument. Arity is now checked before any argument is parsed.
- Hand-check follow-ups: console text 20/14 → 11 pt (the 800×600 canvas reference scaled it to ~48 px at 1440p), and `CameraFollow` ignores the scroll wheel while the pointer is over UI, so scrolling the console no longer zooms.
- Merging develop brought #217's two-handed routing for `spawninhand` into `spawn … hand`, and moved #210's new access scenario to `spawn IDCard hand` / `accessgrant me …`.

**Behaviour changes found while converting:**

- `screeneffect` is now `screeneffect set <effect> <0-1>` / `screeneffect release`.
- Not-found answers from `door`, `lights`, `hurtstructure` and `areaambience`, and `ghostjump`'s "already there", are now `error:` results.
- An unknown ckey reads `no player with ckey '…'` (the disk and ghostjump scenarios were updated).
- A harness `console` line for an unknown command now fails the script, because unknown commands return a failed result.
- `kill` and `hurt` append the target's name to their answers.
- `spawn … feet` calls `ItemSubSystem.SpawnItem` on the server; the old command called the `CmdSpawnItem` ServerRpc from the server.

## Deviations and gaps

- **`round hold` holds the end screen, not the countdown.** [admin-tools.md](../../design/admin-tools.md) §6 defines Hold as pausing the countdown. `RoundAdminAction.HoldEnd` instead holds the `Ending` state, and the lobby UI has no countdown pause. This effort exposes only what exists; pausing the countdown remains a gap for the admin-tools effort.
- **The action log goes to the existing Serilog sinks only.** §1 asks for a real, inspectable log. A persistent admin log, alongside ahelp ticket records (§3), belongs to the later admin-tools effort.

## Tests

- **EditMode** (`SS3D.Tests.EditMode`), against `CommandRegistry` with a fake resolver:
  - tokenizing: whitespace, quotes, case
  - each parameter type, including optionals, defaults, ranges, enums and choices
  - subcommands
  - help output: sorting, grouping, role filtering, notation
  - `help <cmd>` content
  - suggestions
  - the rules: unique lowercase names, an optional `Target` only last, every declared example parses
- **PlayMode:** the existing scenarios, migrated in phase 5 (`ConsoleToolScenarios`, `AccessScenarios`, `ReachScenarios`, `EvaGearScenarios`, `RespawnScenarios`, `SpeechScenarios`, `NukeOpsScenarios`, `EnvironmentScenarios`, `ScenarioFixtureScenarios`), plus new ones:
  - `hurt nearest` on a `spawndummy`
  - a User denied `spawn`
  - a Server command that throws returns `error:` instead of timing out
  - `round end`
- **Multiplayer smoke:** `late-join` and `observer-sync` scripts updated for the renames; `wait_console` retries on `!Ok`.
- **Validation:** `Testing/ui/validate.sh --steps compile,editmode` plus the PlayMode console scenarios; run the multiplayer smoke once phase 5 lands.

## Parked

- Command history and Tab-completion use the declared parameters, but belong in the UITK debug layer, not the condemned uGUI panel ([agent-first composition](../../architecture/2026-07_agent-first-composition.md)).
- Teleport (`tp`), mute, kick, ban, notes and godmode ([admin-tools.md](../../design/admin-tools.md) §4, §5, §7) belong to a later admin-tools effort and are cheap to add on this framework.
- A coordinate target type, waiting on `tp`.
