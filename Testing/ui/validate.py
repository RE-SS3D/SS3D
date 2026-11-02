"""One-command validation for UI/session work. Use Testing/ui/validate.sh or compile.sh.

Steps: preflight → compile → editmode → playmode → visual → lifecycle → sandbox (--full) → build (client and dedicated
server, each if stale) → remote → smoke (Testing/multiplayer scenarios against both builds).
--quick skips playmode and stops after lifecycle. PlayMode failures in KnownIssueReproduction fixtures are reported
separately against Testing/ui/playmode-known-issues.txt (expected failures), so known bugs neither
fail the loop nor hide new failures. Every step has an expected duration and a timeout: past the
expected duration the dialog watchdog runs every 30 s (a modal dialog fails the step at once);
on timeout the last editor.log lines are printed. Exits nonzero on any failure and writes
artifacts/ui/validate-<UTC>/summary.md.
"""
import argparse
import asyncio
import json
import os
import re
import signal
import subprocess
import time
from datetime import datetime, timezone
from pathlib import Path
from typing import NamedTuple

from fastmcp import Client

ROOT = Path(__file__).resolve().parents[2]
MCP_URL = os.environ.get("SS3D_MCP_URL", "http://127.0.0.1:8080")
UI = ROOT / "artifacts/ui"
EDITOR_LOG = UI / "editor.log"
COMPILE_RESULT = UI / "compile-result.json"
BUILD_RESULTS = {"client": UI / "client-build-result.json", "server": UI / "server-build-result.json"}
# The scenarios and client counts of .github/workflows/multiplayer-smoke-test.yml, plus sandbox-reset.
SMOKE_SCENARIOS = [("basic-round", 1), ("late-join", 2), ("reconnect", 1), ("atmos-client-sync", 1),
                   ("sandbox-reset", 1)]
LOG_ERROR = re.compile(r"(: error [A-Za-z]+\d+:|error CS\d+)")
ORDER = ["preflight", "compile", "editmode", "playmode", "visual", "lifecycle", "sandbox", "build", "remote", "smoke"]
QUICK = ["preflight", "compile", "editmode", "visual", "lifecycle"]  # PlayMode is --full only (minutes)
KNOWN_ISSUE_DIR = ROOT / "Assets/Scripts/Tests/KnownIssueReproduction"
KNOWN_ISSUE_LIST = ROOT / "Testing/ui/playmode-known-issues.txt"
PREFLIGHT = 'return string.Join("\\n", SS3D.Editor.Automation.AutomationPreflight.Check());'


class Done(NamedTuple):
    note: str
    report: str = ""
    pngs: tuple = ()


class StepFailed(Exception):
    def __init__(self, message, report=None):
        super().__init__(message)
        self.report = report


class Editor:
    def __init__(self, client):
        self.client = client

    async def connect(self):
        data = json.loads((await self.client.read_resource("mcpforunity://instances"))[0].text)
        matches = [i for i in data.get("instances", []) if i.get("name") == ROOT.name]
        if len(matches) != 1:
            raise StepFailed(f"Expected one SS3D editor at {ROOT}; instances: {data.get('instances')}")
        await self.client.call_tool("set_active_instance", {"instance": matches[0]["id"]})
        info = json.loads((await self.client.read_resource("mcpforunity://project/info"))[0].text)
        if Path(info["data"]["projectRoot"]).resolve() != ROOT:
            raise StepFailed("Connected editor belongs to a different checkout")

    async def execute(self, code):
        result = await self.client.call_tool("execute_code", {"action": "execute", "code": code}, raise_on_error=False)
        payload = json.loads(result.content[0].text)
        if not payload.get("success"):
            raise StepFailed(f"execute_code failed: {payload.get('message') or payload.get('error')}\n{json.dumps(payload.get('data'))[:2000]}")
        return (payload.get("data") or {}).get("result")

    async def menu(self, path):
        result = await self.client.call_tool("execute_menu_item", {"menu_path": path}, raise_on_error=False)
        if result.is_error:
            raise StepFailed(f"{path}: {result.content[0].text if result.content else result}")

    async def busy(self):
        """Why the editor can't take the next step, or None. Asks the editor itself: the state
        resource's ready_for_tools reports a 'stale_status' heartbeat while an idle editor is fine."""
        try:
            return await self.execute(
                'if (EditorApplication.isPlayingOrWillChangePlaymode) return "in Play Mode";'
                'if (EditorApplication.isCompiling) return "compiling";'
                'if (EditorApplication.isUpdating) return "importing assets";'
                'return "";') or None
        except Exception as error:
            return f"bridge unavailable ({str(error).splitlines()[0][:120]})"

    async def wait_ready(self, timeout=180):
        """Out of Play Mode, not compiling or importing, bridge answering."""
        deadline = time.monotonic() + timeout
        reason = "?"
        while time.monotonic() < deadline:
            reason = await self.busy()
            if reason is None:
                return
            await asyncio.sleep(1)
        raise asyncio.TimeoutError(f"editor not ready after {timeout}s: {reason}")


def rel(path):
    return str(Path(path).relative_to(ROOT))


def log_lines():
    if not EDITOR_LOG.exists():
        return []
    return EDITOR_LOG.read_text(errors="replace").splitlines()


def watchdog():
    """(dialog found, output)."""
    proc = subprocess.run([str(ROOT / "Testing/ui/watch-dialogs.sh")], capture_output=True, text=True)
    return proc.returncode == 1, (proc.stdout + proc.stderr).strip()


async def wait_for(check, expected, timeout, what):
    """Poll check() each second; watchdog after `expected` seconds, fail on a dialog or timeout."""
    start = time.monotonic()
    next_watch = start + expected
    while True:
        value = await check()
        if value is not None:
            return value
        now = time.monotonic()
        if now - start > timeout:
            found, output = watchdog()
            tail = [line for line in log_lines() if line.strip()][-40:]
            raise StepFailed(f"Timed out after {timeout}s waiting for {what}.\nWatchdog: {output or 'no dialogs'}\n"
                             "Last editor.log lines:\n  " + "\n  ".join(tail))
        if now >= next_watch:
            next_watch = now + 30
            found, output = watchdog()
            if found:
                raise StepFailed(f"Modal dialog while waiting for {what}:\n{output}")
        await asyncio.sleep(1)


def newest_report(pattern, before):
    created = set(UI.glob(pattern)) - before
    if not created:
        return None
    path = max(created, key=lambda p: p.stat().st_mtime)
    try:
        report = json.loads(path.read_text())
    except json.JSONDecodeError:
        return None
    return (path, report) if report.get("status") in ("passed", "failed") else None


# ---- steps -----------------------------------------------------------------------------------


async def step_preflight(editor, ctx):
    await editor.wait_ready()
    problems = await editor.execute(PREFLIGHT)
    if problems:
        raise StepFailed("Preflight problems (not saved or discarded; the owner decides):\n- "
                         + problems.replace("\n", "\n- "))
    return Done("open scenes clean")


async def step_compile(editor, ctx):
    await editor.wait_ready()
    marker = len(log_lines())
    requested = int(time.time() * 1000)
    await editor.execute("AssetDatabase.Refresh(); UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation(); return \"ok\";")

    async def finished():
        result = json.loads(COMPILE_RESULT.read_text()) if COMPILE_RESULT.exists() else {}
        if result.get("finishedUnixMs", 0) < requested:
            return None
        return None if await editor.busy() else result

    result = await wait_for(finished, 90, 600, "script compilation")
    errors = list(result.get("errors", []))
    seen = set(errors)
    for line in log_lines()[marker:]:
        if LOG_ERROR.search(line) and line.strip() not in seen:
            errors.append(line.strip())
            seen.add(line.strip())
    failed = await editor.execute("return EditorUtility.scriptCompilationFailed.ToString();")
    seconds = (result["finishedUnixMs"] - result["startedUnixMs"]) / 1000
    if errors or failed == "True":
        assemblies = ", ".join(result.get("failedAssemblies", [])) or "?"
        raise StepFailed(f"Compilation failed ({assemblies}; scriptCompilationFailed={failed}):\n  "
                         + "\n  ".join(errors or ["(no messages captured; see editor.log)"]), rel(COMPILE_RESULT))
    return Done(f"no errors ({seconds:.1f}s compile)", rel(COMPILE_RESULT))


async def run_test_job(editor, mode, expected, timeout):
    """Run one Test Runner job through MCP; returns its data once the editor's scenes are restored."""
    await editor.wait_ready()
    args = {"mode": mode}
    if mode == "PlayMode":
        args["init_timeout"] = 120000  # entering Play Mode for the first test takes a while
    started = await editor.client.call_tool("run_tests", args, raise_on_error=False)
    payload = json.loads(started.content[0].text)
    job = (payload.get("data") or {}).get("job_id")
    if not job:
        raise StepFailed(f"run_tests did not start: {json.dumps(payload)[:1000]}")

    async def done():
        try:
            polled = await editor.client.call_tool(
                "get_test_job", {"job_id": job, "wait_timeout": 20, "include_details": True}, raise_on_error=False)
            data = json.loads(polled.content[0].text).get("data") or {}
        except Exception:
            return None  # domain reload between polls
        return data if data.get("status") in ("succeeded", "failed", "completed", "error") else None

    data = await wait_for(done, expected, timeout, f"{mode} tests")
    # The Test Runner restores the scene setup after the job reports done; tests that open scenes
    # leave them dirty until then. Wait for the restore, then hold the tests to the same preflight.
    await editor.wait_ready()
    deadline = time.monotonic() + 60
    while True:
        try:
            problems = await editor.execute(PREFLIGHT)
        except StepFailed as error:  # the bridge refuses calls mid-restore
            problems = f"editor busy restoring scenes ({str(error).splitlines()[0]})"
        if not problems or time.monotonic() > deadline:
            break
        await asyncio.sleep(1)
    return data, problems


def failed_tests(data):
    return [t for t in (data.get("result") or {}).get("results", [])
            if (t.get("state") or "").lower().startswith("fail")]


def first_line(test):
    return ((test.get("message") or "").strip().splitlines() or [""])[0][:200]


async def step_editmode(editor, ctx):
    data, problems = await run_test_job(editor, "EditMode", 300, 1200)
    report = ctx["out"] / "editmode.json"
    report.write_text(json.dumps(data, indent=2))
    summary = (data.get("result") or {}).get("summary") or {}
    failed = summary.get("failed", 0)
    if data.get("status") != "succeeded" or failed:
        names = [t.get("fullName") or t.get("name") for t in failed_tests(data)]
        raise StepFailed(f"EditMode: {failed} failed of {summary.get('total', '?')}; status {data.get('status')} {data.get('error') or ''}\n  "
                         + "\n  ".join(names[:30]), rel(report))
    if problems:
        raise StepFailed("EditMode tests left the editor polluted:\n- " + problems.replace("\n", "\n- "), rel(report))
    return Done(f"{summary.get('passed', '?')}/{summary.get('total', '?')} passed", rel(report))


def known_issue_fixtures():
    """Fixture class names under KnownIssueReproduction: their failures are triaged, not regressions."""
    return {m.group(1) for f in KNOWN_ISSUE_DIR.glob("*.cs")
            for m in re.finditer(r"\bclass\s+(\w+)", f.read_text(errors="replace"))}


def fixture_of(test):
    """'SS3D.Tests.Fixture.Method(args)' → 'Fixture'."""
    parts = (test.get("fullName") or "").split("(")[0].split(".")
    return parts[-2] if len(parts) >= 2 else ""


def expected_failures():
    """Expected-failure test names or fixture prefixes from playmode-known-issues.txt ('#' comments)."""
    if not KNOWN_ISSUE_LIST.exists():
        return []
    lines = (line.split("#", 1)[0].strip() for line in KNOWN_ISSUE_LIST.read_text().splitlines())
    return [line for line in lines if line]


async def step_playmode(editor, ctx):
    data, problems = await run_test_job(editor, "PlayMode", 600, 2400)
    report = ctx["out"] / "playmode.json"
    report.write_text(json.dumps(data, indent=2))
    if data.get("status") == "error" or not (data.get("result") or {}).get("results"):
        raise StepFailed(f"PlayMode run did not produce results: status {data.get('status')} {data.get('error') or ''}", rel(report))

    fixtures = known_issue_fixtures()
    expected = expected_failures()

    def is_known(test):
        return fixture_of(test) in fixtures

    def is_expected(test):
        name = test.get("fullName") or ""
        return any(name == e or name.startswith(e + ".") or name.startswith(e + "(") for e in expected)

    results = data["result"]["results"]
    failing = failed_tests(data)
    regular = [t for t in results if not is_known(t)]
    known = [t for t in results if is_known(t)]
    unexpected = [t for t in failing if not is_expected(t)]
    regressions = [t for t in unexpected if not is_known(t)]
    new_known = [t for t in unexpected if is_known(t)]
    expected_known = [t for t in failing if is_expected(t) and is_known(t)]
    expected_regular = [t for t in failing if is_expected(t) and not is_known(t)]
    now_passing = [t for t in results if is_expected(t) and (t.get("state") or "").lower() == "passed"]

    lines = [f"# PlayMode expected failures ({rel(KNOWN_ISSUE_LIST)})", ""]
    for title, tests in (("Known issues that still reproduce", expected_known),
                         ("Listed regular tests still failing (see the list for why)", expected_regular),
                         ("Unlisted known-issue failures", new_known),
                         ("Listed but now passing (fixed? remove from the list)", now_passing)):
        lines += [f"## {title}: {len(tests)}", ""] + [f"- `{t['fullName']}`: {first_line(t)}" for t in tests] + [""]
    (ctx["out"] / "playmode-known-issues.md").write_text("\n".join(lines))
    ctx["sections"].append("\n".join(["#" + line if line.startswith("#") else line for line in lines]))

    passed = sum((t.get("state") or "").lower() == "passed" for t in regular)
    note = (f"{passed}/{len(regular)} regular passed ({len(expected_regular)} listed failures); known issues: "
            f"{len(expected_known)} reproduce, {sum((t.get('state') or '').lower() == 'passed' for t in known)} pass"
            + (f"; {len(now_passing)} listed but passing (update the list)" if now_passing else ""))
    if unexpected:
        detail = [f"{t['fullName']}: {first_line(t)}" for t in regressions + new_known]
        raise StepFailed(f"PlayMode: {len(regressions)} regular failure(s), {len(new_known)} unlisted known-issue failure(s); {note}\n  "
                         + "\n  ".join(detail[:30]), rel(report))
    if problems:
        raise StepFailed("PlayMode tests left the editor polluted:\n- " + problems.replace("\n", "\n- "), rel(report))
    return Done(note, rel(report))


async def run_pilot(editor, menu, pattern, expected, timeout):
    await editor.wait_ready()
    before = set(UI.glob(pattern))
    await editor.menu(menu)

    async def report():
        return newest_report(pattern, before)

    path, data = await wait_for(report, expected, timeout, menu)
    report_path = rel(path)
    if data["status"] != "passed":
        detail = data.get("failure") or ""
        if data.get("errors"):
            detail += "\nCaptured errors:\n  " + "\n  ".join(e.splitlines()[0] for e in data["errors"][:10])
        raise StepFailed(f"{menu} failed: {detail}", report_path)
    return Done(f"{len(data.get('checks') or [])} checks", report_path, tuple(sorted(rel(p) for p in path.parent.glob("*.png"))))


async def step_visual(editor, ctx):
    return await run_pilot(editor, "SS3D/Testing/Run Lobby UI Pilot", "[0-9]*/status.json", 120, 300)


async def step_lifecycle(editor, ctx):
    return await run_pilot(editor, "SS3D/Testing/Run Lobby Lifecycle Pilot", "lifecycle-*/status.json", 150, 420)


async def step_sandbox(editor, ctx):
    # Two automatic sandbox rounds (2 + 1 minutes plus countdowns): --full only.
    return await run_pilot(editor, "SS3D/Testing/Run Sandbox Pilot", "sandbox-*/status.json", 300, 600)


async def build_target(editor, target):
    """Rebuild the client or dedicated server when its source stamp is stale; returns a short note."""
    method = target.capitalize()
    await editor.wait_ready()
    stale = await editor.execute(f'return SS3D.Editor.Automation.AutomationBuild.{method}StaleReason() ?? "";')
    if not stale:
        return f"{target} current"
    print(f"  {stale}; rebuilding (warm ~1-2 min, cold ~15-20 min; the editor is blocked meanwhile)", flush=True)
    requested = datetime.now(timezone.utc)
    await editor.execute(f"SS3D.Editor.Automation.AutomationBuild.Build{method}(); return \"scheduled\";")
    result_file = BUILD_RESULTS[target]

    async def finished():
        try:
            result = json.loads(result_file.read_text())
        except (OSError, json.JSONDecodeError):
            return None
        started = datetime.fromisoformat(result.get("startedUtc", "1970-01-01T00:00:00+00:00").replace("Z", "+00:00"))
        if started.tzinfo is None:
            started = started.replace(tzinfo=timezone.utc)
        if started < requested.replace(microsecond=0) or result.get("status") == "building":
            return None
        return result

    result = await wait_for(finished, 120, 2400, f"{target} build")
    if result["status"] != "succeeded":
        raise StepFailed(f"{method} build failed: {result.get('error')}", rel(result_file))
    return f"{target} rebuilt {result['sourceHash']} in {result['seconds']:.0f}s"


async def step_build(editor, ctx):
    notes = [await build_target(editor, "client"), await build_target(editor, "server")]
    return Done("; ".join(notes), rel(BUILD_RESULTS["client"]))


async def step_remote(editor, ctx):
    return await run_pilot(editor, "SS3D/Testing/Run Lobby Remote Client Pilot", "remote-*/status.json", 120, 420)


async def step_smoke(editor, ctx):
    """Real headless dedicated server + clients (no editor involvement) per scenario."""
    for target in ("client", "server"):
        stale = await editor.execute(
            f'return SS3D.Editor.Automation.AutomationBuild.{target.capitalize()}StaleReason() ?? "";')
        if stale:
            raise StepFailed(f"{stale}; run the build step first")
    script = ROOT / "Testing/multiplayer/run_smoketest.sh"
    results, failed = [], []
    for scenario, clients in SMOKE_SCENARIOS:
        log = ctx["out"] / f"smoke-{scenario}.log"
        started = time.monotonic()
        proc = await asyncio.create_subprocess_exec(
            "bash", str(script), scenario, str(clients), cwd=ROOT,
            stdout=log.open("w"), stderr=asyncio.subprocess.STDOUT, start_new_session=True)
        try:
            code = await asyncio.wait_for(proc.wait(), 600)
        except asyncio.TimeoutError:
            os.killpg(proc.pid, signal.SIGKILL)  # the script and every server/client player it started
            await proc.wait()
            code = "timeout"
        seconds = time.monotonic() - started
        text = log.read_text(errors="replace")
        verdict = "passed" if code == 0 else f"FAILED ({code})"
        results.append(f"{scenario}×{clients} {verdict} {seconds:.0f}s")
        print(f"   {scenario} ({clients} client{'s' if clients > 1 else ''}): {verdict} in {seconds:.0f}s", flush=True)
        if code != 0:
            errors = [line for line in text.splitlines() if line.startswith(("error:", "FAIL", "Client ", "Server "))]
            failed.append(f"{scenario}: {rel(log)}\n    " + "\n    ".join(errors[:8]))
    lines = ["## Multiplayer smoke", "", *[f"- {r}" for r in results], ""]
    ctx["sections"].append("\n".join(lines))
    if failed:
        raise StepFailed(f"Multiplayer smoke: {len(failed)}/{len(SMOKE_SCENARIOS)} scenarios failed\n  " + "\n  ".join(failed))
    return Done(", ".join(results))


async def step_round_reset(editor, ctx):
    return await run_pilot(editor, "SS3D/Testing/Run Round Reset Regressions",
                           "round-reset-regressions-*/status.json", 90, 300)


async def step_ragdoll_space(editor, ctx):
    return await run_pilot(editor, "SS3D/Testing/Run Ragdoll Space Regressions",
                           "ragdoll-space-*/status.json", 90, 300)


async def step_respawn_cleanup(editor, ctx):
    return await run_pilot(editor, "SS3D/Testing/Run Respawn Cleanup Regressions",
                           "respawn-cleanup-*/status.json", 90, 300)


async def step_critical_speech(editor, ctx):
    return await run_pilot(editor, "SS3D/Testing/Run Critical Speech Regressions",
                           "critical-speech-*/status.json", 90, 300)


async def step_critical_reclaim(editor, ctx):
    return await run_pilot(editor, "SS3D/Testing/Run Critical Reclaim Regressions",
                           "critical-reclaim-*/status.json", 90, 300)


async def step_interaction_overlay(editor, ctx):
    return await run_pilot(editor, "SS3D/Testing/Run Interaction Overlay Regressions",
                           "interaction-overlay-*/status.json", 90, 300)


STEPS = {name: globals()["step_" + name] for name in ORDER}
STEPS["round-reset"] = step_round_reset
STEPS["ragdoll-space"] = step_ragdoll_space
STEPS["respawn-cleanup"] = step_respawn_cleanup
STEPS["critical-speech"] = step_critical_speech
STEPS["critical-reclaim"] = step_critical_reclaim
STEPS["interaction-overlay"] = step_interaction_overlay


async def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    mode = parser.add_mutually_exclusive_group()
    mode.add_argument("--quick", action="store_true", help="preflight → lifecycle without playmode (default)")
    mode.add_argument("--full", action="store_true", help="all steps: PlayMode, client+server build, remote pilot, multiplayer smoke")
    mode.add_argument("--steps", help="comma-separated subset, e.g. compile or visual,remote")
    args = parser.parse_args()
    steps = args.steps.split(",") if args.steps else ORDER if args.full else QUICK
    unknown = [s for s in steps if s not in STEPS]
    if unknown:
        parser.error(f"unknown steps {unknown}; choose from {ORDER}")

    out = UI / ("validate-" + datetime.now(timezone.utc).strftime("%Y%m%d-%H%M%S"))
    out.mkdir(parents=True, exist_ok=True)
    ctx = {"out": out, "sections": []}
    rows, pngs, failures = [], [], []
    async def quiet(_message):
        pass  # the server's INFO notifications would bury the step output

    async with Client(MCP_URL + "/mcp", timeout=120, log_handler=quiet) as client:
        editor = Editor(client)
        await editor.connect()
        for name in steps:
            print(f"== {name}", flush=True)
            started = time.monotonic()
            status, note, report = "passed", "", ""
            try:
                done = await STEPS[name](editor, ctx)
                note, report = done.note, done.report
                pngs += done.pngs
            except (StepFailed, asyncio.TimeoutError, TimeoutError) as error:
                status, note = "FAILED", str(error)
                report = getattr(error, "report", None) or ""
            seconds = time.monotonic() - started
            rows.append((name, status, seconds, report))
            print(f"   {status} in {seconds:.0f}s: {note}" + (f"\n   report: {report}" if report else ""), flush=True)
            if status != "passed":
                failures.append((name, note))
                break  # later steps depend on earlier ones (compiled code, a current build)

    skipped = steps[len(rows):]
    table = ["| step | status | seconds | report |", "|------|--------|---------|--------|"]
    table += [f"| {n} | {s} | {t:.0f} | {r} |" for n, s, t, r in rows]
    table += [f"| {n} | not run | | |" for n in skipped]
    text = [f"# Validation {out.name}", "", *table, ""]
    if failures:
        text += ["## Failure", "", f"**{failures[0][0]}**", "", "```", failures[0][1], "```", ""]
    for section in ctx["sections"]:
        text += [section, ""]
    if pngs:
        text += ["## Screenshots to review", "", *[f"- {p}" for p in pngs], ""]
    (out / "summary.md").write_text("\n".join(text))
    print("\n" + "\n".join(table))
    if pngs:
        print(f"\n{len(pngs)} screenshots to review (listed in summary)")
    print(f"\nSummary: {(out / 'summary.md').relative_to(ROOT)}")
    print("RESULT: " + ("FAILED at " + failures[0][0] if failures else "PASSED"))
    return 1 if failures else 0


if __name__ == "__main__":
    raise SystemExit(asyncio.run(main()))
