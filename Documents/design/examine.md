# Examine — design document

> Status: active

Resolves a gap this project's own docs have been citing without it ever being written: `main-hud.md` §15 is referenced by name in `cargo.md` §5, `disposal.md` §4/§9/§10, `surgery.md` §9, `id-access.md` §3, and `inventory-storage.md` §14 — every one of them assuming a general "examine system" that reads printed properties off an object. Main HUD never actually defined it; it only ever defined the self-case (§5's hold-key per-limb/organ readout). This doc is both.

## 1. Design philosophy

BYOND's examine is a verb that dumps a paragraph of text into the chat log — name, description, whatever the object's examine() override decided to print, mixed in with speech, radio, and system spam. That's the artifact worth replacing: not the idea of reading text about an object, but the delivery mechanism, inherited from an engine where a scrolling text window was the only UI surface available for everything.

This project already half-solved this without naming it. Main HUD's "examine self" key (§5) opens a dedicated on-demand overlay instead of printing to a log, and every doc since that's needed to expose a printed object property — a crate's manifest, an ID card's face, a disposal package's tag — assumed the same on-demand shape, no chat log in sight. **Examine-self and examine-object were always the same verb aimed at different targets; they just got designed a version apart.** This doc unifies them.

Two rules fall out:

1. **One verb, target-dependent content.** Hold the same key. A valid external target under the cursor shows that target's info; no target falls through to the existing self-readout. No second binding to remember.
2. **Examine is always the public tier**, generalized from a rule one other doc already committed to in miniature (§5).

## 2. Target acquisition

Reuses the shared raycast/line-of-sight system already doing this exact job for comms occlusion (`comms.md` §3), hacking device discovery (`hacking-interface.md` §2), combat cover (`combat.md` §3), and several others — examine is simply another consumer, not a new detection mechanism. A wall blocks examine the same way it blocks a shot or a wireless scan.

**No target under the cursor → defaults to self.** This is the whole reason examine-self and examine-object can share one binding instead of needing two: if the raycast comes back empty or hits nothing examinable, the key falls through to the existing per-limb/organ readout (`main-hud.md` §5, `health.md` §7), unchanged.

**Range is generous, not RF-strict.** Unlike the hacking interface's wireless scan range (a real signal-propagation constraint) or comms' hearing range (a real acoustic one), reading a label or a manifest isn't limited by anything physical worth simulating — the constraint is "on screen and unoccluded," not a hard distance cutoff. Exact numbers are a balancing question (§11), not a design one.

## 3. Where it renders

Screen-space, not world-space. The zone-targeting reticle already proved the pattern this project wants for hover-driven info: a compact chip near the aim point, present only while a valid target's under the cursor (`main-hud.md` §6). Examine reuses that exact screen region and grammar rather than inventing a second one.

Floating world-space labels — nameplates hovering above objects — were the alternative, and they fail for the same reason main HUD §6 already flagged as an open risk for zone labels: legibility depends on camera angle, and this project's camera isn't committed to a framing that guarantees clean spatial separation. A fixed screen-space panel reads the same regardless of where the object sits on screen, doesn't fight for room with nearby geometry or other floating labels in a crowded space, and matches every other on-demand surface this project has built — the vitals readout, the alert stack, the zone-label chip, the diegetic device screens. One shared visual grammar, not two competing ones.

## 4. Trigger & persistence

Hold-to-peek: press and hold the examine key, the panel appears; release and it clears — identical behavior to the existing examine-self overlay, just now covering both targets under one mechanism (§1). No click-to-lock state, no dismiss button, no dedicated close gesture to design — the same momentary, minimal-chrome discipline main HUD §1 rule 3 already commits every other on-demand surface to.

**This puts a real ceiling on content length, and that's a feature.** If a manifest or description needs more than a handful of lines to read comfortably during a held key-press, it's too long. §6 sets that discipline explicitly.

## 5. Content rules — what examine actually shows

**Always the public tier, never role-gated.** `id-access.md` §3 already committed to this for one specific case — access levels aren't examine-visible to anyone, cardholder included, only through the PDA or a hacking tool. This doc generalizes that into examine's core rule rather than leaving it a one-off: examine never varies by who's looking. A Head of Personnel and an Assistant see identical text on an identical object. Anything that should differ by role or clearance already has its own tool — the PDA's tabs, the hacking interface's three tiers — and examine never grows a second, competing tiering system to duplicate them.

**Examine renders properties objects already carry — it doesn't invent new data.** A crate's manifest (`cargo.md` §5), an ID's name/job/department (`id-access.md` §3), a disposal package's destination tag (`disposal.md` §4) are all printed-on-the-object fields those docs already defined. Examine is the one shared surface that reads them, not a second place they get authored.

**Closed containers stay closed.** A Container's contents (`inventory-storage.md` §2) are hidden from examine by default — the same "a closed bag genuinely hides its contents" rule that doc's whole philosophy rests on. A crate reveals a manifest only because it's a specific, deliberately labeled object type, not because examine peeks inside every container it's pointed at. Examining an unopened backpack returns "a backpack," not a list of what's in it.

**Devices stop at name and flavor, never status.** The hacking interface's entire three-tier model (`hacking-interface.md` §3–4) is mediated through the FDU on purpose — even the "crew" tier requires the tool. Examine doesn't shortcut that by passively surfacing a device's access status or network state; pointing examine at AIRLOCK-ENG-03 returns "an airlock," not "your ID: authorized." The FDU stays the only way to learn anything live about a device, at any tier.

**Another character shows what's visibly presented, not what's carried.** Name and job appear only if their ID is worn visibly (the main HUD gear-strip ID slot) — not if it's tucked inside a PDA, which `id-access.md` §3 already treats as a second, separate on-person location, distinct from "held up" visibility. Alongside that: a compact list of visibly worn/held items (armor, a weapon in hand, obvious gear-strip contents). Their internal vitals stay entirely off-limits — visible wounds are already on the model directly (main HUD §5), and anything not visible on the model isn't examine's to reveal.

## 6. Length discipline

Given §4's hold-to-peek constraint, examine content is capped at a name/title line plus a small handful of short lines beneath it — never a paragraph. A long manifest (many distinct item types in one crate) truncates with a count rather than scrolling: "5× Advanced Medkit, 3× Trauma Kit, +2 more" rather than a full itemized dump. Full inventory-level detail already has its own surface — opening the container's panel (`inventory-storage.md` §6) — examine's job is a glance, not a manifest audit.

## 7. What different targets show

| Target type | Examine shows |
|---|---|
| Ordinary item (tool, weapon, misc) | Name, one-line flavor description, size class |
| Stack (sheets, ammo, meds) | Name, count, size class |
| Manifest-labeled container (crate) | Name, printed manifest (truncated per §6), lock state if present |
| Ordinary container (backpack, box, pocket) | Name only — contents stay closed (§5) |
| Locked device (crate, chute, locker) | Name, "locked"/"unlocked" only — no access detail (§5) |
| ID card | Name, job, department color (`id-access.md` §3) — never access levels |
| Tagged disposal package | Name, destination tag (`disposal.md` §4) |
| Extracted organ | Organ type, coarse condition (healthy / damaged / necrotic) — exact function is a medical-scanner-tier reading, not designed here (§11) |
| Device/machine (door, fabricator, APC) | Name, generic flavor only — status stays the FDU's job (§5) |
| Another character | Name/job if ID is worn visibly, list of visibly worn/held items — never their internal vitals |
| Self (no valid target under cursor) | Existing per-limb/organ readout, unchanged (`main-hud.md` §5, `health.md` §7) |

## 8. HUD touchpoints

No new permanent chrome — same discipline as everything else.

- Reuses the exact screen region and appear/disappear behavior the zone-targeting reticle already established (`main-hud.md` §6), just for a different key and a broader set of valid targets.
- Never competes with the vitals cluster, alerts, or gear strip — those stay exactly where main HUD §4 put them.
- Diegetic device screens (FDU, PDA, fabricator) still take over the center of the screen when actively open; examine doesn't render while one of those is up, and holding examine while a device screen is open does nothing.

## 9. Worked examples

**A — Glancing at a crate before opening it:**

| Step | What happens | Examine state |
|---|---|---|
| 1 | Cargo tech walks up to a freshly delivered crate | Holds examine, cursor over the crate |
| 2 | Panel appears | "Medical resupply crate — Contents: 5× Advanced Medkit, 3× Trauma Kit — Unlocked" |
| 3 | Releases the key | Panel clears instantly, no lingering state |
| 4 | Tech decides to actually open it | Separate action — prying it open per `cargo.md` §5, not an examine function |

**B — Sizing up another crew member:**

| Step | What happens | Examine state |
|---|---|---|
| 1 | Officer holds examine on an approaching Engineer | Engineer's ID is worn visibly in their gear-strip slot |
| 2 | Panel appears | "[Name] — Engineer — Engineering. Wearing: insulated gloves, tool belt. Holding: wrench." |
| 3 | Same Engineer, ID tucked inside their PDA instead | Panel drops the name/job line entirely — nothing worn-visible to read it from |

**C — An organ on the surgical tray:**

| Step | What happens | Examine state |
|---|---|---|
| 1 | Surgeon holds examine on a freshly extracted kidney | Raycast resolves to the organ item on the tray |
| 2 | Panel appears | "Kidney — condition: damaged" — coarse read only, not a numeric function value |
| 3 | Surgeon wants the exact number | Not available via examine — would need a medical scanner tool, not designed this pass (§11) |

## 10. Integration notes

| Examine element | Touches existing / needed system |
|---|---|
| Target acquisition | Shared raycast/line-of-sight system (`comms.md` §3, `hacking-interface.md` §2, `combat.md` §3, among others) |
| No-target fallback | Existing per-limb/organ readout (`main-hud.md` §5, `health.md` §7) |
| Screen-space panel, appear/disappear behavior | Zone-targeting reticle/chip pattern (`main-hud.md` §6) |
| Crate manifest | `cargo.md` §5 |
| ID card fields | `id-access.md` §3 |
| Disposal package tag | `disposal.md` §4 |
| Container contents staying hidden | `inventory-storage.md` §2, §6 |
| Device name-only, no status | `hacking-interface.md` §3–4 (FDU stays the sole live-status source) |
| Extracted organ condition | `surgery.md` §6 |

## 11. Out of scope for this pass

- Exact flavor text / description copy for every item, organ, and device (a content pass, not a design decision)
- Exact examine range and any distance-based text truncation (balancing, not design)
- A medical scanner tool for exact organ/vitals function values on another character — a plausible future item, not designed here
- Examine of dead/unconscious characters differing from living ones (no distinction assumed this pass)
- Any admin/observer-only omniscient examine variant — `observer.md`'s territory, not this doc's

## 12. Companion edits flagged

- `cargo.md` §5, `disposal.md` §4/§9/§10, `surgery.md` §9, `id-access.md` §3, and `inventory-storage.md` §14 all currently cite "`main-hud.md` §15" for the examine system. Nothing functional changes in any of them — just repoint each citation at this doc (`examine.md`), since that's where the system actually lives now.
- `main-hud.md` never had a §15 to begin with. This doc resolves that gap rather than main HUD growing one; no edit needed there beyond, optionally, a one-line pointer to this doc from §6 where the reticle pattern examine reuses is first defined.

## 13. Prototyping this

**Claude Design, prompt 1 — examine panel across target types:**
> Using our existing SS3D design system, build the examine panel in its screen-space home (same region as the zone-targeting reticle/label). Show four states: an ordinary item (name + flavor + size class), a manifest-labeled crate (name + truncated item list + lock state), an ID card (name/job/department color), and another character (name/job + a short worn/held item list). Same flat-surface, hairline-border, monospace-for-data language as the rest of this project's HUD.

**Claude Design, prompt 2 — self fallback and truncation:**
> Show the same examine key falling through to the existing per-limb/organ readout when nothing's under the cursor, and a long crate manifest truncating to "+N more" rather than overflowing the panel. This is to confirm the length discipline reads clearly before it's implementation-locked.

**Cursor, prompt 1 — data contract first:**
> Here's the examine design doc. Define the shared examine-text resolver: given a raycast hit (or none, falling through to self), return a target-type-tagged content block (item/container/ID/character/device/organ/self) built from each object's existing printed-property fields — no new data invented here, just a read path over what `cargo.md`, `id-access.md`, `disposal.md`, and `inventory-storage.md` already define. Show me the resolver and the content-block shape before wiring any panel UI.

**Cursor, prompt 2 — one vertical slice:**
> Wire the examine key end to end for two target types — an ordinary item and a manifest-labeled crate — reusing the existing raycast system for target acquisition and the zone-targeting reticle's screen region for the panel. Character, ID, organ, and device cases come after this is reviewed.
