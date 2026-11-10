> Implements: Documents/design/networking.md §1, §2 (no control input reaches a character from anyone else), §10 (server authority over outcomes); Documents/design/combat.md §2 (an interrupted windup does nothing), §3 (what you can't see, you can't hit), §5 (the knockdown window); Documents/design/sandbox-server.md §5 (no console for players; undo reverts your own edits), §6 (per-player throttling)
> Touches systems: core-subsystems, combat, entities, interactions-runtime, inventory, health, tile, map-editor-creative-hooks, area, structural-destruction, atmospherics, electricity, persistence, rounds-lobby, ui-shell, machine-interface, examine, ingame-console, networking-session, application
> Status: planned

**Progress:** Planned on 2026-11-09 from the code audit that refreshed [TECH_DEBT.md](TECH_DEBT.md) §1.19–§1.30. Nothing is built yet. The owner answered timing, Map Editor gating and pausing, `HumanoidPredictedMovement` and console access on 2026-11-09 (Decisions 9–12); four [open questions](#open-questions-for-the-owner) remain.

# Tech-debt paydown (Nov 2026)

The 54 commits since the last debt review (~56k lines) shipped features fast by going around structure: partial files instead of components, per-surface copies instead of shared widgets, client-side checks the server repeats or skips, and one assembly that everything lands in. None of it is broken in a way players see every round, but each pattern is now set to repeat with every new feature. This effort pays the debt down in ten phases, cheapest and most dangerous first. It changes no gameplay rule: where code disagrees with a design doc, the design doc wins, and the code changes to match.

Each item names its [TECH_DEBT.md](TECH_DEBT.md) entry. When an item ships, move its entry to TECH_DEBT §6 Resolved and sync the owning system map with `update-system-docs`.

## Starting state (2026-11-09, `06316a98`)

The ranked findings, with file and line evidence, are in [TECH_DEBT.md](TECH_DEBT.md) §1.4, §1.7, §1.9, §1.12, §1.13, §1.18 and §1.19–§1.30. In summary:

- **Authority:** 43 of 74 ServerRpcs skip ownership. Several trust client positions or let any client act on any body. Every console command at `AccessLevel User` runs on the server for any connected player in release builds. That includes `accessgrant` / `accessrevoke` (any access level, on yourself or on another ckey) and `spawnitem`. The server re-checks interactions against its own lagging copy of the player with no margin ([#192](https://github.com/henkhooft/SS3D/issues/192)).
- **Ownership:** hand actions, upper-body pose and item holding each have several writers.
- **Map Editor:** remote editors trust their partial map; undo assumes success; one edit re-sends whole-map state.
- **Infrastructure:** the subsystem registry has lifecycle bugs. World-sim pausing can't nest. Five tick patterns. An unused ECS World runs on the server.
- **Structure:** `SS3D.Systems` is 114k lines with 53 pairs of folders depending on each other. God classes grow through partial files. UI widgets are copied per surface.
- **Docs:** 24 of 49 system maps are 45 commits behind. Several say things that are no longer true (Phase 0).

## Decisions

Proposed by the agent; the owner confirms or overrides them in the [open questions](#open-questions-for-the-owner).

1. **The server derives every position it acts on.** Clients send intent: a target, an aim direction, a clicked object. The server works out origins (eye, muzzle, hand) from its own copy of the body and validates against them with an explicit, named lag margin. This one rule fixes both the trust holes and #192 (combat §3; networking §10).
2. **One writer per piece of state.** For each of hand-action scheduling, upper-body activity, item holder and world-sim suspension, exactly one component writes it and everything else reads it. A second writer is a review blocker.
3. **Delete before refactoring.** Dead and duplicate code goes first (Phases 1 and 3), so the later phases refactor less.
4. **Split assemblies by edge, not by domain.** The core simulation domains form one dependency cycle. Pulling the outer layers off (automation, Map Editor, console, bug reports, UI views) is cheap and removes ~25k lines from the recompile set. A domain split of the core is out of scope ([TECH_DEBT §1.24](TECH_DEBT.md#124-ss3dsystems-is-one-tangled-assembly)).
5. **Human.prefab changes go through recipes.** New components added to Human (hand-action scheduler, throw controller) are wired by a tier-B recipe on `HumanPrefabRecipes`, never by hand ([agent-first composition](2026-07_agent-first-composition.md)). A partial file added only to avoid a prefab edit is now a review blocker.
6. **One PR per item, behaviour-preserving unless the item says otherwise.** Each refactor starts with a characterization test at the cheapest tier that can see the behaviour, per the [testing philosophy](2026-11_testing-philosophy.md) rules 1–4.
7. **Bump `ProtocolVersion.Current` in any PR that changes an RPC signature or a SyncVar layout.** Bump `PersistenceEnvelope.CurrentSchemaVersion` only if a saved payload changes; none in this plan should ([game versioning](2026-10_game-versioning.md)). No item here ends with `Version-Bump: minor`; this work is not a player-visible feature.
8. **Guardrails ratchet.** The Phase 0 checks keep allowlists that may only shrink, the way `AssetAuditUtilities` grandfather lists do. A new violation fails the build. It does not get an allowlist entry (testing rule 7).

Settled with the owner on 2026-11-09:

9. **Timing:** Phases 0–2 before Playtest 1; Phases 3–9 after it, behind the gameplay focus.
10. **The Map Editor is admin-only, except on the sandbox.** `-sandbox` stays the one open mode (sandbox-server §5). The standalone `-openbuild` switch goes away (P1.11). **Editing pauses nothing**: only map load, clear and import suspend simulations (P2.2). The owner may revisit pausing for sessions whose main goal is map building. That would be an explicit, admin-only "freeze world" control, not a side effect of opening the editor.
11. **Delete `HumanoidPredictedMovement`** (P3.2). It is unused and keeps being maintained by mistake.
12. **Console access is owned by the open console-rework branch,** which separates player and admin commands. This effort doesn't change command access levels. G2 and the console half of P1.3 hand over to that branch; check it covers `accessgrant`, `accessrevoke`, `accesspreset`, `spawnitem` and the unknown-command crash before closing §1.19.

## Phase overview

| Phase | Goal | TECH_DEBT | Size | Depends on | When |
|---|---|---|---|---|---|
| [0](#phase-0--guardrails-and-doc-truth) | Ratchet checks; fix docs that are wrong | §1.9, §1.11, §1.19, §1.24 | S–M | — | Before Playtest 1 |
| [1](#phase-1--authority-hardening) | Close trust holes on the public servers; Map Editor admin-only off the sandbox | §1.19, §1.22 | M | 0 (G1) | Before Playtest 1 |
| [2](#phase-2--correctness-traps) | Small bugs that are one step from biting | §1.4, §1.9, §1.12, §1.20–§1.25, §1.29 | M | — | Before Playtest 1 |
| [3](#phase-3--dead-code-and-bootstrap-hygiene) | Delete what nothing uses; one bootstrap | §1.7, §1.26, §1.29, §1.30 | S–M | — | Any time |
| [4](#phase-4--gameplay-ownership-contracts) | One owner for hand actions, pose, item holder | §1.1, §1.9, §1.20, §1.21 | L | 1, animation pass shipped | After Playtest 1 |
| [5](#phase-5--map-editor-and-tile-authority) | Server-decided editing; tile query contract | §1.9, §1.17, §1.18, §1.22 | L | 1, 2 | After Playtest 1 |
| [6](#phase-6--simulation-and-server-infrastructure) | One sim clock; registries instead of scans; round split | §1.9, §1.12, §1.27, §1.28, §2 | M–L | 2, 3 | After Playtest 1 |
| [7](#phase-7--ui-consolidation) | Shared widgets; shell-hosted UI; god-class splits | §1.4, §1.9, §1.13, §1.29 | L | 2, 6 (P6.6) | After Playtest 1 |
| [8](#phase-8--assembly-split-by-edge) | Outer layers out of `SS3D.Systems` | §1.24 | L | 5, 6, 7 | After Playtest 1 |
| [9](#phase-9--lock-in) | Keep it paid down; archive | §1.11 | S | all | Last |

Phases 0–2 are small, independent PRs and protect the 24/7 Nuke Ops server ([test-server.md](../milestones/test-server.md) T8) and the sandbox. They are the only part proposed for before Playtest 1. Phases 3–9 do not block a playable gate and wait behind the [current focus](../milestones/INDEX.md#current-focus), except that Phase 3 items can fill gaps. Phase 4 waits for the [humanoid animation pass](2026-11_humanoid-animation-pass.md) to ship, since both rewrite the same files.

## Phases

### Phase 0 — Guardrails and doc truth

Cheap checks that stop the debt regrowing while it is paid down, plus fixes for docs that now mislead agents.

- **G1 — ServerRpc authority audit (EditMode, AssetAudit assembly).** Reflect over SS3D assemblies for `[ServerRpc(RequireOwnership = false)]` and for every `ServerRpc` taking a `Vector3`, `Quaternion` or `NetworkObject` argument. Each one must appear in an allowlist naming its check: `Admin`, `MapEditorPermission`, `OwnerOfArgument`, `ServerDerived` or `ReadOnly`. An `Unchecked` entry is allowed only for the RPCs Phase 1 fixes, and Phase 1 removes them. Verify first that FishNet's weaver leaves the attribute readable by reflection; if it doesn't, scan source instead.
- **G2 — Console command access audit:** handed to the console-rework branch (Decision 12). If that branch lands without a guard, add this check: every `Command` with `AccessLevel < Administrator` is marked `SelfOnly` or `ReadOnly`, and a Server-type command that changes world state or another player's state fails.
- **G3 — Class-size ratchet (EditMode).** Sum lines per type across its partial files under `Assets/Scripts/SS3D`. A grandfather list holds today's sizes for the classes in [TECH_DEBT §1.9](TECH_DEBT.md#19-god-classes-forming-in-hot-uiinteraction-code). A listed class may not grow by more than 10 % without updating its entry in the same PR. An unlisted class may not pass 900 lines. The point is a visible diff at review time, not a hard cap.
- **G4 — Systems folder dependency ratchet (EditMode).** Count pairs of `Systems/*` folders that depend on each other, taken from `using SS3D.Systems.X` lines outside `Editor/`. Fail if the count rises above the stored value (53 at `06316a98`). Phases 5, 6 and 8 lower it.
- **G5 — UI catalog umbrella check (EditMode).** Every `*AssetCatalog` ScriptableObject type has a builder registered in `UiCatalogRebuildAll`. It fails today on Examine until P2.6 lands, so ship G5 together with P2.6.
- **D1 — Fix docs that are wrong now** (not a full map re-verify; each later phase syncs the maps it touches):
  - `entities.md:16`: the orchestrator does not read `Presentation`; it relies on `BodyState.Ragdoll` and `SetPosingSuppressed`.
  - `entities.md`: add `InteractionAnimation.cs` and `InteractionController.Animation.cs` to Start here.
  - `interactions-runtime.md:177`: "thin router" → name the hand-action scheduling it now owns.
  - `tile.md:48`, `:71`: "admin-gated" is true only off the sandbox. Say: admin, or any non-Restricted player on `-sandbox` (sandbox-server §5). `-openbuild` goes in P1.11.
  - `ui-shell.md`: add the Map Editor and `FpsCounterView` to the not-migrated table.
  - `FORK_STATUS.md:544`: "ECS world" → Burst jobs on native buffers.
- **Tests:** G1–G5 are tier 1. `validate.sh --steps compile,editmode`.
- **Done when:** G1–G4 run in the EditMode CI job, and the D1 statements match code.

### Phase 1 — Authority hardening

Public servers are the Playtest 1 enabler, so trust holes are gameplay bugs there, not theory. All items are small. Bump `ProtocolVersion.Current` once for the phase, or once per PR that changes an RPC signature.

- **P1.1 — Ranged shot origin from the server.** `CombatInteractionNetwork.CmdSyncMeleeAim` (`:848`) stores a client ray origin that `RangedHitscanResolver` fires and checks line of sight from (`:52`, `:64`).
  - The server takes the origin from the body's `ViewPoint` (combat §3: line of sight from the eye) and only the aim point or direction from the client.
  - Reject an aim point that isn't finite or is beyond weapon range, and clamp the direction to a cone around the server-side facing; Q5 sets the margin.
  - Melee uses the same origin.
  - Test: EditMode on a pure resolver seam taking `(serverOrigin, clientAim)`; `scenarios:Combat`.
- **P1.2 — Remove client-callable knockdown.** Delete `Ragdoll.Knockdown`, `KnockdownTimeless` and `Recover` as ServerRpcs (`Ragdoll.cs:347`, `:382`, `:817`); callers use the existing `Server*` methods. Delete `Hacks/RagdollWhenPressingButton` (no prefab or scene uses it). Networking §2: nobody else drives your body.
- **P1.3 — Spawn permissions.**
  - Gate `ItemSubSystem.CmdSpawnItem` / `CmdSpawnItemInContainer` (`:23`, `:63`) on admin. The console calls the server method directly, so nothing legitimate needs the RPC open.
  - Console command access levels and the `FindCommand` null crash belong to the console-rework branch (Decision 12). Before closing [TECH_DEBT §1.19](TECH_DEBT.md#119-server-trusts-client-input-in-several-serverrpcs), confirm it makes `accessgrant`, `accessrevoke`, `accesspreset` and `spawnitem` admin-only. Sandbox §5 says players have no console.
- **P1.4 — Dragging.** `Draggable.SetDragServer(bool, NetworkObject)` → the server resolves the dragger from the caller's owned entity (`EntitySubSystem.TryGetOwnedEntity`) and checks range. It ignores any object argument.
- **P1.5 — Input sanitizing.** One helper (`RpcInput.TryFinite(Vector3)`, plus a clamp to the station bounds) used by every RPC that takes a vector. `ThrowRules.ClampToRange` (`:71-84`) clamps height as well as distance and rejects NaN.
- **P1.6 — Client-started animation and stance.**
  - Restrict `HumanoidBodyStateMachine.CmdFireTrigger` (`:369`) to an allowlist the client may start (emotes only). Combat one-shots come from the server after it validates the swing or shot; the full move to one upper-body model is P4.2.
  - Compute the combat stance on the server from intent and inventory, replacing `HumanoidBodyStateBridge.cs:161-178` → RPC.
- **P1.7 — Dead tile edit RPCs.**
  - Delete `TileSubSystem.RpcPlaceObject` / `RpcClearTileObject` / `RpcClearItemObject` / `RpcSetFloorDecal` / `RpcClearFloorDecal` (`:192-261`) and `ConstructionHologramManager.RpcSendCanBuild` / `RpcReceiveCanBuild` (`:1373-1418`). They have zero callers.
  - Send `RpcMoveObject` through `MapEditorRateLimiter` (sandbox §6), or fold it into `RpcExecuteCommands` (P5.9).
- **P1.8 — Server status rate limit.** Per-endpoint token bucket and a global cap in the vendored Tugboat unconnected-message path (`Assets/FishNet/.../Tugboat/Core/ServerSocket.cs:139-145`, `:378-395`; `ServerStatusPublisher.cs:86-95`). Record the patch in [FORK_STATUS.md](../FORK_STATUS.md) next to the existing vendored change.
- **P1.9 — Interaction re-validation margin (#192).**
  - Step 1, diagnostics: when `TryResolveDispatchedInteraction` fails, log which check failed (no entry with that name vs. `CanInteract` false). For Drop, also log the LOS hit distance and the server/client `ViewPoint` delta.
  - Step 2: give server re-validation a named margin. The LOS hit counts if it is on the same collider or within the margin, and range gets a margin (Q5).
  - Step 3: `TargetRejectInteraction` shows a short "can't reach that" hint (Q5, owner wording).
  - Test: EditMode on the margin rule; a `scenarios:Reach` case with the FishNet latency simulator at 100 ms dropping onto a table while strafing.
- **P1.10 — Owner-only RPCs.** `RpcExecuteClientInteraction`, `RpcExecuteClientInventoryInteraction` and `RpcExecuteMeleeSwing` broadcast to everyone, and non-owners ignore them → `TargetRpc` to the owner.
- **P1.11 — Map Editor admin-only outside the sandbox (Decision 10).**
  - `MapEditorPermissions` (`:25-31`) picks `OpenBuildMapEditorAuthorizer` only when `SandboxProfile.RequestedOnCommandLine`. Every other server uses `AdminMapEditorAuthorizer`.
  - Delete `OpenBuildMapEditorAuthorizer.RequestedOnCommandLine` and the `-openbuild` switch. No script or workflow passes it.
  - `IsSandboxPlayer`, `MayReplaceMapFile`, the rate limiter and the sandbox denylist keep keying off the open authorizer, so the sandbox behaves as before.
  - Update the docs that describe `-openbuild` as its own mode: `map-editor-creative-hooks.md:41`, `:54`; `tile.md` (with D1); `area.md`; [sandbox-server effort](2026-11_sandbox-server.md) Starting state and Implementation notes (mark the switch removed); [test-server.md](../milestones/test-server.md) T3 notes.
  - Test: EditMode on authorizer selection (no flag → admin; `-sandbox` → open); `sandbox` pilot once.
- **Tests:** `compile,editmode`, `scenarios:Combat`, `scenarios:Reach`, `scenarios:MapEditor`, `mppm:late-join` once at the end of the phase (it touches spawning and ownership).
- **Done when:** G1's allowlist has no `Unchecked` entries; outside `-sandbox`, a non-admin can't open the Map Editor or call its RPCs.

### Phase 2 — Correctness traps

Independent small PRs. Each fixes a bug that exists today or is one feature away from biting.

- **P2.1 — The HUD must not mutate weapons.** `MainHudSubSystem.RefreshZoneReticle` calls `ranged.ServerCompleteReloadIfDue()` (`:317`) and `CurrentSpreadDegrees` (which decays recoil) every frame.
  - Combat exposes an `ICombatReadout` with pure getters: ready fraction, reloading, bloom. Ready and bloom are computed from timestamps.
  - Reload completion becomes a server tick in Combat. Check first that nothing relies on the HUD finishing reloads on hosts: today a host's reload may complete only because the HUD polls it.
  - Gate the reticle's raycast and `GetComponentInChildren` on visibility.
  - Test: EditMode on readout purity (two reads with no time passing are equal; no state changes).
- **P2.2 — `WorldSimSuspension`.** One ref-counted, disposable scope in Core, owned by the server, replacing the four copies:
  - `MapEditorSubSystem.cs:461-497`
  - `TileSubSystem.cs:466-515`
  - `PersistenceSubSystem.cs:370-436`
  - `MapImportApplier.cs:94-202`

  `ElectricitySubSystem.SuspendCircuitUpdates(bool)` becomes counted. Opening the editor no longer suspends anything; only load, clear and import take a scope (Decision 10). Remove the editor-open suspension in `MapEditorSubSystem.cs:461-497`. The scope API is shaped so an admin "freeze world" toggle can take one later, if the owner wants it.
  - Test: EditMode on nesting (load inside an editor session leaves circuits suspended until both scopes close).
- **P2.3 — `SubSystems` registry.** `Core/Subsystems.cs`:
  - Reset `_isQuitting`, `_suppressMissingErrors` and the dictionary on `RuntimeInitializeLoadType.SubsystemRegistration`.
  - `Unregister` removes only when the registered instance is the one passed.
  - `TryGet` treats a destroyed Unity object as missing.
  - Codemod the 35 `SubSystems.Get<T>()?.` sites to `TryGet`.
  - Test: EditMode covering duplicate unregister, destroyed instance and the quitting flag.
- **P2.4 — Map Editor undo truth.**
  - Commands record whether `Apply` changed anything. `Revert` does nothing when it didn't.
  - `MoveTileCommand` checks the destination before clearing the source (`MapEditorCommands.cs:241-249`), and `RpcMoveObject` reports failure (`MapEditorSubSystem.cs:1615`).
  - `ClearTileCommand.Revert` / `ClearItemCommand.Revert` re-place only what they removed (`:154-161`, `:194-195`).
  - Test: EditMode command tests (MapEditor tests already exist).
- **P2.5 — Blast crew damage once per blast.**
  - `ApplyCrewBlastBrute` (`StructuralDamageSubSystem.cs:156-190`) runs once over the final force map, like `ApplyCrewBlastPush`, against a body registry (P6.4 generalizes it; a `HumanHealthController` static set registered on server start is enough here).
  - This fixes the double damage on a tile revisited with more force.
  - Test: EditMode on `BlastResolutionService` with a revisited tile.
- **P2.6 — Examine catalog in the umbrella.** Register Examine in `UiCatalogRebuildAll.TryRebuildAll`; delete `SS3D/Examine/Rebuild Examine Asset Catalog`. Ships with G5.
- **P2.7 — Storage panel during map editing.** `StoragePanelHost` subscribes to map-editor visibility the way the HUD does. Phase 7 replaces both with one arbiter (P7.2).
- **P2.8 — No acting while getting up.** Add `IsGettingUp` to `Hand.CanUseHand` (`Hand.cs:58-66`) as the stop-gap; P4.1 replaces the five "can act" definitions with one. Combat §5 makes the knockdown window the victim's vulnerable time.
- **P2.9 — Small crashes and waste.**
  - `HumanInventory.CmdTransferItem`'s error branch dereferences a null container (`:538-540`).
  - `AudioSourceOcclusion` (`:68-80`): cache a missing `AudioListener` and retry on a short interval.
  - Throw mode (`.Throw.cs:260`, `:286`, `:445`): `RaycastNonAlloc` / `SphereCastNonAlloc` with cached buffers; a cached layer mask.
  - `HumanHealthController.cs:641`: drop the `_organs.ToArray()` per modifier per tick.
- **Tests:** `compile,editmode` per PR; `scenarios:Combat` for P2.1 and P2.8; `scenarios:MapEditor` for P2.4; `scenarios:Explosives` for P2.5; `visual` for P2.7.

### Phase 3 — Dead code and bootstrap hygiene

- **P3.1 — Remove `com.unity.entities`.**
  - Delete `Atmospherics/ECS/AtmosWorld.cs` and its create and dispose in `AtmosSubSystem` (`:168`, `:301`).
  - **Before removing the package,** add `com.unity.burst`, `com.unity.collections` and `com.unity.mathematics` as direct dependencies in `Packages/manifest.json` if they are only transitive today. Atmos jobs need all three.
  - Rename the folder `ECS` → `Jobs` and the asmdef to `SS3D.Atmospherics.Jobs`. References are by GUID, so the rename is safe; check for `Type.GetType` strings and `[SerializeReference]` data naming the old assembly first.
  - Test: `compile,editmode`, `scenarios:Atmos`, `mppm:breach-budget` (the atmos cost baseline).
- **P3.2 — Delete `HumanoidPredictedMovement`** (Decision 11). It is disabled on `Human.prefab` (`m_Enabled: 0`) and has drifted from `HumanoidLivingController`.
  - Remove its component from Human (and TestHuman, if present) through a recipe, then delete the script.
  - Grep for references in tests, the movement docs and `entities.md`, and drop them.
  - Test: `HumanPrefabIntegrityTests`, `scenarios:Environment` (movement and float), `mppm:late-join`.
- **P3.3 — Base `SS3D.UI` assembly.**
  - Delete `LabelButton`, `ToggleLabelButton`, `GenericTabView`, `GenericTabControllerView`, `MouseButtonType` and `TestHelpers.PressButton*`; check for scene and prefab references first.
  - Keep `LocalizeFont` until the condemned `PlayerCanvas.prefab` is purged with its uGUI, then delete the assembly.
  - Remove the unused `SS3D.Systems` → `SS3D.UI` reference now.
- **P3.4 — Bootstrap.**
  - Move the seven `AfterSceneLoad` self-bootstraps into `SystemsBootstrap.EnsureProcessWideServices`: `BugReportSubSystem`, `StructuralDamageSubSystem`, `AirLockProximityService`, `FloorDecalView`, `AreaFloorStripeView`, `LocalSpeechDebugTrigger` and `ScreenEffectsDebugMenuView`. Debug-only ones stay behind the development-build check.
  - Replace `TileSubSystem.cs:291`'s `FindFirstObjectByType<FloorDecalView>()` with a registry lookup.
  - Load `NetworkSessionSubSystem` by type, not by name string: `SS3D.Systems` already references `SS3D.Networking`. `CommandLineArgsSubSystem` stays a string, because Systems does not reference `SS3D.CommandLine`.
  - Update the [TECH_DEBT §1.7](TECH_DEBT.md#17-legacy-scene-based-subsystem-registration-coexists-with-three-ad-hoc-bootstrap-styles) residual note.
- **P3.5 — Small dead code.**
  - `HumanHealthController.ApplyKnockback`: its only caller passes 0.
  - The stale "kept for older callers" comment on `ConstructionHologramManager` (gone with P1.7).
  - Stale TODOs that P1–P3 resolve, for example `ChangeRoundStateMessage` "make based on permissions" (it already is).
- **Tests:** `compile,editmode`; `lifecycle` for P3.4 (bootstrap order is a session contract); one `mppm:late-join`.

### Phase 4 — Gameplay ownership contracts

The biggest correctness win and the riskiest phase: it touches Human's composition and the animation pipeline. Start only after the [humanoid animation pass](2026-11_humanoid-animation-pass.md) ships, so the two efforts don't rewrite the same files at once. Write characterization tests first: `InteractionAnimationScenarios`, `ThrowScenarios`, the melee timing tests in `scenarios:Combat`, and the container tests from #208.

- **P4.1 — `HandActionScheduler` (server, one per body).**
  - One pending hand action at a time (animation-pass Decision 15) and one `CanAct(hand)` rule, replacing:
    - `Hand.CanUseHand`
    - `HumanoidBodyStateMachine.CanPerformAction`
    - the `CombatDummyBootstrap` copy
    - the melee and throw timers' own checks
  - The rule covers body presentation, `IsGettingUp`, stun, an empty or occupied hand, and an already pending action.
  - Melee connect, throw release and interaction contact all schedule through it. Each re-checks at its frame and cancels per combat §2.
  - Wired onto Human by a recipe (Decision 5). It replaces the `InteractionController.Animation.cs` partial and the timer code in `CombatInteractionNetwork`.
  - Test: EditMode on the scheduler as a pure state machine (fake clock); the existing scenarios.
- **P4.2 — One upper-body activity model.**
  - A replicated `UpperBodyActivity` (none / swing / aim / throw / interact / kneel) written only by the server side of P4.1 and by aiming. It replaces the orchestrator's private timers (`AnimationOrchestrator.cs:476-558`).
  - `HumanoidIkController` reads the model instead of Animator state hashes. Its hard-coded swing list (`:17-22`) goes; the swing table is the only list.
  - `Ragdoll` stops toggling the orchestrator and state machine directly (`:476-507`, `:731`, `:790-793`, `:874`) and goes through `BodyPresentationState`.
  - Remove `BodyState.Ragdoll`, which duplicates `Presentation`. One shared facing helper replaces the copies in `.Throw.cs:175` and `.Animation.cs:88`.
  - Combat one-shots come only from the server, completing P1.6.
  - Update `entities.md` and TECH_DEBT §6 (1.2) to say who owns what.
- **P4.3 — Interactions declare their animation.** Each interaction exposes an `InteractionAnimationHint` (clip, contact frame, hand, kneel) instead of `InteractionAnimation.cs:208-231` matching `GetGenericName()` strings and `HumanInventory` hard-coding `"Drop"` (`:348`, `:410`). A test enumerates every interaction type and checks it declares a hint or `None` explicitly.
- **P4.4 — Item holder.**
  - A replicated holder on `Item` (container NetworkObject + container index, or none), written only by the server's container add and remove.
  - On every peer, one `ItemPlacementApplier` applies the holder: parenting, freeze, NetworkTransform toggles, visibility of the item and of nested containers.
  - `AttachedContainer` (`:338-432`), `ContainerItemDisplay`, `DisposalSubSystem` and `DisposalOutlet` stop writing parenting or visibility themselves.
  - `Item.LocalHolder` (the #208 guard) goes away, because out-of-order arrival can't disagree with one replicated value.
  - One `ServerReleaseToWorld(item, position, velocity, reason)` replaces `Hand.PlaceHeldItemOutOfHand`, `HumanInventory.ServerPlaceItemInWorld` and `Hand.Dump`, including TileMap tracking, which `Dump` skips today.
  - Bump `ProtocolVersion.Current`.
  - Test: EditMode on the applier given out-of-order add/remove sequences (the #208 cases); `mppm:late-join` (holder state for late joiners).
- **P4.5 — Components instead of partials.**
  - `ThrowController` out of `CombatInteractionNetwork.Throw.cs` (input, overlay feed, server release through P4.1).
  - Split `Ragdoll` (1,224 lines with partials) along presentation, wind and get-up.
  - Each new component is added by a recipe; update G3's grandfather entries downward.
  - Remove the "so Human.prefab is untouched" rationale from the animation-pass doc's live sections when it is archived.
- **Tests:** `compile,editmode`, `scenarios:Combat`, `scenarios:Explosives`, `scenarios:Medical` (CPR kneel), `mppm:late-join`, `visual` for any pose change, owner hand check of throw, pickup and locker.
- **Done when:** each of hand action, upper-body activity and item holder has one writer; G3 shows `InteractionController`, `CombatInteractionNetwork` and `Ragdoll` below their July sizes.

### Phase 5 — Map Editor and tile authority

Remote editing is the sandbox's core activity (sandbox-server §5), and today it rests on the client's partial map.

- **P5.1 — Tile query contract.**
  - An `ITileQuery` façade in Tile returns `Known(value)` or `Unknown` for occupancy, plenum and support. On the server, `Unknown` is possible only before `TileMapLoaded`.
  - The five plenum checks move onto it: `TileMap.cs:377`, `ConstructionHologramManager.cs:1112` and `MapEditorCommandFactory.cs:78`, `:91`, `:107`. So does `HumanoidSpaceSupport.GetSupportAt`.
  - This turns [TECH_DEBT §1.17](TECH_DEBT.md#117-incomplete-tile-knowledge-treated-as-confirmed-open-space) from a convention into a type.
- **P5.2 — Server-decided validity.**
  - The client preview shows `Unknown` (a neutral hologram) where its mirror lacks the chunk, instead of "invalid".
  - The client sends every cell. The server validates and answers per cell in the command acknowledgement, and the preview updates from the answer.
  - Delete targets are resolved on the server from the selection volume, replacing `CollectScopedClear` (`:923-1005`) on the client. `RefreshDeleteCursorHologram` (`:1146-1185`) becomes a preview of that same resolver, with typed markers instead of `"floor-decal"` / `"spawn-point"` strings.
- **P5.3 — Observer anchoring.** While a client has the editor open, its FishNet observer position follows the editor camera, not the body, so the mirror fills where the editor looks. Check the cost with `mppm:breach-budget`-style logging.
- **P5.4 — Picking items on pure clients.** Pick by `Item` (always present on item prefabs), and let the server map it to its `PlacedItemObject`. This is preferred over baking `PlacedItemObject` into every item prefab.
- **P5.5 — Spawn points for remote admins.** Replicate spawn-point markers to clients with admin rights only; sandbox §5 keeps spawn tools from players.
- **P5.6 — Delta sync.** Floor decals (`TileSubSystem.SyncFloorDecalsToClients`, `:264-285`) and area visuals (`AreaSubSystem.NotifyAreaVisualsChanged`, `:1172-1226`) send changed chunks, batched per command group in `MapEditorCommandContext`. A late joiner gets one full snapshot. Measure before and after with the network stats the atmos budget run already logs ([TECH_DEBT §1.18](TECH_DEBT.md#118-structural-impact-traffic-unmeasured)).
- **P5.7 — `MapEditorView` events.** Split `StateChanged` into toast, undo, palette, popover and selection events, so a toast no longer rebuilds the palette grid. Then remove the hint-bar workaround (`MapEditorSubSystem.cs:146`) and the three Pitfalls in [map-editor-creative-hooks.md](systems/map-editor-creative-hooks.md) it causes. Share one default-layer-visibility constant (`MapEditorViewModel.cs:101`, `TileLayerVisibilityService.cs:45`, `:85`).
- **P5.8 — `TileMap` stops calling upward.**
  - `TileMap` raises `Cleared`, `Loaded` and `ItemTracked` / `ItemUntracked` events and takes the build-check occupancy query as an injected delegate. `TileSubSystem` handles them, replacing the direct calls into Area, tile markers, `EntitySubSystem` and FishNet observers (`TileMap.cs:522-536`, `:826-859`, `:1040-1052`).
  - `ClearItemObject` picks the nearest match, not the last one within 1 m (`:766-786`).
  - `ClearUntrackedItems` uses the item registry (P6.4) and `ServerManager.Despawn`, not `DestroyImmediate` on networked objects (`:1195-1216`).
- **P5.9 — Splits.**
  - `MapEditorSubSystem` → `MapEditorClientController` (lifecycle, hotkeys, tools, palette) + `MapEditorServer` (persistence, validation and command RPCs; moves go through `RpcExecuteCommands`). Suspension is already handled by P2.2.
  - `ConstructionHologramManager` stops being a `NetworkActor` and depends on `IMapEditorHost` instead of 16 members of the concrete subsystem.
  - `AreaSubSystem` → area model + `AreaLighting` (`:959-1155`) + `AreaClientSync` (`:1157-1360`).
- **Tests:**
  - `compile,editmode` (command, resolver and query tests).
  - `scenarios:MapEditor`.
  - A new `mppm:map-editor` scenario. Per testing rule 4, its header names the contract: "a remote editor places and deletes outside its body's AOI, and the server decides validity". A host scenario can't see a partial mirror.
  - `sandbox` once at the end.

### Phase 6 — Simulation and server infrastructure

- **P6.1 — `SimulationClock`.**
  - One clock in Core, driven by FishNet `TimeManager.OnTick`, with fixed-rate channels that keep the remainder. It replaces:
    - the atmos `Update` accumulator (`AtmosSubSystem.cs:306-317`)
    - electricity's `FixedUpdateEvent` with `_timeElapsed = 0` (`ElectricitySubSystem.cs:234-253`)
    - per-body health timers (`HumanHealthController.cs:653-666`)
    - area lighting ticks
  - Clients run a local visual channel, so `ElectricitySubSystem.RpcInvokeOnTick` (`:685-688`, an empty ObserversRpc at 5 Hz) is deleted.
  - `WorldSimSuspension` (P2.2) pauses channels, not systems.
  - Test: EditMode on channel rates and remainder carry; `scenarios:Atmos`; `mppm:breach-budget` before and after.
- **P6.2 — Hot-path review of every channel** ([TECH_DEBT §2](TECH_DEBT.md#2-hot-path-gc--performance-debt-tracked-partially-paid-down)): no per-tick allocation; area and device tile resolution cached per electricity tick.
- **P6.3 — Persistence contributors own their payloads.** `IPersistenceContributor.DeserializePayload(string)` replaces the switch in `PersistenceSubSystem.cs:477-488`. Persistence then knows only the interface, which removes one Persistence↔Tile edge. No schema change.
- **P6.4 — Registries instead of scene scans.**
  - A small `SceneRegistry<T>` in Core: register on enable / server start, unregister on disable. It covers `Item`, `ContainerViewer`, `HumanHealthController`, `SolarTrackingBeacon`, `LightPower` and the decal and area views.
  - It replaces the scans in [TECH_DEBT §1.12](TECH_DEBT.md#112-scene-wide-findobjectsbytype-calls-in-gameplay-code-not-just-editor-tooling): `TileMap.ClearUntrackedItems`, `Locker.CloseStorageUIs`, `TileLayerVisibilityService.cs:151`, `MapEditorSettings.cs:68`, `BlastVfxPresenter.cs:62`, `SolarTrackingBeacon.cs:78`, `LightPower.cs:334`, `RoundObservability.cs:137-141`, `AutomationSubSystem` (`:600`, `:1105`, `:1183`).
  - Remove the `FindObjectOfType` fallback from `SubSystems.Get` once G1–G4 and P2.3 have run for a while with no fallback hits. Log each hit as a warning first.
- **P6.5 — Split `RoundSubSystem`.** Its 8 partial files (1,685 lines) become:
  - `ServerSettingsService` (admin RPCs, settings persistence)
  - `RoundObservability` plus a `BugReportIntake` moved into BugReports (`RoundObservability.cs:229-279`), which removes the Rounds↔BugReports cycle
  - `MapEditorStopFlow`

  `RoundSubSystem` keeps the round state machine.
- **P6.6 — Client session service.** Move the lobby's job-preference, appearance and OOC-history handshakes, and its 1 Hz `CmdReadServerSettings` poll, out of `LobbyUiSubSystem.RefreshSession` (`:272-370`, 5 Hz all session) into a Systems-side `ClientSessionSubSystem`. It sends them once on connect, and the server pushes settings changes. The lobby UI subscribes. This unblocks P7.4.
- **Tests:** `compile,editmode`, `scenarios:Atmos`, `scenarios:Environment`, `lifecycle` for P6.5 and P6.6, `mppm:reconnect` for P6.6 (handshake on reconnect), `sandbox` once.

### Phase 7 — UI consolidation

Follows the order the [UI shell consolidation](2026-07_ui-shell-consolidation.md#explicit-non-goals-deferred-to-later-phases) set: Main HUD next, Machine UI last. No condemned view is migrated as a bridge (AGENTS.md); condemned surfaces are replaced per their design doc or left alone. Every item here runs `visual`.

- **P7.1 — `SS3D.UI.Widgets` assembly.** Move `InventorySlot` out of `UI/MachineInterface/Components`. Add one window-drag manipulator, replacing `StoragePanelView.cs:180-252`, `MachineWindow.cs:71-130` and `PanelDragManipulator`, and one item press-to-drag manipulator with a single threshold constant (today 5 px and 6 px). MainHud, StoragePanel and Examine then drop their reference to the MI assembly.
- **P7.2 — `HudVisibilityArbiter`.** One owner of "which HUD surfaces show" (menu, machine UI, map editor, ghost), replacing the copies in `MainHudSubSystem.cs:757-892` and `StoragePanelHost.cs:204-306`. P2.7's stop-gap goes. Main HUD fully hiding under machine UI stays as an accepted deviation ([TECH_DEBT §5](TECH_DEBT.md#5-accepted-fork-deviations-not-debt--do-not-fix-without-a-design-change)).
- **P7.3 — Split `MainHudSubSystem`** (1,627 lines): `HudLocalPlayerBinder` (also feeds `StoragePanel.BindContainerViewer`), `ZoneReticlePresenter` on `ICombatReadout` (P2.1), `AlertStackPresenter`, `HudInventoryActions`, and a thin host.
- **P7.4 — Split the lobby.**
  - `LobbyShellView` (2,022 + 542 lines) → one `ILobbyTab` per tab: Server Info, Jobs, Chat, Players, Settings.
  - A standalone `PlayerSettingsPanel` mounted by both the lobby and the main menu, replacing the "Settings screen placeholder" in `MainMenu.uxml:43`. The panel's behaviour comes from [player settings](2026-10_player-settings.md).
  - The Players search filters rows instead of rebuilding the tab per keystroke.
  - `LobbyUiSubSystem` uses P6.6 events instead of polling.
- **P7.5 — Split `MainMenuSubSystem`** (1,013 lines): `MainMenuConnectionFlow` (state machine, no UI, EditMode-testable), `MainMenuAmbience` (music, video, wallpaper), and the view host.
- **P7.6 — Catalogs onto `UiAssetCatalogBase`,** in order: MainHud, StoragePanel, Lobby, Examine, MachineUi. Each builder moves onto `UiCatalogBuilderKit` and the umbrella. G5 keeps it that way.
- **P7.7 — Hosts onto UiShell layers.**
  - `FpsCounterView`, `AlertStackDebugMenuView` and the F1 `DebugMenuView` → `UiLayer.Debug`.
  - The Map Editor → its own layer.
  - Main HUD and Storage Panel → `UiLayer.Hud`, after P7.2 and P7.3.
  - Machine UI last.
  - Retire the hand-picked sorting orders (−10 / 2000 / 3000). Update [TECH_DEBT §1.13](TECH_DEBT.md#113-debug-tools-centralized-ui-host-consolidation-remains) and `ui-shell.md`.
- **P7.8 — Gameplay out of the MI assembly.** The 11 device controllers (~2.8k lines, e.g. `FilterInterfaceController` calling `Filter.Toggle()`) move to their domain folders in Systems behind a narrow interface the MI binders call. Content prefabs then stop depending on a UI assembly. Script GUIDs move with the `.meta` files, so prefab references survive. Q8 decides `UiBinderBase`: adopt it for the MI binders as this move rewrites them, or delete it.
- **P7.9 — Polling and inline styles.** `RespawnPrompt` → USS and event-driven text (`:70-84`, `:96-127`). `StoragePanelHost.Update` (`:113-118`) and `RoundEndUiSubSystem.Update` subscribe instead of polling. Hand-rolled DOTween panel sequences (`MainHudView.cs:259-332`, `MachineInterfaceHost.cs:398-497`) move onto `PanelAnimator`.
- **Tests:** `compile,editmode,visual` per PR; `lifecycle` for P7.4 and P7.5; `scenarios` for the inventory flows that P7.1 and P7.2 touch.

### Phase 8 — Assembly split by edge

Mechanical once Phases 5–7 have cut the reverse references. Each new assembly references `SS3D.Systems`; nothing in Systems references it back. G4 drops with each step.

- **P8.1 — `SS3D.Automation`.**
  - `Systems/Testing/AutomationSubSystem.cs` (1,328 lines) and its helpers move into an asmdef compiled only with `SS3D_AUTOMATION` defined. The validation harness and CI build scripts (`Tools/build_client_and_server.sh`, the smoke and nightly workflows) set the define; release builds don't (Q7).
  - `SystemsBootstrap.cs:88` stops creating it, and the automation assembly bootstraps itself under the define.
  - Check that `-testscript=` paths in `multiplayer-smoke-test.yml` and the nightly still find it.
- **P8.2 — `SS3D.MapEditor`.**
  - `Tile/MapEditor/`, its UI, and the Map Editor half of `TileMapCreator/` (holograms, layer visibility). Map import goes too if it has no other users.
  - Inbound uses from Systems (round map-stop, bug-report context, ghost editing) go through an `IMapEditorState` interface in Tile.
- **P8.3 — `SS3D.Console`.** `IngameConsoleSystem/` (fan-out 16, almost no fan-in). Commands stay where they are, as a leaf.
- **P8.4 — `SS3D.BugReports`.** After P6.5 removes the Rounds dependency.
- **P8.5 — UI views out of Systems.** The ~26 UI Toolkit view files in Systems move into `SS3D.UI.*` feature assemblies: radial, armed and throw overlays, comms bubbles, `BugReportView`, `RespawnPrompt`. Systems-side presenters expose plain state or interfaces. Feature views parked in `UI.Shell` only because Systems could reference it (BugReport, RoundEnd, MainMenu) move out too, so the shell holds only shell code.
- **P8.6 — Tile as a base layer (prep only).**
  - `Tile/Connections/` electricity, pipe and disposal rules move to their domains behind a connection-rule registry Tile owns.
  - `TileSubSystem` stops importing Atmospherics and Persistence: gas markers become a Tile-owned marker type, and persistence goes through P6.3.
  - The domain split itself is out of scope (Decision 4); this only stops the cycle growing.
- **Pitfalls for every move:**
  - Scripts keep their GUID through `.meta` files, so prefab and scene references survive, but `[SerializeReference]` data stores assembly-qualified type names. Add `[MovedFrom(true, sourceAssembly: "SS3D.Systems")]` to any moved type used that way, or the data is silently dropped.
  - `Type.GetType("…, SS3D.Systems")` strings break; grep for `SS3D.Systems"` first.
  - FishNet weaves every assembly that references `FishNet.Runtime`, so check that RPCs in moved NetworkBehaviours still register (the `compile` step shows weaver errors).
  - Test asmdefs need their references updated.
  - Record each new asmdef in `core-subsystems.md`.
- **Tests:** `compile,editmode` per step; one full `scenarios` run and `mppm:late-join` at the end of the phase; a CI smoke build to prove `SS3D_AUTOMATION` reaches the harness builds.

### Phase 9 — Lock-in

- **P9.1 — An analyzer gate** ([TECH_DEBT §1.11](TECH_DEBT.md#111-stylecop-disabled-repo-wide-with-no-replacement-gate)), with the rule set decided in Q6. Proposed minimum, as Roslyn analyzers in the empty `Analysers` assembly or a DLL, run in the EditMode CI job:
  - `.editorconfig` naming
  - no `FindObject*` in non-Editor `SS3D.Systems` code
  - no `ServerRpc(RequireOwnership = false)` without an `[AuthorityCheck(...)]` attribute; this turns G1's allowlist into an attribute next to the code
  - no `SubSystems.Get<T>()?.`
- **P9.2 — Docs.**
  - Every map touched by this effort carries a fresh `Verified` stamp.
  - TECH_DEBT §1.19–§1.30 and the touched parts of §1.4, §1.7, §1.9, §1.12 and §1.13 move to §6 Resolved, or are rewritten as what's left.
  - INDEX and the coverage table are updated.
- **P9.3 — Archive this effort,** harvesting live items first ([SKILL.md](../SKILL.md#archive-documentsarchive)).

## Out of scope

- **TECH_DEBT items owned by other efforts:** §1.1 Human.prefab beyond the recipes Phase 4 adds ([entity prefab recipes](systems/entities-prefab-recipes.md)); §1.5 the catalog codegen pipeline; §1.8 condemned UI replacement; §1.10 CI multiplayer health ([multiplayer testing](2026-07_multiplayer-testing-self-hosted-ci.md)); §1.14 asset taxonomy ([asset file structure](2026-07_asset-file-structure-taxonomy.md)); §1.15 Addressables ([Addressables migration](2026-07_addressables-expansion-migration.md)).
- **A domain split of the core simulation assembly** (Decision 4).
- **Client-side prediction, rollback or lag compensation** beyond the validation margin in P1.9 (networking §10 leaves netcode to implementation; a real prediction effort would be its own doc).
- **Console permissions** (Decision 12).
- **Gameplay changes.** No rule, number or player-facing behaviour changes. The exceptions are the hint in P1.9 step 3 and the bugs fixed in Phases 1–2, each of which brings code back in line with a design doc.

## Open questions for the owner

Questions 1–4 were settled as Decisions 9–12.

5. **How much lag slack the server's re-checks allow (P1.1, P1.9).** The server re-checks clicks and shots against its own copy of the player, which trails the client by the network delay. Today the checks are exact: a drop needs the server's line-of-sight ray to land within 10 cm of the clicked point, so a moving player's drop can be refused (#192). Recommended: ship P1.9's diagnostics first, which log how far off the server's copy really is. Then set the slack (LOS distance, range margin, aim cone for guns) from what playtests measure, instead of guessing now. Also, what should the "can't reach that" hint say?
6. **Analyzer gate (P9.1):** which rules, and does it fail CI or only warn at first?
7. **Automation in release builds (P8.1):** strip test automation from release builds (recommended), or keep `-testscript=` working on shipped builds for remote debugging?
8. **`UiBinderBase`:** adopt it for the Machine UI binders during P7.8, or delete it?
