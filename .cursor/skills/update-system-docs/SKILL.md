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
- [ ] Step 3: Update affected maps
- [ ] Step 4: Update INDEX.md
- [ ] Step 5: Update linked plans
- [ ] Step 6: Update architecture effort docs
- [ ] Step 7: Sanity check links and paths
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

Open only the maps for systems touched by the change. Read [Documents/SKILL.md](../../Documents/SKILL.md) for the system map template and section order.

## Step 3: Update each affected map

Update only what changed:

- **Start here** — add/remove key files when new important types land
- **Extension points** — document new patterns for future agents
- **Depends on / Used by** — update cross-system links
- **Related docs** — link new plans or effort docs
- **Status** — bump `stub` → `partial` → `shipped` when appropriate
- **Overview** — revise if the system's role changed materially

Keep each map under ~80 lines. Do not duplicate design doc content — link instead.

## Step 4: Update INDEX.md

In [Documents/architecture/INDEX.md](../../Documents/architecture/INDEX.md):

- Update the **Status** column for affected rows
- Add a new row if a new domain was introduced
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
- No edits were made to `Documents/design/` or `Documents/FORK_STATUS.md`

## System map template

New maps go in `Documents/architecture/systems/` (kebab-case filename):

```markdown
> Code paths: <folder(s)>
> Entry points: <SubSystem classes, key services>
> Status: shipped | partial | stub

# <Title>

## Overview
...

## Start here
- `path/to/File.cs` — one-line role

## Extension points
...

## Depends on / Used by
- [other-system](other-system.md)

## Related docs
- Design (read-only): [Documents/design/...](../../design/....md)
```

## Additional resources

- Agent navigation rules: [AGENTS.md](../../AGENTS.md)
- Full doc conventions: [Documents/SKILL.md](../../Documents/SKILL.md)
