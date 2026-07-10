# Rendering & Lighting — design document

> Status: active

Resolves the open item flagged during the light-budget system work: dynamic lighting had a distribution/culling shape (Hero/Key/Ambient/Effect tiers, room-graph culling, shadow-slot budget) before it had a visual philosophy tying it to what SS13 actually is. This doc is that philosophy, plus the post-processing and shading decisions that sit on top of it. Builds on Area's power/APC model (`area.md` §5) for where light comes from, and the shared occlusion raycast already established across comms (`comms.md` §3), the hacking interface's wireless scan (`hacking-interface.md` §2), and combat cover (`combat.md` §3) for how it's blocked.

## 1. Design philosophy

Same test this whole project keeps applying, aimed at rendering instead of UI or mechanics: is a piece of visual convention here because it's genuinely right for this game, or because it's a default inherited from a different kind of game? An outdoor-lit gradient sky with a directional "sun" as primary illumination is that second thing — it's the default for an open-world or outdoor game, and SS13 is neither. A space station has no sun. Every photon in it is either coming from a fixture someone installed and something is powering, a hazard that shouldn't exist, or a tool someone's carrying.

That has a direct mechanical consequence this doc treats as load-bearing, not aesthetic: **light should be scarce, authored, and power-dependent**, because the game already models it that way. Area already ties lighting to its APC (`area.md` §5, extended in §4 below) — "lighting going out when an area loses power is the same event, not two." If the renderer papers over a dead APC with ambient fallback fill, that mechanical beat gets visually undercut: sabotage a breaker, and the room should actually go dark, not just dim.

**Toon/half-toon shading changes what lighting needs to supply, and that's a second reason the sun-and-sky default is wrong here.** A PBR pipeline chases physically plausible response across many light sources. A toon/half-toon ramp does most of the visual work itself — banded diffuse, smoother specular/rim — off of far fewer lights. What that ramp actually needs is a clean lit/shadow split to band against, which a single authored fixture per room supplies better than a diffuse ambient wash would.

Three rules fall out of this:

1. **No ambient fallback "sun."** Base light level is near zero by default. What a player sees, they see because something in the world is actually lit — a fixture, a tool, a fire.
2. **Light sources map to something physical and powered**, the same "everything on screen maps to something physical" rule the hacking interface already established for its panels. A light exists because a fixture exists, on a circuit, on an Area's APC.
3. **Darkness is diegetic information, not an absence of information.** A dead or emergency-lit room is telling the player something (power's out, something's wrong) the same way a dark alert chip or a bleeding wound already do elsewhere in this project — it doesn't need a HUD icon layered on top to say "the power is out here," the room already says it.

## 2. Visual style & shading model

- **Low-poly geometry, smooth-shaded** — normals smoothed rather than faceted, which is most of what gives SS3D's models their soft, near-pastel read even before color grading touches anything.
- **Toon / half-toon shader** — banded diffuse response (a ramp texture, not a hard binary cel-cutoff) with a smoother, less-banded specular/rim pass on top. This is what "half-toon" buys over full cel-shading: enough stylization to read as toon, without the harsh graphic-novel look full cel-shading tends toward on already-low-poly geometry.
- **URP, Forward+ render pipeline.** The practical payoff for this project specifically: Forward+ removes the old per-object light-count ceiling that made many-small-lights setups expensive. That's a direct fit for a station with dozens of rooms each carrying their own fixture(s) — which is exactly the light-budget/tiering system already built. Tiering still matters (see §3), but it's now primarily about shadow cost and visual priority, not a hard light-count wall.

## 3. Light sources & hierarchy

Reframes the four tiers already established in the light-budget manager (Hero / Key / Ambient / Effect) around what they physically are in a station, rather than treating them as generic importance buckets:

| Tier | What it is, here | Powered? |
|---|---|---|
| **Hero** | Personal light — flashlight, helmet light, welding torch | Carried, battery-powered, independent of Area APC |
| **Key** | The room's own fixture(s) — the primary light source for wherever the player currently is | Tied to that Area's APC |
| **Ambient** | Bleed-through from adjacent rooms — light visible through windows, open doors, a lit room 1-2 hops away | Tied to *that* Area's APC, not the viewer's |
| **Effect** | Hazard light — fire, sparking consoles, plasma, hull-breach arcs, explosions, muzzle flashes | Not powered by any Area; exists because something is actively wrong |

**Hero tier's priority just went up.** In a station that can go genuinely dark rather than dimly ambient-lit, a personal light source stops being a nice-to-have and becomes the thing standing between a player and not being able to see at all. It should keep the "always eligible for a shadow slot, never culled" treatment the light-budget system already gives it, but this doc treats that as a real gameplay necessity, not just a rendering nicety.

**Effect tier is often the only light in the room, deliberately.** A fire in a room whose APC was cut on purpose, with no other light source active, is a genuinely strong SS13 moment — worth protecting rather than accidentally flattening with ambient fill that would have washed it out anyway.

## 4. Area lighting states

This is now a real addition to `area.md` §5's Lighting bullet, not just a rendering-layer convention layered on top of it — see the companion edit to that doc. Summarized here for this doc's own completeness:

Each Area's lighting sits in one of three states, driven directly by its APC's power state:

| State | Trigger | What it looks like |
|---|---|---|
| **Normal** | APC powered | Full fixture set active, at the Area's authored color/intensity |
| **Emergency** | APC unpowered, backup battery available | A distinct, reduced subset of fixtures — dim, red-tinted, battery-backed. Not a red-tinted variant of normal lighting; a genuinely separate authored state |
| **Dark** | APC unpowered, no backup remaining | No Key-tier light at all. Only Hero (personal light) or Effect (hazard light) sources illuminate anything in the room |

**Emergency lighting is worth treating as its own state because it's one of the most recognizable "something is wrong" signals SS13 has**, and it's free diegetic information exactly per §1's third rule — no alert icon required, the room itself says it. Collapsing it into "normal lighting, but red" would lose the distinct fixture read (usually fewer, specific emergency units, not every fixture dimmed).

**Dark is genuinely dark, not dim.** This is where §1's "no ambient fallback sun" rule actually gets tested — if base ambient light quietly keeps a "dark" room visible anyway, this state doesn't mean anything.

## 5. Occlusion — a fourth consumer of the shared raycast

Comms occlusion, the hacking device's wireless-scan range, and combat cover already share one raycast/occlusion system rather than each maintaining its own (`combat.md` §3 makes this explicit: "one raycast system, three consumers, no parallel implementation"). Light occlusion — what's actually lit by a given fixture versus what's in its shadow — is a natural fourth consumer of that same system rather than a separate light-occlusion query. A wall blocks a fixture's light exactly the same way it blocks a subtitle bubble or a wireless scan.

## 6. Departmental color, as an authored extension of Area

Area already carries a per-area `ambience track id` for audio (`area.md` §5). A per-area light color/temperature is the same kind of authored, per-area data — medbay reading clean and cool, engineering warm and industrial, security colder-toned — giving a player free wayfinding (which department they're in, by light color alone) the same way the ambience track already gives them free audio wayfinding. Flagged here as a natural extension of that existing field rather than a new system, but not fully spec'd in this pass — see §11.

## 7. Shadows

With no directional sun, shadows are primarily cast by Key-tier fixture lights (and Hero-tier personal lights), not a cascaded directional shadow map. This actually simplifies the light-budget system's existing shadow-slot budget rather than complicating it — it was already built around "which lights currently hold a shadow-casting slot," and that logic barely changes; there's just no directional-light special case to carve out.

**Shadow color shouldn't default to near-black.** Most toon/half-toon shaders let the shadow band be tinted rather than darkened toward black — this is a real part of getting the pastel, soft read rather than harsh dark patches on low-poly geometry, and it matters even more in genuinely dark rooms where players are relying on a flashlight cone to make out shape by contrast.

## 8. Post-processing stack

- **Bloom** — the highest-value single addition, and more important than it would be in a normally-lit game specifically because of §1–4: a flashlight cone cutting through real darkness, or emergency-red bleeding into a dark corridor, both depend on bloom to read as soft light rather than a hard-edged cone.
- **Color Adjustments** — has to work across the full range this doc now implies: a fully-lit medbay and a near-black depowered maintenance tunnel, without crushing shadow detail to pure black in the tunnel case — players still need to make out silhouettes by flashlight there.
- **Tonemapping** — Neutral over ACES, same reasoning as before: ACES crushes toward filmic contrast, which fights a deliberately soft, authored pastel palette more than it helps it here.
- **Vignette** — subtle at most. Real screen-edge feedback is already doing communicative work (comms occlusion, low-oxygen desaturation, fire heat-shimmer, critical heartbeat pulse, per `main-hud.md` §5) — a strong vignette competes with those instead of sitting quietly underneath them.
- **Ambient Occlusion** — mostly moot now rather than just de-prioritized. AO's job is softening gradient darkening in corners of an otherwise-lit scene; a lot of the station is now going to *be* dark rather than dimly gradient-shaded, so there's often no ambient light there for AO to gradient in the first place.
- **Outlines** — a renderer feature (inverted hull or normal/depth edge detection), not strictly post-processing, but belongs in this stack. Reinforces silhouette read at low-poly, which has a real gameplay payoff beyond aesthetics: it directly helps the zone-targeting reticle system (`main-hud.md` §6) read a character's silhouette clearly, including in the darker rooms this doc now assumes exist.

## 9. Integration notes

| Rendering element | Touches existing / needed system |
|---|---|
| Key-tier fixture lighting, Area lighting states | Area → APC power derivation (`area.md` §4, §5) |
| Hero-tier personal lighting | Held-item/tool convention (flashlight, welding torch as equippable items) |
| Effect-tier hazard lighting | Atmospherics (fire, plasma) — same system already driving `main-hud.md` §5's heat-shimmer feedback |
| Light occlusion | Shared raycast/occlusion system (`comms.md` §3, `hacking-interface.md` §2, `combat.md` §3) |
| Shadow-slot budget | Existing light-budget/tiering manager (room-graph distance, shadow-slot cap, reevaluate interval) |
| Departmental color | Area's existing per-area ambience-track field (`area.md` §5), extended per §6 here |
| Outline pass | Zone-targeting reticle legibility (`main-hud.md` §6) |

## 10. Worked example — Engineering bay loses power mid-fire

| Step | What happens | Lighting state |
|---|---|---|
| 1 | Engineering — main bay is fully powered, normal round | Normal state: full fixture set, warm industrial color temperature per §6 |
| 2 | A fire breaks out and damages APC-ENG-03 | APC drops offline |
| 3 | Backup battery kicks in | Area transitions to Emergency: dim, red-tinted, reduced fixture subset — distinctly different from normal, not a red filter over it |
| 4 | Backup battery depletes under sustained draw | Area transitions to Dark: Key-tier light drops out entirely |
| 5 | Only the fire itself lights the room now | Effect-tier hazard light (fire) is the sole illumination — genuinely dark elsewhere in the bay |
| 6 | A crew member arrives with a flashlight | Hero-tier personal light is now the only other source in the room, cutting a real cone through real darkness |
| 7 | Power is restored | Area transitions back to Normal; fixtures return to their authored color/intensity |

## 11. Out of scope for this pass

- Exact numeric light intensities, color values, and shadow-tint values (a lighting/tuning pass, not a design decision)
- Departmental color-temperature's exact data shape as an Area field (§6 flags the extension, doesn't fully spec it)
- Global illumination / lightmapping strategy for static geometry
- Volumetric fog parameters for gas/smoke interaction with dynamic lights (flagged as worth it in earlier light-budget discussion, not detailed here)
- Any planetside/exterior station variant that would reintroduce a real directional sun — this doc assumes a space station interior throughout
- Weather or exterior lighting of any kind

## 12. Prototyping this

This is a rendering/visual-tuning pass, not a UI-mockup one — the natural next step is direct in-Unity iteration (Volume profiles, shader ramp tuning, fixture placement) rather than a Claude Design prototype, the same way Area's own data-architecture pass skipped Claude Design for Cursor. The Area lighting-state switching, though, is real gameplay logic and fits the established data-contract-first pattern:

**Cursor, prompt 1 — data contract first:**
> Here's the rendering/lighting doc and the updated Area doc's lighting-states section. Define the three-state Area lighting model (Normal / Emergency / Dark) as data — what triggers each transition given APC power state and backup battery charge — and how it feeds the existing light-budget manager's Key-tier light list per Area. Show me the data contract before wiring any transition behavior.

**Cursor, prompt 2 — one vertical slice:**
> Wire one Area's lighting-state transitions end to end: APC loses power → Emergency (reduced fixture subset, red tint, battery drain) → Dark (Key-tier light removed) → Normal on power restore. Use Engineering — main bay as the test case. Departmental color temperature and the outline renderer feature come after this is reviewed.
