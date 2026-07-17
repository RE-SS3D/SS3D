> Implements: infrastructure — supports main-hud, lobby, comms, inventory-storage, health, entities, and all UITK surfaces
> Touches systems: application, core-subsystems, scene-management, entities, machine-interface, inputs, inventory, chat-audio-screens, rounds-lobby, crafting, tile, screen-effects, examine, ingame-console, health
> Status: shipped (policy); code deferred — see Follow-on efforts

# Agent-first composition

Binding policy for how systems, UI, and content prefabs are composed so agents can ship features without hand-editing Unity graph YAML. **No runtime code in this effort** — follow-on efforts implement bootstrap, UiShell, and prefab tooling.

## Why

Agents excel at C#, UXML/USS, and docs. They fail at Unity-owned object graphs: scenes, fat prefabs, and SerializeField host forests. Dual uGUI + UI Toolkit stacks compound that with click-through and wiring bugs.

Pressure already in-tree:

- `ScreenEffectsSubSystem` self-bootstraps via `RuntimeInitializeOnLoadMethod` to avoid `Boot.unity` YAML
- `MachineInterfaceHost.EnsureEditorAssets` papers over scene SerializeField debt (Editor-only; fails in builds) — **mitigated for MI** by [2026-07_mi-path-catalog.md](2026-07_mi-path-catalog.md)
- [`Human.prefab`](../../Assets/Content/WorldObjects/Entities/Humanoids/Human/Human.prefab) (~15k lines, ~120 `m_Script` refs) — health, inventory, movement, interactions, and body parts piled onto one graph agents cannot safely edit

## Scene composition policy

- New features **must not** require edits to `Boot.unity` / `Game.unity` to register systems or UI hosts (unless the task *is* the bootstrap effort).
- Subsystems are code-owned (spawn/register from bootstrap), not scene-placed GameObjects.
- Networked subsystems: one hub contract (`NetworkSystemsHub`) — not one NetworkObject per system in Boot. Implementation deferred.

## Prefab composition policy

- Content prefabs own mesh/rig/colliders/`NetworkObject` and a **small** root surface.
- Features attach via code, ScriptableObject recipes, or **Editor setup tools** (`PrefabUtility` / menu items) — not “open Human.prefab and Add Component.”
- Agents **must not** hand-edit mega-prefab YAML.
- Domain redesigns that touch entity wiring purge obsolete components and leave a thinner root — do not grow the dump. Model: [health_implementation_plan.md](../plans/health_implementation_plan.md) Phase 0d (strip-and-rewire). Longer-term: recipe/setup tools so even that rewire is tool-mediated.

## UI policy

- New player-facing UI is **UI Toolkit only** (UXML/USS + catalog + path-based asset load).
- Shared tokens (`ss3d-tokens`, diegetic tokens/typography) are the visual system.
- No new `UnityEngine.UI` / TMP uGUI in gameplay code.
- Reference pattern until UiShell exists: [machine-interface](systems/machine-interface.md), radial, armed overlays.
- Do **not** port condemned uGUI to UITK as a bridge — redesigns replace UX; build the new surface.

## Replace-and-purge

When a redesign effort starts:

1. **Phase 0** deletes that domain’s legacy **UI** and obsolete **prefab components** for that domain.
2. Delete simulation only when design says so (same rule as health clean-slate).
3. Never uGUI→UITK port of a surface the design replaces.
4. Never “add one more behaviour” on a mega-prefab as the feature path.

## Condemned UI

Do not extend or restyle. Replace per design + Phase 0 purge.

| Surface | Replaced by |
|---|---|
| Inventory / hands / intent uGUI | [main-hud.md](../design/main-hud.md), [inventory-storage.md](../design/inventory-storage.md) |
| Chat window (always-on box) | [comms.md](../design/comms.md) |
| Lobby job-select UI | [lobby.md](../design/lobby.md) |
| Crafting menu uGUI | [crafting.md](../design/crafting.md) |
| TileMap creator uGUI | creative-mode / construction redesign (editor) |
| ScreenEffects debug Canvas | delete with health rewrite |
| Examine uGUI views | when HUD / examine redesign lands |
| In-game console uGUI | when debug layer moves to UITK |

## Prefab composition debt

Do not extend by hand. Owning redesigns purge + rewire (preferably via Editor tools).

| Asset | Notes |
|---|---|
| `Human.prefab` + body-part/organ prefabs | Canonical mega-prefab; health / inventory / entity redesigns own strip-and-rewire |
| Vendor / machine prefabs with MI controllers | Same debt class at smaller scale; prefer setup menus over raw YAML |

## Follow-on code efforts

Named only — separate architecture efforts when scheduled:

| Effort | Intent |
|---|---|
| (a) Subsystem bootstrap + `NetworkSystemsHub` | Empty Boot/Game of per-system GameObjects |
| (b) UiShell + path catalog | **Wedges shipped:** MI ([mi-path-catalog](2026-07_mi-path-catalog.md)) and Main HUD (`MainHudAssetCatalog` — see [inventory](systems/inventory.md)). **Still deferred:** full UiShell layers/document ownership; **shared catalog infrastructure** so a third UITK surface does not copy-paste Paths/SO/rebuild-menu again ([ui-shell](systems/ui-shell.md) § Future work) |
| (c) Main-HUD UITK slice | **Partial:** player overlay (`MainHudSubSystem` — hands, gear, equipment, intent) + `MainHudAssetCatalog` Resources load; vitals/alerts/self-examine and Phase 0 uGUI purge still open — see [inventory](systems/inventory.md) |
| (d) Entity prefab setup / recipes | Safe `Human.prefab` evolution beyond one-off Phase 0d edits |

## Related docs

- System maps: [ui-shell](systems/ui-shell.md), [entities](systems/entities.md), [core-subsystems](systems/core-subsystems.md), [machine-interface](systems/machine-interface.md)
- Agent rules: [AGENTS.md](../../AGENTS.md) § Composition, prefabs, and UI
- Authoring: [Documents/SKILL.md](../SKILL.md) (`condemned` status, Phase 0 purge note)
