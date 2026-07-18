---
name: triage-multiplayer-smoke
description: >-
  Triages multiplayer smoke-test failures from Testing/multiplayer/.runs using
  tools/triage_run.sh. Use when a smoke run fails, the user pastes a run id or
  .runs path, or asks to analyze/read multiplayer harness logs, unity.log, or
  LogServer/LogClient JSON.
---

# Triage multiplayer smoke logs

Do **not** `cat` or fully read `unity.log` — client logs can be hundreds of KB of
Blitter/icon spam. Always start with the triage script.

To **start** a run after an Editor build, use **run-multiplayer-smoke**.
For **build → smoke → triage → fix**, use **multiplayer-smoke-e2e**.

## Checklist

```
- [ ] Run triage_run.sh on the run id / path / latest
- [ ] Lead with ScriptFailed payload + last Test signal
- [ ] Treat known_noise buckets as non-root-cause unless they are the only fail
- [ ] Dig into JSON / a small unity.log window only if needed
- [ ] If new benign noise appears, fix source preferred; else update patterns + pitfalls
```

## Step 1: Run the script

```bash
./Testing/multiplayer/tools/triage_run.sh <run-id|path|latest>
```

Runs live under `Testing/multiplayer/.runs/<id>/` (`server/`, `client-N/`).
CI artifacts unpack to the same layout.

## Step 2: Read the summary in this order

1. **`script_failed`** — automation instruction + exception text (primary cause).
2. **`signals`** — how far the scenario got (`ServerReady` → `ClientConnected` → …).
3. **`json_error_fatal`** — Serilog Error/Fatal (often empty when the fail is Unity-only).
4. **`unity_exceptions` → `real`** — uncaught exceptions the harness greps for.
5. **`known_noise`** — allowlisted headless spam (see `tools/known_unity_noise.patterns`).
   Counts only; not the failure unless `real` is empty and harness still exited 1.

## Step 3: Dig only if needed

- JSON: `jq` for the failing `@t` / `signal` / `payload` in `Logs/Log*.json`.
- `unity.log`: `rg -n` the ScriptFailed text or exception type, then read ~40 lines around
  that line — never the whole file.
- Pitfalls: [networking-session.md](../../../Documents/architecture/systems/networking-session.md)
  (PlayerSubSystem wait, batchmode icons), effort doc
  [2026-07_multiplayer-test-harness.md](../../../Documents/architecture/2026-07_multiplayer-test-harness.md).

## Known noise policy

- Prefer **fixing the source** (e.g. skip icon gen in batchmode) over growing the allowlist.
- Add a line to `Testing/multiplayer/tools/known_unity_noise.patterns` only after confirming
  the message is benign under `-batchmode -nographics` / dedicated server.
- `run_smoketest.sh` does **not** yet use this allowlist — triage reports noise separately so
  agents do not chase Blitter stacks when `ScriptFailed` already explains the fail.
- Record new silent headless pitfalls on the relevant system map when you hit them.

## Do not

- Dump entire `unity.log` or staged `SS3D_Data/` into chat.
- Treat triage `total_size` as unique disk use — staging hardlinks player binaries; apparent
  size stays large while free-space cost across runs stays small.
- Edit `Documents/design/*`.
