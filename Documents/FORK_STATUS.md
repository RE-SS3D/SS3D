# Fork status

This fork has diverged meaningfully from `RE-SS3D/SS3D` upstream. It's an active
experiment in redesigning core gameplay systems and development foundation at a
faster pace than upstream's current review capacity supports.

**What's diverged:**
- Documentation: new design/architecture doc structure in `Documents/design/` and
  `Documents/architecture/`, independent of the upstream GitBook.
- Repo presentation: `README.md` and `Documents/CONTRIBUTING.md` describe this fork,
  not upstream; GitHub issue templates and PR checklist point at `Documents/`.
- GitHub automation: upstream milestone/roadmap/release workflows disabled
  (`workflow_dispatch` only); Discord webhook notifications removed from CI.
- Gameplay design: health, combat, stamina, armor, comms, main HUD, area, hacking
  interface — specs in `Documents/design/`.
- [Update as branches merge: URP migration, milestone redesign, etc.]

**Standing offer:** anything here can be proposed upstream on request — open an
issue/discussion and I'll put together a PR-shaped version of whatever's relevant.

**Last updated:** 2026-07-09
