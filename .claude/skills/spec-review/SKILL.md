---
name: spec-review
description: "Review the changes since a fixed point (commit, branch, tag, or merge-base) along two axes: Standards (does the code follow SS3D's documented conventions?) and Spec (does it match the design doc, architecture effort, or plan it implements?). Use when the user wants to review a branch, a PR, or work-in-progress against its plan or design, or asks to \"review since X\"."
---

Two-axis review of the diff between `HEAD` and a fixed point the user supplies:

- **Standards**: does the code conform to SS3D's documented conventions?
- **Spec**: does the code faithfully implement the design, effort, or plan it came from?

Both axes run as **parallel sub-agents** so they don't pollute each other's context, then this skill aggregates their findings. (This complements the built-in `/code-review`, which hunts correctness bugs; this skill checks conformance.)

## Process

### 1. Pin the fixed point

Whatever the user said is the fixed point (a commit SHA, branch name, tag, `develop`, `HEAD~5`, etc.). If they didn't specify one, propose `develop` and confirm.

Capture the diff command once: `git diff <fixed-point>...HEAD` (three-dot, so the comparison is against the merge-base). Also note the commits via `git log <fixed-point>..HEAD --oneline`.

Confirm the fixed point resolves (`git rev-parse <fixed-point>`) and the diff is non-empty before going further.

### 2. Identify the spec sources

Find, in this order, and collect every one that applies:

1. A path the user passed as an argument.
2. A `Documents/plans/*.plan.md` or `Documents/architecture/YYYY-MM_*.md` matching the branch name, the commit messages, or the touched domain.
3. The design doc sections that effort's `Implements:` header cites (`Documents/design/<domain>.md §N`).
4. If nothing is found, ask the user. If there isn't one, the **Spec** sub-agent skips and reports "no spec available".

### 3. Identify the standards sources

- `AGENTS.md`: the hard rules (design docs read-only, no mega-prefab hand edits, no new uGUI/TMP gameplay UI, no edits to condemned surfaces, no per-surface catalog MenuItems, icon placement).
- `.editorconfig` and `Assets/Scripts/Tests/SS3DRules.ruleset` (naming such as `_camelCase` private fields, `PascalCase` events, block-scoped namespaces). Skip anything the analyzer already flags.
- `Documents/architecture/2026-07_agent-first-composition.md` and `2026-07_editor-tooling-tiers.md` when the diff adds a SubSystem, entity behaviour, UI surface, prefab wiring, or editor tooling.
- The **Pitfalls** section of each system map whose code the diff touches.
- Docs hygiene, when the diff touches `Documents/`: headers, section order, and link direction per `Documents/SKILL.md`; a shipped feature with no system map / INDEX / plan update is a finding (`update-system-docs` not run).

On top of these, the Standards axis always carries the **smell baseline** below: a fixed set of Fowler code smells (_Refactoring_, ch.3). Two rules bind it:

- **The repo overrides.** A documented repo standard always wins; where it endorses something the baseline would flag, suppress the smell.
- **Always a judgement call.** Each smell is a labelled heuristic ("possible Feature Envy"), never a hard violation.

Each smell reads *what it is* → *how to fix*; match it against the diff:

- **Mysterious Name**: a name that doesn't reveal what it does or holds. → rename it; if no honest name comes, the design's murky.
- **Duplicated Code**: the same logic shape in more than one hunk or file. → extract the shared shape, call it from both.
- **Feature Envy**: a method that reaches into another object's data more than its own. → move the method onto the data it envies.
- **Data Clumps**: the same few fields or params keep travelling together. → bundle them into one type, pass that.
- **Primitive Obsession**: a primitive or string standing in for a domain concept. → give the concept its own small type.
- **Repeated Switches**: the same `switch`/`if`-cascade on the same type recurs. → replace with polymorphism, or one map both sites share.
- **Shotgun Surgery**: one logical change forces scattered edits across many files. → gather what changes together into one module.
- **Divergent Change**: one file is edited for several unrelated reasons. → split so each module changes for one reason.
- **Speculative Generality**: abstraction or hooks for needs the spec doesn't have. → delete it; inline back until a real need shows.
- **Message Chains**: long `a.B().C().D()` navigation the caller shouldn't depend on. → hide the walk behind one method.
- **Middle Man**: a class that mostly just delegates onward. → cut it, call the real target direct.
- **Refused Bequest**: a subclass that ignores most of what it inherits. → drop the inheritance, use composition.

### 4. Spawn both sub-agents in parallel

**Standards sub-agent prompt** includes:

- The diff command and commit list.
- The standards-source files from step 3, **plus the smell baseline** pasted in full.
- The brief: "Report, per file/hunk, (a) every place the diff violates a documented standard: cite the file and rule; and (b) any baseline smell: name it and quote the hunk. Distinguish hard violations (AGENTS.md rules, documented conventions) from judgement calls (baseline smells). Skip anything the analyzer enforces. Under 400 words."

**Spec sub-agent prompt** includes:

- The diff command and commit list.
- The spec paths from step 2.
- The brief: "Report: (a) requirements or plan todos that are missing or partial; (b) behaviour in the diff that wasn't asked for (scope creep); (c) requirements that look implemented but wrong; (d) silent deviations from a design doc section: a deviation is fine only when the effort doc states it ('deviates from X §N because'). Quote the spec line for each finding. Under 400 words."

If no spec exists, skip the Spec sub-agent and say so in the final report.

### 5. Aggregate

Present the two reports under `## Standards` and `## Spec` headings, verbatim or lightly cleaned. Keep the axes separate; don't merge or rerank findings across them.

End with a one-line summary: total findings per axis, and the worst issue _within each axis_ (if any).

## Why two axes

A change can pass one axis and fail the other:

- Code that follows every convention but implements the wrong thing → **Standards pass, Spec fail.**
- Code that does exactly what the plan asked but hand-edits `Human.prefab` → **Spec pass, Standards fail.**

Reporting them separately stops one axis from masking the other.
