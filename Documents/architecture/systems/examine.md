> Code paths: Assets/Scripts/SS3D/Systems/Examine/, Assets/Scripts/SS3D/UI/Examine/, Assets/Scripts/SS3D/Localization/
> Entry points: ExamineSubSystem, ExamineOverlaySubSystem, ExamineContentResolver, ExamineKeywordFormatter, PresentedIdentity, CharacterExamineHealthBuilder, SearchInteraction, TakeFromCharacterInteraction
> Status: partial
> Verified: 5fac0a4ed — 2026-11-11

# Examine

## Overview

Hover tooltips and shift-hold detailed examine panels, range-gated off the [selection](selection.md) system's current `IExaminable`. A target under the FOV mask counts as no target ([design §2](../../design/examine.md): what you can see, you can examine): `ExamineOverlaySubSystem` asks `VisionSubSystem.IsVisible` about the point under the cursor, on hover change and every 0.1 s, since doors and walking change visibility with no selection event. Supports text and image panel variants. Localization uses a unified Examine string table and `LocalizedTextService`; dynamic content via `IExamineContentProvider` (multi-provider on one GameObject) and character health builders.

Glance examine (hover + Shift-hold) is **not** an `IInteraction` — `ExaminableBase` is read by `ExamineSubSystem` from the current selection. Opening another character's paperdoll **is** the **Search** interaction (`SearchInteraction` / `HandSearchExtension` on hand prefabs; icon `InteractionIcons.Search`).

**Rendering is UI Toolkit** (`ExamineOverlaySubSystem`, `Assets/Scripts/SS3D/UI/Examine/`) — the previous uGUI trio has been deleted, not ported (Phase 0 purge). Surfaces: `GenericExamineHoverView`; `CharacterQuickLookView` / `CharacterExamineWindowView` (character paperdoll).

**Built against the revised design** ([examine.md](../../design/examine.md), effort [2026-11_examine-and-action-feedback](../2026-11_examine-and-action-feedback.md) phases 1–4): keyword colours (`<k=good|warn|danger|dept:id>` markup in strings, `ExamineKeywordFormatter`), two range bands (`ExamineSection.Band`; beyond `ExamineConfig.CloseRange` the panel drops Close lines, the description and the image), a live panel (held examine re-resolves every 0.1 s; the cache serves hover), the bare-item fallback (`ItemFallbackExaminable`: `Item.Name`, size class, stack count, the item's providers), self-examine by pointing at your own model ("You are presenting…"), and Search hiding pockets on conscious targets. Still to build: the examine cue and action messages (phases 5–6), the vitals-cluster readout (no cluster yet), department colours (visual brief), and the effort's Deviations list. Medical scanner (numeric pools) stays out.

**Character examine** (design §7): no new `Human.prefab` component — `Human.asset` is `Type: CHARACTER`; overlay reads sibling `HumanInventory` / `HumanHealthController`. Do **not** reintroduce `CharacterExaminable` ([TECH_DEBT.md](../TECH_DEBT.md) §1.1).

- **Hover** → name tooltip only. Identity is `PresentedIdentity`: a card in the ID or PDA slot (bare or inside the worn PDA crew spawn with), or a card in hand; otherwise a short look at the outfit. Works on self and others.
- **Shift (hold)** → name + presented job (others) + `ExamineData` details + health lines (`CharacterExamineDetail`). Tier 0 (self + others): state + one consolidated appearance sentence (`I` vs `he`). Tier 1 (self only): short feel lines (`I feel dizzy`), then "You are presenting: Name, Job". Quiet when healthy. Health rebuilds each frame while held. Does not open the paperdoll.
- **Shift, no target** → nothing (design §2: nothing under the cursor never means yourself).
- **Search** (radial petal or **Shift+Click** shortcut) → persistent paperdoll on **other** characters only. Same Discover/`CmdRunInteraction` path; client opens UI via `SearchClientInteraction` → `ExamineSubSystem.RequestCharacterWindow`.
- **Click-to-take** — `TakeFromCharacterInteraction` on anyone in reach: 1.5 s dead/unconscious, 3 s open, 4.5 s blind into a hidden slot; a conscious victim gets `TakeFromCharacterWarning` (private feed line; becomes a target-only action line in Phase 5). The window closes out of reach.
- **Hidden slots** — on a conscious target, pockets and suit storage show occupied (`InventorySlot.Unknown`, "?") or empty (`BuildSlots(inventory, revealHidden)`). UI-only: the SyncList still reaches every client ([TECH_DEBT](../TECH_DEBT.md) §1.31). Clothing covering other slots is not modelled. Slot↔`ContainerType`: `CharacterExamineSlotContainerMap`.

**Editor:** **SS3D → Data → Rebuild All UI Catalogs** (the Examine builder runs inside the umbrella). Hand Search wiring: `HandsPrefabSetup.Wire` / `HumanPrefabRecipes`.

**Shock lines (health.md §7):** `CharacterExamineHealthBuilder` reads "pale and shaking" from shock 35 and "down, conscious and in pain" while Downed (keys `examine.health.shock.*` / `examine.health.downed.*`, seeded by `ExamineIdentificationKeySetup.AddExamineFeedbackKeys`).

## Start here

Paths are under `Assets/Scripts/SS3D/Systems/` unless they start with `UI/` (`Assets/Scripts/SS3D/UI/`).

- `Examine/ExamineSubSystem.cs` — hover/detailed/character-window events
- `Examine/ExamineContentResolver.cs` / `ExamineData.cs` / `IExaminable.cs` / `ExaminableBase.cs` / `IExamineContentProvider.cs`
- `Examine/ExamineSection.cs` / `ExamineBand.cs` / `ExamineConfig.cs` / `ExamineDetailText.cs` — range bands and panel body
- `Examine/ExamineKeywordFormatter.cs` — `<k=…>` → colour spans; `ExamineText.cs` — table lookup with formatted English fallback
- `Examine/ItemFallbackExaminable.cs` / `ItemExamineLines.cs` — bare items; `ExamineSubSystem.GetCurrentExaminable` makes the fallback
- `Examine/PresentedIdentity.cs` / `CharacterExamineDetail.cs` — identity resolver, character panel body
- `Examine/CharacterExamineHealthBuilder.cs` / `ExamineHealthKeys.cs` — Tier 0/1 health lines from `HealthSnapshot` + `HealthDebugDetail`
- `UI/Examine/ExamineOverlaySubSystem.cs` — surfaces + take wiring + self fallback
- `UI/Examine/GenericExamineHoverView.cs` / `CharacterExamineWindowView.cs`
- `Examine/CharacterExamineContentBuilder.cs` — slot icons + visible identity
- `Examine/CharacterExamineSlotContainerMap.cs` / `CharacterExamineTargetUtility.cs`
- Providers outside Examine: `Combat/RangedWeaponRoundsReadout.cs` (rounds/charges, synced), `Electricity/BasicPowerConsumer.cs` + `MachinePowerConsumer.cs` (powered), `Furniture/Disposal/DisposalTag.cs`, `Furniture/Nuke.cs` (disk, armed and time left, panel; `ExamineNukeKeys`)
- `Examine/IdentificationCardExaminable.cs` (owner/role lines) / `StructuralIntegrityExaminable.cs` (damaged/cracked lines for walls, windows and tables; added by `StructuralIntegrityPrefabSetup`) — content-provider examples
- `Examine/Editor/` — localization JSON export/import menus; `ExamineIdentificationKeySetup` seeds keys (no menu): `AddExamineFeedbackKeys` writes every tagged and new key from its fallback constant
- `Inventory/Interactions/SearchInteraction.cs` / `HandSearchExtension.cs` — paperdoll open as Help Instant
- `Inventory/Containers/CharacterLootUtility.cs` / `Inventory/Interactions/TakeFromCharacterInteraction.cs`
- UITK examine is `SS3D.UI.Examine` (avoids Systems↔MachineInterface cycle)

## Extension points

- `SimpleExaminable` / `ImageExaminable` / multiple `IExamineContentProvider`s on one GameObject. An item needs no examinable: any component implementing the provider on the item root is read. Mark fine print `ExamineBand.Close`; tag state words `<k=good|warn|danger>`; add the key with its English to `AddExamineFeedbackKeys`.
- Character health: extend `CharacterExamineHealthBuilder` (no Human prefab component).
- `ExamineSubSystem.SetCursorLabelOverride(text)`: while non-null the hover badge shows that text and examines nothing (detailed examine off; empty hides the badge). The Map Editor uses it to name what Edit is about to place; it must pass null when it closes.
- Follow-ups: obscured-slot filtering; restrained loot; StoragePanel foreign-loot; medical scanner (virology).

## Pitfalls

- **Take without Discover:** start via `InteractionController.RequestTakeFromCharacter`, never `CmdRunInteraction` name resolve.
- **Search vs glance:** Shift-hold detail is examine overlay; paperdoll open is Search (Shift+Click runs Search via `InteractionController.TryRunSearchOnCharacterSelection`, not a separate overlay click path).
- **Foreign `CmdTransferItem` rejects** containers not in `ContainerViewer` display list — take uses `Hand.Pickup` instead.
- **Catalog missing in builds:** run **Rebuild All UI Catalogs** and commit `Resources` asset.
- **`InventorySlot` white HUD:** never put `overflow: hidden` on `.inventory-slot__well` (shared with Main HUD).
- **DDOL overlay vs Online hub / Selection enable order:** bind examine/selection events with `Update` retry.
- **Hover label off-screen:** UiShell `ConstantPhysicalSize` — use `InputInterface.ScreenToPanel` then `style.left`/`style.top`.
- **Hover paperdoll blocks Search:** paperdoll is Search/Shift+Click-only; hover is name tooltip.
- **Clothing pick hides CHARACTER:** walk ancestors via `CharacterExamineTargetUtility`.
- **Zone severity for remote examine** reads synced `HealthDebugDetail` (name is historical) — do not expand `HealthSnapshot` just for examine lines.
- **No `box-shadow` in USS:** UI Toolkit rejects it.
- **The pick pass ignores the FOV mask:** selection still reports targets under the black, so anything showing hover information has to ask `VisionSubSystem.IsVisible`. Do not gate examine with `LineOfSight`: it stops at windows, which the FOV sees through, and at furniture, which it doesn't, so it would disagree with the screen.

- **`[MISSING: key]` in EditMode and early frames:** the Examine table loads asynchronously, so `LocalizedTextService` returns the missing marker even for real keys. Look up through `ExamineText.Get`, which formats the English fallback with the same arguments.
- **Examinable with no `ExamineData` showed nothing:** soda cans carry a bare `SubstanceContainerExaminable`; `GetData() == null` read as "no target". `ExamineSubSystem.GetCurrentExaminable` now falls back to the item. `ExamineDataTests.EveryItemPrefabResolvesToAnExamineName` keeps such items named.
- **Remote examine named nobody:** crew spawn with their card inside a PDA in the ID slot, and the old resolver read only a bare card; `PDA.GetInsertedIdCard` also returned null on clients (its container was set only server-side).
- **World object with no `ExamineData` showed nothing:** reinforced walls and windows, girders, three floor tiles, the fuel and water tanks, the airlock panel and the jukebox had a null `key` or no examinable; hover and Shift were silently empty. `StructuralIntegrityPrefabSetup` swaps a `SimpleExaminable` for a `StructuralIntegrityExaminable` but drops the key when the original had none. `ExamineDataTests.EverySelectableWorldObjectHasExamineData` now fails on any selectable non-item prefab without data. After a text edit of a prefab's `key`, the open editor can keep a stale null in memory until the next domain reload; trust a real EditMode run, not an `execute_code` check.
- **Holder-only state:** `RangedWeaponItemExtension` rounds reach only the holder (TargetRpc). Anything examine shows must be synced to observers; check before adding a provider.

## Tests

- EditMode: `ExamineKeywordFormatterTests`, `ExamineDetailTextTests`, `PresentedIdentityTests`, `ExamineContentLineTests`, `SearchVisibilityTests`, `ExamineContentResolverTests`, `ExamineCanonicalKeyGeneratorTests`, `CharacterExamineHealthBuilderTests`, `CharacterExamineContentBuilderTests`, `CharacterExamineSlotContainerMapTests`, `CharacterLootUtilityTests`, `SearchInteractionTests`, `VisionFovTests` (FOV depth lookup).
- PlayMode: `VisionScenarios` (`IsVisible` against real walls); `TaserScenarios.TaserShot_LowersTheChargeExamineShows` (synced rounds).
- AssetAudit: `ExamineDataTests` (every item prefab named; every selectable world object has `ExamineData`; every keyword tag known and closed).

## Depends on / Used by

- **Depends on:** [selection](selection.md), [localization](localization.md), [inputs](inputs.md), [inventory](inventory.md), [health](health.md), [interactions-framework](interactions-framework.md), [interactions-runtime](interactions-runtime.md), [ui-shell](ui-shell.md), [core-subsystems](core-subsystems.md)
- **Used by:** Most world objects with examine content; [tile](tile.md) Map Editor (cursor label override); [structural-destruction](structural-destruction.md) integrity lines, [id-access](id-access.md) card lines

## Related docs

- Known design deltas: [REVIEW_2026-10 — Known delta](../../design/REVIEW_2026-10.md#known-delta-revised-design-vs-code) — rows: id-access (examine names only a bare card)
- Effort: [2026-11_examine-and-action-feedback](../2026-11_examine-and-action-feedback.md) — in-progress
- Plan: [examine_localization_design_5ca361a6.plan.md](../../archive/plans/examine_localization_design_5ca361a6.plan.md), [health_implementation_plan.md](../../plans/health_implementation_plan.md) Phase 6
- Design (read-only): [Documents/design/examine.md](../../design/examine.md), [health.md](../../design/health.md) §7
- Defaults: [2026-07_default-input-scheme.md](../../archive/architecture/2026-07_default-input-scheme.md)
