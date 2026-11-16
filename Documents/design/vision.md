# Vision — design document

> Status: active

SS3D is a 3D Space Station 13. Every other design doc specifies one system; this one says what all of them are for. It names the pillars that make SS13 what it is, and what 3D has to add without breaking them. A feature, a milestone or a design doc that serves none of the pillars is a question to ask, not work to start.

## 1. Design philosophy

The pillars describe what makes SS13 SS13, not a list of its systems. SS13 has hundreds of systems; most of them exist to serve a handful of experiences: a crew of strangers each with a job, a station whose machinery reacts to everything, someone among them who isn't what they seem, and a round that builds to a crisis and ends. Port the systems without those experiences and the result is a space-station sandbox, not SS13. Keep the experiences and many of the systems can be thinner, different, or later.

The same test runs through the other docs as "native vs leftover" (e.g. `explosives-destruction.md` §1, `construction.md` §1): keep what is good about SS13 regardless of engine, drop what was a BYOND limitation. This doc is where that test points.

## 2. The pillars

1. **Everyone has a job on a station that runs.** A role gives each player something to do and a reason to deal with others: Security keeps order, Engineering keeps the power and the hull intact, Medical keeps people alive. Jobs depend on each other.
2. **Deep, interlocking simulation.** Air, power, bodies and structure react to each other. A blown wall vents a room, a vented room suffocates the people in it, a cut cable darkens a department. Stories come from the simulation, not from scripts.
3. **Hidden antagonists among the crew.** Some players are not what they claim to be. Paranoia, deception and trust make the round social, not only a fight.
4. **A round has a shape.** A calm start, escalation, a crisis, an ending (evacuation or catastrophe), then a summary of what really happened, and the next round.
5. **Anything can be interacted with.** Examine it, pick it up, take it apart, hack it, build with it, hit someone with it. The world is a sandbox of physical objects.
6. **3D presentation of a single-plane station.** See §3.

## 3. What 3D adds, and what it must not lose

**The station is one floor on a grid**, played like a 2D RPG seen in 3D. There are no Z-levels for now, and systems may assume a single plane until a design doc says otherwise (`atmospherics.md` §12 and `explosives-destruction.md` §9 already do).

3D earns its keep through:

- **Lighting and darkness** — a dark room is a real obstacle and a real piece of information (`rendering-lighting.md`).
- **Line of sight and occlusion** — what you can't see, you can't shoot or hear (`combat.md` §3, `comms.md` §3).
- **Physical bodies, objects and breaches** — ragdolls, debris, a hole you can see the stars through.
- **Damage you can read at range** — a cracked wall, a wounded body, a sparking machine (`explosives-destruction.md` §3, `health.md` §5).

It must not cost SS13's **information density**. At any moment a player can tell who is who (`combat.md` §6), what is happening around them, and what they can interact with (`examine.md`). Where 3D makes something harder to read than the 2D original — a smaller silhouette, an occluded state, a camera angle — that is a problem to solve, not a trade to accept.

## 4. Using the pillars

- Every playable milestone names the pillars it advances. Enabler work (servers, tooling, testing) names the playable milestone it serves.
- A design doc's opening says which pillars its system serves. Existing docs aren't rewritten for this; it applies when a doc is next revised.
- When two features compete for the same time, prefer the one that moves a pillar the current milestone hasn't touched yet.

## 5. Worked examples

| Question | Pillar test | Outcome |
|---|---|---|
| Should Security get a stun baton before Nuke Ops is playable? | #1: a distinct, non-lethal Security job; #6: a knockdown is readable to everyone around | Yes (`combat.md` §5) |
| Should handcuffs come with it? | Against nuclear operatives the goal is killing, so arrest serves no pillar yet; in an open station round it serves #1 and #3 | Later, with the station round |
| Should blasts damage floors? | #2: a hole to space vents the room; #1: Engineering has a hull to fix; #6: a breach you can see | Yes (`explosives-destruction.md` §3) |
| Should the station get a second deck? | #6 says single plane for now; no pillar is blocked without it | No |

## 6. Integration notes

- Every other design doc in this folder serves one or more pillars; this doc doesn't change any of their rules.
- `main-hud.md` §1 — minimal chrome is pillar #6's information-density rule applied to the HUD: the world carries the information.
- `antagonist-content.md` — pillar #3's antagonists and pillar #4's round shapes.
- `round-end.md` — pillar #4's ending and summary.

## 7. Out of scope for this pass

- Multi-deck stations, Z-levels and vertical play.
- Ordering milestones or features — that's milestone planning, which reads this doc but isn't specified here.
- Art direction and visual style beyond what pillar #6 requires for readability.
