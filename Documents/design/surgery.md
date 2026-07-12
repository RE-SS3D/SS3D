# Surgery — design document

> Status: active

Resolves the deferred procedure flagged in `health.md` (§6, §9): that doc defined what surgery *accomplishes* — internal repair, organ repair/replacement, limb reattachment/prosthetics — without designing the mechanic itself. This doc is the mechanic. It doesn't introduce a new interaction system; it assembles one from pieces this project has already built: the seven-zone targeting system (`main-hud.md` §6), the Tier 2/Tier 3 radial grammar (targeted tool actions and combine/use-with), the interruptible-timed-step pattern from freeform crafting (`crafting.md` §2), and health's own bleeding/blood-volume model.

## 1. Design philosophy

Classic SS13 surgery is a long, fairly granular tool-chain (incise → clamp → retract → saw → operate → set → close) partly there to give a skill-check system something to grind against. This project doesn't have hidden skill rolls anywhere else, so there's nothing for that granularity to serve — it would just be busywork. At the same time, trimming it to a single click would throw away the real thing that makes surgery tense: an open body is a genuine liability, not a menu you step through risk-free.

The resolution is the same one main HUD's zone system already used: trim to what carries real decisions, keep the physical stakes. Four stages, not seven:

**Incise → Clamp → Operate → Close**

Clamp is the one stage that's skippable by choice, not by accident — and skipping it has a real, visible, already-modeled consequence rather than a new failure roll (§5). Everything else about *why* a step succeeds or fails traces to physical state — right tool, right zone condition, patient not thrashing — the same "physical, not RNG" rule every other doc in this project follows.

## 2. Access — incise and clamp

**Incise:** scalpel on the target zone, a standard Tier 2 targeted action (tool in hand, click the zone, reticle confirms per the existing zone-targeting hover-and-confirm behavior). Opens the zone surgically. An open incision is, physically, an open wound — it bleeds exactly like any other open wound in `health.md` §5, no new mechanic needed, and it renders on the model the same way (visible open site, same "body is the doll" rule as everything else, `main-hud.md` §5).

**Clamp:** hemostat on the open zone. Stops the surgical bleeding. **Not mandatory to progress** — Close can be attempted straight after Incise — but it's the one deliberate risk in the sequence (§5).

**Zone mapping for organs:** the seven-zone system doesn't subdivide the torso into an abdomen, so all internally-housed organs (heart, lungs, liver, kidneys) are surgically accessed via the chest zone; the brain is accessed via the head zone. Same reasoning main HUD §6 already used to justify seven zones instead of SS13's eleven — this doesn't need finer subdivision any more than combat did.

## 3. Operate — one slot, three procedures

This is the only stage that varies by what's actually being done. All three share the same shell (open zone in, tool or item applied, zone left open for Close) rather than being three separate systems:

- **Direct repair** — hemostat or cautery on the open zone, a Tier 2 action. Reduces the zone's wound severity directly (per the tiers in `health.md` §5) or resolves an internal bleeding source a bandage can't reach. This is the surgical-grade version of what field treatment already does, for damage past what field treatment alone resolves.
- **Organ exchange** — two steps, both existing grammar: **extract** (hemostat on the open zone, Tier 2) removes the failing organ, and it becomes a real, holdable item — same "everything maps to something physical" rule the hacking interface established, and yes, it's examinable (`main-hud.md` §15). **Install** (the replacement organ used on the open zone, Tier 3 combine/use-with) puts the new one in. Replacement organs come from transplant stock or a fabricator print (`crafting.md` §3) — not designed here, just the natural source.
- **Limb attach** — the severed limb or a fabricated prosthetic, used on the stump zone, Tier 3 combine/use-with. No extraction needed; the severance already did that. No saw needed either, for the same reason.

Nothing here invents a new targeting or combine behavior — organ install and limb attach are literally the Tier 3 grammar's original use case, applied to a body instead of a machine.

## 4. Close

Cautery or suture on the open zone, a Tier 2 action, seals the site. Two real tools with a real trade-off, the same kind of material-logic choice armor's absorption types already established: **cautery** is faster but leaves minor burn damage on the zone; **suture** is slower with no side effect. Neither is strictly better — it's a speed-versus-cleanliness call the player makes in the moment, not a hidden stat difference.

## 5. What skipping Clamp actually costs

Closing an unclamped site converts whatever bleeding was still open into internal bleeding once sealed — invisible externally, still draining blood volume through the exact same systemic pool `health.md` §2 already tracks. It has to be reopened to find and fix. This is a deliberate, informed trade a surgeon can make under time pressure (skip clamping, close faster, accept the risk), not a hidden rule someone would need a wiki to learn — the open, still-bleeding wound is visibly bleeding on the model right up until the moment they choose to close over it anyway. The tell is in the world, same as everywhere else in this project.

## 6. Patient state

**No hard table requirement**, consistent with this project's general refusal to gate physical actions by location (combat and defib both work anywhere the zone is reachable). A table does one real thing: it holds the patient physically immobile, which removes the need for a separate restraint. Field surgery without a table is possible but requires the patient to already be unconscious or physically restrained — reusing the existing grab mechanic (`main-hud.md` §7) as the restraint path, not inventing a new one.

**A conscious, unrestrained patient can interrupt a step** by moving, the same interruption pattern already established for freeform crafting (`crafting.md` §2) and melee windup — a step in progress cancels, the zone stays in whatever partial state it was in, nothing lost but the interrupted step.

**How a patient becomes unconscious or sedated** (chemistry, natural unconsciousness from critical state, a stun) is not designed here — this doc only defines what patient-state gates during a procedure, the same way it treats the material silo or R&D tech tree elsewhere in this project as existing systems it hooks into rather than redesigns.

## 7. Tools

Scalpel, hemostat, cautery, suture — four dedicated tools, no kit or bespoke crafting-adjacent system. Gating reuses the existing tool-in-hand convention already established for freeform crafting steps and the diagnostic device's wired jack (`hacking-interface.md` §2), not a surgery-specific tool-check. **No access/ID gate on the tools themselves** — consistent with every other tool in this project (a screwdriver or welder isn't ID-gated either); physical possession of the tool is the gate, same as it is everywhere else.

## 8. HUD touchpoints

No new permanent chrome.

- Each step is a standard Tier 2 armed-cursor action — reticle, HUD chip showing the pending step ("Incise — Scalpel →"), same visual language already established for weld/pry/splice.
- Procedure state renders on the patient's model — open incision, exposed organ, attached-but-unclosed limb — same "let the world carry the information" rule wounds and armor wear already follow. No separate surgery panel.
- Step timers render as a small progress indicator anchored to the patient, same convention as crafting's build/action timers (`crafting.md` §6).

## 9. Integration notes

| Surgery element | Touches existing / needed system |
|---|---|
| Incise, Clamp, direct repair, Close | Tier 2 targeted-action grammar (radial menu), seven-zone system (`main-hud.md` §6) |
| Organ install, limb attach | Tier 3 combine/use-with grammar (radial menu) |
| Open-site bleeding | Existing bleeding/blood-volume model (`health.md` §2, §5) |
| Unclamped-close → internal bleeding | Same systemic blood volume pool, no new mechanic |
| Interrupted steps | Freeform crafting's interruptible-timed-step pattern (`crafting.md` §2) |
| Patient restraint (no table) | Grab escalation (`main-hud.md` §7) |
| Replacement organs / prosthetics | Fabricator output (`crafting.md` §3) — sourced from, not redesigned here |
| Extracted organ as item | Examine system (`main-hud.md` §15) |
| Anesthesia/sedation | Assumed to exist (chemistry or equivalent), not redesigned here |

## 10. Worked examples

**A — Organ transplant:**

| Step | What happens | Surgical state |
|---|---|---|
| 1 | Patient's kidney is at critical function; a fresh kidney is on hand | Surgeon incises the chest zone with a scalpel |
| 2 | Open site bleeds like any open wound | Surgeon clamps with a hemostat |
| 3 | Extract | Failing kidney removed, becomes a holdable item |
| 4 | Install | Fresh kidney used on the open zone (Tier 3) |
| 5 | Close, cautery chosen for speed | Site seals; minor burn damage left as the trade-off; kidney function restored, tracked via the existing organ model |

**B — Emergency field reattachment, no table:**

| Step | What happens | Surgical state |
|---|---|---|
| 1 | A severed arm needs reattaching mid-crisis, no table available | Patient restrained via Alt+click grab instead |
| 2 | Surgeon incises the stump to prepare it | Bleeding begins at the site |
| 3 | Attach | Severed arm used on the stump zone (Tier 3) |
| 4 | A stray hit interrupts the close step | Step cancels; site stays open and unsealed, nothing else lost |
| 5 | Surgeon resumes, closes with suture | No burn side effect, slower than cautery would've been |

**C — Skipped clamp, paid for later:**

| Step | What happens | Surgical state |
|---|---|---|
| 1 | Surgeon incises, then closes immediately under time pressure — no clamp | Bleeding was visibly still active at the moment of closing |
| 2 | Externally, everything looks fine | Internal bleeding drains blood volume with no visible wound |
| 3 | Patient's condition worsens without explanation | Site has to be reopened to find and fix the unclamped bleeder |

## 11. Out of scope for this pass

- The anesthesia/sedation/chemistry system itself (assumed to exist, not designed here)
- Exact numeric values — step timers, cautery burn amount, complication severity (a balancing pass, not a design decision)
- Improvised surgical tools (a kitchen knife standing in for a scalpel) — plausible future hook in the same spirit as armor's improvised protection, not detailed here
- Multi-person surgical roles (an assistant holding retraction, etc.) — single-operator procedure assumed
- Disease/infection risk from surgery — already flagged as a separate future system in `health.md` §9
- Surgical skill/training affecting speed or quality — not assumed to exist, same caveat combat and stamina already apply to accuracy/exertion modifiers

## 12. Prototyping this

**Claude Design, prompt 1 — procedure sequence:**
> Build the four-stage surgery sequence on a test patient: incise (scalpel, Tier 2 reticle + chip), clamp, an operate step showing organ extract-then-install (Tier 3), and close with a cautery/suture choice. Show the open site rendering visibly on the model at each stage — this is the doll's replacement for surgery, not a separate panel.

**Claude Design, prompt 2 — skipped-clamp consequence:**
> Show two closing paths side by side: incise → clamp → close (clean), versus incise → close directly (unclamped). For the unclamped path, show the visible bleeding right up until the close click, then the site sealing with no external tell — followed by a later scene showing the same patient's condition degrading from internal bleeding with the wound already closed.

**Cursor, prompt 1 — data contract first:**
> Here's the surgery design doc. Define the per-zone surgical state machine (closed → incised → clamped-or-not → operated → closed-clean / closed-unclamped) and the tool-step validity table (which tool is valid given current zone state). Wire the unclamped-close outcome into the existing blood volume model as internal bleeding. Show me both before wiring any UI.

**Cursor, prompt 2 — one vertical slice:**
> Implement direct repair only, end to end — incise, clamp (skippable), repair, close, with the unclamped-close consequence wired to real blood volume. Organ exchange and limb attach come after this is reviewed.
