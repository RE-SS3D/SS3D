# PDA — design document

> Status: active

Formalizes a role the PDA has already been accreting by convention across three other docs without ever being specified itself: the gear-strip slot (`main-hud.md` §8), the comms history log (`comms.md` §9), and the crafting construction guide (`crafting.md` §2, §6). This doc consolidates that into one canonical spec, and resolves what those docs left open: whether PDA-to-PDA private messaging exists, and what future tabs (crew manifest, ID summary, ahelp) should look like when their own systems come up for design.

## 1. Design philosophy

Main HUD §1 sets the governing rule for this whole project: if the 3D model or the world can carry a piece of information, it should, instead of a panel. The PDA exists because some information genuinely can't clear that bar — a scrollable message log, a filterable transcript, a recipe reference list — there's no wound, posture, or lighting state that represents "here's what channel 'med' said ten minutes ago." Rather than let each system that hits this problem invent its own popup, they all dock into one object.

Two rules fall out of that:

1. **The PDA is the fallback, not the default.** Before something becomes a PDA tab, it should already have failed the "can the world carry this" test the way comms' log and crafting's recipe list did. A system that could plausibly show its state on the character model or in the world belongs there instead (this is why health's organ/limb readout is a held-key HUD overlay, not a PDA tab).
2. **One shared device, many tabs.** Same "one shared mechanism, not parallel systems" principle the hacking interface applied to ID/access and Area applied to power — every future system that needs a reference panel adds a tab to the PDA rather than building its own screen. This doc's job is to make that a real, bounded architecture instead of an ad hoc habit.

Visually and behaviorally it's a **diegetic device screen**, the same pattern already established for the field diagnostic unit, the fabricator, and the AI upload console (`hacking-interface.md` §2) — a real held object whose small screen becomes the interface when opened, not a full-screen takeover and not permanent chrome.

## 2. The device

A physical item, held in the gear strip (`main-hud.md` §8) like an ID card or a belt tool. Clicking its gear-strip icon opens its screen as a diegetic overlay — same visual treatment as the FDU, rendered in-world-adjacent rather than a browser-style panel takeover.

**It's a real object, not omnipresent access.** No PDA in the gear-strip slot means no PDA tab available at all — the same "physical possession is the gate" rule tools already follow (`surgery.md` §7: a screwdriver isn't ID-gated, having it in hand is the gate). It can be dropped, stolen, or handed off. That's a deliberate consequence, not an edge case to patch around — it makes stealing someone's PDA (and whatever ID is slotted into it) a real, physical act with real stakes, the same way it already is in SS13.

**ID slot.** The PDA has a physical ID-card slot, mirroring SS13. Whatever ID is currently inserted determines the PDA's displayed owner name and job, and is what the ID/access-linked tabs below actually read. This reuses the existing ID/access system wholesale (the same one the hacking interface's permissions panel already reads, `hacking-interface.md` §3) rather than giving the PDA its own identity concept. Swap the ID, the PDA's identity tab updates — no separate binding to maintain.

## 3. Tab architecture

The screen is a simple tab bar. Confirmed and proposed tabs:

**Already committed elsewhere, canonized here:**
- **Comms log** — full transcript, timestamped, filterable by channel (`comms.md` §9). No changes to that spec; this doc just gives it a formal home.
- **Construction guide** — reference list of known freeform recipes and their steps (`crafting.md` §2). Same treatment.

**New, defined in this doc:**
- **ID/access summary** — a read-only view of the inserted ID's name, job, and access levels. Directly reuses the ID/access system, the same "crew see only whether their own ID would work" framing the hacking interface's permissions panel already established (`hacking-interface.md` §3) — this tab is that same read, just surfaced on your own device instead of someone else's.
- **Crew manifest** — a list of connected crew: name, job, and a coarse online/offline or alive/no-signal status, pulled from the same ID/access and connection data. Deliberately **does not** include live location — see §5.
- **Messages** — private, address-to-a-person text, distinct from comms' proximity/channel model. See §4 for why this exists and how it's built.

**Flagged for later, not designed here:**
- **Ahelp** — an entry point for submitting an admin-help ticket lives on this device in most SS13-derived designs, and there's no reason to invent a different entry point here. This doc guarantees the PDA has a tab-shaped slot ready for it; the ticketing system itself (queue, admin-side view, audit logging) is `in-round admin tools`' job, a separate still-open pass, the same relationship round config has with that same doc (`round-config.md` §10).

## 4. Private messaging — resolving the gap

Comms (`comms.md`) deliberately scopes to proximity-based local speech and channel-based radio. Neither covers SS13's classic PDA feature: a direct, location-independent text message from one specific person to another. That's a genuine fork, not an oversight to just fix quietly — worth stating the two ways to go and why one wins.

- **Option A — don't add it.** Radio channels already cover cross-station coordination; adding a second, parallel messaging concept risks exactly the "parallel systems" problem this project keeps avoiding.
- **Option B — add it, but as an address mode of the existing system, not a new one.** A private message reuses comms' compose input and message/log backend (`comms.md` §11) — the same event pipeline that already handles local speech and radio — just addressed to a recipient ID instead of a channel or a proximity radius. It logs to both parties' comms/message history the same way everything else does.

**Recommendation: Option B.** The use case is real and distinct from radio — quiet one-to-one coordination without broadcasting to an entire department channel, which matters for ordinary crew coordination and matters even more for an antagonist who specifically doesn't want a channel full of witnesses. And because it's an address mode on the existing message backend rather than a new channel type, the cost is genuinely small: no new logging system, no new occlusion/range model (it's not proximity-gated at all, which is the point), just a new "to:" field alongside the existing channel field.

**This does mean a small companion edit is needed to `comms.md`** — its integration notes table (§11) should gain a row for PDA-to-PDA messaging as a new consumer of the message backend, the same way rendering/lighting flagged a companion edit to Area's lighting bullet rather than silently changing it out from under that doc.

## 5. Crew manifest — the location question, deferred on purpose

SS13's PDA-linked manifest sometimes doubles as a tracking tool (implant-based live location). That's a much bigger decision than a UI tab — it has real antagonist-balance and privacy-within-the-fiction implications, and it overlaps with the AI's camera network (`ai-cyborgs.md`) as the more natural home for "who can see where crew are" as a mechanic.

This doc commits only to the safe, clearly-scoped version: name, job, coarse connection status. Live coordinates are explicitly **out of scope for this pass** (§10) — a plausible future extension, but one that should be designed alongside whatever tracking-implant or AI-sensor system it would actually be gated by, not bolted onto a manifest tab as a side effect of writing this doc.

## 6. Notifications

An incoming message or an ahelp response produces a small, momentary HUD chip — same pattern the alert stack already uses (`main-hud.md` §9): appears, stays legible briefly, clears. No permanent "unread" icon sitting in the gear strip; that would be exactly the kind of standing chrome main HUD §1 rule 3 argues against. Opening the PDA and viewing the relevant tab is what clears it.

## 7. HUD touchpoints

No new permanent chrome — same discipline as every other doc in this project.

- Gear-strip icon is the only always-present footprint, and it's just an equipment slot like belt or ID, not PDA-specific chrome.
- The device screen itself only exists while open, rendered diegetically per §2.
- Notification chips are momentary, per §6.

## 8. Integration notes

| PDA element | Touches existing / needed system |
|---|---|
| Gear-strip access | Gear strip slot (`main-hud.md` §8) |
| Device screen rendering | Diegetic device-screen pattern (`hacking-interface.md` §2) |
| Physical possession as the gate | Existing tool-possession convention (`surgery.md` §7) |
| ID slot, identity binding | Existing ID/access system (`hacking-interface.md` §3) |
| Comms log tab | `comms.md` §9 (unchanged) |
| Construction guide tab | `crafting.md` §2, §6 (unchanged) |
| ID/access summary tab | Existing ID/access system, read-only view |
| Crew manifest tab | ID/access + connection data (cross-cutting infra, not designed here) |
| Messages tab | Comms message/log backend (`comms.md` §11) — new address mode, requires companion edit there (§4) |
| Ahelp tab (entry point only) | In-round admin tools (separate pass, not designed here) |
| Notification chip | Alert-stack momentary-chip pattern (`main-hud.md` §9) |

## 9. Worked examples

**A — Quiet coordination without a department channel:**

| Step | What happens | PDA state |
|---|---|---|
| 1 | A player wants to loop in one specific engineer without broadcasting to the whole engineering channel | Opens PDA, Messages tab, selects the engineer by name from a recent-contacts list |
| 2 | Sends a short message | Delivered instantly regardless of either player's location; logs to both PDAs' history |
| 3 | Engineer isn't currently looking at their PDA | Gets a momentary notification chip; opens PDA at their own convenience to read it |

**B — Stolen PDA as a real physical consequence:**

| Step | What happens | PDA state |
|---|---|---|
| 1 | An antagonist knocks out a security officer and takes their PDA and ID | PDA changes hands as a real item — no separate "hijack" mechanic needed |
| 2 | Antagonist opens it | ID/access summary tab shows the officer's real access levels; antag now has a working credential, not just a prop |
| 3 | Officer's contacts message "them" | Messages arrive at the antagonist's screen, indistinguishable from the real officer unless the recipient gets suspicious some other way |

## 10. Out of scope for this pass

- Live location/tracking tied to the manifest (§5) — deferred pending a tracking-implant or AI-sensor system that would actually gate it
- The ahelp ticketing system itself — queue, admin-side view, audit logging (`in-round admin tools`, separate pass; this doc only guarantees the PDA-side entry point)
- Cargo/requisition ordering via PDA — a plausible future hook once an economy/ordering system exists, not designed here
- Notes, ringtones, or other cosmetic PDA features — no gameplay weight, not designed here
- PDA skinning/department color variants — a content/art question, not a systems one

## 11. Prototyping this

Same two-stage pipeline as the rest of this project — Claude Design to validate the visual/interaction language, then Cursor for the Unity implementation.

**Claude Design, prompt 1 — device screen and tab bar:**
> Using our existing SS3D design system, build the PDA's diegetic device screen (same visual treatment as the field diagnostic unit): a tab bar across the top (Comms log, Construction guide, ID/access, Manifest, Messages), with the Comms log tab active showing a filterable timestamped transcript. Same flat-surface, hairline-border language as the FDU and fabricator screens — no browser-panel look.

**Claude Design, prompt 2 — messaging and notification:**
> Build the Messages tab: a recent-contacts list, a simple compose field, and a sent/received thread view. Then show the momentary HUD notification chip that appears when a message arrives while the PDA is closed, matching the alert stack's appear-and-fade behavior.

**Cursor, prompt 1 — data contract first:**
> Here's the PDA design doc. Define the PDA record (owner binding via inserted ID, gear-strip slot reference, tab set) and the message record (sender ID, recipient ID, timestamp, body) as an extension of the existing comms message backend rather than a new store. Show me both before wiring any tab UI.

**Cursor, prompt 2 — one vertical slice:**
> Implement gear-strip open/close and the Comms log tab only, wired to real comms data, matching the mockup. ID/access, Manifest, Construction guide, and Messages tabs come after this is reviewed.
