# Lobby & Role Selection — design document

> Status: active

This doc covers everything a player sees between connecting and taking a body: the lobby screen, the character window where they build who they'll be, how job and antagonist preferences turn into an assignment, and how the round starts and is joined late. It is explicitly a session/meta screen, not an in-fiction one. That is the same honest carve-out already used for OOC (`comms.md` §7) and the respawn timer (`death-cloning-respawn.md` §6).

The yardstick is what an SS13/SS14-style round actually needs before it starts. Both games have converged on the same small set: a character you keep, a job list you rank, antag toggles, a timer, and a way in late. This doc keeps that set and cuts what doesn't serve it.

## 1. Design philosophy

**The test here isn't diegetic-vs-panel, it's native-vs-leftover.** SS13's job-select screen is rendered through BYOND's embedded HTML browser: real tables, real hover tooltips, an actual webpage inside the game window. That's the artifact this doc cuts. It isn't a rendering-fidelity problem the way the targeting doll was. The engine's only menu tool happened to be a browser, so every menu ended up looking like one. The fix is the same visual system as the rest of this project (flat surfaces, hairline borders, muted status colors, no neon) applied to full-screen menus, so they read as part of the same game.

**The character is the unit.** In SS13 and SS14 a saved character carries everything about how you want to play: name and look, which jobs you'd take, which antagonist roles you'd accept. This doc adopts that. A player keeps a few characters, picks one, and readies up. Their preferences come with them and survive a server restart. Nobody re-ranks jobs every session.

**Preferences, not a race.** SS13 has shipped both first-come-first-served job locking and preference resolution. This doc adopts preferences at round start, for the same reason chording replaced intent-cycling in the main HUD doc (§7): a live scramble rewards reflexes and being logged in at the right second, not an informed decision. Every player ranks jobs at their own pace, and all of it resolves in one fair pass when the round starts (§5). Joining late is the deliberate exception (§7).

**Show only what exists.** A control for a system that isn't there yet, like a greyed species, a rule toggle with no consumer, or a fake ping column, teaches the player that the screen lies. Locked-but-reachable things stay visible with their real requirement (§2). Unbuilt things don't appear until they're built.

## 2. Job list

The job list lives in the character window's **Jobs** tab (§6). It's grouped by department (Command, Security, Engineering, Medical, Science, Cargo, Service, Civilian). Each entry shows the job name, its department color (muted, from the existing status-color palette), its slot count, and its priority chip (§3). Before the round nobody holds a job yet, so the list shows total slots only ("Engineer · 4 slots"); open counts and "full" belong to joining late (§7), when slots really are taken. The access each job grants comes from id/access's job→access table (`id-access.md` §5). The lobby reads it; it doesn't author it.

**Every job has a short description**, on hover or expand. A new player should be able to tell what Engineer or Doctor does without opening a wiki. This is the same trap this project has flagged in comms' typed radio prefixes (§2) and crafting's freeform recipes (§1), applied to job names.

**Locked jobs stay visible, not hidden.** A playtime-gated job (e.g. "Chief Engineer — requires 10 hours as Engineer") shows greyed out with the real requirement stated, rather than disappearing from the list. This follows the same "real, visible reason" rule as armor's absorption values and the hacking interface's failure states. A new player should see the whole shape of the game, including what they can't reach yet. Playtime gating depends on stable player identity (`player-accounts.md`), so a server without it simply has no locked jobs.

**No demand indicator.** The list doesn't show how many other players want each job. SS13 and SS14 don't, and for good reason: it invites ranking against the crowd instead of stating what you want to play, and on a small server it mostly broadcasts who is going for what. Open and total slots are the information a player needs.

## 3. Job preferences

**Each job gets a priority: High, Medium, Low or Never.** A player sets it by clicking the job's priority chip (§2). Only one job can be High at a time; setting a new High moves the old one to Medium. This is SS13's and SS14's familiar scheme, and it's quicker to set than ordering a long list. The cost is ties within Medium and Low, which resolution breaks fairly (§5).

**Preferences belong to the character** (§6) and are saved with it. A player's security officer and their doctor each keep their own ranking. Switching character switches rankings. Returning after a server restart, or after a round ends (`round-end.md` §6), costs nothing.

**"No preference" is a real, explicit option.** A player can leave every job at Never and be assigned wherever needed (§5), rather than being forced to rate jobs they don't care about.

**If nothing fits**, each character says what should happen when none of its High, Medium or Low jobs has room at round start: **become Assistant** (the default) or **return to the lobby**, free to observe or join late (§7). There's no "random job" option. It puts players in roles they never asked for.

Preferences are editable at any point up until resolution (§5). There's no lock-in moment before round start, so changing your mind costs nothing.

## 4. Special/antagonist role opt-in

The character window's **Antags** tab (§6) is separate from the Jobs tab and looks different. This is an eligibility toggle, not a job, and conflating the two would misrepresent what it does.

**The list shows the antagonist roles of every gamemode the server has enabled** (`round-config.md` §3). It never shows a role from a mode that can't run, and never hints which mode will. Each role has a one-line description: enough context that a new player isn't clicking blind, not so much that it spoils the round.

**Opt-ins are off by default** and belong to the character, saved alongside its job preferences (§3). Being an antagonist is something a player chooses, not something they forgot to untick. A small playtest that needs antagonist bodies may tick them by default as a deliberate, temporary exception. That's a playtest setting, not the design.

**Opt-ins are set before the round's mode is chosen.** The mode is drawn as the countdown ends, and only players who opted into that mode's roles are considered (`antagonist-content.md` §2). Nobody gets a second chance to opt in after the draw; that would reveal it.

## 5. Resolution — one pass, not a scramble

At round start (the countdown expiring, or an admin forcing the start, `admin-tools.md` §6), every ready player's **selected character** (§6) resolves server-side in a single allocation pass: try each player's High job, then their Medium jobs, then Low, respecting slot caps. Ties within a priority are broken fairly, not by who connected first. Only the selected character is considered, so a player always knows who they'll spawn as.

**A guaranteed fallback** means nobody comes out of resolution unable to play. A player whose character found no job becomes Assistant, the one job without a slot cap, or returns to the lobby if their character says so (§3).

The exact fairness/weighting rule inside that pass, whether a random tiebreak, seniority weighting or anything more sophisticated, is a balancing decision, not designed here. What this doc commits to is the shape: the selected character's preferences in, one fair resolution pass, a guaranteed outcome out.

## 6. Characters

**A player keeps up to about ten characters.** Each holds a name, an appearance, job preferences (§3) and antagonist opt-ins (§4). One is selected at a time, and that one goes into resolution (§5). A player can add a character, duplicate one (a variant of the same look), or delete one after confirming. A character's name is its label in the list; there's no separate slot name.

**A first-time player already has a character.** On first connect they get one with a random name and a random appearance, saved as their first character. Nobody spawns as a placeholder, and nobody has to visit the character window before they can play.

**The character window is one window with tabs**, the SS14 shape: **Appearance**, **Jobs** (§2–§3) and **Antags** (§4). The character's preview is always visible beside them. There's no step-by-step wizard and no review page. The content is short, players come back to tweak it, and a wizard's step navigation would only get in the way.

**Appearance** is a name plus a look:

- **Name.** A "First Last" in-world name, which is what other characters see and what examine reports. A **Randomize** button fills in a fitting random name.
- **Body.** The body-shape sliders and skin and eye colors.
- **Style.** Hair, facial hair and eyebrows, each with its own color.
- **Randomize** also exists for the whole appearance, for a player who just wants someone new.

Fields that nothing in the game reads yet (age, voice, markings) aren't offered. Non-human species appear only once a second species exists (§1); until then there's no species control at all.

**The preview wears the High job's outfit**, or Assistant's clothes when nothing is High. A player sees what they'll look like on shift, and the character window connects visibly to the Jobs tab. A **show clothing** toggle strips it to the body for tuning the look.

## 7. Round start & latejoin

**The round starts itself, on a timer.** Once enough players are connected, a fixed pre-round countdown runs, and the round starts when it reaches zero. This follows SS13 and SS14. **Ready** decides only who spawns at start. A player who isn't ready when the countdown ends stays in the lobby, free to join late or observe. The round never waits on one player who forgot to ready up. Admins can **hold** the countdown (waiting for someone) or **force** an immediate start (`admin-tools.md` §6). No operator has to be present for a round to begin. After a round ends, players return here and the loop runs again (`round-end.md` §6).

**Joining late is a direct pick.** The player sees the jobs with open slots, each with its open and total count and its description (§2), next to the **crew manifest**: who is already aboard and in which job. They click a job and spawn as their selected character. This is first-come-first-served, and that's fine. A latejoiner is joining a world already in motion, not competing at a shared starting line, so the race concern §1 raised doesn't apply. The manifest lets them see that the station is short a Doctor. An admin can turn latejoin off for a round.

**Observing is always available, and immediate.** A player can choose not to take a body at all: Observe puts them into the round as a ghost at once, before the round starts or during it (`observer.md` §2), and leaves them out of round start. It isn't a second toggle beside Ready.

## 8. The lobby screen

The lobby is what's on screen when a player isn't in a body: before the round, after it ends, and as the Escape menu during one.

**The sidebar** carries the round's state and timer, the selected character with its preview and High job, and the entry buttons: **Ready** (or **Join** once the round runs, §7) and **Observe**. Clicking the character opens the character window (§6). The game's wordmark heads the sidebar. A **music** row (on/off, track name, next, volume) controls the same menu music the main menu plays, which carries on from the main menu into the lobby and fades out when the player takes a body or observes. Report a bug and quit-to-menu sit at the bottom.

**The tabs** are:

- **Server Info**, where a player lands. It leads with a piece of the game's banner art (decoration only, never information) and shows the server's name, the message of the day, the map and the configured mode as plain text (a Secret server reads "Secret", so nothing leaks per §4), the build the server runs and its recent changes. A player knows where they are, what's being played and what changed since last time.
- **Chat**, for OOC and the other out-of-character channels (`comms.md` §7).
- **Settings**, for the player's own display, audio and controls.
- **Last Round**, the previous round's summary (`round-end.md`), until the next round ends.
- **Server Settings**, for admins only (`round-config.md`). It shows only rules that have a consumer in the game (§1).

There's no player list tab. The player count and OOC chat say who's around, and moderation belongs to the admin tools (`admin-tools.md`), not to lobby buttons.

**Behind the lobby**, the live station shows through, blurred. It's free, it's three-dimensional, and it's the map the player is about to play on.

This screen doesn't inherit main HUD's minimal-permanent-chrome rule, and that's deliberate. That rule exists to keep panels from competing with a 3D world that's actually there to look at. Here there's no world in play yet, so a dense screen where everything is visible at once is right. What carries over is the visual language (flat surfaces, hairline borders, muted status colors), not the restraint.

## 9. Worked examples

**A — Standard round start:**

| Step | What happens | Lobby state |
|---|---|---|
| 1 | Player selects their character "Ada Brandt", who has Engineer at High and Doctor at Medium | Sidebar shows Ada in engineering clothes, "High: Engineer" |
| 2 | Player opens the character window and makes Doctor High instead | Engineer drops to Medium automatically; the preview switches to doctor's clothes |
| 3 | Player ticks Nuclear Operative in the Antags tab and readies up | The opt-in is saved with Ada, separate from her job ranking |
| 4 | Enough players are connected; the countdown reaches zero | The mode is drawn, then resolution runs once, server-side, over every ready player's selected character |
| 5 | Doctor had room | Player spawns as Ada Brandt, Doctor |

**B — First connect:**

| Step | What happens | Lobby state |
|---|---|---|
| 1 | A new player connects for the first time | They already have a character with a random name and look, every job at Never, antags off |
| 2 | They ready up without opening the character window | Nothing blocks them |
| 3 | Round starts | They're assigned wherever needed and spawn under their random name |

**C — Latejoin:**

| Step | What happens | Lobby state |
|---|---|---|
| 1 | Round has been running for 20 minutes; a player connects | Join shows the open jobs next to the crew manifest |
| 2 | The manifest lists one Engineer and no Doctor; Doctor shows 2/2 open | Player picks Doctor and confirms; there's no resolution pass |
| 3 | Selection resolves immediately | Player spawns right away as their selected character |

## 10. Integration notes

| Element | Touches existing / needed system |
|---|---|
| Saved characters and their preferences | Player meta layer (`persistence-save.md` §5) |
| Job unlock/playtime gating | Stable player identity (`player-accounts.md`) and the player meta layer (`persistence-save.md` §5) |
| Job access | `id-access.md` §5 |
| Antagonist roles on offer | Enabled gamemodes — `round-config.md` §3 |
| Hold / force start, latejoin toggle | `admin-tools.md` §6 |
| Observe entry | The ghost — `observer.md` §2 |
| Return after a round, Last Round tab | `round-end.md` §6 |
| Spawn resolution | Spawn points placed in the map editor (`creative-mode.md` §2) |
| Lobby chat | `comms.md` §7 |

## 11. Out of scope for this pass

- Round configuration and gamemode selection itself (`round-config.md`)
- The admin toolkit itself, beyond hold/force start — designed in `admin-tools.md` §6, including any admin override of resolution
- Loadout choice (what a job spawns carrying beyond its standard kit)
- Non-human species, and appearance fields nothing reads yet (age, voice, markings)
- The exact fairness/weighting algorithm inside preference resolution (a balancing/backend decision, not a design one)
- Real authentication underlying a stable per-player key — designed in `player-accounts.md` (`persistence-save.md` §5 treats it as a black box)
- What a ghost can do (`observer.md`). This doc only adds the entry point
