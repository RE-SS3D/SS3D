---
name: update-system-docs
description: Updates architecture INDEX, system maps, and implementation plans after code changes. Use when finishing a feature, after merging implementation work, or when the user asks to sync navigation docs with the codebase.
disable-model-invocation: true
---

# Update System Docs

Sync navigation documentation after implementation work. Updates INDEX, system maps, plans, and architecture effort status.

**Never edit:** `Documents/design/*`, `Documents/FORK_STATUS.md` (unless the owner explicitly asks).

## Checklist

```
- [ ] Step 1: Identify scope from git diff
- [ ] Step 2: Read affected system maps (don't rewrite untouched systems)
- [ ] Step 3: Update affected maps (content, Status, Verified stamp, Pitfalls)
- [ ] Step 4: Update INDEX.md
- [ ] Step 5: Update linked plans
- [ ] Step 6: Update architecture effort docs
- [ ] Step 7: Sanity check links, paths, stamps
```

## Step 1: Identify scope

Run `git diff` (or review changed paths from the session). Map paths to systems via [Documents/architecture/INDEX.md](../../Documents/architecture/INDEX.md):

| Path prefix | System map |
|-------------|------------|
| `Assets/Scripts/SS3D/Interactions/` | interactions-framework |
| `Assets/Scripts/SS3D/Systems/Interactions/` | interactions-runtime |
| `Assets/Scripts/SS3D/Systems/Selection/`, `Rendering/URP/SelectionPick*` | selection, rendering |
| `Assets/Scripts/SS3D/Systems/Examine/` | examine |
| `Assets/Scripts/SS3D/Systems/Tile/` | tile |
| `Assets/Scripts/SS3D/UI/MachineInterface/` | machine-interface |
| `Assets/Scripts/SS3D/Systems/Rounds/`, `Systems/Lobby/` | rounds-lobby |

When unsure, read INDEX first.

## Step 2: Read affected maps

Open only the maps for systems touched by the change. Read [Documents/SKILL.md](../../Documents/SKILL.md) for the system map template and section order — it's canonical; this skill doesn't restate it.

## Step 3: Update each affected map

Update only what changed:

- **Start here** — add/remove key files when new important types land
- **Extension points** — document new patterns for future agents
- **Pitfalls** — record any silent failure you hit or fixed this session (symptom → cause → fix). Code that compiled and ran but misbehaved with no error belongs here; it's the highest-value thing you can leave the next agent.
- **Depends on / Used by** — update cross-system links
- **Related docs** — link new plans or effort docs
- **Status** — bump `stub` → `partial` → `shipped` when appropriate; use `condemned` (or a Condemned UI / Prefab composition debt subsection) per [Documents/SKILL.md](../../Documents/SKILL.md) and [2026-07_agent-first-composition.md](../../Documents/architecture/2026-07_agent-first-composition.md)
- **Overview** — revise if the system's role changed materially
- **Verified** — bump the header `> Verified: <short-sha> — <date>` to current `HEAD` **only on maps you actually re-checked against code this session.** Never bump a map you didn't touch — the gap between its stamp and `HEAD` is the staleness signal, and a false-fresh stamp destroys it.

Keep each map under ~80 lines (Pitfalls exempt). Do not duplicate design doc content — link instead.

When a redesign **ships** and purges legacy UI or mega-prefab components: remove condemned markers / note prefab debt reduction. Never “upgrade” condemned uGUI to `partial` by restyling. Never document “add component on Human.prefab” as an extension point — point at Editor setup tools or the owning redesign’s Phase 0 instead.

## Step 4: Update INDEX.md

In [Documents/architecture/INDEX.md](../../Documents/architecture/INDEX.md):

- Update the **Status** column for affected rows
- Add a new row if a new domain was introduced (see [Documents/SKILL.md](../../Documents/SKILL.md) "Starting a new domain")
- Add architecture effort or plan links if new docs were created
- Update the **Coverage table**: if this work shipped or created an architecture effort
  or system map for a domain, update that domain's row (never touch its Design column —
  that's owner-authored)

## Step 5: Update implementation plans

If work followed a plan in `Documents/plans/*.plan.md`:

1. Mark completed todos in YAML frontmatter (`status: completed` or `cancelled`)
2. Add an **Implementation notes** section at the bottom if shipped result diverged from the plan
3. Do not delete the plan file

## Step 6: Update architecture effort docs

If work completed a dated effort in `Documents/architecture/YYYY-MM_*.md`:

- Set header `> Status: shipped` (or `abandoned` with explanation)

## Step 7: Sanity check

- Every file listed under **Start here** exists in the repo
- INDEX links resolve to existing system map files
- Every `Verified` stamp you bumped points at a commit you actually checked the map against
- Any `Pitfalls` entry you added describes real, current behavior
- No edits were made to `Documents/design/` or `Documents/FORK_STATUS.md`

## System map template

Use the canonical header block and fixed section order in [Documents/SKILL.md](../../Documents/SKILL.md) ("Required header blocks"). New maps go in `Documents/architecture/systems/` with a kebab-case filename, `Status: stub`, and a `Verified` stamp set to current `HEAD`. Do not re-embed the template here — it drifts.

## Additional resources

- Agent navigation rules: [AGENTS.md](../../AGENTS.md)
- Full doc conventions: [Documents/SKILL.md](../../Documents/SKILL.md)
