---
name: Comms non-diegetic feed
overview: Extend CommsSubSystem for radio/announcements, purge ChatSubSystem, ship left feed + top banner + Tab compose.
todos:
  - id: arch-docs
    content: Architecture effort + this plan
    status: completed
  - id: channel-model
    content: CommsChannel / CommsChannels; drop Local/Whisper/Shout SOs
    status: completed
  - id: comms-hub
    content: Non-positional broadcast; retarget Round/Entity
    status: completed
  - id: purge-chat
    content: Remove ChatSubSystem and hub registration
    status: completed
  - id: feed-ui
    content: CommsFeed on UiShell Hud
    status: completed
  - id: tab-compose
    content: Tab cycle Local+radio in compose
    status: completed
  - id: verify-docs
    content: update-system-docs
    status: completed
---

# Comms non-diegetic feed — implementation plan

See [2026-07_comms-non-diegetic-feed.md](../architecture/2026-07_comms-non-diegetic-feed.md).

## Implementation notes

- Channel assets moved to `Assets/Content/Data/Comms/Channels/`; settings `CommsChannelsSettings.asset`.
- Radio send is `LocalSpeechEmitter.CmdSendRadio` → server `CommsMessage` broadcast (clients cannot forge).
- Player announce: `CmdSendAnnouncement` → `HandleAnnouncementRequest` (`StationAlerts` not CodeOnly; prefix-only).
- Feed attaches to `UiLayer.Hud`; local-speech compose/chips on `UiShell` Overlay (not a private UIDocument).
- Compose: Tab Local↔Eng↔Sec; slash `/eng` `/sec` `/announce` (prefix wins). Radio body uses Liberation Sans.
- Announce audio: `StationAnnounce` then banner (+ `StationWelcome` follow-up at round start).
- Radio cards show tinted `delapouite/radio-tower.svg` VectorImage.
