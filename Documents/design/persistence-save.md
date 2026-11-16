# Persistence & Save — design document

> Status: active

Resolves a gap five other docs have all been quietly assuming shut: `lobby.md` §11 flags "the persistence/accounts system underlying playtime-gated jobs" as cross-cutting infra it assumes exists; `round-config.md` §2 keeps its server settings across rounds; `cargo.md` §12 defers "persistence of the budget across rounds" to it; `id-access.md` §12 defers cross-round job history the same way; `inventory-storage.md` §14 defers container-contents persistence to it. None of those docs invented a parallel save mechanism — they all pointed at this one and moved on. This doc is what they were pointing at.

It also has one hard boundary to respect rather than redesign: `death-cloning-respawn.md` §4 already ruled that genetics/crew records are "a real, breakable database... not an always-available save file sitting outside the simulation" — a deliberate stake, not a gap. This doc's job is everything *around* that rule, never a shortcut through it.

## 1. Design philosophy

**Four genuinely different lifecycles, not one save blob.** A station's layout barely changes and outlives every round drawn on it. Server settings persist until an admin changes them. A player's playtime and preferences outlive any single connection. A round's live world state resets completely at the next round start. Treating these as one generic "save the game" mechanism would force every consumer to filter out what doesn't apply to it — the same problem atmospherics avoided by keeping gas diffusion and pipe bulk-flow as "two real techniques, not one stretched to cover both" (`atmospherics.md` §1). This doc keeps the same discipline: four layers, each with its own lifecycle, sharing a mechanism only where the mechanism is actually the same (write-to-disk, versioned, server-only).

**Diegetic identity is never shortcut through disk.** A crew record's DNA, a genetics console's data — these are live, physical, network-synced objects for the duration of a round (`death-cloning-respawn.md` §4, `id-access.md` §2), destructible like anything else physical. This doc doesn't give them a parallel, always-recoverable save file that would quietly undercut that stake. Where round snapshots (§6) incidentally capture live entity state for crash recovery, a crew record is just more of that same transient state — never its own standalone, separately-queryable archive.

**Real, inspectable data instead of a black box.** Same instinct round config's admin history log and the AI's audit trail already run (`round-config.md` §5, `ai-cyborgs.md` §4) — this doc is what actually produces the data those screens read. A save happened, a load happened, a round was recovered from a crash: each is a real, timestamped, admin-visible fact, not a silent background process nobody can point to.

**A safety net stays a safety net — it doesn't replace admin judgment.** Round snapshots (§6) exist so a crash costs minutes, not a whole round. But a server recovering from one is a visible, logged event an admin can see and override, not a silent substitution of "the round that actually happened" for "whatever the last snapshot remembered."

## 2. The four layers

| Layer | What it holds | Lifecycle | Tied to |
|---|---|---|---|
| **Station templates** | Map layout, Area metadata, placed objects and items with their authored setup | Survives indefinitely until re-authored | A map, not a round or a player |
| **Server meta** | Admin permissions, round config's pools, round history log | Survives until an admin changes it | The server itself |
| **Player meta** | Playtime per role, job unlocks, saved lobby preferences, moderation notes | Survives across every round a player returns to | A player identity |
| **Round snapshots** | Live world state, periodically captured | Superseded every round; retained briefly as a crash safety net | One in-progress round |

Each is real disk state; none of them is the diegetic crew record or ID card data covered in §7.

## 3. Station templates

**What's captured:** everything the Map Editor's save writes (`creative-mode.md` §9) — tiles, Areas, placed objects and items, pipes and cables, spawn points and starting air — plus each object's **authored setup**, exactly as authored and nothing round-specific. This layer's job stops at what a map looks like before anyone's touched it, applied here to the pre-round baseline specifically. Character positions and anything a round has done to the station don't belong here.

**Authored setup is what a builder chose, never what the simulation accumulated.** A builder sets an object up through its own interface, the way a player would (`creative-mode.md` §3), and the template keeps the result:

- **Device settings** — a valve left open, a pump's target pressure, a vent or scrubber's mode, an air alarm's preset.
- **Locks and doors** — a locker left locked or open.
- **Container contents** — what's inside a locker, crate or backpack, including containers inside containers (`inventory-storage.md` §7), exactly as placed. Randomised contents are out of scope (§11).

Three kinds of state stay out even though they live on the same objects:

- **Stored resources** — charge, fuel, the gas in a canister or tank. A device starts in its default state so a newly built section stays honest about what it needs to come online (`creative-mode.md` §3).
- **Damage and wear** — a playtest's broken window is not part of the map.
- **Live simulation** — gas in pipes, power flowing, running timers. Room air at round start is authored separately, as starting air (`creative-mode.md` §9).

Only what differs from an object's normal default is recorded, so an object nobody configured follows the game's default for it, including when that default later changes. A template the game can't fully restore says so when it loads, the same as removed objects do (`creative-mode.md` §9).

**Authored in the Map Editor, chosen by round config.** A save from the Map Editor (`creative-mode.md` §9) becomes a new template; round config's map setting (`round-config.md` §2) is what decides which template a given round actually loads. This doc supplies the load, round config supplies the choice — the same "lobby doesn't invent it, it reads it" relationship this project draws everywhere else.

**Re-saving doesn't destroy history.** A builder can snapshot iterative versions under different names at any point (`creative-mode.md` §9); nothing about this layer forces overwriting a template that's already in rotation.

## 4. Server meta

**Admin permissions** — who can do what in whatever admin tooling exists — persist here, migrating forward from whatever the current permission store already is rather than inventing a second one.

**Round config's server settings** — gamemode, map, round length, player cap, latejoin (`round-config.md` §2) — are server meta, not a template and not round-tied: an admin edits them once, and they apply to every future draw until edited again.

**Round history log.** Round config's own admin screen already specifies what this looks like: the last N rounds' mode, map, and whether the round fell back to Extended (`round-config.md` §5). That screen is the reader; this doc is the writer — one entry appended the moment a round actually ends, never summarized away or overwritten.

## 5. Player meta

**Scope:** playtime accumulated per role (feeding `lobby.md` §2's playtime-gated job display), job unlock state, a returning player's saved characters, each with its job preferences and antagonist opt-ins (`lobby.md` §3, §6), so nothing starts blank after a restart, and moderation notes/bans.

**Keyed to a real player identity — which this doc doesn't invent.** Real authentication is designed in `player-accounts.md`; this doc treats "a stable per-player key exists" as a black box the same way crafting treats the material silo (`crafting.md` §3) or Area treats atmospherics (`area.md` §4) — an assumed dependency, not redesigned here. On a server that doesn't require the verification `player-accounts.md` §3 supplies, player meta is only as trustworthy as the connection claiming it — a known, stated limitation, not a silently-assumed one.

**Moderation notes/bans are stored here, not designed here.** This layer is the substrate a note or a ban gets written to; the actual moderation workflow — who can issue one, what it restricts, any appeal process — is `admin-tools.md` §7's job, the same separation `round-config.md` §10 already drew around its own admin toolkit.

**Distinct from the crew record.** `id-access.md` §2's crew record is an in-round, per-character, network-synced identity object that exists only for the round it's created in. Player meta is the thing that outlives any single round and keys off a real account, not a round-scoped character.

## 6. Round snapshots — an automatic safety net

**Purpose: a crash costs minutes, not a round.** The server periodically captures live round state in the background — no admin has to remember to trigger it, the same way an autosave in any other piece of software doesn't wait for a human to ask. If the server goes down, the next boot recovers from the most recent snapshot rather than losing everything back to round start.

**What's captured, in principle:** structural changes since the round's base template, item/container contents, electricity and atmospherics state, entity and character state, active objectives and antag assignments — everything that makes an in-progress round actually in-progress. This doc doesn't invent new serialization for any one of those; it defines that the capture happens, on what cadence, and what it's captured *against* (see below). The exact shape of what each of those systems' own runtime state looks like when captured is that system's own future design work, the same "read path, not authored here" relationship this project uses everywhere a doc leans on another system's existing state.

**A snapshot references its base template and stores deltas where practical** — changed tiles, moved items, altered machine state — with a full snapshot as the fallback when a delta isn't meaningful yet (e.g., the very first snapshot after round start). This mirrors station templates being the "before" state and a snapshot being "what's changed since."

**Recovery is automatic, but never silent.** On boot, if an unfinished round's snapshot is newer than the last completed entry in the round history log (§4), the server resumes from it by default — matching the reason automatic snapshots exist at all: nobody may be around to manually approve a resume in the middle of an actual crash. But the recovery itself writes its own real, timestamped, admin-visible line to the round history log ("Recovered from snapshot, [time]"), never presented as if the round simply continued uninterrupted. **An admin can discard a recovered snapshot and force a fresh round instead** — the same always-available manual override this project gives every other automated process (round config's admin override of a draw, round end's admin-ends-it trigger).

## 7. What never touches disk

**Genetics/crew records and ID card contents** stay exactly what `death-cloning-respawn.md` §4 and `id-access.md` §2 already established: live, physical, network-synced objects for the duration of a round. A round snapshot (§6) may incidentally capture their live state as part of ordinary entity state — the same as it would capture any other transient sim data — but there is no separate, standalone genetics archive a player or admin could query outside the round it belongs to. That would be exactly the "parallel model" this project has refused everywhere else, just for identity instead of permissions or power.

**Derived UI state** — a diegetic machine screen's current display, for instance — is never persisted at all. It's rebuilt live from underlying simulation state on every read, consistent with how this project's diegetic-screen framework already treats those views: read models, not sources of truth.

## 8. HUD & admin touchpoints

No new permanent chrome, same discipline as everywhere else in this project.

- Round config's own admin config screen (`round-config.md` §5) is where the round history log actually renders — this doc only supplies the data it reads.
- A crash-recovered round gets a plain, real line in that same log, not a special hidden flag only visible to someone who goes looking.
- Player-facing footprint is zero: nobody sees "your data was saved." Playtime and preferences are simply *there* the next time they matter — a playtime-gated job shows correctly, a returning player's characters are there with their rankings — the same "the system already knows, no popup required" discipline examine and every other on-demand surface in this project already run.

## 9. Worked examples

**A — Round start draws a template:**

| Step | What happens | Persistence state |
|---|---|---|
| 1 | The admin's map setting names "Outpost Station" as the countdown ends | Round config supplies the choice (`round-config.md` §2) |
| 2 | This doc loads that station template | Tiles, Areas, objects and their authored setup restored exactly as last authored |
| 3 | Round begins | Live simulation takes over; nothing further is read from the template until next round |

**B — A returning player's playtime gates a job:**

| Step | What happens | Persistence state |
|---|---|---|
| 1 | A player who's logged 12 hours as Engineer connects | Player meta reads their accumulated playtime for that role |
| 2 | Lobby's job list resolves | "Chief Engineer" (10-hour gate) now shows unlocked instead of grayed out, per `lobby.md` §2 |
| 3 | Same player's saved characters come back with their job rankings | No blank slate every session |

**C — Server crash, automatic recovery:**

| Step | What happens | Persistence state |
|---|---|---|
| 1 | A round is 20 minutes in; the server has been auto-snapshotting in the background | Most recent snapshot references the round's base template plus everything changed since |
| 2 | The server process crashes unexpectedly | No admin present to intervene |
| 3 | Server restarts | Detects an unfinished round newer than the last round-history entry; resumes from the most recent snapshot automatically |
| 4 | Round history log gets a new line | "Recovered from snapshot, [timestamp]" — visible, not silent |

**D — Admin declines a recovery:**

| Step | What happens | Persistence state |
|---|---|---|
| 1 | Server restarts after a crash; a recoverable snapshot exists | Same detection as example C |
| 2 | An admin decides the recovered state is stale or undesirable | Uses the always-available override to discard it |
| 3 | A fresh round starts instead | Round config draws again from scratch, same as any normal round start |

## 10. Integration notes

| Persistence element | Touches existing / needed system |
|---|---|
| Station template load | Round config's map setting (`round-config.md` §2) |
| Station template save | The Map Editor's save (`creative-mode.md` §9) |
| Round config settings, permissions | Server meta layer (this doc); admin settings screen reads it (`round-config.md` §2, §5) |
| Round history log | Written here, rendered by round config's admin screen (`round-config.md` §5) |
| Playtime / job unlocks | Feeds lobby's playtime-gated job display (`lobby.md` §2) |
| Saved characters and their preferences | Lobby's characters (`lobby.md` §3, §6) |
| Stable player identity key | `player-accounts.md` §3 |
| Moderation notes/bans | Storage only; workflow is `admin-tools.md` §7's job |
| Crew record / ID card exclusion | `death-cloning-respawn.md` §4, `id-access.md` §2 — never a parallel save |
| Round snapshot cadence, recovery | This doc, §6 |
| Round snapshot per-domain capture shape | Each system's own future design (electricity, atmospherics, inventory, entities) |

## 11. Out of scope for this pass

- **Real authentication/account identity itself** — designed in `player-accounts.md`; this doc treats it as a black box (§5)
- **Exact snapshot cadence, retention window, and disk-space/rotation policy** — a balancing pass, not a design decision
- **Per-domain round-snapshot capture shape** (electricity kWh state, atmospherics turf buffers, item/container contents, entity/mind data) — this doc guarantees the mechanism; each system's own doc is where its own runtime-state shape gets defined
- **Admin-facing save/restore/rewind UI and permissions beyond the always-available override in §6** — `admin-tools.md`'s permission-tier model (§2) covers who could invoke this; the exact UI isn't detailed here
- **Moderation policy** — what a ban actually restricts, appeal process, staff workflow — `admin-tools.md` §7's job; this doc only stores the note/flag
- **Cross-server or off-machine backup/sync** — this doc assumes local disk, same as everything it builds on
- **Randomised container contents** (SS13-style loot spawners) — a station template stores exact contents only (§3); varying them per round is a content design of its own
