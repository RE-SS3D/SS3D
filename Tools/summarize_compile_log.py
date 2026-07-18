#!/usr/bin/env python3
"""Summarize Unity script-compile diagnostics from an Editor or batchmode log.

Finds the latest completed Bee/Tundra compile window and prints unique
`path(line,col): error CODE: message` lines. Exit 0 if clean, 1 if errors.

Usage:
  ./Tools/summarize_compile_log.py artifacts/compile.log
  ./Tools/summarize_compile_log.py ~/.config/unity3d/Editor.log
"""

from __future__ import annotations

import re
import sys
from pathlib import Path

ERROR_RE = re.compile(r":\s*error\s+[A-Z]+[0-9]+:")
TUNDRA_RE = re.compile(r"^\*\*\* Tundra build (failed|success)\b")
SCRIPT_COMP_RE = re.compile(r"^\[ScriptCompilation\] Requested script compilation")
SCRIPTS_HAVE_ERRORS_RE = re.compile(r"Scripts have compiler errors\.")
COMPILECHECK_FAIL_RE = re.compile(r"CompileCheck: FAILED")
COMPILECHECK_OK_RE = re.compile(r"CompileCheck: OK")


def latest_compile_window(lines: list[str]) -> tuple[int, int, str | None]:
    """Return (start, end_exclusive, tundra_status) for the latest Tundra result.

    Window starts at the nearest preceding ScriptCompilation request (else 0).
    end is the line after the Tundra result (exclusive).
    """
    last_tundra: int | None = None
    status: str | None = None
    for i, line in enumerate(lines):
        m = TUNDRA_RE.match(line)
        if m:
            last_tundra = i
            status = m.group(1)

    if last_tundra is None:
        # Fall back to whole file (batchmode abort without Tundra line, etc.).
        return 0, len(lines), None

    start = 0
    for i in range(last_tundra, -1, -1):
        if SCRIPT_COMP_RE.match(lines[i]):
            start = i
            break

    # Include a short trailer — Unity sometimes repeats diagnostics after Tundra.
    end = min(len(lines), last_tundra + 40)
    return start, end, status


def collect_errors(window: list[str]) -> list[str]:
    seen: set[str] = set()
    out: list[str] = []
    for line in window:
        # Strip Unity console stack prefixes; diagnostics are usually bare.
        text = line.rstrip("\n")
        if ERROR_RE.search(text):
            # Prefer the substring that looks like a compiler diagnostic.
            if text not in seen:
                seen.add(text)
                out.append(text)
    return out


def main() -> int:
    if len(sys.argv) != 2 or sys.argv[1] in ("-h", "--help"):
        print(__doc__.strip(), file=sys.stderr)
        return 2

    path = Path(sys.argv[1])
    if not path.is_file():
        print(f"error: log not found: {path}", file=sys.stderr)
        return 1

    # Large Editor.log: read as text; 20–50MB is fine.
    text = path.read_text(encoding="utf-8", errors="replace")
    lines = text.splitlines()
    start, end, tundra = latest_compile_window(lines)
    window = lines[start:end]

    errors = collect_errors(window)
    other_fail = any(
        SCRIPTS_HAVE_ERRORS_RE.search(line) or COMPILECHECK_FAIL_RE.search(line)
        for line in window
    )
    # Whole-file CompileCheck markers (batchmode may exit before a new Tundra).
    if not errors and not other_fail:
        other_fail = COMPILECHECK_FAIL_RE.search(text) is not None and COMPILECHECK_OK_RE.search(
            text
        ) is None

    print(f"log: {path}")
    print(f"window: lines {start + 1}-{end} ({end - start} lines)", end="")
    if tundra:
        print(f"; last Tundra: {tundra}")
    else:
        print("; last Tundra: (none)")

    if errors or other_fail or tundra == "failed":
        print("Compile FAILED")
        for err in errors:
            print(err)
        if errors:
            print(f"({len(errors)} unique error line(s))")
        elif other_fail or tundra == "failed":
            print("(Tundra/scripts failed but no parseable : error CODE: lines in window)")
        return 1

    if COMPILECHECK_OK_RE.search(text) or tundra == "success" or tundra is None:
        print("Compile OK")
        return 0

    print("Compile OK")
    return 0


if __name__ == "__main__":
    sys.exit(main())
