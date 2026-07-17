> Implements: [Documents/design/health.md](../design/health.md) (consciousness / death presentation), [Documents/design/death-cloning-respawn.md](../design/death-cloning-respawn.md) (corpse / ghost handoff)
> Touches systems: entities, health
> Status: planned

# Body presentation authority

Future refactor. Banked after the health-rewrite collapse bugs (death crash, upright walk-cycle corpses, unconsciousness that blocked movement but never ragdolled).

## Problem

**Nobody owns humanoid body presentation as a single authority.** Health, ragdoll, animator orchestration, body-state snapshots, limp bridging, living/predicted movement, and FishNet lifecycle all write the same body independently.

“Collapse” was implemented as scattered side effects: disable a component, set a SyncVar, hope `OnChange` runs on the server, hope Coimbra stops updating, hope ownership teardown does not `Recover()`. Death only looked correct after an explicit observers reinforce RPC; unconsciousness failed while using the fragile path.

## Lessons (binding for interim patches)

1. **One writer for collapsed/dead presentation.** Health emits intent (`Alive` / `Collapsed` / `Dead`). One presentation layer applies it (ragdoll on, animator suppressed, movement off). Other systems *read* that state — they do not invent their own collapse behavior.
2. **Do not use transport quirks as control flow.** `ServerRpc` from server is a no-op. SyncVar `OnChange` may not fire on the server when assigning. `OnDisable` during ownership/network teardown is not “recover.” Prefer explicit server methods + observers RPCs (or one replicated presentation enum).
3. **`enabled = false` is insufficient with Coimbra `UpdateEvent`.** Listeners keep firing after disable — use unsubscribe or an explicit suppress flag (`AnimationOrchestrator.SetPosingSuppressed`).
4. **Match gameplay words to signals.** “Lose consciousness” is not only `!IsConscious` — cardiac arrest can still report conscious until brain ≤10%. Collapse rules must be intentional.
5. **Lifecycle contracts are sacred.** `OnAwake` → `base.OnAwake()`, never `base.Awake()` (ghost stack-overflow).

## Current interim (do not grow)

Working but stopgap paths on `health-rewrite`:

- Death: `Ragdoll.ServerDeathRagdoll` + `Human.RpcApplyDeathRagdoll`
- Unconscious / cardiac arrest: `HumanHealthController.ApplyConsciousnessRagdoll` + `RpcSetConsciousnessCollapsed` → `Ragdoll.ApplyCollapseVisuals`
- Animator: `SetPosingSuppressed` + disable Orchestrator / BodyStateMachine while down

**Do not** add a third collapse path (another SyncVar-only or LivingController-only special case). Extend the shared collapse visuals API or wait for this refactor.

## Target architecture

| Layer | Owns |
|-------|------|
| Health | Intent only: conscious / cardiac arrest / dead (and eventually a single `BodyPresentationIntent` or similar) |
| Body presentation (new or elevated) | One replicated state + apply: ragdoll physics, animator suppress, movement gate, stand-up |
| AnimationOrchestrator / BodyStateBridge | Pose *only* when presentation allows; never fight ragdoll |
| `Human.Kill` / ghost | Mind transfer + component teardown *after* presentation is `Dead` |

Suggested shape (refine when commissioned):

1. Replicated `BodyPresentationState` (`Locomotion` / `Collapsed` / `Dead`) on the humanoid root.
2. Single applier (`BodyPresentationController` or fold into a slimmed `Ragdoll` + orchestrator contract) that observers run identically to the server.
3. Movement (`HumanoidLivingController` / predicted) and limp bridge early-out on presentation ≠ `Locomotion`.
4. Remove death-only vs unconscious-only duplicate RPCs once the shared state exists.
5. Phase 0: delete `OnDisable → Recover`, ServerRpc-as-server-death calls, and SyncVar-OnChange-as-sole-collapse triggers.

## Out of scope for this note

- Prefab strip of `Human.prefab` (that remains [agent-first composition](2026-07_agent-first-composition.md) / health Phase 0d).
- Ghost controller / mind-swap redesign beyond presentation handoff.
- Full animation system redesign ([player-body-animation](2026-07_player-body-animation.md) remains the locomotion foundation).

## Related

- System maps: [entities](systems/entities.md), [health](systems/health.md)
- Plan context: [health_implementation_plan.md](../plans/health_implementation_plan.md)
- Triggering incidents: health-rewrite death/unconscious ragdoll work (commits through `d6d269dea`)
