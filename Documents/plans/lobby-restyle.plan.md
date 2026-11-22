---
name: Lobby restyle
overview: The lobby redrawn in the main menu's look with the shipped lobby's colour coding, prototyped as a UI bible tab first, then ported to Lobby.uss, LobbyJoin.uss and CharacterSwitcher.uss on ui-tokens.uss; behaviour unchanged
todos:
  - id: bible-tab
    content: "UI bible: a 'lobby' screen tab at 1200 × 675 over a blurred station render, with every state LobbyView shows (round states, ready, late join off, Escape menu, Join picker and its notices, switcher and delete, every tab, the unsaved-map Stop prompt), colour-coded by department, round state, chat channel and admin. Owner iterates on it before any Unity work"
    status: in_progress
  - id: layout-b
    content: "UI bible: layout B, the character stage (owner, 2026-11-21): a 264 px sidebar, a 360 px panel on the right, the character large between them; Settings, Last Round and Server Settings widen the panel over the stage. Chosen over A the same day"
    status: completed
  - id: shell-tokens
    content: "ui-tokens.uss gains the --ui-dept-* tokens; Lobby.uss imports it and ui-controls.uss (UiSelect). --ui-float is opaque in game (linear blending let the UI read through). Done 2026-11-21"
    status: completed
  - id: lobby-uss
    content: "Rewrite Lobby.uss, LobbyJoin.uss and CharacterSwitcher.uss on --ui-* tokens and the baked DisketMonoBoldSDF / KodeMonoSDF assets; keep every element name (LobbyUiPilot and LobbyLifecyclePilot query them). The compact rules went: the root scales to the screen (UiReferenceScale). The character window imported frozen copies of the old sheets until its own port (character-window) retired them. Done 2026-11-21, in layout B"
    status: completed
  - id: view-classes
    content: "C#: LobbyTone gains Info for the running countdown (LobbySidebarModel and LobbySidebarModelTests); the --tone and --dept classes on the round section, admin card, character card, switcher rows, job items and Join; lobby__switch--open and its notch while the switcher is up; lobby__ready--disabled for the off states; the chat composer's channel class; Force Start, Cancel Countdown and Hold Summary as --warn, Stop Round as --danger. UXML: Observe's labels in lobby__observe-text, the music Next button as an icon. Done 2026-11-21, with layout B's stage, stacked header, wide tabs, the switcher opening above (ToggleAbove), UiSelect for Map and Mode, and the booth clearing to transparent"
    status: completed
  - id: character-window
    content: "The bible's character tab in game: CharacterWindow.uxml / .uss rewritten (imports Lobby.uss), SquareDealSDF baked (Kode Mono fallback), seven tabs with the appearance split into five panes, Front / Side / Back plus drag to turn, the job outfit preview as a UiSelect (High job, naked, any job), the switcher under the title's dropdown, UiReferenceScale (UI scale shrinks only). The booth renders unlit, on the stage and in the window (owner, 2026-11-21). LobbyClassic.uss, CharacterSwitcherClassic.uss and LobbyTokens.uss deleted. Tests and pilots updated; LobbyUiPilot passed (143 checks). Done 2026-11-21"
    status: completed
  - id: round-end-tab
    content: "Last Round on the bible's parts: RoundEnd.uss is shared with the in-round summary overlay, so this waits for the owner's call (open question 2)"
    status: pending
  - id: captures
    content: "LobbyUiPilot captures of every state at 1920 × 1080 for review. Ad hoc captures done 2026-11-21 (pre-round, chat, settings, switcher, server settings, in progress, Join picker); the pilot's layout checks are updated for layout B but not yet run"
    status: in_progress
  - id: polish
    content: "Owner, 2026-11-21: clearer colours, more movement, less samey, across the lobby and the character window (and the main menu, Settings). Soft fills for state areas, brighter department colours, three button weights, UiMotion entrances and staggers, the stage glow and Ready breathing; the preview booth unlit for its own camera only (it had reached the round through a global volume). Done 2026-11-21"
    status: completed
  - id: docs-sync
    content: "update-system-docs: rounds-lobby.md (look, fonts, colour coding, compact layout gone), the ui-bible README's 'In the game'. Done 2026-11-21"
    status: completed
isProject: false
---

# Lobby restyle

Follows [main-menu-restyle](main-menu-restyle.plan.md): the lobby in the same look (floating plates, Disket Mono Bold labels, Kode Mono text, light tints on near-black plates, the white plate for the call to action), so the screen after the menu is the same family. Layout, flow, text and networking stay as they are; this is a USS rewrite plus small view changes. The character window (opened from the card) is not in this pass.

## Starting state

See [rounds-lobby](../architecture/systems/rounds-lobby.md). `LobbyUiSubSystem` hosts `Lobby.uxml` on the UiShell's Modal layer: a 400 px sidebar (round section, character card, Switch character, Ready / Join, Observe, music, Report a bug, Quit) and a panel with a header band, five tabs (Server Info, Chat, Settings, Last Round, Server Settings for admins) and their views. The Join picker and the character switcher open over it. The palette is `LobbyTokens.uss` (`--lb-*`, rust accent, department and priority pastels); type is Gurajada, Liberation Sans and Spessman as raw TTFs.

The shell panel (`HudOverlayPanelSettings`) is constant pixel size: 1 px is 1 screen px at 96 dpi, times the UI scale setting, so it is 1920 px wide at 1080p and `LobbyView`'s compact switch (below 1600) is for 1366 × 768. Captures of the shipped lobby are in the bible (`references/old/`), behind the tab's **design → old** switch.

## Decisions (proposed)

- **Bible first,** with the shipped class names, so the port is a rewrite; the tab's "what changed" table ([lobby](assets/ui-bible/index.html#lobby)) is the part-by-part spec.
- **The main menu's parts, the shipped lobby's sidebar.** After the owner's second review (2026-11-21) the sidebar runs the screen's full height from its left edge, as the shipped one, with an edge on its right and a rule across it under the logo; the panel is the main menu's floating plate. The main menu's sidebar stays a floating plate (the owner's call, 2026-11-21), so the column moves between menu and lobby. Tabs are the menu's tab plates in a row (white when open); Settings is the main menu's `PlayerSettingsPanel` without its header, its sub-tabs under the lobby's plates.
- **Colour coding, after the owner's review of the first draft** ("needs to be more colour coded, like the one we use now"). As the map editor codes tools and categories, the lobby codes four things, with the shipped lobby's colours, each in fixed places:
  - departments (`--ui-dept-*`): a 3 px left edge on whatever belongs to one (a job, a manifest group, a character by its High job, a roster line), its glyph and its name; a picked job outlines in it and Join fills with it;
  - the round's state: info while counting down, warn loading, starting, held or over, ok in progress, on the round section's edge, dot and word;
  - chat channels: OOC white, LOOC info, Admin warn, on tags, the chosen channel and the field's edge;
  - admin: warn, on the badge, the switch and admin lines.
  Ready, once ready, is the ok plate. The character card is a lighter plate than the round and the rows, with a faint edge, so it stands out (second review).
- **No rust.** Ready up, Join and Start Round are the white plate, as Join on the main menu (open question 1).
- **Layout B, chosen over A (2026-11-21):** the owner asked for a narrow sidebar, a narrow server panel and the character model large in the middle, about 40 to 50 % of the width. The bible opens on it (**layout** still shows A); its section in the tab lists the parts. After review the name plate is compact and the switch bar small and near the screen's bottom. In game the stage shows the preview booth's live render texture, as the card does today, so the port reuses `LobbyCharacterPreview`.
- **Out of scope:** the uGUI intro path, lobby behaviour, and the round-end overlay unless the owner takes Last Round into this pass. The character window was out of scope at first; the owner took it in on 2026-11-21 (question 3).

## Open questions

0. **Scale (settled by the main menu's port, 2026-11-21).** The lobby's root goes through `UiReferenceScale` as the menu's does: laid out at 1200 × 675 and scaled to the screen, times the UI scale, so the USS keeps the bible's numbers. Until then the restyled Settings tab draws at 1:1 in the old lobby.
1. **The call to action:** keep Ready up, Join and Start Round white (the main menu's primary), or give the primary its own colour, as the shipped rust?
2. **Last Round and the overlay:** the tab is `RoundEnd.uss`, which also draws the in-round summary. Restyle both in this pass, or keep the arcade look until the overlay gets its own bible tab?
3. **The character window:** drawn 2026-11-21 as the bible's **character** tab (the shipped window's content in the old character setup reference's look, with the shipped window behind **design → old**); its own questions are in that tab. Decided: it keeps the reference's SquareDeal (`SquareDealSDF.asset`). Ported 2026-11-21 (todo character-window). Departures from the tab: facial hair and eyebrows keep their own colour rows (the game saves them apart; the tab had them follow the hair), and the switcher stays the lobby's popover (duplicate, delete, portraits) rather than a plain list.
4. **Layout B's open points** (B was chosen over A on 2026-11-21): how the model turns, and whether Settings, Last Round and Server Settings cover it (the tab's layout B section).
