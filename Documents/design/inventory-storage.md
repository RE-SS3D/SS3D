# Inventory & storage — design document

> Status: active

Resolves a gap this project has been quietly assuming shut for a while: main HUD's gear strip (`main-hud.md` §8) reserves belt/ID/pocket/back slots and explicitly defers "the full inventory/backpack screen" to later (§13); armor's equip-slot UI points at that same deferred screen (`armor.md` §5); crafting and cargo both talk about being "wired to real inventory" as if the wiring already exists (`crafting.md` §9, `cargo.md` §11). This doc is that wiring.

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
- **Weight** — the sum of every contained item's own weight, including everything inside nested containers (§7). Always the true total. Weight never decides what fits (§4); its job is stamina (§10).
- **Lock** — optional. When present, reuses the ID/access check (§8), same as a cargo crate or a disposal chute.
- **Where it lives** — a container is either worn in an equip slot, held in a hand, sitting as a freestanding world object, or nested inside another container. Same object, different context, no separate code path per case.

Cargo's crate (`cargo.md` §5) and disposal's locked units (`disposal.md` §2) already independently built pieces of this — a crate is a Container with a lock and a printed-manifest examine string. Nothing about those docs needs to change functionally; they're just describable as instances of this one primitive now rather than parallel one-off objects.

## 3. Equip slots

Two different things, easy to conflate:

- **Equip slots** — fixed, named mounts on the character, one item each: head, mask, eyes, ears, uniform, suit (outerwear/armor), suit storage, gloves, shoes, back, belt, ID, plus two always-present pocket slots. **Suit storage exists only while a suit is worn** and holds a full gas tank, so a suited player keeps their backpack (`armor.md` §3). **The ID slot holds a bare ID card or a PDA** (with a card inside it, `id-access.md` §3) — there's no separate PDA slot. Each slot only accepts items tagged for its category. Most render visibly on the character model the moment they're worn — a helmet, gloves, a jumpsuit are visible the same way armor's wear already is (`armor.md` §6), because the model is the doll now, per main HUD §1.
- **Containers** — the multi-item holding capacity from §2. Several equip slots exist specifically to hold one: **back** holds a backpack, **belt** can hold a tool belt (itself a small Container), and the two **pocket** slots are just small built-in Containers that exist regardless of what's worn — no uniform-specific pocket variance modeled this pass (§13).

Quick-access subset: hands, belt, ID, a pocket, and back stay visible as gear-strip icons (`main-hud.md` §8). The rest (head, mask, eyes, ears, uniform, suit, suit storage, gloves, shoes) don't get gear-strip icons — they're equipped and removed through the storage panel (§6).

## 4. Size class & fit

This doc is the natural home for a size-class taxonomy other docs have already assumed without defining. Disposal's chute keys its accept check off "physical size" / "collider class" without naming tiers (`disposal.md` §2). This doc names them, and disposal should read this same field once both are implemented — one property on an item, two consumers, not a redefinition per system.

**Five tiers:** tiny (a pen, a coin), small (a multitool, a flashlight, an emergency gas tank), normal (most handheld tools and weapons), bulky (a fire extinguisher, a toolbox), huge (a full oxygen tank, a stretcher).

**Fit check:** a container declares the largest size class it accepts and a number of slots. An item fits if its size class is at or below that ceiling **and** there's a free slot — flat, binary, no partial-fit fuzziness, and no weight limit. A satchel capped at small can't take a fire extinguisher no matter how many empty slots it has; the extinguisher goes in a hand or on the back instead. Same "physical logic over hidden math" instinct armor's absorption values already run on.

`disposal.md` §2 reads this same five-tier field for its chute accept-check.

## 5. Stacking

Identical stackable items (metal sheets, ammunition, medication) collapse into a single slot entry with a count, the same counted-unit treatment cargo's manifests already use — *"5× Advanced Medkit"* (`cargo.md` §5). A stack still contributes its full combined weight. Splitting or combining stacks is an ordinary drag, not a separate stack-management widget.

## 6. Storage UI — the deferred panel

This is what main HUD §13 left out.

**One shared panel type, opened per-container.** Opening a container — clicking its gear-strip icon, or interacting with a world container like a locker or a crate — opens a lightweight panel anchored near where it was opened from: a grid of slots showing icons, a weight/capacity readout at the top, and highlighting during a drag that shows where an item fits. A world container with a door (a locker) only shows its panel while the door is open — close the door and the panel closes with it. Same flat-surface, hairline-border visual language as every other screen in this project — but it's a plain UI panel, not a diegetic device screen. Nothing physical justifies treating a bag like a terminal; it just follows the same restrained grammar so it doesn't read as a bolted-on app.

**Opening a second container opens a second panel alongside the first**, not a replacement — this is what actually makes looting and transferring work. Rummaging a locker while your own backpack is open shows both panels side by side; dragging an item from one panel to the other moves it, the same way dragging onto a hand or a gear-strip icon does (`main-hud.md` §8). No hard cap on panels open at once, but in practice most interactions only ever need two.

**Click to take, click to put, hold to drag.** A plain click on an item in a panel takes it into the active hand, if that hand is empty. With something held, a click on an empty slot puts the held item there, if it fits. Anything else does nothing: a click with a full hand never swaps. This is what a click on a HUD slot already does. Pressing and moving the pointer is a drag, as above. A double-click on a container item opens it (§7); its single click waits a moment for that second click before taking it.

**Closes when not in use.** No panel is ever pinned open — walking away from a world container, or unequipping your own worn one, closes its panel, consistent with main HUD's minimal-permanent-chrome rule (§1 rule 3). Your own equipped containers reopen instantly from the gear strip at any time; closing never means losing access.

## 7. Nested containers

Full recursive nesting: a box can sit inside a backpack, and another box can sit inside that box. A few things fall out cleanly:

- **Weight is always the true total.** A backpack's weight is its own empty weight plus everything inside it, however deeply nested.
- **Capacity is per-container, not shared down the chain.** A backpack with 6 slots and a box with 4 slots inside it still only costs the backpack one slot — the box itself. The box's own 4 slots are separate capacity, not borrowed from the backpack. This is what makes nesting a real packing strategy instead of a no-op.
- **Opening is sequential, not X-ray.** Opening a backpack shows a nested box as a single icon; you have to open the box (double-click it, and another panel opens alongside, per §6) to see what's inside it. No panel ever shows contents more than one level deep at once — keeps §6's UI legible instead of becoming a nested tree view.
- **Locking is independent of depth.** A locked evidence box can nest inside an unlocked bag and vice versa — the lock is a per-container property (§8), not inherited or required up or down the chain.

**Fit still gates by size and slots**, same as anywhere else (§4): a box goes into a bag only if the box's own size class fits. Its contents' weight adds to what the carrier hauls (§10), not to whether it fits.

**A storage item only nests into a strictly larger storage item.** A toolbox (bulky) can hold a medkit (normal) but never another toolbox, and a medkit can't hold a medkit. Every step down the chain drops a size tier, so nesting stays a packing strategy and never becomes an endless toolbox-in-a-toolbox hoard. Hands, worn slots, and freestanding containers like lockers aren't storage items, so they take any storage item that fits (§4).

## 8. Locked storage

Personal worn containers (a backpack, pockets, a belt) aren't lockable this pass — there's no crew workflow that needs your own backpack to have a combination, and adding one would just be friction. **Freestanding world containers** — station lockers, safes, evidence boxes — reuse the identical ID-locked pattern cargo's crates and disposal's secure chutes already established (`cargo.md` §5, `disposal.md` §2): present an ID with the right access (`id-access.md` §3) and it opens. Crew see "locked" on examine. Picking or bypassing a lock is a hacking-interface job and a later pass (`hacking-interface.md`, deferred).

## 9. Picking up, dropping, and taking from others

**World items** are picked up into the active hand with a plain click, same as the existing two-hand system (`main-hud.md` §2) — no separate "loot" verb.

**Dropping** places an item at the character's feet as a real world object; dropping onto an open container's panel (§6) inserts it there instead.

**Taking from another character** works on **anyone** you can reach, as in SS13 — a corpse, an unconscious crew member, someone restrained, or someone standing in front of you. You open their **Search** view (`examine.md` §4), which lays out what they visibly wear and hold, and pick a slot to take from:

- **It takes time** — a few seconds of visibly rummaging, interrupted if either of you moves away.
- **A conscious victim is warned** the moment you start ("someone is reaching into your pocket") and can simply step away or fight back. Taking from a willing, sleeping, restrained or dead character is easy; taking from an alert one is a contest they can see coming.
- **No hidden roll.** If you finish the take, you have the item.

## 10. Weight & encumbrance

Total carried weight — everything equipped, held, and in every worn container — feeds stamina (`stamina.md` §2): it lowers both the stamina cap and its regen. Not a separate "encumbrance" system: armor's weight and storage's weight are the same number (`armor.md` §4). A stuffed backpack costs mobility the same honest way a heavy suit already does.

## 11. HUD touchpoints

No new permanent chrome — same discipline as every doc in this project.

- Gear-strip icons (hands, belt, ID, pocket, back) are unchanged from `main-hud.md` §8; they're the entry point into opening those slots' panels, nothing added to the always-on layout.
- The storage panel (§6) is strictly on-demand and momentary, per main HUD §1 rule 3.
- The weight/capacity readout lives at the top of an open panel, not as a persistent HUD element — consistent with armor's own choice not to give equipped weight a permanent readout either (`armor.md` §6).
- Worn armor and clothing continue to render on the model exactly as armor §6 already specifies; this doc doesn't change that, just supplies the equip-slot data it was always assumed to read from.

## 12. Worked examples

**A — Packing for an away mission:**

| Step | What happens | Storage state |
|---|---|---|
| 1 | Engineer opens their backpack from the gear strip | Panel opens: 6 slots, small-max, currently empty |
| 2 | Drags a toolbox (bulky) toward the bag | Highlight shows an invalid target — exceeds the bag's size ceiling; toolbox has to go in a hand instead |
| 3 | Drags a spare O2 tank (huge) the same way | Same rejection — a full tank rides in suit storage or a hand, not the bag |
| 4 | Packs six small tools instead | All six fit; weight readout climbs, still under their stamina-affecting threshold |
| 5 | Player checks their load | Stamina cap is marginally lower than empty-handed — same honest cost armor's weight already applies |

**B — Looting a locker with a nested box:**

| Step | What happens | Storage state |
|---|---|---|
| 1 | Security officer finds an unlocked locker, opens it | Locker's panel opens alongside their own already-open backpack panel |
| 2 | Locker contains a small lockbox | Shown as a single icon — contents not visible yet, one level deep only (§7) |
| 3 | Officer opens the lockbox | A third panel opens; it's ID-locked, their Security access passes the check (§8) |
| 4 | Officer drags a stack of zipties from the lockbox into their belt | An ordinary drag between two open panels; stack count and weight update on both containers immediately |

**C — Restraining and searching a suspect:**

| Step | What happens | Storage state |
|---|---|---|
| 1 | Officer grabs and restrains a suspect (Alt+click escalation, `main-hud.md` §7) | Suspect is restrained, no separate "search" mechanic needed |
| 2 | Officer opens the suspect's Search view | Their visibly worn and held items laid out slot by slot (§9) |
| 3 | Officer takes from a pocket; a few seconds pass | The restrained suspect is warned but can't pull away; the contraband lands in the officer's hand — no roll |

## 13. Integration notes

| Inventory element | Touches existing / needed system |
|---|---|
| Equip slots, gear-strip subset | `main-hud.md` §8; combined ID/PDA slot `id-access.md` §3 |
| Container weight → stamina | `stamina.md` §2, `armor.md` §4 |
| Worn item rendering | `armor.md` §6, main HUD §1's "world carries the info" rule |
| Locked containers | ID/access check (`id-access.md` §6) |
| Cargo crates as Container instances | `cargo.md` §5 |
| Disposal chute size check | `disposal.md` §2 |
| Taking items from another character | Search view (`examine.md` §4) |
| Stacking | `cargo.md` §5's counted-unit manifest precedent |

## 14. Out of scope for this pass

- Exact slot counts, size-class ceilings, and weight values per container (a balancing pass, not a design decision — same treatment armor and cargo already gave their own numbers)
- Uniform-specific pocket variance (a uniform with more or fewer pockets than the standard two) — the built-in pocket slots are uniform this pass
- Item degradation/durability for non-armor items sitting in storage
- A standalone "quick loot everything" bulk-transfer action — a plausible UX convenience, not load-bearing for the system's shape
- Persistence of container contents across rounds — round snapshots are the mechanism (`persistence-save.md` §6), but the exact captured shape of item/container state is this doc's own future work, not that doc's
- Visual bulge/silhouette changes on a character model based on how full their bag is — a nice-to-have, not required for the panel-based approach to work
- Lock picking and bypass (`hacking-interface.md`, deferred)
