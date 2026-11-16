> Implements: infrastructure — supports main-hud, lobby, comms, inventory-storage, health, entities, and all UITK surfaces
> Touches systems: application, core-subsystems, scene-management, entities, machine-interface, inputs, inventory, chat-audio-screens, rounds-lobby, crafting, tile, screen-effects, examine, ingame-console, health
> Status: shipped

**Progress:** Standing policy; bootstrap and UI-shell foundations shipped, remaining composition work is tracked under Follow-on efforts.

# Agent-first composition

Binding policy for how systems, UI, and content prefabs are composed so agents can ship features without hand-editing Unity graph YAML. **No runtime code in this effort** — follow-on efforts implement bootstrap, UiShell, and prefab tooling.

## Why

Agents excel at C#, UXML/USS, and docs. They fail at Unity-owned object graphs: scenes, fat prefabs, and SerializeField host forests. Dual uGUI + UI Toolkit stacks compound that with click-through and wiring bugs.

Pressure already in-tree:

- UiShell / MainHud / StoragePanel still self-bootstrap via `RuntimeInitializeOnLoadMethod` until follow-on **(b)**
- `MachineInterfaceHost.EnsureEditorAssets` papers over scene SerializeField debt (Editor-only; fails in builds) — **mitigated for MI** by [2026-07_mi-path-catalog.md](../archive/architecture/2026-07_mi-path-catalog.md)
- [`Human.prefab`](../../Assets/Content/WorldObjects/Entities/Humanoids/Human/Human.prefab) (~15k lines, ~120 `m_Script` refs) — health, inventory, movement, interactions, and body parts piled onto one graph agents cannot safely edit

## Scene composition policy

- New features **must not** require edits to `Boot.unity` / `Game.unity` to register systems or UI hosts (unless the task *is* the bootstrap effort).
- Subsystems are code-owned: `SystemsBootstrap` (process-wide DDOL) + `NetworkSystemsHub` prefab (Online) — not scene-placed GameObjects.
- Networked subsystems: one hub NetworkObject — rebuild via **SS3D → Bootstrap → Rebuild NetworkSystemsHub Prefab** (tier A).

## Prefab composition policy

- Content prefabs own mesh/rig/colliders/`NetworkObject` and a **small** root surface.
- Features attach via code, ScriptableObject recipes, or **tier-B Editor recipes** (`PrefabUtility` statics registered on a domain **Run All …** aggregator — not a new permanent `SS3D/.../Setup …` MenuItem). See [2026-07_editor-tooling-tiers.md](2026-07_editor-tooling-tiers.md).
- Agents **must not** hand-edit mega-prefab YAML.
- Domain redesigns that touch entity wiring purge obsolete components and leave a thinner root — do not grow the dump. Model: [health_implementation_plan.md](../plans/health_implementation_plan.md) Phase 0d (strip-and-rewire). Recipe/setup tools mediate that rewire.

## UI policy

- New player-facing UI is **UI Toolkit only** (UXML/USS + catalog + path-based asset load).
- Shared tokens (`ss3d-tokens`, diegetic tokens/typography) are the visual system.
- No new `UnityEngine.UI` / TMP uGUI in gameplay code.
- Attach new surfaces through [ui-shell](systems/ui-shell.md); [machine-interface](systems/machine-interface.md) documents the catalog/path pattern for surfaces awaiting migration.
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
| Inventory / hands / intent uGUI | **Phase 0 purged** — [inventory.md](systems/inventory.md) confirms uGUI fully removed, not just disabled |
| Chat window (always-on box) | **Phase 0 purged** — `ChatSubSystem` also purged; feed/PDA via [comms.md](../design/comms.md) + [2026-07_comms-non-diegetic-feed.md](../archive/architecture/2026-07_comms-non-diegetic-feed.md) |
| Lobby job-select UI | **Phase 0 purged** — UITK lobby shipped; [rounds-lobby](systems/rounds-lobby.md) |
| Crafting menu uGUI | **Phase 0 purged** (TECH_DEBT 1.6) — redesign per [crafting.md](../design/crafting.md) |
| TileMap creator uGUI | Construction prefabs purged; residual `AssetSlot.cs` remains. Live hologram/layer helpers stay — see [tile](systems/tile.md) §Overview |
| ScreenEffects debug Canvas | Still present, opened through the F1 tool registry; delete with owning debug presentation replacement |
| Examine uGUI views | **Phase 0 purged** (2026-07-26) — `ExamineOverlaySubSystem` on UITK per [examine.md](systems/examine.md) |
| In-game console uGUI | when debug layer moves to UITK |

## Prefab composition debt

Do not extend by hand. Owning redesigns purge + rewire (preferably via Editor tools).

| Asset | Notes |
|---|---|
| `Human.prefab` + body-part/organ prefabs | Canonical mega-prefab; health / inventory / entity redesigns own strip-and-rewire |
| Vendor / machine prefabs with MI controllers | Same debt class at smaller scale; prefer tier-B PrefabUtility recipes over raw YAML |

## Follow-on code efforts

Named only — separate architecture efforts when scheduled:

| Effort | Intent |
|---|---|
| (a) Subsystem bootstrap + `NetworkSystemsHub` | **Shipped:** [2026-07_session-world-lifecycle.md](../archive/architecture/2026-07_session-world-lifecycle.md) — Boot/Game emptied of SubSystem GOs; `SystemsBootstrap` + hub prefab |
| (b) UiShell + path catalog | **Wedges shipped:** MI ([mi-path-catalog](../archive/architecture/2026-07_mi-path-catalog.md)), Main HUD (`MainHudAssetCatalog` — see [inventory](systems/inventory.md)), and shared scaffolding + radial/armed migration ([ui-shell-consolidation](2026-07_ui-shell-consolidation.md)). **Still deferred:** migrating MI/Main HUD/Storage Panel onto the shared `UiShellSubSystem` + `UiAssetCatalogBase` ([ui-shell-consolidation](2026-07_ui-shell-consolidation.md) § Explicit non-goals) |
| (c) Main-HUD UITK slice | **Partial:** player overlay (`MainHudSubSystem` — hands, gear, equipment, intent) + `MainHudAssetCatalog` Resources load; alert stack, self-examine and uGUI purge shipped; vitals cluster remains open — see [inventory](systems/inventory.md) |
| (d) Entity prefab setup / recipes | [Entity prefab recipes](systems/entities-prefab-recipes.md) define the current aggregator and integrity contracts; remaining root-composition debt is tracked in [TECH_DEBT §1.1](TECH_DEBT.md#11-humanprefab-mega-prefab) |

## Related docs

- System maps: [ui-shell](systems/ui-shell.md), [entities](systems/entities.md), [core-subsystems](systems/core-subsystems.md), [machine-interface](systems/machine-interface.md)
- Agent rules: [AGENTS.md](../../AGENTS.md) § Composition, prefabs, and UI
- Authoring: [Documents/SKILL.md](../SKILL.md) (`condemned` status, Phase 0 purge note)
