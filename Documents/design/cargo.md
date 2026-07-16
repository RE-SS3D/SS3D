# Cargo — design document

> Status: active

Resolves a gap three other docs deliberately left as a black box: `shuttles.md` §12 flags "cargo economy specifics" as assumed-to-exist infrastructure, `crafting.md` treats the material silo the same way, and `pda.md` §10 flags "cargo/requisition ordering... a plausible future hook once an economy/ordering system exists." This doc is that system. Builds on the shuttle framework (`shuttles.md`) for delivery, the diegetic device-screen pattern (`hacking-interface.md` §2) for the console, and the ID/access system for approval gating.

## 1. Design philosophy

The same test as everywhere else: is a piece of this genuinely good design, or a BYOND-era abstraction that survived out of habit? Two parts of SS13 cargo pass cleanly and one doesn't.

- **Ordering something and physically shipping it in is good design.** A crate has to travel, land, and get pried open — that's real, legible, interruptible. Nothing here needs reinventing, just reusing the shuttle framework this project already has.
- **A crate as a real, physical object is good design**, for the same reason wounds and armor wear are: the world carries the information (what's inside, whether it's locked) instead of a UI popup.
- **A silently-ticking background credit number is the one part that doesn't pass.** SS13's cargo budget usually just goes up over time whether anyone did anything or not. That's a hidden mechanism standing in for something that should be traceable — the same category of thing this project has rejected everywhere else (combat's percentage rolls, health's abstract damage bar). If credits move, there should be a real, timestamped reason on a ledger a player can actually read.

Two rules fall out of this:

1. **Every credit gained or spent is a real, logged transaction** — an order placed, an export sold, a periodic stipend — never a number that just quietly changes. Same discipline round config's admin history log applies to its own draws (`round-config.md` §5) and the AI's audit trail applies to law changes (`ai-cyborgs.md` §4).
2. **Goods are physical, not inventory entries.** An order doesn't appear in anyone's hands — it appears as a crate, on a shuttle, that has to actually arrive.

## 2. The ordering console

A station-placed device in cargo bay. Its screen is a diegetic device screen, same pattern as the FDU and fabricator (`hacking-interface.md` §2) — rendered in-world when approached and used, not a full-screen takeover.

**Catalog, not stock.** Unlike a vending machine's finite stock count, the console lists supply packs as standing orders from an off-station supplier — always available, gated by credits rather than availability. Each entry shows a name, its manifest (what's actually in the crate), and a cost.

**Placing an order deducts its cost immediately** from the shared station budget — visible on screen before confirming, no hidden approval roll. The order enters a queue for the next shuttle run (§5).

## 3. Approval gating

Below a configurable credit threshold, an order ships automatically once queued. Above it, the order sits pending until approved by someone with cargo-budget authority (Quartermaster, or a Head of Staff role) — a real access check against the existing ID/access system, not a new permission model.

**Approval requires the console.** Consistent with this project's pattern of keeping authority actions physically located (the AI's upload console, round config's admin screen), granting or denying approval happens at the ordering console itself, not remotely.

**But the notification doesn't have to wait for someone to walk by.** This is the bounded version of the hook `pda.md` §10 flagged and deliberately didn't build: an order entering pending-approval state sends a momentary notification chip to the relevant approver's PDA, reusing the alert-stack-style chip pattern already defined there (`pda.md` §6). The PDA gets a nudge, not an ordering tab — the actual approve/deny action still only exists at the console.

## 4. The budget ledger

A history tab on the console — same "real, inspectable data" instinct round config's admin log and the AI's audit trail already established. Every transaction is a timestamped line: order placed (and by whom), order approved or denied (and by whom), a crate sold, a periodic stipend paid. Nothing is summarized away; if the budget changed, there's a line explaining why.

**Passive income stays, but it's a logged line, not a silent trickle.** A small, fixed-interval "standard operations stipend" credits the budget automatically — SS13 tradition, and a reasonable one, since it keeps cargo from being strictly mandatory. The difference here is that it posts as a real ledger entry ("Standard operations stipend: +150") at the moment it happens, same as everything else on the ledger, rather than a number that's just always slightly higher than it was.

**The larger, player-driven source is selling exports** (§6) — which is what actually makes cargo a department people staff, rather than a number that goes up on its own regardless.

## 5. Crates — physical goods

Every order ships inside a real crate item — its own collision, moved by hand or a hand truck the same way any heavy object already gets moved, not teleported into a player's hands.

**The manifest is printed on the crate**, visible via examine without opening it (`main-hud.md` §15) — "Contents: 5× Advanced Medkit." World-carries-the-information, same rule armor wear and wound state already follow; no separate UI popup needed to know roughly what's inside.

**Opening is a real action** — crowbar pry for a standard crate, matching the tool-possession-as-gate convention already used everywhere (`surgery.md` §7). Some crates (weapons, restricted chemistry) are ID-locked instead. That lock isn't a new mechanic: an ID-locked crate is simply another entry in the FDU's existing device taxonomy (`hacking-interface.md` §3) — crew see "locked," a hacker sees the raw access bitmask, the same reuse the AI core and cyborg chassis already got (`ai-cyborgs.md` §5) rather than a bespoke crate-lock system.

## 6. The cargo shuttle

Reuses the shuttle framework wholesale (`shuttles.md`) — this doc doesn't touch movement, docking, or collision. It's a new roster entry alongside the sketches already in that doc's §10 (a companion edit there, the same way rendering/lighting flagged its own companion edit to Area).

**Default behavior:** docked at cargo bay. A **call button at the console** sends it on a round trip to the off-station trade dock and back — a short, real transit time, not an instant swap. This is deliberately smaller than the evac shuttle's call/timer/point-of-no-return shape (`shuttles.md` §9) — a supply run, not an evacuation decision — so it's just autopilot-out, dock, autopilot-back.

**On departure:** any queued, approved orders load as crates automatically. Any crates staged on the export pad (§7) load as outbound cargo.

**On return:** ordered crates unload into cargo bay. Exported crates get appraised at the trade dock before the return leg; their value posts to the ledger the moment the shuttle docks back.

**It can still be hijacked.** Same helm and manual-control framework as any other shuttle (`shuttles.md` §6) — an antagonist commandeering the cargo shuttle is a real possibility this doc doesn't need to prevent, just inherit the existing consequences for.

## 7. Exporting & selling

A designated export bay/pad — a physical Area in or near cargo bay — is where outbound crates get staged. Crew loads salvage, excess material, processed ore, or anything else the game currently treats as sellable into an empty or export-flagged crate and places it on the pad before the shuttle's next departure.

Appraisal happens at the trade dock (§5's return leg) and produces one ledger line per crate: "Sold: [manifest] for [amount]." Exact appraisal values come from a price table this doc doesn't author (§10).

## 8. Mining — the natural hook, not designed here

Mining (ore veins, extraction tools, an actual trip to an asteroid or exploration site) is its own system and a large enough one to deserve its own pass — flagged the same way this project has flagged the material silo and R&D tree elsewhere: assumed to exist as an input, not redesigned here.

The interface point is exactly §7's export loop: raw or processed ore is just another good that gets crated and either exported for credits through this system, or fed directly into the existing material silo (`crafting.md` §5) if the mining doc decides unprocessed material should bypass cargo entirely. Both destinations already exist; mining's own design should pick between them, not this one.

## 9. HUD touchpoints

No new permanent chrome.

- The console is the only screen — diegetic, on-demand, per §2.
- Crate contents render via examine, not a panel (§5).
- The budget total is visible only at the console, not a persistent HUD readout — consistent with main HUD's minimal-chrome rule.
- Pending-approval orders produce a momentary PDA chip, not a standing icon (§3).

## 10. Integration notes

| Cargo element | Touches existing / needed system |
|---|---|
| Ordering console screen | Diegetic device-screen pattern (`hacking-interface.md` §2) |
| Order approval gate | Existing ID/access system (`hacking-interface.md` §3) |
| Approval-pending notification | PDA notification chip (`pda.md` §6) — resolves that doc's §10 deferred hook, in bounded form |
| Budget ledger | Same real-inspectable-transaction discipline as round config's admin log (`round-config.md` §5) and the AI's audit trail (`ai-cyborgs.md` §4) |
| Crate as physical object | Existing heavy-object move convention; tool-possession-as-gate for prying (`surgery.md` §7) |
| Crate manifest | Examine system (`main-hud.md` §15) |
| ID-locked crate | FDU device taxonomy, reused wholesale (`hacking-interface.md` §3), same pattern as AI core/cyborg chassis (`ai-cyborgs.md` §5) |
| Cargo shuttle | Shuttle framework (`shuttles.md`) — new roster entry, companion edit needed at §10 there |
| Manual shuttle hijack | Existing helm/collision model (`shuttles.md` §6, §9) |
| Export appraisal / pricing | To-be-authored price table (balancing pass, not designed here) |
| Mining ore as sellable/sourced good | Future mining doc; material silo (`crafting.md` §5) as the alternate destination |

## 11. Worked examples

**A — Routine order, under threshold:**

| Step | What happens | Cargo state |
|---|---|---|
| 1 | Cargo tech browses the catalog, picks a medical resupply pack | Console shows manifest and cost before confirming |
| 2 | Order confirmed, cost is well under the approval threshold | Budget deducts immediately; ledger logs the order; crate queues for next departure |
| 3 | Tech calls the shuttle | Shuttle undocks, autopilots to the trade dock, docks, autopilots back |
| 4 | Shuttle returns | Crate unloads into cargo bay, manifest readable via examine |

**B — Large order requiring approval:**

| Step | What happens | Cargo state |
|---|---|---|
| 1 | Tech orders a bulk armor-plate shipment, well above the threshold | Budget isn't touched yet; order sits pending |
| 2 | Quartermaster gets a momentary PDA chip | Opens PDA out of habit, sees the notification, heads to cargo bay |
| 3 | QM reviews and approves at the console | Budget deducts now, not at order time; ledger logs both the order and the approval, with QM's name attached |
| 4 | Shuttle runs as in example A | Crate arrives on the next trip |

**C — Selling salvage:**

| Step | What happens | Cargo state |
|---|---|---|
| 1 | Crew clears scrap and excess plating from a maintenance job | Loads it into an export crate, stages it on the export pad |
| 2 | Shuttle is called for an unrelated order pickup | Export crate loads automatically alongside the outbound leg |
| 3 | Trade dock appraises the export crate | Shuttle docks back at the station |
| 4 | Ledger updates | "Sold: scrap plating (14 units) for 340 credits" — a real, attributable line, not a background tick |

## 12. Out of scope for this pass

- The mining/ore-extraction system itself (§8) — a separate, larger pass
- Exact catalog contents, prices, and the approval threshold value (a balancing pass, not a design decision)
- The export price table (same — balancing, not designed here)
- Department-specific sub-budgets, as opposed to one shared station budget — a plausible server-configurable variant, not load-bearing for this design, same category as lobby's optional ready-toggle
- Black-market or antag-specific ordering variants (a gamemode-specific extension, not a base mechanic)
- Persistence of the budget across rounds (persistence & accounts' job, cross-cutting infra)
- Full server-side transaction logging beyond the console's own ledger tab (server logging, cross-cutting infra)

