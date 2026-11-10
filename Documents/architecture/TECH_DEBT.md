# Technical debt tracker

**Last updated:** 2026-11-09

Checked against `06316a98` (source inspection of the 54 commits / ~56k lines since `259787487`;
§1.19–§1.30 are new from that audit). Runtime and CI-run results below are prior observations, not
new test results from this documentation refresh.

This is the project-wide register of architecture problems, code smells, and quality risks that
threaten long-term viability rather than one-off bugs. It is a cross-cutting **reference** doc, not
one of the four layers in [SKILL.md](../SKILL.md) — it doesn't replace the per-domain debt notes
already living in system maps (`Documents/architecture/systems/*.md` **Pitfalls**) or in [2026-07_agent-first-composition.md](2026-07_agent-first-composition.md);
it indexes and ranks them so "what's the biggest structural risk right now" has one answer instead
of 40 scattered ones.

**Scope:** structural debt — things that make the codebase harder to extend safely, not missing
features (those live in [INDEX.md](INDEX.md)'s coverage table) and not gameplay-design gaps (those
live in each domain's design doc `§Out of scope`).

**Maintaining this doc:** update when a debt item is paid down (move to "Resolved" with the PR/date)
or when a new structural smell is found during feature work — same trigger as `update-system-docs`.
Cite system maps by path + section rather than re-explaining; this doc should stay skimmable, not
become a second copy of the maps.

---

## How to read the severity column

- **Blast radius** — how much code/how many systems break or must be touched if this goes wrong or
  ever gets fixed.
- **Trend** — getting worse (more code piles on it), stable (contained by convention), or scheduled
  (a named follow-on effort exists).

---

## Suggested paydown order

Planned in phases in [2026-11_tech-debt-paydown.md](2026-11_tech-debt-paydown.md). In short: cheap,
high-value first; the large splits after the contracts they depend on exist.

1. **Authority holes** ([§1.19](#119-server-trusts-client-input-in-several-serverrpcs)) — small deletions and server-side checks.
2. **Small correctness traps:** world-sim suspension ([§1.23](#123-world-simulation-suspension-copied-four-times-and-cannot-nest)),
   `SubSystems` registry ([§1.25](#125-subsystems-registry-lifecycle-bugs-and-get-misuse)), Map Editor undo
   ([§1.22](#122-map-editor-trusts-the-clients-partial-map-and-assumes-every-command-succeeded)), HUD
   weapon-state mutation ([§1.9](#19-god-classes-forming-in-hot-uiinteraction-code)), Examine catalog outside the umbrella rebuild ([§1.4](#14-ui-asset-catalog-pattern-copy-pasted-three-times-no-shared-infrastructure)).
3. **Dead code:** `com.unity.entities` ([§1.26](#126-comunityentities-installed-for-one-unused-world)),
   `HumanoidPredictedMovement` ([§1.30](#130-humanoidpredictedmovement-disabled-but-maintained)),
   base `SS3D.UI` assembly, unused `UiBinderBase` ([§1.29](#129-ui-widget-and-host-duplication)).
4. **Ownership contracts:** hand actions + upper-body pose ([§1.20](#120-hand-actions-and-upper-body-pose-have-no-single-owner)),
   item holder ([§1.21](#121-item-holder-and-visibility-are-written-by-many-classes)).
5. **Assembly split by edge** ([§1.24](#124-ss3dsystems-is-one-tangled-assembly)), then the god-class
   splits in §1.9.

## 1. Top structural risks (ranked)

### 1.1 `Human.prefab` mega-prefab

**Blast radius: highest — trend: stable by policy, not by tooling**

`Human.prefab` is a large graph spanning health, inventory, movement, combat and identity.
Changes must use the [Entity prefab recipes](systems/entities-prefab-recipes.md), registered on
`HumanPrefabRecipes`, rather than hand-editing YAML. The aggregator covers hygiene, hands,
speech, combat networking, substance-container cleanup and nested-instance resync.
`HumanPrefabIntegrityTests` checks specific script/wiring contracts, not a general component budget.

**Remaining work:** keep domain rewiring recipe-owned when movement/animation, examine, stamina or
other entity composition changes. Continue reducing unrelated root responsibilities; existing
recipes do not make the whole prefab decomposed. Visual-only organ mesh extraction is unscheduled
unless Human/TestHuman visual drift warrants it; removed-organ item prefabs cannot replace rigged
in-body visuals.

- Related: [entities](systems/entities.md), [Entity prefab recipes](systems/entities-prefab-recipes.md),
  [composition policy](2026-07_agent-first-composition.md)

### 1.2 Collapse/death/ragdoll presentation has no single owner

**Resolved 2026-07-23** — see [§6 Resolved](#6-resolved).

### 1.3 Interaction `Discover` has no contract

**Resolved 2026-07-23** — see [§6 Resolved](#6-resolved).

### 1.4 UI asset-catalog pattern copy-pasted three times, no shared infrastructure

**Blast radius: medium, compounding — trend: getting worse**

There are now nine `*AssetPaths` + catalog pairs (checked `06316a98`). Four derive from
`UiAssetCatalogBase` (UiShell, BugReport, MainMenu, RoundEnd — new surfaces do adopt it); five are
still plain `ScriptableObject`s (MachineUi, MainHud, StoragePanel, Lobby, Examine), and only four
Editor builders use `UiCatalogBuilderKit`. **Examine is missing from the umbrella**
`UiCatalogRebuildAll.TryRebuildAll` and keeps its own `SS3D/Examine/Rebuild Examine Asset Catalog`
MenuItem (`ExamineOverlayAssetCatalogBuilder.cs:16`), so "Rebuild All UI Catalogs" silently skips it.
Each copy shares the same failure mode: a stale catalog after adding a UXML path without running
the rebuild.

**Next steps:** fold Examine into the umbrella (S); migrate the five legacy catalogs onto the base.

- Related: [ui-shell.md](systems/ui-shell.md), [2026-07_ui-shell-consolidation.md](2026-07_ui-shell-consolidation.md) § Explicit non-goals (the migration phases), [machine-interface.md](systems/machine-interface.md), [inventory.md](systems/inventory.md), [rounds-lobby.md](systems/rounds-lobby.md)

### 1.5 One-off Editor rebuild-menu proliferation (data-codegen)

**Blast radius: medium — trend: shrinking (menu hygiene shipped; catalog pipeline still open)**

PrefabUtility **one-shot setup MenuItems** and finished migration menus were thinned under
[2026-07_editor-tooling-tiers.md](2026-07_editor-tooling-tiers.md) (tier A keep / tier B recipe
aggregators / tier C delete). Remaining debt is the **catalog rebuild** class: feature work still lands
per-surface rebuild scripts (interaction icon sprites, UI path catalogs in §1.4) with GUID hacks and
"did anyone run the menu?" drift, instead of one shared import → Addressables →
`AssetDatabase.LoadAssetsFromAssetGroup` → codegen pipeline. No CI check verifies a committed catalog
asset is in sync with the C# path constants it should mirror — drift is discovered at Play Mode/build
time, not at PR time. Do not add another per-surface rebuild MenuItem; use the umbrella /
`UiCatalogBuilderKit`.

- Related: [data-codegen.md](systems/data-codegen.md), [2026-07_editor-tooling-tiers.md](2026-07_editor-tooling-tiers.md)

### 1.6 Crafting is dead code that hasn't been deleted

**Resolved 2026-07-23** — see [§6 Resolved](#6-resolved).

### 1.7 Legacy scene-based subsystem registration coexists with three ad-hoc bootstrap styles

**Resolved 2026-07-23** for gameplay SubSystems — see [§6 Resolved](#6-resolved). Residual UI-host
`RuntimeInitializeOnLoad` self-bootstraps (UiShell / MainHud / StoragePanel) stay under agent-first
follow-on **(b)**, not this item.

**Regressing (checked `06316a98`):** seven Systems types self-bootstrap DDOL hosts with
`AfterSceneLoad` again, bypassing `SystemsBootstrap` and its Test-Runner-scene guard:
`BugReportSubSystem`, `StructuralDamageSubSystem`, `AirLockProximityService`, `FloorDecalView`,
`AreaFloorStripeView`, `LocalSpeechDebugTrigger`, `ScreenEffectsDebugMenuView`. This is why
`TileSubSystem.cs:291` has to `FindFirstObjectByType<FloorDecalView>()`. Move them into
`SystemsBootstrap.EnsureProcessWideServices` (S). `SystemsBootstrap.cs:23-29` still loads
`NetworkSessionSubSystem` by type-name string "to avoid asmdef cycles", but Systems already
references SS3D.Networking — the indirection is stale.

### 1.8 Condemned UI remains; legacy tile UI partly purged

**Blast radius: medium — trend: shrinking slowly**

Still present at `259787487`: the console panel (`ConsolePanelView`,
[ingame-console.md](systems/ingame-console.md)) and ScreenEffects debug canvas
(`ScreenEffectsDebugMenuView`, now opened through F1; [screen-effects.md](systems/screen-effects.md)).
The old TileMap Creator construction prefabs and prefab-collection entry were purged; residual
`Tile/UI/AssetSlot.cs` remains, while `TileMapCreator/` also contains live Map Editor hologram and
layer-visibility helpers. Do not treat that whole folder as obsolete UI.

- Purged so far: crafting menu (2026-07-23), examine panels (2026-07-26), inventory/hands/intent,
  legacy lobby + `GameScreens` (lobby Phase F, 2026-10-30), legacy construction UI prefabs
  ([tile.md](systems/tile.md) §Overview).

### 1.9 God-classes forming in hot UI/interaction code

**Blast radius: medium — trend: getting worse (growth hidden in partial files)**

The July `InteractionController` decomposition
([archive](../archive/architecture/2026-07_interaction-controller-decomposition.md)) has regrown:
1,231 → 1,585 lines with `InteractionController.Animation.cs`, which now owns hand-action scheduling.
Partial files added "so Human.prefab is untouched"
([2026-11_humanoid-animation-pass.md](2026-11_humanoid-animation-pass.md)) hide growth from file-size
checks. Sizes at `06316a98` (including partials):

| Class | Lines | Clusters to split out |
|---|---|---|
| `LobbyShellView` | 2,022 + 542 | one class per tab; standalone `PlayerSettingsPanel` (the main menu ships a "Settings screen placeholder" because settings are locked inside the lobby) |
| `MapEditorSubSystem` | 1,686 + partials | client controller, server RPC layer, sim suspension (§1.23) |
| `RoundSubSystem` | 1,685 over 8 partials | `ServerSettingsService`, observability/bug intake (closes the Rounds↔BugReports cycle), map-stop flow |
| `MainHudSubSystem` | 1,627 | visibility arbiter, local-player binder, reticle presenter, alert presenter, inventory actions |
| `InteractionController` | 1,585 | hand-action scheduler (§1.20) |
| `CombatInteractionNetwork` | 1,451 | `ThrowController` component via the Human recipe |
| `ConstructionHologramManager` | 1,452 | dead RPCs (§1.19); duplicate "what would Delete remove" logic |
| `AreaSubSystem` | 1,360 | lighting; client sync |
| `Ragdoll` | 1,224 | see §1.20 |
| `MainMenuSubSystem` | 1,013 | `MainMenuConnectionFlow` (no UI), ambience |

**Bug-class risk in MainHud:** `Update` → `RefreshZoneReticle` calls
`ranged.ServerCompleteReloadIfDue()` (`MainHudSubSystem.cs:317`) and `CurrentSpreadDegrees` (which
decays recoil) every frame, even with the reticle hidden. On a host, the HUD completes reloads on the
server. Combat should expose a read-only readout for the HUD (S).

**Lobby polling:** `LobbyUiSubSystem.RefreshSession` polls at 5 Hz for the whole session, including
in-round, and sends the job-preference / appearance / OOC handshakes and polls `CmdReadServerSettings`
from there. That handshake belongs in a client-session service in Systems.

- Related: [interactions-runtime.md](systems/interactions-runtime.md), [inventory.md](systems/inventory.md)
- Effort (interactions): [2026-07_interaction-controller-decomposition.md](../archive/architecture/2026-07_interaction-controller-decomposition.md) — shipped
- Effort (MainHud): none yet

### 1.10 Play Mode / multiplayer verification is optional in practice

**Blast radius: high — trend: mixed (local loop stronger; CI health last recorded as failing)**

**Current tooling:** [Tools/build_client_and_server.sh](../../Tools/build_client_and_server.sh)
builds Linux client/server binaries via Unity batchmode (close the project's Editor first).
With the Editor open, [Testing/ui/validate.sh](../../Testing/ui/validate.sh) `--nightly` drives compile,
EditMode, PlayMode, stale client/server builds and the smoke set; `--weekly` drives the
visual, lifecycle and remote-client pilots (the sandbox pilot is on demand). Both paths require an
installed Unity Editor; the scripts do not make runtime verification automatic at merge time.

**CI configuration:** [editmodetestrunner.yml](../../.github/workflows/editmodetestrunner.yml)
runs EditMode; [multiplayer-smoke-test.yml](../../.github/workflows/multiplayer-smoke-test.yml)
runs PlayMode and then the smoke set (`late-join`, `reconnect`, `observer-sync`; `sandbox-reset`
and `nukeops-round` nightly only) against real client/server builds on manual dispatch, labeled PRs
(`test:multiplayer`) and a nightly schedule. PlayMode and the smoke set are nightly gates, not
merge gates, and local full validation is not enforced before merge. The load/soak runner is local
tooling, not a scheduled soak service
([networking-session.md](systems/networking-session.md) §Tests).

**Last recorded CI observation (2026-10-31):** no smoke pass since 2026-10-01; 10-05/06 failed,
10-07 through 10-19 waited for the runner and were cancelled. Checked 2026-11-01: there have been no
runs since, because GitHub has the workflow `disabled_inactivity`; re-enable it
(`gh workflow enable multiplayer-smoke-test.yml`) and confirm the self-hosted runner picks jobs up. Recheck runs and runner
availability before claiming CI coverage is healthy. Headless smoke still cannot prove rendered
selection/drop behavior or the Nuke Ops happy path.

- Related: [networking-session.md](systems/networking-session.md), [Testing/ui/README.md](../../Testing/ui/README.md), [test-server.md](../milestones/test-server.md) T3

### 1.11 StyleCop disabled repo-wide with no replacement gate

**Blast radius: low today, compounding — trend: stable**

StyleCop analyzers were disabled project-wide on 2026-07-14 "to reduce friction on AI-assisted
edits" ([FORK_STATUS.md](../FORK_STATUS.md) § Analyzer tooling). `.editorconfig` still encodes the
naming/formatting rules, but nothing enforces them in CI — conformance now depends entirely on
reviewer attention (human or AI) at PR time, with no automated signal when a change drifts from
convention. As the volume of AI-assisted PRs grows, this is the kind of gate that's cheap to skip
early and expensive to reinstate once thousands of lines have drifted.

### 1.12 Scene-wide `FindObjectsByType` calls in gameplay code, not just Editor tooling

**Blast radius: low-medium — trend: improving for locator; Item/Locker scans unchanged**

Scene scans remain in gameplay code: `TileMap.ClearUntrackedItems` scans all `Item`s during
map restore/cleanup, `Locker.CloseStorageUIs` scans `ContainerViewer`s on close, and
`Core/Subsystems.cs` falls back to `FindObjectOfType` on a registry miss. These are lifecycle/event
paths, not evidence of a per-frame scan. The locator fallback is skipped while quitting and while
missing-subsystem errors are suppressed (Disconnecting / WaitingForServer); prefer `TryGet` and
world-readiness gates. A registry/event-based replacement for the Item/Locker scans remains open.

More gameplay scans found at `06316a98`:
- `StructuralDamageSubSystem.cs:185` — `FindObjectsByType<HumanHealthController>` **once per tile** a
  blast reaches, with a fresh `HashSet` each call. A tile revisited with more force damages its
  bodies twice. `ApplyCrewBlastPush` already runs once per blast over the force map; brute damage
  should too.
- `TileSubSystem.cs:291` (every decal sync), `TileLayerVisibilityService.cs:151` (every layer toggle),
  `MapEditorSettings.cs:68`, `BlastVfxPresenter.cs:62`, `SolarTrackingBeacon.cs:78`,
  `LightPower.cs:334`, `RoundObservability.cs:137-141` (all GameObjects, every round reset).
- `AudioSourceOcclusion.cs:68-80` — every playing source rescans for an `AudioListener` **every
  frame** while none exists, because the miss is not cached.
- `TileMap.ClearUntrackedItems` still uses the deprecated `FindObjectsOfType` and `DestroyImmediate`s
  networked items (`TileMap.cs:1195-1216`).

- Related: [core-subsystems.md](systems/core-subsystems.md), [tile.md](systems/tile.md), [2026-07_session-world-lifecycle.md](../archive/architecture/2026-07_session-world-lifecycle.md)

### 1.13 Debug tools centralized; UI-host consolidation remains

**Blast radius: medium — trend: improving (F1 registry shipped; separate panels remain)**

Player settings Phase 3 (`fdf2de0d8`, PR #90) replaced scattered H/P/I/L/Y/F2–F6 debug keys with
one arbitrated F1 window in Editor/development builds. `Core/Debugging/DebugTools` owns named
registrations; tool owners subscribe to `Invoked` and provide `IsOn` for toggles. Old debug action
bindings are erased at boot so those keys are available for player rebinding. F12 remains console.
The old collision table is obsolete; do not restore per-tool keys.

**Remaining debt:** `UI/MainHud/Dev/DebugMenuView` self-bootstraps a DDOL host and private
`UIDocument`, loading PanelSettings from `MainHudAssetCatalog`. It does not attach to
`UiShell`'s Debug layer. Seven hosts own a private `UIDocument` at `06316a98` (Main HUD, Storage
Panel, Machine UI, F1 window, `AlertStackDebugMenuView`, the new `FpsCounterView`, and
`MapEditorSubSystem`), arbitrated by hand-picked sorting orders (−10 / 2000 / 3000) on one shared
PanelSettings. Of 11 UI `RuntimeInitializeOnLoadMethod` bootstraps, only four attach to the shell. Domain panels still own their presentation, including the condemned
ScreenEffects canvas. Fold these hosts into the shared shell when their presentation is replaced;
keep console commands as the scriptable debug API.

- Related: [inputs.md](systems/inputs.md) §Pitfalls, [ui-shell.md](systems/ui-shell.md),
  [2026-10_player-settings.md](2026-10_player-settings.md) Phase 3

### 1.14 Asset/file organization drift (icons scattered across 8+ locations)

**Blast radius: low individually, high in aggregate — trend: Phase 1 icons done; leftovers scheduled**

The intended `Assets/Art/` (raw art, by type then domain) vs. `Assets/Content/` (game data/composition) split
is sound. Phase 0 (docs/tooling + CI `AssetTaxonomyTests`) and **Phase 1 icon consolidation** have landed:
icon images now live under `Assets/Art/Icons/` (`External/`, `Heroicons/`, `Inventory/`, `Alerts/`,
`Rendered/`, `map-editor/`). `Graphics/Misc` and `Graphics/UI/Misc` were disposed with those moves.
Still open from Phase 1: flatten `Content/Systems/UI/Systems/` (also holds orphan `Construction.meta` / `Tilemap.meta` with no folder), delete orphaned `Scripts/External/FishNet/`
stubs, disposition animation/localization Misc folders, move `splatter.png`. Phase 2 still owns the
InteractionIcons Art/Content folder-name collision, Substances → `Content/Data/`, and first-party
`Assets/Editor/` → `Scripts/SS3D/Editor/`. Grandfather lists in `AssetAuditUtilities.cs` shrank for the
shipped icon moves — do not grow them for new violations.

- Related: [asset-organization.md](systems/asset-organization.md), [data-codegen.md](systems/data-codegen.md)
  (same one-off-Editor-menu root cause),
  [2026-07_asset-file-structure-taxonomy.md](2026-07_asset-file-structure-taxonomy.md)

### 1.15 Addressables async loading partial — most DBs still eager-loaded

**Blast radius: high (whole-game memory footprint) — trend: Phases 1–3 done; rest scheduled**

21+ Addressables groups under `Assets/Content/Addressables/` were historically editor-curation only.
**Phases 1–3 shipped (2026-07-23)** per
[2026-07_addressables-expansion-migration.md](2026-07_addressables-expansion-migration.md): orphan
`AddressableAssetsData/` removed; `com.unity.addressables` 2.9.1 pinned; `AssetHandle`/`AssetProvider`
+ `Assets.GetAsync` dual-path live; **InteractionIcons** migrated to `AddressablesAsync` (GUID keys,
warm preload, no eager SO hard refs). Items/Materials/Sounds/etc. still eager-load — Phase 4 needs
FishNet preload ordering; Phase 5 deletes sync `Assets.Get`; Phase 6 retires UI `Resources.Load`
catalogs. Same root cause as upstream [RE-SS3D/SS3D#1494](https://github.com/RE-SS3D/SS3D/issues/1494)
until remaining DBs migrate.

- Related: [data-codegen.md](systems/data-codegen.md)

### 1.16 Session/world lifecycle — shipped; optional backoff remains

**Resolved 2026-07-23** — see [§6 Resolved](#6-resolved). Optional reconnect exponential backoff and
UI-host consolidation remain deferred elsewhere (not reopen criteria for this item).

### 1.17 Incomplete tile knowledge treated as confirmed open space

**Blast radius: medium (float, vacuum, any future tile-backed probe) — trend: partially mitigated; still a contract gap**

Server owns a full tilemap after `TileMapLoaded`; pure clients hold an empty map that fills via HashGrid
AOI `PlacedTileObject` mirrors (layers can arrive out of order — non-plenum before Plenum). Gameplay
probes that treat `!TryGetOccupancy` or client `!HasPlenum` as “confirmed void” will false-positive at
join/AOI lag, and packing that into a SyncVar (e.g. Floating) makes the mistake sticky for owners who
then “keep coast” while local support is still Unknown.

**Partial paydown (2026-07-26):** `HumanoidSupportState` / `HumanoidSpaceSupport.GetSupportAt`
(`Unknown` / `Supported` / `Unsupported`) — client miss and client `!HasPlenum` → Unknown; server
occupancy-miss → Unsupported only after `TileMapLoaded`; server clears Floating while Unknown. Health
vacuum sampling uses the same probe. See [entities.md](systems/entities.md) § Pitfalls (client spawn
float) and [health.md](systems/health.md) § Pitfalls (Unknown vs Unsupported).

**Still open:** this is not yet a shared tile-query contract. New float/vacuum/passability/vision-style
call sites can reintroduce “miss = space.” Prefer `GetSupportAt` (or a future tile façade), gate
server past-map conclusions on `TileMapLoaded`, and keep sparse-client *consequences* server-authoritative.

- Related: [tile.md](systems/tile.md) § Pitfalls (client mirror ≠ full map), [entities.md](systems/entities.md), [health.md](systems/health.md), [core-subsystems.md](systems/core-subsystems.md) (`WorldReadyPhase.TileMapLoaded`)

---

### 1.18 Structural impact traffic unmeasured

Every landed hit on a structure now sends `RpcStructuralImpact`, and any non-steel break sends one per destroyed tile. A shot on a structure drops its `RpcPlayAudioSource`, so gunfire traffic should fall, but nobody has measured the net change; the owner dropped that measurement from [ambience-and-impact-audio](../archive/architecture/2026-11_ambience-and-impact-audio.md). Measure it if [test-server.md](../milestones/test-server.md) T3 runs over budget during a firefight.

Same class, found at `06316a98`: one Map Editor edit re-sends whole-map state to every client.
Each `SetFloorDecalCommand.Apply` calls `SyncFloorDecalsToClients` (`TileSubSystem.cs:264-285`, every
chunk, unbatched — a 30-tile drag sends 30 snapshots), and `AreaSubSystem.NotifyAreaVisualsChanged`
(`:1172-1226`, every chunk's room ids, names and ambience) runs from 11 call sites. Send changed
chunks batched per command group; keep one full snapshot for late joiners (M).

### 1.19 Server trusts client input in several ServerRpcs

**Blast radius: high (public sandbox server) — trend: getting worse (each feature adds RPCs)**

43 of 74 ServerRpcs use `RequireOwnership = false`. Most check permissions (`ServerSettings`,
`MapEditorRoundStop`, `EntitySubSystem.CmdSpawnLatePlayer`), but these do not:

- **Ranged shot origin:** `CmdSyncMeleeAim(rayOrigin, aimPoint)` stores the client's origin unchecked
  (`CombatInteractionNetwork.cs:848-854`); `RangedHitscanResolver` fires and checks line of sight from it
  (`:52`, `:64`). A modified client can shoot from anywhere, through walls. Derive the origin on the
  server (muzzle/eye) and take only a clamped direction from the client.
- **`Ragdoll.Knockdown` / `KnockdownTimeless` / `Recover`** (`Ragdoll.cs:347`, `:382`, `:817`): any
  client can knock down or stand up any body. The only caller is `Hacks/RagdollWhenPressingButton`;
  admin paths use the `Server*` variants. Delete the RPCs.
- **`ItemSubSystem.CmdSpawnItem` / `CmdSpawnItemInContainer`** (`ItemSubSystem.cs:23`, `:63`): no
  permission check. Gate on admin (the console command calls the server method anyway).
- **Console commands at `AccessLevel User` run on the server for any player**, in release builds
  (`CommandsController.CmdProcessOnlineCommand`). These include `accessgrant` / `accessrevoke` /
  `accesspreset` (any access level, on yourself or on another ckey) and `spawnitem`. Sandbox-server §5
  says players get no console. An unknown command name throws, because `FindCommand`'s result is
  ignored (`:144`).
- **`Draggable.SetDragServer(bool, NetworkObject)`**: trusts a client-supplied dragger.
- **Throw target height** is unbounded and NaN isn't rejected (`ThrowRules.cs:71-84` clamps
  horizontal distance only).
- **Combat one-shots** start via client `HumanoidBodyStateMachine.CmdFireTrigger` (`:369`), and the
  combat stance is computed on the client and sent unvalidated
  (`HumanoidBodyStateBridge.cs:161-178`). The server already knows the intent and the inventory.
- **Dead but reachable:** `TileSubSystem.RpcPlaceObject` / `RpcClearTileObject` / `RpcClearItemObject`
  / `RpcSetFloorDecal` / `RpcClearFloorDecal` (`:192-261`) and
  `ConstructionHologramManager.RpcSendCanBuild` (`:1373-1418`) have zero callers. Under
  `-openbuild` / `-sandbox` they let any non-Restricted player bypass `MapEditorSandboxRules`, the rate
  limiter and undo. Their XML comments ("Requires Administrator") are wrong. Delete them.
- **Server status query** (vendored Tugboat patch, `ServerSocket.cs:139-145`, `:378-395`;
  `ServerStatusPublisher.cs:86-95`) answers unconnected UDP with no per-endpoint rate limit, and
  replies are larger than requests. Add a token bucket.

**The opposite failure:** server re-validation runs the same strict `CanInteract` (for Drop, a 0.1 m
LOS hit tolerance) against a server pose that lags the client, with no tolerance and no player
feedback — the likely cause of [#192](https://github.com/henkhooft/SS3D/issues/192). One rule for both:
the server derives positions itself and validates with an explicit lag margin.

### 1.20 Hand actions and upper-body pose have no single owner

**Blast radius: medium-high (every new hand action, animation phase) — trend: getting worse**

§1.2 settled who owns collapse/death presentation, not the upper body.

- **Three server "act at the clip frame" timers**, each with different checks: melee connect
  (`CombatInteractionNetwork.cs:653-677`), throw release (`.Throw.cs:463-474`), interaction hand actions
  (`InteractionController.Animation.cs:155-181`). Throw ignores a pending hand action and vice versa
  (`.Throw.cs:343`, `.Animation.cs:121`), against the animation pass's "one pending hand action" rule.
- **"Can act" is defined five ways:** `Hand.CanUseHand` (`Hand.cs:58-66`),
  `HumanoidBodyStateMachine.CanPerformAction` (`:449`), `CombatDummyBootstrap.cs:81`, and the two timers.
  Only the dummy considers `IsGettingUp`, so a body can shoot or melee during the 2.2 s stand-up.
- **Pose:** `AnimationOrchestrator` arbitrates priority from its own timers (`:476-558`);
  `HumanoidIkController` reads Animator state hashes with a hard-coded swing list (`:17-22`) while the
  orchestrator derives its list from the swing table (`:109`); `Ragdoll` writes the Animator and
  toggles the orchestrator and state machine directly (`:476-507`, `:731`, `:790-793`, `:874`). The
  snapshot's `BodyState.Ragdoll` repeats `Presentation`. The facing rule is copied
  (`.Throw.cs:175`, `.Animation.cs:88`).
- **Effect timing keys on name strings:** `InteractionAnimation.cs:208-231` picks clips by
  `GetGenericName()`; `"Drop"` is hard-coded (`HumanInventory.cs:348`, `:410`). Renaming an
  interaction silently changes when its effect lands.

**Fix:** one server `HandActionScheduler` with one slot and one `CanAct(hand)` rule; interactions
declare their own animation hint; the server fires one-shots after validating; IK reads one
upper-body model (M–L).

### 1.21 Item holder and visibility are written by many classes

**Blast radius: medium — trend: recurring bugs (#203, #208)**

Where an item is held and whether it renders is set by `AttachedContainer` (add/remove plus
NetworkTransform toggles, `:338-432`), `ContainerItemDisplay` (`:105-122`, `:171-197`),
`DisposalSubSystem` (`:199-352`) and `DisposalOutlet` (`:143-146`). Release-to-world exists three
times: `Hand.PlaceHeldItemOutOfHand` (`:213-232`, registers TileMap tracking even for a throw),
`HumanInventory.ServerPlaceItemInWorld` (`:429-438`), and `Hand.Dump` (`:193-199`, **skips** tracking).
The #208 fix guards out-of-order network arrival with a per-peer `Item.LocalHolder`
(`AttachedContainer.cs:368`) instead of one replicated holder.

**Fix:** a replicated holder on `Item`, applied by one routine on every peer; one
`ServerReleaseToWorld` for drop, place and throw (M–L). Also: `HumanInventory.CmdTransferItem`'s error
branch dereferences a null container (`:538-540`).

### 1.22 Map Editor trusts the client's partial map and assumes every command succeeded

**Blast radius: medium (remote editors, sandbox) — trend: new**

- **§1.17 reappears:** remote editors validate placement against their AOI-limited mirror
  (`ConstructionHologramManager.cs:813-820`, `:1106`) and drop "invalid" cells before sending; delete
  targets also come from the client map (`:923-1005`). Spawn points never reach clients, so remote
  admins cannot see or delete them. `PlacedItemObject` is added at runtime (`PlacedItemObject.cs:59`), so
  on a pure client item picking (`MapEditorCursorPick.cs:54`, `ConstructionHologramManager.cs:1261`)
  never finds one and selects the tile under it. Let the server decide validity; show "unknown" when a
  chunk is missing; anchor observer visibility to the editor camera.
- **Undo:** `MoveTileCommand.Move` (`MapEditorCommands.cs:241-249`) clears the source, then places with
  a build check. If the place fails the object is gone, yet `RpcMoveObject` reports success
  (`MapEditorSubSystem.cs:1615`) and undo clears whatever blocked the destination.
  `ClearTileCommand.Revert` / `ClearItemCommand.Revert` (`:154-161`, `:194-195`) re-place with
  `replaceExisting: true` even when nothing was cleared. `RpcMoveObject` skips the rate limiter. Record
  `Apply` success; route moves through `RpcExecuteCommands` (S–M).
- **`MapEditorView` rebuilds on any state change** (`_vm.StateChanged += Refresh`, `:100`, including
  toasts and undo state), rebuilding the palette grid and open popovers. This is the root cause of three
  Pitfalls in [map-editor-creative-hooks.md](systems/map-editor-creative-hooks.md). Split the event (M).

### 1.23 World-simulation suspension copied four times and cannot nest

**Blast radius: medium — trend: new**

The same pause/restore block lives in `MapEditorSubSystem.cs:461-497`, `TileSubSystem.cs:466-515`,
`PersistenceSubSystem.cs:370-436` and `MapImportApplier.cs:94-202`.
`ElectricitySubSystem.SuspendCircuitUpdates(bool)` (`:197`) is a plain flag, so loading a map with the
editor open turns circuits back on while the editor is still open. Opening the editor is a local UI
action, yet on a host it pauses atmos and power for everyone, and a remote editor pauses nothing.
**Fix:** one ref-counted, disposable `WorldSimSuspension` scope, decided by the server (S–M).

### 1.24 `SS3D.Systems` is one tangled assembly

**Blast radius: high (compile iteration, hidden coupling) — trend: getting worse**

894 runtime files / ~114k lines; 25 asmdefs depend on it. Among 36 top-level `Systems/*` folders, 53
pairs depend on each other. Tile, meant to be a base layer, uses 19 other folders (12 even without
MapEditor/MapImport: `Tile/Connections/` holds electricity, pipe and disposal rules; `TileSubSystem`
imports Atmospherics and Persistence; `TileMap.Clear` calls into Area and Tile markers,
`TileMap.cs:826-859`). The core (Tile / Area / Atmos / Electricity / Entities / Inventory) is one
strongly connected component, so a split by domain is expensive.

**A split by edge works now** — move high-fan-out, low-fan-in leaves into outer assemblies that
reference Systems:
- **MapEditor + its UI** (~8.9k lines inside Tile).
- **Test automation:** `Testing/AutomationSubSystem.cs` (1,328 lines, 13 Systems namespaces) has no
  `#if` and is created in every build by `SystemsBootstrap.cs:88`, gated only at runtime by
  `-testscript=`. Move it to an `SS3D.Automation` asmdef compiled under a harness/CI define.
- **IngameConsole, BugReports.**
- **UI views inside Systems:** 26 files (~11k lines) use `UnityEngine.UIElements`. The inverse problem:
  the MachineInterface UI assembly hosts 11 gameplay device controllers (~2.8k lines, e.g.
  `FilterInterfaceController.ApplyControl` → `Filter.Toggle()`), so content prefabs depend on a UI
  assembly, and `UI.Shell` is accumulating feature views (BugReport, RoundEnd, MainMenu) because it is
  the only UI assembly Systems may reference.

That removes ~25k lines from the recompile set (L). Also: `SS3D.Systems.asmdef` references the base
`SS3D.UI` assembly with zero uses (§1.29).

### 1.25 `SubSystems` registry lifecycle bugs and `Get` misuse

**Blast radius: medium (silent nulls, wrong instance) — trend: stable**

- `Core/Subsystems.cs:33-36`: `_isQuitting` is set by `Application.quitting` and never reset. Domain
  reload is on in project settings, but the MCP / Test-Runner path enters Play Mode without one
  ([application.md](systems/application.md)); there `Get` returns null for the rest of the session.
  `_suppressMissingErrors` and the dictionary also survive. Reset on `SubsystemRegistration`.
- `Unregister` (`:115`) removes **by type**, so a destroyed duplicate (content-prefab SubSystem vs.
  hub/bootstrap instance) evicts the live one. Remove only when the registered instance matches.
- `TryGet` (`:50-53`) returns true for a destroyed instance. Add a Unity-null check.
- 35 sites write `SubSystems.Get<T>()?.` — they expect a miss but call `Get`, which on a miss runs
  `FindObjectOfType` and logs an Error. Codemod to `TryGet` (S).

### 1.26 `com.unity.entities` installed for one unused World

**Blast radius: low-medium (player and server footprint) — trend: stable**

`Atmospherics/ECS/AtmosWorld.cs` is the only `using Unity.Entities` in the repo; `AtmosSubSystem`
creates and disposes it and nothing else touches it. The "ECS" folder is Burst `IJob`s on NativeArrays.
`com.unity.entities` 1.5.0 has no `UNITY_DISABLE_AUTOMATIC_SYSTEM_BOOTSTRAP`, so a default World and
its systems run in every player, including the dedicated server. Delete `AtmosWorld`, drop the package,
rename the folder to `Jobs` (S). [FORK_STATUS.md](../FORK_STATUS.md)'s "ECS world" wording is misleading.

### 1.27 Tick-driven systems share no clock

**Blast radius: medium — trend: stable (each new sim picks its own)**

Five patterns: Atmos `Update` + `deltaTime` accumulator (`AtmosSubSystem.cs:306-317`); Electricity
Coimbra `FixedUpdateEvent` with `_timeElapsed = 0` discarding the remainder
(`ElectricitySubSystem.cs:234-253`); one reset-to-zero timer per body in Health
(`HumanHealthController.cs:653-666`); `UniTask.Delay` loops in Rounds; FishNet `TimeManager.OnTick` for
movement. `ElectricitySubSystem.RpcInvokeOnTick` (`:685-688`) is a payload-less `ObserversRpc` sent to
every client 5×/s so client visuals can tick. **Fix:** one `SimulationClock` on `TimeManager.OnTick`
with fixed-rate channels; clients tick locally (M). Health also allocates `_organs.ToArray()` once
per modifier per tick (`HumanHealthController.cs:641`, §2).

### 1.28 Persistence contributors deserialized by a central switch

**Blast radius: low-medium — trend: stable**

`PersistenceSubSystem.DeserializePayload` (`:477-488`) switches over all five contributor IDs, so
Persistence must know every domain's payload type, which helps keep the Persistence↔Tile cycle.
Let `IPersistenceContributor` deserialize its own payload (S).

### 1.29 UI widget and host duplication

**Blast radius: medium, compounding — trend: getting worse**

- HUD/menu/MI visibility handling is copy-pasted (`MainHudSubSystem.cs:757-892` vs.
  `StoragePanelHost.cs:204-306`) and has drifted: StoragePanel never subscribes to the map editor, so
  storage panels stay up during map editing while the HUD hides.
- Window drag exists three times (`StoragePanelView.cs:180-252`, `MachineWindow.cs:71-130`, the new
  `PanelDragManipulator`); the press-to-drag threshold three times with different values (5 px in
  `EquipmentGrid` / `HandsGearStrip`, 6 px in `StorageSlot`).
- The shared `InventorySlot` lives in `UI/MachineInterface/Components`, forcing MainHud, StoragePanel
  and Examine to reference the MI assembly. Move it and the manipulators into `SS3D.UI.Shell` or a new
  `SS3D.UI.Widgets`.
- Shared scaffolding is unused: `UiBinderBase` has zero subclasses (the three new MI binders skipped
  it); `PanelAnimator` has one consumer while `MainHudView` and `MachineInterfaceHost` hand-roll DOTween.
  Adopt or delete.
- The base `SS3D.UI` assembly (LabelButton, ToggleLabelButton, GenericTab*) has no prefab or scene
  references beyond `LocalizeFont` on `PlayerCanvas.prefab`. Purge candidate (S).
- `RespawnPrompt` is all inline styles and rewrites its label every frame (`:70-84`, `:96-127`).

### 1.30 `HumanoidPredictedMovement` disabled but maintained

**Blast radius: low-medium — trend: drifting**

The component is `m_Enabled: 0` on `Human.prefab`, yet its 612 lines are still maintained. Its
space-float / coast code duplicates `HumanoidLivingController` (`HumanoidPredictedMovement.cs:304-352` vs.
`HumanoidLivingController.cs:250-304`) and has already drifted (the #124 fallback exists only in the
living path). Delete it, or schedule it as the replacement for client-authoritative movement (S–M).
Related small cleanups: `RpcExecuteClientInteraction`, `RpcExecuteClientInventoryInteraction` and
`RpcExecuteMeleeSwing` are `ObserversRpc`s that non-owners ignore — make them `TargetRpc`s;
`HumanHealthController.ApplyKnockback` is dead (only caller passes 0); `.Throw.cs` allocates every frame
in throw mode (`RaycastAll`, `SphereCastAll`, `LayerMask.GetMask` by string at `:260`, `:286`, `:445`).

## 2. Hot-path GC / performance debt (tracked, partially paid down)

PR #13 fixed a first wave of per-tick allocation ([FORK_STATUS.md](../FORK_STATUS.md) § Hot-path
performance): `TileCoord` boxing on dictionary lookups, atmos GPU-upload allocations, pipe-network
list churn, and `SubstanceContainer.AsReadOnly()` on bleed ticks. The pattern keeps recurring in new
code rather than being structurally prevented (no analyzer/test flags a new per-tick `List.AsReadOnly()`
or `new float[]` in a hot loop) — treat every new tick-driven system (atmospherics ports, area
recompute, electricity distribution) as needing the same review pass PR #13 did, not as automatically
safe because "the pattern was fixed once."

- Full list of pitfalls-with-fixes: [tile.md](systems/tile.md), [atmospherics.md](systems/atmospherics.md), [substances.md](systems/substances.md) § Pitfalls
- **Coimbra PackageCache GC (Editor):** `ServiceLocator.GetChecked` builds `$"..."` assert messages every call (~724 B); `EventSystem.Invoke` pairs with unconditional `String.Format` in deep profiles. Lives in `com.coimbrastudios.core` — do not patch PackageCache; cache `IEventService` / avoid hot-path `GetChecked`, or vendor later. Area/device tile resolution still recomputed per electricity tick (`TileMap.TryGetAreaId` / `GetTileInFront`) — defer a cache pass after re-profile. See [core-subsystems.md](systems/core-subsystems.md).

---

## 3. Prefab / mega-object composition debt (tracked in [agent-first-composition.md](2026-07_agent-first-composition.md))

That doc is the canonical register for condemned UI and prefab composition debt (§ Condemned UI, §
Prefab composition debt tables). Don't fork a duplicate list here — see 1.1 and 1.8 above for the
two items from that table with the highest current risk, and check that doc directly for the full
tables before starting any redesign that touches `Human.prefab` or a condemned surface.

---

## 4. Known correctness bugs living in shipped infrastructure

- **Dedicated server vs. real client:** selection outline and drop interaction broke when a real
  client connected to a dedicated server build; not root-caused. Tracked as [test-server.md](../milestones/test-server.md)
  T3; origin in [2026-07_headless-dedicated-server.md](../archive/architecture/2026-07_headless-dedicated-server.md) § Known issues.
- **Multiplayer harness gaps:** mouse/screen-space interaction and pocket/container round-trip are
  not covered by the automated harness yet, so regressions there ship silently.
  [Multiplayer testing](systems/networking-session-testing.md).

---

## 5. Accepted fork deviations (not debt — do not "fix" without a design change)

Several intentional divergences from design docs are already recorded as accepted, not scheduled for
rework, each directly in its owning system map: Main HUD fully hides rather than layers under machine
UI ([inventory.md](systems/inventory.md) § Fork deviation), Area is APC-seeded rather than
auto-detected ([area.md](systems/area.md)), combat's primary click always swings rather than requiring
a hover target ([combat.md](systems/combat.md)), and examine always shows the name tooltip on plain
hover rather than gating everything behind the hold key ([examine.md](systems/examine.md)). Listed
here only so this doc doesn't accidentally get read as "these are bugs" — they are direction calls,
not quality problems.

---

## 6. Resolved

*(Move items here with the PR/commit that closed them, so the register shows real progress rather
than only growing. Keep a one-line stub under the old §1.x number so external citations still resolve.)*

### 1.2 Body presentation authority — 2026-07-23

`Ragdoll` owns replicated `BodyPresentationState` (`Locomotion` / `Collapsed` / `Dead`) and is the
sole applier. Health writes intent via `BodyPresentationIntent`; dual death/unconscious reinforce
RPCs removed. Movement / bridge / orchestrator read `Presentation`.

- **Closed by:** this session's body-presentation refactor (TECH_DEBT 1.2 plan).
- **Not in this close:** ghost/mind-swap redesign; collapse animation content; prefab strip.
- Related: [2026-07_body-presentation-authority.md](../archive/architecture/2026-07_body-presentation-authority.md), [health.md](systems/health.md), [entities.md](systems/entities.md)

### 1.3 Interaction Discover contract — 2026-07-23

Discover semantics locked: candidates vs `FilterAndSort` viability; `InteractionEntry.IsSourceOnly` /
`SourceOnly()` for Drop; `InteractionEvent.HasPoint` replaces zero-point sentinel; source discovery
receives hit `context`. Pickable≠rangeable (missing wall-mount colliders) remains open under the
framework map.

- **Closed by:** `0499ab7cd` on `cursor/interaction-discover-contract` ([2026-07_interaction-discover-contract.md](../archive/architecture/2026-07_interaction-discover-contract.md)).
- **Not in this close:** forcing one extension gate style; wall-mount colliders (pickable≠rangeable).
- Related: [interactions-framework.md](systems/interactions-framework.md), [interactions-runtime.md](systems/interactions-runtime.md)

### 1.6 Crafting dead-code purge — 2026-07-23

Obsolete `CraftingSubSystem`, hand/tool `Craft`, `GirderCraftable`/`RecipeIngredient`, crafting menu
uGUI, recipe assets, `CraftingRecipes` database/Addressables group, and generated recipe refs removed.
Empty-hand outline pollution from `OpenCraftingMenu` discovery is gone with the extension.

- **Closed by:** this session's crafting deletion (TECH_DEBT 1.6 plan).
- **Not in this close:** future freeform crafting per [design/crafting.md](../design/crafting.md);
  optional leftover `InteractionIcons.Crafting` sprite/codegen entry (lookup never used that id).
- Related: [crafting.md](systems/crafting.md), [interactions-framework.md](systems/interactions-framework.md)

### 1.7 Legacy scene-based subsystem registration (gameplay) — 2026-07-23

Gameplay SubSystems are code-owned: `SystemsBootstrap` (DDOL process-wide) + `NetworkSystemsHub`
(Online spawn). Boot/Game no longer place per-system SubSystem GameObjects
([2026-07_session-world-lifecycle.md](../archive/architecture/2026-07_session-world-lifecycle.md) Phase 3h).

- **Closed by:** `b74f47123` (Phase 3h hub + scene strip); post-ship hardening `ab79afee2` (ServerMeta
  boot ownership), `b57f3974e` (PlayerCamera lazy resolve); smoke hardening (Selection/Armed
  `TryGet`, Disconnecting suppress).
- **Not in this close:** UiShell / MainHud / StoragePanel `RuntimeInitializeOnLoad` — agent-first
  follow-on **(b)**. Also residual: content-prefab `SubSystem`s (PlayerCamera / Radial / Armed /
  MapEditor) that still register when Game loads before hub Online — consumers use `TryGet` until
  those move to bootstrap/hub ([2026-07_session-world-lifecycle.md](../archive/architecture/2026-07_session-world-lifecycle.md)).
- Related: [core-subsystems.md](systems/core-subsystems.md), [2026-07_agent-first-composition.md](2026-07_agent-first-composition.md)

### §4 `C` double-bound (Cancel vs Help/Harm) — 2026-07

The default input scheme moved Cancel Interaction to **Backspace** and intent to **F**
([2026-07_default-input-scheme.md](../archive/architecture/2026-07_default-input-scheme.md); [inputs.md](systems/inputs.md)).

### 1.16 Session/world lifecycle — 2026-07-23

Session FSM, Empty offline, world-readiness graph, `PrepareRound` gate, `SystemsBootstrap`, and
`NetworkSystemsHub` shipped under
[2026-07_session-world-lifecycle.md](../archive/architecture/2026-07_session-world-lifecycle.md) (`Status: shipped`).

- **Closed by:** `77f4d9798` / `84401b2fe` / `b74f47123` (phases 1–3h); follow-up Play Mode fixes on
  `cursor/session-world-lifecycle` as above.
- **Deferred elsewhere (do not reopen this item):** reconnect exponential backoff; UI-host bootstrap
  consolidation.
- Related: [networking-session.md](systems/networking-session.md), [core-subsystems.md](systems/core-subsystems.md)
