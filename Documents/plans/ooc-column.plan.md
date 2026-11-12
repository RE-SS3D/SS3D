---
name: OOC column
overview: Right-edge HUD column for OOC, LOOC, dead chat, admin and system lines; compose tinted by channel; admin OOC mute
todos:
  - id: column-view
    content: "UITK HUD column through ui-shell, fed by existing OOC client events and history replay; channel tag + colour per line; account name for OOC/admin, character + account for LOOC; fade when idle, recall key and hover"
    status: pending
  - id: setting
    content: "Player setting: column always visible (default off)"
    status: pending
  - id: persistent-line
    content: "Persistent line type with dismiss + sound, ready for admin PMs; system notices in neutral grey (bug-report confirmation, round restart)"
    status: pending
  - id: compose-tint
    content: "Compose box takes the destination channel's colour and tag; add OOC/LOOC to Tab cycle per comms.md §5; unbound 'talk OOC' action"
    status: pending
  - id: ooc-mute
    content: "Server Settings toggle: OOC on/off mid-round, persisted with server meta; server refuses OOC while muted with a notice; LOOC, dead chat and admin unaffected"
    status: pending
  - id: docs-sync
    content: "update-system-docs: chat-audio-screens map (drop 'no HUD feed yet'), main-hud map, rounds-lobby map for the setting"
    status: pending
isProject: false
---

# OOC column

Implements `Documents/design/comms.md` §7 (out-of-character column), `Documents/design/main-hud.md` §4 (right-edge zone) and `Documents/design/round-config.md` §2 (OOC mute). Parallel to [2026-11_examine-and-action-feedback.md](../architecture/2026-11_examine-and-action-feedback.md); shares nothing with it but the visual brief ([examine-feedback-visual-brief.md](examine-feedback-visual-brief.md)).

## Starting state

The server side is done ([chat-audio-screens](../architecture/systems/chat-audio-screens.md)): `CommsSubSystem.Ooc.cs` posts OOC to everyone, Admin to Administrators, LOOC within 10 m, rate-limited, with bounded history replayed on connect. The lobby/Escape Chat tab is the only UI. Admin help and admin PMs do not exist in code (`admin-tools.md` §3 is its own work).

## Decisions

- **New surface, not the feed.** The column is its own UITK view attached through [ui-shell](../architecture/systems/ui-shell.md), not a filter on the bottom-left feed, so the in-character feed and scrollback stay clean.
- **Idle-empty by default.** A line shows, then fades after a few seconds. A recall key (default-unbound until the input scheme picks one) or hovering the column shows recent history. The "always visible" setting keeps it on.
- **Persistent line type now, admin PMs later.** Build the styled persistent line with dismiss and sound; admin help wires into it when it exists.
- **Mute is server-enforced.** Refuse in `PostOocSystem` while muted, with a system notice to the sender; never a client-side hide.

## Tests

EditMode: line formatting per channel (tag, name form); fade/recall state; mute refusal in `OocChatRules`. Visual: one `validate.sh --steps visual` capture of the column with all four channels and a persistent line.
