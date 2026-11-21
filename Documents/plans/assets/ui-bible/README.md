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
| `index.html` | One tab per section: overview, all elements, states, foundations, one per component (each compared against its reference screenshot), the character setup screen and the map editor (B) at 1200 × 675 reference px, the CSS → USS notes and open questions | UXML (each specimen's **uxml** button copies its tree) |
| `bible.js` | Builders and demo behaviour standing in for the C# views | Nothing; C# views |
| `bible.css` | The page's own chrome and the stand-in backdrops | Nothing |
| `references/` | The old-build screenshots each specimen is compared against (JPEG copies) | Nothing |
| `serve.py` | A no-cache server rooted at the repo, so the page can load `Assets/` | Nothing |
| `fonts/` | Disket Mono for the page. Disket Mono Bold and Kode Mono are now in the project too, for the map editor (`Assets/Art/Font/Fonts/`, baked UI font assets in `Assets/Content/Systems/UI/Font/Fonts/`); the page still loads Kode Mono from Google Fonts | `Assets/Art/Font/Fonts/` plus a baked SDF asset |

## Conventions

- Sizes are UI Toolkit reference px. PanelSettings scales from 1200 × 800 matching width, so a 1920 × 1080 screen is 1200 × 675 here and every number draws 1.6× larger in game.
- Class names reuse the shipped USS where one exists (`inventory-slot`, `hands-gear-strip`, `storage-panel`, `radial-interaction-menu`, …).
- Sprites and fonts come from `Assets/`; nothing is copied in, except the trial fonts in `fonts/`.
- Only what the reference screenshots show is built. New pieces (vitals, alerts, the full HUD) wait until these are polished.
- Every specimen built from a screenshot has a **reference** strip: side by side, or overlay (the UI see-through over the screenshot). Screenshots are pinned at their own scale (mostly ~1.87 screenshot px per UI px; each one's is in `CMP`), so size differences show; drag a reference to nudge it. **low res** blurs the UI by each screenshot's own measured softness, and side by side puts the specimen on the screenshot's floor tone, so both halves are judged alike. The pins, scales, blur and floor tones live in `CMP` in `bible.js`.
- The **states** tab draws every element in each of its states. Hover, pressed and focus are forced through twins of the real `:hover` / `:active` / `:focus` rules (`.is-hover`, `.is-pressed`, `.is-focus`), so they draw exactly like the live ones.
- Tokens are edited in `tokens.css`; there is no live tweak panel.

The page is tabbed: one section at a time, named by the URL hash (`#strip`, `#setup`, …), so a link opens a tab and back/forward step between them. The **open questions** tab lists where the references, the design docs and the shipped HUD disagree.

## In the game

The **map editor** tab is built in UI Toolkit: `Assets/Content/Systems/UI/MapEditor/MapEditor.uss` over `Assets/Content/Systems/UI/Tokens/ui-tokens.uss` (this folder's `tokens.css` under the same names; the glyph alphas become tint colours there), with the layout in `MapEditorView` ([tile](../../../architecture/systems/tile.md) map). Change a token or rule here first, then copy it across. The rest of the bible (HUD, character setup) is not ported yet.
