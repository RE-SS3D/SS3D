# RE:SS3D Fork — Documentation Skill

This fork maintains its own documentation layer, independent of upstream's GitBook.
Read this before writing or editing anything in `Documents/`.

## Structure

| Path | Answers | Who updates |
|------|---------|-------------|
| `Documents/design/` | WHAT and WHY (gameplay specs) | **Owner only** — agents read, never write |
| `Documents/plans/` | HOW for in-flight work (temporary plans) | Agents — mark todos done on ship |
| `Documents/architecture/YYYY-MM_*.md` | HOW and IN WHAT ORDER for one effort | Agents — set header `Status` when shipping |
| `Documents/architecture/INDEX.md` | Navigation hub for agents | Agents via `update-system-docs` |
| `Documents/architecture/systems/` | WHERE in code (one map per domain) | Agents via `update-system-docs` |
| `Documents/FORK_STATUS.md` | Upstream divergence log | Owner periodically — agents do not touch |

### Design (`Documents/design/`)

Gameplay design specs. Stable. One file per system (e.g. `health.md`, `combat.md`).

**Agents must not create or edit design docs.** Read for context; link from system maps. If implementation diverges from design, note the divergence in a system map, plan, or architecture effort doc — do not "fix" the design doc to match code.

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
