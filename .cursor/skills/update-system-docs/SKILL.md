---
name: update-system-docs
description: Updates architecture INDEX, system maps, implementation plans, and milestone focus after code changes. Use when finishing a feature, after merging implementation work, or when the user asks to sync navigation docs with the codebase.
disable-model-invocation: true
---

# Update System Docs

Sync navigation documentation after implementation work. Update current sections in place; leave session history and test-run timelines in git/artifacts (see Documents/SKILL.md, Live-reference principle). Updates INDEX, system maps, plans,
architecture effort status, and playable milestone slice/focus status.

**Never edit:** `Documents/design/*` (unless the owner explicitly asks).

## Checklist

```
- [ ] Step 1: Identify scope from git diff
- [ ] Step 2: Read affected system maps (don't rewrite untouched systems)
- [ ] Step 3: Update affected maps (content, Status, Verified stamp, Pitfalls)
- [ ] Step 4: Update INDEX.md
- [ ] Step 5: Update linked plans
- [ ] Step 6: Update architecture effort docs, archive what shipped
- [ ] Step 7: Update milestones (slice status + hub current focus)
- [ ] Step 8: Sanity check links, paths, stamps
```

## Step 1: Identify scope

Run `git diff` (or review changed paths from the session). Map paths to systems via [Documents/architecture/INDEX.md](../../../Documents/architecture/INDEX.md):

| Path prefix | System map |
|-------------|------------|
| `Assets/Scripts/SS3D/Interactions/` | interactions-framework |
| `Assets/Scripts/SS3D/Systems/Interactions/` | interactions-runtime |
| `Assets/Scripts/SS3D/Systems/Selection/`, `Rendering/URP/SelectionPick*` | selection, rendering |
| `Assets/Scripts/SS3D/Systems/Examine/` | examine |
| `Assets/Scripts/SS3D/Systems/Tile/` | tile |
| `Assets/Scripts/SS3D/UI/MachineInterface/` | machine-interface |
| `Assets/Scripts/SS3D/Systems/Rounds/`, `UI/Lobby/` | rounds-lobby |
| `Assets/Scripts/SS3D/UI/Shell/` | ui-shell |

When unsure, read INDEX first. If the question or change advances a **playable gate**, also open [Documents/milestones/INDEX.md](../../../Documents/milestones/INDEX.md).

## Step 2: Read affected maps

Open only the maps for systems touched by the change. Read [Documents/SKILL.md](../../../Documents/SKILL.md) for the system map template and section order — it's canonical; this skill doesn't restate it.

## Step 3: Update each affected map

Update only what changed:

- **Start here** — add/remove key files when new important types land
- **Extension points** — document new patterns for future agents
- **Pitfalls** — record any silent failure you hit or fixed this session (symptom → cause → fix). Code that compiled and ran but misbehaved with no error belongs here; it's the highest-value thing you can leave the next agent.
- **Depends on / Used by** — update cross-system links
- **Related docs** — link new plans or effort docs
- **Status** — bump `stub` → `partial` → `shipped` when appropriate; use `condemned` (or a Condemned UI / Prefab composition debt subsection) per [Documents/SKILL.md](../../../Documents/SKILL.md) and [2026-07_agent-first-composition.md](../../../Documents/architecture/2026-07_agent-first-composition.md)
- **Overview** — revise if the system's role changed materially
- **Verified** — bump the header `> Verified: <short-sha> — <date>` **only on maps you actually re-checked against code this session.** Never bump a map you didn't touch — the gap between its stamp and `HEAD` is the staleness signal, and a false-fresh stamp destroys it. The SHA must survive the squash merge: on a feature branch stamp `git merge-base HEAD origin/develop`, on `develop` stamp `HEAD` (rule and how to read an old stamp: [Documents/SKILL.md](../../../Documents/SKILL.md) **Verified stamp**).

Keep each map under ~80 lines (Pitfalls exempt). Do not duplicate design doc content — link instead.

When a redesign **ships** and purges legacy UI or mega-prefab components: remove condemned markers / note prefab debt reduction. Never “upgrade” condemned uGUI to `partial` by restyling. Never document “add component on Human.prefab” as an extension point — point at Editor setup tools or the owning redesign’s Phase 0 instead.

## Step 4: Update INDEX.md

In [Documents/architecture/INDEX.md](../../../Documents/architecture/INDEX.md):

- Update the **Status** column for affected rows
- Add a new row if a new domain was introduced (see [Documents/SKILL.md](../../../Documents/SKILL.md) "Starting a new domain")
- Add architecture effort or plan links if new docs were created
- Update the **Coverage table**: if this work shipped or created an architecture effort
  or system map for a domain, update that domain's row (never touch its Design column —
  that's owner-authored)

## Step 5: Update implementation plans

If work followed a plan in `Documents/plans/*.plan.md`:

1. Mark completed todos in YAML frontmatter (`status: completed` or `cancelled`)
2. Add an **Implementation notes** section at the bottom if shipped result diverged from the plan
3. When every todo is `completed` or `cancelled`, archive the plan in Step 6

## Step 6: Update architecture effort docs

If work completed a dated effort in `Documents/architecture/YYYY-MM_*.md`:

- Set header `> Status: shipped` (or `abandoned` with explanation). One word; put phase detail in the effort's `**Progress:**` line.

Then archive every effort and plan that is now `shipped` or fully completed, per [Documents/SKILL.md](../../../Documents/SKILL.md) Archive:

1. **Harvest**: move known issues to the map's Pitfalls or TECH_DEBT, deferred phases to the milestone slice, and design deviations to the map.
2. Keep current policies/runbooks active. If a completed effort is cited from code, CI, tests, skills or governance, harvest its live guidance and retarget the citations before archiving it (Documents/SKILL.md, Live-reference principle).
3. `git mv` the rest into `Documents/archive/architecture/` or `Documents/archive/plans/`, then fix relative links into and out of each moved file.
4. In INDEX, drop the doc from **Active efforts and plans** and repoint its coverage-table link at the archive path.

## Step 7: Update milestones

If the shipped work advances a slice under [Documents/milestones/](../../../Documents/milestones/)
(see [Documents/SKILL.md](../../../Documents/SKILL.md) Milestones section):

1. Bump that slice’s **Status** (and child effort/plan link) in the owning gate doc
   (e.g. `mvp1-nuke-ops.md`)
2. Update the gate header `> Current focus:` to the deepest unfinished critical leaves
3. Mirror **Current focus** on [Documents/milestones/INDEX.md](../../../Documents/milestones/INDEX.md)
4. When the gate's first slice ships, set its header `Status: in-progress`; when the whole gate ships, set `Status: shipped` and retarget hub focus to the next gate
5. Update the slice's **Player view** so it matches the new status (what now works goes in parentheses), then run `python3 Tools/generate_roadmap.py` to regenerate `ROADMAP.md` and the README block. Done when `python3 Tools/generate_roadmap.py --check` passes

Do not restate design rules or implementation HOW in milestone docs — cite only.

## Step 8: Sanity check

- Every file listed under **Start here** exists in the repo
- INDEX links resolve to existing system map files
- Every `Verified` stamp you bumped points at a commit you actually checked the map against, and that commit is on `develop` (`git merge-base --is-ancestor <sha> origin/develop`)
- Any `Pitfalls` entry you added describes real, current behavior
- Milestone links resolve; hub current focus matches the active gate header
- No edits were made to `Documents/design/`
- No active doc links into a moved file's old path (`git grep` the old filename)

## System map template

Use the canonical header block and fixed section order in [Documents/SKILL.md](../../../Documents/SKILL.md) ("Required header blocks"). New maps go in `Documents/architecture/systems/` with a kebab-case filename, `Status: stub`, and a `Verified` stamp set to current `HEAD`. Do not re-embed the template here — it drifts.

## Additional resources

- Agent navigation rules: [AGENTS.md](../../../AGENTS.md)
- Full doc conventions: [Documents/SKILL.md](../../../Documents/SKILL.md)
- Playable milestones: [Documents/milestones/INDEX.md](../../../Documents/milestones/INDEX.md)
