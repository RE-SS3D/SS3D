# Examine — design document

> Status: active

Defines examine: the one shared way a player learns what something is by looking at it — an object, another character, or themselves. `cargo.md` §5, `disposal.md` §4, `surgery.md` §9 and `id-access.md` §3 all assume a general examine system that reads printed properties off an object; this doc is that system, and it also covers examining yourself. What other people *do* — the narrated line when someone flicks a switch or injects a patient — is its sibling, `action-messages.md`.

## 1. Design philosophy

BYOND's examine is a verb that dumps a paragraph of text into the chat log — name, description, whatever the object's examine() override decided to print, mixed in with speech, radio, and system spam. That's the artifact worth replacing: not the idea of reading text about an object, but the delivery mechanism, inherited from an engine where a scrolling text window was the only UI surface available for everything.

This project already half-solved this without naming it. Main HUD's "examine self" key (§5) opens a dedicated on-demand overlay instead of printing to a log, and every doc since that's needed to expose a printed object property — a crate's manifest, an ID card's face, a disposal package's tag — assumed the same on-demand shape, no chat log in sight. **Examine-self and examine-object were always the same verb aimed at different targets; they just got designed a version apart.** This doc unifies them.

Three rules fall out:

1. **One examine key, three depths.** A glance (hover), a look (hold the key), a search (characters only). Each is a deliberate step further than the last (§4); none needs a second binding to learn.
2. **Examine never varies by who you are — only by what you're wearing.** The same object reads the same to a Head of Personnel and an Assistant. Gear you put on may add lines; your role never does (§5).
3. **Examine describes the object as it appears.** If you could see it by looking, examine says it; if you'd have to open, hack or test it, examine doesn't (§5). `vision.md` §3 sets the bar: where 3D makes a state harder to read than the 2D original — a small model, an unclear wound — examine closes the gap in words.

## 2. Target acquisition

Examine follows the same line-of-sight rule as speech (`comms.md` §3) and gunfire (`combat.md` §3): what you can see, you can examine; a wall blocks examine the same way it blocks a shot.

**Examining yourself is pointing at yourself.** Your own character is always on screen, so holding examine over your own model gives the self readout (§7). Clicking the vitals cluster (`main-hud.md` §5) opens the same readout without aiming. Floors, walls and furniture are real examine targets — a cracked wall says so — so "nothing under the cursor" is never what selects yourself.

**Two range bands.** Examine doesn't simulate eyesight, but it does separate what you'd notice walking past from what you'd have to lean in to read:

- **Any visible range** — identity on a presented ID (name, job, department colour), a character's visible injuries and state (§5), coarse device state (powered, broken, bolted, armed), a breached seal, locked or unlocked. Identity has to read at range because sides are told apart by outfit and the examine name, with no friend-or-foe overlay (`combat.md` §6).
- **Close only** (within a couple of tiles) — fine print: a crate's manifest lines, a gun's round count, a gas tank's contents and level, a disposal package's tag, a nuclear device's timer digits, an organ's condition, a stack's count, the one-line flavour description. At range these lines simply don't appear; the panel shows what the range band allows.

Exact distances are balancing, not design (§11).

## 3. Where it renders

Screen-space, not world-space. The zone-targeting reticle already proved the pattern this project wants for hover-driven info: a compact chip near the aim point, present only while a valid target's under the cursor (`main-hud.md` §6). Examine reuses that exact screen region and grammar rather than inventing a second one.

Floating world-space labels — nameplates hovering above objects — were the alternative, and they fail for the same reason main HUD §6 already flagged as an open risk for zone labels: legibility depends on camera angle, and this project's camera isn't committed to a framing that guarantees clean spatial separation. A fixed screen-space panel reads the same regardless of where the object sits on screen, doesn't fight for room with nearby geometry or other floating labels in a crowded space, and matches every other on-demand surface this project has built — the vitals readout, the alert stack, the zone-label chip, the diegetic device screens. One shared visual grammar, not two competing ones.

**Keywords are colour-coded by what they mean for the reader.** A fixed palette of four meanings, learned once and read on any object at a glance:

| Colour | Meaning | Examples |
|---|---|---|
| Green | Fine, working | powered, intact, sealed, healthy |
| Amber | Degraded, needs attention | damaged, low, panel open, unpowered |
| Red | Danger | breached, armed, bleeding, dead, broken |
| Department colour | The job on a presented ID only (`id-access.md` §3) | Engineer, Security |

Everything else stays neutral. **Locked and unlocked are neutral** — whether a lock is good news depends on who's reading, and examine is the same for everyone (§1 rule 2). **The word always carries the meaning on its own**; colour speeds the read, it never replaces it.

## 4. Trigger & persistence

Three depths, each a deliberate step further:

- **Hover** — resting the cursor on something shows a small tooltip with its name (on a character, the identity they're presenting, §5). Free, momentary, gone when the cursor moves.
- **Hold the examine key** — opens the full examine panel (§5–§7) for as long as the key is held; releasing clears it. No click-to-lock state, no dismiss button — the same momentary, minimal-chrome discipline main HUD §1 rule 3 commits every other on-demand surface to. The panel is **live**: a draining tank, a counting-down nuclear device or a character starting to bleed updates while you watch.
- **Search** — on another character within reach, a persistent **Search** view lays out what they visibly wear and hold, slot by slot. It stays open while you stand next to them, and it's where taking items from someone happens (`inventory-storage.md` §9).

**No history.** Examine is a glance, not a log; nothing it shows is kept. Search covers the case where you need to keep looking.

**Being examined up close is noticed.** Holding examine on another character from within a couple of tiles sends them a target-only action message — "Marcus Voss looks you over" (`action-messages.md` §4). From further away examine stays silent and free. Hover never triggers it.

**The panel has a real ceiling on content length, and that's a feature.** If a description needs more than a handful of lines to read during a held key, it's too long. §6 sets that discipline explicitly.

## 5. Content rules — what examine actually shows

**Never role-gated; gear may add lines.** `id-access.md` §3 already committed to this for one specific case — access levels aren't examine-visible to anyone. This doc generalizes it: examine never varies by who's looking. Anything that should differ by role or clearance already has its own tool — the PDA's tabs, the hacking interface's three tiers. What *can* add lines is worn equipment: a pair of goggles that shows extra readings shows them to whoever wears it, so examine stays public to anyone willing to put the gear on. No gear does this in this pass; the first candidate is named in §11.

**Examine renders properties objects already carry — it doesn't invent new data.** A crate's manifest (`cargo.md` §5), an ID's name/job/department (`id-access.md` §3), a disposal package's destination tag (`disposal.md` §4) are all printed-on-the-object fields those docs already defined. Examine is the one shared surface that reads them, not a second place they get authored.

**Closed containers stay closed.** A Container's contents (`inventory-storage.md` §2) are hidden from examine by default — the same "a closed bag genuinely hides its contents" rule that doc's whole philosophy rests on. A crate reveals a manifest only because it's a specific, deliberately labeled object type, not because examine peeks inside every container it's pointed at. Examining an unopened backpack returns "a backpack," not a list of what's in it.

**Examine shows what's visible from outside — never hidden internals.** If you could see it by looking at the object, examine says it: a crate is locked, a door's panel is open and sparking, a gun has twelve rounds in its magazine (`combat.md` §3), a suit's seal is intact, a nuclear device is armed with ninety seconds on its display (`antagonist-content.md` §6). What you couldn't see by looking stays off examine: a device's network state, which access a door wants, whether *your* ID would pass. Those belong to the tools built for them — the hacking interface (`hacking-interface.md` §3–4) or just trying the door. This is SS13's rule: examine describes the object as it appears.

**Another character shows what's visibly presented, not what's carried.**

- **Identity.** Name and job appear when they're **presenting an ID** (`id-access.md` §3): a card in their ID slot, bare or inside a worn PDA, or held in their hand. A card in a pocket or bag doesn't count — then examine names them only by what it can see ("an unknown crew member in an engineering jumpsuit"). Identity comes from the ID alone this pass; a visible face is not read (§11).
- **Visible injuries.** What a look at the body would tell you: bleeding, a missing limb, how badly a part is hurt in coarse words ("badly burned left arm"), whether they're conscious. Main HUD §5 puts wounds on the model; examine says in words what a small model can't show reliably.
- **State.** Dead ("appears to be dead"), unconscious, restrained ("is handcuffed"), and disconnected — "is asleep and unresponsive" (`networking.md` §2).
- **Visible gear.** A compact list of visibly worn and held items — armor, a weapon in hand, obvious gear-strip contents.
- **Never their internals.** How they feel, organ state, exact numbers — none of it. Exact readings are a medical scanner's job (`virology.md` §5).

**Search shows only what's visible.** On a conscious character, Search lays out what they visibly wear and hold. Pockets and covered slots appear only as **occupied or empty**, never what's in them. Reaching into one blind is possible; it's slower than an open take and the target is warned (`inventory-storage.md` §9; `action-messages.md` §4). Once they're dead, unconscious or restrained, every slot reads plainly.

## 6. Length discipline

Given §4's hold-to-look constraint, examine content is capped at a name/title line plus a small handful of short lines beneath it — never a paragraph. A long manifest (many distinct item types in one crate) truncates with a count rather than scrolling: "5× Advanced Medkit, 3× Trauma Kit, +2 more" rather than a full itemized dump. Full inventory-level detail already has its own surface — opening the container's panel (`inventory-storage.md` §6) — examine's job is a glance, not a manifest audit.

## 7. What different targets show

Fine-print lines (§2) appear only up close; everything else reads at any visible range.

| Target type | Examine shows |
|---|---|
| Ordinary item (tool, weapon, misc) | Name, size class; flavor line up close |
| Stack (sheets, ammo, meds) | Name, size class; count up close |
| Manifest-labeled container (crate) | Name, lock state if present; printed manifest (truncated per §6) up close |
| Gun | Name; rounds in the loaded magazine up close |
| Armor or sealed suit | Name, condition, seal intact or breached |
| Gas tank | Name; what it holds and level remaining up close |
| Ordinary container (backpack, box, pocket) | Name only — contents stay closed (§5) |
| Locked device (crate, chute, locker) | Name, "locked"/"unlocked" only — no access detail (§5) |
| ID card | Name, job, department color (`id-access.md` §3) — never access levels |
| Tagged disposal package | Name; destination tag up close (`disposal.md` §4) |
| Extracted organ | Organ type; coarse condition (healthy / damaged / necrotic) up close — exact function is a medical-scanner reading (`virology.md` §5) |
| Device/machine (door, fabricator, APC, nuclear device) | Name, externally visible state (powered, bolted, panel open, armed); flavor and countdown digits up close — never access or network detail (§5) |
| Another character | Identity if presenting an ID, visible injuries, state (dead / unconscious / restrained / asleep), visibly worn/held items — never their internals |
| Self (pointing at your own model, or clicking the vitals cluster) | Your condition in plain sentences (`health.md` §7), including how you feel; and the identity you're presenting ("You are presenting: [Name], Engineer"), so you know what others read on you |

## 8. HUD touchpoints

No new permanent chrome — same discipline as everything else.

- Reuses the exact screen region and appear/disappear behavior the zone-targeting reticle already established (`main-hud.md` §6), just for a different key and a broader set of valid targets.
- Never competes with the vitals cluster, alerts, or gear strip — those stay exactly where main HUD §4 put them. Clicking the vitals cluster is the one new touchpoint: it opens self-examine (§2).
- Diegetic device screens (FDU, PDA, fabricator) still take over the center of the screen when actively open; examine doesn't render while one of those is up, and holding examine while a device screen is open does nothing.

## 9. Worked examples

**A — Glancing at a crate before opening it:**

| Step | What happens | Examine state |
|---|---|---|
| 1 | Cargo tech sees a crate across the bay | Holds examine on it: "Medical resupply crate — Unlocked". The manifest is fine print; it doesn't show at this range |
| 2 | Walks up and holds examine again | "Medical resupply crate — Contents: 5× Advanced Medkit, 3× Trauma Kit — Unlocked" |
| 3 | Releases the key | Panel clears instantly, no lingering state |
| 4 | Tech decides to actually open it | Separate action — prying it open per `cargo.md` §5, not an examine function |

**B — Sizing up another crew member:**

| Step | What happens | Examine state |
|---|---|---|
| 1 | Officer holds examine on an approaching Engineer, a few tiles away | Engineer's PDA, with their ID inside, is in their ID slot |
| 2 | Panel appears | "[Name] — **Engineer** (in engineering colour). Wearing: insulated gloves, tool belt. Holding: wrench." |
| 3 | The Engineer is within a couple of tiles | They get "[Officer's name] looks you over" |
| 4 | Same Engineer, ID stuffed in a pocket instead | Panel drops the name/job line — "an unknown crew member in an engineering jumpsuit" |

**C — Triage at a glance:**

| Step | What happens | Examine state |
|---|---|---|
| 1 | Medic holds examine on two bodies in a corridor | First: "appears to be dead" (red). Second: "is unconscious. **Bleeding** heavily; badly burned left arm" |
| 2 | Medic goes to the second | The reading never gives a number — exact values need the medical scanner (`virology.md` §5) |

**D — An organ on the surgical tray:**

| Step | What happens | Examine state |
|---|---|---|
| 1 | Surgeon holds examine on a freshly extracted kidney | Raycast resolves to the organ item on the tray |
| 2 | Panel appears | "Kidney — condition: **damaged**" (amber) — coarse read only, not a numeric function value |
| 3 | Surgeon wants the exact number | Not available via examine — needs the medical scanner tool (`virology.md` §5) |

## 10. Integration notes

| Examine element | Touches existing / needed system |
|---|---|
| Target acquisition | Same line-of-sight rule as `comms.md` §3, `combat.md` §3 |
| Identity at range, no friend-or-foe overlay | `combat.md` §6 |
| Self-examine sentences | `health.md` §7 |
| Vitals cluster as a self-examine shortcut | `main-hud.md` §5 |
| Presented ID | `id-access.md` §3 |
| Search view, blind take | `inventory-storage.md` §9 |
| Examine cue to the target, blind-take warning | `action-messages.md` §4 |
| Screen-space panel, appear/disappear behavior | Zone-targeting reticle/chip pattern (`main-hud.md` §6) |
| Disconnected character | `networking.md` §2 |
| Crate manifest | `cargo.md` §5 |
| ID card fields | `id-access.md` §3 |
| Disposal package tag | `disposal.md` §4 |
| Nuclear device state | `antagonist-content.md` §6 |
| Container contents staying hidden | `inventory-storage.md` §2, §6 |
| Visible state, not internals | `hacking-interface.md` §3–4 (the tool for internals) |
| Extracted organ condition | `surgery.md` §6 |
| Exact readings | Medical scanner (`virology.md` §5) |

## 11. Out of scope for this pass

- Exact flavor text / description copy for every item, organ, and device (a content pass, not a design decision)
- Exact distances for the two range bands (§2) and for the examine cue (§4) — balancing, not design
- Gear that adds examine lines (§5). The first candidate is a **medical HUD** showing a coarse health status on others (healthy / injured / critical / dead); a **security HUD** waits on a crew-records system that doesn't exist yet
- **Face identity** — a visible face naming someone, masks and helmets hiding it, and a mismatch with their ID showing ("Name (as ID-name)"). The intended later pass; this pass reads identity from the ID alone (§5)
- A medical scanner tool for exact organ/vitals function values on another character — built in `virology.md` §5
- Any admin/observer-only omniscient examine variant — `observer.md`'s territory, not this doc's
