# Lobby and character window — Claude Design brief

> Generated 2026-11-14 from `Documents/design/lobby.md` (§2–§8), `round-end.md` §6, `comms.md` §7 and `Assets/Content/Systems/UI/Tokens/ss3d-tokens.uss`. Regenerate rather than edit once those move. Current build state: [rounds-lobby](../architecture/systems/rounds-lobby.md).

Paste everything under **Prompt** into Claude Design. Bring the result back as static mockups; the build turns them into UI Toolkit layouts against the tokens. The implementation grill that follows decides HOW; this brief only fixes WHAT is on screen.

## Prompt

You are redesigning two full-screen menus for a 3D multiplayer space-station game in the tradition of Space Station 13 / Space Station 14: a social sandbox round of 8–40 players, each with a job on the crew, some secretly antagonists. The two menus are the **lobby** (what a player sees when they're not in a body: before a round, after it, and as the Escape menu during one) and the **character window** (where a player builds the characters they play). Produce static mockups at 1920×1080, 100% scale, plus the lobby and the Jobs tab again at 1366×768 to show they still fit.

### What these screens are for

The lobby is a meta screen, not an in-fiction one, and it should look like a native part of this game rather than an embedded web page (SS13's lobby literally is one). Its job is short: tell the player where they are and what's being played, let them pick a character, and get them into the round. Dense is fine. There's no world in play behind it to compete with. But every control must do something real. Don't add placeholder features, decorative stats, or controls for things the brief doesn't list.

**The character is the unit.** A player keeps up to about ten characters. Each one holds a name, a look, its job priorities and its antagonist opt-ins. One is selected at a time, and that one is who the player spawns as.

### Visual language (fixed — do not redesign)

- Flat surfaces, hairline 1px borders, small radii (2–4px). No neon, no glow, no glitch effects, no sci-fi HUD decoration. Muted status colours used only for real meaning.
- Sans for UI chrome, monospace for data readouts (timers, slot counts, build ids).
- The UI engine has **no box-shadow, no gradients, no backdrop blur and no text shadows**. Depth comes from surface tone and borders only. Text inputs, dropdowns and image frames are **square-cornered** (rounded ones render wrong on one platform).
- Tokens:
  - Surfaces: void `#0a0b0d`, panel `#171a1e`, HUD `#161616`, elevated `#23262b`
  - Text: primary `#e8e6e3`, secondary `#9a9690`, tertiary `#5c5955`
  - Borders: subtle `#2a2d35`, strong `#3d4149`
  - Accent: rust `#c54f30` (hover `#d45f40`, dim `#8a3822`)
  - Feedback: success `#4a8f5c`, warning `#c9a227`, danger `#b94848`, info `#4a7a9f` (each also has a 15% background and a 40% border variant)
  - Spacing on a 4px grid; motion 80 / 160 / 400 ms.
- The current lobby uses its own greyer panels and pure-white text. Replace those with the tokens above, so the lobby and the in-game HUD read as one game.
- **Department colours** (Command, Security, Engineering, Medical, Science, Cargo, Service, Civilian): use the set from the examine and feedback brief if one exists. Otherwise propose eight muted colours that sit with these tokens, stay distinguishable from the feedback colours and from each other, and pass contrast on the panel surface.
- **Priority colours** for the four job priorities (High, Medium, Low, Never): propose these too. High must stand out; Never must recede.

### The backdrop

Behind the lobby the game shows the live 3D station, blurred and darkened by the engine. Mock it as a dark, heavily blurred image of a dim station interior. The menu panels sit on top of it and must be opaque enough to read on any part of that image.

### The character preview

The character is a live 3D render delivered as an **opaque rectangular image**. It can't be cut out, overlaid with transparency, or bled into the panels around it. Give it a frame. It supports: rotate left / right (front, side, back) and a **show clothing** toggle. It wears the clothes of the character's High job, or Assistant clothes when nothing is High. The same render appears small in the lobby sidebar and large in the character window.

### Screen 1 — Lobby, before the round

**Header strip:** server name; map; mode as plain text (a secret mode reads "Secret"); player count; round state.

**Sidebar** (one side, full height):
- **Round state and timer.** Before the round: a pre-round countdown, e.g. "Round starts in 02:41", that runs once enough players are connected. Show also the waiting state, "Waiting for players — 3/5 connected", and the admin-held state, "Countdown held by an admin".
- **Selected character:** the small preview, the name, and their High job with its department colour ("High: Engineer"). Clicking it opens the character window. It needs a clear affordance that it's clickable, and a quick way to switch to another character.
- **Ready** (a toggle, the most prominent control on screen) and **Observe** (enter the round as a ghost without a body). Ready and Observe are mutually exclusive.
- At the bottom, quiet: **Report a bug (F8)** and **Quit to menu** (two-click confirm).

**Tabs** (main area):
- **Server Info**, where a player lands. Server name, message of the day (a few paragraphs, may contain links), map and mode as plain text, server build and uptime, and a change log of recent changes grouped by release (title rows that link out).
- **Chat**: the out-of-character channels OOC / LOOC / Admin help, as a log with timestamps and a single-line input.
- **Settings**: display, audio and controls sub-tabs. These exist already. Just show that the tab fits the shell; you don't need to design its contents.
- **Last Round**: the previous round's summary. It exists already. Show the tab only.
- **Server Settings**: admin only, behind an Admin toggle. Show the tab only.

There is deliberately **no player list** and **no job list** in the lobby. Jobs live in the character window.

### Screen 2 — Lobby, during a round (Escape menu)

The same shell over the blurred station. The round state shows the elapsed round time. The Ready button becomes **Join** (opens Screen 7). If the player already has a body in the round, the button shows **In round** (disabled). If an admin has turned off late joining, Join is disabled and says why. Observe is still available.

### Screen 3 — Character window: Appearance tab

One window with three tabs, **Appearance / Jobs / Antags**, with the large preview always visible beside them. There's no step-by-step wizard and no review page. A top bar holds the character switcher (Screen 6) and **Return to lobby**. Unsaved changes need a visible **Save** and a way to discard them.

Appearance contains:
- **Name**: one text field, "First Last", with a **Randomize name** button. Show a validation error ("Use a first and last name").
- **Body**: six sliders: Height, Female, Fat, Muscle, Chest, Jaw. Plus **Skin tone** and **Eye colour**, each a row of about 8 swatches.
- **Style**: Hair, Facial hair, Eyebrows. Each is a grid of style tiles (a small thumbnail plus a name, including "None"), with its own colour row (about 8 swatches) underneath.
- **Randomize appearance**, for the whole look.

There's no species, age, pronouns, voice or markings. Don't add fields.

### Screen 4 — Character window: Jobs tab

Jobs grouped by department, each group headed with its department name, colour and icon. Show a full roster to stress the layout: Command (Captain, Head of Personnel), Security (Head of Security, Security Officer, Detective), Engineering (Chief Engineer, Engineer, Atmospheric Technician), Medical (Chief Medical Officer, Doctor, Chemist), Science (Research Director, Scientist), Cargo (Quartermaster, Cargo Technician), Service (Bartender, Chef, Janitor), Civilian (Assistant). Then show the same tab with the five-job playtest roster: Captain, Security Officer, Engineer, Doctor, Assistant.

Each job row: icon, name, slots as open/total in monospace ("2/4"; Assistant is "unlimited"), and a **priority control** with High / Medium / Low / Never. Only one job can be High at a time; setting a new High drops the old one to Medium. Show that happening.

Each job has a **short description**, on hover or expand (two or three sentences). Show one expanded.

States to show:
- a **full** job (0 open)
- a **locked** job, greyed but still visible, with its real requirement: "Requires 10 hours as Engineer"
- a character with **no preferences** (everything Never), with a line making clear that's valid: "You'll be assigned wherever you're needed"

At the bottom: **If none of my jobs is available:** a choice between *Become Assistant* (default) and *Return to lobby*.

There's no indicator of how many other players want each job. Don't add one.

### Screen 5 — Character window: Antags tab

Visually distinct from the Jobs tab. This is an eligibility toggle, not a job. A list of antagonist roles, today only **Nuclear Operative**, each with an on/off control and a one-line description: "A member of a Syndicate strike team sent to destroy the station with a nuclear device." Opt-ins are **off by default**. A note says these roles are only drawn if their gamemode is picked, and that the lobby never reveals which mode will run. Show a two-role version too, to prove the list scales.

### Screen 6 — Character switcher

Opened from the character window's top bar and from the lobby sidebar. It lists the player's characters, each with a small portrait, name and High job, and the selected one marked. Actions: select, **New character**, **Duplicate**, **Delete** (with a confirmation step naming the character). Show a list of 3 and a full list at the cap of 10 (New disabled, with the reason).

Also show the **first-connect** case: a brand-new player already has one character with a random name, and nothing blocks them from readying up.

### Screen 7 — Join a round in progress

Opened from Join (Screen 2). On one side, the jobs with open slots, each with its department colour, open/total count, and description on hover. Clicking one and confirming spawns the player at once as their selected character (shown, with a way to change it). On the other side, the **crew manifest**: everyone already aboard, grouped by department, name plus job. The point is to let the player see that the station is short a Doctor. Show a state where only two jobs are open, and the empty state where only Assistant is open.

### Deliverables

- Screens 1–7 as listed, at 1920×1080, with their named states.
- Screens 1 and 4 again at 1366×768.
- A one-page token sheet with your department and priority colour proposals, contrast-checked on the panel surface.
- Brief annotations where a layout decision isn't obvious (what collapses at 1366×768, where focus starts, what the keyboard does).
