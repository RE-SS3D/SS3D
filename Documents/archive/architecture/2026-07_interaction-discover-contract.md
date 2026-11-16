> Implements: Documents/architecture/TECH_DEBT.md#13-interaction-discover-has-no-contract
> Touches systems: interactions framework, interactions runtime, inventory, health, combat
> Status: shipped

# Interaction Discover contract (Jul 2026)

## Goal

Lock a Discover contract so source extensions and consumers share one meaning for candidate
entries, distinguish source-only from target-bound interactions at the type level, and stop
using `Vector3.zero` as an “unset point” sentinel (TECH_DEBT 1.3).

## Contract

| Kind | Rule |
|---|---|
| **Target-bound candidate** | `Target != null`. Add only when the interaction is structurally relevant to that target (type/shape and/or `CanInteract`). Never unconditional Add for every hover. |
| **Source-only candidate** | `Target == null`, wire index `SourceOnlyTargetIndex`. Add **once** per discover (e.g. Drop while holding). Not “doable to this hover.” |
| **Viability** | `FilterAndSort` is the sole full gate for menus/RPC (`CanInteract` + intent + `CanExecute`). Discover may pre-filter; it must not imply “already range-checked and menu-ready” without FilterAndSort. |
| **Outline** | Target-bound only — `TryEvaluateOutlineInteractability` (no source-only discovery). |
| **Point** | `InteractionEvent.HasPoint` is explicit. Unresolved hits use the no-point constructor; a real hit at world origin sets `HasPoint = true` with `Point = zero`. |

## Shipped

- Effort doc + XML on `IInteractionSourceExtension` / `CreateSourceInteractions` / `Discover`.
- `InteractionEvent.HasPoint`, `WithTarget` / `WithSource`; `RangeCheck` and `ZoneTargetResolver` use `HasPoint`.
- `InteractionEntry.IsSourceOnly` + `SourceOnly` factory; Drop uses the factory; `FilterForOutline` filters on `IsSourceOnly`.
- `CreateSourceInteractions` / `GetSourceInteractions` take discover `context`; health extensions use `context.WithTarget`.
- `InteractionController` builds no-point events when selection point resolve fails.
- EditMode: Discover merge, source-only, HasPoint at world origin (`InteractionPipelineTests`, `InteractionRangeCheckTests`) — 17 passed.
- Ship commit: `0499ab7cd`.

## Out of scope (still open)

- Forcing all extensions onto one gate style (cheap structural only).
- Decomposing `InteractionController` (TECH_DEBT 1.9).
- Wall-mount collider content (pickable ≠ rangeable).

## Related

- [interactions-framework.md](../../architecture/systems/interactions-framework.md)
- [interactions-runtime.md](../../architecture/systems/interactions-runtime.md)
- [TECH_DEBT.md](../../architecture/TECH_DEBT.md) §1.3 → Resolved
- Prior: [2026-07_interaction-system-hardening.md](2026-07_interaction-system-hardening.md)
