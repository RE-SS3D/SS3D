#!/usr/bin/env python3
"""Rebuild the 2021 MiniStation (RE-SS3D PR #857) as a current station template.

The source map was deleted in c8c0997dc, so it is read from git history: tiles from the
old save file, loose items from the old scene. Everything the source lacks and a round
needs today (power plant, cable runs, spawn markers, nuke, disk) is listed in ADDED_*.

    python3 Tools/map_import/classic_ministation.py [--check]

Writes Builds/Game/Data/StationTemplates/classic-ministation.json and prints what could
not be carried over. --check fails instead of writing when the output would change.
"""
import argparse
import collections
import json
import pathlib
import re
import subprocess
import sys

SOURCE_COMMIT = "5f7e737a238412838decebc88c0e4274b6a31f12"
SOURCE_SAVE = "Assets/StreamingAssets/Saves/MiniStation.txt"
SOURCE_SCENE = "Assets/Content/Scenes/Maps/MiniStation.unity"
OUTPUT = pathlib.Path("Builds/Game/Data/StationTemplates/classic-ministation.json")

CHUNK_SIZE = 16
WALL_MOUNT_LAYERS = (5, 6)  # TileLayer.WallMountHigh, WallMountLow

# (new asset name, TileLayer) for assets whose name survived or that have a stand-in.
LAYERS = {
    "Plenum": 0, "Catwalk": 0, "Lattice": 0,
    "SteelWall": 1, "SteelWallReinforced": 1, "SteelWindow": 1, "SteelWindowReinforced": 1, "SteelGirder": 1,
    "TileGrey": 1, "TileGreyDark": 1, "TileKitchen": 1, "TilePlating": 1, "TileReinforced": 1, "TileWhite": 1,
    "TileWood": 1, "CivillianAirlock": 1, "SecurityAirlock": 1,
    "Cables": 2, "DisposalPipes": 3,
    "LightBulbFixture": 5, "LightTubeFixture": 5,
    "APC": 6, "FireAlarm": 6, "LightSwitch": 6, "PosterVertical": 6,
    "ChemicalDispenser": 7, "Cryotube": 7, "DisposalBin": 7, "DisposalOutlet": 7, "JukeboxBlueGreen": 7, "Locker": 7,
    "LockerSecure": 7, "MachineFrame": 7, "Microwave": 7, "NukeCart": 7, "OfficeChair": 7, "PacManGenerator": 7,
    "PlasticChair": 7, "PlasticChairGrey": 7, "SMES": 7, "Scrubber": 7, "TableBoozeDispenser": 7, "TableGlass": 7,
    "TablePoker": 7, "TableSteel": 7, "TableWood": 7, "VendorHotDrinks": 7, "VendorRobustSoftdrinks": 7,
    "VendorSecTech": 7, "VendorYouTool": 7, "Vent": 7,
    "AtmosPipesL1": 10, "AtmosPipesL3": 11,
}

TILE_RENAMES = {
    "AirlockCivilianGlass": "CivillianAirlock", "AirlockGlass": "CivillianAirlock",
    "AirlockMaintenanceGlass": "CivillianAirlock",
    "BarStool": "PlasticChairGrey", "BoozeVendor": "TableBoozeDispenser", "ChemicalHeater": "MachineFrame",
    "CigaretteVendor": "VendorYouTool", "ConsoleCrewMonitor": "MachineFrame", "ConsoleOff": "MachineFrame",
    "Cryotube - Blue": "Cryotube", "FilingCabinet": "Locker", "Jukebox": "JukeboxBlueGreen",
    "PipesLayer1": "AtmosPipesL1", "PipesLayer3": "AtmosPipesL3", "SnackVendor": "VendorHotDrinks",
    "SodaVendor": "VendorRobustSoftdrinks", "Stove": "Microwave", "fridge": "Locker",
}
# No current asset is close enough to stand in.
TILES_DROPPED = {"CanisterAir", "CanisterOxygen", "CanisterPlasma", "TankStorageUnit"}

ITEM_RENAMES = {
    "Banana": "DonkPocket", "BikeHorn": "BikeHorn", "Burger": "DonkPocket", "CardboardBox": "Backpack",
    "CellCharger": "PowerCell", "HardHatYellow": "TruckerCap", "HotCocoa": "Mug", "IDCard": "IDCard",
    "LaserCannon": "M4", "Milk": "SodaCanCola", "RedToolbox": "ToolboxBlue", "StunBaton": "KitchenKnife",
    "TableLamp": "FlashlightBlue", "TankPlasma": "PlasmaTank", "microwave": "Medkit", "mug": "Mug",
}
ITEMS_DROPPED = {"Cigarette", "CigarettePack", "PaperNote1"}

SECURITY_ROOM = (range(3, 9), range(7, 13))
SECURITY_DOORS = {(2, 9), (6, 6)}

# The map predates the power grid: the alcove north of the west corridor holds the plant.
ADDED_TILES = [
    ("PacManGenerator", -13, 12, 2), ("PacManGenerator", -13, 11, 2), ("PacManGenerator", -13, 10, 2),
    ("SMES", -11, 12, 0), ("SMES", -11, 11, 0),
    ("DisposalOutlet", -11, -8, 4),  # the old pipe run ends here, at the maintenance edge
    ("NukeCart", 7, 11, 4),
]
ADDED_ITEMS = [
    ("NuclearAuthenticationDisk", 5.0, 0.8, 7.0),
    ("HealthScanner", -4.5, 0.8, 1.5),
]
ADDED_SPAWNS = [
    ("Assistant", -3, -3), ("Assistant", -1, -3), ("Assistant", 1, -3), ("Assistant", 3, -3),
    ("Security", 7, 9), ("Security", 7, 8),
    ("Operative", 18, 7),
]
NUCLEAR_OPERATIVES = 2  # AntagonistSpawnCategory.NuclearOperatives


def git_show(path):
    return subprocess.check_output(["git", "show", f"{SOURCE_COMMIT}:{path}"]).decode("utf8", "replace")


def read_tiles():
    """Returns {(x, y): [(name, dir, old layer)]} in map-wide grid coordinates."""
    cells = collections.defaultdict(list)
    for tile_map in json.loads(git_show(SOURCE_SAVE))["saveObjectList"]:
        for chunk in tile_map["saveObjectList"]:
            for tile in chunk["tileObjectSaveObjectArray"]:
                for placed in tile["placedSaveObjects"]:
                    if placed["tileObjectSOName"]:
                        cell = (chunk["chunkKey"]["x"] * CHUNK_SIZE + tile["x"], chunk["chunkKey"]["y"] * CHUNK_SIZE + tile["y"])
                        cells[cell].append((placed["tileObjectSOName"], placed["dir"], tile["layer"]))
    return cells


def prefab_names():
    paths = subprocess.check_output(["git", "ls-tree", "-r", "--name-only", SOURCE_COMMIT]).decode().splitlines()
    metas = [path for path in paths if path.endswith(".prefab.meta")]
    query = "\n".join(f"{SOURCE_COMMIT}:{path}" for path in metas).encode()
    blob = subprocess.run(["git", "cat-file", "--batch"], input=query, capture_output=True, check=True).stdout
    names, offset = {}, 0
    for path in metas:
        header_end = blob.index(b"\n", offset)
        size = int(blob[offset:header_end].split()[-1])
        guid = re.search(rb"guid: (\w+)", blob[header_end + 1:header_end + 1 + size])
        offset = header_end + 1 + size + 1
        if guid:
            names[guid.group(1).decode()] = path[:-len(".prefab.meta")]
    return names


def read_items():
    """Returns [(prefab name, x, y, z, rotation)] for prefab instances under the scene's Items root."""
    prefabs = prefab_names()
    documents = re.split(r"^--- ", git_show(SOURCE_SCENE), flags=re.M)
    items = []
    for document in documents:
        if not document.startswith("!u!1001"):
            continue
        source = re.search(r"m_SourcePrefab: \{fileID: \d+, guid: (\w+)", document)
        path = prefabs.get(source.group(1), "") if source else ""
        if "/Items/" not in path and not path.endswith(("/microwave", "/CellCharger")):
            continue
        values = {key: float(value) for key, value in re.findall(r"propertyPath: (m_Local(?:Position|Rotation)\.\w)\n\s+value: ([-\de.]+)", document)}
        position = [values.get(f"m_LocalPosition.{axis}", 0.0) for axis in "xyz"]
        rotation = [values.get(f"m_LocalRotation.{axis}", 0.0) for axis in "xyzw"]
        items.append((path.rsplit("/", 1)[-1], position, rotation))
    return sorted(items, key=lambda item: (item[0], item[1]))


def convert_tiles(source, report):
    """Returns {(x, y): [(name, dir)]} with old assets renamed and doors moved to the turf layer."""
    cells = collections.defaultdict(list)
    for cell, placed in source.items():
        is_door = any("Airlock" in name for name, _, _ in placed)
        for name, direction, _ in placed:
            if name in TILES_DROPPED:
                report[f"dropped {name}"] += 1
                continue
            if name.startswith("PosterV"):
                name = "PosterVertical"
            new_name = TILE_RENAMES.get(name, name)
            if new_name not in LAYERS:
                raise SystemExit(f"No mapping for tile asset {name}")
            if new_name != name:
                report[f"{name} -> {new_name}"] += 1
            if is_door and LAYERS[new_name] == 1 and "Airlock" not in new_name:
                continue  # doors occupy the turf layer now, and only one turf fits a cell
            if new_name.startswith("Vendor") and cell[0] in SECURITY_ROOM[0] and cell[1] in SECURITY_ROOM[1]:
                new_name = "VendorSecTech"
            cells[cell].append((new_name, direction))
    return cells


def is_solid(cells, cell):
    return any("Wall" in name or "Window" in name for name, _ in cells.get(cell, []))


def orient_doors(cells):
    """Current doors span along their wall run: dir 4 between east/west walls, else 2."""
    for cell, placed in cells.items():
        for index, (name, _) in enumerate(placed):
            if "Airlock" not in name:
                continue
            x, y = cell
            east_west = is_solid(cells, (x + 1, y)) and is_solid(cells, (x - 1, y))
            door = "SecurityAirlock" if cell in SECURITY_DOORS else name
            placed[index] = (door, 4 if east_west else 2)


def route_cables(cells):
    """Grows a cable tree from the plant to every APC; a cable under an APC links it to the grid."""
    routable = {cell for cell, placed in cells.items() if any(name in ("Plenum", "Catwalk") for name, _ in placed)}
    plant = sorted((x, y) for name, x, y, _ in ADDED_TILES if name in ("PacManGenerator", "SMES"))
    tree = {plant[0]}
    targets = set(plant) | {cell for cell, placed in cells.items() if any(name == "APC" for name, _ in placed)}
    while targets - tree:
        frontier = collections.deque(sorted(tree))
        came_from = {cell: None for cell in tree}
        reached = None
        while frontier and reached is None:
            cell = frontier.popleft()
            for step in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                neighbour = (cell[0] + step[0], cell[1] + step[1])
                if neighbour in came_from or neighbour not in routable:
                    continue
                came_from[neighbour] = cell
                if neighbour in targets:
                    reached = neighbour
                    break
                frontier.append(neighbour)
        if reached is None:
            raise SystemExit(f"Cannot route a cable to {sorted(targets - tree)}")
        while reached is not None and reached not in tree:
            tree.add(reached)
            reached = came_from[reached]
    for cell in tree:
        if not any(name == "Cables" for name, _ in cells[cell]):
            cells[cell].append(("Cables", 0))
    return len(tree)


def build_tilemap(cells, items):
    chunks = collections.defaultdict(list)
    references = []

    def location(cell):
        return (cell[0] // CHUNK_SIZE, cell[1] // CHUNK_SIZE), (cell[0] % CHUNK_SIZE, cell[1] % CHUNK_SIZE)

    def add(key, kind, data):
        rid = 1000 + len(references)
        references.append({"rid": rid, "type": {"class": kind, "ns": "SS3D.Systems.Tile", "asm": "SS3D.Systems"}, "data": data})
        chunks[key].append({"rid": rid})

    for cell in sorted(cells):
        key, (x, y) = location(cell)
        mounts = collections.defaultdict(list)
        for name, direction in sorted(cells[cell], key=lambda placed: LAYERS[placed[0]]):
            saved = {"tileObjectSOName": name, "origin": {"x": x, "y": y}, "dir": direction}
            if LAYERS[name] in WALL_MOUNT_LAYERS:
                mounts[LAYERS[name]].append(saved)
            else:
                add(key, "SavedTileSingleLocation", {"_placedSaveObject": saved, "_x": x, "_y": y})
        for _, saved in sorted(mounts.items()):
            add(key, "SavedTileCardinalLocation", {"_placedSaveObjects": saved, "x": x, "y": y})

    saved_chunks = [
        {
            "chunkKey": {"x": key[0], "y": key[1]},
            "originPosition": {"x": float(key[0] * CHUNK_SIZE), "y": 0.0, "z": float(key[1] * CHUNK_SIZE)},
            "savedTiles": tiles,
            "areaIds": [0] * (CHUNK_SIZE * CHUNK_SIZE),  # APC-seeded default rooms flood on load
            "floorDecalIds": [],
        }
        for key, tiles in sorted(chunks.items())
    ]
    return {
        "mapName": "Classic MiniStation",
        "savedChunkList": saved_chunks,
        "savedItemList": items,
        "savedAreas": [],
        "references": {"version": 2, "RefIds": references},
    }


def build_items(report):
    items = []
    for name, position, rotation in read_items():
        if name in ITEMS_DROPPED:
            report[f"dropped {name}"] += 1
            continue
        if name not in ITEM_RENAMES:
            raise SystemExit(f"No mapping for item {name}")
        if ITEM_RENAMES[name] != name:
            report[f"{name} -> {ITEM_RENAMES[name]}"] += 1
        items.append({
            "itemName": ITEM_RENAMES[name],
            "worldPosition": dict(zip("xyz", position)),
            "rotation": dict(zip("xyzw", rotation if any(rotation) else [0.0, 0.0, 0.0, 1.0])),
        })
    for name, x, y, z in ADDED_ITEMS:
        items.append({"itemName": name, "worldPosition": {"x": x, "y": y, "z": z}, "rotation": {"x": 0.0, "y": 0.0, "z": 0.0, "w": 1.0}})
    return items


def build_spawns():
    records = []
    for job, x, y in ADDED_SPAWNS:
        antagonist = job == "Operative"
        records.append({
            "kind": 1 if antagonist else 0,
            "jobName": "" if antagonist else job,
            "antagonistCategory": NUCLEAR_OPERATIVES if antagonist else 0,
            "position": {"x": float(x), "y": 0.0, "z": float(y)},
            "direction": 0,
        })
    return {"records": records}


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--check", action="store_true", help="fail if the checked-in template is out of date")
    args = parser.parse_args()

    report = collections.Counter()
    cells = convert_tiles(read_tiles(), report)
    for name, x, y, direction in ADDED_TILES:
        cells[(x, y)].append((name, direction))
    orient_doors(cells)
    cable_count = route_cables(cells)

    template = {
        "schemaVersion": 1,
        "envelopeType": "station-template",
        "createdAt": "2026-11-05T00:00:00.0000000Z",
        "gameVersion": "0.0.7.6",
        "chunks": [
            {"contributorId": "tilemap", "payloadJson": json.dumps(build_tilemap(cells, build_items(report)), separators=(",", ":"))},
            {"contributorId": "spawn-points", "payloadJson": json.dumps(build_spawns(), separators=(",", ":"))},
        ],
    }
    text = json.dumps(template, separators=(",", ":"))
    if args.check:
        if not OUTPUT.exists() or OUTPUT.read_text(encoding="utf8") != text:
            sys.exit(f"{OUTPUT} is out of date; rerun {pathlib.Path(__file__).name}")
        return
    OUTPUT.write_text(text, encoding="utf8", newline="\n")
    print(f"wrote {OUTPUT}: {len(cells)} cells, {cable_count} cabled cells")
    for line, count in sorted(report.items()):
        print(f"  {count:3d} {line}")


if __name__ == "__main__":
    main()
