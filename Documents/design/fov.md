# Field of view — design document

> Status: active

Defines field of view: what your character can see right now, what the rest of the station looks like on screen while they can't see it, and the one line of sight that the rest of the game asks. `examine.md` §2, `action-messages.md` (public lines), `combat.md` §3 and `comms.md` §3 each assume there is a line of sight; this doc says what it is for sight and how it looks. It serves pillar 3 (what you can't see can hide an antagonist) and pillar 6 (line of sight is one of the things 3D has to earn, `vision.md` §3).

## 1. Design philosophy

SS13 and SS14 hide everything your character can't see behind solid black, and remember nothing. You stand at a closed airlock and know nothing about the room behind it until it opens. On a station where airlocks are airtight, that is the right feeling: every closed door is a question, and opening one is a small risk. This doc keeps it.

What 3D changes is the camera. In SS13 the camera looks straight down, so black tiles line up with the walls that cause them. Here the camera can tilt and look past a corner your character can't see round. Sight still comes from the character, never the camera, so the black has to read as "behind that wall", not as a shadow floating in the room.

Four rules follow:

1. **You see what your eyes reach now.** Nothing is remembered; a room you walked out of goes black behind you (§3).
2. **Hidden is black, never dim.** Out of sight shows nothing at all, not a darkened view of the layout (§3).
3. **People and things are shown whole or not at all.** Nobody is drawn cut in half by the edge of sight (§4).
4. **Nothing hidden leaks.** A soft edge, a shadow or a far-side poster never gives away what's out of sight (§3, §4).

## 2. What blocks sight

Sight is all around you, not a cone in front of you, and it runs level across the station's single plane (`vision.md` §3).

| Blocks sight | Doesn't block sight |
|---|---|
| Walls, including a plated girder (`construction.md` §6) | Windows (`construction.md` §7) |
| Closed doors and airlocks, whatever your access | Open doors; a door opening reveals the room as it opens |
| | A bare girder (`construction.md` §6) |
| | Furniture, machines, lockers, crates and people, however tall |

Furniture never blocks sight: the station reads as rooms and walls, and a vending machine is something you see past. It can still be cover or muffle a voice (§8).

How far you can see is balancing, not design. It must be far enough that a lit corridor never ends on screen before it ends in the station.

## 3. What hidden looks like

**Solid black.** Everything out of sight is fully black: floor, walls, furniture, whatever is lying there. There is no dimmed layout, no outline, no memory of what was there.

**Space stays visible.** The stars and the void beyond the hull are never hidden; there is nothing to learn from them. Anything out there, a body, debris or an operative on a spacewalk, follows §4.

**A wall is seen whole.** A wall you can see any part of is drawn whole, top and faces. The black starts at the wall's outline, so it reads as "behind this wall". It never runs along the middle of a wall or blacks out a wall face standing in plain view.

**Edges soften inward only.** Where sight ends, the visible side fades into the black, so edges don't look like stair steps or broken lines. The fade only darkens what you can see; it never lifts the black off something hidden.

**Darkness is a different thing.** A room in sight with its power out is dark in the way `rendering-lighting.md` §4 describes: a flashlight finds shapes in it. A room out of sight is black whatever its lighting. Dark tells you something about the room; black tells you that you can't see it.

## 4. People, things and what they give away

**Whole or not at all.** A person, an item or anything else that moves is shown whole as soon as any part of it, across your line of sight, is in view, and not at all otherwise. A shoulder sticking out past a door jamb shows the whole person. The edge of sight never draws someone in half.

**Wall-mounted things belong to the floor in front of them.** A poster, an APC, an air alarm, a light fixture: you see it when you can see the spot you would stand on to use it. The poster on the far side of a wall you can see stays hidden.

**Hidden people cast no visible shadow.** A shadow falling into view would show where they are.

**Light still travels.** A flashlight beam, a fire or a sparking machine round the corner still lights the floor you can see. The source stays hidden, but the light is honest information: someone is coming with a torch. Sound already reaches past sight the same way (`action-messages.md`, sound lines).

## 5. Sight comes from the eyes, not the camera

The camera is free to tilt and turn; sight isn't. What's round the corner stays black even when the camera can see round it. Because walls in view are drawn whole (§3), the black begins at their outlines rather than in the open room.

Ghosts see everything (`observer.md` §2), and authoring the station shows everything (`creative-mode.md`); neither has a field of view.

## 6. One line of sight

The game decides with the same line it draws. What isn't in sight can't be examined (`examine.md` §2), doesn't give you its public action line (`action-messages.md`), and isn't something you saw happen.

The soft edge doesn't change that: something at the dim edge of view is in sight, and nothing drawn black is. Where the picture and the rule could ever disagree, the picture may hide a little more than the rule, never less.

## 7. Worked examples

| Situation | What you see |
|---|---|
| You stand at a closed airlock you have no access to | The airlock, and black behind it. You learn what's there when it opens |
| The airlock opens | The room behind it appears as the doors part, as far as you can see through the doorway |
| You look through a window into the next room | The room, as if the window weren't there for sight |
| An operative waits behind a door jamb with a shoulder showing | The whole operative |
| The same operative steps fully behind the jamb | Nothing: no body, no shadow. If they carry a flashlight, its light still falls on the floor you can see |
| A poster hangs on the far side of a wall you're looking at | The wall, whole; not the poster |
| You tilt the camera low next to a corner | The corner's walls in full; beyond their outlines, black floor and black walls up to the sky, which stays visible |
| Crew build a line of bare girders across a corridor | You see through them until they're plated (`construction.md` §6) |
| You walk out of a room | It goes black behind you; nothing is remembered |

## 8. Integration notes

- `vision.md` §3 — line of sight is one of the things 3D adds; this doc defines it for sight.
- `examine.md` §2 — examine targets only what's in sight, as defined here.
- `action-messages.md` — "anyone with line of sight" for public lines means in sight as defined here. Sound lines reach past it.
- `combat.md` §3 — "what you can't see, you can't hit" holds: sight is the most permissive of the three blockers. Furniture can be cover against a shot you can see past, and a closed door blocks both.
- `comms.md` §3 — speech occlusion can muffle on more than sight blocks (a workbench); a sealed airlock blocks both.
- `rendering-lighting.md` §4–§5 — darkness and light are separate from sight (§3, §4). Light round a corner shows; its source doesn't.
- `construction.md` §6–§7 — girder, plated girder and window sight rules are theirs; listed in §2.
- `observer.md` §2, `creative-mode.md` — no field of view for ghosts or authoring.

## 9. Out of scope for this pass

- Remembering explored areas or last-seen states.
- A facing cone, peripheral vision or looking over your shoulder.
- Eyes and eyewear: blindness, eye damage, night vision, mesons or x-ray, welding masks. Health's blurred vision (`health.md` §2) is a screen effect, not a change to what's in sight.
- Smoke, gas or fire blocking sight.
- Seeing through station cameras, an AI's view, or any remote viewpoint.
- Camera framing and tilt limits.
