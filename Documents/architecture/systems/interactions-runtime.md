> Code paths: Assets/Scripts/SS3D/Systems/Interactions/
> Entry points: InteractionController, InteractionDiscovery, InteractionDispatch, RadialInteractionSubSystem, ArmedInteractionSubSystem
> Status: shipped
> Verified: e2f86cf20 — 2026-11-05

# Interactions (runtime)

## Overview

Client-side interaction routing: discovers available interactions from the current selection and player state, presents the three-tier radial menu, arms targeted interactions, and dispatches `InteractionIdentifier`-based requests to the server. Bridges [selection](selection.md) hover targets with the shared [interactions-framework](interactions-framework.md). Owns Help/Harm intent (`IIntentProvider`). Harm-primary combat RPCs live on sibling [combat](combat.md) `CombatInteractionNetwork` — the controller only routes Harm primary to that behaviour.

**Intent gate:** unrestricted verbs (Drop, Open, MI, …) are **Help-default** via `InteractionPipeline.MatchesIntent`. Harm is combat-exclusive (`IIntentRestrictedInteraction`); primary never falls through to world verbs when a swing cannot start. Drop hotkey also requires Help.

Radial menu and armed overlay attach into `UiShellSubSystem`'s shared overlay layer (see [ui-shell](ui-shell.md)) instead of owning a private `UIDocument` — `RadialInteractionSubSystem`/`ArmedInteractionSubSystem` no longer require `[RequireComponent(typeof(UIDocument))]`; `RadialInteractionMenuView`'s open/close tween runs through the shared `PanelAnimator`.

### Player flow

1. `SelectionSubSystem` resolves hovered `Selectable`.
2. `InteractionController` builds viable list via `InteractionPipeline` + active hand/tool source.
3. Primary click: **Shift+Click** on another character → `TryRunSearchOnCharacterSelection` (Search petal path). Else Harm → `CombatInteractionNetwork.TryRunRangedFirePrimary` (held firearm) else `TryRunMeleeSwingPrimary` / then **return** (no Drop/Open fallback); Help → highest-priority unrestricted / Help-tagged interaction.
4. Targeted radial choices arm the cursor via `TryRouteRadialInteraction`; second click resolves the matching `InteractionEntry` by `GetGenericName()` and dispatches RPC.
5. Server re-validates gates (intent, stamina, ownership, permissions) then `InteractionSource.Interact`.
6. Observers run client FX; rejections use `TargetRejectInteraction` to roll back optimistic UI.

### Outline feedback

| Color | Meaning |
|-------|---------|
| Green | Target-bound interaction viable now (in range, intent, gates) |
| Yellow | Target-bound interaction discovered but not currently viable (e.g. out of range) |
| Blue (pending) | Instant interaction awaiting server confirm |
| Hidden | No hover, entity target, no source, or only source-only entries (e.g. Drop while holding) |

Entities (`Human`, ghosts) are excluded from hover outlines; medical targeting will use dedicated UI.

Hover outlines ignore source-only discoveries such as `Drop` (see Pitfalls).

Discover / `HasPoint` contract: [interactions-framework](interactions-framework.md) Overview + [2026-07_interaction-discover-contract](../../archive/architecture/2026-07_interaction-discover-contract.md).

### Cancellation

- **Cancel Interaction** (`<Keyboard>/backspace`) — `CmdCancelInteraction` for in-progress delayed interactions.
- **Intent toggle** is **F**, not Cancel — see [2026-07_default-input-scheme.md](../../archive/architecture/2026-07_default-input-scheme.md).
- Movement — `DelayedInteraction` auto-cancel via `CharacterMoveCheck` (melee uses entity root).

## Start here

- `Assets/Scripts/SS3D/Systems/Interactions/InteractionController.cs` — thin router: primary-click policy, radial/armed, intent SyncVar, world/inventory/examine RPCs, delayed tracking; editor-only `RunInteractionForTesting` sends a named verb through `CmdRunInteraction` for scenarios
- `Assets/Scripts/SS3D/Systems/Interactions/InteractionDiscovery.cs` / `InteractionDispatch.cs` — shared discover/resolve helpers (client + server revalidation)
- `Assets/Scripts/SS3D/Systems/Interactions/InteractionOutlineDriver.cs` / `DelayedInteractionTracker.cs` — outline LateUpdate + active delayed refs
- `Assets/Scripts/SS3D/Systems/Interactions/RadialInteractionSubSystem.cs` — three-tier radial menu subsystem
- `Assets/Scripts/SS3D/Systems/Interactions/UI/RadialInteractionMenuView.cs` — radial menu UI (UI Toolkit)
- `Assets/Scripts/SS3D/Systems/Interactions/UI/RadialInteractionPetal.cs` — dynamic petal elements
- `Assets/Scripts/SS3D/Systems/Interactions/UI/ArmedInteractionOverlayView.cs` — reticle, chip, and target highlight overlay
- `Assets/Scripts/SS3D/Systems/Interactions/ArmedInteractionSubSystem.cs` — armed-mode interaction overlay
- `Assets/Scripts/SS3D/Systems/Interactions/InteractionOutlineView.cs` — hover and pending interaction outlines (exclude from pick via [selection](selection.md) rendering layers; clear on pickup)
- `Assets/Scripts/SS3D/Systems/Interactions/ArmedTargetEvaluation.cs` — armed target filtering
- `Assets/Art/Graphics/InteractionOutline.shader` — inverted-hull outline material for availability feedback; the hull is extruded in clip space by a fixed pixel width (`_OutlineWidth`, 2 px from `InteractionOutlineView`), so the line stays thin at any distance or object scale

## Extension points

- New world interactions: implement in domain system via framework contracts; they appear automatically when source/target resolution succeeds.
- Shared discover/resolve logic: extend `InteractionDiscovery` / `InteractionDispatch` — do not grow `InteractionController` for list building or RPC match helpers.
- Radial menu tiers: implement `IInteractionTierProvider` on sources/targets.
- Armed mode: extend `ArmedTargetEvaluation` for new armed interaction categories.
- Character paperdoll open: `SearchInteraction` via `HandSearchExtension` (Discover/`CmdRunInteraction`); Shift+Click is only a shortcut — do not re-add overlay click bypasses.
- UI-started delayed takes (character examine): `InteractionController.RequestTakeFromCharacter` — do not force paperdoll slots through Discover/`CmdRunInteraction`.
- Harm primary combat: `CombatInteractionNetwork` on Human — see [combat](combat.md).
- Reach through walls: `InteractionExtensions.RangeCheck` fails when the hand-to-target segment crosses an `IReachBlocker` that isn't the target. `PlacedTileObject` blocks for `Wall` (walls, windows, girders) and `Door` generic types. Make a new solid structure block by giving it one of those types or its own `IReachBlocker`; don't add per-interaction wall raycasts. Wall mounts implement `IReachSurface` through `PlacedTileObject`: their tile is the wall's, so reach is measured to the wall face on the side they face.

## Pitfalls

- **Armed CPR labels drift at scaled resolutions:** raw mouse pixels are not UI Toolkit units. Armed/radial surfaces fill the overlay layer and convert via `InputInterface.ScreenToPanel` then into surface-local coordinates. Position chips after layout from their measured size, and keep them inside panel bounds. The terminal font lacks em-dash/arrow glyphs; armed captions use an ASCII separator.

- **Outline MissingReferenceException after destroying a wall/object:** outline MeshRenderers are children of source meshes; destruction can wipe them while `InteractionOutlineView` on the `Selectable` still lives. `SetState` / `SetRenderersEnabled` must prune Unity-null `Renderer` refs — do not assume `_entries` stay valid for the view's lifetime. Do not use `?.` on `InteractionOutlineView` — it ignores Unity fake-null.
- **Player builds silently omit hover outlines:** `InteractionOutlineView` uses `Shader.Find("Custom/InteractionOutline")` with no material/prefab reference. Editor finds it; player strips it (`Custom/InteractionOutline` absent from `SS3D_Data`). Symptom: clicks/MI still work (Selection pick shader is wired on the URP feature), but no green/yellow/blue hull. Keep the shader in **Always Included Shaders** (`GraphicsSettings`); warn if Find returns null.
- **Spawn NRE in `OnAwake` / `SubscribeToInput`:** if `CameraSubSystem.PlayerCamera` is null (hub before Game camera — see [chat-audio-screens](chat-audio-screens.md)), wiring `_controls` after the camera line leaves SubscribeToInput cascading. Resolve inputs first; tolerate a late camera.
- **`ArmedInteractionSubSystem` must not `Get<SelectionSubSystem>` in Awake.** Selection is a sibling on `NetworkSystemsHub`; Awake order can leave it unregistered, and FishNet also briefly enables scene copies before the hub exists. Lazy `TryGet` + null-safe enable/disable.
- **Outline on every hover while holding an item:** `Item` discovers Drop via `InteractionEntry.SourceOnly`. Outline LateUpdate must use `TryEvaluateOutlineInteractability` (no source discovery) or `FilterForOutline` — never treat full Discover as hover-available.
- **Outline LateUpdate GC:** do not call full `Discover`/`FilterAndSort` every frame for hover feedback. That path allocates lists, `targets.ToArray()`, and source-only entries (Drop) that outlines discard. Use `TryEvaluateOutlineInteractability` + reused target buffers; `CollectTargetsInto` must use `GetComponents(List)` (not the array overload); mutate `InteractionEvent.Target` / hit fields instead of `WithTarget`; throttle same-selectable re-eval (~10 Hz). Cache `CreateTargetInteractions` instances on hot targets (e.g. airlocks). Marker: `SS3D.Interactions.Outline`. Hit 2026-07-28 (~784 KB / 507 frames on Metastation).
- **Unresolved selection point:** when `TryResolveInteractionPoint` fails, build `InteractionEvent` without a point (`HasPoint = false`) — do not pass `Vector3.zero` into the four-arg ctor.
- **Entity body-part selectables vs NetworkObject root:** Client builds viable lists on the hovered child `Selectable`; `CmdRunInteraction` revalidates on the parent `NetworkObject.gameObject`, so `targetComponentIndex` often mismatches (`SyntheticTargetIndex` -2). Use `InteractionDispatch.TryResolveDispatchedInteraction` (exact id, then generic-name fallback) — do not require limb mesh contact for combat Hits.
- **`F` toggles Help/Harm** via arbitrated `InputSubSystem.ToggleIntent` (was hardcoded `C`, which
  also fired Cancel). Cancel delayed/armed interactions with **Backspace**. Defaults:
  [2026-07_default-input-scheme.md](../../archive/architecture/2026-07_default-input-scheme.md).
- **Harm must not fall through to world verbs:** `HandleRunPrimary` always returns after the melee attempt in Harm — never resume the Help path when recovery blocks the swing. Unrestricted interactions are Help-default in `MatchesIntent`; Drop hotkey also checks Help.
- **Radial shows icons but petal clicks no-op after first close:** `Disappear` unsubscribes `InteractionSelected`/`CloseRequested` (avoids double-fire during hide). The UiShell-backed menu view is reused, so `ShowInteractionsMenu` must call `BindMenuViewHandlers` every open — otherwise the second hold-RMB session looks fine (icons populate) but petals never route. Not Addressables-related.
- **Hover outline and examine tooltip flicker at some angles:** the outline hull used to draw at `Geometry-1` with `ZWrite On`. The selection pick pass reuses scene depth after opaques, so where the expanded hull poked in front of the object's own surface the object failed its own pick, lost hover, hid the outline and regained hover next frame. `InteractionOutline.shader` now draws at `Geometry+1` with `ZWrite Off`; keep it from writing depth.
- **Reach cast only sees the Default layer:** wall and door solids sit on Default; the Walls layer holds only the vision-obstacle child boxes. The cast stops 5 cm short of each end and runs both ways, so an origin inside a wall still blocks. A blocker that is (or contains) the target never blocks, so walls and airlocks stay interactable from their own side. Many targets, airlocks included, aren't `IGameObjectProvider`s, so resolve a target's transform with `GetTargetTransform`, not `GetGameObject()` alone.
- **Wall mounts sit inside the wall's 1 m solid:** a mount shares its wall's tile and its colliders are centred there, so a picked point is 0.4 m inside the wall. Reach goes through `IReachSurface` to the face in the mount's `dir`, the side `BuildChecker` keeps free of walls.
- **New interaction icons:** image assets belong under `Assets/Art/Icons/`; consult the [icon index](../../icon-index.md). The legacy interaction icon builder still targets `Assets/Art/Graphics/UI/Interactions/InteractionIcons/` and must be updated before using it for new images.

## Tests

- `Testing/ui/validate.sh --steps compile,interaction-overlay` checks radial/armed CPR overlay geometry at 1.5 and 0.75 panel scales and near screen edges, with screenshots.
- EditMode: `InteractionPipelineTests`, `InteractionDispatchTests`.
- PlayMode scenarios (`Assets/Scripts/Tests/PlayMode/Scenarios/`): `MedicalScenarios` (Bandage) and `AccessScenarios` (door) dispatch real verbs through `RunInteractionForTesting`, so the server revalidates range, intent and gates; run with `--steps scenarios:<category>`.

## Depends on / Used by

- **Depends on:** [interactions-framework](interactions-framework.md), [selection](selection.md), [player-control](player-control.md), [inputs](inputs.md)
- **Used by:** Nearly all player-facing gameplay actions; [combat](combat.md) Harm swing / intent

## Related docs

- Effort: [2026-07_interaction-controller-decomposition](../../archive/architecture/2026-07_interaction-controller-decomposition.md) — shipped (TECH_DEBT 1.9 interactions)
- Effort: [2026-07_interaction-discover-contract](../../archive/architecture/2026-07_interaction-discover-contract.md)
- Effort: [2026-07_interaction-system-hardening](../../archive/architecture/2026-07_interaction-system-hardening.md)
- Defaults: [2026-07_default-input-scheme.md](../../archive/architecture/2026-07_default-input-scheme.md)
- Plan: [radial_menu_implementation_5a83bdf9.plan.md](../../plans/radial_menu_implementation_5a83bdf9.plan.md)
- Plan: [interaction_system_improvements_9e14ae22.plan.md](../../archive/plans/interaction_system_improvements_9e14ae22.plan.md)
- Design (read-only): [Documents/design/main-hud.md](../../design/main-hud.md)
