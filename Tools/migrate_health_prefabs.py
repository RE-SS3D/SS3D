#!/usr/bin/env python3
"""Migrate Human health prefabs for Phase 0 health rewrite."""

from __future__ import annotations

import re
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parents[1]

REMOVE_GUIDS = {
    "80e876a9101cc6042b1b5a1fbf89b810",
    "f6efa7aa428bc2449a4e1933c49b5d68",
    "8a01db53685baa84d8ca938524f2638f",
    "81823982a1f0a0a4b8b5b6c5915d1083",
}

ANATOMY_NODE_GUID = "d25aaf3a871b3d1438c1a27c2c6b57c6"
REPLACE_WITH_ANATOMY = {
    "692f4890b7494814083561e6206c4334",
    "de75930610499f24788224d6b0e10d6b",
    "87fb11738474538428e7ab7a2732a1bb",
    "83a3281d5348d93498a0d8d0e3b7751d",
    "d25aaf3a871b3d1438c1a27c2c6b57c6",
}

ORGAN_INSTANCE_GUID = "7e0d645991b499c43ac8ca1478c49742"
REPLACE_WITH_ORGAN = {
    "7e0d645991b499c43ac8ca1478c49742",
    "d1046a032bc197247b9aab3d1812b254",
    "157870bdbb19bf744af2f60b1dfa17cc",
}

HEALTH_CONTROLLER_GUID = "dba8acd5c6a2f1c438c5d3b65c03ebc2"
ZONE_COLLIDER_GUID = "c3e4f5a6b7c8901234567890abcdef12"
LIVING_CONTROLLER_GUID = "318cd55c1238afd41875e5eb60b62493"

ZONE_BY_BONE = {
    "head": 0,
    "chest": 1,
    "spine": 1,
    "BodyColliderArm_l": 2,
    "upper_arm_l": 2,
    "forearm_l": 2,
    "hand_l": 2,
    "BodyColliderArm_r": 3,
    "forearm_r": 3,
    "hand_r": 3,
    "BodyColliderLeg_l": 4,
    "lower_leg_l": 4,
    "thigh_l": 4,
    "foot_l": 4,
    "BodyColliderLeg_r": 5,
    "thigh_r": 5,
    "foot_r": 5,
}

PREFAB_GLOBS = [
    "Assets/Content/WorldObjects/Entities/Humanoids/Human/Human.prefab",
    "Assets/Content/WorldObjects/Entities/Humanoids/Human/HumanBodyParts/*.prefab",
    "Assets/Content/WorldObjects/Entities/Humanoids/Human/HumanOrgans/*.prefab",
]

BLOCK_HEADER = re.compile(r"^--- !u!(?P<type>\d+) &(?P<id>-?\d+)$")
SCRIPT_LINE = re.compile(r"^\s*m_Script: \{fileID: 11500000, guid: (?P<guid>[a-f0-9]+), type: 3\}")
GAMEOBJECT_NAME = re.compile(r"^\s*m_Name: (?P<name>.+)$")
COMPONENT_REF = re.compile(r"^\s*- component: \{fileID: (?P<id>-?\d+)\}$")


def parse_blocks(text: str) -> list[tuple[str, list[str]]]:
    lines = text.splitlines(keepends=True)
    blocks: list[tuple[str, list[str]]] = []
    current_lines: list[str] = []

    for line in lines:
        if line.startswith("--- "):
            if current_lines:
                blocks.append((current_lines[0].strip(), current_lines))
            current_lines = [line]
        else:
            current_lines.append(line)

    if current_lines:
        blocks.append((current_lines[0].strip(), current_lines))
    return blocks


def block_meta(header: str, lines: list[str]) -> tuple[str | None, str | None]:
    match = BLOCK_HEADER.match(header)
    if not match:
        return None, None
    return match.group("type"), match.group("id")


def block_script_guid(lines: list[str]) -> str | None:
    for line in lines:
        match = SCRIPT_LINE.match(line)
        if match:
            return match.group("guid")
    return None


def block_gameobject_name(lines: list[str]) -> str | None:
    for line in lines:
        match = GAMEOBJECT_NAME.match(line)
        if match:
            return match.group("name")
    return None


def strip_legacy_fields(lines: list[str]) -> list[str]:
    skip = False
    out: list[str] = []
    legacy_keys = (
        "_parentBodyPart:",
        "HealthController:",
        "_bodyPartVolume:",
        "_bodyPart:",
        "isBleeding:",
        "_circulatoryController:",
        "_feetController:",
        "_heart:",
        "_container:",
        "_healthController:",
        "attackParticleEffect:",
        "attackType:",
        "damageAmount:",
        "_inflictToSingleLayer:",
        "_bodyLayerType:",
        "_feetHealthFactor:",
    )
    for line in lines:
        stripped = line.strip()
        if any(stripped.startswith(key) for key in legacy_keys):
            continue
        out.append(line)
    return out


def simplify_organ_block(lines: list[str]) -> list[str]:
    out = strip_legacy_fields(lines)
    for i, line in enumerate(out):
        if line.startswith("  m_Script:"):
            out[i] = f"  m_Script: {{fileID: 11500000, guid: {ORGAN_INSTANCE_GUID}, type: 3}}\n"
            break
    body = "".join(out)
    if "_type:" not in body:
        out.insert(-1, "  _type: 0\n")
        out.insert(-1, "  _functionPercent: 100\n")
    return out


def simplify_anatomy_block(lines: list[str]) -> list[str]:
    out = strip_legacy_fields(lines)
    for i, line in enumerate(out):
        if line.startswith("  m_Script:"):
            out[i] = f"  m_Script: {{fileID: 11500000, guid: {ANATOMY_NODE_GUID}, type: 3}}\n"
            break
    body = "".join(out)
    if "_primaryZone:" not in body:
        out.insert(-1, "  _primaryZone: 0\n")
        out.insert(-1, "  _isDetachable: 0\n")
    return out


def zone_collider_block(file_id: int, game_object_id: str, zone: int) -> list[str]:
    return [
        f"--- !u!114 &{file_id}\n",
        "MonoBehaviour:\n",
        "  m_ObjectHideFlags: 0\n",
        "  m_CorrespondingSourceObject: {fileID: 0}\n",
        "  m_PrefabInstance: {fileID: 0}\n",
        "  m_PrefabAsset: {fileID: 0}\n",
        f"  m_GameObject: {{fileID: {game_object_id}}}\n",
        "  m_Enabled: 1\n",
        "  m_EditorHideFlags: 0\n",
        f"  m_Script: {{fileID: 11500000, guid: {ZONE_COLLIDER_GUID}, type: 3}}\n",
        "  m_Name: \n",
        "  m_EditorClassIdentifier: \n",
        f"  _zone: {zone}\n",
    ]


def migrate_prefab(path: Path) -> bool:
    original = path.read_text(encoding="utf-8")
    blocks = parse_blocks(original)

    removed_ids: set[str] = set()
    gameobject_names: dict[str, str] = {}

    for header, lines in blocks:
        block_type, block_id = block_meta(header, lines)
        if block_type == "114":
            guid = block_script_guid(lines)
            if guid in REMOVE_GUIDS and block_id:
                removed_ids.add(block_id)
        if block_type == "1" and block_id:
            name = block_gameobject_name(lines)
            if name:
                gameobject_names[block_id] = name

    processed: list[tuple[str, list[str]]] = []
    for header, lines in blocks:
        block_type, block_id = block_meta(header, lines)
        if block_type == "114":
            guid = block_script_guid(lines)
            if guid in REMOVE_GUIDS:
                continue
            if guid in REPLACE_WITH_ANATOMY:
                processed.append((header, simplify_anatomy_block(lines)))
                continue
            if guid in REPLACE_WITH_ORGAN:
                processed.append((header, simplify_organ_block(lines)))
                continue
            if guid == HEALTH_CONTROLLER_GUID:
                processed.append((header, strip_legacy_fields(lines)))
                continue
            if guid == LIVING_CONTROLLER_GUID:
                processed.append((header, strip_legacy_fields(lines)))
                continue
        processed.append((header, lines))

    if path.name == "Human.prefab":
        existing_zone_targets: set[str] = set()
        for header, lines in processed:
            block_type, block_id = block_meta(header, lines)
            if block_type != "114" or block_script_guid(lines) != ZONE_COLLIDER_GUID:
                continue
            for line in lines:
                if "m_GameObject:" in line:
                    existing_zone_targets.add(line.strip())

        next_id = 9100000000000000000
        updated_gameobjects: dict[str, list[str]] = {}
        for header, lines in processed:
            block_type, block_id = block_meta(header, lines)
            if block_type != "1" or not block_id:
                continue
            name = gameobject_names.get(block_id)
            if name not in ZONE_BY_BONE:
                continue
            go_ref = f"m_GameObject: {{fileID: {block_id}}}"
            if any(go_ref in entry for entry in existing_zone_targets):
                continue
            collider_id = next_id
            next_id += 1
            processed.append((f"--- !u!114 &{collider_id}", zone_collider_block(collider_id, block_id, ZONE_BY_BONE[name])))
            existing_zone_targets.add(go_ref)

            new_lines: list[str] = []
            inserted = False
            for line in lines:
                match = COMPONENT_REF.match(line)
                if match and match.group("id") in removed_ids:
                    continue
                new_lines.append(line)
                if not inserted and line.startswith("  m_Layer:"):
                    new_lines.insert(-1, f"  - component: {{fileID: {collider_id}}}\n")
                    inserted = True
            updated_gameobjects[block_id] = new_lines

        processed = [
            (header, updated_gameobjects.get(block_meta(header, lines)[1], lines) if block_meta(header, lines)[0] == "1" else lines)
            for header, lines in processed
        ]

    final_blocks: list[tuple[str, list[str]]] = []
    for header, lines in processed:
        block_type, _ = block_meta(header, lines)
        if block_type == "1":
            filtered = []
            for line in lines:
                match = COMPONENT_REF.match(line)
                if match and match.group("id") in removed_ids:
                    continue
                filtered.append(line)
            final_blocks.append((header, filtered))
        else:
            final_blocks.append((header, lines))

    migrated = "".join(line for _, lines in final_blocks for line in lines)
    if migrated != original:
        path.write_text(migrated, encoding="utf-8")
        return True
    return False


def main() -> int:
    changed = 0
    for pattern in PREFAB_GLOBS:
        for path in sorted(REPO.glob(pattern)):
            if migrate_prefab(path):
                print(f"updated {path.relative_to(REPO)}")
                changed += 1
    print(f"Done. {changed} prefab(s) updated.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
