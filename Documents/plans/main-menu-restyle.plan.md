---
name: Main menu restyle
overview: The main menu redrawn in the UI bible's look (the map editor's style), prototyped as a bible tab first, then ported to MainMenu.uss on the shared ui-tokens.uss; behaviour unchanged
todos:
  - id: bible-tab
    content: "UI bible: a 'main menu' screen tab at 1200 × 675 over a real wallpaper, with every state the menu has (home, join list with a server's details open, settings, connecting with steps, refused/connection lost, update banner, name needed); built from the menu-controls family and the map editor's surfaces. Owner iterates on it before any Unity work. Approved as drawn 2026-11-21"
    status: completed
  - id: shell-tokens
    content: "ui-tokens.uss gains --ui-info; MainMenu.uss and PlayerSettingsPanel.uss @import it, as MapEditor.uss does (not on the UiShell root yet: the other bible-only tokens wait for a surface that needs them)"
    status: completed
  - id: menu-uss
    content: "Rewrite MainMenu.uss on --ui-* tokens and the baked DisketMonoBoldSDF / KodeMonoSDF assets; drop the local --menu-* palette and the raw TTF references. Keep every element name and main-menu__hidden (MainMenuTests Dialog_* query them). Done 2026-11-21, with UiReferenceScale drawing the menu at the bible's 1200 × 675"
    status: completed
  - id: view-colours
    content: "MainMenuView.cs: move its hard-coded colours (status dots, status text, players, progress marks, hints) into USS classes; --accent on Join only while no screen is open and --selected on the open row; a notch element in each nav row; the music Next button as an icon. No change to MainMenuSubSystem (class-size ratchet, P7.5 split). Done 2026-11-21; the empty states' dashed border went with it"
    status: completed
  - id: settings-panel
    content: "PlayerSettingsPanel.uss on the same tokens and fonts (it is mounted in the menu and in the lobby's Settings / Escape tab). Done 2026-11-21. Resolution is now an in-panel select (a DropdownField's list breaks under the scaled menu). In the lobby it draws at 1:1 until the lobby port scales its root too"
    status: completed
  - id: motion-colour
    content: "Owner, 2026-11-21: hosting and joining more animated, more colour coded, with icons. The dialog's colour says what it does (--op: hosting ok, joining info, updating warn, lost or failed danger) on its top edge, title icon, progress bar and working step; it eases in; the progress bar fills and a glint runs along it; the working step spins and a done step's check pops. Sidebar rows get icons, Join an info edge and Play locally an ok one; a server's state is a coloured icon. Bible first (components.css, bible.js), then MainMenu.uxml / .uss and MainMenuView (in-place step updates, Animate). Done 2026-11-21"
    status: completed
  - id: polish
    content: "Owner, 2026-11-21 (with the lobby's): sidebar and server rows stagger in, the side panel slides, section headings with icons, server cards edged in their state's colour, the wallpaper's slow drift, Settings' tabs with icons and colours, visible hovers on the white plates. Done 2026-11-21"
    status: completed
  - id: captures
    content: "MainMenuPreviewWindow captures of every state at 1920 × 1080 for review; a Testing/ui visual step for the menu if the preview window can run headless"
    status: pending
  - id: docs-sync
    content: "update-system-docs: ui-shell-main-menu.md (look, fonts, the stale 'Settings shows the reference placeholder' line), INDEX.md coverage row for the main menu, TECH_DEBT.md settings-placeholder entry, the bible README's 'In the game'"
    status: pending
isProject: false
---

# Main menu restyle

Implements the look of `Documents/design/main-menu.md` §8 in the UI bible's style, the one the map editor shipped with (`287b25017`): Disket Mono Bold labels, Kode Mono text, dense near-black plates with the storage panel's 2 px edge, storage-cell outlines, colour only where it carries meaning. Behaviour, flow and networking stay as they are; this is a USS rewrite plus small view changes.

## Starting state

See [ui-shell-main-menu](../architecture/systems/ui-shell-main-menu.md). The menu is already UI Toolkit, hosted on the UiShell's Modal layer, shown when `Launcher.unity` loads:

- `MainMenu.uxml` / `MainMenu.uss` (`Assets/Content/Systems/UI/MainMenu/`), built by `MainMenuView`, driven by `MainMenuSubSystem`.
- A 400 px left sidebar at 97 % (logo, update banner, NAME, JOIN A SERVER in rust, PLAY LOCALLY with an Advanced port, SETTINGS, QUIT, a music row, a footer with version, Discord and the art credit). A panel to its right holds the server list or the live `PlayerSettingsPanel`. Dialogs (connecting, starting, refused, connection lost, update) are 440 px over a blurred wallpaper.
- Wallpaper: Clownin only, since 2026-11-21 (until then 40 stills and 6 videos rotated every 60 s).
- Type: Gurajada titles, Liberation Sans body, Spessman mono, all raw TTFs. Palette: its own `--menu-*` sheet, the same hex values as `LobbyTokens.uss` (`--lb-*`) and `PlayerSettingsPanel.uss` (`--sp-*`).
- `ui-tokens.uss` (the bible's tokens) is imported only by `MapEditor.uss`; it is not on the shell root.

## Decisions

- **Bible first.** As with the map editor: a bible tab the owner tunes in the browser, then the port. The tab ([main menu](assets/ui-bible/index.html#mainmenu)) uses `MainMenu.uss`'s and `PlayerSettingsPanel.uss`'s class names so the port is a rewrite, not a redesign; its "what changed" table is the part-by-part spec.
- **Same layout, new skin.** The sidebar and the panel beside it stay (§2's list maps onto it one to one, and the server list needs the width). The sidebar becomes a floating plate inset 16 px from the screen's edges, rounded like the storage panel, instead of a full-height strip.
- **Parts map onto bible components:**

| Part | Now | Restyled |
|---|---|---|
| Plates (sidebar, panel, dialogs) | `#222` at 97 %, `#161616` headers | `--ui-editor-panel` (near-black, 92 %) with `--ui-panel-edge`; headers are a Disket title over a `screen-rule`, no header band |
| Name | Inset field | `menu-field` + `menu-text-field`, Disket label |
| Nav rows | Rust Join, dark plates, Gurajada titles | A tab column, as on character setup: light tints with a Disket title and a Kode subtitle. The white plate marks where you are: the open screen's row, with the continue button's notch; on the home screen Join holds it as the default action |
| Advanced, music toggle / next | Text buttons, caret | Storage cells at 20 px (the map editor's reset-view size) with glyphs; volume is a `menu-slider` |
| Server rows | Rows with coloured dots and text | Tints separated by spacing, not outlines; the status dot and status text in `--ui-ok` / `--ui-warn` / `--ui-info` / `--ui-danger`; players and ping in Kode Mono |
| Server details | Inline fields | `menu-field`s; Remove as a secondary `menu-button` |
| Dialogs | 440 px, progress steps | The same plate; steps in Kode Mono with the active one white; Cancel / Retry as `menu-button`s (the primary keeps its notch) |
| Update banner | Blue dot, blue button | The new info colour (`--ui-info`, sky) on its edge and dot; Update and restart as the white primary |
| Footer, version | Grey sans | Kode Mono, dim |
| Scrollbars | Theme default | The map editor's thin scroller |

- **No rust.** The bible has no accent colour; selection is a white plate or a white line. Colour stays for state (server status, refusals) and news (`--ui-info`: an update, a changed setting), as on the map editor.
- **Tokens on the shell root.** `ui-tokens.uss` joins `ss3d-tokens.uss` on the UiShell root, so the menu, settings and later the lobby read one sheet. The names don't collide (`--ui-*` vs `--ss3d-*`); retiring `ss3d-tokens.uss` and the three local palettes is follow-up work.
- **Deviation from `main-hud.md` §1** ("monospace for data-flavored readouts, sans for UI chrome"): labels and text are both monospace (Disket Mono Bold, Kode Mono), as on the map editor. The owner chose these faces for the bible's look; `main-menu.md` §8 points at `main-hud.md` for the visual language, so the deviation carries over to the menu.
- **Out of scope:** the uGUI Intro path (`IntroCanvas`, `ServerConnectionScreenCanvas`), menu behaviour (public list, LAN address, quit confirm), UI sounds, and the lobby itself (its own plan once the menu settles).

## Settled with the owner (2026-11-21)

The owner approved the bible tab as drawn, which settles the questions this plan and the tab left open:

1. **The settings panel is in this pass.** It is restyled with the menu, so the lobby's Settings tab changes too ([lobby-restyle](lobby-restyle.plan.md) draws it there).
2. **The sidebar layout stays.**
3. **Titles are Disket Mono Bold,** as on the map editor; this closes the bible's titling-font question.
4. **The white plate marks where you are:** Join holds it on the home screen and gives it up while Settings is open; the open row keeps the notch; `--ui-info` (sky) is the news colour.
5. **One wallpaper, Clownin, always.** Shipped ahead of the port: the game no longer picks a random still or video, rotates every 60 s or dips to black ([ui-shell-main-menu](../architecture/systems/ui-shell-main-menu.md)). The bible draws Clownin and has no wallpaper arrows.

**Scale, settled by the port (2026-11-21):** the menu draws as the bible does. `UiReferenceScale` lays the menu's root out at 1200 × 675 and scales it to fill the screen, times the player's UI scale, so the USS keeps the bible's numbers. The lobby port will use the same helper. The earlier note follows for the record.

**Was open:** scale. The bible draws on a 1200 × 675 stage (1 px = 1.6 screen px at 1080p), but the shell panel is constant pixel size (1 px = 1 screen px at 96 dpi, times the UI scale setting). Ported number for number the new menu comes out at 1/1.6 of the drawing, smaller than today's; either the shell scales the menu and lobby by 1.6, or the USS sizes are 1.6×. The tab's **design → old** switch shows the shipped menu at its true share of the screen.

The lobby's review asked for more colour coding ([lobby-restyle](lobby-restyle.plan.md)); if the owner wants the menu to follow, its server status already codes by state and only the call to action would change.

Readability is settled: at 92 % the plates kept text readable over the old set's brightest wallpaper (TheLastBrunch), so Clownin needs no veil.
