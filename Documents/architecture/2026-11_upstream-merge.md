> Implements: infrastructure — moving development from the `henkhooft/SS3D` fork into `RE-SS3D/SS3D`, decided by the owner (now upstream maintainer) on 2026-11-15/16
> Touches systems: networking-session (CI, releases, updaters), ui-shell-main-menu (release banner), application (bug reports), docs governance (`AGENTS.md`, `CLAUDE.md`, `Documents/SKILL.md`, `Documents/FORK_STATUS.md`, milestones)
> Status: in-progress

**Progress:** Phase 0 done on `upstream-prep` (#1491 recorded, repository setting, `.mailmap`, this doc). Phases 1–5 open.

# Upstream merge (Nov 2026)

Fork `develop` goes into `RE-SS3D/SS3D` `develop` as one PR, replacing upstream #1525 (a blunt copy
of fork `develop`). Afterwards upstream is the only home: the fork becomes the owner's personal PR
fork. The work here is everything a fork says about itself that upstream must not: links, release
and updater targets, CI that trusts its contributors, presentation, and the "this fork" framing.

All translation work lands on the long-lived `upstream-prep` branch, which merges `develop` in
regularly. `develop` keeps shipping to fork players until merge day.

## Starting state (2026-11-15, `953fcbbd`)

- Fork `develop` is 1,482 commits ahead of `upstream/develop` and 1 behind (#1491 Port Vision
  System, whose code the fork already carries, ported to URP). The diff is 19.6k files, +1.76M / −340k lines.
- Upstream's other open PRs are out of date and will be closed (agreed with the previous maintainer).
- 27 files name `henkhooft`: the release banner (`ReleaseCheck.cs`), the bug report link
  (`BugReportBundle.cs`), both updaters, the bootstrap launchers, issue templates, `README.md`,
  `Documents/CONTRIBUTING.md`, `Documents/CODE_OF_CONDUCT.md`, `Builds/TESTERS.md`, and fork PR links in milestones.
- About 20 docs and the workflows name TomNAS, the owner's self-hosted runner and server host.
- CI: `editmodetestrunner.yml` runs on `pull_request` and picks the self-hosted runner for PRs by default.
  Secrets `UNITY_LICENSE`, `UNITY_SERIAL`, `UNITY_EMAIL`, `UNITY_PASSWORD`.
- The fork deleted upstream's `.github/FUNDING.yml`, Feature/Other issue templates, milestone/project/Discord
  workflows, `main.yml`, `activation.yml`, `Documents/UnityLicense.ulf` and five `Start_SS3D_*.bat`.
- History: 329 commits reference `#N`, which upstream reads as its own issues. 12 use closing keywords; 11 hit
  upstream issues that are already closed, but `bdd12c395` quotes "closes #1494", and upstream #1494 is open
  and not fixed. 648 commits are co-authored by Claude. 52 system-map `Verified:` stamps cite `develop` commits.
- Release tags: the fork's `v0.1.0`–`v0.17.0` all sit on `develop`. Upstream has only unprefixed tags
  (`0.3.95o`), which `ReleaseCheck`, the updaters and `Tools/versioning/next_version.py` ignore.
- 21 binary `.fbx` models embed `/home/rutger/...` export paths; `Testing/ui/README.md` says "Rutger's machine".

## Decisions

Settled with the owner in a grilling session, 2026-11-15/16.

1. **Upstream becomes the only home.** The owner merges upstream. After merge day the fork's `develop` resets to
   upstream's, and fork releases, issues and roadmap stop.
2. **Merge commit, full history.** "Create a merge commit", never squash, so blame and the `Verified:` stamps
   keep resolving. Reopen upstream #1494 right after merging.
3. **#1491 is recorded with `git merge -s ours`** after checking each of its changes against the fork (see the
   merge commit message on `upstream-prep`).
4. **`upstream-prep` absorbs `develop` regularly.** Translations stay a small set of labelled commits on top.
   `develop` freezes only for merge day.
5. **The repository is named once per consumer.** `SS3D.Utils.ProjectRepository`, both updaters and both
   launchers each hold one copy; `Tools/updater/set_project_repo.py` switches them all and
   `test_project_repo.py` fails when they disagree ([Tools/updater/README.md](../../Tools/updater/README.md)).
6. **Agent tooling ships.** `AGENTS.md`, `CLAUDE.md`, `.claude/` and `.cursor/` move upstream; the PR
   description states the AI-assisted share plainly.
7. **`Documents/` is the canonical documentation.** Milestones and the generated `ROADMAP.md` replace upstream's
   GitHub-milestone and project-board automation, which stays deleted. GitBook stays background reading only.
8. **PR code never runs on the self-hosted runner.** PR jobs run GitHub-hosted. The self-hosted runner takes only
   pushes to `develop`, schedules, manual dispatch and maintainer-labelled PRs (`test:*`). Upstream requires
   approval before outside contributors' workflows run.
9. **Unity secrets:** a project-owned Unity account if one exists, otherwise the owner's re-added with a note on
   who owns them. Fork PRs get no secrets, so their Unity jobs skip with a clear "maintainer runs this" status.
10. **Releases keep the fork's numbering.** v0.17.1 ships from the fork after the merge and points the updaters
    and bug reports at upstream; v0.18.0 is then the first upstream release. Upstream's old builds stay as they
    are. MAJOR bumps are a team decision. ss3d.space links to upstream's releases page; the central team edits it.
11. **`FORK_STATUS.md` becomes the reviewer guide** ("What the 2026 merge brought": Shipped-gameplay table,
    engine and CI changes), linked from the PR and archived after the merge. All "this fork" wording is rewritten
    to describe the project.
12. **Docs name roles, never hosts.** "The self-hosted runner", "the project test server"; no TomNAS anywhere.
    One ops note lists the roles, who runs each, and what breaks without it.
13. **Fork issues:** the owner triages them, closes the irrelevant ones and recreates the rest upstream by hand.
    Fork PR links in docs stay as history. Issues and Discussions are then disabled on the fork.
14. **Contributing steers toward agent-assisted work.** Root `CONTRIBUTING.md` (where GitHub finds it) names
    Claude Code and Cursor, with `AGENTS.md` as the entry point, and walks the loop with the skill for each step.
    The PR template keeps upstream's networking checklist and adds "docs synced via `update-system-docs`".
    Hand-written PRs meet the same bar.
15. **Repo pages:** README keeps the fork's play-first layout plus upstream's Community and Donate sections and
    badges; restore `FUNDING.yml`; keep the YAML issue forms (ideas go to Discussions); Code of Conduct contact
    becomes upstream's; drop "Not an official RE:SS3D build" from `Builds/README.txt`; GitBook mentions in code
    comments point at `Documents/`; the deleted `.bat` files and license file stay deleted.
16. **One identity: BroodjeKipcorn `<henkhooft@gmail.com>`.** `.mailmap` folds the other names and addresses
    without rewriting history; the `.fbx` export paths get a same-length placeholder.
17. **No review window.** The merge is already greenlit; merge once CI is green.

## Phases

### Phase 0: prep branch — done

- [x] `upstream-prep` from `develop`; #1491 merged with `-s ours` (decision 3)
- [x] `ProjectRepository` + `set_project_repo.py` + `test_project_repo.py` in `updater-tests.yml` (decision 5)
- [x] `.mailmap` (decision 16)
- [x] This effort doc

### Phase 1: project framing and docs

- [ ] `FORK_STATUS.md` → reviewer guide (decision 11); retarget its citations in `AGENTS.md`, `CLAUDE.md`, `Documents/SKILL.md`
- [ ] Rewrite "this fork" framing: `AGENTS.md`, `CLAUDE.md`, `Documents/SKILL.md` title and layer table, milestones, `Builds/README.txt`, `Builds/TESTERS.md`
- [ ] Role wording for every TomNAS mention, including workflow comments, the `runner` input values and `Testing/ui/README.md`; add the ops note (decision 12)
- [ ] GitBook mentions in code comments (`Actor.cs`, `NetworkActor.cs`, `Assets.cs`) → `Documents/`
- [ ] `GasDefaults.cs:41` "henkhooft tuning values" → describe the source of the values

### Phase 2: repository pages

- [ ] Root `CONTRIBUTING.md`, agent-assisted (decision 14); remove `Documents/CONTRIBUTING.md` and retarget links
- [ ] `.github/PULL_REQUEST_TEMPLATE.md`: networking checklist, docs-sync and agent items
- [ ] `README.md` (decision 15); keep the generated roadmap block working with `Tools/generate_roadmap.py`
- [ ] Restore `.github/FUNDING.yml` from upstream; issue template and `Config.yml` links; `Documents/CODE_OF_CONDUCT.md` contact

### Phase 3: CI for a public repository

- [ ] PR events never pick the self-hosted runner unless a maintainer label asks for it (decision 8)
- [ ] Unity jobs on fork PRs skip with a clear status when secrets are absent (decision 9)
- [ ] `docs-check.yml` fails on `henkhooft/SS3D` outside an allowlist (`Documents/archive/`, fork PR links in milestones, `ServerInfoTests.cs` inputs, `.mailmap`)
- [ ] Sync [2026-07_ci-develop-release-pipeline](2026-07_ci-develop-release-pipeline.md) and [2026-07_multiplayer-testing-self-hosted-ci](2026-07_multiplayer-testing-self-hosted-ci.md)

### Phase 4: the switch commit

- [ ] Confirm the casing GitHub returns in upstream's `browser_download_url` (see Pitfalls)
- [ ] `python3 Tools/updater/set_project_repo.py RE-SS3D/SS3D`, plus the remaining links phases 1–2 left pointing at the fork
- [ ] `.fbx` export paths: same-length placeholder in all 21 files, reimport in Unity, confirm the models still load

### Phase 5: checks before the PR opens

- [ ] `Testing/ui/validate.sh --steps compile,editmode`; `updater-tests.yml` green (including the Windows PowerShell job)
- [ ] The Phase 3 stray-link check passes
- [ ] A `develop-<suffix>` prerelease cut from `upstream-prep` on the fork builds, and its updater finds nothing newer on upstream

## Merge day

1. Announce on Discord and freeze fork `develop`. Final `develop` → `upstream-prep` merge; rerun Phase 5.
2. Prepare upstream: create the labels (`nightly-failure`, `weekly-failure`, `editmode-nightly-failure`,
   `test:multiplayer`, `map-authoring`, `visual`, `blocker`); set the secrets (decision 9); require approval for
   outside contributors; register the self-hosted runner for trusted events only.
3. Push the fork's `v*` tags to upstream (see Pitfalls). Open the PR from `upstream-prep`, linking the reviewer
   guide and stating the AI share; close #1525 in favour of it; close the outdated upstream PRs with a short note.
4. Merge with **Create a merge commit**. Reopen #1494.
5. Branch protection on upstream `develop` with the EditMode run as a required check.
6. Fork: reset `develop` to upstream's (tag the old head `pre-upstream-merge` first) and cut **v0.17.1** from it
   on the fork with `version_bump: patch`. Its updater and bug report link already point upstream.
7. Fork: disable scheduled workflows and Actions, deregister the runner, disable Issues and Discussions.
8. Upstream: cut **v0.18.0**. The central team points ss3d.space at upstream's releases page.
9. The owner recreates the relevant fork issues upstream. Archive this effort and the reviewer guide.

## Pitfalls

- **The first upstream release numbers itself v0.1.0** unless the fork's `v*` tags are on upstream:
  `next_version.py` and `BuildInfoGenerator` read local `v*` tags. Push them before cutting v0.18.0.
- **`version_bump: auto` may skip v0.17.1.** Any `Version-Bump: minor` trailer or newly shipped milestone
  slice since v0.17.0 makes `next_version.py` pick v0.18.0. Dispatch the redirect release with `patch`.
- **Slug casing is checked.** The updaters compare `browser_download_url` case-sensitively and refuse to install
  on a mismatch. Use the owner/name casing GitHub puts in upstream's release URLs, not a lowercase clone URL.
- **A merge with history closes upstream #1494** via the quoted "closes #1494" in `bdd12c395`. Reopen it.
- **`updater-e2e.yml` needs two upstream stable releases** (previous → latest); before v0.19.0 it has only one.
- **GitHub's contributor graph ignores `.mailmap`;** it only affects `git log`, `shortlog` and blame.

## Open questions

- Which Unity account do upstream's existing `UNITY_EMAIL` / `UNITY_PASSWORD` secrets belong to? Decides decision 9.
