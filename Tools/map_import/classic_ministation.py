#!/usr/bin/env python3
"""One-time build of the 2021 MiniStation (RE-SS3D PR #857) as a station template.

Builds/Game/Data/StationTemplates/classic-ministation.json is the source of truth now, edited in
the in-game map editor. This script predates those edits and rebuilding throws them away, so it
writes nothing unless asked with --overwrite-map-edits. Change the map in the editor instead.

The source map was deleted in c8c0997dc, so it is read from git history: tiles from the
old save file, loose items from the old scene. Everything the source lacks and a round
needs today (power plant, atmos room, cable runs, spawn markers, nuke, disk) is listed in
ADDED_* and add_atmos_room.

    python3 Tools/map_import/classic_ministation.py --overwrite-map-edits

Writes the template and prints what could not be carried over.
"""
import argparse
import collections
import json
import pathlib
import re
import subprocess
import sys

from asset_ids import SCHEMA_VERSION, load_asset_ids, saved_entry

SOURCE_COMMIT = "5f7e737a238412838decebc88c0e4274b6a31f12"
SOURCE_SAVE = "Assets/StreamingAssets/Saves/MiniStation.txt"
SOURCE_SCENE = "Assets/Content/Scenes/Maps/MiniStation.unity"
OUTPUT = pathlib.Path("Builds/Game/Data/StationTemplates/classic-ministation.json")
ASSET_IDS = load_asset_ids()

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
    "AtmosPipesMixed": 9, "AtmosPipesSupply": 10, "AtmosPipesWaste": 11, "AtmosPipesSurface": 4,
    "GasPump": 4, "AtmosValveManual": 4, "AtmosManifold4": 12,
}

TILE_RENAMES = {
    "AirlockCivilianGlass": "CivillianAirlock", "AirlockGlass": "CivillianAirlock",
    "AirlockMaintenanceGlass": "CivillianAirlock",
    "BarStool": "PlasticChairGrey", "BoozeVendor": "TableBoozeDispenser", "ChemicalHeater": "MachineFrame",
    "CigaretteVendor": "VendorYouTool", "ConsoleCrewMonitor": "MachineFrame", "ConsoleOff": "MachineFrame",
    "Cryotube - Blue": "Cryotube", "FilingCabinet": "Locker", "Jukebox": "JukeboxBlueGreen",
    "PipesLayer1": "AtmosPipesSupply", "PipesLayer3": "AtmosPipesWaste", "SnackVendor": "VendorHotDrinks",
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
# South of the corridor: a 7x3 atmos room, a wall with windows, then a sealed 7x2 air chamber.
ROOM_COLUMNS = range(-1, 6)
ROOM_FLOOR_ROWS = range(-8, -5)
CHAMBER_FLOOR_ROWS = range(-11, -9)
ROOM_DOOR = (2, -5)
ROOM_WINDOWS = {(0, -9), (4, -9)}
ROOM_SHELL = sorted(
    {(x, y) for x in range(-2, 7) for y in (-9, -12)} | {(x, y) for x in (-2, 6) for y in range(-12, -5)}
)
WASTE_OUTLET_VENT = (7, -8)  # on a catwalk outside the east wall, open to space
ADDED_SUPPLY_PIPES = [
    *((x, -3) for x in range(8, 16)),  # over the catwalk to the east wing
    (2, -4), (2, -5), (2, -6),  # from the corridor into the room
    (1, -6), (0, -6), (0, -7), (0, -8),  # down to the air pump's outlet
    (3, -6), (4, -6), (4, -7),  # the room's own vent
]
ADDED_WASTE_PIPES = [
    *((x, -3) for x in range(8, 16)),  # over the catwalk, so every scrubber shares one outlet
    (5, -4), (5, -5), (5, -6),  # from the corridor into the room
]
ADDED_MIXED_PIPES = [
    *((2, y) for y in range(-8, -12, -1)),  # from the chamber's intake up to the air pump's inlet
    (5, -8), (6, -8), WASTE_OUTLET_VENT,  # from the waste valve out through the hull
]
ADDED_SURFACE_PIPES = [(2, -8), (0, -8), (5, -6), (5, -8)]  # each sits on a manifold, at a device's port
ADDED_ATMOS_TILES = [
    ("Scrubber", 2, -10, 0),  # the chamber's intake
    ("AtmosManifold4", 2, -8, 0), ("GasPump", 1, -8, 6), ("AtmosManifold4", 0, -8, 0),  # chamber to Supply
    ("AtmosManifold4", 5, -6, 0), ("AtmosValveManual", 5, -7, 4), ("AtmosManifold4", 5, -8, 0),  # Waste to space
    ("Vent", 4, -7, 0),
    ("Vent", *WASTE_OUTLET_VENT, 0),
    ("APC", 1, -9, 4), ("APC", 3, -9, 0),
    ("LightBulbFixture", -2, -7, 2), ("LightBulbFixture", 6, -7, 6), ("LightBulbFixture", -2, -10, 2),
]
AIR = [20.79, 83.17, 0.0, 0.0]  # gas fractions by id, the map's own air
PIPE_LEFT = 10  # TileLayer.PipeLeft, the Supply line
ADDED_GAS_MARKERS = [
    (0, 3, -10, 0, 500.0),  # Source: the chamber
    (1, 3, -6, PIPE_LEFT, 300.0),  # LineFill: Supply starts charged
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


def add_atmos_room(cells):
    """Air chamber and atmos room south of the corridor, with the Supply line fed from the chamber.

    The intake scrubber fills a short Mixed run that a manifold lifts to the Surface line, where the
    air pump feeds Supply through a second manifold. Waste ends at a manual valve, then a vent out
    through the hull. The valve starts closed: until scrubbers leave room air alone, an open outlet
    would bleed the station into space faster than the intake can replace it.
    """
    for x, y in ROOM_SHELL:
        wall = "SteelWindowReinforced" if (x, y) in ROOM_WINDOWS else "SteelWallReinforced"
        cells[(x, y)] = [("Plenum", 0), (wall, 0)]
    for y in ROOM_FLOOR_ROWS:
        for x in ROOM_COLUMNS:
            cells[(x, y)] = [("Plenum", 0), ("TileGreyDark", 0)]
    for y in CHAMBER_FLOOR_ROWS:
        for x in ROOM_COLUMNS:
            cells[(x, y)] = [("Plenum", 0), ("TileReinforced", 0)]
    cells[ROOM_DOOR] = [("Plenum", 0), ("CivillianAirlock", 0)]
    cells[WASTE_OUTLET_VENT] = [("Catwalk", 0)]
    for line, pipes in (
        ("AtmosPipesSupply", ADDED_SUPPLY_PIPES),
        ("AtmosPipesWaste", ADDED_WASTE_PIPES),
        ("AtmosPipesMixed", ADDED_MIXED_PIPES),
        ("AtmosPipesSurface", ADDED_SURFACE_PIPES),
    ):
        for cell in pipes:
            cells[cell].append((line, 0))
    for name, x, y, direction in ADDED_ATMOS_TILES:
        cells[(x, y)].append((name, direction))


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
    """Grows a cable tree from the plant to every APC and the waste outlet, which no APC covers.

    A cable under a device links it to the grid.
    """
    routable = {cell for cell, placed in cells.items() if any(name in ("Plenum", "Catwalk") for name, _ in placed)}
    plant = sorted((x, y) for name, x, y, _ in ADDED_TILES if name in ("PacManGenerator", "SMES"))
    tree = {plant[0]}
    targets = set(plant) | {WASTE_OUTLET_VENT}
    targets |= {cell for cell, placed in cells.items() if any(name == "APC" for name, _ in placed)}
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
            saved = saved_entry("tileObjectSOName", name, ASSET_IDS, origin={"x": x, "y": y}, dir=direction)
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
        items.append(saved_entry(
            "itemName", ITEM_RENAMES[name], ASSET_IDS,
            worldPosition=dict(zip("xyz", position)),
            rotation=dict(zip("xyzw", rotation if any(rotation) else [0.0, 0.0, 0.0, 1.0])),
        ))
    for name, x, y, z in ADDED_ITEMS:
        items.append(saved_entry("itemName", name, ASSET_IDS, worldPosition={"x": x, "y": y, "z": z}, rotation={"x": 0.0, "y": 0.0, "z": 0.0, "w": 1.0}))
    return items


def build_gas_markers():
    return {"records": [
        {"Kind": kind, "Position": {"x": float(x), "y": 0.0, "z": float(y)}, "Line": line, "PressureKpa": pressure, "GasFractions": AIR}
        for kind, x, y, line, pressure in ADDED_GAS_MARKERS
    ]}


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
    parser.add_argument("--overwrite-map-edits", action="store_true", help="replace the template, losing its map editor edits")
    args = parser.parse_args()
    if not args.overwrite_map_edits:
        sys.exit(f"{OUTPUT} is edited in the map editor; rebuilding loses those edits. See --help.")

    report = collections.Counter()
    cells = convert_tiles(read_tiles(), report)
    for name, x, y, direction in ADDED_TILES:
        cells[(x, y)].append((name, direction))
    add_atmos_room(cells)
    orient_doors(cells)
    cable_count = route_cables(cells)

    template = {
        "schemaVersion": SCHEMA_VERSION,
        "envelopeType": "station-template",
        "createdAt": "2026-11-05T00:00:00.0000000Z",
        "gameVersion": "0.0.7.6",
        "chunks": [
            {"contributorId": "tilemap", "payloadJson": json.dumps(build_tilemap(cells, build_items(report)), separators=(",", ":"))},
            {"contributorId": "spawn-points", "payloadJson": json.dumps(build_spawns(), separators=(",", ":"))},
            {"contributorId": "gas-markers", "payloadJson": json.dumps(build_gas_markers(), separators=(",", ":"))},
        ],
    }
    text = json.dumps(template, separators=(",", ":"))
    OUTPUT.write_text(text, encoding="utf8", newline="\n")
    print(f"wrote {OUTPUT}: {len(cells)} cells, {cable_count} cabled cells")
    for line, count in sorted(report.items()):
        print(f"  {count:3d} {line}")


if __name__ == "__main__":
    main()
