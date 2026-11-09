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

Nothing else is persistent. **A machine's screen replaces the HUD while it's open.** Using an APC, an air alarm or a vending machine puts its diegetic screen at the center and hides this layout until you step away — there's nothing else you should be doing with your hands at that moment, and two layers of UI would just compete.

## 5. Vitals cluster — doll replacement, part 1

A small always-on card, not a body diagram: an aggregate condition read plus four thin bars (brute, burn, toxin, oxy), each rendered as a labeled icon + bar rather than a silhouette. Bars that are at zero stay visually quiet — no "OK" clutter, just a flat empty bar — so the cluster only draws the eye when something's actually wrong.

**Screen-space feedback carries the rest of the load that the doll used to:**

| Condition | Feedback |
|---|---|
| Low oxygen | Peripheral desaturation / cold tint |
| High brute/burn pain | Red screen-edge pulse, intensity scales with damage |
| On fire | Heat-shimmer distortion at screen edges |
| Critical / dying | Slow heartbeat audio (personal tier, `audio.md` §4) + a visual pulse synced to it |

This is diegetic in the same sense the hacking interface is diegetic — the player learns their own state the way they'd learn it in real life, by how things *feel*, with the panel as backup, not primary.

**On-demand detail:** examining yourself (`examine.md` §2) describes your condition in plain sentences — "your left arm is badly bruised", "you feel light-headed". A per-limb readout is a later pass (`health.md` §7).

**The body itself is the doll now:** damage renders directly on the character model — visible wounds, blood, burns, a limp on a hurt leg — both to the player (via self-view/camera framing) and to anyone else looking at them. This is a genuine upgrade over the old system: a medic can eyeball someone's injuries at a glance without opening anything, which SS13's 2D sprites couldn't really do.

## 6. Zone targeting — doll replacement, part 2

Direct world-space aiming: you point at the part of the body you mean, and that's the zone.

**Zone set — trimmed from SS13's roughly eleven click-zones to seven:** head, chest, l_arm, r_arm, l_leg, r_leg, groin. Hand- and foot-specific effects (disarm, drop item, trip) move into the arm/leg zone's effect table instead of needing pixel-precise hits on a hand or foot — that precision made sense on a static 2D doll with generous click targets; it's punishing to demand in fast, camera-relative 3D combat.

**Reticle behavior:** the crosshair shows a small zone-label chip when hovering a valid limb on a targetable character (e.g. "chest"), confirming the target before it's committed. That's the same "know what you're about to hit" value the old doll gave, just resolved in the world instead of a side panel. Applies uniformly to ranged and melee — melee reads the same hover-and-confirm at contact range.

**Zone selection works in every intent**, not just harm. Bandaging a leg, splinting an arm or injecting a shoulder is a help-intent action that needs a zone just as much as a punch does — SS13's body doll selects a zone whatever your intent is, and this keeps that.

**Self-targeting:** medical self-care reuses the same reticle system aimed at your own model (self-view / look-down), rather than reviving a doll just for that one case.

**Open dependency — flag for camera team:** this reads correctly on screen only if the camera shows enough vertical separation of a body (head above chest above legs) to aim it, which an isometric/three-quarter camera does and a true orthographic top-down camera doesn't. If the camera lands closer to pure top-down, the fallback is screen-space vertical banding — cursor's vertical offset from the target's on-screen center maps to a zone — same player-facing result (hover, see a zone label, click), different resolution method underneath. Worth confirming before this is implementation-locked.

## 7. Intent & combat verbs

SS13's four-way intent selector (help / harm / disarm / grab) collapses to a two-state help/harm toggle, with disarm and grab moved to modifier-chorded clicks instead of a menu:

| Input | Result |
|---|---|
| Plain click | Resolves per current intent (help = gentle/non-damaging, harm = attack) |
| Tap toggle key | Flips persistent help/harm state |
| Hold Ctrl + click | Disarm attempt |
| Hold Alt + click | Grab / escalate an existing grab |
| Tap R, then click | Throw the active hand's item at the clicked point |

Ctrl and Alt were picked deliberately: SS13 already trains players to ctrl-click and alt-click objects for shortcuts, so this reuses existing muscle memory rather than teaching a new one.

**Why this isn't just a rebind.** In SS13, switching from help to grab mid-fight costs an extra input — you have to change intent before you can act. Chording removes that tax: Alt+click grabs and Ctrl+click disarms regardless of what the base toggle is set to. Shift is not an intent modifier: it's examine (`examine.md` §4).

**Grab escalation stays on Alt+click, never falls through to plain click.** Once a grab is active, it's tempting to let a plain click on the grabbed target escalate it. Don't — that makes plain click mean different things depending on hidden state. Initiate and escalate use the identical gesture (Alt+click), so there's no silent context-switching for the player to trip over.

**Throw is a mode, not a chord.** R arms a throw; the next click throws the active hand's item at the point under the cursor, in help or harm alike, and the mode ends. A click with an empty hand or Esc ends it too. It's a toggle rather than a held modifier because aiming a throw takes a moment of looking: holding a key while you pick a spot is a worse hand position than tapping once. While armed, the world shows the throw itself instead of a cursor badge: a dashed arc from the hand to where the item will land, and a ring on the floor there. The arc stops short, in a warning colour, when the target is past what that item's weight lets you throw; heavy enough items can't be thrown at all and show only a red ring. Same SS13 lineage as the other verbs (SS13's throw toggle), minus its silent "the next click might be a throw" state, since the arc is on screen the whole time the mode is live.

**The cost is discoverability, and it needs a mitigation.** A four-icon selector is self-documenting — every option is visible, every time. A toggle plus three invisible modifiers is faster but only for players who already know the modifiers exist, which is exactly the kind of thing that ends up wiki-only knowledge in SS13. Mitigate with fading hints: show modifier labels (ctrl — disarm / alt — grab / shift — examine) next to the intent pip for new or infrequent players, and shrink them to unlabeled ticks once a player's demonstrably used each one a few times.

## 8. Hands & gear strip

Previously left out of scope on the interaction-menu pass; covered here.

- Bottom-center: two hand slots, unchanged from SS13's swap convention. Active hand gets a distinct border.
- A slim gear strip beside the hands: belt, ID, a pocket, back — icon-only, click to open that slot's contents. The ID slot holds either a bare ID or a PDA with the ID inside it; there's no separate PDA slot (`id-access.md` §3). The full storage panel is `inventory-storage.md` §6.
- Drag-and-drop between hands, gear strip, open storage panels and the world is one consistent drag language.

## 9. Alerts

Top-right icon stack. Restyled only — same underlying set as SS13 (pressure, temperature, fire, radiation, hunger/thirst, pulling, restrained), plus the acute health alerts **Bleeding** and **Cardiac arrest** (`health.md` §7) and **Knocked down** (`combat.md` §5). Icons are hidden, not grayed out, when inactive, so the stack is empty on a healthy, unencumbered character and only fills in with what's actually true.

**Two severities.** An alert can be a **warning** (act soon — light bleeding, pressure dropping) or **critical** (act now — heavy bleeding, a stopped heart, vacuum). The colour says which, so the stack reads at a glance without opening anything.

## 10. Comms

Comms grew into its own system — local speech bubbles, distance/occlusion, crowd handling, radio and other non-positional channels, whisper/shout/announcement behavior, message composition, and the history log. It's spun out into its own document: `comms.md`. This doc keeps only what the main HUD needs to know: the feed's corner in §4.

## 11. Integration notes

| HUD element | Touches existing system |
|---|---|
| Vitals cluster, on-demand readout | Per-limb damage model (unchanged data layer) |
| Zone targeting | Combat/medical damage application, character skeleton/rig |
| Hands, gear strip | `inventory-storage.md` §3, §6 |
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
| 5 | Player checks severity | Examines themselves: "your left arm is bleeding badly" |
| 6 | Player calls for a medic | Switches the chat to the medical radio channel and asks for help (`comms.md` §5) |

## 13. Out of scope for this pass

- The storage panel itself — `inventory-storage.md` §6
- Any ability/action-bar row (no evidence this exists in SS3D currently — not assumed here)
- The ghost's screen — `observer.md` §3
- A per-limb self-readout (sentences only in this pass)
- Settings/options menus

