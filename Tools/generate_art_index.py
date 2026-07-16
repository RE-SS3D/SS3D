#!/usr/bin/env python3
"""Generate a combined index of SS3D art assets (in-game + SS3D-Art repo)."""

from __future__ import annotations

import argparse
import json
import os
import re
import sys
import urllib.error
import urllib.request
from collections import defaultdict
from dataclasses import asdict, dataclass, field
from datetime import datetime, timezone
from pathlib import Path
from typing import Iterable

REPO_ROOT = Path(__file__).resolve().parents[1]
IN_GAME_ROOT = REPO_ROOT / "Assets" / "Art"
OUTPUT_JSON = REPO_ROOT / "Documents" / "art-asset-index.json"
OUTPUT_AVAILABLE_JSON = REPO_ROOT / "Documents" / "art-available-for-import.json"
OUTPUT_MD = REPO_ROOT / "Documents" / "art-asset-index.md"
CACHE_FILE = REPO_ROOT / "Tools" / ".ss3d-art-tree-cache.json"

SS3D_ART_REPO = "RE-SS3D/SS3D-Art"
SS3D_ART_BRANCH = "main"
SS3D_ART_TREE_URL = (
    f"https://api.github.com/repos/{SS3D_ART_REPO}/git/trees/{SS3D_ART_BRANCH}?recursive=1"
)
SS3D_ART_BLOB_URL = f"https://github.com/{SS3D_ART_REPO}/blob/{SS3D_ART_BRANCH}"

ART_EXTENSIONS = {
    "blend",
    "fbx",
    "gif",
    "jpg",
    "jpeg",
    "ma",
    "mp3",
    "mp4",
    "ogg",
    "otf",
    "png",
    "psd",
    "svg",
    "ttf",
    "wav",
}

EXCLUDE_PATH_PREFIXES = (
    "Font/TextMesh Pro/Examples & Extras",
    "Font/TextMesh Pro/Documentation",
)

EXCLUDE_PATHS_REMOTE = (
    ".github/",
    "Documents/",
)


@dataclass
class AssetRecord:
    name: str
    stem: str
    extension: str
    category: str
    in_game_path: str | None = None
    ss3d_art_path: str | None = None
    github_url: str | None = None
    import_target_path: str | None = None
    status: str = "unknown"  # imported | in_game_only | ss3d_art_only

    def to_dict(self) -> dict:
        return asdict(self)


@dataclass
class CategoryStats:
    path: str
    in_game: int = 0
    ss3d_art: int = 0
    imported: int = 0
    in_game_only: int = 0
    ss3d_art_only: int = 0


def is_art_file(path: Path) -> bool:
    return path.suffix.lstrip(".").lower() in ART_EXTENSIONS


def should_exclude(relative: str, source: str) -> bool:
    if source == "in_game":
        return any(relative.startswith(prefix) for prefix in EXCLUDE_PATH_PREFIXES)
    if source == "ss3d_art":
        return any(relative.startswith(prefix) for prefix in EXCLUDE_PATHS_REMOTE)
    return False


def import_target_path(ss3d_art_path: str) -> str | None:
    """Map an SS3D-Art game asset path to its expected in-game import location."""
    if not ss3d_art_path.startswith("Assets/"):
        return None

    relative = ss3d_art_path[len("Assets/") :]
    path = Path(relative)
    export_extension = {
        "blend": "fbx",
        "ma": "fbx",
    }.get(path.suffix.lstrip(".").lower(), path.suffix.lstrip(".").lower())

    return f"Assets/Art/{path.with_suffix(f'.{export_extension}')}"


def is_available_for_import(record: AssetRecord) -> bool:
    return (
        record.status == "ss3d_art_only"
        and record.ss3d_art_path is not None
        and record.ss3d_art_path.startswith("Assets/")
    )


def normalize_key(relative_path: str, source: str) -> tuple[str, str]:
    """Return (category, stem) used to correlate assets across sources."""
    path = relative_path.replace("\\", "/")

    if source == "in_game":
        category = path
    elif path.startswith("Assets/"):
        category = path[len("Assets/") :]
    else:
        category = path

    filename = category.rsplit("/", 1)[-1]
    stem = Path(filename).stem.lower()
    category_dir = category.rsplit("/", 1)[0] if "/" in category else ""
    return category_dir, stem


def walk_in_game_assets() -> dict[tuple[str, str], AssetRecord]:
    records: dict[tuple[str, str], AssetRecord] = {}

    if not IN_GAME_ROOT.is_dir():
        return records

    for file_path in sorted(IN_GAME_ROOT.rglob("*")):
        if not file_path.is_file() or file_path.name.endswith(".meta"):
            continue
        if not is_art_file(file_path):
            continue

        relative = file_path.relative_to(IN_GAME_ROOT).as_posix()
        if should_exclude(relative, "in_game"):
            continue

        category_dir, stem = normalize_key(relative, "in_game")
        category = category_dir or "(root)"
        record = AssetRecord(
            name=file_path.name,
            stem=stem,
            extension=file_path.suffix.lstrip(".").lower(),
            category=category,
            in_game_path=f"Assets/Art/{relative}",
            status="in_game_only",
        )
        records[(category_dir, stem)] = record

    return records


def fetch_ss3d_art_tree(use_cache: bool, refresh_cache: bool) -> list[str]:
    if use_cache and CACHE_FILE.is_file() and not refresh_cache:
        with CACHE_FILE.open(encoding="utf-8") as handle:
            cached = json.load(handle)
        return cached["paths"]

    request = urllib.request.Request(
        SS3D_ART_TREE_URL,
        headers={"Accept": "application/vnd.github+json", "User-Agent": "ss3d-art-index"},
    )

    try:
        with urllib.request.urlopen(request, timeout=60) as response:
            payload = json.load(response)
    except urllib.error.URLError as exc:
        if CACHE_FILE.is_file():
            print(f"Warning: GitHub fetch failed ({exc}); using cache.", file=sys.stderr)
            with CACHE_FILE.open(encoding="utf-8") as handle:
                cached = json.load(handle)
            return cached["paths"]
        raise SystemExit(f"Failed to fetch SS3D-Art tree: {exc}") from exc

    paths = [
        item["path"]
        for item in payload.get("tree", [])
        if item.get("type") == "blob" and is_art_file(Path(item["path"]))
    ]

    CACHE_FILE.parent.mkdir(parents=True, exist_ok=True)
    with CACHE_FILE.open("w", encoding="utf-8") as handle:
        json.dump(
            {
                "fetched_at": datetime.now(timezone.utc).isoformat(),
                "repo": SS3D_ART_REPO,
                "branch": SS3D_ART_BRANCH,
                "paths": paths,
            },
            handle,
            indent=2,
        )

    return paths


def merge_ss3d_art_assets(
    records: dict[tuple[str, str], AssetRecord], remote_paths: Iterable[str]
) -> None:
    for remote_path in remote_paths:
        if should_exclude(remote_path, "ss3d_art"):
            continue

        path = Path(remote_path)
        category_dir, stem = normalize_key(remote_path, "ss3d_art")
        category = category_dir or "(root)"
        key = (category_dir, stem)

        github_url = f"{SS3D_ART_BLOB_URL}/{remote_path}"
        target = import_target_path(remote_path)
        if key in records:
            record = records[key]
            record.ss3d_art_path = remote_path
            record.github_url = github_url
            record.import_target_path = target
            record.status = "imported"
            continue

        records[key] = AssetRecord(
            name=path.name,
            stem=stem,
            extension=path.suffix.lstrip(".").lower(),
            category=category,
            ss3d_art_path=remote_path,
            github_url=github_url,
            import_target_path=target,
            status="ss3d_art_only",
        )


def build_category_stats(records: Iterable[AssetRecord]) -> list[CategoryStats]:
    grouped: dict[str, CategoryStats] = {}

    for record in records:
        top = record.category.split("/")[0] if record.category != "(root)" else "(root)"
        stats = grouped.setdefault(top, CategoryStats(path=top))
        if record.in_game_path:
            stats.in_game += 1
        if record.ss3d_art_path:
            stats.ss3d_art += 1
        if record.status == "imported":
            stats.imported += 1
        elif record.status == "in_game_only":
            stats.in_game_only += 1
        elif record.status == "ss3d_art_only":
            stats.ss3d_art_only += 1

    return sorted(grouped.values(), key=lambda item: item.path)


def build_subcategory_stats(records: Iterable[AssetRecord]) -> list[CategoryStats]:
    grouped: dict[str, CategoryStats] = {}

    for record in records:
        stats = grouped.setdefault(record.category, CategoryStats(path=record.category))
        if record.in_game_path:
            stats.in_game += 1
        if record.ss3d_art_path:
            stats.ss3d_art += 1
        if record.status == "imported":
            stats.imported += 1
        elif record.status == "in_game_only":
            stats.in_game_only += 1
        elif record.status == "ss3d_art_only":
            stats.ss3d_art_only += 1

    return sorted(grouped.values(), key=lambda item: item.path)


def extension_counts(records: Iterable[AssetRecord]) -> dict[str, int]:
    counts: dict[str, int] = defaultdict(int)
    for record in records:
        counts[record.extension] += 1
    return dict(sorted(counts.items(), key=lambda item: (-item[1], item[0])))


def render_markdown(payload: dict, available: list[dict]) -> str:
    summary = payload["summary"]
    categories = payload["categories"]

    available_by_category: dict[str, int] = defaultdict(int)
    for item in available:
        top = item["category"].split("/")[0]
        available_by_category[top] += 1

    lines = [
        "# Art asset index (agent reference)",
        "",
        "> Generated by `Tools/generate_art_index.py`. Re-run to refresh.",
        "",
        "Use this index when implementing a feature that needs art. Most game-ready assets",
        f"live in [{SS3D_ART_REPO}](https://github.com/{SS3D_ART_REPO}) but are not yet",
        "imported into Unity.",
        "",
        f"**Generated:** {payload['generated_at']}",
        "",
        "## When to use",
        "",
        "Consult this index **before** searching the filesystem or GitHub manually when:",
        "",
        "- Adding a prefab, item, machine, or structure that may already have art",
        "- Wiring a placeholder and you need to know if source art exists",
        "- Checking whether art is already imported (`status: imported`) or still only in SS3D-Art",
        "",
        "## Agent workflow",
        "",
        "1. Search [`art-available-for-import.json`](art-available-for-import.json) for the",
        "   object name or category (e.g. `welder`, `Models/Furniture/Machines`).",
        "2. If `status: imported` in the full index, use the existing `in_game_path` under `Assets/Art/`.",
        "3. If available only in SS3D-Art, note `ss3d_art_path`, `github_url`, and `import_target_path`.",
        "4. Export `.blend` / `.ma` models to `.fbx` before placing in `Assets/Art/`.",
        "5. After importing, regenerate this index so status updates.",
        "",
        "### Search examples",
        "",
        "```bash",
        "# Find importable models for engineering tools",
        'jq \'.assets[] | select(.category | contains("Tools/Engineering"))\' \\',
        "  Documents/art-available-for-import.json",
        "",
        "# Check if a specific asset is already imported",
        'jq \'.assets[] | select(.stem == "welder")\' Documents/art-asset-index.json',
        "",
        "# Grep by name (fast, no jq needed)",
        'grep -i "airlock" Documents/art-available-for-import.json',
        "```",
        "",
        "## Available for import",
        "",
        f"**{len(available)}** game-ready assets in SS3D-Art not yet in `Assets/Art/`:",
        "",
        "| Category | Available |",
        "|----------|----------:|",
    ]

    for category, count in sorted(available_by_category.items(), key=lambda item: (-item[1], item[0])):
        lines.append(f"| {category} | {count} |")

    lines.extend(
        [
            "",
            "Full list: [`art-available-for-import.json`](art-available-for-import.json)",
            "",
            "## Full catalog",
            "",
            "| Metric | Count |",
            "|--------|------:|",
            f"| Already imported (in-game + SS3D-Art) | {summary['imported']} |",
            f"| In-game only | {summary['in_game_only']} |",
            f"| Available for import | {summary['available_for_import']} |",
            f"| Promotional artwork (not game-ready) | {summary['artwork_only']} |",
            "",
            "Complete data with import status: [`art-asset-index.json`](art-asset-index.json)",
            "",
            "### Status values",
            "",
            "| Status | Meaning for agents |",
            "|--------|-------------------|",
            "| `imported` | Already in `Assets/Art/` — use `in_game_path` |",
            "| `ss3d_art_only` | Source exists in SS3D-Art — import via `import_target_path` |",
            "| `in_game_only` | Fork-specific; no SS3D-Art source tracked |",
            "",
            "### Path mapping",
            "",
            "| SS3D-Art | In-game target |",
            "|----------|----------------|",
            "| `Assets/Models/.../Foo.blend` | `Assets/Art/Models/.../Foo.fbx` |",
            "| `Assets/Textures/.../Bar.png` | `Assets/Art/Textures/.../Bar.png` |",
            "| `Assets/Sound/.../Baz.ogg` | `Assets/Art/Sound/.../Baz.ogg` |",
            "",
            "Promotional assets under SS3D-Art `Artwork/` are excluded from import lists.",
            "",
            "## Regenerating",
            "",
            "```bash",
            "python3 Tools/generate_art_index.py",
            "```",
            "",
            "Use `--refresh-cache` to force a new fetch from GitHub.",
            "",
            "## Related",
            "",
            "- [SS3D art guide](https://ss3d.gitbook.io/art-guide/) — contribution workflow",
            "- [AGENTS.md](../../AGENTS.md) — agent navigation",
            "",
        ]
    )

    return "\n".join(lines)


def build_available_for_import(records: Iterable[AssetRecord]) -> list[dict]:
    available = []
    for record in records:
        if not is_available_for_import(record):
            continue
        available.append(
            {
                "name": record.name,
                "stem": record.stem,
                "extension": record.extension,
                "category": record.category,
                "ss3d_art_path": record.ss3d_art_path,
                "import_target_path": record.import_target_path,
                "github_url": record.github_url,
            }
        )
    return available


def build_payload(records: dict[tuple[str, str], AssetRecord], remote_paths: list[str]) -> tuple[dict, list[dict]]:
    ordered = sorted(records.values(), key=lambda item: (item.category, item.name.lower()))
    in_game_files = sum(1 for item in ordered if item.in_game_path)
    ss3d_art_files = len(remote_paths)
    imported = sum(1 for item in ordered if item.status == "imported")
    in_game_only = sum(1 for item in ordered if item.status == "in_game_only")
    ss3d_art_only = sum(1 for item in ordered if item.status == "ss3d_art_only")
    available = build_available_for_import(ordered)
    artwork_only = ss3d_art_only - len(available)

    payload = {
        "generated_at": datetime.now(timezone.utc).isoformat(),
        "purpose": "Agent reference for locating SS3D art assets and determining import status.",
        "sources": {
            "in_game": {
                "root": "Assets/Art",
                "file_count": in_game_files,
            },
            "ss3d_art": {
                "repo": f"https://github.com/{SS3D_ART_REPO}",
                "branch": SS3D_ART_BRANCH,
                "file_count": ss3d_art_files,
            },
        },
        "summary": {
            "in_game_files": in_game_files,
            "ss3d_art_files": ss3d_art_files,
            "unique_assets": len(ordered),
            "imported": imported,
            "in_game_only": in_game_only,
            "ss3d_art_only": ss3d_art_only,
            "available_for_import": len(available),
            "artwork_only": artwork_only,
        },
        "categories": [asdict(item) for item in build_category_stats(ordered)],
        "subcategories": [asdict(item) for item in build_subcategory_stats(ordered)],
        "extensions": extension_counts(ordered),
        "assets": [item.to_dict() for item in ordered],
    }
    return payload, available


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--refresh-cache",
        action="store_true",
        help="Force refresh of the SS3D-Art GitHub tree cache.",
    )
    parser.add_argument(
        "--offline",
        action="store_true",
        help="Use only local files and cached SS3D-Art tree (no network).",
    )
    return parser.parse_args()


def main() -> None:
    args = parse_args()
    records = walk_in_game_assets()
    remote_paths = fetch_ss3d_art_tree(use_cache=True, refresh_cache=args.refresh_cache)
    if args.offline and not CACHE_FILE.is_file():
        remote_paths = []

    merge_ss3d_art_assets(records, remote_paths)
    payload, available = build_payload(records, remote_paths)

    OUTPUT_JSON.parent.mkdir(parents=True, exist_ok=True)
    with OUTPUT_JSON.open("w", encoding="utf-8") as handle:
        json.dump(payload, handle, indent=2)
        handle.write("\n")

    with OUTPUT_AVAILABLE_JSON.open("w", encoding="utf-8") as handle:
        json.dump(
            {
                "generated_at": payload["generated_at"],
                "purpose": "Game-ready SS3D-Art assets not yet imported into Assets/Art/.",
                "count": len(available),
                "assets": available,
            },
            handle,
            indent=2,
        )
        handle.write("\n")

    with OUTPUT_MD.open("w", encoding="utf-8") as handle:
        handle.write(render_markdown(payload, available))

    summary = payload["summary"]
    print(f"Wrote {OUTPUT_JSON} ({summary['unique_assets']} assets)")
    print(f"Wrote {OUTPUT_AVAILABLE_JSON} ({summary['available_for_import']} available for import)")
    print(f"Wrote {OUTPUT_MD}")


if __name__ == "__main__":
    main()
