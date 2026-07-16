> Code paths: Assets/Scripts/SS3D/Systems/Combat/, Assets/Scripts/SS3D/Systems/Entities/Humanoid/Body/
> Entry points: (minimal — hit interactions); combat stance via HumanoidCombatController
> Status: stub

# Combat

## Overview

Minimal hit interaction code on `develop`. Full combat model from design spec (melee windup, blocking, ranged accuracy, damage) is not yet implemented.

**Shipped adjacent foundation (not design combat):** Peaceful/Melee/Ranged stance locomotion, aim look-at IK, and melee swing triggers live under [entities](entities.md) body animation — see [player-body-animation](../2026-07_player-body-animation.md). That is presentation/stance only; it does not implement combat.md hit resolution.

## Start here

- `Assets/Scripts/SS3D/Systems/Combat/Interactions/` — basic hit interactions
- `Assets/Scripts/SS3D/Systems/Entities/Humanoid/Body/HumanoidCombatController.cs` — stance / swing triggers (animation side)

## Extension points

Implement per design spec when the health rewrite and combat effort begin; reuse stance packs already on the humanoid animator.

## Depends on / Used by

- **Depends on:** [interactions-framework](interactions-framework.md), [health](health.md), [entities](entities.md) (stance presentation)

## Related docs

- Design (read-only): [Documents/design/combat.md](../../design/combat.md)
- Effort (stance foundation only): [2026-07_player-body-animation.md](../2026-07_player-body-animation.md)
- [entities](entities.md)
