# Main HUD — design document

> Status: active

## 1. Design philosophy

The test for every element below is the same one already applied to the radial menu and the diagnostic interface: is this a genuinely good piece of game design, or is it an artifact of SS13 being a 2D, top-down BYOND client? Those are different questions, and SS13's UI answers both the same way — as flat panels — because the engine gave it no other option.

Three rules:

1. **If the 3D model can carry the information, it should, instead of a panel.** A character now has a real rigged body. Damage, posture, and state can show up on that body directly — wounds, limp, blood, breathing — rather than in an abstract readout the player has to glance at and mentally translate back onto the character they're looking at.
2. **Keep what's good regardless of engine.** Two-hand item juggling, intent selection, limb-specific damage, a glanceable status-icon stack — none of these exist because of the top-down camera. They're just solid mechanics, and this pass keeps them close to as-is.
3. **Minimal permanent chrome.** Panels earn their place on screen at all times only if they convey something the world and the character model genuinely can't. Everything else should be on-demand (a held key) or momentary (a chip that appears and clears).

Visual language matches the diagnostic interface and radial menu work already done: flat surfaces, hairline borders, muted status colors reserved for real alerts, monospace for data-flavored readouts, sans for UI chrome. No neon, no glitch effects, no HUD-as-decoration.

## 2. What carries over unchanged

- **Intent as a persistent mode, not a menu** — carries over from the interaction-menu pass in spirit, but the shape of it changed since: a two-state help/harm toggle plus modifier-chorded verbs rather than a four-way selector. See §7.
- **Alert icon stack** — pressure, temperature, fire, radiation, hunger/thirst, restrained, pulling, etc. This is one of SS13's cleanest systems: glanceable, only shows what's active, doesn't need a redesign — just a restyle.
- **Two-hand item juggling** — both hand slots stay. It's core tool-use gameplay, not a legacy artifact.
- **Per-limb damage as a data model** — the backend concept of tracking brute/burn/toxin/oxy damage per body part doesn't change. Only how the player perceives it changes (see §6).
- **Radial menu as the interaction surface** — this HUD is the canvas the radial and the diagnostic device screens render on top of.

## 3. What's being cut, and why

- **The targeting doll.** It exists because a 2D sprite can't be aimed at directly — clicking a limb in the world isn't possible when there's no 3D geometry to click. In SS3D there is. Direct world-space aiming replaces it entirely (§6).
- **A permanent, always-visible damage panel.** Once damage shows on the character model and through screen-space feedback, a panel that duplicates that information earns its keep only as an on-demand readout, not a fixture.

## 4. HUD layout

| Zone | Contents | Always visible? |
|---|---|---|
| Top-left | Vitals cluster (§5) | Yes, compact |
| Top-right | Alert icon stack (§9) | Only active alerts |
| Center | Aim reticle + zone label (§6) | Only while a valid target is under the cursor |
| Bottom-left | Comms feed — see `comms.md` | Collapsed strip, expands on activity |
| Bottom-center | Hands + gear strip (§8) | Yes |
| Bottom-right | Intent module — help/harm toggle + modifier hints (§7) | Yes |

Nothing else is persistent. Diagnostic/hacking device screens (from the earlier pass) temporarily take over the center of the screen as a diegetic in-hand display; they don't compete with this layout, they sit on top of it.

## 5. Vitals cluster — doll replacement, part 1

A small always-on card, not a body diagram: an aggregate condition read plus four thin bars (brute, burn, toxin, oxy), each rendered as a labeled icon + bar rather than a silhouette. Bars that are at zero stay visually quiet — no "OK" clutter, just a flat empty bar — so the cluster only draws the eye when something's actually wrong.

**Screen-space feedback carries the rest of the load that the doll used to:**

| Condition | Feedback |
|---|---|
| Low oxygen | Peripheral desaturation / cold tint |
| High brute/burn pain | Red screen-edge pulse, intensity scales with damage |
| On fire | Heat-shimmer distortion at screen edges |
| Critical / dying | Slow heartbeat audio + a visual pulse synced to it |

This is diegetic in the same sense the hacking interface is diegetic — the player learns their own state the way they'd learn it in real life, by how things *feel*, with the panel as backup, not primary.

**On-demand detail:** holding the existing "examine self" key opens a lightweight per-limb readout — this is where the granular data lives when the player wants it, without needing a permanent panel.

**The body itself is the doll now:** damage renders directly on the character model — visible wounds, blood, burns, a limp on a hurt leg — both to the player (via self-view/camera framing) and to anyone else looking at them. This is a genuine upgrade over the old system: a medic can eyeball someone's injuries at a glance without opening anything, which SS13's 2D sprites couldn't really do.

## 6. Zone targeting — doll replacement, part 2

Direct world-space aiming, as decided: a raycast against a lightweight limb-collider rig on the character skeleton, resolved into the same zone data the medical/combat systems already use.

**Zone set — trimmed from SS13's roughly eleven click-zones to seven:** head, chest, l_arm, r_arm, l_leg, r_leg, groin. Hand- and foot-specific effects (disarm, drop item, trip) move into the arm/leg zone's effect table instead of needing pixel-precise hits on a hand or foot — that precision made sense on a static 2D doll with generous click targets; it's punishing to demand in fast, camera-relative 3D combat.

**Reticle behavior:** the crosshair shows a small zone-label chip when hovering a valid limb on a targetable character (e.g. "chest"), confirming the shot before it's committed. That's the same "know what you're about to hit" value the old doll gave, just resolved in the world instead of a side panel. Applies uniformly to ranged and melee — melee reads the same hover-and-confirm at contact range.

**Self-targeting:** medical self-care reuses the same reticle system aimed at your own model (self-view / look-down), rather than reviving a doll just for that one case.

**Open dependency — flag for camera team:** this reads correctly on screen only if the camera shows enough vertical separation of a body (head above chest above legs) to aim it, which an isometric/three-quarter camera does and a true orthographic top-down camera doesn't. If the camera lands closer to pure top-down, the fallback is screen-space vertical banding — cursor's vertical offset from the target's on-screen center maps to a zone — same player-facing result (hover, see a zone label, click), different resolution method underneath. Worth confirming before this is implementation-locked.

## 7. Intent & combat verbs

SS13's four-way intent selector (help / harm / disarm / grab) collapses to a two-state help/harm toggle, with disarm and grab moved to modifier-chorded clicks instead of a menu:

| Input | Result |
|---|---|
| Plain click | Resolves per current intent (help = gentle/non-damaging, harm = attack) |
| Tap toggle key | Flips persistent help/harm state |
| Hold Shift + click | Momentary override — one click of the opposite intent, state reverts on release |
| Hold Ctrl + click | Disarm attempt |
| Hold Alt + click | Grab / escalate an existing grab |

Ctrl and Alt were picked deliberately: SS13 already trains players to ctrl-click and alt-click objects for shortcuts, so this reuses existing muscle memory rather than teaching a new one.

**Why this isn't just a rebind.** In SS13, switching from help to grab mid-fight costs an extra input — you have to change intent before you can act. Chording removes that tax: Alt+click grabs regardless of what the base toggle is set to. All four verbs are reachable in one click at any time, not gated behind cycling to the right mode first.

**Grab escalation stays on Alt+click, never falls through to plain click.** Once a grab is active, it's tempting to let a plain click on the grabbed target escalate it. Don't — that makes plain click mean different things depending on hidden state. Initiate and escalate use the identical gesture (Alt+click), so there's no silent context-switching for the player to trip over.

**The cost is discoverability, and it needs a mitigation.** A four-icon selector is self-documenting — every option is visible, every time. A toggle plus three invisible modifiers is faster but only for players who already know the modifiers exist, which is exactly the kind of thing that ends up wiki-only knowledge in SS13. Mitigate with fading hints: show modifier labels (shift — swap once / ctrl — disarm / alt — grab) next to the intent pip for new or infrequent players, and shrink them to unlabeled ticks once a player's demonstrably used each one a few times.

## 8. Hands & gear strip

Previously left out of scope on the interaction-menu pass; covered here.

- Bottom-center: two hand slots, unchanged from SS13's swap convention. Active hand gets a distinct border.
- A slim gear strip beside the hands: belt, ID, PDA, back — icon-only, click to open that slot's contents. This is not a full inventory/backpack redesign (still out of scope, see §13), just quick-glance access to what's equipped.
- Drag-and-drop between hands, gear strip, and world reuses the Tier 3 "combine" convention already defined for the radial menu, so there's one consistent drag language across hands and world interactions instead of two.

## 9. Alerts

Top-right icon stack. Restyled only — same underlying set as SS13 (pressure, temperature, fire, radiation, hunger/thirst, pulling, restrained). Icons are hidden, not grayed out, when inactive, so the stack is empty on a healthy, unencumbered character and only fills in with what's actually true.

## 10. Comms

Comms grew into its own system — local speech bubbles, distance/occlusion, crowd handling, radio and other non-positional channels, whisper/shout/announcement behavior, message composition, and the history log. It's spun out into its own document: `comms.md`. This doc keeps only what the main HUD needs to know: the feed's corner in §4, and its hook into the PDA in the gear strip (§8).

## 11. Integration notes

| HUD element | Touches existing system |
|---|---|
| Vitals cluster, on-demand readout | Per-limb damage model (unchanged data layer) |
| Zone targeting | Combat/medical damage application, character skeleton/rig |
| Hands, gear strip | Inventory slots, Tier 3 drag/combine convention from the radial menu |
| Intent module | Click-resolution logic (what a plain click and each modifier+click does) |
| Alerts | Atmospherics, hunger/thirst, restraint state — existing trackers |
| Comms | See `comms.md` for full integration notes |

## 12. Worked example — taking a hit and calling for help

| Step | What happens | HUD state |
|---|---|---|
| 1 | Player is set to help, but the attacker rushes in armed — no time to toggle | Holds Ctrl, clicks attacker: disarm attempt, no intent switch needed |
| 2 | Attacker aims at player mid-melee | Attacker's reticle shows zone chip "l_arm" on hover |
| 3 | Hit connects | Player's screen edge pulses red once; wound appears on the arm model |
| 4 | Bleeding starts | "Bleeding" alert chip lights up top-right |
| 5 | Player checks severity | Holds examine-self key, sees per-limb readout, releases |
| 6 | Player calls for a medic | Opens PDA via the gear strip, sends a message; channel chip shows "med" |

## 13. Out of scope for this pass

- Full inventory/backpack screen (opening a gear-strip slot shows its contents, but redesigning that screen is separate work)
- Any ability/action-bar row (no evidence this exists in SS3D currently — not assumed here)
- Observer/ghost UI
- Settings/options menus

## 14. Prototyping this

Same two-stage pipeline as the last two passes — Claude Design to validate the visual/interaction language, then Cursor for the Unity implementation.

**Claude Design, prompt 1 — full HUD at rest:**
> Using our existing SS3D design system, build a static mockup of the main HUD per the attached doc (main-hud.md): vitals cluster top-left, alerts top-right, hands + gear strip bottom-center, intent selector bottom-right, comms feed bottom-left, all over a game-viewport placeholder. Character is at full health — most of the HUD should look quiet and empty, not busy.

**Claude Design, prompt 2 — condition states:**
> Make the vitals cluster and screen-space feedback interactive: a toggle for nominal / injured / critical that changes the vitals bars, lights up the bleeding and critical alert chips, and pulses a red screen-edge vignette at increasing intensity. I want to check the diegetic feedback is legible without staring at the vitals card.

**Claude Design, prompt 3 — zone targeting reticle:**
> On a test target character, show the aim reticle hovering over different zones (head, chest, arm, leg) with the zone-label chip updating live. This is to check the zone label reads clearly at a glance before we commit to raycast-based limb targeting in Unity.

**Claude Design, prompt 4 — intent module and hint fade:**
> Build the help/harm intent module: a two-state pip toggle, with modifier hint chips (shift — swap once, ctrl — disarm, alt — grab) beside it. Add a "first session" vs "experienced" toggle that swaps the hints between fully labeled and minimal unlabeled ticks, so we can check the fade doesn't lose legibility.

Comms-specific prototyping prompts now live in `comms.md`.

**Cursor, prompt 1 — data contract first:**
> Here's a HUD spec (doc + mockup screenshots attached). Before touching UI: define or extend the data structures for (a) per-limb damage snapshot exposed to the vitals cluster and on-demand readout, and (b) the seven-zone raycast hit result feeding into the existing combat/medical damage application. Show me both before wiring any screen.

**Cursor, prompt 2 — one vertical slice:**
> Implement the vitals cluster and screen-space condition feedback only, wired to a real character's damage state, matching the mockup. Zone targeting and the hands/gear strip come after this is reviewed.
