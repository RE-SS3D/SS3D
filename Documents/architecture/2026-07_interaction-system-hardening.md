> Implements: Documents/design/ (interaction-related gameplay expectations)
> Touches systems: interactions framework, interactions runtime, selection, inventory, combat
> Status: shipped

# Interaction system hardening (Jul 2026)

## Goal

Harden SS3D's interaction system for production multiplayer without rewriting the source/target model.

## Shipped phases

### Phase 1 — Stable identification & RPC safety
- `InteractionIdentifier` wire protocol
- RPC signatures use `genericName` + `targetComponentIndex`
- `GetGenericName()` on all interactions
- Null-safe observer RPCs

### Phase 2 — Deterministic discovery
- `IInteraction.Priority` + pipeline sort
- `InteractionPipeline` shared by client and server
- `OpenInteraction` routes through `NetworkedOpenable.SetOpenState`

### Phase 3 — Gameplay gates & optimistic feedback
- Intent sync + `IIntentRestrictedInteraction`
- Stamina gates on hand + server validation
- Inventory ownership validation
- `InteractionPermission` for lockers
- Delayed optimistic loading bars + `TargetRejectInteraction`
- Instant pending outline on click (blue)

### Phase 4 — Cancellation
- Active interaction tracking
- `CmdCancelInteraction` (C key)
- Movement auto-cancel on `DelayedInteraction`

### Phase 5 — Tests & docs
- `InteractionPipelineTests` (EditMode)
- PlayMode pickup regression via existing `PlayerCanDropAndPickUpItem`
- Contract XML on `IInteraction` / `IIntentRestrictedInteraction`
- System maps + this effort doc

### Follow-up (branch extras)
- Hover availability outlines (green/yellow), entity exclusion, silhouette shader

## Deferred / out of scope

- Full inventory prediction
- Interaction ScriptableObject registry
- RPC rate limiting
- Body-part targeting integration
- Damage/stun auto-cancel
- Medical UI outlines on entities

## Success criteria

- [x] No RPC matches on `GetName()`
- [x] Deterministic primary-click order via `Priority`
- [x] Intent and stamina in discovery + server path
- [x] Inventory ownership validation
- [x] Open/close via `NetworkedOpenable` when available
- [x] Client feedback on attempt with rollback on rejection
- [x] EditMode pipeline tests
- [x] PlayMode pickup regression (existing test)
