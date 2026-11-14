> Implements: Documents/design/lobby.md §2 (job list, descriptions, slot counts), §3 (per-character priorities, fallback), §4 (per-character antag opt-in, off by default), §5 (selected character only, guaranteed outcome), §6 (characters, random first character, one tabbed window, name, preview outfit), §7 (connected-count timer, direct latejoin with manifest, immediate Observe), §8 (lobby screen, music row, banner art); Documents/design/round-end.md §6; Documents/design/observer.md §2
> Touches systems: rounds-lobby, gamemodes-roles-traits, entities, ui-shell, ui-shell-main-menu, audio, chat-audio-screens
> Status: planned

**Progress:** Decided in the 2026-11-14 lobby grill. Nothing built yet. Everything ships before Playtest 1 ([mvp1-nuke-ops.md](../milestones/mvp1-nuke-ops.md) M11) on one branch and one pull request. Phases 1–3 are non-visual and run while the canvas is recoloured. Phases 4–6 build against the recoloured canvas. Phase 7 purges. Visual brief and recolour prompt: [lobby-character-window-visual-brief.md](../plans/lobby-character-window-visual-brief.md). Tests follow the [testing philosophy](2026-11_testing-philosophy.md).

# Lobby and character window (Nov 2026)

Rebuild the lobby and the character creator to the revised [lobby.md](../design/lobby.md). In it the saved character is the unit: it carries the name, the look, the job priorities and the antag opt-ins. The round starts on a timer once enough players are connected. Joining late is a direct pick next to the crew manifest. The work also rebuilds both screens in UXML/USS to the Claude Design canvas "SS3D Lobby & Character window", recoloured to the SS3D design system.

## Starting state (2026-11-14, `de37a6ed`, develop at `e03d3fca`)

- **Views built in C#:** both screens are built in code with no UXML. `LobbyShellView.cs` is about 1,610 lines (#278 moved Settings out to the shared `PlayerSettingsPanel`). `CharacterCreatorView.cs` is about 1,190 lines and is a four-step wizard.
- **Mock data:** `LobbyMockData` and `CharacterCreatorMockData` hold real data: the skin and eye palettes, slider definitions and species. They also hold the "Marcus Voss" fallback name.
- **Job priorities and antag opt-ins:**
  - Both live in `RoleSubSystem._preferences` and `_antagonistOptIns`, keyed by ckey, in server memory only. They're sent by `JobPreferencesMessage`.
  - Antag opt-ins default to on (round-end loop Decision 22).
  - The lobby has a Jobs tab with H/M/L/N chips and an "N interested" line.
- **Characters:** `CharacterProfileStore` saves looks only, to `Lobby/characters.json` on the player's machine. The appearance is sent by `CharacterAppearanceMessage`. There's one starter slot and no delete control.
- **Round start:** the countdown waits for `MinimumReady` ready players. `NukeOpsProfile.MinimumReadyPlayers` is 4.
- **Observe:** a toggle on `ReadyPlayersSubSystem`'s observing list. Observers become ghosts at round start.
- **Late join:** resolves the H/M/L/N chips against open slots. There's no crew manifest.
- **Players tab:** fake Ping/Joined/Account-age fields and stub Kick/Ban/Mute buttons.
- **Server Settings:** three rule toggles are always disabled. Friendly Fire is drawn ticked.
- **Menu music:** `MainMenuMusic` belongs to `MainMenuSubSystem` and fades out when the game loads. `PlayerAudioSettings.MenuMusic` is the saved on/off.
- **Palette:** the lobby styles use a local palette. The main menu and `PlayerSettingsPanel` use the SS3D design-system palette.

## Decisions

1. **Characters are saved on the player's machine and carry their preferences.** The `characters.json` schema version is bumped.
   - Each character adds job priorities, antag opt-ins and a fallback (Assistant or return to lobby).
   - The client sends only the selected character's *saved* state, on connect, on selecting a character and on saving.
   - The server keeps its in-memory copy per ckey. Deviation below.
2. **Migration:**
   - Existing characters keep their look and start with every job at Never and every antag off.
   - A store holding only the untouched mock starter is replaced by a random character.
3. **Random first character:**
   - First and last names come from about 200 + 200 names we write ourselves, in a ScriptableObject next to the character data.
   - The look comes from the existing catalog: sliders, swatches and styles.
4. **Name rules, checked on both client and server:**
   - two or more words
   - letters, hyphens, apostrophes and spaces only
   - 3–32 characters
   - capitalisation is fixed automatically
   - duplicate names are allowed
   - no profanity filter
5. **One Save per character** covers all three tabs. Esc with unsaved changes asks Save / Discard / Keep editing.
6. **`ProtocolVersion` bump:** the preferences message changes shape (fallback, per-character). The server re-checks names and late-join picks.
7. **Resolution:** `JobResolver` reads the selected character's priorities. A player with no fit becomes Assistant (no slot cap) or returns to the lobby, per their character.
8. **Round start:**
   - The countdown runs once `MinimumConnected` players are connected: default 1, 4 in the `-nukeops` profile. It replaces `MinimumReady`.
   - Its length defaults to 120 s and admins can change it.
   - If fewer players are ready than the mode needs when it ends, the round falls back to Extended ([round-config.md](../design/round-config.md) §5).
   - Ready only decides who spawns.
9. **Observe is immediate**, before or during the round. It's enabled only while a station is loaded. The pre-round observing toggle goes away.
10. **Crew manifest:**
    - Lists everyone given a crew job this round, dead or alive.
    - Excludes operatives and ghosts.
    - Is sent to every client, carrying only names and crew jobs.
11. **Late join:** a direct pick from open jobs, first come first served, re-checked on the server.
12. **Slot counts:** before the round, total slots only. Open counts and "full" appear only on the Join screen.
13. **Job descriptions:** a new field on `RoleData`.
14. **Preview outfit:** the High job's worn clothing from its `RoleLoadout`, or Assistant's when nothing is High. A show-clothing toggle strips it.
15. **Menu music:** one music service that lives for the whole process, registered in `SystemsBootstrap` and not placed in a scene.
    - Both the main menu and the lobby drive it, using the same `MenuMusic` setting.
    - It plays on from the main menu into the lobby.
    - It fades when the player spawns or observes.
16. **Palette:** the SS3D design system, the same as the main menu and `PlayerSettingsPanel`.
    - Add department and priority colours to the tokens, re-checked for 4.5:1 on `#353538` and `#2a2a2e`.
    - Text on rust is `#ffffff`: the design system's `#fff8ef` reaches only about 4.4:1.
    - Fonts: Gurajada for titles, tabs and buttons, Spessman for readouts, LiberationSans for body text.
17. **Rebuild, don't restyle:**
    - The lobby shell, character window, switcher and join picker become new UXML/USS views.
    - `LobbyUiSubSystem` wiring and `CharacterPreviewBooth` stay.
    - Chat, Last Round and Server Settings move unchanged into their own view classes; Settings mounts `PlayerSettingsPanel`.
18. **Escape menu:** the lobby shell. The main-menu design's small in-round Escape menu is dropped.
19. **Small screens:** a USS class switched on window width replaces media queries for the 1366 × 768 layout (sidebar 320, preview 80 × 136, one-column departments).

## Phases

### Phase 1 — Characters carry preferences (non-visual)

- [ ] Character schema v2 in `CharacterProfileStore`: priorities, antag opt-ins, fallback; migration (Decisions 1–2)
- [ ] Add, duplicate and delete characters (cap 10); a character's name is its label
- [ ] Random name lists ScriptableObject and a random-character generator (Decision 3)
- [ ] Shared name validator used by client and server (Decision 4)
- [ ] Preferences message carries the selected character's priorities, opt-ins and fallback; `ProtocolVersion` bump (Decision 6)
- [ ] `RoleSubSystem` and `JobResolver`: fallback per character; antag opt-in defaults to off, except the `-nukeops` profile (deviation below)
- [ ] `RoleData` description field, filled for the five Playtest 1 jobs

### Phase 2 — Round flow (non-visual)

- [ ] `MinimumConnected` replaces `MinimumReady` in `ServerSettingsService`, the admin form and the profiles; 120 s default countdown (Decision 8)
- [ ] When the countdown ends below the mode's ready minimum, the round falls back to Extended, with the existing round-history log line
- [ ] Immediate Observe before the round; remove the observing toggle from `ReadyPlayersSubSystem` (Decision 9)
- [ ] Crew manifest data replicated to all clients (Decision 10)
- [ ] Late-join pick: the client requests a job, the server checks the slot and spawns (Decision 11)

### Phase 3 — Menu music service (non-visual)

- [ ] Move `MainMenuMusic` into a process-wide service in `SystemsBootstrap`; the main menu drives it as today (Decision 15)
- [ ] Keep it playing through session load into the lobby; fade on spawn or observe; hide the lobby row in the in-round Escape menu

### Phase 4 — Lobby shell (needs the recoloured canvas)

- [ ] Tokens: department and priority colours, `#ffffff` on rust, fonts (Decision 16)
- [ ] Shell UXML/USS:
  - the sidebar: wordmark, round state with waiting / countdown / held / in-round states, character card with switch, Ready, Observe, music row, Report a bug, Quit
  - the header strip
  - the tabs: Server Info with banner art, Chat, Settings, Last Round, Server Settings behind the Admin switch in the tab bar
- [ ] Move Chat, Last Round and Server Settings into their own view classes; mount `PlayerSettingsPanel` in Settings (Decision 17)
- [ ] Server Settings hides Random Events, Friendly Fire and Respawn until their systems exist
- [ ] The 1366 layout class (Decision 19); focus order and keys as annotated on the canvas
- [ ] The in-round Escape state: Join / In round / late join off

### Phase 5 — Character window

- [ ] Window UXML/USS: top bar (switcher, Saved / Discard / Save, Return to lobby), the preview column with rotation and a show-clothing toggle, and the Appearance / Jobs / Antags tabs
- [ ] Appearance: name with Randomize name and its error state; six sliders; skin and eye swatches; Hair / Facial hair / Eyebrows grids, each with a colour row; Randomize appearance
- [ ] Jobs: departments with icons; total slots only; priority radio groups with the one-High rule and its notice; expandable descriptions; locked rows; the "no preferences" line; the pinned fallback choice
- [ ] Antags: role rows with descriptions, off by default, and the mode-secrecy note
- [ ] Switcher popover (lobby sidebar and top bar): select, New, Duplicate, Delete with confirm, the full state at 10
- [ ] Preview wears the High job's outfit (Decision 14); Esc prompt for unsaved changes (Decision 5)

### Phase 6 — Join a round in progress

- [ ] Join picker: open jobs with open/total counts and descriptions, "Joining as … Change", and the crew manifest by department; live updates when the chosen job fills; the "only Assistant open" state

### Phase 7 — Purge and docs

- [ ] Delete `LobbyShellView.cs`, `CharacterCreatorView.cs`, `CharacterCreatorDraft.cs`, `LobbyMockData.cs` and `CharacterCreatorMockData.cs`, after moving their real data (palettes, slider definitions) into catalog or ScriptableObject data; move `JobPriorityUtil` out of mock data
- [ ] Delete the old Jobs and Players tab code, the "N interested" demand line, the species dropdown, the step rail and the old "loadout" naming for character slots
- [ ] Trim `LobbyShell.uss` and `CharacterCreator.uss` down to what the new views use, or replace them
- [ ] Record the dropped small Escape menu in [player settings](2026-10_player-settings.md)
- [ ] Run `update-system-docs`: the [rounds-lobby](systems/rounds-lobby.md) map (overview, start-here, pitfalls), the [gamemodes-roles-traits](systems/gamemodes-roles-traits.md) and [ui-shell-main-menu](systems/ui-shell-main-menu.md) maps, the INDEX coverage row, and M11

## Tests

- **EditMode:**
  - resolution with the per-character fallback, including the Assistant guarantee
  - the name validator and name normalisation
  - character schema migration, covering the untouched starter, an edited starter and a v1 save with several characters
  - the countdown's connected-count gating and the Extended fallback
  - the manifest leaves out operatives and ghosts
  - the server rejects a late-join pick for a full job
- **Pilots:** rewrite `LobbyUiPilot` (shell, character window, switcher and join visuals) and `LobbyLifecyclePilot`:
  - saved characters survive a restart
  - priorities reach the server after Save
  - Observe works before the round
  - the countdown starts at the connected minimum
  - review screenshots against the canvas
- **Smokes:** the nightly `nukeops-round` smoke needs `-nukeops` to keep its default opt-in and connected threshold. Re-run it once Phase 2 lands.
- **Per phase:** `Testing/ui/validate.sh --steps compile,editmode` plus the lifecycle step; add `visual` from Phase 4 on.

## Deviations

- **From [persistence-save.md](../design/persistence-save.md) §5:** characters and their preferences are saved on the player's machine, not as server-side player data. That layer doesn't exist yet. A player on a second machine starts with a random character. Move them when the layer is built.
- **From [lobby.md](../design/lobby.md) §4 (opt-in off by default):** the `-nukeops` profile keeps antag opt-ins on by default, for Playtest 1 only, so an 8-player squad draw finds operatives. Remove this after the playtest.
- **From [lobby.md](../design/lobby.md) §2 (playtime-locked jobs):** the locked row is built, but no job has a playtime requirement yet. That needs [player-accounts.md](../design/player-accounts.md) and player data.

## Related docs

- Design: [lobby.md](../design/lobby.md), [round-end.md](../design/round-end.md) §6, [observer.md](../design/observer.md) §2, [round-config.md](../design/round-config.md), [main-menu.md](../design/main-menu.md)
- Brief and recolour prompt: [lobby-character-window-visual-brief.md](../plans/lobby-character-window-visual-brief.md)
- Maps: [rounds-lobby](systems/rounds-lobby.md), [gamemodes-roles-traits](systems/gamemodes-roles-traits.md), [ui-shell](systems/ui-shell.md), [ui-shell-main-menu](systems/ui-shell-main-menu.md)
- Policy: [agent-first composition](2026-07_agent-first-composition.md), [testing philosophy](2026-11_testing-philosophy.md), [game versioning](2026-10_game-versioning.md)
- Previous lobby effort: [lobby-uitk-redesign](../archive/architecture/2026-08_lobby-uitk-redesign.md)
