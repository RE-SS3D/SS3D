# Contributing to SS3D

SS3D is built **docs-first and agent-assisted**. The recommended way to contribute is with an AI coding
agent: the repository is laid out so an agent can find the right code, follow the project's rules and
check its own work. You stay the author. You choose the work, steer the agent, and own every line you
submit.

New here after working on SS3D before November 2026? Read the [merge guide](Documents/archive/architecture/2026-11_merge-guide.md) first.

## Before you start

- **Questions and ideas** go to [Discussions](https://github.com/RE-SS3D/SS3D/discussions) or
  [Discord](https://discord.gg/Z3sPhyS). **Bugs** go in [issues](https://github.com/RE-SS3D/SS3D/issues/new/choose);
  in game, press **F8** to bundle a report with logs and a screenshot.
- **What to work on:** the current focus is in [Documents/milestones/](Documents/milestones/INDEX.md). Say on the
  issue or in Discussions what you're picking up, so two people don't build the same thing.

## Set up

```bash
git clone https://github.com/RE-SS3D/SS3D.git
cd SS3D
git checkout develop
```

Open the folder in Unity Hub with editor **`6000.3.28f1`**. The project adds **Scene Switcher** (left) and
**Launcher** and **Network Settings** (right) to Unity's main toolbar; if they're missing, right-click the
toolbar and enable them under **Tools**.

Then set up your agent:

| Agent | Reads | Skills |
|---|---|---|
| [Claude Code](https://claude.com/claude-code) | [CLAUDE.md](CLAUDE.md), which sends it to [AGENTS.md](AGENTS.md) | [.claude/skills/](.claude/skills/README.md) |
| [Cursor](https://cursor.com) | [AGENTS.md](AGENTS.md) | [.cursor/skills/](.cursor/skills/) |
| Any other agent | Point it at [AGENTS.md](AGENTS.md) first | Read a skill's `SKILL.md` when the task calls for it |

`AGENTS.md` is the contract: where to look before searching code, what may and may not be edited, and the
traps that compile and run but misbehave. For rendered UI checks, also set up the Unity MCP bridge in
[Testing/ui/README.md](Testing/ui/README.md).

## How work flows

Every change takes the same loop. The skill column is what to ask your agent for at each step.

| Step | What happens | Skill |
|---|---|---|
| 1. **Design** | Gameplay lives in [Documents/design/](Documents/design/) as WHAT and WHY. Design docs belong to the maintainers: propose new gameplay in Discussions, and stress-test the idea before you do. | `grilling` |
| 2. **Plan** | Decide HOW and IN WHAT ORDER: an architecture effort (`Documents/architecture/YYYY-MM_*.md`) for multi-system work, a plan (`Documents/plans/`) for a smaller piece. [Documents/SKILL.md](Documents/SKILL.md) defines both. | `grilling` |
| 3. **Build** | Start from [architecture/INDEX.md](Documents/architecture/INDEX.md) and the system map for your domain, and read its **Pitfalls** first. Write the test before the fix where you can. | `tdd`, `diagnosing-bugs` |
| 4. **Validate** | Run targeted checks: `Testing/ui/validate.sh --steps compile,editmode` plus the steps for what you touched; add `visual` for visual changes. Which tier a test belongs in: [testing philosophy](Documents/architecture/2026-11_testing-philosophy.md). | |
| 5. **Review** | Check the branch against its plan and the project's conventions. | `spec-review` |
| 6. **Sync docs** | Update the system maps (with their `Verified` stamp and any new Pitfalls), the INDEX, the plan or effort, and any milestone slice the work advanced. | `update-system-docs` |
| 7. **PR** | Open a PR into `develop` and fill in the template. | |

Step 6 is what keeps the maps true for the next person and the next agent. A PR that changes a system
without syncing its docs goes back for that before review.

The rules agents follow apply to you as well. The ones that cost the most when broken:

- New systems and UI register through code and recipes, not by editing `Boot.unity`, `Game.unity` or mega-prefabs
  such as `Human.prefab` ([agent-first composition](Documents/architecture/2026-07_agent-first-composition.md)).
- New UI is UI Toolkit (UXML/USS); uGUI is not extended.
- Art and icons go where the [art index](Documents/art-asset-index.md) and [icon index](Documents/icon-index.md) say.
  Most source art lives in [RE-SS3D/SS3D-Art](https://github.com/RE-SS3D/SS3D-Art); the
  [art guide](https://ss3d.gitbook.io/art-guide/) covers making it.

## What a mergeable PR looks like

- **One feature or fix**, with no unrelated changes, stray Unity reserialization or generated churn.
- **Matches its design doc**, or the PR says where it deviates and the system map or effort records it.
- **Docs synced** (step 6).
- **Validated** with the targeted checks above. Anything networked works for host, dedicated server and a
  remote client, and stays in sync between clients (the PR template has the checklist).
- **Versioned**: bump `PersistenceEnvelope.CurrentSchemaVersion` or `ProtocolVersion.Current` when old saves or
  clients break; end the commit with a `Version-Bump: minor` trailer when it completes a player-visible feature
  ([game versioning](Documents/architecture/2026-10_game-versioning.md)).
- **Honest about AI help**: name the agent you used in the PR. You have read and understood the whole diff, and
  you can explain any line of it.

PRs are squash-merged, so the PR title becomes the commit message on `develop`.

**CI on your PR.** The docs and link checks run on every PR. Unity jobs (EditMode, and the multiplayer smoke set
when a PR is labelled `test:multiplayer`) run on PRs from branches of this repository. PRs from forks never
receive the project's Unity license, so their Unity jobs wait for a maintainer, who reads the change and runs
them by hand (`gh workflow run editmodetestrunner.yml -f pr=<number>`). If you have write access here and open
the PR from your own fork, EditMode is dispatched for you and shows as the `EditMode (dispatched)` status.
Run `validate.sh` locally before you ask.

## Code style

`.editorconfig` (ReSharper/Rider inspections) sets the style: `_camelCase` private fields, `PascalCase` events,
block-scoped namespaces. Match the surrounding code.

## Playing and releases

To play, download the latest [release](https://github.com/RE-SS3D/SS3D/releases/latest), unzip it and run
`Game\SS3D.exe` (Linux: `Game/SS3D-Client`); the [testers' guide](Builds/TESTERS.md) covers map building and
bug reports. Maintainers publish numbered releases from `develop` with **Develop Release**
([release pipeline](Documents/architecture/2026-07_ci-develop-release-pipeline.md)).

## License

Code is [MIT](Documents/LICENSE-CODE.md); assets are [CC BY-NC-SA 4.0](Documents/LICENSE-ASSETS.md).
By contributing you agree your work is published under the same licenses. Everyone follows the
[code of conduct](Documents/CODE_OF_CONDUCT.md).
