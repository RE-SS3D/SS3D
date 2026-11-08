#!/usr/bin/env python3
"""Prefab ids for saved maps, read without Unity.

Saved maps (schema 2) store each placed tile object and item by its prefab's
ObjectAssetReference.Id (the prefab's asset GUID, which a rename keeps) beside its name.
This module reads that name -> id table from the asset files, and stamps ids into map files.

    python3 Tools/map_import/asset_ids.py Builds/Game/Data/StationTemplates/*.json Builds/Game/Data/Tilemaps/*.json

Stamping sets the `id` of every entry whose name is a tile asset, leaves the rest alone, raises a
station template's schemaVersion to 2, and prints the names it could not resolve.
"""
import json
import pathlib
import re
import sys

REPO = pathlib.Path(__file__).resolve().parents[2]
TILE_ASSETS = REPO / "Assets/Content/Data/TileMap/Resources"
ASSET_REFERENCES = REPO / "Assets/Content/Data/ObjectAssetReferences"
SCHEMA_VERSION = 2
NAME_KEYS = ("tileObjectSOName", "itemName")


def _guid(meta_path):
    return re.search(r"^guid: (\w+)", meta_path.read_text(encoding="utf8"), re.M).group(1)


def load_asset_ids():
    """Lower-cased asset name -> prefab id, for every GenericObjectSo the tilemap loads."""
    references = {}
    for path in ASSET_REFERENCES.rglob("*.asset"):
        match = re.search(r"^  Id: (\w+)", path.read_text(encoding="utf8"), re.M)
        if match:
            references[_guid(path.with_name(path.name + ".meta"))] = (path.stem, match.group(1))

    ids = {}
    for path in TILE_ASSETS.rglob("*.asset"):
        match = re.search(r"PrefabAsset: \{fileID: \d+, guid: (\w+)", path.read_text(encoding="utf8"))
        if not match or match.group(1) not in references:
            continue
        name, asset_id = references[match.group(1)]
        if name.lower() in ids and ids[name.lower()] != asset_id:
            raise SystemExit(f"Two tile assets are named {name}")
        ids[name.lower()] = asset_id
    return ids


def saved_entry(name_key, name, ids, **fields):
    """A saved tile object or item dict with its id first, for scripts that build maps."""
    return {"id": ids.get(name.lower(), ""), name_key: name, **fields}


def stamp(node, ids, unresolved):
    if isinstance(node, list):
        return [stamp(child, ids, unresolved) for child in node]
    if not isinstance(node, dict):
        return node

    node = {key: stamp(value, ids, unresolved) for key, value in node.items()}
    for name_key in NAME_KEYS:
        name = node.get(name_key)
        if not name:
            continue
        asset_id = ids.get(name.lower())
        if asset_id is None:
            unresolved.add(name)
            asset_id = node.get("id", "")
        rest = {key: value for key, value in node.items() if key != "id"}
        return {"id": asset_id, **rest}
    return node


def stamp_file(path, ids):
    unresolved = set()
    document = json.loads(path.read_text(encoding="utf8"))
    if "chunks" in document:  # PersistenceEnvelope: payloads are JSON strings
        document["schemaVersion"] = max(document.get("schemaVersion", 1), SCHEMA_VERSION)
        for chunk in document["chunks"]:
            payload = stamp(json.loads(chunk["payloadJson"]), ids, unresolved)
            chunk["payloadJson"] = json.dumps(payload, separators=(",", ":"), ensure_ascii=False)
    else:  # legacy SavedTileMap
        document = stamp(document, ids, unresolved)
    path.write_text(json.dumps(document, separators=(",", ":"), ensure_ascii=False), encoding="utf8", newline="\n")
    return unresolved


def main(paths):
    if not paths:
        raise SystemExit(__doc__)
    ids = load_asset_ids()
    for path in map(pathlib.Path, paths):
        unresolved = stamp_file(path, ids)
        print(f"{path}: " + (f"unresolved {sorted(unresolved)}" if unresolved else "all ids set"))


if __name__ == "__main__":
    main(sys.argv[1:])
