---
name: grilling
description: Grill the user relentlessly about a plan, decision, or idea, gameplay design or implementation. Use when the user wants to stress-test their thinking, or uses any 'grill' trigger phrases.
---

Interview the user relentlessly until you reach a shared understanding. Map this as a **design tree**: every decision branches into the decisions that hang off it.

## Pick the altitude

Every SS3D grill sits at one of two altitudes, and the altitude decides what counts as settled and where the result lands. Name it to the user in your first round; a session can move between them, so re-label when it does.

- **Gameplay**: WHAT the player experiences and WHY (a new system, a role, a mechanic, a round flow). The user decides as owner of `Documents/design/`. Implementation HOW stays out of the tree: when a HOW question surfaces, park it in a **Parked for implementation** list rather than asking it.
- **Implementation**: HOW and IN WHAT ORDER to build something. Design doc sections are **premises**, already settled. When a decision would contradict one, put that to the user as its own question: revise the design (owner's call, made in a gameplay grill) or deviate (recorded as "deviates from `<doc>` §N because ...").

## Ground the tree before round 1

Facts are your job, never the user's. Before the first round, read in this order and cite what you read:

1. `Documents/architecture/INDEX.md`: the domain's row in the coverage table (designed? architected? mapped?).
2. The domain's design doc, plus design docs of adjacent systems it touches.
3. `Documents/milestones/INDEX.md`: which playable gate this serves and what it blocks.
4. Implementation altitude only: the system map, its **Pitfalls**, and its `Verified` stamp (far behind `HEAD` means confirm files before trusting it); `Documents/architecture/2026-07_agent-first-composition.md` for SubSystem, prefab, and UI constraints.

When a later question needs a fact (does X exist, what does the code do today, how did SS13 handle it), dispatch a sub-agent with that same docs-first order; don't ask the user for anything you could look up. Don't block on it: a running exploration is an unsettled prerequisite, so only the questions downstream of it wait; ask the rest of the frontier now.

## Rounds

Work the tree in **rounds**. The **frontier** is every decision whose prerequisites are already settled: the questions you can ask _now_ without guessing at answers you haven't heard yet. Ask the whole frontier in one round: number each question and give your recommended answer. Then wait for the user's answers before the next round.

Format a round like so:

```
❓ **Q1** - **<question title>**: <question body, might be multiple paragraphs, including multiple choices>

➡️ <your recommended answer>

---

❓ **Q2** - **<question title>**: <question body, might be multiple paragraphs, including multiple choices>

➡️ <your recommended answer>
```

Each round the user answers reshapes the tree: settled decisions push the frontier outward and unblock questions that depended on them. Recompute the frontier and ask the next round. A question whose answer depends on another question still open in this round belongs to a _later_ round, not this one.

The _decisions_ are the user's: put each to them and wait. Recommend from the design docs, SS13 precedent, and the milestone's needs; say which one drove the recommendation.

## Finish

The session is done when the frontier is empty: every branch of the design tree visited, nothing left silently assumed. Summarise the settled tree, then ask the user to confirm you have reached a shared understanding. Act on nothing before that confirmation.

After confirmation, offer the write-up that fits the altitude, following `Documents/SKILL.md` for headers, section order, and linking:

- **Gameplay**: a new or revised `Documents/design/<domain>.md`. Design docs are owner-only, so write it only when the user says yes to this offer. Keep it WHAT/WHY: cite other design docs only, no HOW, no build status, no prototyping prompts. Hand the **Parked for implementation** list back separately, for a later implementation grill.
- **Implementation**: an architecture effort `Documents/architecture/YYYY-MM_<slug>.md` (`Implements:` cites the design sections), or a `Documents/plans/<slug>.plan.md` with todos for a smaller piece; then the milestone slice it advances.
