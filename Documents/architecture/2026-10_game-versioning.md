> Implements: Documents/architecture/2026-07_ci-develop-release-pipeline.md (versioned releases)
> Touches systems: networking-session, rounds-lobby, persistence
> Status: shipped

# Game versioning (Oct 2026)

## Goal

Give the fork its own version line, worked out from git so no human edits a version field, with
bump rules precise enough that CI and agents apply them. The owner's only manual call is 1.0
(any MAJOR bump).

Upstream RE:SS3D tags (`0.0.7.6`, `0.3.95o`, …) stay in the repo but are ignored: fork tags are
`vMAJOR.MINOR.PATCH` and every lookup matches `v[0-9]*`. The line starts at `v0.1.0`, above
upstream's last `0.0.7.6`.

## Version strings

| Build | Version | Example |
|---|---|---|
| On a release tag | the tag | `0.1.0` |
| N commits after a tag | next patch, dev pre-release + commit | `0.1.1-dev.12+5da58b9` |
| Uncommitted changes | `.dirty` appended to the metadata | `0.1.1-dev.12+5da58b9.dirty` |
| No `v*` tag yet | pre-release of the first release, counts all commits | `0.1.0-dev.1764+5da58b9` |

Produced by `GameVersion` (C#, editor + EditMode-tested) and mirrored by
`Tools/versioning/next_version.py describe` (CI). Keep the two in sync.

## When a release bumps what

A release is a manual `develop-release.yml` dispatch (nightlies stay on the floating
`develop-nightly` tag and are never numbered). The prepare job runs
`Tools/versioning/next_version.py next`, which checks everything since the last `v*` tag; the
strongest signal wins:

| Bump | Signal | Detected from |
|---|---|---|
| MINOR | Save schema changed | `PersistenceEnvelope.CurrentSchemaVersion` increased |
| MINOR | Network protocol changed | `ProtocolVersion.Current` increased |
| MINOR | A milestone slice shipped | Slice row status in `Documents/milestones/*.md` became `shipped` |
| MINOR | A milestone gate reached | Milestone header `> Status:` became `shipped` |
| MINOR | Agent/owner judgement | Commit trailer `Version-Bump: minor` |
| MAJOR | Owner only (1.0 and beyond) | Commit trailer `Version-Bump: major`, or dispatch `version_bump=major` |
| PATCH | Anything else | default |

The dispatch input `version_bump` (`auto` default) overrides the computed level. A dispatch with
no commits since the last tag fails rather than re-releasing. `tag_suffix` still gives an
unversioned `develop-<suffix>` test cut. Numbered releases are stable GitHub releases even while
MAJOR is 0 (1.0 means a complete game, far off); nightlies and `develop-*` cuts are prereleases.

### Rules for agents

- **Bump `ProtocolVersion.Current`** when a build can no longer talk to the previous one: added,
  removed or renamed `NetworkBehaviour`s / SyncVars / SyncTypes on spawned objects, changed RPC or
  broadcast signatures, or a FishNet upgrade. Mismatched clients are rejected with a message
  naming both versions.
- **Bump `PersistenceEnvelope.CurrentSchemaVersion`** when saved station templates / server meta
  written by the previous build no longer load as-is, or when a save from the new build would lose
  data in an older one (a new saved field, such as schema 3's object setup). An older build loads a
  newer save but warns that some settings may be missing. Restamp shipped maps with
  `Tools/map_import/asset_ids.py` (it writes the current version).
- **Add `Version-Bump: minor`** as a commit trailer (last paragraph of the message) when a change
  completes a player-visible feature end to end without tripping a signal above — e.g. a whole new
  role, gamemode or system becomes playable. Ordinary fixes, polish and refactors get no trailer.
- **Never add `Version-Bump: major`.** That is the owner's call.

## Shipped

- `Assets/Scripts/SS3D/Systems/Rounds/GameVersion.cs` — formats versions from `git describe`.
- `Assets/Scripts/SS3D/Editor/BuildInfoGenerator.cs` — writes `version` into the gitignored
  `BuildInfo.json` (editor load + every player build) and stamps `PlayerSettings.bundleVersion`
  for the build only (restored afterwards, so `ProjectSettings.asset` never churns). CI passes
  `-ss3dVersion X.Y.Z` because the release tag does not exist yet when the player is built.
- `BuildInfo.Version` / `BuildInfo.Label` — lobby Server Info and changelog show the version.
- `Assets/Scripts/SS3D/Networking/ProtocolVersion.cs` + `ProtocolVersionAuthenticator.cs` — FishNet
  authenticator installed at runtime by `NetworkSessionSubSystem.StartNetworkSession`; rejects a
  client whose protocol differs. `ServerConnectionView` and `ClientConnectionRecovery` show the reason.
- `.github/workflows/develop-release.yml` — prepare job computes the version (full-history
  checkout), builds receive it, release tags `vX.Y.Z` at the built commit.
- `ProjectSettings.asset` `bundleVersion` is a placeholder (`0.0.0-editor`); `Application.version`
  is only meaningful in player builds. Code that needs the version in the editor uses `BuildInfo.Version`.

## Not done

- Save files record `gameVersion` but nothing reads it; compatibility checks key on
  `schemaVersion` only, by design.
- The multiplayer smoke and EditMode workflows check out shallow history, so their builds report
  `0.1.0-dev.1+<sha>` (or `X.Y.Z-dev.1`). Harmless; deepen the checkout if it ever matters.
