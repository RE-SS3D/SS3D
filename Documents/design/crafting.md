# Crafting — design document

> Status: active

Resolves the deferred "crafting" items flagged in `combat.md` (§8, weapon crafting/attachments) and `armor.md` (§8, armor crafting or modification). Builds on the Tier 3 combine/use-with grammar established for the radial menu (referenced in `main-hud.md` §8), the diegetic device-screen pattern from `hacking-interface.md` §2, and Area's power derivation (`area.md` §5).

## 1. Design philosophy

Same thread as everything else in this project: no abstract menu floating independent of the world, no hidden roll standing in for a real mechanism. Crafting splits into two genuinely different jobs wearing one name — the same shape of split armor drew between combat mitigation and environmental sealing:

- **Freeform / hand construction** answers "can these two physical things become a third thing right now." It's manual, immediate, and small in scope — bolting a frame together, wiring a board. It reuses an interaction grammar that already exists rather than inventing one.
- **Fabrication** answers "can this machine turn stored material into a finished, precise object." It's industrial, gated, and produces the more complex/powerful items — weapons, armor plates, circuitry — the things freeform assembly by hand shouldn't plausibly produce.

Forcing both into one interaction model would either make simple assembly needlessly heavy (a full fabricator UI just to bolt two things together) or make fabrication feel like idle drag-and-drop when it should feel like running a real machine. Splitting them, and letting each reuse an existing pattern instead of inventing a new one, keeps both honest.

**Recipe discovery follows the same split, for the same reason.** Freeform recipes are known from the start — simple, physically intuitive combinations don't need a research gate, and gating them would only be friction plus a return of the "wiki-only knowledge" problem this project has already flagged and avoided once (comms doc §2, on typed radio prefixes). Fabricator designs are gated behind the existing R&D tech-tree/research-point system — reused wholesale, not redesigned, the same "no parallel model" principle Area §1 already established for permissions and the hacking interface established for ID/access.

## 2. Freeform / hand construction

**Interaction:** reuses the Tier 3 combine/use-with grammar defined for the radial menu — drag one item onto another, or select "use with..." from an item's radial to arm a targeting cursor and click a valid target. No new interaction pattern to learn; this is that system's original use case.

**Discoverability without a menu:** valid-target highlighting during Tier 3 targeting already shows which combinations are live when an item's in hand — hover a compatible item, it lights up. That covers single-step combinations for free. Multi-step chains (a machine frame needing four or five sequential steps) are less legible from hover alone, so the PDA gains a **construction guide** tab — a plain reference list of known recipes and their steps, sitting next to the existing comms log entry point (`comms.md` §9, accessed via the gear strip in `main-hud.md` §8). This is a reference the player opens on demand, not a crafting menu that performs the action — the action still happens by combining real items in the world.

**Multi-step assembly:** each step is a single Tier 3 combine action with a short, visible completion timer — the same timed-interaction pattern SS13 already uses for wrenching, welding, and similar actions, not a new mechanic. A step is interruptible (moving away, taking damage cancels it), which is what keeps it physical rather than an instant, risk-free menu click.

**Partial state renders on the object.** An unfinished frame looks visibly different from a completed one — missing panels, exposed wiring, whatever the model supports — the same "let the world carry the information" rule the doll replacement and armor wear already follow. There's no separate "crafting progress" readout; the object *is* the readout.

**Tool gating:** some steps require a specific tool in hand (screwdriver to secure, welder to seal). This reuses the existing tool-interaction convention rather than introducing a crafting-specific tool-check system.

## 3. Fabricators / machines

**The device:** a station-placed machine (protolathe-equivalent). Its screen becomes the interface exactly the way the field diagnostic unit's does — a diegetic HUD element rendered in-world on the machine itself, not a full-screen takeover (`hacking-interface.md` §2).

**Power:** drawn from the machine's Area's APC, the same derivation every other powered device already uses (`area.md` §5) — no separate wiring or authoring per fabricator.

**Material:** drawn from the existing material silo/network. This doc assumes that economy layer already exists in SS3D as it does in SS13 and doesn't redesign it — same treatment Area gave atmospherics (§4, deliberately decoupled, not designed there) and the hacking interface gave ID/access (reused, not rebuilt).

**Selecting and running a job:** browse the list of unlocked designs, pick one, confirm. The machine visibly runs — a real build-time, not an instant pop — and ejects the finished item into an output tray on completion. Nothing here is a hidden roll: a job either has the material and power to start, or it doesn't; once running, it either finishes or gets interrupted by a real cause (power cut, material depletion mid-print), and either outcome is visible on the machine itself — a stalled printer with a part-formed item sitting in the tray, not a silent failure or a percentage that quietly ticked against you.

## 4. Recipe discovery / research

- **Freeform** — known from the start, per §1 and §2.
- **Fabricator** — gated by the existing R&D tech-tree/research-point system: points from scanning or destroying tech, spent in a tech web to unlock new designs. Reused wholesale, not redesigned here. Flagged as an assumption the same way this project has flagged others: if SS3D isn't carrying the R&D system forward as-is, this doc treats it as a black-box unlock gate and doesn't invent a substitute progression economy.

**Why the split holds up:** gating simple assembly behind research would just be friction — nobody needs to unlock bolting two metal sheets together. An ungated fabricator would flatten the tech-tier progression that's core to SS13's economy. The complexity split already made for scope carries the discovery split along with it at no extra cost — freeform is simple enough to be legible on sight, fabricator output is powerful enough to be worth pacing.

## 5. Materials

Real, countable physical stock — sheets, bars, ore, cable coils — not abstract crafting points, consistent with the "physical, not abstract" language already used for armor's absorption values and health's organ model. Freeform construction consumes items directly from hands/inventory. Fabricators consume from the material silo they're wired into.

## 6. HUD touchpoints

No new permanent chrome — same discipline as every other doc in this project.

- **Freeform** reuses the Tier 3 armed-targeting cursor and HUD chip already defined for combine actions generally — no separate crafting-mode UI.
- **Fabricator** reuses the diegetic device-screen pattern (`hacking-interface.md` §2).
- **PDA** gains one more tab — the construction guide (§2) — alongside the existing comms log, not a new permanent panel.
- **Action/build timers** render as a small progress indicator anchored to the object or machine itself, not a HUD-wide element — consistent with everything else in this project living in the world rather than in a fixed panel.

## 7. Integration notes

| Crafting element | Touches existing / needed system |
|---|---|
| Freeform combine | Tier 3 combine/use-with grammar (radial menu) |
| Freeform discoverability | Valid-target highlight (radial menu); PDA gear strip (`main-hud.md` §8) |
| Freeform action timing | Existing timed-interaction pattern (wrench/weld-style) |
| Fabricator interface | Diegetic device-screen pattern (`hacking-interface.md` §2) |
| Fabricator power | Area → APC derivation (`area.md` §5) |
| Fabricator material | Existing material silo/network (not redesigned here) |
| Fabricator recipes | Existing R&D tech-tree/research-point system (not redesigned here) |
| Weapon/armor items produced | `combat.md`, `armor.md` — resolves both docs' deferred crafting items |
| Tool-gated steps | Existing tool-interaction convention |

## 8. Worked examples

**Freeform — building a basic machine frame:**

| Step | What happens | Crafting state |
|---|---|---|
| 1 | Player holds a metal sheet, hovers it over a frame kit | Valid-target highlight confirms the combination before committing |
| 2 | Player commits the combine | Short visible timer runs; frame appears in a part-built state on the tile |
| 3 | Player checks the PDA construction guide | Sees the remaining steps (circuit board, wiring, weld seal) without leaving the game |
| 4 | Player is interrupted mid-weld by a passing hit | Step cancels; frame stays in its last completed partial state, nothing lost but the interrupted step |
| 5 | Player finishes the remaining steps | Frame renders as fully assembled, ready for the next system (e.g., becomes an APC once wired and powered) |

**Fabricator — printing an armor plate:**

| Step | What happens | Crafting state |
|---|---|---|
| 1 | Player approaches a protolathe, screen lights up as a diegetic in-hand-adjacent display | Design list shows only what's unlocked via R&D so far |
| 2 | Player selects a plate design | Machine checks material silo and its Area's APC state before starting |
| 3 | Material and power are both sufficient | Job starts; machine visibly runs for its build time |
| 4 | Someone cuts power to the Area mid-print | Job stalls visibly — a part-formed plate sits in the tray, no progress lost silently, but nothing completes until power's restored |
| 5 | Power returns | Job resumes and completes; finished plate ejects to the output tray |

## 9. Out of scope for this pass

- Exact recipe list, material costs, research costs, and build-time balancing (a balancing pass, not a design decision)
- The material silo/network economy itself (assumed to exist, not redesigned here)
- The R&D tech-tree/research-point system itself (assumed to exist, not redesigned here)
- Repair mechanics using crafted parts — already flagged as a natural follow-up in `armor.md` §5, not designed here either
- Deconstruction (reversing a fabricated or freeform item back into materials) — plausible future hook, not designed here
- Found-blueprint items as an alternate unlock path alongside R&D — a small, real future addition, but not needed for this pass

## 10. Prototyping this

Same two-stage pipeline as the rest of this project — Claude Design to validate the visual/interaction language, then Cursor for the Unity implementation.

**Claude Design, prompt 1 — freeform combine sequence:**
> Build a multi-step freeform crafting sequence: a metal sheet dragged onto a frame kit with a valid-target highlight, a short visible action timer on commit, and a part-built object state on the tile. Show an interrupted step leaving the object in its last completed partial state rather than resetting or failing silently. Add the PDA construction-guide tab listing the remaining steps.

**Claude Design, prompt 2 — fabricator screen:**
> Build the fabricator's diegetic device screen: a design list filtered to unlocked R&D designs, a material/power check before a job starts, a running-job state with a visible build timer, and a stalled-job state (part-formed item sitting in the tray) triggered by a power cut mid-print. Same visual language as the FDU screens from the hacking interface mockups — industrial diagnostic tool, not a sci-fi crafting menu.

**Cursor, prompt 1 — data contract first:**
> Here's the crafting design doc. Define the recipe record covering both freeform (required items/tools, step sequence, per-step timer) and fabricator (material cost, research/unlock gate, build time) cases, and the job-state machine for a fabricator print (idle → running → stalled → complete), including what happens to in-progress material on a stall. Show me both before wiring any UI.

**Cursor, prompt 2 — one vertical slice:**
> Implement one freeform recipe (the machine-frame example) and one fabricator recipe end to end, wired to real inventory, Area/power, and material-silo state. Everything else in §9 comes after this is reviewed.
