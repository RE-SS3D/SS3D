# CLAUDE.md

**Read [AGENTS.md](AGENTS.md) first.** It holds the docs-first navigation order, the hard rules on what you may edit, and where art and icons live. [Documents/SKILL.md](Documents/SKILL.md) is canonical for every doc you write. This file adds only what's specific to Claude Code and the Unity workflow.

## What this is

**henkhooft/SS3D** is a development fork of [RE-SS3D/SS3D](https://github.com/RE-SS3D/SS3D), a 3D take on Space Station 13. It targets Unity 6 (`6000.3.28f1`) + URP with [FishNet](https://fish-networking.com/) networking. Upstream is still on Unity 2021.3 + Built-in RP, so the codebases have diverged substantially; the divergence log is [Documents/FORK_STATUS.md](Documents/FORK_STATUS.md). Active branch: `develop`.

## Build, run, and test

Day-to-day dev runs through the Unity Editor. For a batchmode Linux client/server build, close the Editor on this project and run `Tools/build_client_and_server.sh` (`UNITY_EDITOR_PATH` selects the installed Unity executable). For validation with the Editor open, use targeted `Testing/ui/validate.sh --steps …` (see below).

- **Editor toolbar**: the fork adds Scene Switcher (left), Launcher toggle and Network Settings (right). If they're missing, right-click the toolbar and enable them under **Tools**.
- **Tests**: EditMode and PlayMode suites live under `Assets/Scripts/Tests/` (`SS3D.Tests.EditMode`, `SS3D.Tests.PlayMode`, `AssetAudit`, shared `Common`). Run them from **Window > General > Test Runner**. CI runs EditMode via `.github/workflows/editmodetestrunner.yml`.
- **Validation**: targeted by default (`--steps compile,editmode` + focused steps for what you touched; `visual` only for visual changes). No `--nightly`/`--weekly` unless the owner asks. Policy: [2026-11_testing-philosophy.md](Documents/architecture/2026-11_testing-philosophy.md); runbook: [Testing/ui/README.md](Testing/ui/README.md).
- **CI builds**: `.github/workflows/develop-release.yml` cuts a manual prerelease, Windows by default. See [2026-07_ci-develop-release-pipeline.md](Documents/architecture/2026-07_ci-develop-release-pipeline.md).
- **Code style**: `.editorconfig` (ReSharper/Rider inspections): `_camelCase` private fields, `PascalCase` events, block-scoped namespaces. Match the surrounding code.

## Skills

Project skills live in `.claude/skills/` (index: [.claude/skills/README.md](.claude/skills/README.md)). Run `update-system-docs` after feature work. Use `grilling` to stress-test a plan or design, `diagnosing-bugs` for hard bugs, and `spec-review` to review a branch against its plan. Unity perf, Frame Debugger and multiplayer-smoke skills live in `.cursor/skills/`. Read their `SKILL.md` directly when a task calls for them.
