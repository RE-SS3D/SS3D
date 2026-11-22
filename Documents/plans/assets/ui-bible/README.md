# UI bible

An HTML/CSS prototype of SS3D's main UI, rebuilt from the old-build reference screenshots, for tuning the look in a browser before it moves to UI Toolkit.

## Open it

```
python Documents/plans/assets/ui-bible/serve.py
```

Then open `http://localhost:8642/Documents/plans/assets/ui-bible/`. In the Claude desktop app, the `ui-bible` entry in `.claude/launch.json` starts the same server. The server roots at the repo so the page can load sprites and fonts from `Assets/` by relative path, and it turns caching off so CSS edits show on reload. Opened straight from disk, the page still draws, but Chromium blocks the fonts over `file://`.

## Files

| File | Holds | Ports to |
|---|---|---|
| `tokens.css` | Every colour, size and type decision as a `--ui-*` custom property | A token USS on the UiShell root, same names |
| `components.css` | The components, written in the subset of CSS that USS also speaks; lines that can't port say `web-only` | Component USS, mostly line for line |
| `index.html` | One tab per section: overview, all elements, states, foundations, one per component (each compared against its reference screenshot), the character setup screen, the map editor (B), the main menu, the lobby and the character window at 1200 × 675 reference px, the CSS → USS notes and open questions | UXML (each specimen's **uxml** button copies its tree) |
| `bible.js` | Builders and demo behaviour standing in for the C# views | Nothing; C# views |
| `bible.css` | The page's own chrome and the stand-in backdrops | Nothing |
| `references/` | The old-build screenshots each specimen is compared against (JPEG copies); `old/` holds the shipped main menu, lobby and character window, captured in the Editor at 1920 × 1080, behind those tabs' **design → old** switch | Nothing |
| `renders/` | Stand-ins rendered in the Editor for what the page can't draw: the lobby's character model (`lobby-character.png`, the preview booth's camera with a transparent background), also the character tab's. That tab's style pictures and job icons are the game's own, from `Assets/Art/Icons/Lobby/` | Nothing; the live render texture |
| `serve.py` | A no-cache server rooted at the repo, so the page can load `Assets/` | Nothing |
| `fonts/` | Disket Mono for the page. Disket Mono Bold and Kode Mono are now in the project too, for the map editor (`Assets/Art/Font/Fonts/`, baked UI font assets in `Assets/Content/Systems/UI/Font/Fonts/`); the page still loads Kode Mono from Google Fonts | `Assets/Art/Font/Fonts/` plus a baked SDF asset |

## Conventions

- Sizes are UI Toolkit reference px. The shell's PanelSettings are constant pixel size (1 px per screen px at 96 dpi), so a screen tab (main menu, lobby) is drawn on a 1200 × 675 stage and the port scales its root to fill the screen (`UiReferenceScale`, 1.6× at 1080p). The map editor shipped its numbers as they are.
- Class names reuse the shipped USS where one exists (`inventory-slot`, `hands-gear-strip`, `storage-panel`, `radial-interaction-menu`, …).
- Sprites and fonts come from `Assets/`; nothing is copied in, except the trial fonts in `fonts/`.
- Only what the reference screenshots show is built. New pieces (vitals, alerts, the full HUD) wait until these are polished.
- Every specimen built from a screenshot has a **reference** strip: side by side, or overlay (the UI see-through over the screenshot). Screenshots are pinned at their own scale (mostly ~1.87 screenshot px per UI px; each one's is in `CMP`), so size differences show; drag a reference to nudge it. **low res** blurs the UI by each screenshot's own measured softness, and side by side puts the specimen on the screenshot's floor tone, so both halves are judged alike. The pins, scales, blur and floor tones live in `CMP` in `bible.js`.
- The **states** tab draws every element in each of its states. Hover, pressed and focus are forced through twins of the real `:hover` / `:active` / `:focus` rules (`.is-hover`, `.is-pressed`, `.is-focus`), so they draw exactly like the live ones.
- Tokens are edited in `tokens.css`; there is no live tweak panel.

The page is tabbed: one section at a time, named by the URL hash (`#strip`, `#setup`, …), so a link opens a tab and back/forward step between them. The **open questions** tab lists where the references, the design docs and the shipped HUD disagree.

## In the game

The **map editor** tab is built in UI Toolkit: `Assets/Content/Systems/UI/MapEditor/MapEditor.uss` over `Assets/Content/Systems/UI/Tokens/ui-tokens.uss` (this folder's `tokens.css` under the same names; the glyph alphas become tint colours there), with the layout in `MapEditorView` ([tile](../../../architecture/systems/tile.md) map). The **main menu** tab is `Assets/Content/Systems/UI/MainMenu/MainMenu.uss` and its Settings state `SettingsPanel/Resources/PlayerSettingsPanel.uss` ([ui-shell-main-menu](../../../architecture/systems/ui-shell-main-menu.md)). The **lobby** tab's layout B is `Assets/Content/Systems/UI/Lobby/Lobby.uss` with `LobbyJoin.uss` and `CharacterSwitcher.uss` ([rounds-lobby](../../../architecture/systems/rounds-lobby.md)); its Last Round summary is not ported. Change a token or rule here first, then copy it across. One deliberate difference: `--ui-float` is opaque in game, because UI Toolkit blends in linear space. The **character** tab is `Assets/Content/Systems/UI/Lobby/CharacterWindow.uss`, which imports `Lobby.uss`. The rest of the bible (HUD, character setup) is not ported yet. The 2026-11-21 polish (clearer state and department colours, soft fills, motion) went into the game first: `tokens.css` carries its colours; its motion lives in USS transitions and `UiMotion`, which the bible doesn't mimic beyond the main menu's dialog.
