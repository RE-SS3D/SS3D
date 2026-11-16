# ID/Access — design document

> Status: active

Formalizes a system this project has been quietly depending on since the first hacking pass without ever specifying it. `hacking-interface.md` §3 builds its entire "System permissions" panel on "the game's existing ID/access-level system"; §4's three-tier model gates its middle tier on an unnamed "engineering credential." `area.md` §5 defaults door requirements to it. `pda.md` §2–3 gives it a physical card slot and a summary tab. `cargo.md` §5 and `disposal.md` §2/§4 lock crates and chutes behind it. `ai-cyborgs.md` §3–5 expresses the AI's entire authority model as holding it, and §4 revokes it as a crew response tool. `death-cloning-respawn.md` §4 ties DNA records to the same identity rail. `chemistry.md`'s dispenser screen gates behind it. None of those docs invented a parallel system — they all assumed this one and moved on. This doc is what they were assuming.

One thing falls out of reading all of them together that's worth stating up front, because it's the spine of this whole doc: the hacking interface's "engineer tier" was never actually about the *profession* engineer. AIRLOCK-ENG-03's worked example unlocks its maintenance firmware for "an engineering credential" — a specific access level, not a job title. Every device in that taxonomy checks whether the operator holds *the access level that device's firmware cares about*. That's an access check. This doc supplies the system being checked.

## 1. Design philosophy

Four moves carry this doc:

1. **One identity rail, not a database per system.** A crew member's name, job, department, access-level set, and DNA-record reference (`death-cloning-respawn.md` §4) all live on one server-side crew record. The ID card is a physical token bound to that record, not a second source of truth — the same "no parallel model" move Area made for power and the hacking interface made for permissions, applied to identity itself.
2. **Physical possession is the gate, same as everywhere else.** An access check reads the ID the character is **wearing in their ID slot or holding in their active hand** (§3) — not a hidden player-level flag, and not a card buried in a pocket or bag. No ID presented, no access, full stop.
3. **Granular, named credentials — not eight department flags.** `area.md` §6 already needed "an additional cert" beyond bridge access for the captain's private office. The hacking interface's engineering credential is its own thing, not "has Engineering." `disposal.md` §4 gates a restricted destination on "matching access" that's clearly narrower than a whole department. The precedent across this project already assumes a real list of named levels, not a coarse eight-way split — this doc makes that list's shape explicit rather than retrofitting it.
4. **Every consumer reads the same access levels, and the places that matter keep a log.** Doors, device firmware tiers, crate and chute locks, the AI's Area authority, the chem dispenser gate — one rule, one set of named levels, used from a dozen places. Devices that guard something worth auditing (secure doors, the ID console) keep a log of who used them (§6) — the "detectable, not prevented" doctrine `ai-cyborgs.md` §4 applies to law violations.

## 2. The crew record — identity rail

A record created when lobby's preference resolution assigns a job (`lobby.md` §5), one per active crew member for the round:

- Name, job, department
- Access-level set (§4–5)
- DNA record reference, where one exists (`death-cloning-respawn.md` §4 — this doc is the "existing crew identity record" that doc already assumed)
- Connection status — what `pda.md` §5's crew manifest tab actually reads

This is the authoritative object. The physical card (§3) and the PDA's identity tab (`pda.md` §3) are both just views onto it, or in the console's case (§7), an editor for its access-level field. Persistence across rounds — a returning player's job history feeding lobby's playtime gates (`lobby.md` §2) — rides on the player meta layer (`persistence-save.md` §5), keyed to a stable player identity that doc treats as a black box pending real authentication.

## 3. The ID card — physical object

A real, held/worn item. It's a passive token: no onboard intelligence, no firmware of its own. It carries a reference to the crew record in §2, nothing more.

**One slot for ID and PDA.** The gear strip has a single **ID slot** (`main-hud.md` §8). It holds either a bare ID card or a PDA with a card inside it (`pda.md` §2) — a crew member normally spawns with the latter. Both count identically.

**What counts as presenting an ID** — for access and for identity on examine alike:

- the card in the ID slot, bare or inside the worn PDA; or
- a card (or PDA with a card) held in the **active hand**, as when you hold your badge up to a door.

A card in a pocket, a backpack, or a box doesn't count. It's yours, but you aren't presenting it. This is SS13's rule, and it means taking someone's ID out of their slot really does strip them of their access and their name.

**Examine reveals name, job, and department color** of the presented ID (`examine.md` §5) — printed on the card's face, readable even through a worn PDA's window, world-carries-the-information the same way a crate's manifest is readable without opening it (`cargo.md` §5). Anyone can glance at a held-up ID and know who it claims to be.

**Access levels are not visible on examine.** They're only readable by the card's own holder (through the PDA's ID/access summary tab once the PDA exists, `pda.md` §3), or by someone running the hacking interface's permissions panel on a device the card is being used against — exactly the "crew see only whether their own ID works" framing that panel already commits to (`hacking-interface.md` §3). Nobody gets an omniscient readout of a stranger's clearance by looking at their badge.

**Dropped, stolen, handed off — no special binding.** Whoever's holding it is whoever it authorizes, the instant they're holding it. `pda.md`'s worked example B already commits to this for a stolen PDA-plus-ID; this doc is what makes that true rather than a one-off.

## 4. Access levels — the model

Not a flat eight-way department switch. A **named, independently-grantable set of levels** — the exact requirement the hacking interface's hacker tier reads (§3 there) and the exact thing a cargo crate lock or a disposal chute lock checks against (`cargo.md` §5, `disposal.md` §2).

Two bands:

- **Department levels**, one per department already established in `lobby.md` §2: Command, Security, Engineering, Medical, Science, Cargo, Service, Civilian. Most jobs carry exactly their own department level plus a couple of station-wide levels almost everyone gets (Maintenance, general Crew).
- **Cross-cutting levels**, narrower than a department and not owned by any one of them: Bridge, Captain's Office, Armory, EVA, AI Upload, Evidence Lockup, Change ID (§7), and others as content demands. These are what `area.md` §6's "additional cert" and the hacking interface's "engineering credential" were always pointing at — a real, specific thing, not a department flag standing in for it.

The exact enumerated list — every level that will ever exist and which device checks which — is a content pass, not a design decision (§12). What this doc fixes is the *shape*: department levels plus a short, growable list of named extras, all in one flat set so no consumer needs to know which band a given level came from.

## 5. Job → access resolution

Each job record (`lobby.md` §2) resolves to a starting access-level set the moment lobby assigns it — this doc supplies the table lobby reads from, — lobby doesn't invent it, it reads it. A representative slice, not exhaustive:

| Job | Access-level set |
|---|---|
| Captain | Every department level, plus Bridge, Captain's Office, Change ID, EVA |
| Head of Personnel | Command, plus Change ID, EVA |
| Head of Security | Security, Command, plus Armory, Evidence Lockup, Bridge |
| Security Officer | Security, plus Armory, Evidence Lockup |
| Chief Engineer | Engineering, Command, plus EVA, AI Upload |
| Engineer | Engineering, plus EVA |
| Chief Medical Officer | Medical, Command |
| Medical Doctor | Medical |
| Research Director | Science, Command |
| Quartermaster | Cargo, Command |
| Cargo Technician | Cargo |
| Bartender / Chef | Service |
| Assistant | Civilian |

Even the Captain's set is enumerated, not a hardcoded bypass flag — a compromised or subverted Captain's ID is a real set of bits, and revoking it at the console (§7) actually removes something rather than fighting a special case that ignores the check entirely.

**Gamemode-specific deltas** (a Head Revolutionary needing nothing extra, a traitor being ordinary crew with a hidden objective) are explicitly not this doc's business — the same disclaimer `round-config.md` §6 already gives its own per-gamemode antag logic. Traitor's "nothing extra" is now confirmed explicitly in `antagonist-content.md` §3; Revolution itself remains undesigned (`antagonist-content.md` §6).

## 6. The access check — one shared function

Every consumer in this project asks the same question: *does the holder's presented ID (§3) carry the required level(s)?* Nothing here is bespoke per device:

- **Doors** default to their Area's requirement, with a per-door override for real exceptions (`area.md` §5) — this doc is what that requirement check actually calls.
- **The hacking interface's three tiers** (`hacking-interface.md` §3–4) are this check, restated per-doc: crew tier is "does your ID pass," maintenance tier is "does your ID carry the one specific level this device's firmware wants," hacker tier is "read the raw requirement and match it illegitimately."
- **Cargo crate locks and disposal chute locks** (`cargo.md` §5, `disposal.md` §2) are the same check applied to an object instead of a door.
- **The AI's Area authority** (`ai-cyborgs.md` §3) is this same check running against the AI's own held authority set instead of a crew ID — "the same underlying check with a different credential," which that doc already says outright. Revoking the AI's access (§4 there) means removing levels from that set, the identical console-adjacent action §7 below performs on a crew ID.
- **The chem dispenser's ID/access gate** (`chemistry.md`) reads the same function.

**Logging devices.** A device that guards something worth auditing — a secure door (armory, vault, bridge), the ID console (§7) — keeps an auth log: device, identity, requested level, pass/fail, time. Ordinary doors don't. Each log keeps **this round's entries only**, up to a cap, so it's a usable record rather than an endless one. Crew never see it; engineer tier reads it as the "last-auth log" the hacking interface promises (§3 there). This is the concrete mechanism behind the "detectable, not prevented" claims this project makes about credentials.

## 7. The ID console — legitimate issuance and modification

A real console, one or a small handful per station (siting is a layout question, §12), diegetic device screen in the same visual family as the FDU and fabricator (`hacking-interface.md` §2).

**Insert a target card, and the console runs §6's check on the *operator's own* held ID before allowing any edit** — no bespoke authorization logic, just the same function pointed at itself. The level it's gating on is **Change ID**, one more entry in §4's cross-cutting list.

**Only the Captain and Head of Personnel carry Change ID by default.** This is a deliberate recommendation, not a placeholder: distributing self-serve write access across every department head would turn eight consoles into eight equally-forgettable objects, where one console with real gatekeeping becomes a landmark worth defending and worth targeting — the same role the AI upload console already plays in `ai-cyborgs.md` §4. A head who needs a crew member's access adjusted asks HR or the Captain over comms; the request is social, the edit is still centralized and auditable in one place.

**Every grant or revoke writes to the console's own log**, same discipline as everything in §6 — an inspectable trail of who changed what access, when, and under whose authorization.

## 8. Forgery and spoofing — a hacking-interface consumer, not a new mechanic

**Later pass.** Forgery needs the hacking interface (`hacking-interface.md`, deferred), so none of this section is in the first pass. Until then, the only ways to get access you weren't issued are to be granted it at the console (§7) or to take someone else's card (§9).

The console is itself a device in the FDU's three-tier taxonomy, and nothing about it needs inventing beyond plugging it in:

- **Crew tier:** insert your own ID, see your own summary. No edit capability — that was never a firmware question, it was always the Change ID check in §7.
- **Legitimate operator tier:** identical UI, unlocked the moment the operator's own ID satisfies §7's Change ID check — structurally the same "maintenance firmware unlocked by a credential" shape as an engineer at any other device, just with Change ID standing in for an engineering credential.
- **Hacker tier:** the console's real weak points — a replayable Change ID credential captured elsewhere, an unsigned-firmware slot, its physical tamper switch — become available the same way any other device's security layers get bypassed (`hacking-interface.md` §3). A successful bypass lets the hacker write an arbitrary access set onto a card without ever holding Change ID themselves.

This is the literal mechanism behind "a cloned or forged credential" and "the raw bitmask... where a cloned or forged credential would need to match" — phrases the hacking interface doc already used (§3, §5 worked example) without ever saying how the write actually happens. Now it does: through this console, bypassed. **The write still logs**, per §7 — an illegitimate grant leaves an edit in the console's log with no matching legitimate operator session, real evidence for whoever reviews it later, not a clean erasure.

A standalone, console-independent forging tool — a portable cloner an antagonist could carry and use anywhere — is a plausible future antag item in the SS13 tradition, but it's a content addition layered on top of this mechanism, not a different mechanism. Now designed as a Traitor uplink item (`antagonist-content.md` §4) — the write still goes through this exact mechanism, still logs, per this section.

## 9. Loss, theft, revocation

**A stolen card or PDA is instantly and fully functional for whoever's holding it** — no re-binding, no hacking required to use a card that isn't yours, per §3's "physical possession is the gate" rule and the PDA doc's own worked example. That's the stake, not a bug to route around.

**Reporting a card lost or stolen** is a social action — telling security or HR over comms or in person — not a button that instantly kills the card. The old card keeps working exactly as it did until someone with Change ID access actually revokes it at the console (§7), which is itself a real, logged, timed action, not a silent flip.

**AI and cyborg revocation** (`ai-cyborgs.md` §4) is this identical console-adjacent mechanism, scoped to the AI's authority set instead of a crew card. No separate revocation system exists for the AI; it's this one, pointed at a different record.

## 10. Worked examples

**A — Ordinary door:**

| Step | What happens | State |
|---|---|---|
| 1 | Engineer walks up to AIRLOCK-ENG-03 | Door reads Area's default requirement: Engineering |
| 2 | Engineer's ID (in ID slot) carries Engineering | §6 check passes; door opens; auth log gets a pass entry |
| 3 | An Assistant tries the same door later | Their ID carries only Civilian; check fails; door stays shut; auth log gets a fail entry, visible to anyone running engineer-tier diagnostics on the device |

**B — HR grants access to a newly promoted engineer:**

| Step | What happens | State |
|---|---|---|
| 1 | A Cargo Technician is promoted mid-round to Engineer over comms | No mechanical change yet — a job title change alone does nothing |
| 2 | They bring their ID to the HoP's console | HoP inserts their own ID (carries Change ID), then the target card |
| 3 | HoP adds the Engineering level, removes Cargo | Console checks HoP's Change ID per §7, allows the edit, writes it to its log |
| 4 | Target's card now opens engineering doors | Same §6 check, now passing where it didn't before |

**C — Hacker forges Bridge access, caught later:**

| Step | What happens | State |
|---|---|---|
| 1 | A hacker fingerprints the ID console, finds a replayable Change ID credential | Standard hacker-tier discovery, `hacking-interface.md` §3 |
| 2 | Writes Bridge access onto a blank card without ever holding Change ID | §8's illegitimate-write path |
| 3 | Uses the card to walk onto the bridge | §6 check passes — the card genuinely carries Bridge now |
| 4 | Security later reviews the console's log | Sees an edit with no matching legitimate operator session — real evidence, not a hunch |

**D — Stolen ID, later revoked:**

| Step | What happens | State |
|---|---|---|
| 1 | An antagonist knocks out a Security Officer, takes ID and PDA | Card and PDA change hands as real items — no hijack mechanic needed |
| 2 | Antagonist uses the Officer's Security access freely | §6 check reads the card in hand, passes exactly as it would for the real Officer |
| 3 | Officer wakes up, reports the theft to HR | Social step — nothing mechanical yet |
| 4 | HoP revokes the stolen card's access at the console | §7 edit, logged; the card stops passing §6 checks the moment the edit lands, not before |

## 11. Integration notes

| Element | Touches existing / needed system |
|---|---|
| Crew record | Lobby's job resolution (`lobby.md` §5) |
| DNA record reference | `death-cloning-respawn.md` §4 |
| ID card gear-strip slot | `main-hud.md` §8 |
| ID-in-PDA equivalence | `pda.md` §2 |
| ID/access summary tab | `pda.md` §3 (this doc is the system it reads) |
| Crew manifest tab | `pda.md` §5, reads this doc's crew record |
| Door default/override requirement | `area.md` §5 |
| Hacking interface permissions panel, three-tier model | `hacking-interface.md` §3–4 |
| Cargo crate locks | `cargo.md` §5 |
| Disposal chute/tagged-destination locks | `disposal.md` §2, §4 |
| AI Area authority and revocation | `ai-cyborgs.md` §3–4 |
| Chem dispenser gate | `chemistry.md` |
| Console diegetic screen pattern | `hacking-interface.md` §2 |
| Physical possession as the gate | `surgery.md` §7 |
| Examine-readable identity | Same convention as crate manifests (`cargo.md` §5) |

Lobby reads each job's access from this doc's §5 table (`lobby.md` §2). Everywhere else that said "the existing ID/access system," this doc is that system.

## 12. Out of scope for this pass

- The full enumerated list of every access level and exactly which device checks which (a content pass, not a design decision)
- Real authentication behind a stable per-player identity — designed in `player-accounts.md` (`persistence-save.md` §5 covers the cross-round job-history/playtime shape itself)
- A standalone, console-independent forging tool ("agent card" or similar) — designed as a Traitor uplink item in `antagonist-content.md` §4
- Photo ID or any biometric verification beyond the printed name/job/department
- Forgery and spoofing through the hacking interface (§8) — waits on `hacking-interface.md`
- Per-gamemode access deltas beyond Traitor's (Head Revolutionary and other undesigned roster entries) — that gamemode's own business, per `antagonist-content.md` §6
- Console siting, count, and station layout (a map-authoring question)
- Exact wording/UX of the "report lost" social flow — it's a comms conversation, not a system

