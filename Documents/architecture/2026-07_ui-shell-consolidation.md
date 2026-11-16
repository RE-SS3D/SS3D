> Implements: Documents/architecture/2026-07_agent-first-composition.md follow-on (b) — UiShell scaffolding + Phase 1 wedge
> Touches systems: ui-shell, interactions-runtime, inputs
> Status: shipped

**Progress:** Phase 0-1: scaffolding + radial/armed migration.

# UI shell consolidation

Machine Interface, Main HUD, and Storage Panel each independently reinvented an asset-catalog/`Resources.Load`
stack, a hand-rolled DOTween open/close animation, and (radial/armed) a private `UIDocument`. This effort starts
the consolidation `ui-shell.md` names as deferred follow-on (b): shared scaffolding (`SS3D.UI.Shell`) plus a real
`UiShellSubSystem` that owns one document and a fixed set of layers, proven first on the smallest surfaces
(radial interaction menu, armed overlay) before Main HUD, Storage Panel, and Machine Interface migrate in later,
separate efforts.

## What shipped

- `SS3D.UI.Shell` assembly (`Assets/Scripts/SS3D/UI/Shell/`) — new, references only `SS3D.Core` + `DOTween.Modules`
  (deliberately not `SS3D.Systems`, to avoid a circular reference now that `SS3D.Systems` references it back).
- `PanelAnimator` (`Animation/PanelAnimator.cs`) — shared opacity/scale/translateY open/close tween with an
  optional scrim-alpha join, encapsulating the "close must finish its tween before invoking the completion
  callback" invariant in one place (see `machine-interface.md` Pitfalls) instead of per-surface.
- `UiAssetCatalogBase` / `UiCatalogRuntimeLoader` (`Catalog/`) — shared `ScriptableObject` base (panel settings +
  shared tokens) and `Resources.Load` + required-assets check, so a surface catalog only has to declare its own
  fields and delegate the rest. `UiCatalogBuilderKit` (`Assets/Scripts/SS3D/Editor/`) does the same for the
  Editor-side rebuild-menu boilerplate (load-required, load-or-create asset, save/refresh).
- `UiBinderBase` / `UiBinderBase<TViewModel>` / `IUiBinder<TViewModel>` (`Binding/`) — shared query/wire/disconnect
  plumbing for panel binders. Not yet consumed by any Machine Interface binder (that migration is a later,
  separate phase) — this scaffolding is unvalidated by a real binder until then.
- `IUiSurface` — formalizes the `Attach(VisualElement)`/`Detach()` convention already used by radial menu, armed
  overlay, and Main HUD views.
- `UiShellSubSystem` + `UiLayer` — self-bootstraps like `ScreenEffectsSubSystem` (`RuntimeInitializeOnLoadMethod`,
  no Boot/Game scene edit required); owns one `UIDocument` + `UiShellAssetCatalog` (panel settings + the two
  global token stylesheets, reusing the existing `HudOverlayPanelSettings.asset`); builds one child
  `VisualElement` per `UiLayer` (`Hud → Overlay → Diegetic → Modal → Debug`) and applies the shared tokens once at
  the document root instead of per-panel-open.
- Radial interaction menu (`RadialInteractionSubSystem` / `RadialInteractionMenuView`) and armed overlay
  (`ArmedInteractionSubSystem` / `ArmedInteractionOverlayView`) migrated onto `UiShellSubSystem.GetLayer(UiLayer.Overlay)`:
  their private `UIDocument`/`[RequireComponent(typeof(UIDocument))]` and the `EnsureDocumentActive`/`ShutdownDocument`
  enable-disable dance are gone; each view now attaches its own child container into the shared overlay layer once
  and controls its own container's `pickingMode`/`display`, exactly as it controlled its own document root before.
  `RadialInteractionMenuView` also now drives its open/close tween through `PanelAnimator` instead of a hand-written
  `DOTween.Sequence`.

## Agent checklist (for the next surface to adopt UiShellSubSystem)

1. Get the layer: `SubSystems.TryGet(out UiShellSubSystem uiShell)` then `uiShell.TryGetLayer(UiLayer.X, out root)`.
2. Register the shared document once per attaching surface: `InputInterface.RegisterDocument(uiShell.Document)` —
   idempotent, and deliberately never paired with `UnregisterDocument` on that surface's teardown, since the
   document is shared and outlives any one surface (see the comment in `RadialInteractionSubSystem.AttachMenuView`).
3. Build your view/surface's own child `VisualElement` under the layer root (don't manipulate the layer root's
   `pickingMode`/children directly — other surfaces share that same layer root).
4. Implement `IUiSurface` on the view; use `PanelAnimator` for open/close tweens if the surface animates.

## Explicit non-goals (deferred to later phases)

- Machine Interface, Main HUD, and Storage Panel are **not** migrated in this pass — they keep their own
  `*AssetPaths`/`*AssetCatalog`/`UIDocument` stacks. Retargeting them onto `UiAssetCatalogBase`/`UiShellSubSystem`
  is later, separate work (Main HUD next, MI last since it's shipped and most load-bearing).
- No MI binder migrates onto `UiBinderBase<TViewModel>` yet.
- No drag/drop abstraction (Storage Panel / Main HUD pointer-capture drag is untouched).
- No localization/tooltip wiring (still zero UI Toolkit ↔ `LocalizedTextService` call sites).
- **Prefab cleanup**: `RadialInteractionMenu.prefab` and `ArmedInteractionOverlay.prefab` still carry their old
  (now unused) `UIDocument` component and serialized `_document` reference — removing a component from a
  committed prefab needs an Editor session to do safely; flagged here rather than hand-edited as prefab YAML.

## Related docs

- Policy: [2026-07_agent-first-composition.md](2026-07_agent-first-composition.md)
- Map: [systems/ui-shell.md](systems/ui-shell.md), [systems/interactions-runtime.md](systems/interactions-runtime.md), [systems/inputs.md](systems/inputs.md)
- Sibling wedge: [2026-07_mi-path-catalog.md](../archive/architecture/2026-07_mi-path-catalog.md)
