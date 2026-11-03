# RE:SS3D Fork — Documentation Skill

This fork maintains its own documentation layer, independent of upstream's GitBook.
Read this before writing or editing anything in `Documents/`.

## Live-reference principle

Active documentation describes current contracts, code entry points, operating procedures and
unfinished work. Update the relevant section in place; do not append dated session reports,
phase-by-phase implementation diaries, solved investigations, test-run counts or artifact timelines.
Git and generated validation reports preserve that history.

Keep a solved issue only when it teaches a constraint a future change could violate. Express it
as a short symptom → cause → required practice in the owning map's Pitfalls, without the session
story. Tests sections name coverage, commands and outstanding checks; do not treat an old passing
run as evidence for the current checkout. Keep accepted design deviations explicit.

Completed efforts leave the active index after useful contracts and follow-ups are harvested.
Avoid creating a new history document just to preserve prose already recoverable from git.
`Documents/FORK_STATUS.md` remains the explicit upstream-divergence log, not a session changelog.

## Structure

| Path | Answers | Who updates |
|------|---------|-------------|
| `Documents/design/` | WHAT and WHY (gameplay specs) | **Owner only** — agents read, never write |
| `Documents/plans/` | HOW for in-flight work (temporary plans) | Agents — mark todos done on ship |
| `Documents/architecture/YYYY-MM_*.md` | HOW and IN WHAT ORDER for one effort | Agents — set header `Status` when shipping |
| `Documents/architecture/INDEX.md` | Navigation hub for agents; project-wide coverage table (what's designed, what's architected, what's mapped) | Agents via `update-system-docs` |
| `Documents/architecture/systems/` | WHERE in code (one map per domain) | Agents via `update-system-docs` |
| `Documents/milestones/` | What to focus on next; dependency trees toward a playable goal | Agents + owner — update slice status when child work ships; refresh hub current focus |
| `ROADMAP.md` + README roadmap block | Where the game is at, for players and visitors | Generated from `Documents/milestones/` by `python3 Tools/generate_roadmap.py`; never edit by hand |
| `Documents/archive/` | WHY something was built that way: shipped efforts, completed plans | Agents move docs here on ship (see [Archive](#archive-documentsarchive)); never maintained afterwards |
| `Documents/FORK_STATUS.md` | Upstream divergence log | Owner + agents — keep it a log of divergences, not a changelog |

This table is canonical. `AGENTS.md` inlines only the hard prohibitions drawn from it; it does not restate the table, so the two cannot drift.

### Design (`Documents/design/`)

Gameplay design specs. Stable. One file per system (e.g. `health.md`, `combat.md`).

**Agents must not create or edit design docs.** Read for context; link from system maps. If implementation diverges from design, note the divergence in a system map, plan, or architecture effort doc — do not "fix" the design doc to match code.

**Design docs are pure spec — WHAT and WHY only, never HOW.** Two things deliberately do not live here:

- **No prototyping prompts.** A design doc never ends with Claude Design or Cursor seed prompts. Those go stale the moment they're written (they reference a codebase snapshot that keeps moving) and they're implementation instruction, which doesn't belong in the one folder agents can't touch. When a design doc is actually commissioned for a build, generate the prompt fresh from the design doc plus the current system map — that's a `Documents/plans/` artifact, not a `Documents/design/` one.
- **No implementation-status field.** `Status: draft | active | deferred | superseded` answers "is this still the intended design," never "has this been built." `deferred` means the design still stands but no milestone up to the next planned gate needs it, so agents shouldn't plan work against it or treat its cross-references as near-term commitments. Build status lives in the architecture effort and system map for that domain, surfaced project-wide in `Documents/architecture/INDEX.md`'s coverage table (below) — not duplicated into the design doc.

**Design docs cite other design docs only.** Never link a design doc to an architecture effort, a plan, or a system map — that direction of reference only goes the other way. This keeps design docs stable regardless of how implementation churns.

### Plans (`Documents/plans/`)

Temporary implementation plans — typically Cursor plan files (`*.plan.md`) with YAML frontmatter and todos.

- Update todos (`completed` / `cancelled`) when work ships.
- Add an **Implementation notes** section at the bottom if the shipped result diverged.
- When every todo is `completed` or `cancelled`, archive the plan (see [Archive](#archive-documentsarchive)).

### Architecture efforts (`Documents/architecture/YYYY-MM_*.md`)

Implementation plans for a specific effort (can span multiple systems). Named `YYYY-MM_short-description.md`.

**One HOW doc per piece of work.** Use an effort for multi-system work and a plan for a smaller piece, never both. If a Cursor plan grows into an effort, fold its todos into the effort and archive the plan.

**Status is one word** (`planned | in-progress | shipped | abandoned`). Phase-level progress goes in a `**Progress:**` line directly under the header block, and nowhere else: INDEX, maps and milestones carry the one-word status and link here.

### Archive (`Documents/archive/`)

Shipped efforts and completed plans move to `Documents/archive/architecture/` or `Documents/archive/plans/` with `git mv`, rewriting relative links in both directions. The root `.ignore` hides the folder from default searches, so active docs stay the only ones search finds. Archived docs are never edited again, and their links may go stale.

**Harvest before archiving.** Move the doc's still-live content to where it belongs:

- known issues → the owning map's **Pitfalls**, or [TECH_DEBT.md](architecture/TECH_DEBT.md)
- deferred phases or follow-ups → the milestone slice, or a new effort or plan
- deviations from design → the owning system map

**Standing references.** Keep policies and current operational contracts active. A citation from
code, CI, tests, skills or governance must keep resolving, but does not make an implementation
history a permanent reference. First move its live guidance to a system map, child map or runbook,
retarget those citations, then archive the completed effort. The INDEX lists retained policies and
runbooks as `shipped (reference)` or `shipped (standing policy)`; trim their historical sections too.

### Milestones (`Documents/milestones/`)

Playable product gates and focus sequencing — not a substitute for design, architecture efforts, or plans. A milestone answers **what to work on next** and shows the **dependency tree** of features that unlock a playable goal (e.g. Nuke Ops → ranged combat → armor/health; nuke device → blast → station structural damage). Domain-level “what's designed / built” stays in the INDEX coverage table; structural risk stays in [TECH_DEBT.md](architecture/TECH_DEBT.md).

Hub: [milestones/INDEX.md](milestones/INDEX.md) (current focus + list of gates). One file per playable gate (e.g. `mvp1-nuke-ops.md`).

**Rules:**

- A milestone doc **cites** design §§, architecture efforts, and plans — it does **not** restate gameplay rules or full implementation HOW.
- Milestone docs may link **down** to design / architecture / plans (same direction as architecture → design). Design docs still never link up to milestones.
- Status on a milestone is about the **playable gate**, not “is the design active” or “did one PR ship.”
- Do not invent a parallel `Documents/plans/*.plan.md` as a second source of truth for the same gate — day-to-day HOW stays in plans/efforts; the milestone tracks focus and tree status.
- A gate is `planned` until one of its slices ships, then `in-progress` until the gate itself ships.
- **Slice status** starts with one word: `shipped | partial | pending | blocked`, optionally followed by ` — <detail>`. Versioning and the roadmap generator both read that first word.
- **Player view** (last Slices column) says what the slice means for a player, in plain language with no internal names: the outcome, then what already works in parentheses when `partial`. A closing slice that only checks the others together starts with `Finish line:` and is not counted as progress. Run `python3 Tools/generate_roadmap.py` after changing any slice status, Player view, or the `Goal`/`Roadmap` header; CI (`docs-check.yml`) fails when ROADMAP.md is stale.
- Every gate names the [vision](design/vision.md) pillars it advances, or, for an enabler gate (servers, tooling, testing), the gameplay gate it serves. An enabler slice that serves no gameplay gate goes to **Explicitly deferred**.
- **Playable definition** opens with a **Done when** acceptance scenario someone could run and fail; slices that aren't closing slices say what done means in their Name or Status.
- Bugs live in GitHub Issues, not milestone docs. A slice may cite an issue that blocks it in its Links column.

**Header template:**

```
> Goal: <one-line playable definition>
> Status: planned | in-progress | shipped | abandoned
> Depends on: <other milestone paths, if any>
> Current focus: <slice id or “blocked on X” — also mirrored on milestones INDEX>
> Roadmap: <Player-facing name> — <one-line summary of what players will be able to do>
> Pillars: <pillar numbers from design/vision.md §2 it advances> | enabler for <gameplay gate it serves>
```

**Body sections (fixed order):** **Playable definition**, **Dependency tree** (mermaid and/or nested bullets; leaf nodes link to design§ / effort / plan — this is the primary artifact), **Slices** (status table for the same nodes; columns `Id | Name | Status | Links | Player view`, Status third), **Explicitly deferred**, **Maintenance**.

### System maps (`Documents/architecture/systems/`)

Navigation docs: entry points, key files, dependencies. One file per domain (kebab-case, e.g. `tile.md`). Status reflects code navigation coverage, not upstream divergence (that stays in `FORK_STATUS.md`).

**Status values:** `shipped | partial | stub | condemned`, one word with no parenthetical. What is and isn't built goes in **Overview**.

**Child maps.** When one part of a domain needs its own page (an anatomy contract, an extension-hook list, or a section that pushes the parent past ~80 lines), split it into `systems/<domain>-<part>.md`. Its header is `> Parent map: [<domain>.md](<domain>.md)`, then `Status` and `Verified`. The parent links to the child from **Start here**, and the child follows the same section order.

- `shipped` / `partial` / `stub` — live code navigation coverage.
- `condemned` — do not extend; scheduled for replace-and-purge per [2026-07_agent-first-composition.md](architecture/2026-07_agent-first-composition.md). A map may stay `partial` or `shipped` overall while carrying a **Condemned UI** and/or **Prefab composition debt** subsection when domain logic still lives but presentation or prefab wiring must not grow.

**Verified stamp.** Every map header carries `> Verified: <short-sha> — <YYYY-MM-DD>`, the commit the map was last checked against code. `update-system-docs` bumps it to `HEAD` on each map it actually touches; untouched maps keep their old stamp, so a stamp far behind `HEAD` is the staleness signal — the map may still be right, but nobody has confirmed it, and a confidently wrong map is worse than none. Never bump the stamp on a map you didn't re-verify.

**Pitfalls.** When you hit a *silent* failure — code that compiles and runs but misbehaves with no error or log (UI Toolkit layout quirks, FishNet serialization limits, prefab wiring that no-ops, order-dependent init) — record it in the owning map's **Pitfalls** section as symptom → cause → fix. This is the single highest-value content a map carries: it's the wiki-only, hours-to-rediscover knowledge that AI agents are worst at recovering on their own because nothing throws. Keep entries to one or two lines; the fix or the constraint is the payload, not the story.

**Redesign Phase 0:** architecture efforts that replace a condemned surface must include a purge checklist for legacy UI files/prefabs and obsolete MonoBehaviours on shared roots (e.g. `Human.prefab`). Prefer tool-mediated prefab mutation (`PrefabUtility` / Editor menus) over raw YAML edits. See agent-first composition policy.

### Coverage table (`Documents/architecture/INDEX.md`)

Besides navigation, INDEX.md carries one table, one row per domain, three columns:

| Domain | Design | Architecture | System map |
|---|---|---|---|
| id-access | `design/id-access.md` — active | none yet | `systems/id-access.md` — partial |
| atmospherics | `design/atmospherics.md` — active | `2026-07_atmos-ecs-foundation.md` — shipped | `systems/atmospherics.md` — partial |

This is the single place that answers "what's left" at the domain level — a design doc with no architecture-effort entry means it's designed but unbuilt; a domain with no design entry at all means it hasn't been designed yet. Individual design docs keep their own `§Out of scope for this pass` for feature-level gaps within an already-designed system; this table is for gaps between systems, not within one. Update this table as part of `update-system-docs`, same trigger as everything else in this file.

## Required header blocks

Every **design** doc starts with:

```
> Status: draft | active | deferred | superseded
> Supersedes: <path, if replacing an old doc/section>
```

Every **architecture effort** doc starts with:

```
> Implements: <path/to/design-doc.md#section>, <...>
> Touches systems: <list>
> Status: planned | in-progress | shipped | abandoned
```

Every **system map** starts with:

```
> Code paths: <primary folder(s)>
> Entry points: <SubSystem classes, key services>
> Status: shipped | partial | stub | condemned
> Verified: <short-sha> — <YYYY-MM-DD>
```

Every **milestone** doc starts with the header template under [Milestones](#milestones-documentsmilestones) above.

System map body sections (fixed order): **Overview**, **Start here**, **Extension points**, **Pitfalls** (optional — include only when there are real silent-failure landmines), **Tests** (optional — which suites and loops cover this domain), **Depends on / Used by**, **Related docs**. Other headings fold into these: unit models and player flows into Overview, conventions into Extension points, architecture smells into [TECH_DEBT.md](architecture/TECH_DEBT.md), future work into the effort doc or milestone.

Design doc body sections (fixed order): unnumbered opening — what this doc formalizes and why it exists — then **Design philosophy**, doc-specific numbered sections, **Worked examples**, **Integration notes**, **Out of scope for this pass**. Nothing follows "Out of scope." No prototyping section, no status field beyond the one above.

Milestone body sections: fixed order under [Milestones](#milestones-documentsmilestones) above.

Keep each system map under ~80 lines; **Pitfalls** does not count against this — it's the one section worth letting a map run long for. Do not duplicate gameplay rules from design docs — link and cite by path + section.

## Linking convention

Always cite by path + section (`Documents/design/combat.md §3`), never by paraphrasing
another doc's content into a new one. If a fact needs restating, restate it briefly and
link back to the source of truth — never fork the explanation into two places that can
drift apart. This rule binds these governance docs too: `AGENTS.md` and the
`update-system-docs` skill point at this file for layers and templates rather than copying them.

## Starting a new domain

A domain sitting at "none yet / none yet" in INDEX.md's coverage table goes from design-only to navigable in a fixed order — the cold-start path, distinct from the steady-state `update-system-docs` loop:

1. **Owner** authors `Documents/design/<domain>.md` (agents never create design docs).
2. When the build is commissioned, create the first architecture effort `Documents/architecture/YYYY-MM_<domain>-<slice>.md`. Header per template above; `Implements:` the specific design-doc sections; read those sections in full first (see below).
3. Create the first system map `Documents/architecture/systems/<domain>.md` at `Status: stub`, `Verified` stamped with the current `HEAD`. It grows from stub → partial → shipped as slices land.
4. Fill in the domain's row in INDEX.md's coverage table — the **Architecture** and **System map** columns only. Never touch the **Design** column; that's owner-authored.
5. From here on it's the normal loop: implement, then run `update-system-docs`.

## Before writing an architecture effort doc

1. Read every design doc listed in the effort's `Implements:` line, in full.
2. Cite specific sections when a plan decision follows from a design decision.
3. If the effort requires deviating from a design doc, say so explicitly in the
   architecture doc ("deviates from health.md §4 because...") rather than silently
   building something different from spec.

## Before writing or revising a design doc (owner)

1. Check `Documents/design/` for existing docs on adjacent systems and cross-link
   rather than re-explain (this codebase's docs already do this well — follow that
   pattern, e.g. armor.md citing combat.md §6).
2. Mark old/superseded docs explicitly rather than deleting silently — set
   `Status: superseded`, point to the replacement.

## After implementing a feature (agents)

Run the `update-system-docs` skill (`.cursor/skills/update-system-docs/SKILL.md`) to sync INDEX, affected system maps (status, `Verified` stamp, new **Pitfalls**), linked plans, architecture effort status, archiving of what shipped, and any [milestones](milestones/) slice status / hub current focus the work advanced. Never edit `Documents/design/` unless the owner explicitly asks.
