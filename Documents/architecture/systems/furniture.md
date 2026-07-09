> Code paths: Assets/Scripts/SS3D/Systems/Furniture/
> Entry points: (various world object behaviours)
> Status: stub

# Furniture / world objects

## Overview

Station furniture and interactable world objects — airlocks, lockers, vending machines, disposal units, draggable objects. (Navigation map not yet fully reviewed.)

## Start here

- `Assets/Scripts/SS3D/Systems/Furniture/` — furniture implementations

## Extension points

- New furniture: add `InteractionTargetBehaviour` + domain-specific interactions per [interactions-framework](interactions-framework.md).

## Depends on / Used by

- **Depends on:** [interactions-framework](interactions-framework.md), [tile](tile.md)

## Related docs

- [INDEX.md](../INDEX.md)
