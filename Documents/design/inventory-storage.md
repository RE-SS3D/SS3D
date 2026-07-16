# Inventory & storage — design document

> Status: active

Resolves a gap this project has been quietly assuming shut for a while: main HUD's gear strip (`main-hud.md` §8) reserves belt/ID/PDA/back slots and explicitly defers "the full inventory/backpack screen" to later (§13); armor's equip-slot UI points at that same deferred screen (`armor.md` §5); crafting and cargo both talk about being "wired to real inventory" as if the wiring already exists (`crafting.md` §9, `cargo.md` §11). This doc is that wiring.

## 1. Design philosophy

The same test as everywhere else: is a piece of this genuinely good design, or a BYOND-era abstraction that survived out of habit? Two things about SS13 storage pass cleanly, and one thing needs the same treatment the PDA already got.

- **Physical carrying, hands, and equip slots are good design.** Nothing here is a 2D-engine leftover — keep it, the same way main HUD §2 already kept two-hand item juggling as core gameplay, not legacy.
- **A closed bag genuinely hides its contents from the world.** Unlike a wound or a crate's printed manifest, there's no plausible way for the character model to carry "here's every item inside this backpack." That's the exact test the PDA doc already applied to message logs and recipe lists: *"before something becomes a [panel], it should already have failed the can-the-world-carry-this test"* (`pda.md` §1). Storage contents fail that test the same way. A panel here isn't an artifact surviving out of habit — it's the same legitimate exception PDA already carved out, just for a different kind of information.
- **A pile of parallel storage systems is the part that needs fixing.** SS13, and this project's own docs so far, treat backpacks, boxes, belts, pockets, lockers, and crates as separate mechanics that happen to look similar. That's exactly the shape this project keeps collapsing elsewhere — one BFS technique instead of four, one raycast system instead of six. Storage gets the same treatment.

Two rules fall out:

1. **One Container primitive, every consumer.** A backpack, a tool belt, a pocket, a supply crate, a station locker — all the same data shape, differing only in capacity and where they're worn or placed (§2).
2. **The panel is on-demand and minimal, never permanent chrome** — same discipline as everything else (main HUD §1 rule 3). It opens when a container is opened and closes when it isn't in use; it never competes with the always-on HUD.

## 2. The Container primitive

One shared shape underneath every place that can hold items:

- **Capacity** — a slot count, and a single max size-class the container accepts (§4). A satchel might be 6 slots, small-max; a supply crate might be 20 slots, bulky-max.
- **Contents** — a list of item references. Identical stackable items collapse into one slot entry with a count (§5).
- **Weight** — computed, not stored: the sum of every contained item's own weight, plus, recursively, the total weight of anything inside a nested container (§7). Always current, never a cached number that can drift — the same discipline cargo's ledger applies to credits applies here to weight.
- **Lock** — optional. When present, reuses the existing ID/access check and the FDU's device-lock taxonomy wholesale (§8), same as a cargo crate or a disposal chute.
- **Where it lives** — a container is either worn in an equip slot, held in a hand, sitting as a freestanding world object, or nested inside another container. Same object, different context, no separate code path per case.

Cargo's crate (`cargo.md` §5) and disposal's locked units (`disposal.md` §2) already independently built pieces of this — a crate is a Container with a lock and a printed-manifest examine string. Nothing about those docs needs to change functionally; they're just describable as instances of this one primitive now rather than parallel one-off objects (§14).

## 3. Equip slots

Two different things, easy to conflate:

- **Equip slots** — fixed, named mounts on the character, one item each: head, mask, eyes, ears, uniform, suit (outerwear/armor), gloves, shoes, back, belt, ID, PDA, plus two always-present pocket slots. Each slot only accepts items tagged for its category. Most render visibly on the character model the moment they're worn — a helmet, gloves, a jumpsuit are visible the same way armor's wear already is (`armor.md` §6), because the model is the doll now, per main HUD §1.
- **Containers** — the multi-item holding capacity from §2. Several equip slots exist specifically to hold one: **back** holds a backpack, **belt** can hold a tool belt (itself a small Container), and the two **pocket** slots are just small built-in Containers that exist regardless of what's worn — no uniform-specific pocket variance modeled this pass (§13).

Quick-access subset: hands, belt, ID, PDA, and back stay visible as gear-strip icons exactly as main HUD §8 already committed to. The rest (head, mask, eyes, ears, uniform, suit, gloves, shoes, pockets) don't get gear-strip icons — they're equipped and removed through the storage panel (§6) or by dragging directly onto the character model, same Tier 3 combine grammar as everything else.

## 4. Size class & fit

This doc is the natural home for a size-class taxonomy other docs have already assumed without defining. Disposal's chute keys its accept check off "physical size" / "collider class" without naming tiers (`disposal.md` §2). This doc names them, and disposal should read this same field once both are implemented — one property on an item, two consumers, not a redefinition per system.

**Five tiers:** tiny (a pen, a coin), small (a multitool, a flashlight), normal (most handheld tools and weapons), bulky (a fire extinguisher, a toolbox), huge (a full oxygen tank, a stretcher).

**Fit check:** a container declares the largest size class it accepts. An item fits if its size class is at or below that ceiling — flat, binary, no partial-fit fuzziness. A satchel capped at small can't take a fire extinguisher no matter how many empty slots it has; the extinguisher goes in a hand or on the back instead. Same "physical logic over hidden math" instinct armor's absorption values already run on.

**Companion edit flagged:** `disposal.md` §2 should reference this same five-tier field for its chute accept-check rather than an unnamed "collider class" (§14).

## 5. Stacking

Identical stackable items (metal sheets, ammunition, medication) collapse into a single slot entry with a count, the same counted-unit treatment cargo's manifests already use — *"5× Advanced Medkit"* (`cargo.md` §5). A stack still contributes its full combined weight. Splitting or combining stacks is a Tier 3 combine action like everything else, not a separate stack-management widget.

## 6. Storage UI — the deferred panel

This is what main HUD §13 left out.

**One shared panel type, opened per-container.** Opening a container — clicking its gear-strip icon, or interacting with a world container: a locker, a crate, a dead crew member's gear — opens a lightweight panel anchored near where it was opened from: a slot grid showing icons, a weight/capacity readout at the top, size-class-gated highlighting during a drag. Same flat-surface, hairline-border visual language as every other screen in this project — but it's a plain UI panel, not a diegetic device screen. Nothing physical justifies treating a bag like a terminal; it just follows the same restrained grammar so it doesn't read as a bolted-on app.

**Opening a second container opens a second panel alongside the first**, not a replacement — this is what actually makes looting and transferring work. Rummaging a locker while your own backpack is open shows both panels side by side; dragging an item from one to the other is the same Tier 3 combine grammar already used for hands and the gear strip (`main-hud.md` §8), just with a container's grid slot as the drop target instead of a hand or gear-strip icon. No hard cap on panels open at once, but in practice most interactions only ever need two.

**Closes when not in use.** No panel is ever pinned open — walking away from a world container, or unequipping your own worn one, closes its panel, consistent with main HUD's minimal-permanent-chrome rule (§1 rule 3). Your own equipped containers reopen instantly from the gear strip at any time; closing never means losing access.

## 7. Nested containers

Full recursive nesting: a box can sit inside a backpack, and another box can sit inside that box. A few things fall out cleanly because §2 already made weight and capacity computed, not cached:

- **Weight is always the true recursive sum.** A backpack's weight is its own empty weight plus every contained item's weight, and a contained container's weight is *its* empty weight plus its own recursive contents — the same function called on itself, no special-casing depth.
- **Capacity is per-container, not shared down the chain.** A backpack with 6 slots and a box with 4 slots inside it still only costs the backpack one slot — the box itself. The box's own 4 slots are separate capacity, not borrowed from the backpack. This is what makes nesting a real packing strategy instead of a no-op.
- **Opening is sequential, not X-ray.** Opening a backpack shows a nested box as a single icon; you have to open the box (another panel alongside, per §6) to see what's inside it. No panel ever shows contents more than one level deep at once — keeps §6's UI legible instead of becoming a nested tree view.
- **Locking is independent of depth.** A locked evidence box can nest inside an unlocked bag and vice versa — the lock is a per-container property (§8), not inherited or required up or down the chain.

**Weight-based fit still gates everything**, same as anywhere else: dropping a heavy nested box into a bag doesn't bypass §4/§5, it just adds its full recursive weight to the outer container's total the moment it's placed inside.

## 8. Locked storage

Personal worn containers (a backpack, pockets, a belt) aren't lockable this pass — there's no crew workflow that needs your own backpack to have a combination, and adding one would just be friction. **Freestanding world containers** — station lockers, safes, evidence boxes — reuse the identical ID-locked pattern cargo's crates and disposal's secure chutes already established (`cargo.md` §5, `disposal.md` §2): another entry in the FDU's device taxonomy (`hacking-interface.md` §3), not a bespoke lock system. Crew see "locked," a hacker sees the raw access bitmask, same as every other locked device in this project.

## 9. Picking up, dropping, and taking from others

**World items** are picked up into the active hand with a plain click, same as the existing two-hand system (`main-hud.md` §2) — no separate "loot" verb.

**Dropping** places an item at the character's feet as a real world object; dropping onto an open container's panel (§6) inserts it there instead, via the same Tier 3 drag.

**Taking from another character** — a corpse, an unconscious crew member, or someone currently grabbed/restrained (`main-hud.md` §7) — opens their equip slots and worn containers as panels exactly like any other container, subject to the same physical-possession-is-the-gate rule the ID/access and surgery docs already established (`id-access.md` §3, `surgery.md` §7): if you can physically reach their gear, you can take it. No separate theft roll, no hidden check.

## 10. Weight & encumbrance

Total carried weight — everything equipped plus everything in every worn container, computed recursively per §7 — becomes a second input into the exact stamina function armor's equipped weight already feeds (`stamina.md` §2, `armor.md` §4). Not a new mechanic, not a separate "encumbrance" system: armor's weight term and storage's weight term are the same number, just with more contributors now. A stuffed backpack costs mobility the same honest way a heavy suit already does.

## 11. HUD touchpoints

No new permanent chrome — same discipline as every doc in this project.

- Gear-strip icons (hands, belt, ID, PDA, back) are unchanged from `main-hud.md` §8; they're the entry point into opening those slots' panels, nothing added to the always-on layout.
- The storage panel (§6) is strictly on-demand and momentary, per main HUD §1 rule 3.
- The weight/capacity readout lives at the top of an open panel, not as a persistent HUD element — consistent with armor's own choice not to give equipped weight a permanent readout either (`armor.md` §6).
- Worn armor and clothing continue to render on the model exactly as armor §6 already specifies; this doc doesn't change that, just supplies the equip-slot data it was always assumed to read from.

## 12. Worked examples

**A — Packing for an away mission:**

| Step | What happens | Storage state |
|---|---|---|
| 1 | Engineer opens their backpack from the gear strip | Panel opens: 6 slots, small-max, currently empty |
| 2 | Drags a toolbox (bulky) toward the bag | Highlight shows an invalid target — exceeds the bag's size ceiling; toolbox has to go in a hand instead |
| 3 | Drags a spare O2 tank (huge) the same way | Same rejection — the tank occupies the back equip slot directly, not the bag |
| 4 | Packs six small tools instead | All six fit; weight readout climbs, still under their stamina-affecting threshold |
| 5 | Player checks their load | Stamina cap is marginally lower than empty-handed — same honest cost armor's weight already applies |

**B — Looting a locker with a nested box:**

| Step | What happens | Storage state |
|---|---|---|
| 1 | Security officer finds an unlocked locker, opens it | Locker's panel opens alongside their own already-open backpack panel |
| 2 | Locker contains a small lockbox | Shown as a single icon — contents not visible yet, one level deep only (§7) |
| 3 | Officer opens the lockbox | A third panel opens; it's ID-locked, their Security access passes the check (§8) |
| 4 | Officer drags a stack of zipties from the lockbox into their belt | Standard Tier 3 drag between two open panels; stack count and weight update on both containers immediately |

**C — Restraining and searching a suspect:**

| Step | What happens | Storage state |
|---|---|---|
| 1 | Officer grabs and restrains a suspect (Alt+click escalation, `main-hud.md` §7) | Suspect is restrained, no separate "search" mechanic needed |
| 2 | Officer opens the suspect's gear strip | Their equip slots and worn containers open as panels, same as any other container (§9) |
| 3 | A contraband item turns up in a pocket | Physically dragged out into the officer's own hand — no roll, no hidden check, exactly as visible as anything else this project does |

## 13. Out of scope for this pass

- Exact slot counts, size-class ceilings, and weight values per container (a balancing pass, not a design decision — same treatment armor and cargo already gave their own numbers)
- Uniform-specific pocket variance (a uniform with more or fewer pockets than the standard two) — the built-in pocket slots are uniform this pass
- Item degradation/durability for non-armor items sitting in storage
- A standalone "quick loot everything" bulk-transfer action — a plausible UX convenience, not load-bearing for the system's shape
- Persistence of container contents across rounds (persistence & accounts' job, cross-cutting infra, same carve-out every other doc gives it)
- Visual bulge/silhouette changes on a character model based on how full their bag is — a nice-to-have, not required for the panel-based approach to work

## 14. Companion edits flagged

- `disposal.md` §2 — its chute accept-check should reference this doc's five-tier size-class field (§4) rather than an unnamed "collider class."
- `main-hud.md` — several existing docs (`cargo.md` §5, `disposal.md` §4/§9, `surgery.md` §9, `id-access.md` §3) already cite an "examine system" at main HUD §15 that isn't present in the current main HUD doc (which currently ends at §14). Not this doc's gap to fix, but worth closing — this doc's own examine-adjacent behavior (crate-style manifests, worn-item visibility) assumes it exists too.
- `cargo.md` §5 and `disposal.md` §2 — both crates and locked disposal units are describable as instances of this doc's Container primitive (§2). No functional change needed in either doc; flagging only so future lockable containers get built against this shared shape instead of a third one-off.

## 15. Integration notes

| Inventory element | Touches existing / needed system |
|---|---|
| Equip slots, gear-strip subset | `main-hud.md` §8 |
| Drag/insert interaction | Tier 3 combine/use-with grammar (radial menu, per `main-hud.md` §8) |
| Container weight → stamina | `stamina.md` §2, `armor.md` §4 |
| Worn item rendering | `armor.md` §6, main HUD §1's "world carries the info" rule |
| Locked containers | ID/access check + FDU device taxonomy (`id-access.md` §6, `hacking-interface.md` §3) |
| Cargo crates as Container instances | `cargo.md` §5 |
| Disposal chute size check | `disposal.md` §2 (companion edit, §14) |
| Taking items from another character | `id-access.md` §3, `surgery.md` §7 (physical possession as the gate) |
| Stacking | `cargo.md` §5's counted-unit manifest precedent |

