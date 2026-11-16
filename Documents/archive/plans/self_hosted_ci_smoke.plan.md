---
name: Self-hosted CI smoke
overview: "Phase 0–2 of multiplayer-testing-self-hosted-ci: TomNAS runner + warm Library stash, then server-kill and light-sync harness coverage."
todos:
  - id: phase0-runner-ops
    content: "Via SSH: register runner, Library stash, pull Unity image"
    status: completed
  - id: phase0-smoke-workflow
    content: "Update multiplayer-smoke-test.yml for self-hosted + Library stash + nightly"
    status: completed
  - id: phase0-warm-proof
    content: "workflow_dispatch smoke on TomNAS; confirm warm second run"
    status: pending
  - id: phase1-server-kill
    content: "Harness SIGKILL + DSL + server-kill scenarios"
    status: pending
  - id: phase2-light-sync
    content: "lightdebug/lightclientstatus + light-sync scenarios"
    status: pending
  - id: docs-skills
    content: "update-system-docs + smoke skills when Phase 0–2 ship"
    status: pending
isProject: false
---

# Self-hosted CI + multiplayer harness (Phases 0–2)

Implements [Documents/architecture/2026-07_multiplayer-testing-self-hosted-ci.md](../../architecture/2026-07_multiplayer-testing-self-hosted-ci.md).

## Phase 0 status (2026-07-24)

- Runner `TomNAS-unity` registered and **online** (user systemd unit + linger).
- Docker group + image `unityci/editor:ubuntu-6000.3.16f1-linux-il2cpp-3` pulled.
- Library stash path: `/home/cu6e/.cache/ss3d/Library`.
- Workflow YAML updated for self-hosted + stash `mv` + nightly.
- **Next:** push + `workflow_dispatch`; prove cold then warm smoke.

## Where work happens

| Machine | Role |
|---------|------|
| Dev machine | Edit workflows/harness/C#; push; `workflow_dispatch` |
| TomNAS (`ssh tomnas`) | Runner + Docker + Library stash; executes self-hosted jobs |

See the Cursor plan / architecture effort for Phases 1–2 detail.
