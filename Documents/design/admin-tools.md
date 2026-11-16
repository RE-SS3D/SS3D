# Admin Tools — design document

> Status: active

Resolves a gap six other docs have all been carving out space for without ever designing: `pda.md` §3/§10 reserves an ahelp tab but defers "the ticketing system itself" here; `lobby.md` §5/§11 defers "an admin/host override" of resolution; `objectives.md` §12 defers "admin override of an objective's state"; `observer.md` §9 defers "admin/staff observer tooling (godmode, jump-to-player, stealth observation)"; `round-end.md` §2 already specifies "an admin ends it" as always available but not how; `persistence-save.md` §5/§6/§11 defers the moderation workflow and the recovery-override surface. `round-config.md` keeps only the settings screen and leaves the rest of the toolkit here. This doc is that toolkit.

## 1. Design philosophy

**This is explicitly out-of-fiction, like two other surfaces already are.** Lobby's job browser and creative mode's construction menu each earned a carve-out from "everything maps to something physical" because they're meta/administrative contexts, not in-fiction gameplay (`lobby.md` §1, `creative-mode.md` §1). Admin tools are the third. An admin toggling invulnerability or teleporting to a player doesn't need a diegetic justification any more than a lobby dropdown does — confining this exemption to these three surfaces is what keeps it from spreading into ordinary play.

**Detectable, not prevented — turned toward the admins themselves.** Every other doc in this project gives crew real, inspectable evidence instead of a hidden mechanism: id/access's per-device auth log, the AI's law audit trail, round config's draw history, persistence's round-history log. This doc runs the identical discipline on a different population. **Every admin action** — a kick, a teleport, a spawned item, a forced round start, a changed server setting — writes a real, timestamped line to this doc's own log: who, what, to whom, when. Permission *checks* aren't logged; actions are. Not because admins are suspects, but because "power is real and its use is logged" is the same bargain this project has made everywhere else, just extended above the simulation instead of inside it.

**Reuse the existing frameworks; this doc only fills the specific gaps.** Stealth observation is observer's ghost framework (`observer.md` §2–§3) plus one added privilege, not a second camera system. Ahelp is one more conversation in the chat a player already uses (`comms.md` §7), addressed to the admins. A permission tier is the same named, checkable, cumulative credential shape id/access already uses for crew (`id-access.md` §4–§5) — just a separate rail, because admin authority has nothing to do with a character's in-fiction job.

**World-intervention actions bypass the simulation on purpose — that's not a contradiction of "physical, not RNG."** That rule governs how the simulated world resolves outcomes for players. An admin teleport or heal is a deliberate, logged escape hatch sitting above the simulation, not a simulated event pretending to be one. The two rules govern different layers and don't conflict.

## 2. Permission tiers

A named, tiered role, entirely separate from the in-fiction ID/access rail (`id-access.md` §4) — a Captain player isn't automatically an Administrator, and an Administrator's character carries no special in-fiction access because of it. Two different rails.

| Role | Can do |
|---|---|
| **Restricted** | Play, with limits an admin has placed on them short of a ban (for example no OOC, no antagonist roles) |
| **User** | An ordinary player — the default for anyone who connects |
| **Administrator** | Answer ahelp (§3), mute, kick, ban, observe (§4), world intervention (§5), round controls and overrides (§6), and the server settings screen (`round-config.md` §2) |
| **ServerOwner** | Everything an Administrator can, plus granting and revoking other players' roles |

**Strictly cumulative** — each role can do everything the role above it in the table can. A separate Moderator role between User and Administrator (ahelp and mutes without bans or world intervention) is out of scope for this pass (§11).

## 3. Ahelp — a conversation with the admins

A player who needs help opens the **Admin help** channel in the Chat tab — the same chat surface they already use in the lobby and in the round (`comms.md` §7). It's **two-way**: the player writes, an admin answers, and both see the thread. Admins also have their own admin-only channel for talking among themselves.

- Writing to Admin help starts or continues a ticket thread — the same compose/read interaction as any other chat, no new UI grammar.
- Any on-duty Administrator sees the open queue; **claiming a ticket assigns it**, so two admins don't answer past each other.
- The full thread — player's message, replies, who claimed it, when it closed — is retained as the ticket's own permanent record, same "real, inspectable data, never summarized away" instinct as every other log in this project.

## 4. Stealth observation — observer's framework, one privilege added

An admin entering stealth observation *is* a ghost (`observer.md` §2, §3) — the identical free-flying, invisible, undetectable camera every dead or never-embodied player already gets, not a second spectator system. What's different:

- **Jump-to-player** — an admin-only camera snap, using the exact follow-lock every ghost already has (`observer.md` §3), just selectable from a full roster instead of only nearby points of interest.
- **Omniscient examine** — the variant `examine.md` §11 flagged and left unclaimed ("any admin/observer-only omniscient examine variant... not this doc's"). An admin in stealth observation examines any character or object for its full underlying state — the same content categories `examine.md` §7's table already defines per target type, just without that doc's §5 "always the public tier" restriction, because an admin isn't a crew member subject to the fiction.
- **Godmode**, for the rarer case an admin possesses a body directly (testing, demonstration) rather than staying a ghost — invulnerability and unlimited resources on that body, logged the same as every other action here.

## 5. World intervention

Teleport (to a player or a coordinate), spawn an item or creature, heal or damage a character, freeze/unfreeze — gated at Administrator (§2), a deliberate, logged escape hatch above the normal simulation.

**Playtest tools.** A few narrow interventions earn their place because they unstick a round without ending it: **reassign a player's job**, and **teleport a key object** (the nuke disk back to its vault, say). Same gate, same log line.

**Every intervention writes a real log line** — who, what action, target, timestamp — the same discipline as everything else in this doc. Nothing here is invisible fiat; it's a real action with a real record, same as any legitimate crew action already leaves one elsewhere in this project.

## 6. Round & objective override

**Round controls**, available from the lobby and during a round:

- **Start / force start** — begin the round now, skipping the countdown (`lobby.md` §7).
- **Hold** — pause the countdown, waiting for someone.
- **Extend** — add time to the running round's timer (`round-end.md` §2).
- **Cancel** — stop a countdown that's running.
- **End round** — `round-end.md` §2's "an admin ends it"; the reveal and summary still run.

**Other overrides** other docs defined the effect of but left open:

- **Place a player** — put a specific player into a specific job (`lobby.md` §5).
- **Objective-state override** — `objectives.md` §12's flagged manual correction of a pending/complete/failed record, for the rare bug or edge case that needs it.

None of these invent new round or objective logic — they're the identical state transitions those docs already defined, just given a real trigger and a log line here.

**Server presentation.** Administrators also edit what players see about the server: its name and the message of the day (`lobby.md` §2).

## 7. Moderation — kick, mute, ban, notes

`persistence-save.md` §5 already commits moderation notes and bans to disk as player meta; this doc is the workflow that writes them.

- **Mute** — silences a player's comms output (`comms.md`) for a duration, logged.
- **Kick** — disconnects the current session; no persistent record beyond the log line.
- **Ban** — kicks and blocks reconnection, keyed to the same stable identity `persistence-save.md` §5 already flags. On a server requiring `player-accounts.md` §3's verification, this is a real, reliable ban; on one that doesn't, a ban is only as good as the connection claiming that identity — the identical caveat that doc already states, not a new one invented here.
- **Notes** — a plain, timestamped remark attached to a player's record, visible to Administrators, never player-facing.

Appeal process, ban-duration conventions, and escalation policy are server policy, not designed here (§11).

## 8. HUD & touchpoints

No new permanent chrome for ordinary players — this entire doc is admin-facing.

- The admin toolkit itself is a real, deliberately-designed panel, the same out-of-fiction carve-out lobby's job browser and creative mode's construction menu already claim (`lobby.md` §1, `creative-mode.md` §1) — not diegetic, and not apologizing for it.
- The Admin help channel in the Chat tab (§3) is the only player-facing footprint. Once the PDA exists, it can mirror it as a tab (`pda.md`).
- Stealth observation reuses observer's existing camera and UI wholesale (`observer.md` §3) — no separate admin-camera interface to build.

## 9. Worked examples

**A — Ahelp ticket claimed and resolved:**

| Step | What happens | Admin-tools state |
|---|---|---|
| 1 | A player opens Admin help in the Chat tab and describes a stuck interaction | Ticket enters the open queue |
| 2 | An on-duty Administrator claims it | Ticket assigned; no second admin can claim it out from under them |
| 3 | Admin replies, confirms the issue, resolves it in-round | Full thread retained as the ticket's permanent record |

**B — Stealth observation catches a rule violation:**

| Step | What happens | Admin-tools state |
|---|---|---|
| 1 | An admin enters stealth observation, flies to a reported location | Identical ghost camera every player already gets, per `observer.md` §3 |
| 2 | Omniscient examine confirms a player is exploiting an interaction | Full underlying state visible, bypassing examine's normal public-tier restriction |
| 3 | Admin escalates to a mute and a logged note | §7's moderation actions fire; both write real log lines |

**C — Objective-state override after a bug:**

| Step | What happens | Admin-tools state |
|---|---|---|
| 1 | A steal objective's target item is destroyed by an unrelated explosion mid-round, leaving it uncheckable | Objective would otherwise never resolve cleanly |
| 2 | An Administrator manually sets the objective to failed | §6's override fires; `objectives.md` §2's record now holds a real, non-ambiguous state again |
| 3 | Action logs the same as any other admin action | Real, inspectable correction, not a silent database edit |

**D — A ban later found to be ckey-spoofed:**

| Step | What happens | Admin-tools state |
|---|---|---|
| 1 | An admin bans a player for repeated griefing | Ban keyed to the player's claimed identity, per §7 |
| 2 | The banned player reconnects under a different claimed ckey | Ban doesn't catch them — the exact limitation `persistence-save.md` §5 already flagged |
| 3 | Admin notes the evasion; real authentication (`player-accounts.md`) is the actual fix, and even that only raises the cost of evasion rather than eliminating it (`player-accounts.md` §5) | Consistent with the rest of this project's honesty about what it hasn't solved yet |

## 10. Integration notes

| Admin-tools element | Touches existing / needed system |
|---|---|
| Permission tiers | Separate rail, same shape as ID/access (`id-access.md` §4–§6) |
| Ahelp channel | The Chat tab (`comms.md` §7); later mirrored on the PDA (`pda.md`) |
| Stealth observation | Observer's ghost framework (`observer.md` §2, §3) |
| Omniscient examine | Flagged and left unclaimed by `examine.md` §5, §7, §11 |
| World intervention logging | Same "real, inspectable data" discipline as `round-config.md` §5, `ai-cyborgs.md` §4 |
| Round controls | `lobby.md` §7, `round-end.md` §2 |
| Server name and MOTD | `lobby.md` §2 |
| Objective-state override | `objectives.md` §12 |
| Moderation notes/bans storage | `persistence-save.md` §5 (player meta layer) |
| Ban reliability caveat | `persistence-save.md` §5's stable-identity limitation, inherited not re-solved |
| Server settings screen | `round-config.md` §2, §5 |

## 11. Out of scope for this pass

- **The full admin action roster and exact tier-to-action mapping** beyond §2's representative slice — a content pass, not a design decision.
- **Ban duration conventions, appeal process, and escalation policy** — server policy, not a mechanic.
- **A Moderator role** between User and Administrator (§2).
- **Logging every permission check**, pass or fail — only actions are logged (§1).
- **Vote-kick or other player-driven moderation** — a different, player-facing mechanic, not this doc's territory.
- **Real player authentication** — designed in `player-accounts.md`; this doc inherits the same black-box treatment for it that `persistence-save.md` §5 already established.
- **Server-level operations** (restart, config-file edits, hosting/deployment) — outside gameplay design scope entirely.
