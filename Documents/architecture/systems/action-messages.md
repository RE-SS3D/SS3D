> Code paths: Assets/Scripts/SS3D/Systems/ActionMessages/, Assets/Scripts/SS3D/Interactions/Interfaces/INarratedInteraction.cs
> Entry points: ActionMessageSubSystem, INarratedInteraction, InteractionNarration
> Status: partial
> Verified: 5fac0a4ed — 2026-11-11

# Action messages

## Overview

Short narrated lines for what an animation alone doesn't explain ([action-messages.md](../../design/action-messages.md)). The server picks recipients by visibility level and sends a string key plus actor, target and item `NetworkObject` refs over a TargetRpc, never formatted text. Each client formats names through [examine](examine.md)'s `PresentedIdentity`, so a disguise reads the same in examine and in action lines.

- **Public** — everyone within `ActionMessageConfig.PublicRange` (hearing range); each client drops the line if its `VisionSubSystem.IsVisible` rejects the actor.
- **Close** — filtered on the server to `CloseRange` (2.5 m).
- **TargetOnly** — the target's owner only. The actor never gets their own line.

Lines render as chips on the actor through the local speech chip controller ([chat-audio-screens](chat-audio-screens.md)): `SpeechMode.Action`, smaller, italic, `LocalSpeechConfig.ActionFadeDuration` (2.5 s), sharing the crowd cap. `CrowdCapRanker.RankByTier` orders speech, then hostile lines aimed at you, then lines aimed at you, then routine ones, then proximity. A line aimed at you with no actor sits on your own head.

Consumers: the examine cue (holding examine on someone within `CueRange`, one per pair per 5 s), the take-from-character warning (target-only), the light switch (public) and planting a breaching charge (close). Not built: sound lines, the comms feed's Actions history (the feed has no scrollback yet), failure reasons (`IExplainsFailure`). Status per phase: [2026-11_examine-and-action-feedback](../2026-11_examine-and-action-feedback.md).

## Start here

- `Systems/ActionMessages/ActionMessageSubSystem.cs` — `Narrate` (server), `RequestExamineCue` / `CmdExamineCue`, `TargetLine` → `ClientLineReceived`
- `Systems/ActionMessages/ActionMessageRecipients.cs` — the recipient split (pure, EditMode-tested)
- `Systems/ActionMessages/ActionMessageKeys.cs` — keys and English templates (`{0}` actor, `{1}` target, `{2}` item)
- `Systems/ActionMessages/ExamineCueLimiter.cs`, `ActionMessageConfig.cs`, `ActionLine.cs`, `ActionVisibility.cs`
- `Systems/ActionMessages/Editor/ActionMessageHubRecipe.cs` — puts the subsystem on `NetworkSystemsHub.prefab`
- `Interactions/Interfaces/INarratedInteraction.cs` — `INarratedInteraction`, `InteractionNarration.ServerNarrated`; raised by `InteractionSource` after a server `Start`
- `Systems/Comms/LocalSpeechBubbleController.cs` — `HandleActionLine`; styles in `LocalSpeechBubble.uss` (`comms-subtitle--action`)

(Paths under `Assets/Scripts/SS3D/`.)

## Extension points

- **Narrate an interaction:** implement `INarratedInteraction.TryGetNarration` (return false when nothing happened; failures are private, design §3). `ToggleInteraction.NarrationKey` covers simple toggles. Capture anything the effect destroys (a held item's parent) during `Start`.
- **Narrate a non-interaction event:** `SubSystems.Get<ActionMessageSubSystem>().Narrate(key, actor, target, item, level)` on the server.
- **New line:** add the key and English template to `ActionMessageKeys`, then run `ExamineIdentificationKeySetup.AddExamineFeedbackKeys` to write it into the Examine table.

## Pitfalls

- **The Interactions assembly can't reference Systems:** narration goes out through the static `InteractionNarration.ServerNarrated` event, which `ActionMessageSubSystem` subscribes to in `OnStartServer`. Raising it before the hub has started drops the line silently.
- **Don't filter Public lines with server `LineOfSight`:** it disagrees with the FOV at windows and furniture ([examine](examine.md) Pitfalls). The client's `VisionSubSystem.IsVisible` decides.
- **Lines live in the Examine table** under `action.` keys, not a separate ActionMessages table, so the Examine keyword audit covers them.

## Tests

- EditMode: `ActionMessageTests` (recipient split per level, cue rate limit, formatting, ranker tiers, toggle narration).
- PlayMode: `ExplosivesScenarios.AttachedCharge_FallsWhenItsWallIsDestroyed` (plant narration on a live host).
- Multiplayer: `Testing/ui/validate.sh --steps mppm:action-line` (a host-narrated target-only line reaches the remote client and is formatted there).
- AssetAudit: `ServerRpcAuthorityAuditTests` lists `CmdExamineCue` as Revalidated.

## Depends on / Used by

- **Depends on:** [examine](examine.md) (`PresentedIdentity`, `ExamineText`), [entities](entities.md), [interactions-framework](interactions-framework.md), vision ([rendering](rendering.md)), [core-subsystems](core-subsystems.md) (hub)
- **Used by:** [chat-audio-screens](chat-audio-screens.md) (chips), [examine](examine.md) (cue), [inventory](inventory.md) (take warning), [interactions-runtime](interactions-runtime.md)

## Related docs

- Design (read-only): [action-messages.md](../../design/action-messages.md), [examine.md](../../design/examine.md) §4–§5
- Effort: [2026-11_examine-and-action-feedback](../2026-11_examine-and-action-feedback.md) — in-progress
- Visual brief (chip and sound-line look): [examine-feedback-visual-brief.md](../../plans/examine-feedback-visual-brief.md)
