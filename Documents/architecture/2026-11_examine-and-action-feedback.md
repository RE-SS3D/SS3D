> Implements: Documents/design/examine.md §2 (self-examine, range bands), §3 (keyword colours), §4 (live panel, examine cue), §5 (gear rule, visible injuries and states, Search visibility), §7 (target rows); Documents/design/action-messages.md §2–§6
> Touches systems: examine, interactions-runtime, interactions-framework, chat-audio-screens, inventory, localization, main-hud
> Status: planned

**Progress:** Not started. Decided in the 2026-11 examine grill. Phases 1–3 fix shipped content and serve [mvp1-nuke-ops.md](../milestones/mvp1-nuke-ops.md) M0 (identity at a glance) and M3 (examine shows the nuke state); phases 4–6 build action messages. The out-of-character column is a separate, parallel piece: [ooc-column.plan.md](../plans/ooc-column.plan.md). Visual brief for Claude Design: [examine-feedback-visual-brief.md](../plans/examine-feedback-visual-brief.md). Tests follow the [testing philosophy](2026-11_testing-philosophy.md).

# Examine and action feedback (Nov 2026)

Bring shipped examine up to the revised design, then build action messages on top of it. The two share one identity resolver (the name on a presented ID, otherwise a short look) and one first consumer, the examine cue, so they are one effort.

## Starting state (2026-11-09, `cd3c9eb1`)

- **Section model.** `ExamineSection` (`Systems/Examine/ExamineSection.cs`) is a plain string. No tone, no range band, no inline markup. `ExamineContentResolver` outputs name, description and provider sections only, so size class and stack count never show.
- **Stale panel.** `ExamineOverlaySubSystem.RefreshGenericDisplay` runs every frame while Shift is held, but non-character content comes from `GetCachedContent`, cleared only on locale change or when the target goes null or hidden. Tank fill, seal breach and broken-device lines stay stale. Character health lines rebuild per frame.
- **Range.** Text examine has no range limit. `ExamineData.DetailedImageRange` (1.5 × 2) gates only the image variant.
- **Coverage.** 319 prefabs; 168 carry an examinable. 28 `Item` prefabs have none: 12 body parts, 10 organs, M4, Wrench, ScrewdriverBlue, SteelSheet, Sunglasses, Doodad. Dynamic providers exist for ID cards, structural integrity (40 prefabs), substance containers (hardcoded English), air alarm, `BreakableDevice`, gas tank fill, suit seal. No provider for gun rounds, door/machine state, lock state, nuke, organ condition, stack count or size class.
- **Disposal tag never shows.** `DisposalTaggerInteraction` adds a bare `DisposalTag` but never `DisposalTagExaminable` (0 prefabs use it), and `DisposalTag` is a plain MonoBehaviour that does not sync.
- **Self fallback.** Shift with no target shows self-examine — the design now removes that path (`examine.md` §2).
- **Search.** `CharacterExamineContentBuilder.BuildSlots` shows all 13 slots including pockets and suit storage; Search (Shift+Click) opens on any other character at any range. Only Take is gated (`CharacterLootUtility.IsLootable`).
- **Replication.** `AttachedContainer._storedItems` is a SyncList sent to every observer, so every container's contents reach every client regardless of UI. Project-wide; see [TECH_DEBT.md](TECH_DEBT.md) §1.31.
- **No server knowledge of examine.** `ExamineSubSystem` has no RPC; examine is computed on the client from synced state.
- **No failure reasons.** `IInteraction.CanInteract` returns a bare `bool`.
- **Speech chips.** `LocalSpeechBubbleController` + `CrowdCapRanker` rank speakers by proximity per viewer under `LocalSpeechConfig.MaxVisibleBubbles`.

## Decisions

1. **Keyword colour is inline semantic markup.** Localized strings carry `<k=good>`, `<k=warn>`, `<k=danger>` or `<k=dept:<id>>` around a word. One formatter maps tags to `ss3d-tokens` colours at render (`--ss3d-feedback-success|warning|danger`, department colours from the visual brief) and emits UITK rich text. An unknown or missing tag renders as plain text, so the word still carries the meaning (`examine.md` §3). Translators move the tag with the word. Rejected: per-line tone (cannot colour one word in a sentence) and structured runs (every provider builds segments by hand).
2. **Range bands on the section.** `ExamineSection` gains `Band { Any, Close }`. Providers mark fine print `Close` per `examine.md` §2/§7. The overlay drops `Close` lines when the local player is beyond one tunable (`ExamineConfig.CloseRange`), which replaces `DetailedImageRange`. Client-side: the data is already on every client, so server enforcement buys nothing.
3. **Live while held.** While the examine key is held, the one examined target's content rebuilds on a 0.1 s throttle, reusing the visibility-poll tick. The cache serves hover only. Rejected: provider `Changed` events (ten existing providers to retrofit, and a forgotten event goes stale silently).
4. **Resolver fallback for bare items.** An `Item` with no `ExaminableBase` resolves to `Item.Name` plus the generic lines (size class from `Item._sizeClass`; stack count, close band, when `MaxStackSize > 1`). Prefabs add an examinable only for real flavour or a provider. Guarded by an EditMode test and an AssetAudit test (see Tests).
5. **Self-examine paths.** Remove the no-target fallback. Holding examine on your own model already resolves to the character and shows the self readout (Tier 1 lines plus "You are presenting: [Name], [Job]" from the same identity resolver). Press-and-hold on the vitals cluster shows the same readout while held — momentary, per `examine.md` §4; no click-toggle.
6. **Provider order** (MVP1-driven): generic size/stack lines → gun round count (close) → door/machine state (powered, bolted, panel open) → disposal tag fix → nuclear device armed/countdown, built with the M3 nuke slice → lock state, when crates and lockers have locks → organ condition (close) → restrained, when restraints exist. The crate manifest waits for cargo. Character Tier 0 already covers visible injuries, dead, unconscious and SSD; reword to the design's coarse phrasing and add keyword tags.
7. **Search: UI filtering plus server range.** While the target is conscious, pocket and suit-storage slots render as occupied or empty only; every slot reads plainly once they're dead, unconscious or restrained. The server rejects Search and Take beyond reach. This is presentation only while the SyncList leak stands: [TECH_DEBT.md](TECH_DEBT.md) §1.31 records it, and closing it is its own inventory/FishNet effort. Rejected: a filtered server snapshot for Search (pointless while the SyncList already leaks).
8. **Action messages are declarative.** An optional `INarratedInteraction` on an interaction returns a narration key and a visibility level (`Public | Close | TargetOnly`, `action-messages.md` §4). The interaction runtime emits it on a successful server-side `Start`. `ActionMessageSubSystem.Narrate(...)` (server) is the escape hatch for non-interaction events: blasts, sound lines.
9. **Recipients split by privacy.** Payload is a string key plus actor, target and item `NetworkObject` refs and the level — no formatted text on the wire. Public: the server sends to connections within sight-or-hearing range, and each client drops lines its own `VisionSubSystem.IsVisible` rejects (consistent with the screen; the map Pitfall rules out server `LineOfSight`). Close and TargetOnly are filtered on the server, because those are the lines that must not leak. Sound lines: hearing range from `comms.md` §3, sent with the source position so the client places them by direction.
10. **Identity formatting on the client.** Each client formats the actor and target with the examine identity resolver, extracted from `CharacterExamineContentBuilder` into one shared utility (ID name, otherwise a short look), so a disguise reads the same in examine and in action lines.
11. **Failure reasons are opt-in.** An optional `IExplainsFailure` lets an interaction return a localized reason when the chosen interaction is refused or fails at `Start`; the runtime shows it as a self-only line. Start with hands full, no free hand, and it won't budge. Rejected: changing `CanInteract` to `out reason` across every interaction.
12. **Examine cue is the pipeline's first consumer.** When a hold begins on another character, the client sends a rate-limited `ServerRpc` (one per target per few seconds). The server re-checks distance against `ActionMessageConfig.CueRange` and narrates TargetOnly "[examiner] looks you over". Hover never sends it.
13. **One chip controller, one cap.** Action chips are a style on the existing speech chip controller, not a second controller. `CrowdCapRanker` gains priority tiers above proximity: speech and shouts → hostile actions → actions on the viewer → routine. Action chips are smaller, italic and fade faster (exact look from the visual brief). History: action lines go to the comms feed scrollback under an Actions filter, on by default; never backfilled.

## Phases

Each phase is one PR off `develop`, run through `update-system-docs` before merge.

### Phase 1 — Section model

- [ ] `ExamineSection` gains `Band`; existing providers mark their fine print `Close` (tank level, substance contents).
- [ ] `ExamineKeywordFormatter`: `<k=…>` → UITK rich text from tokens; plain on unknown tag.
- [ ] Add tags to existing strings in the Examine string table: integrity (damaged/cracked), seal (intact/breached), tank (low), device (broken), health Tier 0.
- [ ] Move `SubstanceContainerExaminable`'s hardcoded English into the Examine table.

### Phase 2 — Refresh, fallback, self

- [ ] Throttled live rebuild while held; cache for hover only.
- [ ] Resolver fallback for bare `Item`s, with size class and stack count lines.
- [ ] Remove the no-target self fallback; vitals-cluster press-and-hold opens the self readout; "You are presenting…" line.
- [ ] Extract the identity resolver into a shared utility (Decision 10).

### Phase 3 — Content lines

- [ ] Gun round count (close) on the gun item, read from the loaded magazine.
- [ ] Door/machine state provider: powered, bolted, panel open.
- [ ] Disposal tag: the tagger adds `DisposalTagExaminable`; `DisposalTag` syncs.
- [ ] Nuclear device provider, filed against the M3 nuke slice (not built ahead of the device).
- [ ] Organ condition provider (close), via a tier-B recipe on organ prefabs.
- [ ] Lock state and restrained: follow-ups when locks and restraints exist.

### Phase 4 — Search

- [ ] Pocket and suit-storage slots occupied/empty only while the target is conscious.
- [ ] Server-side reach check on Search and Take.
- [ ] Blind take: slower than an open take, warns the target. The warning becomes a TargetOnly action line in Phase 5.

### Phase 5 — Action-message pipeline

- [ ] `ActionMessageSubSystem` on the `NetworkSystemsHub` (rebuild via the hub menu): `Narrate`, recipient split per Decision 9, client formatting per Decision 10.
- [ ] `INarratedInteraction`, emitted by the interaction runtime on successful `Start`.
- [ ] Action chip style and ranker priority tiers on `LocalSpeechBubbleController`.
- [ ] Sound lines placed by direction.
- [ ] Actions filter in the comms feed scrollback.
- [ ] Examine cue (Decision 12) and the blind-take warning as the first two consumers.

### Phase 6 — Narration and failure reasons

- [ ] `INarratedInteraction` on existing interactions that fail `action-messages.md` §2's test: light switch, consoles, take from character, restrain/inject/bandage as they exist, breaching charge.
- [ ] `IExplainsFailure` with the first common reasons.

## Tests

- **EditMode** (`SS3D.Tests.EditMode`): keyword formatter (tags, unknown tag, nested text); band filtering by distance; resolver fallback — an `Item` with no examinable resolves to its name plus size and stack lines (owner request: the cheap, traceable guard); identity resolver (bare card, PDA, card in pocket, no ID); recipient split for each visibility level; ranker priority tiers; cue rate limit.
- **AssetAudit:** every `Item` prefab resolves to a non-empty examine name; every `<k=…>` tag in the Examine and ActionMessages tables is a known tag.
- **PlayMode / multiplayer:** one `validate.sh` mppm step for the cue and a Close-level line reaching only the near client. Run targeted, per [testing philosophy](2026-11_testing-philosophy.md).

## Deviations

None from the revised design. Recorded limit: Search's hidden slots are hidden in the UI only until [TECH_DEBT.md](TECH_DEBT.md) §1.31 is paid.

## Related docs

- Maps: [examine](systems/examine.md), [chat-audio-screens](systems/chat-audio-screens.md), [interactions-runtime](systems/interactions-runtime.md), [inventory](systems/inventory.md)
- Parallel: [ooc-column.plan.md](../plans/ooc-column.plan.md); brief: [examine-feedback-visual-brief.md](../plans/examine-feedback-visual-brief.md)
- Policy: [2026-07_agent-first-composition.md](2026-07_agent-first-composition.md)
