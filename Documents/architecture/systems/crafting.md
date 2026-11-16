> Code paths: none (obsolete runtime purged)
> Entry points: none — redesign not started
> Status: stub
> Verified: e2f86cf20 — 2026-11-05

# Crafting

## Overview

Obsolete recipe-crafting runtime, hand `Craft` extension, uGUI menu, recipe assets, and
`CraftingSubSystem` hub registration were **purged** (TECH_DEBT 1.6). Do not reintroduce those
types. The design is `deferred` (no milestone up to MVP2 needs it); future freeform crafting
follows it only — no implementation yet. Only leftover: the unused `InteractionIcons.Crafting`
codegen constant in `Assets/Scripts/SS3D/Data/Generated/InteractionIcons.cs`. Substances
`RecipeDefinition` is chemistry reactions, not crafting.

## Start here

- Design (read-only): [Documents/design/crafting.md](../../design/crafting.md) — intended direction
- Debt close: [TECH_DEBT.md](../TECH_DEBT.md) §6 Resolved / former §1.6

## Extension points

None until a redesign effort lands. Prefer a new architecture effort + Phase 0 from design over
reviving deleted recipe graphs or uGUI menus.

## Pitfalls

*(none for live code — system has no runtime surface)*

## Depends on / Used by

- **Depends on:** none (code removed)
- **Used by:** no runtime consumers. Waiting on it: MVP2 S3 cloning (fabricator pattern,
  [mvp2-station-round](../../milestones/mvp2-station-round.md)) and structural-damage repair
  (Phase 6 of [station_structural_damage.plan.md](../../plans/station_structural_damage.plan.md))

## Related docs

- Design (read-only): [Documents/design/crafting.md](../../design/crafting.md)
- [2026-07_agent-first-composition](../2026-07_agent-first-composition.md) (crafting menu row: purged)
- [INDEX.md](../INDEX.md)
