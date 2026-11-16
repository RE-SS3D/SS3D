<!-- Notes inside these arrows are for you and can be deleted. Remove optional sections you don't use. -->
<!-- Add "[WIP]" to the start of the title if this isn't ready for review. The title becomes the squash commit message. -->

# Summary

<!-- What does this change do for players or contributors, and why? Link the milestone slice, design doc section, effort or plan it implements. -->

#### Checklist
- [ ] One feature or fix: no unrelated changes, stray Unity reserialization or trash files.
- [ ] `Testing/ui/validate.sh --steps compile,editmode` passes, plus the focused steps for what I touched (`visual` for visual changes).
- [ ] Follows its design doc, or the deviation is recorded in the system map or effort (say where below).
- [ ] Docs synced with `update-system-docs`: system maps (`Verified` stamp, new Pitfalls), INDEX, plan or effort, milestone slice.
- [ ] Save or protocol version bumped if old saves or clients break; `Version-Bump: minor` trailer if this completes a player-visible feature.

#### AI assistance
<!-- Name the agent(s) you used: Claude Code, Cursor, another, or none. -->
- Agent used:
- [ ] I have read and understood every change in this PR.

<!-- optional -->
# Pictures/Videos

<!-- Screenshots or clips help reviewers, and may end up in a devblog. -->

# Testing

<!-- How can a reviewer check this? Which validate.sh steps, scenarios or manual steps did you run? -->

<!-- The networking checklist is for anything networked. Remove it otherwise. -->
#### Networking checklist
<!-- (ex. The host can open a door.) -->
- [ ] Works from host in host mode.
<!-- (ex. The server can open a door, even if not interacting directly.) -->
- [ ] Works from server in server mode.
<!-- (ex. The client tries to open a door, and the server opens it. The client and the server have to see the same thing. This would fail if only the client sees this interaction.) -->
- [ ] Works on server in client mode.
<!-- (ex. The client tries to open a door, the server opens it, and another client sees that interaction.) -->
- [ ] Works and is synchronized across different clients.
<!-- (ex. The client opens a door, the server opens it. The client closes the game, reopens it, and rejoins the server. The client sees the door open.) -->
- [ ] Is persistent.

<!-- optional, but encouraged for technical context -->
# Changes

<!-- Major asset, script or scene changes and why. Anything not working correctly, in this change or elsewhere. -->

<!-- optional -->
# Related issues/PRs

<!-- If this PR closes an issue, write "Closes #123". -->
