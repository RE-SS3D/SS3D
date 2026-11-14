> Implements: Documents/design/lobby.md §2 (job list, descriptions, slot counts), §3 (per-character priorities, fallback), §4 (per-character antag opt-in, off by default), §5 (selected character only, guaranteed outcome), §6 (characters, random first character, one tabbed window, name, preview outfit), §7 (connected-count timer, direct latejoin with manifest, immediate Observe), §8 (lobby screen, music row, banner art); Documents/design/round-end.md §6; Documents/design/observer.md §2
> Touches systems: rounds-lobby, gamemodes-roles-traits, entities, ui-shell, ui-shell-main-menu, audio, chat-audio-screens
> Status: in progress

**Progress:** Decided in the 2026-11-14 lobby grill. Phases 1–5 are built (2026-11-14). The lobby shell and the character window are the new UXML views; the Join picker is Phase 6. Everything ships before Playtest 1 ([mvp1-nuke-ops.md](../milestones/mvp1-nuke-ops.md) M11) on one branch and one pull request. Phases 1–3 are non-visual and run while the canvas is recoloured. Phases 4–6 build against the recoloured canvas. Phase 7 purges. Visual brief and recolour prompt: [lobby-character-window-visual-brief.md](../plans/lobby-character-window-visual-brief.md). Tests follow the [testing philosophy](2026-11_testing-philosophy.md).

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
- **Menu music:** `MainMenuMusic` (now `MenuMusicPlayer`) belongs to `MainMenuSubSystem` and fades out when the game loads. `PlayerAudioSettings.MenuMusic` is the saved on/off.
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

- [x] Character schema v2 in `CharacterProfileStore`: priorities, antag opt-ins, fallback; migration (Decisions 1–2)
- [x] Add, duplicate and delete characters (cap 10); a character's name is its label
- [x] Random name lists ScriptableObject and a random-character generator (Decision 3)
- [x] Shared name validator used by client and server (Decision 4)
- [x] Preferences message carries the selected character's priorities, opt-ins and fallback; `ProtocolVersion` bump (Decision 6)
- [x] `RoleSubSystem` and `JobResolver`: fallback per character; antag opt-in defaults to off, except the `-nukeops` profile (deviation below)
- [x] `RoleData` description field, filled for the five Playtest 1 jobs

What Phase 1 built:

- **Data.** `CharacterPreferences` lives on each `CharacterCreatorDraft`, and `characters.json` is schema v2.
  - A v1 file is migrated on load and written back.
  - A store holding only the untouched "Marcus Voss" starter gets a random character instead (`IsUntouchedMockStarter`).
  - A first run with no file also starts with a random character, saved at once.
- **Roster.** `CharacterRoster` holds the characters and the selection: New (random), Duplicate (a deep copy placed right after its source), Delete (never the last one), cap 10.
  - The old creator uses it for New and Save. Duplicate and Delete have no UI until the Phase 5 switcher.
- **Names.** `CharacterNameRules` (in `Systems/Entities/Character`) normalises and validates names.
  - The creator refuses to save an invalid name and shows an interim error line under the field.
  - The server clears an invalid name, so the body goes by the ckey, and logs a warning.
  - `CharacterNameList` (`Assets/Content/Data/Characters/CharacterNames.asset`, 200 + 200 names) is referenced from `LobbyAssetCatalog`. `RandomCharacter` builds the look from the catalog's choices.
- **Antagonists.** A character's choice is three-way: in, out or unset. The message carries `AntagonistOptOuts` beside `AntagonistOptIns`.
  - Unset takes `ServerSettings.AntagonistOptInDefault`: off, except that the `-nukeops` profile turns it on.
  - The default is replicated as `ServerSettingsService.AntagonistOptInDefault`, so the lobby shows it.
- **Fallback.** `JobFallback` is sent as `JobPreferencesMessage.Fallback`.
  - A player left without a job whose character returns to the lobby is not spawned at round start (`ResolveRoundStartRoles` returns them) or at late join (`TryAssignLateJoinRole`).
  - They get a private OOC notice (`CommsSubSystem.SendOocNoticeTo`).
  - `ProtocolVersion` is 56.
- **Interim Jobs tab.** The old Jobs tab now edits the selected saved character's preferences in place. `LobbyUiSubSystem` saves the store and resends on every change.
  - Its chips follow the one-High rule (`CharacterPreferences.SetPriority`).
  - There's no fallback control yet, so every character stays on Assistant until Phase 5.
- **Descriptions.** `RoleData.Description` is filled for Captain, Security, Engineer, Doctor and Assistant. `RoleSubSystem.DescriptionOf` reads it from local assets, so nothing extra is replicated.

### Phase 2 — Round flow (non-visual)

- [x] `MinimumConnected` replaces `MinimumReady` in `ServerSettingsService`, the admin form and the profiles; 120 s default countdown (Decision 8)
- [x] When the countdown ends below the mode's ready minimum, the round falls back to Extended, with the existing round-history log line
- [x] Immediate Observe before the round; remove the observing toggle from `ReadyPlayersSubSystem` (Decision 9)
- [x] Crew manifest data replicated to all clients (Decision 10)
- [x] Late-join pick: the client requests a job, the server checks the slot and spawns (Decision 11)

What Phase 2 built:

- **Countdown.** `ServerSettings.MinimumConnected` (default 1) and `CountdownSeconds` (default 120, 5–3600) drive `AutoStartCountdown`, which now counts online players, not ready ones.
  - The admin form has Minimum Connected and Countdown (s) fields, idle-only like the other round fields.
  - `-sandbox` keeps a 20 s countdown (`SandboxProfile.CountdownSeconds`, the design's "short countdown").
  - `-nukeops` sets `MinimumConnected` to the same value as `MinimumReadyForNukeOps` (4), so `-nukeops-min-ready=N` lowers both. `-nukeops-countdown-seconds=N` shortens the countdown; the `nukeops-round` smoke passes 20.
- **Extended fallback.** Already built by the round-end loop: `SquadDraw.HasEnoughReady` against `MinimumReadyForNukeOps` calls `FallBackToExtended`, and round history records it as `fallbackMap`. Nothing changed.
- **Observe.** `EntitySubSystem.CmdObserve` makes a ghost at once whenever a station is loaded (`ObserveRules.StationLoaded`: WarmingUp or Ongoing, load state Ready) and takes the player off the ready list.
  - The lobby countdown runs in Stopped with no station loaded, so before the round Observe is open only during the warmup.
  - `ChangePlayerObservingMessage` and the observing list are deleted. Round start skips anyone who already has an entity, including under `AllConnectedReady`, and the server ignores Ready from them.
- **Manifest.** `RoleSubSystem.Manifest` is a replicated list of `CrewManifestEntry` (name, job). `CrewManifest` keeps one row per ckey, so a respawn updates its row.
  - Rows are written when a body gets its role loadout. Antagonist roles are left out, and observer ghosts never get a role, so they're left out too.
  - The list is cleared at Stopped. Nothing shows it until Phase 6.
- **Late-join pick.** `EntitySubSystem.CmdJoinAsJob(job)` → `RoleSubSystem.TryAssignLateJoinPick` → `JobResolver.LateJoinPickRefusal`. A full or unknown job is refused with a private OOC notice; the fallback job has no cap.
  - The interim Join Round button still resolves the character's priorities (`CmdSpawnLatePlayer`). Phase 6's picker calls `CmdJoinAsJob`.
- **Protocol.** `ProtocolVersion` stays 56: develop is on 55, and this branch ships as one pull request.

### Phase 3 — Menu music service (non-visual)

- [x] Move `MainMenuMusic` into a process-wide service in `SystemsBootstrap`; the main menu drives it as today (Decision 15)
- [x] Keep it playing through session load into the lobby; fade on spawn or observe. Hiding the lobby's music row in the in-round Escape menu moved to Phase 4, which builds the row.

What Phase 3 built:

- **Service.** `MenuMusicSubSystem` (`Systems/Audio`) is created by `SystemsBootstrap` on clients, beside the main menu. It owns the source (`MenuMusicPlayer`, formerly `MainMenuMusic`) and loads the tracks from the main-menu catalog itself, so a session started from the command line has music in the lobby too.
- **Who wants it.** `MenuMusicDemand` holds three inputs: the main menu's screen, the lobby and a handover. The music plays when any of them wants it and `PlayerAudioSettings.MenuMusic` is on.
  - The main menu reports its screen as before: menu, server list, settings and Connecting yes; Starting local session and Update no.
  - When the menu hides because a session loaded, `HandOverToLobby` keeps the music playing until the lobby reports, at most 30 s.
  - The lobby reports `connected ? !spawned : null` on each session refresh. A ghost counts as spawned, so the music fades on spawn, on observe and in the in-round Escape menu.
  - Hosting starts from the silent Starting screen, so nothing is handed over there; the lobby fades the paused track back in.
- **Setting.** The Audio row is now "Menu music", described as playing on the main menu and in the lobby. The saved field is unchanged.

### Phase 4 — Lobby shell (needs the recoloured canvas)

- [x] Tokens: department and priority colours, `#ffffff` on rust, fonts (Decision 16)
- [x] Shell UXML/USS:
  - the sidebar: wordmark, round state with waiting / countdown / held / in-round states, character card with switch, Ready, Observe, music row, Report a bug, Quit
  - the header strip
  - the tabs: Server Info with banner art, Chat, Settings, Last Round, Server Settings behind the Admin switch in the tab bar
- [x] Move Chat, Last Round and Server Settings into their own view classes; mount `PlayerSettingsPanel` in Settings (Decision 17)
- [x] Server Settings hides Random Events, Friendly Fire and Respawn until their systems exist
- [x] The 1366 layout class (Decision 19); focus order and keys as annotated on the canvas
- [x] The in-round Escape state: Join / In round / late join off; the music row hidden (moved from Phase 3)

What Phase 4 built:

- **Files.** `Lobby.uxml` and `Lobby.uss` (`Content/Systems/UI/Lobby`), with the tokens in `LobbyTokens.uss` (`.lobby-theme`, imported by `Lobby.uss`; Phase 5 reuses it). `LobbyAssetCatalog.LobbyTemplate` holds the UXML; the UXML links its own stylesheet. Icons, the wordmark and the five framed banners from the canvas (`Art/Textures/Lobby/Banners`) are `url()` references in the USS, so the catalog gained only the template.
- **Views.** `LobbyView` binds the UXML by element name and hosts `LobbyServerInfoView`, `LobbyChatView`, `LobbyServerSettingsView`, `LobbyLastRoundView` and `LobbyMapStopPrompt`, each built once and swapped into the tab area. `LobbySidebarModel` is the pure text-and-state rule for the round section, Ready / Join and Observe (`LobbySidebarModelTests`).
  - `LobbyShellView` and its partials are deleted now rather than in Phase 7: nothing used them once the shell switched. The old Jobs and Players tabs went with them.
  - `LobbyShell.uss` stays: the old character creator still uses it until Phase 5.
- **Sidebar.**
  - The round section shows Pre-round / Starting / In progress / Round over. The waiting state reads "3/5 connected. The countdown starts at 5." The held state strikes the last countdown value through and says "Countdown held by an admin". With auto start off it reads "Waiting for an admin".
  - `ServerSettingsService` replicates `PublicPlayerCap` and `PublicMinimumConnected` for the header and the waiting line (protocol stays 56, see Phase 2).
  - The card shows the saved character's name and "High: Job" in the department colour (`CharacterPreferences.HighJob`), or "No job preferences".
  - Ready is a toggle (rust, then green "Ready"). During a round it's Join, "In round" with a body, or disabled with a note when late joining is off.
  - The music row drives `PlayerAudioSettings.MenuMusic`, the Music volume and `MenuMusicSubSystem.Next`, and hides once the player has a body or a ghost.
  - Quit arms for 3 s; Esc or focus loss disarms it.
- **Header and tabs.** Server name, Map, Mode, Players `online/cap`, the round status, and "Back to round (Esc)" in the in-round menu. The Admin switch shows only for admins; on adds Server Settings and opens it, off falls back to Server Info. Ctrl+Tab cycles the tabs. Focus starts on Ready, or on Observe when Ready is disabled.
- **Server Info.** One banner per round (`RoundId` mod 5, the same on every client), the MOTD, Map / Mode / Build / Uptime, and the change log grouped by release with a "Release notes" link per release.
- **Chat.** One log of every channel the player can read, tagged OOC / LOOC / ADMIN. The channel buttons pick where a line goes; Send or Enter sends.
- **1366.** `.lobby--compact` switches on below 1600 px of panel width: sidebar 320, preview 80 × 136, gutters 12, timer 36.

Interim until Phases 5–6:

- **No job editor.** Nothing edits job priorities, antag opt-ins or the fallback until the Phase 5 Jobs and Antags tabs. Saved preferences still reach the server. The lifecycle pilot sets High Security by reflection for now.
- **Switch character** opens the old creator, which has the slot list.
- **Join** still resolves the character's priorities (`CmdSpawnLatePlayer`).

Phase 5 ended the first three. Join still resolves priorities until Phase 6.

### Phase 5 — Character window

- [x] Window UXML/USS: top bar (switcher, Saved / Discard / Save, Return to lobby), the preview column with rotation and a show-clothing toggle, and the Appearance / Jobs / Antags tabs
- [x] Appearance: name with Randomize name and its error state; six sliders; skin and eye swatches; Hair / Facial hair / Eyebrows grids, each with a colour row; Randomize appearance
- [x] Jobs: departments with icons; total slots only; priority radio groups with the one-High rule and its notice; expandable descriptions; locked rows; the "no preferences" line; the pinned fallback choice
- [x] Antags: role rows with descriptions, off by default, and the mode-secrecy note
- [x] Switcher popover (lobby sidebar and top bar): select, New, Duplicate, Delete with confirm, the full state at 10
- [x] Preview wears the High job's outfit (Decision 14); Esc prompt for unsaved changes (Decision 5)

What Phase 5 built:

- **Files.** `CharacterWindow.uxml` and `CharacterWindow.uss`, and `CharacterSwitcher.uss` (`Content/Systems/UI/Lobby`).
  - The window's root also carries `.lobby`, and its stylesheet imports `Lobby.uss`, so it reuses the shell's resets, icons, tabs, switches, keycaps, prompt card and `.lobby--compact`.
  - `Lobby.uss` imports the switcher's stylesheet, so both screens have it.
  - `LobbyAssetCatalog.CharacterWindowTemplate` holds the UXML.
- **Views.**
  - `CharacterWindowView` binds the UXML and hosts the three tabs: `CharacterAppearanceTab`, `CharacterJobsTab` and `CharacterAntagsTab`.
  - `CharacterWindowModel` holds the saved character and the working copy the tabs edit, with no UI, so its rules are unit-tested. The rules are dirty, Save enabled, the name gate, the one-High notice, the all-Never notice and the outfit caption.
  - `CharacterSwitcher` is the popover. The lobby sidebar and the window's top bar each host one; `LobbyUiSubSystem` handles their Select, New, Duplicate and Delete.
  - `LobbyCharacterPreview` owns the booth, what it wears and the switcher portraits. It split out of `LobbyUiSubSystem`, which had reached the class-size cap.
  - `CharacterCreatorView` is deleted.
- **Save.** One Save covers all three tabs. It normalises the name, stores the character in the roster, writes `characters.json` and sends the look and preferences.
  - Save stays disabled while the name is invalid, with the reason as its tooltip.
  - Discard reloads the saved character.
  - Esc, Return to lobby, or a switcher action that would leave the edited character asks Save / Discard / Keep editing first. Deleting another character doesn't ask.
  - Keys: Ctrl+S saves. Ctrl+Tab cycles the tabs. Q / E rotate unless a text field has focus; ←/→ rotate with the preview focused.
- **Jobs.** The tab shows the whole station roster from the canvas's Jobs board (`JobRoster`, Systems/Roles), merged with the replicated job list. A roster job the server runs is a normal row (the game's "Security" is the roster's Security Officer). One it doesn't run is a locked row reading "Not in the game yet", with no slot count, as lobby.md §2 asks ("locked jobs stay visible"). A server job the roster lacks follows its department's roster jobs. Departments split into two columns of about half the rows each (one column at 1366). Each row has its pictogram tinted in the department colour, or the department symbol when it has none. It also has "1 slot" / "N slots" / "unlimited" and an H/M/L/N radio group (Tab reaches it, ←/→ change it).
  - A new High drops the old one to Medium. The moved row flashes and the notice names both jobs.
  - With every job at Never, a green line says what happens instead: you join as Assistant, or you stay in the lobby, depending on the fallback.
  - A chevron opens the job's description. It's hidden when the job has none.
  - The fallback radio pair is pinned under the list.
- **Antags.** One row per `AntagonistCategories` entry, which gained a description. An unset role shows the server's default (on under `-nukeops`), and the intro says so.
- **Preview outfit.** `RoleSubSystem.LoadoutEquipmentOf(job)` returns the loadout's equipment prefabs from the local role assets. The booth wears every one with a `Cloth`, for the working copy's High job or the fallback job.
  - `CharacterPreviewBooth.ApplyOutfit` now dresses any worn set, hides the parts nothing covers, applies occlusion and refits the body morphs.
  - Show clothing off strips the booth.
  - The lobby card shows the saved character dressed the same way.
  - Before the role assets are reachable (not connected), it falls back to the catalog's first preview outfit.
- **Switcher.** Rows show the head-crop portrait (or a silhouette before one is rendered), the name, "High: Job" in the department colour, and Selected. They also have Duplicate and Delete.
  - ↑/↓ move, Enter selects and closes, Delete starts the confirm, and Esc backs out.
  - At 10 characters, New and Duplicate disable and a line says to delete one first.
  - A press outside the popover closes it.
- **Icons.** Department symbols are game-icons.net SVGs already in `Art/Icons/External` (CC BY 3.0). The canvas's own department PNGs have no recorded source, so they weren't imported. `Art/Icons/Lobby/Jobs/Assistant.png` comes from SS3D-Art. A job without a pictogram uses its department symbol.

### Phase 6 — Join a round in progress

- [ ] Join picker: open jobs with open/total counts and descriptions, "Joining as … Change", and the crew manifest by department; live updates when the chosen job fills; the "only Assistant open" state

### Phase 7 — Purge and docs

- [ ] Delete `CharacterCreatorView.cs`, `CharacterCreatorDraft.cs`, `LobbyMockData.cs` and `CharacterCreatorMockData.cs`, after moving their real data (palettes, slider definitions) into catalog or ScriptableObject data; move `JobPriorityUtil` out of mock data
- [ ] Delete the "N interested" count (`JobSlotInfo.Interested`), `LobbyMockData.PlayerRow`, `LobbyAssetCatalog`'s unused banner and chevron fields, the species dropdown, the species dropdown, the step rail and the old "loadout" naming for character slots
- [ ] Delete `LobbyShell.uss` and `CharacterCreator.uss`: nothing uses them since Phase 5, but `LobbyAssetCatalog` still requires `_lobbyStyle`. Its `ChevronDown`, loadout thumbnails, department icons, placeholder and banner fields are unused too.
- [ ] The draft's `OutfitIndex` (a random preview outfit) only feeds the booth's not-connected fallback since Phase 5
- [ ] Record the dropped small Escape menu in [player settings](2026-10_player-settings.md)
- [ ] Run `update-system-docs`: the [rounds-lobby](systems/rounds-lobby.md) map (overview, start-here, pitfalls), the [gamemodes-roles-traits](systems/gamemodes-roles-traits.md) and [ui-shell-main-menu](systems/ui-shell-main-menu.md) maps, the INDEX coverage row, and M11

## Tests

- **EditMode:**
  - resolution with the per-character fallback, including the Assistant guarantee
  - the name validator and name normalisation
  - character schema migration, covering the untouched starter, an edited starter and a v1 save with several characters
  - the countdown's connected-count gating and the Extended fallback
  - menu music: the join handover, fading on spawn or observe, the handover timeout (`MenuMusicDemandTests`)
  - the manifest leaves out operatives and ghosts
  - the server rejects a late-join pick for a full job
- **Pilots:** rewrite `LobbyUiPilot` (shell, character window, switcher and join visuals) and `LobbyLifecyclePilot`:
  - saved characters survive a restart
  - priorities reach the server after Save
  - Observe works before the round
  - the countdown starts at the connected minimum
  - review screenshots against the canvas
- **Smokes:** the nightly `nukeops-round` smoke needs `-nukeops` to keep its default opt-in and connected threshold. Phase 2 added `-nukeops-countdown-seconds=20` to its conf; re-run it before the pull request.
- **Per phase:** `Testing/ui/validate.sh --steps compile,editmode` plus the lifecycle step; add `visual` from Phase 4 on.
- **Phase 4:** `LobbySidebarModelTests` (every sidebar state on the canvas) and `LobbyViewTests` (the shell binds the committed template, keeps its stylesheet, paints the sidebar, card and admin tabs). `LobbyUiPilot` and `LobbyLifecyclePilot` now target the new element names; the remote-client scenario too.
- **Phase 5:**
  - `CharacterWindowModelTests`: dirty across all three tabs, the name gate, Save normalising the name, the one-High notice, the all-Never notice per fallback, the outfit job, slot text, the column split.
  - `CharacterWindowModelTests` also covers `JobRoster.Merge`: the server's entries win, the rest lock, extras follow their department.
  - `CharacterWindowViewTests`: the window binds its template and paints the selected character, Saved / Unsaved, the invalid-name state, the unsaved prompt on Return, the Jobs tab (locked roster rows included) and the Antags tab, and the switcher's delete confirm and full state. No panel means no change events, so edits go through the model.
  - `LobbyUiPilot` drives the window, its tabs, the switcher (New, select, delete) and the unsaved prompt.
  - `LobbyLifecyclePilot` sets Security High in the Jobs tab and checks that it reaches the server after Save. That replaces the Phase 4 reflection step.

## Deviations

- **From [persistence-save.md](../design/persistence-save.md) §5:** characters and their preferences are saved on the player's machine, not as server-side player data. That layer doesn't exist yet. A player on a second machine starts with a random character. Move them when the layer is built.
- **From [lobby.md](../design/lobby.md) §4 (opt-in off by default):** the `-nukeops` profile keeps antag opt-ins on by default, for Playtest 1 only, so an 8-player squad draw finds operatives. Remove this after the playtest.
- **From [lobby.md](../design/lobby.md) §7 and the canvas (Admin help):** players can't send to Admin chat; the server accepts it only from admins, and an admin has no way to reply to one player. The Chat tab shows the Admin channel to admins only. Player-to-admin help belongs to [admin-tools.md](../design/admin-tools.md).
- **From [lobby.md](../design/lobby.md) §2 (playtime-locked jobs):** the locked row is built, but no job has a playtime requirement yet. That needs [player-accounts.md](../design/player-accounts.md) and player data.
- **From the canvas (switcher, Duplicate):** the copy keeps the original's name instead of "Name (2)". Brackets break the name rule, and two characters may share a name (Decision 4).
- **From the canvas (switcher rows):** Duplicate and Delete show on every row, dimmed until the row is hovered, focused or selected. The canvas shows them only on the hovered row; hidden buttons can't be reached with Tab.
- **From the canvas (playtest Jobs board):** the owner asked for the full roster with unavailable jobs locked, so the five-job playtest board isn't what players see; its "no preferences" line is.
- **From the canvas (style grids):** the Hair, Facial hair and Eyebrows tiles show a generic head icon and the style's name. There are no per-style thumbnails yet.

## Related docs

- Design: [lobby.md](../design/lobby.md), [round-end.md](../design/round-end.md) §6, [observer.md](../design/observer.md) §2, [round-config.md](../design/round-config.md), [main-menu.md](../design/main-menu.md)
- Brief and recolour prompt: [lobby-character-window-visual-brief.md](../plans/lobby-character-window-visual-brief.md)
- Maps: [rounds-lobby](systems/rounds-lobby.md), [gamemodes-roles-traits](systems/gamemodes-roles-traits.md), [ui-shell](systems/ui-shell.md), [ui-shell-main-menu](systems/ui-shell-main-menu.md)
- Policy: [agent-first composition](2026-07_agent-first-composition.md), [testing philosophy](2026-11_testing-philosophy.md), [game versioning](2026-10_game-versioning.md)
- Previous lobby effort: [lobby-uitk-redesign](../archive/architecture/2026-08_lobby-uitk-redesign.md)
