> Implements: infrastructure — moving development from the `henkhooft/SS3D` fork into `RE-SS3D/SS3D`, decided by the owner (now upstream maintainer) on 2026-11-15/16
> Touches systems: networking-session (CI, releases, updaters), ui-shell-main-menu (release banner), application (bug reports), docs governance (`AGENTS.md`, `CLAUDE.md`, `Documents/SKILL.md`, `Documents/MERGE_GUIDE.md`, milestones)
> Status: in-progress

**Progress:** Phases 0–5 done on `upstream-prep`; `develop` frozen since 2026-11-16. Next: merge day.

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
- About 20 docs and the workflows named the owner's self-hosted runner and server host by its machine name;
  the workflows also hardcode the runner user's home in stash paths.
- CI: `editmodetestrunner.yml` runs on `pull_request` and picks the self-hosted runner for PRs by default.
  Secrets `UNITY_LICENSE`, `UNITY_SERIAL`, `UNITY_EMAIL`, `UNITY_PASSWORD`.
- The fork deleted upstream's `.github/FUNDING.yml`, Feature/Other issue templates, milestone/project/Discord
  workflows, `main.yml`, `activation.yml`, `Documents/UnityLicense.ulf` and five `Start_SS3D_*.bat`.
- History: 329 commits reference `#N`, which upstream reads as its own issues. 12 use closing keywords; 11 hit
  upstream issues that are already closed, but `bdd12c395` quotes "closes #1494", and upstream #1494 is open
  and not fixed. 648 commits are co-authored by Claude. 52 system-map `Verified:` stamps cite `develop` commits.
- Release tags: the fork's `v0.1.0`–`v0.17.0` all sit on `develop`. Upstream has only unprefixed tags
  (`0.3.95o`), which `ReleaseCheck`, the updaters and `Tools/versioning/next_version.py` ignore.
- 21 binary `.fbx` models embed the owner's home directory in export paths; `Testing/ui/README.md` named the owner's machine.

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
8. **Fork PR code never runs on the self-hosted runner.** The runner takes only trusted code: schedules, manual
   dispatch (including a maintainer's `pr: <number>` run of a fork PR they have read), and PRs from branches of
   this repository, which only people with write access can push. Upstream requires approval before outside
   contributors' workflows run. *Refined in Phase 3:* the grilling said "PR jobs run GitHub-hosted" with labelled
   PRs as the exception. Same-repo PRs stay on the runner because their authors can already dispatch any
   workflow, and a label alone is not a safe gate for fork PRs (it survives the fork's next push).
9. **Unity secrets:** a project-owned Unity account if one exists, otherwise the owner's re-added with a note on
   who owns them. Fork PRs get no secrets, so their Unity jobs skip with a clear "maintainer runs this" status.
10. **Releases keep the fork's numbering.** v0.17.1 ships from the fork after the merge and points the updaters
    and bug reports at upstream; v0.18.0 is then the first upstream release. Upstream's old builds stay as they
    are. MAJOR bumps are a team decision. ss3d.space links to upstream's releases page; the central team edits it.
11. **`FORK_STATUS.md` became the reviewer guide** [MERGE_GUIDE.md](../MERGE_GUIDE.md) ("What the 2026 merge
    brought": Shipped-gameplay table, engine and CI changes), linked from the PR and archived after the merge. All "this fork" wording is rewritten
    to describe the project.
12. **Docs name roles, never hosts.** "The self-hosted runner", "the project test server"; no machine names anywhere.
    [2026-11_project-infrastructure](2026-11_project-infrastructure.md) lists the roles, who runs each, and what breaks without it.
13. **Fork issues:** the owner triages them, closes the irrelevant ones and recreates the rest upstream by hand.
    Fork PR links in docs stay as history. Issues and Discussions are then disabled on the fork.
14. **Contributing steers toward agent-assisted work.** Root `CONTRIBUTING.md` (where GitHub finds it) names
    Claude Code and Cursor, with `AGENTS.md` as the entry point, and walks the loop with the skill for each step.
    The PR template keeps upstream's networking checklist and adds "docs synced via `update-system-docs`".
    Hand-written PRs meet the same bar.
15. **Repo pages:** README stays upstream's (info, downloads to the latest release, donate, community,
    contributing) with a generated roadmap section, no Trello, and contributing pointed at `CONTRIBUTING.md` and `Documents/`; restore `FUNDING.yml`; keep the YAML issue forms (ideas go to Discussions); Code of Conduct contact
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

### Phase 1: project framing and docs — done

- [x] `FORK_STATUS.md` → [MERGE_GUIDE.md](../MERGE_GUIDE.md); citations retargeted (`AGENTS.md`, `CLAUDE.md`, `Documents/SKILL.md`, `.cursor/skills/*`, maps, `TECH_DEBT.md`, `Tools/generate_roadmap.py`). The vendored-FishNet rule moved to [networking-session](systems/networking-session.md) § Extension points
- [x] "This fork" framing rewritten in agent docs, efforts, maps, plans, code comments, rulesets, `Builds/README.txt`; "fork deviation" is now "design deviation" (TECH_DEBT §5 anchor updated)
- [x] Bare fork `#N` references in active docs qualified as `henkhooft/SS3D#N`; `CLAUDE.md` tells agents how to read `#N` in pre-merge commits
- [x] Role wording for every machine-name mention, including the workflows' `runner` input (`self-hosted` / `github`); [2026-11_project-infrastructure](2026-11_project-infrastructure.md) added (decision 12)
- [x] GitBook mentions in code comments → `Documents/`; `GasDefaults.cs` wording
- [x] MAJOR bumps described as a team decision (`AGENTS.md`, [2026-10_game-versioning](2026-10_game-versioning.md))
- Left for the owner (design docs are owner-only): "fork" wording in `design/player-accounts.md` (lines 17, 44, 50) and `design/shuttles.md` (line 33)

### Phase 2: repository pages — done

- [x] Root [CONTRIBUTING.md](../../CONTRIBUTING.md), agent-assisted (decision 14): agent setup table, the seven-step loop with the skill for each step, what a mergeable PR looks like. `Documents/CONTRIBUTING.md` removed. Its "CI on your PR" paragraph describes the Phase 3 behaviour for fork PRs
- [x] `.github/PULL_REQUEST_TEMPLATE.md`: validation, design-deviation, `update-system-docs` and versioning checklist; AI-assistance section; upstream's networking checklist kept
- [x] `README.md` (decision 15): upstream's README, plus a ROADMAP section holding the generated block (now without its own heading); Trello and the milestones board removed; contributing points at `CONTRIBUTING.md` and `Documents/`; the dead `archive/master.zip` link points at `develop`
- [x] `.github/FUNDING.yml` restored; issue form, `Config.yml` and `Builds/TESTERS.md` links point upstream; `Documents/CODE_OF_CONDUCT.md` is upstream's text again (the project email as contact)
- [x] "Never add `Version-Bump: major`" now says it's a team decision

### Phase 3: CI for a public repository

- [x] Fork PRs never reach the self-hosted runner (decision 8): `editmodetestrunner.yml` and `multiplayer-smoke-test.yml` gate PR runs on `head.repo.full_name == github.repository`; both take a `pr` dispatch input that checks out `refs/pull/<n>/head`
- [x] Fork PRs get a notice job (EditMode always, smoke when labelled) naming the maintainer's dispatch command, instead of a Unity job that fails at license activation (decision 9)
- [x] `docs-check.yml` runs on every PR and push to `develop` (path filter dropped; both checks take seconds) and adds `Tools/ci/check_fork_links.py`: fails on `henkhooft/SS3D` outside the allowlist; historical `henkhooft/SS3D#N` and `/pull/N` / `/issues/N` links pass anywhere; the repository copies are left to `test_project_repo.py`
- [x] Stash and UPM cache paths come from the runner user's `$HOME` (`SS3D_UNITY_LIBRARY_STASH_NAME`; `persistent-upm-cache` default)
- [x] Synced [2026-07_ci-develop-release-pipeline](2026-07_ci-develop-release-pipeline.md), [2026-07_multiplayer-testing-self-hosted-ci](2026-07_multiplayer-testing-self-hosted-ci.md) (new Pitfall), [networking-session-testing](systems/networking-session-testing.md), [project infrastructure](2026-11_project-infrastructure.md), `CONTRIBUTING.md` and the merge guide
- Not run yet: these workflow changes are only checked by YAML parsing and by reading the expressions. The first PR from `upstream-prep` exercises the same-repo path; a fork PR, the notice jobs and the `pr` dispatch are checked on merge day (step 2)

### Phase 4: the switch commit — done

- [x] Upstream's `browser_download_url` is cased `RE-SS3D/SS3D` (checked 2026-11-16 against its releases API)
- [x] `python3 Tools/updater/set_project_repo.py RE-SS3D/SS3D`; no other fork links were left after phases 1–2
- [x] `.fbx` export paths: `rutger` → `ss3dev` (same length) in all 21 files, including two embedded `/tmp/claude-…` scratch paths; force-reimported in the editor with unchanged mesh, vertex and material counts and no new import errors

### Phase 5: checks before the PR opens

- [x] `Testing/ui/validate.sh --steps compile,editmode` passes after the switch (5428/5429, 2026-11-16); the updater tests pass locally, the PowerShell one under `pwsh` (CI uses Windows PowerShell 5.1)
- [x] The Phase 3 stray-link check passes
- [x] `develop-upstream-prep` prerelease (fork run 43922814045, 2026-11-16): Windows build and both updater jobs green. In the zip, the release check, bug report link and bundled updater name `RE-SS3D/SS3D`; `Update_SS3D.ps1 -CheckOnly` reports no stable numbered release upstream. The build info's `repository` still reads the fork because it comes from the build checkout's `origin`; upstream builds write their own

## Merge day

1. Announce on Discord and freeze fork `develop`. Final `develop` → `upstream-prep` merge; rerun Phase 5.
2. Prepare upstream: create the labels (`nightly-failure`, `weekly-failure`, `editmode-nightly-failure`,
   `test:multiplayer`, `map-authoring`, `visual`, `blocker`); set the secrets (decision 9); require approval for
   outside contributors; register the self-hosted runner (the workflows keep fork PRs off it); open a test PR from a
   fork and confirm the EditMode notice job, then dispatch `-f pr=<number>`; give the nightly release
   dispatcher ([2026-11_project-infrastructure](2026-11_project-infrastructure.md)) a token for upstream.
3. Push the fork's `v*` tags to upstream (see Pitfalls). Open the PR from `upstream-prep`, linking the reviewer
   guide and stating the AI share; close #1525 in favour of it; close the outdated upstream PRs with a short note.
4. Merge with **Create a merge commit**. Reopen #1494.
5. Branch protection on upstream `develop` with the EditMode run as a required check.
6. Fork: reset `develop` to upstream's (tag the old head `pre-upstream-merge` first) and cut **v0.17.1** from it
   on the fork with `version_bump: patch`. Its updater and bug report link already point upstream.
7. Fork: disable scheduled workflows and Actions, deregister the runner, disable Issues and Discussions.
8. Upstream: cut **v0.18.0**. The central team points ss3d.space at upstream's releases page.
9. The owner recreates the relevant fork issues upstream. Archive this effort and the reviewer guide; point
   `Tools/generate_roadmap.py`'s "what already works" link at the [coverage table](INDEX.md) and regenerate.

## Pitfalls

- **The first upstream release numbers itself v0.1.0** unless the fork's `v*` tags are on upstream:
  `next_version.py` and `BuildInfoGenerator` read local `v*` tags. Push them before cutting v0.18.0.
- **`version_bump: auto` may skip v0.17.1.** Any `Version-Bump: minor` trailer or newly shipped milestone
  slice since v0.17.0 makes `next_version.py` pick v0.18.0. Dispatch the redirect release with `patch`.
- **Slug casing is checked.** The updaters compare `browser_download_url` case-sensitively and refuse to install
  on a mismatch. Use the owner/name casing GitHub puts in upstream's release URLs, not a lowercase clone URL.
- **A merge with history closes upstream #1494** via the quoted "closes #1494" in `bdd12c395`. Reopen it.
- **`updater-e2e.yml` needs two upstream stable releases** (previous → latest); before v0.19.0 it has only one.
- **`develop` merges that touch `Documents/FORK_STATUS.md` conflict** (deleted on `upstream-prep`). Carry the change into
  [MERGE_GUIDE.md](../MERGE_GUIDE.md) if it matters to reviewers, then keep the deletion.
- **`develop` merges that touch `README.md` or the roadmap conflict**: the README is upstream's on `upstream-prep`.
  Keep `upstream-prep`'s README, then run `python3 Tools/generate_roadmap.py` to refresh its roadmap block.
- **GitHub's contributor graph ignores `.mailmap`;** it only affects `git log`, `shortlog` and blame.

## Open questions

- Which Unity account do upstream's existing `UNITY_EMAIL` / `UNITY_PASSWORD` secrets belong to? Decides decision 9.
