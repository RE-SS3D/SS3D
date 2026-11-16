# Antagonist Content — design document

> Status: active

Resolves the exact gap four other docs have all pointed at without ever naming it. `round-config.md`
§6 punts "how a gamemode picks its antags from the eligible, opted-in crew" as "that mode's own
business, not round config's." `id-access.md` §5 and §12 defer "gamemode-specific deltas (a Head
Revolutionary needing nothing extra, a traitor being ordinary crew with a hidden objective)" on the
same precedent, and separately flag a "standalone, console-independent forging tool... a plausible
future antag item" (§8, §12). `ai-cyborgs.md` §5 and §12 defer "Malf-AI's actual special-ability
roster (turrets, an APC overload burst, hologram duplicates)" the same way. `objectives.md` §12
defers "uplink or any antag-item-acquisition system... a real and eventually necessary system,
deliberately not assumed or designed here." This doc is that business, for three concrete antagonist
types — Traitor, Malfunctioning AI, and Nuclear Operatives — plus the assignment mechanism every
future antagonist type will need the same way.

## 1. Design philosophy

**SS13's uplink is already good design under an artifact-heavy delivery mechanism** — the same
framing `objectives.md` §1 already applied to the objective system this doc builds on. A hidden
shop, a real countable currency, real physical items: the idea doesn't need replacing. What it's
never had on this project is a home inside systems that already exist.

**Reuse is the entire point of a "toolkit," not a loophole in it.** Nearly everything an antagonist
buys or does in this pass is a restricted-access grant into a system another doc already built —
combat's weapons, explosives' charges, chemistry's reagents, id/access's own flagged forging gap —
not a parallel arsenal invented here. The one thing genuinely new is the shop itself (§4) and the
Malf-AI ability set (§5), and even those are framed as extensions of existing authority (Area
control) and existing delivery patterns (a PDA tab), not new mechanisms.

**One deliberate, bounded exception to "everything maps to something physical."** Lobby, creative
mode, and admin tools each earned a narrow carve-out from that rule because they're meta contexts,
not in-fiction gameplay (`lobby.md` §1, `creative-mode.md` §1, `admin-tools.md` §1). An uplink
purchase materializing an item is the first *in-round, in-fiction* exception this project makes —
flagged explicitly as one, scoped to this single mechanic, not a precedent for anything else (§4).

**Detectable, not prevented, still applies — to what antagonists* do*, not to shopping.** Emagging a
door, deploying a turret, triggering an overload: all real, logged actions, the identical discipline
`id-access.md` §6 and `ai-cyborgs.md` §4 already run. Browsing a hidden shop isn't an action against
the world, so it isn't logged into it — the same distinction `round-config.md` §4 already draws
between the secret itself and what's done with it.

**This pass ships three antagonist types, not the whole roster.** The same restraint `objectives.md`
§1 used for archetypes applies to gamemodes: Traitor, Malfunctioning AI, and Nuclear Operatives are
fully designed here; Revolution, Cult, and others are named as a growable roster (§7), not invented
as placeholders.

**Nuclear Operatives earns its place first among team gamemodes because its loop barely touches the
station.** Every other faction gamemode this project will eventually design (Revolution, Cult)
needs a functioning crew to convert or fight through. This one doesn't — its entire loop (steal a
disk, arm a device, detonate or get defused) is a self-contained infiltration-and-defense problem
that already has almost every piece it needs sitting in other docs: a shuttle (`shuttles.md`), a
timed explosive (`explosives-destruction.md` §6), and a vault access check (`id-access.md` §6). That
makes it a genuinely good first team gamemode to actually build, not just design — the same
practical reasoning `creative-mode.md` §1 already used to justify shipping *some* out-of-fiction
exceptions before others.

## 2. Antagonist assignment — the mechanism round config punted

Given the round's chosen gamemode (`round-config.md` §3) and the players who opted into that
mode's antagonist roles in the lobby (`lobby.md` §4), this doc resolves who actually becomes an
antagonist.

**Selection draws from the opted-in, eligible pool only** — never from someone who didn't opt in,
matching the opt-in section's entire reason for existing. Count and any per-job exclusions are a
content/balancing decision (§11), not fixed here; what this doc fixes is that the draw happens
exactly once, at the same resolution moment lobby's own job-preference pass runs (`lobby.md` §5), and
is a plain random draw among eligible volunteers — not a hidden weighting toward or away from any
particular player.

**Every mode has a minimum.** If fewer players opted in than the mode needs, the round runs as
Extended instead, and admins are told why (`round-config.md` §3). Nobody is ever made an antagonist
without opting in.

**Assignment is what actually fires `objectives.md` §4's own trigger.** That doc already defines
"given an antagonist category, a pool of objective templates resolves into concrete records the
moment that antagonist is assigned" — this section is the moment. Nothing about objective resolution
changes; this doc supplies the event objectives.md was already waiting on.

**Malfunctioning AI resolves differently, against a role instead of a headcount.** If the AI role is
filled and its player opted into the Malf-AI category, drawing that gamemode simply makes that AI the
antagonist — there's no pool to draw from beyond the one AI seat. If no AI is connected, Malf-AI
isn't eligible this round, the same way any gamemode can fail its own precondition (`round-config.md`
§3).

## 3. Traitor

**Later pass.** Traitor's tools come through the uplink (§4), which lives on the PDA (`pda.md`, deferred), so Traitor isn't a playable mode until the PDA exists. Nuclear Operatives (§6) is this doc's first pass.

**An ordinary crew member with a hidden objective set and an uplink.** No ID/access delta exists for
this category — the same disclaimer `id-access.md` §5 already gave it by name. A traitor's job,
department, and access levels are exactly what they'd be if they'd never been picked; the only
things that changed are objectives (`objectives.md` §2, assigned per §2 above) and a new hidden
capability (§4).

**Objectives are drawn from the existing archetypes.** Steal, assassinate, and escape
(`objectives.md` §5–7) apply unmodified — this doc doesn't add a fourth archetype, it's the thing
that actually assigns instances of the three that already exist.

## 4. The uplink

**Later pass**, with the PDA (`pda.md`, deferred).

**A hidden PDA tab, gated identically to the objectives tab.** Reuses `pda.md` §3's tab-bar pattern
and `objectives.md` §8's exact hidden-tab precedent — present only while the PDA is being operated by
a character holding this antagonist status, absent for everyone else, no empty state to stumble
into. This means the same consequence the objectives tab already accepts applies here too: a stolen
PDA-and-ID pair (`pda.md` §9 worked example B, `id-access.md` §9) exposes the uplink to whoever's
holding the real ID, exactly as it would expose the objectives tab — a deliberate consequence of
"physical possession is the gate," not an oversight to patch.

**Telecrystals are a real, logged currency**, spent at the uplink and nowhere else. Every purchase
itemizes on the tab's own transaction list — the same "real, inspectable ledger" discipline
`cargo.md` §4 already runs for its budget, just scoped to one antagonist's own private shop instead
of a shared station budget. Starting balance is a content/balancing number (§11).

**Purchases materialize the item directly** — the one deliberate, bounded exception §1 already
flags. No further mechanic needed: this isn't a delivery system to build, it's a single, contained
break from "everything maps to something physical," confined to this one shop.

**The catalog is a representative slice** (§11), and deliberately leans on existing systems rather
than inventing new items wherever one already fits:

| Item | What it actually is |
|---|---|
| Portable forging tool | The "standalone, console-independent forging tool" `id-access.md` §8 and §12 already flagged as a plausible antag item — writes an arbitrary access bitmask onto a card anywhere, no console needed. Every write still logs, per §8 there; there's no clean, evidence-free version of this tool. |
| Energy weapon | An ordinary dedicated melee weapon in combat's existing tier (`combat.md` §2) — no new damage type, just a purpose-built weapon sold outside normal channels. |
| Breaching charge or timed charge | Existing explosive items, unmodified (`explosives-destruction.md` §6) — sold here, not redesigned here. |
| Restricted reagent | An existing chemistry reagent (`chemistry.md` §2) with a lethal or fast-acting effect profile, sold as a finished dose rather than requiring synthesis. |
| Encrypted channel | A restricted comms channel type (`comms.md` §6) for coordinating with other traitors — reuses the existing channel/radial selection grammar, not a new communication system. |

None of these needed a new mechanic except the forging tool, and that one was already designed in
outline by `id-access.md` §8 — this doc just gives it a place to be sold from.

## 5. Malfunctioning AI

**Later pass.** Malf needs the AI role (`ai-cyborgs.md`, deferred).

**The AI's baseline is unchanged** (`ai-cyborgs.md` §2–§4) — laws, subversion tiers, the driver
interface all apply exactly as that doc already specifies. What Malf-AI adds is a small, named
ability roster, each one framed the same way `ai-cyborgs.md` §5 already frames malf abilities in
general: "larger, illegitimate grants of the same Area authority already defines, reached through
the same bypass path" — just concrete instead of deferred.

- **Turret deployment** — a station-placed automated weapon the Malf-AI can activate in any Area it
  holds authority over (`ai-cyborgs.md` §3). Combat resolution is combat's own ranged model
  unmodified (`combat.md` §3) — the turret is a new *actor* firing an existing weapon type, not a new
  damage mechanic. Activation logs at the console, the identical audit discipline every other AI
  action already writes (`ai-cyborgs.md` §4).
- **APC overload burst** — a deliberate, remote-triggered version of the shorting/overload sabotage
  electricity's own doc already defines (`electricity.md` §6): a real localized electrical fault
  (sparks, a small fire) at the target APC. No new hazard invented — this is that same event,
  reachable without wirecutters because the AI's authority already reaches the Area's power path.
- **Hologram duplicates** — a non-solid, non-interactive projected decoy rendered into any Area the
  AI has camera authority over (`ai-cyborgs.md` §2–§3), visually a second AI presence for scouting or
  distraction. It cannot open doors, speak, or otherwise causally touch anything — the same
  "no physical footprint" restraint `observer.md` §3 already places on a ghost's camera, borrowed
  here for a different kind of non-corporeal presence. A close look reveals it does nothing; that's
  the honest limit of the ability, not a bug to fix later.

**Every ability writes a real log line**, the same discipline every other AI action already commits
to (`ai-cyborgs.md` §4) — Malf-AI's toolkit is bigger, not quieter.

## 6. Nuclear Operatives

**A team gamemode.** A squad of operatives shares one objective — detonate the station's nuclear
device — held as a single objective record by every member of the squad (`objectives.md` §2). It is
not a separate record shape; a team objective is an ordinary objective with several holders.

**The squad never enters the ordinary job pool.** Once the mode is chosen (`round-config.md` §3),
this doc draws the squad from the players who opted into Nuclear Operative (§2) and removes them
from lobby's job resolution before it runs (`lobby.md` §5). An operative doesn't have a crew job to
give up. **The mode has a minimum squad size**; too few opt-ins and the round runs as Extended
instead (`round-config.md` §3).

### First pass

What a Nuclear Operatives round is in this pass:

- **Ops start geared, off the station, on a spawn pad.** Each operative spawns at an operative spawn
  point placed on the map (`creative-mode.md` §2) with a fixed kit: hardsuit and internals, a gun and
  spare magazines (`combat.md` §3), an ID with the access an operative needs, and the tools to load
  and arm the device. No shopping, no starting decisions beyond the plan.
- **A pinpointer finds the disk.** Every operative carries a pinpointer that points at the
  authentication disk wherever it is — in the vault, in a crew member's pocket, on the floor. The
  crew know the ops can track it, so moving the disk is a real defensive play, not a way to make the
  round unwinnable.
- **The authentication disk is a team steal-target.** Sited in the station's vault (access-gated the
  ordinary way, `id-access.md` §6), it's a possession check like `objectives.md` §5's Steal, with any
  operative — or the device itself once loaded — counting as a valid holder.
- **The device is an ordinary timed charge with one added precondition.** `explosives-destruction.md`
  §6's timed charge — arm, visible countdown, defuse — applies unmodified once the disk has been
  physically loaded into the device. Loading the disk takes a short, interruptible action; nothing is
  lost if it's interrupted.
- **Arming is announced.** The moment the device arms, a **station-wide announcement** tells
  everyone a nuclear device is armed, and where. The crew always get a fair chance to respond. This
  is a comms announcement (`comms.md` §8), not a HUD element.
- **Once armed, there is exactly one way to stop it.** The standard defuse interaction
  (`explosives-destruction.md` §6) — a tool check against a visible countdown. Pulling the disk back
  out after arming does not stop the clock.
- **Examining the device shows its state** — whether the disk is loaded, whether it's armed, and the
  time left (`examine.md` §5).

### How the round ends

Every Nuclear Operatives round has a winner (`round-end.md` §2):

| What happens | Outcome |
|---|---|
| The device detonates | **Operatives win.** The round ends at once. |
| Every operative is dead or restrained | **Crew win.** The round ends at once. |
| The round timer runs out with the disk still on the station | **Crew win.** |
| The round timer runs out with the disk held by an operative | **Minor operative win.** They got the disk but not the bang. |

**A defuse doesn't end the round by itself.** It removes the immediate threat, but the operatives
can re-arm if they still have the disk. The crew finish the job by dealing with the operatives — or
by holding out with the disk until the timer runs out.

### Later passes

- **An operative shuttle.** The squad starts on their own shuttle off-station and flies in, reusing
  the shuttle framework (`shuttles.md`), instead of starting on a spawn pad.
- **The uplink and telecrystals.** Each operative gets an uplink (§4) and a starting balance to
  choose their own gear, instead of a fixed kit.

## 7. Roster — what's here, what's growable

Traitor, Malfunctioning AI, and Nuclear Operatives are fully designed. Named, explicitly not
designed this pass: **Revolution**, **Cult**, **Blob**, and **Wizard** — plausible future entries in
round config's gamemode pool (`round-config.md` §2), each its own future pass through this same
shape (§2's assignment mechanism, an objective or team-goal set, whatever antagonist-specific
capability each needs). None of them are placeholder-designed here; naming them is just honest
scope, the same way `objectives.md` §12 named sabotage and protect as plausible future archetypes
without inventing stubs for either.

## 8. HUD & touchpoints

No new permanent chrome.

- The uplink tab renders exactly like every other PDA tab (`pda.md` §7) — on-demand, momentary,
  nothing added to any always-on layout.
- Turret and overload-burst feedback render on the world objects themselves (a turret's aim/fire
  animation, sparks and fire at an overloaded APC) — no HUD overlay.
- Hologram duplicates render as an ordinary AI presence through the camera network
  (`ai-cyborgs.md` §8) — no special-cased UI.
- The device's countdown and the disk's insertion state render on the object itself, same "let the
  world carry the information" rule every other timed charge already follows
  (`explosives-destruction.md` §7) — no separate nuclear-specific HUD. Arming is announced
  station-wide (§6), which is comms, not HUD.
- Operatives have no friend-or-foe overlay; they recognise each other by their gear (`combat.md` §6).

## 9. Worked examples

**A — A traitor buys a way past a locked door:**

| Step | What happens | State |
|---|---|---|
| 1 | Round config draws Traitor; a crew member who opted in is selected (§2) | Objectives assigned per `objectives.md` §4 |
| 2 | They open their PDA's newly-visible uplink tab | Telecrystal balance and catalog shown, nobody else can see this tab |
| 3 | They buy the portable forging tool | Purchase itemizes on the uplink's own ledger |
| 4 | They use it on a restricted door's access requirement | Write succeeds; the door opens |
| 5 | Security later audits the door's log | Sees an edit with no matching legitimate operator session — real evidence, per `id-access.md` §8 |

**B — Malf-AI escalates after being suspected:**

| Step | What happens | State |
|---|---|---|
| 1 | Round config draws Malfunctioning AI; the connected AI player opted in | AI's baseline authority unchanged |
| 2 | A crew member grows suspicious and reviews the console's audit log | Sees a law-change entry outside the normal access pattern (`ai-cyborgs.md` §5) |
| 3 | The AI deploys a turret in the Area they're approaching from | Combat resolves through the ordinary ranged model; the deployment itself logs |
| 4 | Crew responds with access revocation | Same containment tool `ai-cyborgs.md` §4 already defines, unchanged by this doc |

**C — A stolen PDA exposes an uplink:**

| Step | What happens | State |
|---|---|---|
| 1 | An unrelated crew member knocks out a traitor and takes their PDA and ID | Ordinary physical theft, no hijack mechanic (`pda.md` §9) |
| 2 | They open the PDA | ID/access summary reads the real ID as normal; the uplink tab is also visible, since it reads the same inserted identity |
| 3 | They now hold telecrystals and a catalog that were never theirs | A real, physical consequence of theft, the same "possession is the gate" rule as everywhere else — not a special case for this one tab |

**D — A successful nuclear operation:**

| Step | What happens | Team objective state |
|---|---|---|
| 1 | Nuclear Operatives is chosen; a squad is drawn from the opted-in pool and removed from lobby's job resolution (§6) | Squad spawns geared on their spawn pad, off-station |
| 2 | Following their pinpointers, the squad breaches into the vault (`explosives-destruction.md` §6) | Disk still on-station |
| 3 | An operative takes the disk | Team objective's possession check now reads true for the squad |
| 4 | They return to the device and load the disk in | Arming's precondition is met |
| 5 | Arming completes | Countdown starts; a station-wide announcement goes out; only the standard defuse interaction can stop it now |
| 6 | Countdown reaches zero | Detonation fires `round-end.md` §2's gamemode-end-condition trigger; round ends |

**E — Crew defuses in time:**

| Step | What happens | Team objective state |
|---|---|---|
| 1 | Same as example D through arming | Countdown running |
| 2 | An engineer reaches the device before it reaches zero | Applies the standard defuse interaction (`explosives-destruction.md` §6) |
| 3 | Defuse succeeds | The immediate threat is gone; the round does not end, since the ops could re-arm |
| 4 | Security corners and restrains the last operative | Every operative dead or restrained — crew win (§6), round ends |

## 10. Integration notes

| Antagonist-content element | Touches existing / needed system |
|---|---|
| Antagonist assignment and minimums | `round-config.md` §3, lobby's opt-in (`lobby.md` §4, §5) |
| Objective assignment trigger | `objectives.md` §4 |
| Traitor's (lack of) access delta | `id-access.md` §5 |
| Uplink tab | `pda.md` §3, `objectives.md` §8's hidden-tab precedent |
| Portable forging tool | `id-access.md` §8, §12 |
| Uplink weapon/explosive/reagent/channel items | `combat.md` §2, `explosives-destruction.md` §6, `chemistry.md` §2, `comms.md` §6 |
| Uplink transaction ledger | Same discipline as `cargo.md` §4 |
| Malf-AI turret | `ai-cyborgs.md` §3, `combat.md` §3 |
| Malf-AI overload burst | `electricity.md` §6 |
| Malf-AI hologram | `ai-cyborgs.md` §2–§3, `observer.md` §3's no-physical-footprint precedent |
| Malf-AI audit logging | `ai-cyborgs.md` §4 |
| Nuclear Operative squad assignment | §2's mechanism, generalized to a squad; removed from lobby's job pool (`lobby.md` §5, §7) |
| Nuclear Operative spawn point | Placed in the map editor (`creative-mode.md` §2); checked before start (`creative-mode.md` §8) |
| Disk pinpointer, arm announcement | `comms.md` §8 (announcement) |
| Crew win condition | `round-end.md` §2 |
| Authentication disk | Team steal-target, generalized `objectives.md` §5; vault access (`id-access.md` §6) |
| Nuclear device | `explosives-destruction.md` §6 (timed charge, unmodified) with a disk-loaded precondition; state on examine (`examine.md` §5) |
| Detonation as round trigger | `round-end.md` §2's existing gamemode-end-condition bullet |


## 11. Out of scope for this pass

- **Revolution, Cult, Blob, Wizard, and any other gamemode roster entry** (§7) — each its own future
  pass through this doc's assignment shape.
- **Exact telecrystal starting balance, item prices, and antagonist-count ratio** — a
  balancing/content pass, not a design decision.
- **The full uplink catalog beyond the representative slice in §4** — a content pass.
- **Any exclusion rules for who's eligible to be drawn** (e.g., excluding head-of-staff roles) — a
  server-policy/balancing choice, not fixed here.
- **A disguise/chameleon item type** — plausible future uplink content, would need its own real
  appearance-change mechanic this pass doesn't invent.
- **Nuclear Operative squad size, device timer duration, and disk/vault siting** — a
  balancing/map-authoring pass, not a design decision.
- **The Nuclear Operative shuttle and uplink** — later passes (§6).
- **Ship-to-ship combat or a hostile response shuttle** — `shuttles.md` §12 already flags this as not
  a base mechanic; this pass doesn't add one for Nuclear Operatives specifically either.
- **Malfunctioning AI as a playable mode** — designed in §5, but it needs the AI role, and
  `ai-cyborgs.md` is deferred.
