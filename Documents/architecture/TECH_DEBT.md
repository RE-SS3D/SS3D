# Technical debt tracker

**Last updated:** 2026-11-01

Checked against `259787487` (source and workflow inspection). Runtime and CI-run results below
are prior observations, not new test results from this documentation refresh.

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

Machine UI, Main HUD, Storage Panel and now the Lobby each ship their own `*AssetPaths` constants
class + plain `ScriptableObject` catalog, rebuilt by the umbrella **SS3D → Data → Rebuild All UI
Catalogs**. Examine adds a lighter one-off (`ExamineOverlayAssetCatalog`). `UiAssetCatalogBase`
shipped with [ui-shell](systems/ui-shell.md) Phase 0–1, but only `UiShellAssetCatalog` derives from it
(checked 2026-10-31). The fourth copy this item predicted landed with the lobby redesign. Each copy
shares the same failure mode: a stale catalog after adding a UXML path without running the rebuild.

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

**Blast radius: medium — trend: interactions paid down; MainHud still open**

`InteractionController` decomposed (TECH_DEBT paydown): discovery/dispatch helpers, outline driver,
delayed tracker, and sibling `CombatInteractionNetwork` for Harm melee/ranged RPCs — see
[2026-07_interaction-controller-decomposition.md](../archive/architecture/2026-07_interaction-controller-decomposition.md)
(**shipped**). World/inventory/examine RPCs remain on the controller by design (follow-on optional).
`MainHudSubSystem.cs` (1,530 lines at `259787487`, up from ~1.1k in July) still composes equip/unequip, gear, hands, intent chip, zone-reticle,
and HUD/MI visibility in one class with **no** decomposition plan — remains open under this item.

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
nightly only) against real client/server builds on manual dispatch, labeled PRs
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
`UiShell`'s Debug layer. Domain panels still own their presentation, including the condemned
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
