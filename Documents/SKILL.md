# RE:SS3D Fork — Documentation Skill

This fork maintains its own documentation layer, independent of upstream's GitBook.
Read this before writing or editing anything in `Documents/`.

## Structure

| Path | Answers | Who updates |
|------|---------|-------------|
| `Documents/design/` | WHAT and WHY (gameplay specs) | **Owner only** — agents read, never write |
| `Documents/plans/` | HOW for in-flight work (temporary plans) | Agents — mark todos done on ship |
| `Documents/architecture/YYYY-MM_*.md` | HOW and IN WHAT ORDER for one effort | Agents — set header `Status` when shipping |
| `Documents/architecture/INDEX.md` | Navigation hub for agents; project-wide coverage table (what's designed, what's architected, what's mapped) | Agents via `update-system-docs` |
| `Documents/architecture/systems/` | WHERE in code (one map per domain) | Agents via `update-system-docs` |
| `Documents/FORK_STATUS.md` | Upstream divergence log | Owner periodically — agents do not touch |

### Design (`Documents/design/`)

Gameplay design specs. Stable. One file per system (e.g. `health.md`, `combat.md`).

**Agents must not create or edit design docs.** Read for context; link from system maps. If implementation diverges from design, note the divergence in a system map, plan, or architecture effort doc — do not "fix" the design doc to match code.

**Design docs are pure spec — WHAT and WHY only, never HOW.** Two things deliberately do not live here:

- **No prototyping prompts.** A design doc never ends with Claude Design or Cursor seed prompts. Those go stale the moment they're written (they reference a codebase snapshot that keeps moving) and they're implementation instruction, which doesn't belong in the one folder agents can't touch. When a design doc is actually commissioned for a build, generate the prompt fresh from the design doc plus the current system map — that's a `Documents/plans/` artifact, not a `Documents/design/` one.
- **No implementation-status field.** `Status: draft | active | superseded` answers "is this still the intended design," never "has this been built." Build status lives in the architecture effort and system map for that domain, surfaced project-wide in `Documents/architecture/INDEX.md`'s coverage table (below) — not duplicated into the design doc.

**Design docs cite other design docs only.** Never link a design doc to an architecture effort, a plan, or a system map — that direction of reference only goes the other way. This keeps design docs stable regardless of how implementation churns.

### Plans (`Documents/plans/`)

Temporary implementation plans — typically Cursor plan files (`*.plan.md`) with YAML frontmatter and todos.

- Update todos (`completed` / `cancelled`) when work ships.
- Add an **Implementation notes** section at the bottom if the shipped result diverged.
- Link finished plans from affected system maps.
- Do not delete plan files; keep them as historical record.

### Architecture efforts (`Documents/architecture/YYYY-MM_*.md`)

Implementation plans for a specific effort (can span multiple systems). Named `YYYY-MM_short-description.md`.

### System maps (`Documents/architecture/systems/`)

Navigation docs: entry points, key files, dependencies. One file per domain (kebab-case, e.g. `tile.md`). Status reflects code navigation coverage, not upstream divergence (that stays in `FORK_STATUS.md`).

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
> Status: draft | active | superseded
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
> Status: shipped | partial | stub
```

System map body sections (fixed order): **Overview**, **Start here**, **Extension points**, **Depends on / Used by**, **Related docs**.

Design doc body sections (fixed order): unnumbered opening — what this doc formalizes and why it exists — then **Design philosophy**, doc-specific numbered sections, **Worked examples**, **Integration notes**, **Out of scope for this pass**. Nothing follows "Out of scope." No prototyping section, no status field beyond the one above.

Keep each system map under ~80 lines. Do not duplicate gameplay rules from design docs — link and cite by path + section.

## Linking convention

Always cite by path + section (`Documents/design/combat.md §3`), never by paraphrasing
another doc's content into a new one. If a fact needs restating, restate it briefly and
link back to the source of truth — never fork the explanation into two places that can
drift apart.

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

Run the `update-system-docs` skill (`.cursor/skills/update-system-docs/SKILL.md`) to sync INDEX, affected system maps, linked plans, and architecture effort status. Never edit `Documents/design/` or `Documents/FORK_STATUS.md` unless the owner explicitly asks.
