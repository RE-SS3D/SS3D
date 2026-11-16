> Implements: infrastructure — pays down [TECH_DEBT.md](TECH_DEBT.md) §1.5 menu proliferation; codifies Editor tooling policy named in [2026-07_agent-first-composition.md](2026-07_agent-first-composition.md)
> Touches systems: entities, inventory, combat, structural-destruction, data-codegen, networking-session, ui-shell, atmospherics, examine
> Status: shipped

# Editor tooling tiers

Agent-first composition correctly forbids hand-editing mega-prefabs and Boot/Game. PrefabUtility
setup scripts were the approved escape hatch — but every script also got a permanent
`[MenuItem("SS3D/...")]`, so the toolbar accumulated finished migrations and one-shot wiring.

**Rule:** the *capability* stays; the *menu surface* does not grow by default.

## Tiers

| Tier | When | Menu surface | Examples |
|------|------|--------------|----------|
| **A — Repeatable tools** | Humans run often in day-to-day work | Keep `[MenuItem]` under `SS3D/` (or `Tools/SS3D/`) | Build, Perf export, MI preview windows, animator rebuild/rebind, Auto-Bind Rig, `Rebuild NetworkSystemsHub Prefab`, RuleSetManager |
| **B — Idempotent recipes** | PrefabUtility / asset wiring that must stay re-runnable after merges or new content | **No individual MenuItem.** Public static `Run`/`Wire`/`SetupAll` + register in a domain **Run All …** aggregator (optional single menu). Prefer EditMode integrity tests where drift is checkable. BatchMode via `-executeMethod` on the aggregator. | `HumanPrefabRecipes`, `CombatContentPrefabRecipes`, `InventoryContentPrefabRecipes`, `StructuralDamageContentPrefabRecipes`; demoted atmos registry / GridCondition statics |
| **C — Time-boxed migrations** | One-shot rewrites tied to an architecture effort | Prefer `-executeMethod` only during the effort. **Delete menus (and usually scripts) when Status ships.** | Finished URP Migration menus, Bootstrap Phase 3h strip menus, MI clear-host scene refs, one-shot localization key seeders |

## Catalog rebuilds (interim)

UI path catalogs (MI / Main HUD / UI Shell / Storage Panel) still use per-surface builders via
`UiCatalogBuilderKit` — known debt under [TECH_DEBT.md](TECH_DEBT.md) §1.4/1.5 and
[data-codegen.md](systems/data-codegen.md). Until the shared Addressables → codegen path lands:

- Keep builder **methods**; do **not** add another `SS3D/.../Rebuild Asset Catalog` MenuItem.
- Prefer one umbrella `SS3D/Data/Rebuild All UI Catalogs` (or the existing four menus until collapsed).
- Interaction Icons + Map Editor bake/regen remain named exceptions.

## Aggregator pattern

Model: [`HumanPrefabRecipes`](../../Assets/Scripts/SS3D/Systems/Entities/Editor/HumanPrefabRecipes.cs).

1. Implement idempotent `SetupAll` / `Wire` / `Strip` on a static Editor class (no MenuItem).
2. Register a call in the domain `*ContentPrefabRecipes` / `*PrefabRecipes` `RunAllMenu` + `RunAllBatch`.
3. Document the aggregator path in the system map **Start here** / **Extension points** — not each leaf recipe as its own menu.

## Anti-patterns

- Shipping a feature with a new permanent `SS3D/.../Setup …` MenuItem for a one-time wire.
- Leaving Phase N migration menus after the effort Status is `shipped`.
- Copy-pasting a fifth UI catalog rebuild MenuItem instead of extending `UiCatalogBuilderKit`.
- Hand-editing `Human.prefab` / Boot / Game because “there’s no menu” — write a tier-B recipe instead.

## Related docs

- [2026-07_agent-first-composition.md](2026-07_agent-first-composition.md)
- [TECH_DEBT.md](TECH_DEBT.md) §1.5
- [TECH_DEBT.md](TECH_DEBT.md) §1.4–1.5, §1.15
- [entities.md](systems/entities.md) (Human recipe registry)
