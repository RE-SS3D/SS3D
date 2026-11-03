> Code paths: Assets/Scripts/SS3D/Systems/Comms/, Assets/Scripts/SS3D/Systems/Screens/, Assets/Content/Data/Comms/Channels/, Assets/Content/Systems/UI/Comms/
> Entry points: CommsSubSystem, LocalSpeechBubbleController, CommsFeedController, PlayerCameraSubSystem, CameraSubSystem, CameraFollow
> Status: partial
> Verified: 504c22428 — 2026-11-03

# Chat / audio / screens

## Overview

In-game comms hub, local-speech UI, non-diegetic radio/announcement feed, and camera/screen
controllers. Audio has its own map — [audio](audio.md). Camera pose is still multi-writer — see
[camera ownership](../2026-07_camera-ownership.md).

**Chat command:** `/bug <note>` is intercepted in the server's OOC handler (`CommsSubSystem.Ooc.cs`) and in-round compose forwards it to OOC; it writes `bugs.jsonl` and answers with a notice instead of posting ([rounds-lobby](rounds-lobby.md), `BugCommand`). A bare `/bug` never reaches the server: `CommsSubSystem.SendOoc` opens the bug report dialog through `BugReportRequests` ([ui-shell](ui-shell.md)).

**Comms hub:** `CommsSubSystem` owns local speech (proximity ObserversRpc) and non-positional radio /
announcements (`CommsMessage` broadcast; radio to all clients, announcements only to connections
with a spawned crew body, so lobby clients never get the round-start welcome). Legacy `ChatSubSystem` / `Engine.Chat` purged;
channel SOs live under `Assets/Content/Data/Comms/Channels/` (`CommsChannel` / `CommsChannels`).

Local speech: weighted chips + T-compose (Enter speak / Shift+Enter whisper / Ctrl+Enter shout).
**Tab / Shift+Tab** cycles Local → compose-available radio (Engineering/Security). Leading slash
prefixes (`/eng`, `/sec`, `/announce`) also route commit (prefix wins over Tab); Announcement is
prefix-only (not in Tab). Radio → `CmdSendRadio`; announce → `CmdSendAnnouncement`. Feed UI on
`UiShell` HUD: middle-left radio stack + top ALL-STATION banner.

**OOC / LOOC / Admin chat (comms.md §7):** server-attributed, rate-limited broadcasts in the
`CommsSubSystem.Ooc.cs` partial. OOC goes to everyone, Admin only to Administrators, and LOOC to
spawned players within 10 m of the sender's body. The server keeps a bounded OOC/Admin history,
replayed by id on connect. The lobby/Escape shell Chat tab is the only UI so far; there is no
HUD feed yet.

## Start here

- `CommsSubSystem.cs` — local + radio/announce hub; `SendAnnouncement` / `HandleAnnouncementRequest`;
  `TryResolveComposePrefix`; transcript `Logs/Comms.txt` on server.
- `CommsSubSystem.Ooc.cs` — `SendOoc` / `RequestOocHistory` (client), `PostOocSystem` (server); `OocChat.cs` — messages, `OocChatRules`, `OocRateLimiter`, `OocChatHistory` (EditMode-tested in `OocChatTests`).
- `LocalSpeechEmitter` — `CmdSpeak` / `CmdSendRadio` / `CmdSendAnnouncement` on the speaking Entity; `CanSpeak` reads health. All three server handlers refuse critical, unconscious, arrested or dead speakers; OOC remains separate.
- `CommsComposePrefix` — pure `/token` parse/strip (EditMode-tested).
- `LocalSpeechBubbleController` + `LocalSpeechBubbleView` — head chips + compose on
  `UiShell` `UiLayer.Overlay` (not the hub/MI UIDocument).
- `CommsFeedController` + `CommsFeedView` — attaches to `UiLayer.Hud`; radio middle-left (header +
  radio-tower icon + sender top row), announce top. Announcements always play `StationAnnounce`
  first; when it ends, the banner reveals and optional `CommsMessage.SoundId` follow-up (e.g.
  `StationWelcome`) starts in parallel.
- Channel settings: `Assets/Settings/CommsChannelsSettings.asset`.
- Screens: `PlayerCameraSubSystem`, `CameraSubSystem`, `CameraFollow` (guard `isActiveAndEnabled`).

## Extension points

- Mode-pip fading hints (comms.md §5); whisper presence-while-typing for other viewers.
- OOC/LOOC lines in the in-round `CommsFeedView` (§7 typography); mute/ban moderation; persistent history.
- PDA log / history ([comms.md](../../design/comms.md) §9).
- Channel radial; headset/ID gating (MVP2 S4) — replaces interim Tab + slash prefixes.
- More compose prefixes: set `CommsChannel.ComposePrefix` (or Radio Abbreviation fallback) +
  `AvailableInCompose` / announcement writability flags.

## Pitfalls

- **Critical characters talking:** validate `LocalSpeechEmitter.CanSpeak` inside all three server handlers, not only in the compose UI. Critical can still have `IsConscious == true`, so speech must require Healthy state as well as consciousness and no cardiac arrest.

- **Local speech on UiShell Overlay:** do not attach bubbles/compose to the hub UIDocument —
  `MachineInterfaceHost` disables it while closed, and a child `UIDocument` under the hub cannot
  set its own `PanelSettings` (Unity asserts parent panel mismatch). Use `UiLayer.Overlay`.
- **Channel pick (design deviation):** Tab cycle + slash prefixes replace design §6 radial /
  §5 “no typed prefixes” for this slice. Do **not** add SS13 `;` / `:h` aliases — slash only.
  `CommsChannel.ComposePrefix` optional; Radio defaults to lowercased Abbreviation. Unknown `/token`
  stays literal local text. Empty body after a known prefix closes compose.
- **Announcements:** all `Announcement`-kind traffic uses the top banner (no routine→feed split yet).
  Client always plays `StationAnnounce` first; missing clip falls through to immediate reveal.
  Optional `CommsMessage.SoundId` is a follow-up started with the banner (round-start:
  `StationWelcome` + welcome copy). Player `/announce` is unlocked (`StationAlerts` not CodeOnly);
  not listed in Tab. Third-party clip credit:
  [Sound ATTRIBUTIONS](../../../Assets/Art/Sound/ATTRIBUTIONS.md).
- **Headset traits** on `CommsChannel` are data-only until MVP2 gating — Tab compose only lists
  channels with `AvailableInCompose` (Engineering + Security for now).
- **Radio feed:** middle-left stack (`justify-content: center`); top row is radio-tower icon +
  channel header left + sender (ckey) right. Icon is `delapouite/radio-tower.svg` (VectorImage),
  tinted to the channel accent.
- **Do not resurrect UGUI always-on chat.**
- **Speech bubbles invisible / NaN size:** `EnsureOverlay` must require `root.panel != null` and tear
  down on disable (UIDocument rebuild orphans).
- **Compose:** `TextEntry` + `InputTextEntryScope` + TrickleDown Enter/Tab; draft focus lock; measure
  draft width via Label proxy; no USS `translate: -50%` on draft or announce banner.
- **Tab channel cycle:** while composing, Tab is edge-detected from `Keyboard.current` in
  `LateUpdate` — UITK TextField focus swallows Tab (focus navigation) so InputActions/KeyDown are
  unreliable. Also ignore `NavigationMoveEvent` on the draft. Draft chrome shows `LOCAL` /
  `ENG > OPEN` / `ALL-STATION` (prefix). Empty channel settings recover via loaded assets / Editor
  AssetDatabase.
- **Objectives hold-to-show removed:** legacy `Other/Fade` (Tab) binding erased; uGUI panel stays
  hidden until the PDA objectives tab ships.
- **UITK:** do not combine `border-radius` + `overflow: hidden` on the same element.
- **Occlusion:** shared `SS3D.Utils.LineOfSight` — no private raycast in `LocalSpeechListener`.
- **`CameraFollow` ignores `enabled = false`:** Coimbra `UpdateEvent` needs `isActiveAndEnabled` early-out.
- Radio/Announcement **head-chip** CSS is draft/preview only; live path is `CommsFeedView`.

- **OOC sender identity:** never trust a client-supplied name. The server resolves the ckey from the connection. Chat labels set `enableRichText = false`.
- **OOC history replay** may arrive after live lines. `OocChatHistory.Add` inserts by id and dedupes, so don't append blindly.

## Tests

- `Testing/ui/validate.sh --steps compile,scenarios:Speech` (`SpeechScenarios`) exercises healthy delivery, critical refusal, treatment recovery and corpse refusal for speak/whisper/shout, radio and announcements in a host session. Remote clients and OOC are outside it.

## Depends on / Used by

- **Depends on:** [health](health.md) (speech capability), [player-control](player-control.md), [inputs](inputs.md), [ui-shell](ui-shell.md)
- **Used by:** [rounds-lobby](rounds-lobby.md) (announcements), [tile](tile.md) (map editor camera),
  [interactions-runtime](interactions-runtime.md)

## Related docs

- Known design deltas: [REVIEW_2026-10 — Known delta](../../design/REVIEW_2026-10.md#known-delta-revised-design-vs-code) — rows: comms (OOC/LOOC in the in-round feed), admin-tools (two-way Admin help)
- Design: [Documents/design/comms.md](../../design/comms.md)
- Effort: [2026-07_comms-non-diegetic-feed.md](../../archive/architecture/2026-07_comms-non-diegetic-feed.md)
- Plan: [comms_non_diegetic_feed.plan.md](../../archive/plans/comms_non_diegetic_feed.plan.md)
- [2026-07_agent-first-composition](../2026-07_agent-first-composition.md)
- [2026-07_input-arbitration](../../archive/architecture/2026-07_input-arbitration.md)
- [2026-07_camera-ownership.md](../2026-07_camera-ownership.md)
