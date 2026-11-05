# Examine — design document

> Status: active

Defines examine: the one shared way a player learns what something is by looking at it — an object, another character, or themselves. `cargo.md` §5, `disposal.md` §4, `surgery.md` §9 and `id-access.md` §3 all assume a general examine system that reads printed properties off an object; this doc is that system, and it also covers examining yourself.

## 1. Design philosophy

BYOND's examine is a verb that dumps a paragraph of text into the chat log — name, description, whatever the object's examine() override decided to print, mixed in with speech, radio, and system spam. That's the artifact worth replacing: not the idea of reading text about an object, but the delivery mechanism, inherited from an engine where a scrolling text window was the only UI surface available for everything.

This project already half-solved this without naming it. Main HUD's "examine self" key (§5) opens a dedicated on-demand overlay instead of printing to a log, and every doc since that's needed to expose a printed object property — a crate's manifest, an ID card's face, a disposal package's tag — assumed the same on-demand shape, no chat log in sight. **Examine-self and examine-object were always the same verb aimed at different targets; they just got designed a version apart.** This doc unifies them.

Two rules fall out:

1. **One verb, target-dependent content.** Hold the same key. A valid external target under the cursor shows that target's info; no target falls through to the existing self-readout. No second binding to remember.
2. **Examine is always the public tier**, generalized from a rule one other doc already committed to in miniature (§5).

## 2. Target acquisition

Examine follows the same line-of-sight rule as speech (`comms.md` §3) and gunfire (`combat.md` §3): what you can see, you can examine; a wall blocks examine the same way it blocks a shot.

**No target under the cursor → defaults to self.** This is the whole reason examine-self and examine-object can share one binding instead of needing two: with nothing examinable under the cursor, examine describes your own condition in plain sentences (`health.md` §7).

**Range is generous, not RF-strict.** Unlike the hacking interface's wireless scan range (a real signal-propagation constraint) or comms' hearing range (a real acoustic one), reading a label or a manifest isn't limited by anything physical worth simulating — the constraint is "on screen and unoccluded," not a hard distance cutoff. Exact numbers are a balancing question (§11), not a design one.

## 3. Where it renders

Screen-space, not world-space. The zone-targeting reticle already proved the pattern this project wants for hover-driven info: a compact chip near the aim point, present only while a valid target's under the cursor (`main-hud.md` §6). Examine reuses that exact screen region and grammar rather than inventing a second one.

Floating world-space labels — nameplates hovering above objects — were the alternative, and they fail for the same reason main HUD §6 already flagged as an open risk for zone labels: legibility depends on camera angle, and this project's camera isn't committed to a framing that guarantees clean spatial separation. A fixed screen-space panel reads the same regardless of where the object sits on screen, doesn't fight for room with nearby geometry or other floating labels in a crowded space, and matches every other on-demand surface this project has built — the vitals readout, the alert stack, the zone-label chip, the diegetic device screens. One shared visual grammar, not two competing ones.

## 4. Trigger & persistence

Three levels, each a deliberate step further:

- **Hover** — resting the cursor on something shows a small tooltip with its name. Free, momentary, gone when the cursor moves.
- **Shift** — holding Shift over a target opens the full examine panel (§5–§7); releasing clears it. No click-to-lock state, no dismiss button — the same momentary, minimal-chrome discipline main HUD §1 rule 3 commits every other on-demand surface to.
- **Search** — on another character, a persistent **Search** view lays out what they visibly wear and hold, slot by slot. It stays open while you stand next to them, and it's where taking items from someone happens (`inventory-storage.md` §9).

**The Shift panel has a real ceiling on content length, and that's a feature.** If a description needs more than a handful of lines to read during a held key, it's too long. §6 sets that discipline explicitly.

## 5. Content rules — what examine actually shows

**Always the public tier, never role-gated.** `id-access.md` §3 already committed to this for one specific case — access levels aren't examine-visible to anyone. This doc generalizes that into examine's core rule rather than leaving it a one-off: examine never varies by who's looking. A Head of Personnel and an Assistant see identical text on an identical object. Anything that should differ by role or clearance already has its own tool — the PDA's tabs, the hacking interface's three tiers — and examine never grows a second, competing tiering system to duplicate them.

**Examine renders properties objects already carry — it doesn't invent new data.** A crate's manifest (`cargo.md` §5), an ID's name/job/department (`id-access.md` §3), a disposal package's destination tag (`disposal.md` §4) are all printed-on-the-object fields those docs already defined. Examine is the one shared surface that reads them, not a second place they get authored.

**Closed containers stay closed.** A Container's contents (`inventory-storage.md` §2) are hidden from examine by default — the same "a closed bag genuinely hides its contents" rule that doc's whole philosophy rests on. A crate reveals a manifest only because it's a specific, deliberately labeled object type, not because examine peeks inside every container it's pointed at. Examining an unopened backpack returns "a backpack," not a list of what's in it.

**Examine shows what's visible from outside — never hidden internals.** If you could see it by looking at the object, examine says it: a crate is locked, a door's panel is open and sparking, a gun has twelve rounds in its magazine (`combat.md` §3), a suit's seal is intact, a nuclear device is armed with ninety seconds on its display (`antagonist-content.md` §6). What you couldn't see by looking stays off examine: a device's network state, which access a door wants, whether *your* ID would pass. Those belong to the tools built for them — the hacking interface (`hacking-interface.md` §3–4) or just trying the door. This is SS13's rule: examine describes the object as it appears.

**Another character shows what's visibly presented, not what's carried.** Name and job appear when they're **presenting an ID** (`id-access.md` §3): a card in their ID slot, bare or inside a worn PDA, or held in their hand. A card in a pocket or bag doesn't count — then examine names them only by what it can see ("an unknown crew member in an engineering jumpsuit"). A disconnected character reads as asleep and unresponsive (`networking.md` §2). Alongside that: a compact list of visibly worn/held items (armor, a weapon in hand, obvious gear-strip contents). Their internal vitals stay entirely off-limits — visible wounds are already on the model directly (main HUD §5), and anything not visible on the model isn't examine's to reveal.

## 6. Length discipline

Given §4's hold-to-peek constraint, examine content is capped at a name/title line plus a small handful of short lines beneath it — never a paragraph. A long manifest (many distinct item types in one crate) truncates with a count rather than scrolling: "5× Advanced Medkit, 3× Trauma Kit, +2 more" rather than a full itemized dump. Full inventory-level detail already has its own surface — opening the container's panel (`inventory-storage.md` §6) — examine's job is a glance, not a manifest audit.

## 7. What different targets show

| Target type | Examine shows |
|---|---|
| Ordinary item (tool, weapon, misc) | Name, one-line flavor description, size class |
| Stack (sheets, ammo, meds) | Name, count, size class |
| Manifest-labeled container (crate) | Name, printed manifest (truncated per §6), lock state if present |
| Gun | Name, rounds in the loaded magazine |
| Armor or sealed suit | Name, condition, seal intact or breached |
| Gas tank | Name, what it holds, level remaining |
| Ordinary container (backpack, box, pocket) | Name only — contents stay closed (§5) |
| Locked device (crate, chute, locker) | Name, "locked"/"unlocked" only — no access detail (§5) |
| ID card | Name, job, department color (`id-access.md` §3) — never access levels |
| Tagged disposal package | Name, destination tag (`disposal.md` §4) |
| Extracted organ | Organ type, coarse condition (healthy / damaged / necrotic) — exact function is a medical-scanner-tier reading, not designed here (§11) |
| Device/machine (door, fabricator, APC, nuclear device) | Name, flavor, and externally visible state (bolted, panel open, armed, countdown) — never access or network detail (§5) |
| Another character | Name/job if presenting an ID, list of visibly worn/held items — never their internal vitals |
| Self (no valid target under cursor) | Your condition in plain sentences (`health.md` §7) |

## 8. HUD touchpoints

No new permanent chrome — same discipline as everything else.

- Reuses the exact screen region and appear/disappear behavior the zone-targeting reticle already established (`main-hud.md` §6), just for a different key and a broader set of valid targets.
- Never competes with the vitals cluster, alerts, or gear strip — those stay exactly where main HUD §4 put them.
- Diegetic device screens (FDU, PDA, fabricator) still take over the center of the screen when actively open; examine doesn't render while one of those is up, and holding examine while a device screen is open does nothing.

## 9. Worked examples

**A — Glancing at a crate before opening it:**

| Step | What happens | Examine state |
|---|---|---|
| 1 | Cargo tech walks up to a freshly delivered crate | Holds Shift, cursor over the crate |
| 2 | Panel appears | "Medical resupply crate — Contents: 5× Advanced Medkit, 3× Trauma Kit — Unlocked" |
| 3 | Releases the key | Panel clears instantly, no lingering state |
| 4 | Tech decides to actually open it | Separate action — prying it open per `cargo.md` §5, not an examine function |

**B — Sizing up another crew member:**

| Step | What happens | Examine state |
|---|---|---|
| 1 | Officer holds Shift on an approaching Engineer | Engineer's PDA, with their ID inside, is in their ID slot |
| 2 | Panel appears | "[Name] — Engineer — Engineering. Wearing: insulated gloves, tool belt. Holding: wrench." |
| 3 | Same Engineer, ID stuffed in a pocket instead | Panel drops the name/job line — "an unknown crew member in an engineering jumpsuit" |

**C — An organ on the surgical tray:**

| Step | What happens | Examine state |
|---|---|---|
| 1 | Surgeon holds examine on a freshly extracted kidney | Raycast resolves to the organ item on the tray |
| 2 | Panel appears | "Kidney — condition: damaged" — coarse read only, not a numeric function value |
| 3 | Surgeon wants the exact number | Not available via examine — needs the medical scanner tool (`virology.md` §5) |

## 10. Integration notes

| Examine element | Touches existing / needed system |
|---|---|
| Target acquisition | Same line-of-sight rule as `comms.md` §3, `combat.md` §3 |
| No-target fallback | Self-examine sentences (`health.md` §7) |
| Presented ID | `id-access.md` §3 |
| Search view | `inventory-storage.md` §9 |
| Screen-space panel, appear/disappear behavior | Zone-targeting reticle/chip pattern (`main-hud.md` §6) |
| Crate manifest | `cargo.md` §5 |
| ID card fields | `id-access.md` §3 |
| Disposal package tag | `disposal.md` §4 |
| Container contents staying hidden | `inventory-storage.md` §2, §6 |
| Visible state, not internals | `hacking-interface.md` §3–4 (the tool for internals) |
| Extracted organ condition | `surgery.md` §6 |

## 11. Out of scope for this pass

- Exact flavor text / description copy for every item, organ, and device (a content pass, not a design decision)
- Exact examine range and any distance-based text truncation (balancing, not design)
- A medical scanner tool for exact organ/vitals function values on another character — built in `virology.md` §5
- Examine of dead/unconscious characters differing from living ones (no distinction assumed this pass)
- Any admin/observer-only omniscient examine variant — `observer.md`'s territory, not this doc's
