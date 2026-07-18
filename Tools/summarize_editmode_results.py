#!/usr/bin/env python3
"""Summarize Unity EditMode NUnit XML (artifacts/editmode-results.xml).

Prints assembly totals, each failed/inconclusive case with message + truncated stack,
and a suggested --filter string for Tools/run_editmode_tests.sh.

Exit 0 if no failures/inconclusive; 1 otherwise; 2 if XML missing/unreadable.
"""

from __future__ import annotations

import sys
import xml.etree.ElementTree as ET
from pathlib import Path


def text(el: ET.Element | None) -> str:
    if el is None or el.text is None:
        return ""
    return el.text.strip()


def main() -> int:
    if len(sys.argv) < 2:
        print("Usage: summarize_editmode_results.py <editmode-results.xml>", file=sys.stderr)
        return 2

    path = Path(sys.argv[1])
    if not path.is_file():
        print(f"error: results file not found: {path}", file=sys.stderr)
        return 2

    try:
        root = ET.parse(path).getroot()
    except ET.ParseError as exc:
        print(f"error: failed to parse {path}: {exc}", file=sys.stderr)
        return 2

    # Unity writes <test-run> root; tolerate missing attributes.
    passed = root.get("passed", "?")
    failed = root.get("failed", "?")
    inconclusive = root.get("inconclusive", "?")
    skipped = root.get("skipped", "?")
    total = root.get("total", "?")
    result = root.get("result", "?")

    print(f"=== EditMode results: {path} ===")
    print(f"result={result}  passed={passed}  failed={failed}  inconclusive={inconclusive}  skipped={skipped}  total={total}")

    for suite in root.iter("test-suite"):
        if suite.get("type") != "Assembly":
            continue
        print(
            f"  assembly {suite.get('name')}: "
            f"{suite.get('passed', '0')} passed, "
            f"{suite.get('failed', '0')} failed, "
            f"{suite.get('inconclusive', '0')} inconclusive, "
            f"{suite.get('skipped', '0')} skipped / {suite.get('total', '0')} total"
        )

    problems: list[tuple[str, str, str, str, str]] = []
    for case in root.iter("test-case"):
        case_result = case.get("result", "")
        if case_result not in ("Failed", "Inconclusive"):
            continue
        name = case.get("name") or "?"
        classname = case.get("classname") or ""
        failure = case.find("failure")
        message = text(failure.find("message") if failure is not None else None)
        stack = text(failure.find("stack-trace") if failure is not None else None)
        if not message:
            reason = case.find("reason")
            message = text(reason.find("message") if reason is not None else None)
        problems.append((case_result, name, classname, message, stack))

    if not problems:
        print("failures: (none)")
        return 0

    print(f"failures ({len(problems)}):")
    filter_parts: list[str] = []
    for case_result, name, classname, message, stack in problems:
        fq = f"{classname}.{name}" if classname and not name.startswith(classname) else name
        print(f"  [{case_result}] {fq}")
        if message:
            for line in message.splitlines()[:8]:
                print(f"    {line}")
        if stack:
            for line in stack.splitlines()[:12]:
                print(f"    {line}")
        # Prefer short unique filter tokens from the method name.
        method = name.split(".")[-1] if name else ""
        if method:
            filter_parts.append(method)

    # Dedupe while preserving order
    seen: set[str] = set()
    uniq = []
    for p in filter_parts:
        if p not in seen:
            seen.add(p)
            uniq.append(p)
    if uniq:
        # Unity -testFilter is a regex; join with | for a focused re-run.
        joined = "|".join(uniq[:20])
        print(f"re-run: ./Tools/run_editmode_tests.sh --filter '{joined}'")

    return 1


if __name__ == "__main__":
    sys.exit(main())
