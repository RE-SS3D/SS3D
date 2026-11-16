> Implements: Documents/design/comms.md §5 (compose), §6 (radio / non-positional), §8 (announcements)
> Touches systems: chat-audio-screens (comms), ui-shell, rounds-lobby, entities, inputs
> Status: shipped (feed + Tab/slash compose + Chat purge; headset/PDA/radial deferred)

# Comms non-diegetic feed + Tab compose (Jul 2026)

Extends [CommsSubSystem](../../architecture/systems/chat-audio-screens.md) into the single comms hub: local speech (already shipped) plus radio and station announcements. Purges the headless legacy `ChatSubSystem` (`SS3D.Engine.Chat`) after salvaging channel ScriptableObject data.

## Deviates from design

- **Channel pick:** Tab / Shift+Tab while drafting instead of hold-comms → channel radial ([comms.md](../../design/comms.md) §6). Radial deferred; recorded on the system map.
- **Slash prefixes:** interim `/eng`, `/sec`, `/announce` in T-compose (design §5 rejects typed prefixes). Prefix wins over Tab; Announcement is prefix-only (not in Tab cycle).
- **Announcements:** All `Announcement`-kind traffic uses the top-middle banner this pass (no routine→feed split yet). Player `/announce` unlocked until headset/role gating.
- **Placement:** Radio stack on the left (mock); design said bottom-left of main HUD — same family.

## Phases

### Phase 1 — Channel model under Comms — this pass

- `CommsChannel` / `CommsChannels` settings (salvaged abbr, color, flags, headset trait ref).
- Drop DistanceBased Local/Whisper/Shout channel SOs (redundant with `SpeechMode`).
- Kinds: Radio (left feed), Announcement (banner), Meta (OOC/system — not Tab-writable this pass).

### Phase 2 — Hub + purge — this pass

- `CommsMessage` FishNet broadcast; `SendRadio` / `SendAnnouncement` on `CommsSubSystem`.
- Retarget Round/Entity station alerts.
- Delete `ChatSubSystem` / `ChatMessage` / hub registration.

### Phase 3 — Feed UI + Tab/slash compose — this pass

- `CommsFeedController` on `UiLayer.Hud`: left radio cards (radio-tower icon) + top ALL-STATION banner.
- Announce SFX: always `StationAnnounce`, then banner + optional follow-up (`StationWelcome` at round start).
- T-compose: Tab cycles Local + Eng/Sec; slash prefixes `/eng` `/sec` `/announce` (prefix wins; announce not in Tab).
- Local-speech compose/chips on `UiShell` `UiLayer.Overlay`.

## Deferred (MVP2 / later)

- Headset trait / ID gating for channels
- PDA history log ([comms.md](../../design/comms.md) §9)
- Channel radial (replaces interim Tab + slash prefixes)
- OOC/LOOC/dead distinct typography
- Re-lock player `/announce` behind role/access when gating lands

## Related

- Plan: [Documents/plans/comms_non_diegetic_feed.plan.md](../plans/comms_non_diegetic_feed.plan.md)
- Design: [Documents/design/comms.md](../../design/comms.md)
- Map: [systems/chat-audio-screens.md](../../architecture/systems/chat-audio-screens.md)
