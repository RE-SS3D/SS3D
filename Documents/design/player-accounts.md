# Player Accounts — design document

> Status: active

Resolves the black box five other docs have all been reading from without anyone supplying it.
`persistence-save.md` §5 treats "a stable per-player key exists" as an assumed dependency, explicitly
noting "until that stable key exists, player meta is only as trustworthy as the connection claiming
it." `admin-tools.md` §7, §10, and §11 inherit the identical caveat for ban reliability. `id-access.md`
§12 and `lobby.md` §11 both defer "real authentication behind a stable
per-player identity" the same way. This doc is what supplies that key.

## 1. Design philosophy

**SS13's own identity model — a real, external, pre-verified account — is already good design.**
BYOND's ckey isn't an artifact to fix; it's a genuinely reasonable idea: identity comes from an
account system that already exists and already handles its own security, not something the game
itself has to build and defend. What actually needs fixing on this fork specifically is narrower and
less flattering: today's stand-in is client-trusted — a connecting player's claimed identity is
simply believed, not verified against anything. The idea survives the native-vs-leftover test; the
implementation doesn't yet.

**Delegate identity, don't rebuild it.** The same "no parallel model" instinct this project runs for
every other cross-cutting system — reusing the material silo instead of a second economy, reusing
atmospherics instead of a second gas model — applies here to security instead of gameplay. This doc
doesn't design a registration screen, a password reset flow, or an email-verification pipeline. It
delegates identity to an existing, already-verified external account (a platform identity — Steam or
equivalent), the same way `crafting.md` §3 delegates material stock to an existing silo it doesn't
redesign.

**Accounts are infrastructure a server can require, not a mandate this doc forces everywhere.** A
server that doesn't require verified identity simply keeps today's client-trusted status quo — an
explicit, visible server setting, not a silent gap nobody chose. This doc adds the capability; it
doesn't remove the option to run without it.

**Honest about what this actually fixes.** Verified identity raises the real cost of ban evasion; it
doesn't eliminate it. This doc states that plainly rather than overclaiming a solved problem, the
identical "detectable, not prevented" honesty `admin-tools.md` §7 already committed to before this
doc existed to back it up.

## 2. Identity source — delegate, don't rebuild

**An external, already-verified platform identity supplies the account.** No self-built
registration, login, or credential-recovery system exists on this project's side — deliberately, to
avoid owning a large, security-sensitive subsystem this fork has no particular advantage building or
maintaining. A platform identity (Steam or an equivalent distribution platform) already solved this
problem at a scale and security bar this project doesn't need to re-attempt.

**The provider is a swappable dependency, not a hard-coded name.** The same "named list, growable as
content demands" treatment `id-access.md` §4 gives its own cross-cutting access levels applies here:
if this fork ever ships somewhere without the assumed platform, a different verified-identity source
slots into the same verification step (§3) without changing anything downstream that reads the
resulting key.

## 3. Connecting — the verification handshake

**At connect time, the client presents a real credential issued by the identity provider; the server
validates it before trusting the connection's claimed identity.** Only once validated does a stable
per-player key exist for that session — the exact key `persistence-save.md` §5's player-meta layer,
`lobby.md`'s playtime gates, and `admin-tools.md`'s bans all already assumed would eventually exist.

**A server can be configured to require this, or not.** Requiring it is the real fix; not requiring
it is today's status quo, kept as an explicit, visible setting rather than something this doc
silently forces on every server, including ones (a local dev/test server, say) that have no real
need for it.

## 4. What this doc owns, and what it doesn't

**Owns:** the verification step, and the stable identity key itself.

**Doesn't own:** playtime, job unlocks, saved lobby preferences, or moderation notes/bans — all of
that is `persistence-save.md` §5's player-meta layer, keyed to the identity this doc supplies. One
shared key, many consumers, exactly the "one shared mechanism, not parallel systems" shape this
project runs everywhere else — this doc doesn't duplicate storage persistence-save.md already owns.

## 5. Ban evasion — honest about the actual fix

**Verified accounts raise the bar; they don't eliminate evasion.** `admin-tools.md` §7 already
flagged this precisely: "a ban is only as good as the connection claiming that identity... real
authentication is the actual fix." This doc is that fix, and it's a real one — a ban now keys to a
verified external account instead of an unverified claim. What it doesn't do is stop someone willing
to create a new verified account from evading anyway; that costs them something real (whatever the
platform identity itself costs to obtain), which is genuine friction, not a closed door. Stating this
plainly matches the same "detectable, not prevented" honesty this project runs everywhere else,
rather than quietly implying a solved problem.

## 6. HUD & touchpoints

No new permanent chrome — verification happens before a player ever sees the lobby.

- A failed or rejected verification attempt is a real, legible message at the point of connecting,
  the same discipline `networking.md` §5 already uses for a server-at-capacity rejection.
- Nothing about the lobby, job list, or any in-round system changes visibly — this doc only supplies
  the key other systems already knew they'd eventually read.

## 7. Worked examples

**A — Verified connect, playtime gate resolves correctly:**

| Step | What happens | State |
|---|---|---|
| 1 | A returning player connects to a server that requires verified identity | Client presents its platform credential |
| 2 | Server validates it | A stable per-player key now exists for this session |
| 3 | Lobby reads accumulated playtime for that key | Chief Engineer's playtime gate resolves correctly, per `lobby.md` §2 |

**B — A server that doesn't require verification:**

| Step | What happens | State |
|---|---|---|
| 1 | A local test server is configured without required verification | Explicit setting, not a silent gap |
| 2 | A player connects with any claimed identity | Accepted, exactly today's client-trusted behavior |
| 3 | Playtime/ban systems downstream still function | Just with the same reliability caveat they already had before this doc existed |

**C — A banned player evades, honestly:**

| Step | What happens | State |
|---|---|---|
| 1 | A player is banned, keyed to their verified account | Real progress over an unverified ckey ban |
| 2 | They create a new verified platform account and reconnect | Ban doesn't catch the new identity |
| 3 | This is a real, acknowledged limitation | Not a workaround invented here, per §5 — the same honesty `admin-tools.md` §7 already committed to |

## 8. Integration notes

| Player-accounts element | Touches existing / needed system |
|---|---|
| Stable identity key | `persistence-save.md` §5 (player meta, keyed to this) |
| Ban reliability | `admin-tools.md` §7 (the caveat this doc resolves) |
| Playtime/job-unlock gating | `lobby.md` §2 |
| Cross-round job history | `id-access.md` §12 |
| Server-side config persistence | `round-config.md` §2 (this doc doesn't touch config persistence itself) |
| Rejection-message precedent | `networking.md` §5, §6 |

## 9. Out of scope for this pass

- **Exact identity provider chosen** (Steam or another platform) — a distribution/business decision,
  not fixed here.
- **A self-built registration, login, or password-recovery system** — deliberately not designed,
  per §2.
- **Custom per-account display names distinct from the platform identity** — a plausible small
  addition, not designed here.
- **Cross-server or cross-fork shared account systems** — out of scope.
- **Payment, purchases, or any monetization tied to accounts** — not this project's concern.
- **Deeper ban-evasion prevention** (device fingerprinting, hardware IDs, or similar) — a harder
  problem this pass doesn't attempt, per §5's stated honesty about what's actually solved.
