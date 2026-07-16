# AI & Cyborgs — design document

> Status: active

Builds on more of this project's existing systems than any doc so far: Area's power/access/camera model (`area.md`) for what the AI actually controls, the hacking interface's three-tier device model (`hacking-interface.md`) for subversion, comms' existing announcement channel (`comms.md` §8) for how the AI speaks, the fabricator job-state pattern (`crafting.md` §3) for cyborg chassis and AI core fabrication, and the revival framework (`death-cloning-respawn.md` §5) for both AI and cyborg as mid-round paths back into a round. It also extends that last doc's permanent-death condition — see §7.

## 1. Design philosophy

Three moves carry most of this doc:

1. **The AI's body is the station.** A stationary intelligence with no physical body isn't a BYOND limitation to fix — it's the concept. What *is* a BYOND artifact is treating the AI's control over doors, lights, APCs, and cameras as its own bespoke ability list layered on top of everything else, when Area (`area.md`) already models every one of those things natively. The AI doesn't get a special-powers system. It gets read/write authority over the exact same Area data everything else in this project already touches.
2. **Subversion is a hacking-interface consumer, not a new minigame.** The AI core and every cyborg chassis are, physically, devices — board, network trace, security layers, firmware signature. Malf, a forced law upload, a hijacked borg: all of it is "someone with the wrong tier of access reached a panel that resolves a privileged action," which is exactly what `hacking-interface.md`'s crew/engineer/hacker model was built for.
3. **Laws are roleplay-constrained, not mechanically enforced — but violations are real and inspectable.** Hard-gating the AI out of law-violating actions would remove the thing the AI role exists to test: whether the player holds the line. What this doc changes isn't the AI's freedom to break its laws, it's giving crew a genuine, physical, in-fiction way to notice when it does — an audit trail, not a leash. Same instinct as everywhere else in this project: give real information instead of a hidden mechanism.

A fourth, smaller move threads through §9: the AI's actual set of possible actions — request a camera, toggle a door, speak on comms — is defined once as a small, bounded interface, independent of who or what is issuing those actions. That's what makes an eventual non-human driver (§9) an extension of this doc rather than a rewrite of it.

## 2. What the AI is, physically

**No character rig.** The AI has no body to render information on — its presence is resolved entirely through the camera network and the control surface in §3.

**The AI core** is a physical object — a mainframe, sited in a specific Area, drawing power from that Area's APC (`area.md` §5) like any other powered device. It carries a backup battery, giving it the same kind of real physical window other systems already use rather than an instant cutoff: losing primary power drops the AI to a degraded state, and only sustained loss past the battery's charge takes it fully offline. This is the same shape as cardiac arrest not being instant death (`health.md` §4) — a real, inspectable window rather than a hidden threshold, and it gives an EMP or power-cut attack on the AI actual stakes.

**Destroying the core** is possible — a physical object with its own integrity, the same "physical, not RNG" line the rest of this project draws for anything that can be broken. A destroyed core doesn't just go offline, it stops existing as a functioning object; getting a new AI online after that requires rebuilding it (§7), not a respawn timer.

## 3. Control surface — the AI's authority over Area

**Cameras are how the AI sees.** A real diegetic through-the-lens view — one focused feed at a time, switched from a list, not a multi-feed dashboard — reusing the project's shared raycast/occlusion system for line-of-sight. Comms occlusion, the FDU's wireless scan, combat cover, light occlusion, and shuttle obstacle detection already share this system (`rendering-lighting.md` §5, extended to a fifth consumer in `shuttles.md` §4); camera line-of-sight is a sixth. A wall blocks a camera's view exactly the same way it blocks any of the others.

**Doors, lights, and APCs** are triggered by the AI through the same access check any other actor goes through — the AI is a high-privilege entry on the existing ID/access rail (`area.md` §5), not a parallel permission system. Default authority is broad but not absolute: some Areas are AI-restricted by design (a captain's private office, a specific vault), the same per-door-override mechanism Area already supports for crew access. Opening a door because the AI willed it and opening it because someone swiped a badge are the same underlying check with a different credential.

**Announcements** use the comms channel that already exists for this — `comms.md` §8 names "Station/AI announcements" as a reserved, restrained channel type. Nothing new here; the AI is simply the actor most likely to use it.

## 4. Laws

**A physical law module**, inserted at the AI core or a dedicated upload console — a real item, not a menu edit, consistent with this project's "everything on screen maps to something physical" rule. The console's screen is a diegetic device screen, same pattern as the FDU and the fabricator (`hacking-interface.md` §2).

**Laws stay an ordered, real list**, visible to the AI player, not automatically visible to crew — an AI can be asked to state its laws and can lie about the answer, the classic tension, fully preserved. Nothing about making laws physical changes that; it changes what happens *around* a violation.

**Audit trail.** Every law change and every "state your laws" query is logged at the console, timestamped, physically inspectable by anyone with the access to read it — the same discipline round config's admin history log already applies to its own draws (`round-config.md` §5: "real, inspectable data... rather than adjusting numbers against a black box"). This is the concrete answer to "detectable, not prevented": the game doesn't stop a rogue AI from acting, it makes sure the evidence exists in the world rather than only in an admin's tools.

**Real response tools for crew, once something's caught** — not a leash on the AI, a toolkit for the people who suspect it:

- **Access revocation** — pull the AI's authority over a specific Area or globally, through the same access system §3 already established. This doesn't stop the AI from *wanting* to act against its laws; it removes the physical means to.
- **Power cut** — the existing Area/APC path from §2, same as cutting power to anything else.
- **A wipe/reset action** at the core or console — an explicit, real, interruptible timed action (same convention as crafting's steps, `crafting.md` §2) that clears to a blank or crew-default lawset. A non-lethal path back to a compliant AI, distinct from destroying the core outright.

## 5. Subversion — the AI core and cyborg chassis as hacking-interface devices

The AI core, the upload console, and every cyborg chassis are devices in the FDU's taxonomy (`hacking-interface.md` §3) — nothing new, just high-value entries in a system that already exists:

- **Crew** see status: online/offline, basic power state.
- **Engineers** see full diagnostics and legitimate maintenance: recalibrate, replace a failing part, apply an official firmware update.
- **Hackers** see the real weak points — an unsigned-firmware slot, a replayable credential on the upload console's access gate, the core's tamper switch — and the antag-tier modifications that unlock once one's actually bypassed: insert a law without going through the legitimate upload path, plant a persistent backdoor bypassing the console entirely, or disable the audit logger itself. That last one is worth calling out: killing the logger should leave a hole *in* the log — a gap where entries stop — which is itself a detectable anomaly, not a clean erasure. Same "detection risk tied to real logs, not an abstract countdown" rule the hacking interface already commits to (§3 there).

**Cyborg subversion works the same way**, chassis as device: a hidden law insert, or a full override bypassing the AI's actual authority over that borg — a hijacked cyborg that no longer answers to a legitimate AI's laws, discovered the same way any other tampered device would be.

**Malf-AI's actual special-ability roster (turrets, an APC overload burst, hologram duplicates) is deliberately not designed here** — per scope, this doc gives malf its baseline mechanism, not its toolkit. A malf gamemode's abilities are just larger, illegitimate grants of the same Area authority §3 already defines, reached through the same bypass path §5 defines — the same treatment round config gives per-mode antag logic ("that mode's own business, not round config's," `round-config.md` §6).

## 6. Cyborgs

A cyborg has a full character rig and moves like a player, but its damage and support model diverges from a crew member's in ways that follow directly from having no biology:

**Damage** reuses the per-limb brute/burn tiers from `health.md` §2 (module ≈ limb) and skips toxin/oxy outright — no blood, no lungs, nothing for those pools to regulate. **EMP is a new systemic pool this doc has to define**, since nothing else in the project has one: a raw, ungoverned integrity value (no organ regulates it, unlike toxin/oxy) that drains from EMP sources and produces escalating malfunction — degraded movement, garbled comms, a module tool going unresponsive — before a full shutdown, mirroring health's "several independent real thresholds" shape (§4 there) without inventing a second aggregate number.

**Field treatment reuses the field/surgical split** with reskinned tools rather than a new system: a welder does what burn dressing does, a cable coil does what a bandage does, and an EMP-specific diagnostic reset at a repair station is the mechanical equivalent of organ repair — same tiering health already established, different vocabulary. Damage renders on the model the same way wounds do — sparking, scorching, exposed wiring — the doll-replacement rule holding regardless of what's underneath the paint.

**No death, no bleeding.** A destroyed or fully drained chassis goes inert — recoverable the way §7 describes, not a second death system. **Recharging** draws from the chassis's own internal battery, refilled at a dock station wired to its Area's APC, same derivation as defib's charge cycle (`death-cloning-respawn.md` §3). Running dry leaves the borg inert until recharged — a soft, recoverable state, not a failure roll.

**Module tools reuse the radial menu**, not a bespoke borg UI: a borg's fixed toolkit is what a hand slot and gear strip are for a crew member, so holding the tool-select key and choosing from the radial is the borg-native equivalent of the existing Tier 3 grammar (`main-hud.md` §8). Module roster (Engineering, Medical, Service, Security, Janitor) is deliberately not fully spec'd here — content and balancing, not a mechanic.

**Law sync** reads directly off the AI core's law record — no parallel law storage for borgs to drift out of sync with. If the AI goes offline or is destroyed, a borg's recommended fallback is to retain its last-synced laws rather than freeze — flagged as a recommendation, not locked, since "what borgs do with no AI" is a real open design question worth confirming before implementation.

## 7. Revival paths — extending death, cloning & respawn

Both AI and cyborg are viable mid-round paths back into a round, extending `death-cloning-respawn.md` §5 rather than redesigning it — the observer gets a narrow, on-demand prompt, same category as the existing "Clone ready — enter?" prompt (§5 there), not a rebuild of the deferred ghost/observer UI.

**Cyborg conversion doesn't need a corpse or a DNA record.** Recommended distinction, since it's the natural mechanical payoff of "cyborgs have no biology to source from": a chassis is fabricated fresh from material stock, the same fabricator job-state pattern already established (`crafting.md` §3), and a waiting observer's consciousness attaches to the finished body. This means cyborg conversion works in exactly the case cloning can't — a husked or destroyed body that fails the DNA-record check.

**This is a real amendment, not just an added feature, to `death-cloning-respawn.md` §6's permanent-death condition.** That doc defines permanent death as "no valid DNA record and no remains usable for reactive extraction." With cyborg conversion in play, that's no longer the true floor — a husked death that blocks cloning outright still leaves cyborg conversion open. Worth syncing back into that doc directly rather than leaving the two documents disagreeing; flagged here so it isn't lost.

**Becoming the AI mid-round** requires an unclaimed, functional core — one with no current occupant and power available, per §2. A destroyed core isn't an empty slot waiting for a new occupant; it's a wreck that needs rebuilding (engineering/crafting task, same fabricator-adjacent pattern) before anyone can claim it. This gives AI continuity a real, physical cost on the way back in, the same "real, visible cost to the process" rule cloning already pays with its fidelity-loss penalty (`death-cloning-respawn.md` §5).

## 8. HUD & interaction

The AI player's screen is a genuine third case, distinct from both of this project's existing extremes. Main HUD's minimal-chrome rule exists to keep panels from competing with a 3D world a character is actually looking at (`main-hud.md` §1); lobby earned a carve-out because there's no world yet at all (`lobby.md` §8). The AI has a world — it sees through real cameras with real occlusion — but no body in it. So: primarily diegetic through the live camera feed itself, not lobby's dense everything-visible treatment, but with real permanent chrome around it for things with no physical analogue to render on — the camera quick-switch list, the Area control panel tied to whatever's currently in view, laws, and (if the AI is directing them) a borg roster. Its own category, not a rule this doc borrows wholesale from either existing precedent.

Everything else reuses existing patterns unchanged: diegetic device screens for the upload console and diagnostics (`hacking-interface.md` §2), the radial for borg tools (§6 above), the existing comms channel for announcements (§3 above).

## 9. The driver-agnostic action interface

This is the piece built now specifically so a future non-human driver is an extension, not a rewrite.

**The AI's entire action surface is a small, well-typed interface**, independent of what's issuing the calls:

- Request a camera view
- Toggle a door / light / APC within current authority
- Send a comms message on a given channel
- Issue an order to a borg (already natural language in the base design — crew have always given borgs free-form spoken instructions, nothing new to invent)

**Deliberately no bespoke action per feature.** Everything routes through this same handful of primitives, checked against the same access/authority model any other actor uses. That's the actual design payoff, not just tidiness: a small, bounded, well-defined surface is what makes a future non-human driver tractable at all.

**Three pluggable drivers of the identical interface:**

- **Human player** — the default and primary case, choosing actions through the camera/console UI in §8.
- **Scripted fallback** — a lightweight, non-clever rules layer (opens a door on a valid request, nothing more) that keeps a station minimally functional when the AI slot is unmanned, so an empty seat doesn't mean the station simply has no AI at all.
- **A future LLM-driven agent, explicitly not designed here.** Flagged rather than built: because comms is already text throughout (`comms.md` §1) and the action surface above is already small and typed, the actual hookup point already exists by construction once this doc is implemented — that was the job of this pass, not standing up the agent itself. Worth naming honestly what that would still require before it's real: latency for time-critical requests, hosting cost for continuous inference on a self-run project, and a genuine safety-engineering question — not a balance one — around giving something real tool-calling authority over station systems driven by arbitrary, adversarial player chat. The social-engineering half of the AI role has always been "convince the AI its laws mean something they don't"; a language-model driver makes that literally a prompt-injection problem, which is either a great thematic fit or a real risk depending entirely on how carefully the guardrails around it get built. Not a reason to rule it out, a reason to treat it as its own, later, dedicated pass rather than a corner of this one.

## 10. Integration notes

| AI/Cyborg element | Touches existing / needed system |
|---|---|
| AI core, power/backup window | Area → APC derivation (`area.md` §5); real physical window pattern (`health.md` §4) |
| Camera view | Camera network (`area.md` §5); shared raycast/occlusion, sixth consumer (`rendering-lighting.md` §5, `shuttles.md` §4) |
| Door/light/APC control | Area's existing access/power model (`area.md` §5) — AI as an access tier, not a parallel permission system |
| Announcements | Existing comms channel (`comms.md` §8) |
| Law module, upload console | Diegetic device-screen pattern (`hacking-interface.md` §2); ID/access rail |
| Audit trail | Same pattern as round config's admin history log (`round-config.md` §5) |
| AI / lawboard / cyborg subversion | Hacking interface's three-tier device model, reused wholesale (`hacking-interface.md` §3, §4) |
| Malf-AI special abilities | Gamemode-specific escalation of this doc's baseline — not designed here, same treatment as `round-config.md` §6 |
| Cyborg damage/repair | Per-limb brute/burn model (`health.md` §2), reskinned treatment tools; EMP as a new pool |
| Cyborg chassis fabrication | Fabricator job-state pattern (`crafting.md` §3) |
| Borg module tool select | Radial menu, Tier 3 convention (`main-hud.md` §8) |
| Borg recharge | Area → APC derivation, same as defib charge (`death-cloning-respawn.md` §3) |
| Borg law sync | AI core's law record — no parallel storage |
| Cyborg as revival path | Extends `death-cloning-respawn.md` §5 — amends §6's permanent-death condition, see §7 here |
| AI as revival path | Extends `death-cloning-respawn.md` §5 — new "unclaimed core" eligibility case |
| Observer prompts (become AI / borg) | Narrow exception to deferred ghost/observer UI (`main-hud.md` §13), same category as the existing clone-ready prompt |
| Driver-agnostic action interface | New — architected for a future non-human driver, not implemented (§9) |

## 11. Worked examples

**A — Ordinary operation:**

| Step | What happens | State |
|---|---|---|
| 1 | AI is asked over comms to open a door | Access check runs against AI's authority for that Area, same as any actor |
| 2 | Door opens | No special-case logic — same door-open event any credentialed access triggers |
| 3 | A crew member asks the AI to state its laws | AI answers (truthfully or not); the query itself is logged at the console |

**B — Subversion, caught after the fact:**

| Step | What happens | State |
|---|---|---|
| 1 | A hacker fingerprints the upload console, finds a replayable credential | Standard hacker-tier discovery, per `hacking-interface.md` §3 |
| 2 | A law is inserted without a legitimate upload | AI begins acting on it; the audit log shows an entry outside the normal access pattern |
| 3 | Engineering reviews the console's log | The out-of-process entry is visible, not hidden — real evidence, not a hunch |
| 4 | Crew responds | Access revocation and a wipe/reset action, per §4 — the AI is contained, not "arrested" |

**C — Cyborg conversion after a death cloning can't fix:**

| Step | What happens | State |
|---|---|---|
| 1 | A crew member dies in a fire, body husked, no prior DNA scan | Per `death-cloning-respawn.md` §6, cloning is unavailable |
| 2 | Observer is offered cyborg conversion instead | No corpse or DNA record required, per §7 here |
| 3 | A robotics fabricator builds a fresh chassis | Same job-state pattern as any other fabricator print |
| 4 | Observer accepts, wakes in the new chassis | A real revival path where the previous doc's permanent-death condition alone would have said none existed |

## 12. Out of scope for this pass

- Malf-AI's specific special-ability roster (turrets, APC overload burst, hologram duplicates, etc.) — gamemode-specific escalation of this doc's baseline authority, not designed here
- Exact numeric values — EMP thresholds, audit-log retention, borg battery capacity, core rebuild time (a balancing pass, not a design decision)
- The LLM-driven agent itself — architecture only (§9), not implementation, guardrails, or hosting
- Full cyborg module/tool roster per type — content and balancing, not a mechanic
- Multiplayer/netcode implications of a future non-human driver
- Voice-chat compatibility for a future non-human driver (comms' existing voice-as-additive-layer caveat, `comms.md` §10, isn't extended further here)
- Server policy on who may become AI or cyborg (round config / job-list territory, not this doc)

