#!/usr/bin/env python3
"""Report unmapped SS13 type paths in a .dmm using ss13_type_map.yaml (no Unity)."""
from __future__ import annotations

import argparse
import re
import sys
from collections import Counter
from pathlib import Path

PREFAB_RE = re.compile(r'^"([^"]+)"\s*=\s*\(', re.M)
PATH_RE = re.compile(r"(/[a-zA-Z0-9_/\-]+)")

INFRA_KIND_DEFAULTS = {
    "cable": "Cables",
    "pipe": "AtmosPipesWaste",
    "disposal": "DisposalPipes",
    "disposalterminal": "DisposalBin",
    "vent": "Vent",
    "scrubber": "Scrubber",
    "apc": "APC",
    "light": "LightTubeFixture",
    "lattice": "Lattice",
    "table": "TableSteel",
    "smes": "SMES",
}


def load_type_map(path: Path) -> tuple[dict[str, str], list[tuple[str, str, str]]]:
    defaults = {
        "floor": "TileGrey",
        "wall": "SteelWall",
        "window": "SteelWindow",
        "door": "CivillianAirlock",
        **INFRA_KIND_DEFAULTS,
    }
    prefixes: list[tuple[str, str, str]] = []
    text = path.read_text(encoding="utf-8")
    current = None
    section = None
    for raw in text.splitlines():
        line = raw.split("#", 1)[0].rstrip()
        if not line.strip():
            continue
        s = line.strip()
        if s == "defaults:":
            section = "defaults"
            continue
        if s == "prefixes:":
            section = "prefixes"
            continue
        if section == "defaults" and ":" in s:
            k, v = [p.strip() for p in s.split(":", 1)]
            defaults[k] = v
            continue
        m = re.match(r"- match:\s*(\S+)", s)
        if m:
            if current:
                prefixes.append(current)
            current = (m.group(1), "skip", "")
            continue
        if current is None:
            continue
        km = re.match(r"kind:\s*(\S+)", s)
        if km:
            current = (current[0], km.group(1).lower(), current[2])
            continue
        sm = re.match(r"so:\s*(\S+)", s)
        if sm:
            current = (current[0], current[1], sm.group(1))
    if current:
        prefixes.append(current)
    prefixes.sort(key=lambda t: len(t[0]), reverse=True)
    return defaults, prefixes


def match_path(path: str, prefixes: list[tuple[str, str, str]], defaults: dict[str, str]) -> str | None:
    if path.startswith(("/area/", "/turf/open/space", "/turf/template_noop", "/turf/open/openspace")):
        return None
    if path.startswith("/obj/machinery/light/floor"):
        return ""
    for match, kind, so in prefixes:
        if path.startswith(match):
            return so or defaults.get(kind, "")
    if path.startswith(("/turf/closed/wall", "/turf/closed/r_wall")):
        return defaults["wall"]
    if path.startswith(("/turf/open/floor", "/turf/open/misc")):
        return defaults["floor"]
    if path.startswith("/obj/machinery/door"):
        return defaults["door"]
    if "/window" in path:
        return defaults["window"]
    if path.startswith("/obj/structure/cable"):
        return defaults["cable"]
    if path.startswith("/obj/structure/disposalpipe"):
        return defaults["disposal"]
    if path.startswith("/obj/structure/disposaloutlet"):
        return "DisposalOutlet"
    if path.startswith("/obj/machinery/disposal"):
        return defaults["disposalterminal"]
    if path.startswith("/obj/machinery/atmospherics/components/unary/vent_scrubber"):
        return defaults["scrubber"]
    if path.startswith("/obj/machinery/atmospherics/components/unary/vent_pump"):
        return defaults["vent"]
    if path.startswith("/obj/machinery/atmospherics/pipe"):
        return defaults["pipe"]
    if path.startswith("/obj/machinery/power/apc"):
        return defaults["apc"]
    if path.startswith("/obj/machinery/light/floor"):
        return ""
    if path.startswith("/obj/machinery/light/small"):
        return "LightBulbFixture"
    if path.startswith("/obj/machinery/light"):
        return defaults["light"]
    if path.startswith("/obj/structure/lattice/catwalk"):
        return "Catwalk"
    if path.startswith("/obj/structure/lattice"):
        return defaults["lattice"]
    if path.startswith("/obj/structure/table/wood"):
        return "TableWood"
    if path.startswith("/obj/structure/table"):
        return defaults["table"]
    if path.startswith("/obj/machinery/power/smes"):
        return defaults["smes"]
    return ""


def iter_paths(dmm: str) -> list[str]:
    paths: list[str] = []
    # Prefer prefab bodies only
    for m in PREFAB_RE.finditer(dmm):
        start = m.end() - 1  # at '('
        depth = 0
        i = start
        while i < len(dmm):
            c = dmm[i]
            if c == "(":
                depth += 1
            elif c == ")":
                depth -= 1
                if depth == 0:
                    body = dmm[start + 1 : i]
                    for pm in PATH_RE.finditer(body):
                        p = pm.group(1)
                        if p.startswith("/"):
                            paths.append(p.rstrip(","))
                    break
            i += 1
    return paths


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("dmm", type=Path)
    ap.add_argument(
        "--type-map",
        type=Path,
        default=Path(__file__).with_name("ss13_type_map.yaml"),
    )
    ap.add_argument("-n", type=int, default=40, help="top N unmapped")
    args = ap.parse_args()
    defaults, prefixes = load_type_map(args.type_map)
    counts: Counter[str] = Counter()
    mapped = 0
    for path in iter_paths(args.dmm.read_text(encoding="utf-8", errors="replace")):
        result = match_path(path, prefixes, defaults)
        if result is None:
            continue
        if result == "":
            counts[path] += 1
        else:
            mapped += 1
    print(f"mapped_hits={mapped} unmapped_types={len(counts)} type_map={args.type_map}")
    for path, n in counts.most_common(args.n):
        print(f"{n}\t{path}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
