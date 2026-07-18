# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

**henkhooft/SS3D** — a development fork of [RE-SS3D/SS3D](https://github.com/RE-SS3D/SS3D), an open-source
resurrection of *beep's original SS3D (a 3D take on Space Station 13). This fork redesigns core gameplay systems
and dev foundations faster than upstream's review capacity allows. It targets Unity 6 (`6000.3.16f1`) + URP,
whereas upstream is still on Unity 2021.3 + Built-in RP — the two codebases have diverged substantially
(~248 commits, ~12k files). Networking is via [FishNet](https://fish-networking.com/).

There is no official RE:SS3D release channel for this fork; day-to-day play is still build-from-source
in the Unity Editor. Maintainers can optionally cut a **manual prerelease** (Windows player zip with
launch bats; Linux secondary when opted in) via `.github/workflows/develop-release.yml` (default
Windows-only; Linux, EditMode, and smoke are opt-in) — see
`Documents/architecture/2026-07_ci-develop-release-pipeline.md`.

## Read the docs before searching code

This repo's most important convention is **docs-first navigation**, defined in `AGENTS.md` (repo root). Follow it:

1. Read `Documents/architecture/INDEX.md` first — it's the navigation hub and a per-domain coverage table
   (design spec / architecture effort / system map, each linked or marked "none yet").
2. Open the linked **system map** under `Documents/architecture/systems/` for entry points and key files for that
   domain.
3. Only then read source files or grep — don't do full-tree exploration as a first move.

Doc layers and who owns them:

| Layer | Path | Rule |
|-------|------|------|
| Design specs | `Documents/design/` | **Read-only.** Owner-authored WHAT/WHY gameplay specs. Never create or edit these, even if code diverges — note the divergence in a system map/plan/architecture doc instead. |
| Architecture efforts | `Documents/architecture/YYYY-MM_*.md` | HOW/order for one implementation effort. Set header `Status` when it ships. |
| System maps | `Documents/architecture/systems/*.md` | WHERE in code (entry points, key files) per domain. Update after feature work. Keep under ~80 lines; link to design docs rather than duplicating rules. |
| Plans | `Documents/plans/*.plan.md` | Temporary in-flight implementation plans with YAML frontmatter/todos. Update todos on ship; never delete. |
| `Documents/FORK_STATUS.md` | — | **Read-only** unless the owner explicitly asks. Divergence log vs. upstream. |
| `Documents/SKILL.md` | — | Authoring conventions (header blocks, linking rules, section order) for everything above. Read before writing any doc. |

After implementing a feature, run the `update-system-docs` skill (`.cursor/skills/update-system-docs/SKILL.md`) to
sync system maps, INDEX.md, plans, and architecture effort status.

### Finding art and UI icons

- Art: `Documents/art-asset-index.md` (overview) → `Documents/art-available-for-import.json` (not-yet-imported
  assets from `RE-SS3D/SS3D-Art`) → `Documents/art-asset-index.json` (what's already imported). Regenerate with
  `python3 Tools/generate_art_index.py` after importing new art.
- UI icons: `Documents/icon-index.md` → `Documents/icon-index.json`. Icons live under
  `Assets/Art/Icons/external icons/` (game-icons.net SVGs). Regenerate with `python3 Tools/generate_icon_index.py`.

## Build, run, and test

This is a Unity project — there is no CLI build/test flow for day-to-day dev; everything runs through the Unity
Editor.

- **Open the project**: open the repo root in Unity Hub (Unity `6000.3.16f1`). `git checkout develop` is the
  active branch upstream builds from.
- **Editor tools**: this fork adds custom items to Unity's main toolbar — Scene Switcher (left), Launcher toggle
  and Network Settings (right). If missing, right-click the toolbar and enable them under **Tools** (Unity hides
  custom toolbar items until enabled per-user).
- **Tests**: EditMode and PlayMode tests live under `Assets/Scripts/Tests/` (`SS3D.Tests.EditMode`,
  `SS3D.Tests.PlayMode`, plus an `AssetAudit` edit-mode assembly and a `Common` shared-fixture assembly). Run them
  via Unity's **Test Runner** window (`Window > General > Test Runner`) inside the Editor. CI runs the same
  EditMode suite headlessly via `game-ci/unity-test-runner` — see `.github/workflows/editmodetestrunner.yml`.
- **CI build / prerelease**: `.github/workflows/develop-release.yml` is the manual path
  (default Windows client + bats → GitHub prerelease; `build_linux` / `run_editmode` /
  `run_smoke` opt-in). Cheap EditMode: `editmodetestrunner.yml`. Opt-in smoke:
  `multiplayer-smoke-test.yml`.
- **Code style**: `.editorconfig` at the repo root enforces C# naming/formatting (enforced as ReSharper/Rider
  inspections, not a separate lint CLI step) — e.g. `_camelCase` private fields, `PascalCase` events, block-scoped
  namespaces. Match existing surrounding code style; don't fight the analyzer.

## Architecture

### Subsystem pattern

Nearly every gameplay domain is a `SubSystem` (non-networked) or `NetworkSubSystem` (networked, extends FishNet's
`NetworkActor`), looked up at runtime via the `SubSystems.Get<T>()` service locator rather than direct references.
See `Documents/architecture/systems/core-subsystems.md` and `Assets/Scripts/SS3D/Core/`. When adding a new domain
service, follow this pattern rather than inventing a new lookup mechanism.

### Top-level source layout

- `Assets/Scripts/SS3D/Systems/` — one folder per gameplay domain (Area, Atmospherics, Electricity, Health,
  IdAccess, Interactions, Inventory, Rounds, Tile, ...), each its own asmdef under `SS3D.Systems`.
- `Assets/Scripts/SS3D/Interactions/` — the shared `IInteraction` framework (contracts, pipeline, wire
  identifiers) that `Systems/Interactions/` builds runtime behavior (radial menu, armed interactions, outlines) on
  top of.
- `Assets/Scripts/SS3D/Core/`, `Networking/`, `SceneManagement/`, `Data/`, `Rendering/`, `Localization/`,
  `Logging/`, `Permissions/`, `Application/` — cross-cutting infrastructure, not gameplay domains; each has a
  system map under `Documents/architecture/systems/`.
- `Assets/Scripts/Tests/` — EditMode/PlayMode test assemblies, separate from `Assets/Scripts/SS3D/`.
- `Assets/Scripts/External/` — vendored third-party code (e.g. NaughtyAttributes) — don't apply this repo's
  conventions retroactively to it.

For any specific domain (e.g. "how does the machine-interface UI talk to areas/electricity"), the system map is
much faster and more reliable than tracing code cold — read it first.

### Documentation is directional

Design docs cite only other design docs. Architecture effort docs and system maps may cite design docs, but never
the reverse. This keeps design specs stable regardless of how implementation churns — don't add a design-to-plan
or design-to-architecture-doc link even if it would seem locally convenient.
