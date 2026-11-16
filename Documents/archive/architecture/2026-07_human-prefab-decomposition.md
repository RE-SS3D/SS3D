> Implements: infrastructure — pays down [TECH_DEBT.md](../../architecture/TECH_DEBT.md) §1.1 and delivers follow-on (d) "Entity prefab setup / recipes" named in [2026-07_agent-first-composition.md](../../architecture/2026-07_agent-first-composition.md)
> Touches systems: entities, health, inventory, combat, core-subsystems
> Status: shipped

**Progress:** Recipe infrastructure shipped. Current recipe contracts live in the entities-prefab-recipes system map; remaining composition debt is tracked in TECH_DEBT §1.1.

# Human.prefab decomposition

Ranked #1 structural risk in [TECH_DEBT.md](../../architecture/TECH_DEBT.md) §1.1. This effort turns "strip-and-rewire via
Editor tooling" from a one-off precedent into a repeatable, machine-checked convention, and fixes the
concrete hygiene debt found on `Human.prefab` along the way (Phase 0). Phase 1's original organ-extraction
scope turned out to be based on a wrong assumption and is deprioritized; Phase 2 (recipe convention) is
done; Phase 3 (domain strip-and-rewire) has its first concrete instance done — inventory's hands wiring
is now recipe-managed. Remaining Phase 3 domains stay scheduled-not-forced, picked up as their own
redesigns touch entity wiring.

## Problem

`Assets/Content/WorldObjects/Entities/Humanoids/Human/Human.prefab` is 15,604 lines with 126 `m_Script`
occurrences (39 distinct component types) spanning health, inventory, movement/animation, combat,
entity identity, interactions, comms, examine, selection, stamina, substances, and networking — one
GameObject graph no agent can safely hand-edit. Auditing it for this effort surfaced four concrete,
previously undocumented problems, not just the general size complaint:

1. **The only precedent tool isn't a Unity tool.** Health's Phase 0d strip-and-rewire (`health.md`,
   `health_implementation_plan.md`) was executed by `Tools/migrate_health_prefabs.py` — a standalone
   Python script doing regex/GUID find-replace on raw prefab YAML outside the Editor, not
   `PrefabUtility`. TECH_DEBT.md's phrase "strip-and-rewire via Editor tooling" describes this
   loosely; there is no reusable C# recipe, contradicting the policy in
   [agent-first-composition.md](../../architecture/2026-07_agent-first-composition.md) that prefab mutation should be
   tool-mediated via `PrefabUtility`/menu items (the pattern already used correctly elsewhere, e.g.
   `Assets/Scripts/SS3D/Systems/Combat/Editor/MeleePrefabSetup.cs`,
   `Assets/Scripts/SS3D/Systems/Inventory/Containers/Editor/StorageContainerPrefabSetup.cs`).
2. **Phase 0d left orphaned missing-script stubs.** 4 of the 39 `m_Script` GUIDs on `Human.prefab` do
   not resolve to any `.cs.meta` in the repo — residual `PrefabInstance` component-override blocks on
   `HumanTorso`/`HumanHandRight`/`HumanFootLeft`/`HumanHead` left over from the migration to
   `AnatomyNode`, despite the health plan's stated acceptance criterion that missing-script cleanup was
   part of Phase 0d.
3. **Docs already point at a tool that was never built.** Both `inventory.md` and `combat.md` cite
   `Assets/Scripts/SS3D/Systems/Inventory/Containers/Editor/BodyPartContainerInteractiveStrip.cs`
   ("SS3D → Inventory → Strip Head/Torso ContainerInteractive") as the mechanism that stripped
   `ContainerInteractive` from `HumanHead`/`HumanTorso`. It does not exist anywhere in the working tree
   or git history — the strip was done by hand, and there is currently no way to safely re-run it if it
   regresses.
4. **Organs are copy-pasted visual meshes, not prefabs — but they are not duplicates of `HumanOrgans/*.prefab`.**
   Each organ inlined in `Human.prefab` (`HumanBrain`, `HumanHeart`, `HumanLungLeft/Right`,
   `HumanStomach`, `HumanLiver`, `HumanAppendix`, `HumanIntestineLarge/Small`, duplicated again in
   `TestHuman.prefab`) is a bare `Transform` + `SkinnedMeshRenderer` — the in-body decorative mesh rigged
   to the skeleton, nothing else. `HumanOrgans/*.prefab` (e.g. `HumanHeart.prefab`) is a *different*
   thing: a standalone networked item (`NetworkObject`, `Rigidbody`, `Item`, `OrganInstance`, `Selectable`)
   representing the organ once surgically removed. `health-anatomy-map.md` already documents this
   duality for Liver ("inline on Human.prefab + item prefab") as current state, not a flagged bug — it's
   true of every organ, just previously unstated as a general pattern. **This means "replace the inline
   mesh with a `PrefabInstance` of `HumanOrgans/*.prefab`" is the wrong fix** — you cannot sensibly nest
   a `NetworkObject`+`Rigidbody`+`Item` prefab as a bone-rigged decorative mesh inside an animated body.
   The only real duplication is the ~9 × 2-component inline mesh itself, copy-pasted between
   `Human.prefab` and `TestHuman.prefab` (~150–200 lines total) — real but minor, not the size driver.
   Deprioritized; see Phase 1.
5. A dev-only hack, `Assets/Scripts/SS3D/Hacks/RagdollWhenPressingButton.cs`, is attached directly to
   production `Human.prefab` — the same class of leftover `combat.md` already flags for
   `CombatDummyBootstrap` ("not on Human.prefab, `AddComponent`'d only on spawn").

Enforcement today is convention only: nothing stops a 127th component, another debug hack, or another
copy-pasted organ from landing on `Human.prefab` in review.

## Goals

- A reusable, Unity-native (`PrefabUtility`-based) recipe pattern for evolving `Human.prefab`, so no
  future change requires hand-editing its YAML — this is follow-on (d) from
  [agent-first-composition.md](../../architecture/2026-07_agent-first-composition.md).
- A machine-checked gate (EditMode test in the existing headless CI run) that fails a PR which adds an
  unreviewed component, reintroduces a debug-only hack, or leaves a missing-script reference — closing
  "enforcement is convention only."
- The hygiene gaps found in the initial audit (missing-script drift, the never-built strip tool, the
  dev-only hack) fixed as part of the same pass, since they're concrete debt on the exact prefab this
  effort touches, not separate work.

## Non-goals

- Rewriting health/inventory/combat/movement components themselves, or changing runtime behavior.
- Forcing every domain (hands wiring, comms, examine, stamina) to strip-and-rewire in one pass — each
  keeps doing that as part of its own redesign touching entity wiring (existing policy), this effort
  only makes sure a recipe tool exists when they do.
- Body presentation ownership (collapse/death/ragdoll) — that's
  [2026-07_body-presentation-authority.md](2026-07_body-presentation-authority.md), out of scope here
  by that doc's own "Out of scope" note.

## Phased plan

### Phase 0 — Stabilize and instrument (no behavior change)

- Remove the 4 orphaned missing-script override stubs on the `HumanTorso`/`HumanHandRight`/
  `HumanFootLeft`/`HumanHead` `PrefabInstance` blocks in `Human.prefab` (verify `AnatomyNode` already
  covers the behavior before deleting — it should, per health.md).
- Write `BodyPartContainerInteractiveStrip.cs` (`Assets/Scripts/SS3D/Systems/Inventory/Containers/
  Editor/`) as a real `[MenuItem("SS3D/Inventory/Strip Head/Torso ContainerInteractive")]` tool,
  mirroring `StorageContainerPrefabSetup.cs`'s structure, so the two system maps that already cite it
  stop being wrong and the strip becomes safely re-runnable.
- Move or delete `RagdollWhenPressingButton.cs` off production `Human.prefab` (test-only; belongs on a
  `CombatDummyBootstrap`-style spawn-time `AddComponent`, or deleted if superseded by `spawndummy`).
- Add an EditMode test (`Assets/Scripts/Tests/EditMode/`) that loads `Human.prefab` and asserts: no
  `m_Script` GUID fails to resolve to a `.cs.meta`; no component type on an explicit debug/dev
  denylist (starting with anything under `SS3D/Hacks/`) is present. Wire it into
  `.github/workflows/editmodetestrunner.yml` (already runs EditMode headlessly — no new CI plumbing).

**Status: done and verified.** The owner ran `SS3D → Entities → Run All Human Prefab Recipes` in the
Editor (twice — the second pass picked up the `ResyncNestedPrefabInstances` fix below). Confirmed by
direct inspection of the committed `Human.prefab`: zero `RagdollWhenPressingButton` references, zero
`ContainerInteractive` references (root or dangling mirror), zero `m_Script` GUIDs that fail to resolve
to a `.cs.meta`. Both `HumanPrefabIntegrityTests` assertions are re-enabled (no longer `[Ignore]`d) and
pass. Play Mode smoke verification (movement, hands, `spawndummy`, examine, speech) is the owner's call
on timing — no known regression risk given the above.

Running the tools the first time surfaced one more real bug, now fixed: `BodyPartContainerInteractiveStrip`
edits `HumanHead.prefab`/`HumanTorso.prefab` directly, but `Human.prefab`'s own stripped mirror of those
instances doesn't refresh until `Human.prefab` itself is reloaded and resaved — so the first pass left
`Human.prefab` with 2 dangling stripped mirrors pointing at the now-deleted `ContainerInteractive`
instances. Fixed by adding `HumanPrefabHygiene.ResyncNestedPrefabInstances()` (menu:
**SS3D → Entities → Resync Human Prefab Against Body Parts**), which `HumanPrefabRecipes.RunAllMenu`
now always calls last regardless of what the other recipes changed — see entities.md § Pitfalls.

### Phase 1 — Organ mesh de-duplication (deprioritized)

**Revised after investigation — do not implement as originally scoped.** The original plan here was
"replace the inline organ mesh with a `PrefabInstance` of `HumanOrgans/*.prefab`" — wrong, per Problem
item 4 above: those are different prefabs for different roles (in-body decorative mesh vs. standalone
networked item), not two copies of the same thing. The only real, much smaller win available is
extracting each inline `Transform`+`SkinnedMeshRenderer` mesh into its own tiny visual-only prefab (no
`NetworkObject`) so `Human.prefab`/`TestHuman.prefab` reference one shared asset instead of two
independent copies (~150–200 lines total, not a meaningful size lever). Left unscheduled — pick up only
if `Human.prefab`/`TestHuman.prefab` visual drift between the two organ copies becomes an actual problem.

### Phase 2 — Name the recipe convention — done this pass

- Documented (here and in `entities.md`) what a "recipe" is: a `PrefabUtility`-based Editor menu item
  under `SS3D/<Domain>/<Verb>`, one per domain, that attaches or verifies a domain's wiring on an
  entity prefab — generalizing the pattern `MeleePrefabSetup.cs` and `StorageContainerPrefabSetup.cs`
  already used, now also followed by `HumanPrefabHygiene.cs` and `BodyPartContainerInteractiveStrip.cs`
  (Phase 0).
- Added `HumanPrefabRecipes.cs` (`Assets/Scripts/SS3D/Systems/Entities/Editor/`,
  `SS3D/Entities/Run All Human Prefab Recipes`) as the single aggregator entry point — calls
  `HumanPrefabHygiene.RemoveDevHacks()` and `BodyPartContainerInteractiveStrip.StripAll()` in sequence.
  Scoped deliberately to recipes that mutate `Human.prefab`/its nested body-part prefabs; `MeleePrefabSetup`
  (hand tools) and `StorageContainerPrefabSetup` (backpacks/lockers) already exist and follow the same
  convention independently — not folded in here since they have nothing to do with Human.prefab
  decomposition specifically.
- No behavior change — this phase is discoverability and naming, not new extraction. Still needs the
  same Editor run + Play Mode verification as Phase 0 before the `[Ignore]`d tests can be re-enabled.

### Phase 3 — Domain strip-and-rewire

**Hands wiring — done this pass.** The debt inventory.md named was concrete: `Human.prefab`'s `Hands`
component carries `PlayerHands` (`[SerializeField] public List<Hand>`), a 2-entry reference list manually
wired by dragging `fileID`s into the Inspector, with no recipe tool to reproduce or verify it. Currently
correctly wired (Left then Right — `Hands.OnStartServer` selects `PlayerHands.FirstOrDefault()` as the
initial active hand, so order is load-bearing, not cosmetic). Added `HandsPrefabSetup.cs`
(`Assets/Scripts/SS3D/Systems/Inventory/Containers/Editor/`, **SS3D → Inventory → Wire Human Hands**),
registered in `HumanPrefabRecipes.cs`. No `NetworkBehaviour` add/remove involved (`PlayerHands` is a
plain reference list, not part of FishNet's `_networkBehaviours` bookkeeping), so no resync step needed
for this one. `HumanPrefabIntegrityTests.HumanPrefab_HandsAreWiredLeftThenRight` guards both membership
and order.

Every other domain directly on `Human.prefab` (movement/animation, combat, comms, examine, stamina,
substances) keeps the existing policy: strip-and-rewire happens when that domain's own redesign touches
entity wiring, using a Phase 2-style recipe tool instead of raw YAML. Nothing forced here — pick up the
next one when its owning redesign is ready to touch entity wiring.

## Verification

- After each phase: open `Human.prefab` and `TestHuman.prefab` in the Unity Editor, confirm zero
  missing-script warnings in the Inspector.
- Host Play Mode smoke pass per existing system-map testing notes: movement, health zone hits, hands/
  equip, `spawndummy` (combat), examine, local speech — the same paths already named in
  `health.md`/`combat.md`/`inventory.md`.
- Run `HumanPrefabIntegrityTests` locally via Test Runner, then confirm it passes in
  `editmodetestrunner.yml` CI — done, all assertions passing, none `[Ignore]`d.
- Play Mode smoke pass for hands specifically after running `HandsPrefabSetup`: swap hands, drop item,
  pick up item in each hand, confirm the correct hand is active on spawn.

## Related docs

- [TECH_DEBT.md](../../architecture/TECH_DEBT.md) §1.1 — ranking and rationale
- [2026-07_agent-first-composition.md](../../architecture/2026-07_agent-first-composition.md) — policy this effort
  implements (follow-on d)
- System maps: [entities](../../architecture/systems/entities.md), [health](../../architecture/systems/health.md), [inventory](../../architecture/systems/inventory.md), [combat](../../architecture/systems/combat.md)
- [health_implementation_plan.md](../../plans/health_implementation_plan.md) — Phase 0d precedent
