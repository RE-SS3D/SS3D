> Implements: [Documents/design/health.md](../../design/health.md) (consciousness / death presentation), [Documents/design/death-cloning-respawn.md](../../design/death-cloning-respawn.md) (corpse / ghost handoff)
> Touches systems: entities, health
> Status: shipped

# Body presentation authority

Shipped refactor. Banked after the health-rewrite collapse bugs (death crash, upright walk-cycle corpses, unconsciousness that blocked movement but never ragdolled).

## Problem (historical)

**Nobody owned humanoid body presentation as a single authority.** Health, ragdoll, animator orchestration, body-state snapshots, limp bridging, living/predicted movement, and FishNet lifecycle all wrote the same body independently. Dual death/unconscious reinforce RPCs papered over SyncVar-OnChange gaps.

## Lessons (still binding)

1. **One writer for collapsed/dead presentation.** Health emits intent (`BodyPresentationIntent.FromSnapshot`). `Ragdoll` applies via replicated `BodyPresentationState`.
2. **Do not use transport quirks as control flow.** `ServerRpc` from server is a no-op. SyncVar `OnChange` may not fire on the server when assigning. `OnDisable` during ownership/network teardown is not “recover.” Apply in `ServerSetPresentation` **and** `OnStartNetwork` / OnChange.
3. **`enabled = false` is insufficient with Coimbra `UpdateEvent`.** Use `AnimationOrchestrator.SetPosingSuppressed`.
4. **Match gameplay words to signals.** Critical and cardiac arrest can still report conscious until brain ≤10% — collapse must OR those states, not only `!IsConscious`.
5. **Lifecycle contracts are sacred.** `OnAwake` → `base.OnAwake()`, never `base.Awake()` (ghost stack-overflow).

## Shipped architecture

| Layer | Owns |
|-------|------|
| Health | Intent only via `BodyPresentationIntent` → `Ragdoll.ServerSetPresentation` (Collapsed/Locomotion). Death ignored here — `Human.Kill` sets Dead. |
| `Ragdoll` | Replicated `BodyPresentationState` (`Locomotion` / `Collapsed` / `Dead`) + `ApplyPresentation` (physics, animator suppress, movement gate, stand-up). |
| AnimationOrchestrator / BodyStateBridge / LivingController | Read `Ragdoll.Presentation`; never invent Health consciousness rules. |
| `Human.Kill` / ghost | `ServerDeathRagdoll` **before** mind transfer; component teardown after. |

### API surface

- `BodyPresentationState` — enum on humanoid root SyncVar inside `Ragdoll`
- `Ragdoll.ServerSetPresentation(state, timed?, addSeconds?)` — sole server writer
- `Ragdoll.ServerDeathRagdoll` / `ServerKnockdownTimeless` / `ServerKnockdown` / `ServerRecover` — thin wrappers
- `BodyPresentationIntent.FromSnapshot` — Health mapping (EditMode: `BodyPresentationIntentTests`)

### Do not

- Add a parallel collapse RPC or SyncVar-only path.
- Gate `ServerSetPresentation` on `Ragdoll.enabled` (prefab may ship disabled; applier enables for Update/AlignToHips).
- Call `Recover()` from `OnDisable`.

## Play Mode smoke checklist

- [ ] Host: go unconscious → ragdoll → wake → stand-up
- [ ] Conscious critical (brain still &gt; 10%) → collapsed
- [ ] Cardiac arrest while still reporting conscious → collapsed
- [ ] Death after unconscious → stays Dead; no walk cycle
- [ ] Late-join observer sees existing corpse collapsed
- [ ] Ownership / mind-swap teardown does not stand the corpse up
- [ ] Admin `ragdoll` timed knockdown recovers without Health fighting it

## Out of scope

- Prefab strip of `Human.prefab` ([agent-first composition](../../architecture/2026-07_agent-first-composition.md) / health Phase 0d).
- Ghost controller / mind-swap redesign beyond presentation handoff.
- Full animation system redesign ([player-body-animation](2026-07_player-body-animation.md)).

## Related

- System maps: [entities](../../architecture/systems/entities.md), [health](../../architecture/systems/health.md)
- Plan context: [health_implementation_plan.md](../../plans/health_implementation_plan.md)
- TECH_DEBT §1.2 — resolved with this effort
