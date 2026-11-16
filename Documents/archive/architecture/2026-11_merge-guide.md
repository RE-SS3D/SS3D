# What the 2026 merge brought

In November 2026 the `develop` branch of the `henkhooft/SS3D` development fork was merged into
`RE-SS3D/SS3D` `develop` with its full history. This page is the reviewer guide for that merge and
the orientation for anyone who worked on the project before it: what changed, what you need before you
pull, and what is gone. It was archived after the merge; the live docs start at
[architecture/INDEX.md](../../architecture/INDEX.md) and [AGENTS.md](../../../AGENTS.md).

Plan and status of the merge itself: [2026-11_upstream-merge.md](2026-11_upstream-merge.md).

## At a glance

| | Before the merge | After the merge |
|---|---|---|
| Unity version | 2021.3.15f1 | **6000.3.28f1** (Unity 6) |
| Render pipeline | Built-in | **URP 17** (Forward+) |
| Releases | Unprefixed tags (`0.3.95o`), ss3d.space download | Numbered `vMAJOR.MINOR.PATCH` releases (Windows + Linux client/server) cut by `develop-release.yml`, with an in-game and scripted updater; nightlies stay prereleases |
| Documentation | GitBook ([ss3d.gitbook.io](https://ss3d.gitbook.io/dev-guide/)) | `Documents/`: design specs, architecture efforts, system maps, [milestones](../../milestones/INDEX.md) |
| Roadmap | GitHub milestones + project board automation | [milestones/](../../milestones/INDEX.md), rendered to [ROADMAP.md](../../../ROADMAP.md) |
| Tests | EditMode on PRs | EditMode, PlayMode, multiplayer smoke against real server + client builds, rendered UI pilots |
| Size of the change | — | ~1,480 commits; ~19.6k files, +1.76M / −340k lines |

## Before you pull

- **Install Unity 6000.3.28f1.** The first import rebuilds `Library/` from scratch and takes a while.
  Built-in RP materials and shaders are gone; anything made for 2021.3 needs porting, not merging.
- **Branches and PRs made before the merge cannot be rebased.** Assets, ProjectSettings, prefabs and most
  systems were rewritten. The open PRs from before the merge were closed; redo the change on the new `develop`.
- **Read [AGENTS.md](../../../AGENTS.md) and [CONTRIBUTING.md](../../../CONTRIBUTING.md) first.** Development is
  docs-first and agent-assisted: design spec → architecture effort or plan → code → targeted validation →
  doc sync. [Documents/SKILL.md](../../SKILL.md) defines every doc layer.
- **Releases continue the numbered line** (`v0.17.x` before the merge, `v0.18.0` the first from this
  repository). The old `0.3.95x` builds stay where they are; the updater ignores them.
- **Running a game changed.** The default port is **1151** everywhere. `Builds/Game/Config/permissions.txt`
  is now seeded from `permissions.default.txt`; whoever plays locally is ServerOwner; dedicated servers
  check admin tokens. See [Builds/README.txt](../../../Builds/README.txt).
- **CI changed.** See [Repository, CI and documentation](#repository-ci-and-documentation) below.

## Shipped gameplay

Player-facing systems the merge brought, or brought in a much fuller form. Coverage is how much of the
design intent is playable, not "code exists". Each row links to the system map that is kept current;
the [coverage table](../../architecture/INDEX.md) is the live, project-wide view.

| Feature | Coverage | What shipped |
|---|---|---|
| [Selection](../../architecture/systems/selection.md) | shipped | Shader mesh picking; hover outlines exclude pick shells |
| [Examine](../../architecture/systems/examine.md) | partial | UITK hover/Shift detail; character paperdoll Search + hold-to-take; health Tier 0/1 lines; obscured-slot filter / restrained loot open |
| [Interactions + radial menu](../../architecture/systems/interactions-runtime.md) | shipped | Hardened multiplayer interactions; three-tier UITK radial; armed Tier-2; Discover contract; `InteractionController` decomposed (combat on sibling network) |
| [Main HUD](../../architecture/systems/inventory.md) | partial | Gear/hands/intent UITK overlay; health+atmos alert stack; storage panels; melee/ranged reticle; vitals cluster still pending |
| [Inventory & storage](../../architecture/systems/inventory.md) | partial | Container model + on-demand panel; clothing folded world presentation; bright full-toon ObjectIcon HUD icons; Play Mode verification pending |
| [Comms](../../architecture/systems/chat-audio-screens.md) | partial | Local speech chips + crowd cap; non-diegetic radio feed + Tab/slash compose + announce SFX; headset/PDA/radial channel pick deferred |
| [Disposal](../../architecture/systems/disposal.md) | partial | BFS pipe routing, chute/outlet transit, SizeClass gate; pipe crafting, Cargo hook, player transit deferred |
| [Health](../../architecture/systems/health.md) | partial | Two-tier damage, organs, bleeding, critical/defib, treatments, severing; env exposure + feel SFX; critical ragdoll; alert stack; vitals UITK pending |
| [Combat](../../architecture/systems/combat.md) | partial | Melee + ranged hitscan (M4) + armor absorption + swing/fire stamina drains; M1p feel (muzzle/holes/audio/two-hand); disarm/block/seal deferred |
| [Structural destruction](../../architecture/systems/structural-destruction.md) | partial | Per-tile integrity, melee force, blast BFS, tint/examine VFX; debris spawn / Area local remesh deferred |
| [Body animation](../../architecture/systems/entities.md) | partial | Peaceful / melee / ranged stance; injured limp; Ragdoll owns collapsed/dead/critical presentation; ranged aim IK deferred |
| [Screen effects](../../architecture/systems/screen-effects.md) | partial | Health overlays + hit/blast flash; turf temp/fire from `HealthSnapshot.Environment` |
| [Audio](../../architecture/systems/audio.md) | partial | SFX occlusion, impact and machine sounds, department room tone, wind and sound needs air, personal heartbeat/breathing, alert cues, footsteps; music/volume categories pending |
| [Vision / FOV](../../architecture/systems/rendering.md) | shipped | Hard black fog-of-war from viewpoint raycasts (hot-path GC cut) |
| [Station lighting (URP)](../../architecture/systems/rendering.md) | partial | Fixture-driven half-toon Forward+ look; client SyncVar fixture visuals; visual plate polish still open |
| [Areas + station power](../../architecture/systems/area.md) | partial | APC-seeded areas, kWh cells, channel shedding, area lights/switches; [solar panels + tracker](../../architecture/systems/electricity.md); live remesh pending |
| [Machine interfaces](../../architecture/systems/machine-interface.md) | shipped | Diegetic UITK APC/SMES/atmos ports/vending; engineering ID swipe; host-echo TargetRpc gate |
| [Atmospherics](../../architecture/systems/atmospherics.md) | partial | Turf gas Burst sim + fire + per-edge bulk flow + equal-P composition diffusion; pipe networks + ports; Phase 1 dirty-chunk client VFX sync (AOI / late-join deferred) |
| [ID / access](../../architecture/systems/id-access.md) | partial | Crew records, door + machine gates, ID console; auth logs / broader design pending |
| [Tilemap / construction](../../architecture/systems/tile.md) | shipped | Adjacency engine, construction service, AOI tile sync, layer visibility |
| [Map Editor](../../architecture/systems/map-editor-creative-hooks.md) | partial | Full-screen UITK Map Editor + spawn-point authoring/persistence; Builder / instant place / round-config pool / runtime spawn pick deferred |
| [Rounds](../../architecture/systems/rounds-lobby.md) | shipped | Single-flight round state machine (join/start/end races hardened) |
| [Lobby](../../architecture/systems/rounds-lobby.md) | partial | UITK lobby shell + Character Creator (Human; saved slots), job/antag prefs with one-pass resolution, networked OOC/LOOC/Admin chat, Server Settings + live Server Info; legacy uGUI lobby purged |
| [Session / world lifecycle](../../architecture/systems/networking-session.md) | shipped | Session FSM + world readiness graph + `NetworkSystemsHub`; Boot disconnect storm fixed; host loopback / client spawn polish |

## Foundations

Engineering work players don't see directly, each with its own map or effort:

- **Persistence** ([persistence](../../architecture/systems/persistence.md)): contributor-based save/load with a
  versioned `PersistenceEnvelope`, replacing the monolithic tilemap JSON; station templates restore areas.
- **Structured logging** ([logging](../../architecture/systems/logging.md)): Serilog with mandatory sender and
  context, per-namespace filtering, console and file sinks, client-ID enrichment.
- **Input arbitration** ([inputs](../../architecture/systems/inputs.md)): handle-based input ownership; one writer
  of `InputAction.enabled`; pointer-over-UI spanning uGUI and UI Toolkit.
- **Headless dedicated server** ([networking-session](../../architecture/systems/networking-session.md)): a real
  Server-subtarget Linux build with `UNITY_SERVER` guards, Docker Compose and start scripts.
- **Session / world lifecycle** ([networking-session](../../architecture/systems/networking-session.md)): a session
  FSM and world readiness graph instead of timing-based init; `NetworkSystemsHub` for network subsystems.
- **Examine localization** ([localization](../../architecture/systems/localization.md)): one Examine string table
  for every `ExamineData` asset, behind `LocalizedTextService`.
- **Agent-first composition** ([policy](../../architecture/2026-07_agent-first-composition.md),
  [ui-shell](../../architecture/systems/ui-shell.md)): no scene or mega-prefab hand edits for new features; UI
  Toolkit with catalog/path loading; condemned uGUI is replaced, not migrated.
- **Human.prefab decomposition** ([entities-prefab-recipes](../../architecture/systems/entities-prefab-recipes.md)):
  `PrefabUtility` recipes instead of hand edits, with integrity tests.

### Hot-path performance

Per-tick GC cut on the simulation hot paths (`TileCoord` dictionary keys, atmos upload, pipe network
queries, substance containers) and in Vision FOV and interaction outline discovery. The pitfalls are
recorded on the atmospherics, tile, substances and rendering maps.

## Engine and toolchain

### Unity 6 upgrade

- Assets and ProjectSettings reserialized to Unity 6; ugui 2.0 / TMP Essential Resources
- Addressables 2.x cleanup; Scriptable Build Pipeline settings
- Input System `Controls.cs` regenerated (inputsystem 1.19.0); FishNet `DefaultPrefabObjects` rebuilt
- Editor toolbar tools moved to the Unity 6 **MainToolbar API**: Scene Switcher (left), Launcher toggle and
  Network Settings (right). If they're missing, right-click the toolbar and enable them under **Tools**.

### URP migration

- URP 17 (`com.unity.render-pipelines.universal` 17.3.0); pipeline asset and Forward+ renderer in
  `Assets/Settings/URP/`
- Simple Toon shader ported to URP HLSL; content materials converted from Built-in Standard to URP Lit
- Post Processing Stack v2 removed; post-processing on URP volumes; pipeline MSAA off so camera TAA runs
- Migration tooling in `Assets/Editor/URPMigration/`
- Selection pick pass and the atmospherics composite as URP render features ([rendering](../../architecture/systems/rendering.md))

Station lighting on top of it: fixture-driven half-toon lighting, zero ambient, dual fixture lights, SSAO.
Look polish is still open: [2026-07_urp-lighting-look-polish.md](../../architecture/2026-07_urp-lighting-look-polish.md).

### Addressables

A dual-path loader (`AssetHandle<T>` / `AssetProvider`, eager sync by default, async opt-in per database),
`com.unity.addressables` 2.9.1 and a single settings root under `Assets/Content/`. The remaining phases are in
[2026-07_addressables-expansion-migration.md](../../architecture/2026-07_addressables-expansion-migration.md).

### Analyzer tooling

StyleCop analyzers are disabled in the project to cut friction on AI-assisted edits. `.editorconfig`
(ReSharper/Rider inspections) still sets the style; EditMode CI runs on every PR.

### Testing and validation

- **Multiplayer smoke harness** (`Testing/multiplayer/`): real dedicated-server and client processes driven
  by a scenario DSL; nightly on the self-hosted runner and on PRs labelled `test:multiplayer`
- **PlayMode suite** repaired and extended; passing known-issue repros promoted into the regular suite
- **One-command validation** (`Testing/ui/validate.sh`): compile check, EditMode, rendered UI pilots with
  real OS input; `--nightly` adds PlayMode, builds and smoke ([Testing/ui/README.md](../../../Testing/ui/README.md))
- **Multiplayer Play Mode checks** (`--steps mppm:<scenario>`): the smoke scenarios in an editor host plus
  virtual player, no build needed ([networking-session-testing](../../architecture/systems/networking-session-testing.md))
- Policy for which tier a test belongs in: [2026-11_testing-philosophy.md](../../architecture/2026-11_testing-philosophy.md)

Structural risks are ranked in [TECH_DEBT.md](../../architecture/TECH_DEBT.md); profiler and Frame Debugger
exports for agents are in `.cursor/skills/`.

## Repository, CI and documentation

### Documentation

`Documents/` replaces the GitBook as the source of truth. Layers and rules are in [SKILL.md](../../SKILL.md);
agents start at [AGENTS.md](../../../AGENTS.md), Claude Code additionally at [CLAUDE.md](../../../CLAUDE.md). The
GitBook still helps with generic Unity and FishNet concepts. Shipped efforts and completed plans live in
[archive/](..).

### Workflows

| Workflow | What it does |
|---|---|
| `develop-release.yml` | Manual numbered releases and `develop-<suffix>` test cuts; nightly `develop-nightly` prerelease |
| `editmodetestrunner.yml` | EditMode on PRs from this repository's branches, nightly and manual runs; a maintainer runs fork PRs with `-f pr=<number>` |
| `multiplayer-smoke-test.yml` | Nightly PlayMode and headless smoke scenarios; on this repository's PRs labelled `test:multiplayer` |
| `weekly-graphical.yml` | Weekly rendered-editor pilots (`validate.sh --weekly`) |
| `updater-tests.yml`, `updater-e2e.yml` | Updater regression tests on PRs; a real previous-stable → latest-stable update after releases |
| `docs-check.yml` | On every PR: fails when `ROADMAP.md` or the README roadmap block is stale, or something links to the old fork |
| `asset-hygiene.yml` | Fails on orphan `.meta` files under `Assets/` |

Heavy jobs run on a self-hosted runner, and only for trusted code: branches of this repository, schedules and
maintainer dispatches. PR code from forks never reaches it; how
the project's infrastructure is run is in [2026-11_project-infrastructure.md](../../architecture/2026-11_project-infrastructure.md).
Failed nightly and weekly runs open issues labelled `nightly-failure` / `weekly-failure`, closed by the next green run.

### What is gone

- GitHub milestone and project-board automation (`AddIssueToMilestoneProject`, `AddToRoadmap`,
  `*ProjectWhenMilestone*`, `TaskAssignment`), the Discord test notifications and the old `main.yml` /
  `activation.yml` packaging workflows
- The Markdown issue templates (now YAML issue forms; ideas go to Discussions)
- `Documents/UnityLicense.ulf` and the `Start_SS3D_*.bat` launch files (the main menu replaces them)

## How the merge was made

- **Merge commit, full history.** `git blame` and the system maps' `Verified:` commit stamps keep working.
  Commit messages from before the merge say `#N` for the fork's own PRs and issues; those numbers are not
  this repository's. Docs qualify them as `henkhooft/SS3D#N`.
- **Upstream #1491 (Port Vision System)** was already ported to URP and is recorded as merged.
- **Upstream #1494** was closed by a commit message that only quoted it; it was reopened after the merge.
- **AI-assisted development.** About 45% of the merged commits were written with AI coding agents
  (Claude Code, Cursor) and merged by the maintainer. The docs, skills and validation loop are
  built for that way of working.
- `.mailmap` folds the maintainer's identities into one author for `git log`, `shortlog` and blame.
