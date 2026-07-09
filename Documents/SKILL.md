# RE:SS3D Fork — Documentation Skill

This fork maintains its own documentation layer, independent of upstream's GitBook.
Read this before writing or editing anything in Documents/design or Documents/architecture.

## Structure

- `Documents/design/` — gameplay design specs. Stable. Changes rarely, deliberately.
  One file per system (e.g. `health.md`, `combat.md`). This defines WHAT and WHY.
- `Documents/architecture/` — implementation plans. One file per effort, not per system
  (an effort can span multiple systems, e.g. a tilemap refactor touching health + area).
  Named `YYYY-MM_short-description.md`. This defines HOW and IN WHAT ORDER.
- `Documents/FORK_STATUS.md` — plain-language divergence log from upstream. Updated
  periodically, not per-commit.

## Required header block

Every design doc starts with:
```
> Status: draft | active | superseded
> Supersedes: <path, if replacing an old doc/section>
```

Every architecture doc starts with:
```
> Implements: <path/to/design-doc.md#section>, <...>
> Touches systems: <list>
> Status: planned | in-progress | shipped | abandoned
```

## Linking convention

Always cite by path + section (`Documents/design/combat.md §3`), never by paraphrasing
another doc's content into a new one. If a fact needs restating, restate it briefly and
link back to the source of truth — never fork the explanation into two places that can
drift apart.

## Before writing an architecture doc

1. Read every design doc listed in the effort's `Implements:` line, in full.
2. Cite specific sections when a plan decision follows from a design decision.
3. If the effort requires deviating from a design doc, say so explicitly in the
   architecture doc ("deviates from health.md §4 because...") rather than silently
   building something different from spec.

## Before writing or revising a design doc

1. Check `Documents/design/` for existing docs on adjacent systems and cross-link
   rather than re-explain (this codebase's docs already do this well — follow that
   pattern, e.g. armor.md citing combat.md §6).
2. Mark old/superseded docs explicitly rather than deleting silently — set
   `Status: superseded`, point to the replacement.

## Status checks (ephemeral, not a file)

"What's actually built" is answered on demand by comparing a design/architecture doc
against current code — not maintained as a standing document. Ask directly:
"Compare Documents/design/health.md and its architecture docs against the current
codebase — what's shipped, what's planned, what's diverged."
