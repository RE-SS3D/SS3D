# Examine and feedback — Claude Design brief

> Generated 2026-11-09 from `Documents/design/examine.md`, `action-messages.md`, `comms.md` §7, `main-hud.md` §1/§4 and `Assets/Content/Systems/UI/Tokens/ss3d-tokens.uss`. Regenerate rather than edit once those move. Serves [2026-11_examine-and-action-feedback.md](../architecture/2026-11_examine-and-action-feedback.md) and [ooc-column.plan.md](ooc-column.plan.md).

Paste everything under **Prompt** into Claude Design. Bring the result back as static mockups. The build turns them into UXML/USS against the tokens.

## Prompt

You are designing six small HUD surfaces for a 3D, three-quarter-view space-station game: a multiplayer social sandbox where crew, traitors and security read each other at a glance. The game runs at 1920×1080 and up, over a dark, often dimly lit 3D scene with coloured emergency lighting. Produce static mockups for each surface over a representative dark scene background, at 100% scale, plus one composite screen showing them together with the existing HUD.

### Visual language (fixed — do not redesign)

- Flat surfaces, hairline 1px borders, small radii (2–4px). No neon, no glow, no glitch effects, no HUD-as-decoration. Muted status colours reserved for real meaning.
- Sans for UI chrome, monospace for data-flavoured readouts (counts, timers, levels).
- No drop shadows on text (the UI engine can't do box-shadow); legibility over bright backgrounds must come from the surface itself (a translucent panel) or the text weight.
- Tokens:
  - Surfaces: void `#0a0b0d`, panel `#171a1e`, HUD `#161616`, elevated `#23262b`
  - Text: primary `#e8e6e3`, secondary `#9a9690`, tertiary `#5c5955`
  - Borders: subtle `#2a2d35`, strong `#3d4149`
  - Accent: rust `#c54f30`
  - Feedback: success `#4a8f5c`, warning `#c9a227`, danger `#b94848`, info `#4a7a9f` (each also has a 15% background and a 40% border variant)
  - Spacing on a 4px grid; motion 80 / 160 / 400 ms.

### Existing HUD layout (keep clear of these)

Top-left vitals cluster (a small card: condition read plus four thin bars). Top-right alert icon stack. Centre: aim reticle with a small zone-label chip ("chest"). Bottom-left in-character comms feed. Bottom-centre hands and gear strip. Bottom-right intent module. **The middle of the right edge is now free** for surface 5.

### Surface 1 — Hover tooltip

A tiny label near the cursor naming what's under it: "Wrench", "Ana Ruiz", "an unknown crew member in an engineering jumpsuit". Appears instantly, gone when the cursor moves. Must read over any background and never cover the thing it names. Show three cases: an item, a named character, an unnamed character (long descriptor — show how it truncates).

### Surface 2 — Examine panel (held key)

Appears while a key is held, in the same screen region and grammar as the reticle's zone-label chip, and clears the instant the key is released: no close button, no chrome to dismiss. Content is a title line plus at most about five short lines; never a paragraph. A long list truncates with a count ("5× Advanced Medkit, 3× Trauma Kit, +2 more").

Show these examples:

- **Crate, far:** "Medical resupply crate" / "Unlocked"
- **Crate, close:** title / "Contents: 5× Advanced Medkit, 3× Trauma Kit, +2 more" / "Unlocked"
- **Gun, close:** "M4 carbine" / "Rounds: 12" / size class "Normal"
- **Character:** "Ana Ruiz" / "Engineer" in her department colour / "Wearing: insulated gloves, tool belt" / "Holding: wrench"
- **Injured character:** "is unconscious." / "**Bleeding** heavily; badly **burned** left arm"
- **Nuclear device, close:** "Nuclear fission device" / "**Armed**" / countdown "01:30" in monospace
- **Self readout:** "Your left arm is badly bruised. You feel light-headed." / "You are presenting: Marcus Voss, Engineer"

### Keyword colours (applies to surfaces 2 and 3)

Four fixed meanings, coloured on the *word*, never the whole line:

| Meaning | Token | Words |
|---|---|---|
| Fine, working | success green | powered, intact, sealed, healthy |
| Degraded | warning amber | damaged, low, panel open, unpowered |
| Danger | danger red | breached, armed, bleeding, dead, broken |
| Job on an ID | department colour | Engineer, Security, Medical… |

Locked/unlocked stays neutral. The word must carry the meaning without colour, so show the panel in greyscale too and confirm it still reads. **Propose department colours** (Command, Security, Engineering, Medical, Science, Cargo, Service, Civilian) that sit with these tokens, stay distinguishable from the three feedback colours and from each other, and pass contrast on the panel surface.

### Surface 3 — Action chip and sound line

A short line anchored above a character in the 3D world when they do something the animation alone doesn't explain: "Marcus Voss flicks the light switch", "someone in a red hardsuit plants something on the door", "Marcus Voss looks you over". It sits in the same place speech subtitles use, and **must never be mistaken for speech**. Speech subtitles are already: a flat chip near the head, hairline border, primary text, fading a few seconds after the line. Design action chips to be smaller, italic and quicker to fade, plus a variant for actions done *to the viewer* (slightly more emphasis) and one for hostile actions.

Show a crowded moment: two speech subtitles and two action chips near three heads, with a "+2 more nearby" overflow chip.

A **sound line** ("You hear glass shatter") has no visible source; it appears at the screen edge in the sound's direction. Show it on the left edge and the top edge.

### Surface 4 — Self readout from the vitals cluster

Press-and-hold on the top-left vitals cluster shows the self readout (the same content as the surface 2 self example) attached to the cluster while held. Show the cluster idle, and with the readout open.

### Surface 5 — Out-of-character column (right edge, middle)

Out-of-character chat — OOC (server-wide), LOOC (local), DEAD (ghosts), ADMIN — lives here, never in the bottom-left feed, so *where* a line appears tells you it isn't happening in the game world. It must be unmistakably distinct from every in-world surface (surfaces 1–4, the feed, speech subtitles) without being loud.

- **Idle:** empty. A new line appears, then fades after a few seconds. Show: empty; one fresh line; three lines mid-fade.
- **Recalled:** a key or hover shows the recent history as a short scrollable column. Show it.
- **Each line:** a short channel tag plus colour (`OOC`, `LOOC`, `DEAD`, `ADMIN`), then the name: account name for OOC and admin ("henk"), "Character (account)" for LOOC ("Marcus Voss (henk)"). Propose four channel colours that don't collide with the keyword palette or the department colours.
- **Persistent admin message:** a message from an admin stays until dismissed (show the dismiss affordance) and must be impossible to miss while staying in the visual language.
- **System notice:** neutral grey ("Round restarting in 60 seconds", "Bug report sent").

### Surface 6 — Compose box tinted by channel

The existing compose input appears on demand at the bottom-left with a channel tag; Tab cycles channels. Before sending, the box must make "you are typing in OOC" unmistakable. Show it in: local speech (default), Engineering radio, OOC and LOOC, using the surface 5 channel colours for the out-of-character states.

### Deliverables

1. Each surface's states as listed, at 100% on a dark scene.
2. One composite 1920×1080 screen with every surface in place over the existing HUD.
3. A token sheet: department colours, the four out-of-character channel colours, the action chip and sound line styles, and the fade timings — expressed as additions to the tokens above, not replacements.
