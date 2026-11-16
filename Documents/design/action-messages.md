# Action messages — design document

> Status: active

Defines action messages: the short line that tells you what someone just did — "Marcus Voss flicks the light switch", "someone in a red hardsuit plants something on the door", "you hear glass shatter". SS13 prints these into the chat log for nearly every action; SS14 shows them as brief popups. This doc decides which actions get one, who perceives it, and where it shows. Its sibling is `examine.md`, which covers what something *is*; this covers what someone *does*.

## 1. Design philosophy

**Animation shows the motion; the line names what it couldn't.** SS3D characters animate their actions, and the world carries a lot on its own — a door opening, a light coming on, a wound on the model (`main-hud.md` §1 rule 1). But an animation often shows only that someone did *something*. A hand reaching to the wall could be a light switch, an APC or a lock; a hand moving to someone's arm could be a bandage or a syringe. Where the onlooker can't tell **what was used, on what, and whether it worked**, a short line fills the gap. Where the world already said it, no line is needed.

**Not a chat log, not silence.** BYOND's version scrolls every action into the same box as speech and radio, so the important ones drown. Narrating nothing loses SS13's information density, which `vision.md` §3 says 3D must not cost. The middle path: a few momentary lines anchored at the actor, the ones that matter kept in history.

**Actions on people are always worth a line.** Trust, investigation and antagonist play all run on knowing who did what to whom. Whatever an animation shows, an action done to another person is always narrated.

## 2. What gets narrated

The test for any action: **could an onlooker tell from the animation and the world what was used, on what, and whether it worked?** If not, it gets a line.

- **Actions on another person** — always: treating, injecting, restraining, taking an item, attacking, examining them up close (`examine.md` §4).
- **Actions on objects where the animation is ambiguous** — flicking a switch, using a console, prying a crate, tagging a package, arming a device.
- **Not narrated** — actions the world already shows plainly: walking, picking an item off the floor in plain view, opening a door that visibly opens.

Each action's own design doc decides whether it's narrated, by this test, and at which visibility level (§4).

## 3. Wording and names

**Others see a third-person line**: "Marcus Voss injects Ana Ruiz with a syringe." The line names the object, never its hidden contents — what's in the syringe is the same "internals" examine keeps off-limits (`examine.md` §5).

**Names follow examine's identity rule** (`examine.md` §5): the name on a presented ID, otherwise a short visible look — "someone in a red hardsuit", "someone in an engineering jumpsuit". Never a bare "Unknown"; a look tells the onlooker something and keeps disguises working (`combat.md` §6).

**Your own actions show only what you can't see.** Your animation already confirms the routine; what you need is why something didn't work and what came of it:

- **Failures and reasons** — "It won't budge." "You need a free hand." "Your hands are full."
- **Hidden results** — "The light doesn't come on." "Nothing happens."

**Failure text is private.** Onlookers never get a line for someone else's failure. They get the world's own feedback — a door's denied buzz, a locker that stays shut. A line saying "X tries the door; it doesn't open" would reveal that X lacks access, which examine keeps private (`examine.md` §5).

## 4. Who perceives it

**Three visibility levels.** Each narrated action has one; its own design doc picks it.

| Level | Who sees the line | Typical actions |
|---|---|---|
| Public | Anyone with line of sight (`comms.md` §3 rule) | Switches, consoles, attacks, restraining |
| Close | Only those within a tile or two | Slipping something into a drink, planting a charge, quiet tool work |
| Target-only | Only the person it's done to | Being looked over up close (`examine.md` §4), a blind reach into your pocket (`examine.md` §5) |

**There is no fully silent level.** Someone standing right beside you always has a chance to notice. That's SS13's balance: stealth comes from choosing where and when, not from a mechanic that hides you outright.

**Sounds reach past sight.** A loud or distinctive action — something breaking, a gunshot, a door bolting, a struggle — also gives a **sound line** to those who can hear it but can't see it: "You hear glass shatter." It uses the same hearing range as speech (`comms.md` §3), and it shows toward the sound's rough direction, not on the hidden source. What you can see you can examine; what you can hear you know about.

## 5. Where it renders

**A short chip anchored at the actor**, using the same grammar as a speech subtitle (`comms.md` §3) — flat surface, hairline border, fades after a few seconds — but styled so it never reads as speech: smaller, italic, quicker to fade. A sound line sits at the edge of the view toward the sound.

**One shared crowd cap.** Action chips share the per-viewer cap speech already uses (`comms.md` §4); two floating-text systems competing near the same heads would be worse than either alone. Within that cap, speech and shouts outrank action lines; among action lines, hostile actions and actions done to you outrank routine ones.

## 6. History

Every action line you perceived is logged to the comms feed's scrollback (`comms.md` §9) under an **Actions** filter, on by default. Live, the line is only the momentary chip; the record is where investigation happens — "who took the disk, and when?" Lines you didn't perceive are never backfilled, the same rule comms applies to whispers.

## 7. Worked examples

**A — Flicking a light switch in a busy room:**

| Step | What happens | Who sees what |
|---|---|---|
| 1 | Marcus flicks a switch; the light comes on | Others in sight: "Marcus Voss flicks the light switch" (public). Marcus: nothing — he saw it work |
| 2 | The room has no power; Marcus flicks it again | Marcus: "The light doesn't come on." Others: the same public line; the dark room says the rest |

**B — A quiet pocket reach:**

| Step | What happens | Who sees what |
|---|---|---|
| 1 | A traitor reaches blind into a conscious officer's pocket via Search (`examine.md` §5) | The officer: a target-only warning. A crew member standing beside them: nothing — the reach itself is target-only |
| 2 | The traitor pulls out the ID card and pockets it | The take is an action on a person: a close line for anyone within a tile or two; logged for the officer |
| 3 | Later, the officer scrolls the feed's Actions filter | The warning and the take are there, with the traitor named by ID or look |

**C — Nuke ops at the vault door:**

| Step | What happens | Who sees what |
|---|---|---|
| 1 | An operative plants a breaching charge (`explosives-destruction.md` §6) | Crew within a tile or two: "someone in a red hardsuit plants something on the door" (close) |
| 2 | The charge blows | Crew in sight see the blast. Crew down the corridor, out of sight: "You hear an explosion" toward its direction |

## 8. Integration notes

| Element | Touches existing / needed system |
|---|---|
| Names and the look descriptor | `examine.md` §5 identity rule; `combat.md` §6 disguises |
| Examine cue, blind-take warning | `examine.md` §4–§5 |
| Line of sight, hearing range | `comms.md` §3 |
| Chip grammar, crowd cap | `comms.md` §3–§4 |
| History and filter | `comms.md` §9 |
| World feedback carrying the rest | `main-hud.md` §1 rule 1 |
| Hidden contents stay hidden | `examine.md` §5 |
| Taking from people | `inventory-storage.md` §9 |
| Charges and blasts | `explosives-destruction.md` §6 |

## 9. Out of scope for this pass

- The exact narrated line, and visibility level, for each action — each action's own design doc assigns those by §2's test
- Exact fade timing and distances for the close level and sound lines (balancing, not design)
- Emotes as a player verb (*waves*, *nods*) — a comms question, not designed here
- Sense loss changing what you perceive: blindness turning sight lines into sound lines, deafness removing sound lines
- Admin or observer views that see every action regardless of visibility (`observer.md`'s territory)
