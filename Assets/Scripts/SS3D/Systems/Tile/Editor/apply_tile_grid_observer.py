#!/usr/bin/env python3
"""Apply FishNet NetworkObserver + GridCondition to TileObjectSo prefabs."""

from __future__ import annotations

import random
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[6]
RESOURCES = ROOT / "Assets/Content/Data/TileMap/Resources"
OBJECT_REF_DIR = ROOT / "Assets/Content/Data/ObjectAssetReferences"
WORLD_OBJECTS = ROOT / "Assets/Content/WorldObjects"

TILE_OBJECT_SO_GUID = "4584c35d672b8a74797cc5efa123ce16"
NETWORK_OBJECT_GUID = "26b716c41e9b56b4baafaf13a523ba2e"
NETWORK_OBSERVER_GUID = "c71fd7f855ec523429999fc4e14a1928"
GRID_CONDITION_REF = "{fileID: 11400000, guid: cc503f7541ebd424c94541e6a767efee, type: 2}"


def build_guid_index() -> dict[str, Path]:
    index: dict[str, Path] = {}
    assets_root = ROOT / "Assets"
    for meta in assets_root.rglob("*.meta"):
        text = meta.read_text(encoding="utf-8", errors="ignore")
        match = re.search(r"^guid: ([0-9a-f]{32})$", text, re.MULTILINE)
        if match:
            index[match.group(1)] = meta.with_suffix("")
    return index


def resolve_prefab_path(prefab_asset_guid: str, guid_index: dict[str, Path]) -> Path | None:
    ref_path = guid_index.get(prefab_asset_guid)
    if ref_path is None or ref_path.suffix != ".asset":
        return None

    ref_text = ref_path.read_text(encoding="utf-8", errors="ignore")
    name_match = re.search(r"  m_Name: (.+)", ref_text)
    if not name_match:
        return None

    prefab_name = f"{name_match.group(1).strip()}.prefab"
    matches = list(WORLD_OBJECTS.rglob(prefab_name))
    if len(matches) == 1:
        return matches[0]
    if len(matches) > 1:
        return matches[0]
    return None


def extract_prefab_guid(asset_path: Path) -> str | None:
    text = asset_path.read_text(encoding="utf-8", errors="ignore")
    if TILE_OBJECT_SO_GUID not in text:
        return None
    match = re.search(
        r"PrefabAsset: \{fileID: 11400000, guid: ([0-9a-f]{32}), type: 2\}",
        text,
    )
    return match.group(1) if match else None


def random_negative_file_id() -> int:
    return -random.randint(1, 9_000_000_000_000_000_000)


def patch_prefab(prefab_path: Path) -> bool:
    content = prefab_path.read_text(encoding="utf-8")
    if NETWORK_OBSERVER_GUID in content:
        return False
    if NETWORK_OBJECT_GUID not in content:
        return False

    nob_match = re.search(
        rf"(--- !u!114 &(?P<comp_id>-?[0-9]+)\nMonoBehaviour:\n(?:.*\n)*?"
        rf"  m_GameObject: {{fileID: (?P<go_id>[0-9]+)}}\n(?:.*\n)*?"
        rf"  m_Script: {{fileID: 11500000, guid: {NETWORK_OBJECT_GUID}, type: 3}})",
        content,
    )
    if not nob_match:
        print(f"  skip (no NetworkObject): {prefab_path.relative_to(ROOT)}")
        return False

    go_id = nob_match.group("go_id")
    go_pattern = re.compile(
        rf"--- !u!1 &{go_id}\nGameObject:\n(?:.*\n)*?  m_Component:\n(?P<components>(?:  - component: {{fileID: -?[0-9]+}}\n)+)",
        re.MULTILINE,
    )
    go_match = go_pattern.search(content)
    if not go_match:
        print(f"  skip (no root GameObject): {prefab_path.relative_to(ROOT)}")
        return False

    observer_id = random_negative_file_id()
    components = go_match.group("components")
    new_components = components + f"  - component: {{fileID: {observer_id}}}\n"
    content = (
        content[: go_match.start("components")]
        + new_components
        + content[go_match.end("components") :]
    )

    observer_yaml = (
        f"--- !u!114 &{observer_id}\n"
        "MonoBehaviour:\n"
        "  m_ObjectHideFlags: 0\n"
        "  m_CorrespondingSourceObject: {fileID: 0}\n"
        "  m_PrefabInstance: {fileID: 0}\n"
        "  m_PrefabAsset: {fileID: 0}\n"
        f"  m_GameObject: {{fileID: {go_id}}}\n"
        "  m_Enabled: 1\n"
        "  m_EditorHideFlags: 0\n"
        f"  m_Script: {{fileID: 11500000, guid: {NETWORK_OBSERVER_GUID}, type: 3}}\n"
        "  m_Name: \n"
        "  m_EditorClassIdentifier: \n"
        "  _overrideType: 3\n"
        "  _updateHostVisibility: 1\n"
        "  _observerConditions:\n"
        f"  - {GRID_CONDITION_REF}\n"
    )

    prefab_path.write_text(content.rstrip() + "\n" + observer_yaml, encoding="utf-8")
    return True


def main() -> None:
    guid_index = build_guid_index()
    prefab_paths: set[Path] = set()

    for asset_path in RESOURCES.rglob("*.asset"):
        prefab_guid = extract_prefab_guid(asset_path)
        if not prefab_guid:
            continue
        prefab_path = resolve_prefab_path(prefab_guid, guid_index)
        if prefab_path:
            prefab_paths.add(prefab_path)
        else:
            print(f"missing prefab for {asset_path.relative_to(ROOT)}")

    updated = 0
    skipped = 0
    for prefab_path in sorted(prefab_paths):
        if patch_prefab(prefab_path):
            updated += 1
            print(f"updated: {prefab_path.relative_to(ROOT)}")
        else:
            skipped += 1

    print(f"Done. Updated {updated}, skipped/already configured {skipped}.")


if __name__ == "__main__":
    main()
